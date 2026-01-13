// =============================
// SIGNALR CONNECTION
// =============================
const messageconn = new signalR.HubConnectionBuilder()
    .withUrl("/MessageHub")
    .withAutomaticReconnect()
    .build();

// =============================
// MESSAGE SERVICE
// =============================
const MessageService = (() => {
    let started = false;

    async function start() {
        if (started) return;
        started = true;

        await messageconn.start();
        console.log("[SignalR] Connected");

        await messageconn.invoke("AttachUserSession", AuthState.userId);
    }

    async function joinGroups(groupIds) {
        for (const gid of groupIds) {
            await messageconn.invoke("JoinGroup", gid);
        }
    }

    return { start, joinGroups };
})();

// =============================
// RECONNECT HANDLING
// =============================
messageconn.onreconnected(async () => {
    console.log("[SignalR] Reconnected");
    if (AuthState.loggedIn) {
        await AuthState.bootstrap();
    }
});

// =============================
// WEBRTC STATE
// =============================
let rtcPeer = null;
let rtcTargetUserId = null;
let localStream = null;

// =============================
// GROUP MESSAGE HANDLERS
// =============================
messageconn.on("OnGroupMessage", data => {
    const { groupId, messageId } = data;
    fetch(`/api/groups/${groupId}/messages/${messageId}`)
        .then(r => r.json())
        .then(m => appendMessage(m, groupId));
});

// =============================
// WEBRTC SIGNAL HANDLERS
// =============================
messageconn.on("RtcOffer", async data => {
    rtcTargetUserId = data.fromUserId;
    await ensureRtcPeer();

    await rtcPeer.setRemoteDescription({ type: "offer", sdp: data.sdp });
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

messageconn.on("RtcHangup", closeCall);

// =============================
// WEBRTC HELPERS
// =============================
async function startCall(targetUserId) {
    AuthState.requireAuth();

    rtcTargetUserId = targetUserId;
    await ensureRtcPeer();

    const offer = await rtcPeer.createOffer();
    await rtcPeer.setLocalDescription(offer);

    messageconn.invoke("SendRtcOffer", rtcTargetUserId, offer.sdp);
}

async function ensureRtcPeer() {
    if (rtcPeer) return;

    localStream = await navigator.mediaDevices.getUserMedia({ audio: true });

    rtcPeer = new RTCPeerConnection({
        iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
    });

    localStream.getTracks().forEach(t => rtcPeer.addTrack(t, localStream));

    rtcPeer.onicecandidate = e => {
        if (e.candidate && rtcTargetUserId)
            messageconn.invoke("SendRtcIceCandidate", rtcTargetUserId, e.candidate);
    };
}

function closeCall() {
    rtcPeer?.close();
    rtcPeer = null;
    localStream?.getTracks().forEach(t => t.stop());
    localStream = null;
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
    }).then(() => input.value = "");
});

// =============================
// LOAD GROUP MESSAGES
// =============================
function loadGroupMessages(groupId) {
    return fetch(`/api/groups/${groupId}/messages`)
        .then(r => r.json())
        .then(messages => {
            const container =
                document.querySelector(`#group-${groupId} .messages`);

            if (!container) return;

            container.innerHTML = "";
            messages.forEach(m => appendMessage(m, groupId));
        });
}

// =============================
// RENDER MESSAGE
// =============================
function appendMessage(m, groupId) {
    const container =
        document.querySelector(`#group-${groupId} .messages`);

    if (!container) return;

    const div = document.createElement("div");
    div.classList.add("mb-1");
    div.innerHTML = `
        <strong>${m.senderName ?? m.senderId}</strong>: ${m.text}
        <span class="text-muted small ms-2">${m.timestamp}</span>
    `;
    container.appendChild(div);
}




