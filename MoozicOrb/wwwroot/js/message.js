// =============================
// EXISTING CONSTANTS
// =============================
const GROUP_ID = 9;
let lastMessageId = 0;

// =============================
// SIGNALR CONNECTION
// =============================
const messageconn = new signalR.HubConnectionBuilder()
    .withUrl("/MessageHub")
    .withAutomaticReconnect()
    .build();

// =============================
// WEBRTC STATE
// =============================
let rtcPeer = null;
let rtcTargetUserId = null;
let localStream = null;

// =============================
// START CONNECTION
// =============================
messageconn.start()
    .then(() => {
        console.log("MessageHub connected");

        // Join chat group
        messageconn.invoke("JoinGroup", GROUP_ID);

        loadGroupMessages();
    })
    .catch(console.error);

// =============================
// GROUP MESSAGE NOTIFY
// =============================
messageconn.on("OnGroupMessage", data => {
    if (data.groupId !== GROUP_ID) return;
    if (data.messageId <= lastMessageId) return;

    fetch(`/api/groups/${GROUP_ID}/messages/${data.messageId}`)
        .then(r => r.json())
        .then(m => {
            appendMessage(m);
            lastMessageId = m.messageId;
        });
});

// =============================
// WEBRTC SIGNAL HANDLERS
// =============================
messageconn.on("RtcOffer", async data => {
    rtcTargetUserId = data.fromUserId;
    await ensureRtcPeer();

    await rtcPeer.setRemoteDescription({
        type: "offer",
        sdp: data.sdp
    });

    const answer = await rtcPeer.createAnswer();
    await rtcPeer.setLocalDescription(answer);

    messageconn.invoke(
        "SendRtcAnswer",
        rtcTargetUserId,
        answer.sdp
    );
});

messageconn.on("RtcAnswer", async data => {
    await rtcPeer.setRemoteDescription({
        type: "answer",
        sdp: data.sdp
    });
});

messageconn.on("RtcIceCandidate", async data => {
    if (!rtcPeer) return;

    await rtcPeer.addIceCandidate(data.candidate);
});

messageconn.on("RtcHangup", () => {
    closeCall();
});

// =============================
// WEBRTC HELPERS
// =============================
async function startCall(targetUserId) {
    rtcTargetUserId = targetUserId;
    await ensureRtcPeer();

    const offer = await rtcPeer.createOffer();
    await rtcPeer.setLocalDescription(offer);

    messageconn.invoke(
        "SendRtcOffer",
        rtcTargetUserId,
        offer.sdp
    );
}

async function ensureRtcPeer() {
    if (rtcPeer) return;

    localStream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        video: false
    });

    rtcPeer = new RTCPeerConnection({
        iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
    });

    localStream.getTracks().forEach(t =>
        rtcPeer.addTrack(t, localStream)
    );

    rtcPeer.onicecandidate = e => {
        if (e.candidate && rtcTargetUserId) {
            messageconn.invoke(
                "SendRtcIceCandidate",
                rtcTargetUserId,
                e.candidate
            );
        }
    };

    rtcPeer.ontrack = e => {
        const audio = document.getElementById("audio-player");
        if (audio) audio.srcObject = e.streams[0];
    };
}

function closeCall() {
    if (rtcPeer) {
        rtcPeer.close();
        rtcPeer = null;
    }

    if (localStream) {
        localStream.getTracks().forEach(t => t.stop());
        localStream = null;
    }

    rtcTargetUserId = null;
}

// =============================
// EXISTING MESSAGE SEND
// =============================
$(document).on("click", ".send-group", function () {
    const groupId = $(this).data("group-id");
    const input = $(`#group-${groupId} .group-text`);
    const text = input.val();

    if (!text) return;

    fetch(`/api/groups/${groupId}/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text })
    })
        .then(() => input.val(""))
        .catch(console.error);
});

// =============================
// LOAD ALL
// =============================
function loadGroupMessages() {
    fetch(`/api/groups/${GROUP_ID}/messages`)
        .then(r => r.json())
        .then(messages => {
            const container = $(`#group-${GROUP_ID} .messages`);
            container.empty();

            messages.forEach(m => {
                appendMessage(m);
                lastMessageId = Math.max(lastMessageId, m.messageId);
            });

            container.scrollTop(container[0].scrollHeight);
        });
}

// =============================
// RENDER MESSAGE
// =============================
function appendMessage(m) {
    $(`#group-${GROUP_ID} .messages`).append(`
        <div class="mb-1">
            <strong>${m.senderName ?? m.senderId}</strong>:
            ${m.text}
            <span class="text-muted small ms-2">
                ${m.timestamp}
            </span>
        </div>
    `);
}


