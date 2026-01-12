// =============================
// SIGNALR CONNECTION
// =============================
const messageconn = new signalR.HubConnectionBuilder()
    .withUrl("/MessageHub")
    .withAutomaticReconnect()
    .build();

// Track last message per group
const GROUP_ID = 9;
let lastMessageId = 0;

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
        console.log("[SignalR] MessageHub connected");

        // If already logged in, attach user and join group
        if (AuthState.loggedIn) {
            messageconn.invoke("AttachUserSession", AuthState.userId)
                .then(() => messageconn.invoke("JoinGroup", GROUP_ID))
                .catch(console.error);
        }

        // Load initial group messages
        loadGroupMessages();
    })
    .catch(console.error);

// =============================
// GROUP MESSAGE HANDLERS
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

    messageconn.invoke("SendRtcAnswer", rtcTargetUserId, answer.sdp);
});

messageconn.on("RtcAnswer", async data => {
    if (!rtcPeer) return;
    await rtcPeer.setRemoteDescription({ type: "answer", sdp: data.sdp });
});

messageconn.on("RtcIceCandidate", async data => {
    if (!rtcPeer) return;
    await rtcPeer.addIceCandidate(data.candidate);
});

messageconn.on("RtcHangup", () => closeCall());

// =============================
// WEBRTC HELPERS
// =============================
async function startCall(targetUserId = null) {
    AuthState.requireAuth();

    rtcTargetUserId = targetUserId;
    await ensureRtcPeer();

    const offer = await rtcPeer.createOffer();
    await rtcPeer.setLocalDescription(offer);

    messageconn.invoke("SendRtcOffer", rtcTargetUserId, offer.sdp);
}

async function ensureRtcPeer() {
    if (rtcPeer) return;

    localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });

    rtcPeer = new RTCPeerConnection({
        iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
    });

    localStream.getTracks().forEach(t => rtcPeer.addTrack(t, localStream));

    rtcPeer.onicecandidate = e => {
        if (e.candidate && rtcTargetUserId) {
            messageconn.invoke("SendRtcIceCandidate", rtcTargetUserId, e.candidate);
        }
    };

    rtcPeer.ontrack = e => {
        const remoteVideo = document.getElementById("remote-video");
        if (remoteVideo) remoteVideo.srcObject = e.streams[0];
    };

    const localVideo = document.getElementById("local-video");
    if (localVideo) localVideo.srcObject = localStream;
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
// GROUP CHAT SEND
// =============================
document.addEventListener("click", e => {
    if (!e.target.matches(".send-group")) return;

    AuthState.requireAuth();

    const groupId = parseInt(e.target.dataset.groupId);
    const input = document.querySelector(`#group-${groupId} .group-text`);
    const text = input.value.trim();
    if (!text) return;

    fetch(`/api/groups/${groupId}/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text })
    })
        .then(() => input.value = "")
        .catch(console.error);
});

// =============================
// LOAD GROUP MESSAGES
// =============================
function loadGroupMessages() {
    fetch(`/api/groups/${GROUP_ID}/messages`)
        .then(r => r.json())
        .then(messages => {
            const container = document.querySelector(`#group-${GROUP_ID} .messages`);
            container.innerHTML = "";

            messages.forEach(m => {
                appendMessage(m);
                lastMessageId = Math.max(lastMessageId, m.messageId);
            });

            container.scrollTop = container.scrollHeight;
        });
}

// =============================
// RENDER MESSAGE
// =============================
function appendMessage(m) {
    const container = document.querySelector(`#group-${GROUP_ID} .messages`);
    const div = document.createElement("div");
    div.classList.add("mb-1");
    div.innerHTML = `
        <strong>${m.senderName ?? m.senderId}</strong>: ${m.text}
        <span class="text-muted small ms-2">${m.timestamp}</span>
    `;
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}

// =============================
// STREAM SERVICE (stub, auth-gated)
// =============================
const StreamService = {
    startBroadcast() {
        AuthState.requireAuth();
        console.log("[StreamService] Start Broadcast triggered");
        // TODO: implement broadcast start logic
    },
    joinStream() {
        AuthState.requireAuth();
        console.log("[StreamService] Join Stream triggered");
        // TODO: implement join stream logic
    }
};



