(() => {
    const messageConn = new signalR.HubConnectionBuilder()
        .withUrl("/MessageHub")
        .withAutomaticReconnect()
        .build();

    const callConn = new signalR.HubConnectionBuilder()
        .withUrl("/CallHub")
        .withAutomaticReconnect()
        .build();

    const MessageService = {
        started: false,

        async start() {
            if (this.started) return;
            this.started = true;

            await messageConn.start();
            await callConn.start();

            if (AuthState.loggedIn) {
                await messageConn.invoke("AttachUserSession", AuthState.userId);
                await callConn.invoke("AttachUserSession", AuthState.userId);
            }
        },

        async joinGroups(groupIds) {
            for (const id of groupIds) {
                await messageConn.invoke("JoinGroup", Number(id));
            }
        }
    };

    // =============================
    // UI HELPERS
    // =============================
    const dmContainerParent = document.querySelector("#chat-container .dm-threads");
    const groupContainerParent = document.querySelector("#chat-container .group-threads");

    function buildThreadInput({ type, id }) {
        const wrapper = document.createElement("div");
        wrapper.className = "card-footer d-flex gap-2";

        wrapper.innerHTML = `
            <input type="text"
                   class="form-control"
                   placeholder="Type message..." />
            <button class="btn btn-primary">Send</button>
        `;

        const input = wrapper.querySelector("input");
        const button = wrapper.querySelector("button");

        const send = async () => {
            const text = input.value.trim();
            if (!text) return;

            if (type === "direct") {
                await fetch("/api/direct/messages", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "X-Session-Id": AuthState.sessionId
                    },
                    body: JSON.stringify({
                        receiverId: id,
                        text
                    })
                });
            } else {
                await fetch(`/api/groups/${id}/messages`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "X-Session-Id": AuthState.sessionId
                    },
                    body: JSON.stringify({ text })
                });
            }

            input.value = "";
        };

        button.addEventListener("click", send);
        input.addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();
                send();
            }
        });

        return wrapper;
    }

    function ensureDMContainer(userId) {
        let el = document.querySelector(`#dm-thread-${userId}`);

        if (!el) {
            el = document.createElement("div");
            el.id = `dm-thread-${userId}`;
            el.className = "dm-chat card mb-3";

            el.innerHTML = `
                <div class="card-header bg-secondary text-white">
                    DM: User #${userId}
                </div>
                <div class="card-body messages" style="height:200px;overflow:auto;"></div>
            `;

            el.appendChild(buildThreadInput({
                type: "direct",
                id: userId
            }));

            dmContainerParent.appendChild(el);
        }

        return el.querySelector(".messages");
    }

    function ensureGroupContainer(groupId) {
        let el = document.querySelector(`#group-thread-${groupId}`);

        if (!el) {
            el = document.createElement("div");
            el.id = `group-thread-${groupId}`;
            el.className = "group-chat card mb-3";

            el.innerHTML = `
                <div class="card-header bg-primary text-white">
                    Group #${groupId}
                </div>
                <div class="card-body messages" style="height:200px;overflow:auto;"></div>
            `;

            el.appendChild(buildThreadInput({
                type: "group",
                id: groupId
            }));

            groupContainerParent.appendChild(el);
        }

        return el.querySelector(".messages");
    }

    function appendMessage(msg, { type, userId, groupId }) {
        const container =
            type === "group"
                ? ensureGroupContainer(groupId)
                : ensureDMContainer(userId);

        const div = document.createElement("div");
        div.className = "mb-1";

        div.innerHTML = `
            <strong>${msg.senderName ?? msg.senderId}</strong>:
            ${msg.text}
            <span class="text-muted small ms-2">
                ${msg.timestamp ?? ""}
            </span>
        `;

        container.appendChild(div);
        container.scrollTop = container.scrollHeight;
    }

    // =============================
    // DIRECT MESSAGES
    // =============================
    async function loadDirectMessages(userId) {
        const res = await fetch(`/api/direct/messages/with/${userId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (!res.ok) return;

        const messages = await res.json();
        for (const msg of messages) {
            appendMessage(msg, { type: "direct", userId });
        }
    }

    messageConn.on("OnDirectMessage", async ({ senderId, messageId }) => {
        const res = await fetch(`/api/direct/messages/single/${messageId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (!res.ok) return;

        const msg = await res.json();

        // ✅ FIX: determine correct thread owner
        const threadUser =
            msg.senderId === AuthState.userId
                ? msg.receiverId
                : msg.senderId;

        appendMessage(msg, {
            type: "direct",
            userId: threadUser
        });
    });

    // =============================
    // GROUP CHAT (FIXED)
    // =============================
    messageConn.on("OnGroupMessage", async ({ groupId, messageId }) => {
        const res = await fetch(`/api/groups/${groupId}/messages/${messageId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (!res.ok) return;

        const msg = await res.json();

        appendMessage(msg, {
            type: "group",
            groupId
        });
    });

    async function loadGroupMessages(groupId) {
        const res = await fetch(`/api/groups/${groupId}/messages`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (!res.ok) return;

        const messages = await res.json();
        for (const msg of messages) {
            appendMessage(msg, { type: "group", groupId });
        }
    }

    // -----------------------------
    // CALL STATE
    // -----------------------------
    let pc = null;
    let localStream = null;
    let currentCallId = null;

    async function ensurePeer() {
        if (pc) return;

        try {
            localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
        } catch (err) {
            console.error("Failed to get local media:", err);
            alert("Unable to access microphone/camera.");
            return;
        }

        const localVideo = document.getElementById("local-video");
        if (localVideo) {
            localVideo.srcObject = localStream;
            localVideo.muted = true;
            localVideo.autoplay = true;
            localVideo.playsInline = true;
        }

        pc = new RTCPeerConnection({
            iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
        });

        localStream.getTracks().forEach(t => pc.addTrack(t, localStream));

        pc.onicecandidate = e => {
            if (e.candidate && currentCallId) {
                callConn.invoke("SendRtcIceCandidate", currentCallId, e.candidate)
                    .catch(err => console.error("Failed to send ICE candidate:", err));
            }
        };

        pc.ontrack = e => {
            const remote = document.getElementById("remote-video");
            if (remote) remote.srcObject = e.streams[0];
        };
    }

    async function startCall(calleeUserId) {
        if (!calleeUserId || calleeUserId <= 0) return;

        const res = await fetch("/api/calls/start", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ CalleeUserId: calleeUserId, Type: "audio" })
        });

        if (!res.ok) return;

        const { callId } = await res.json();
        currentCallId = callId.toString();

        await callConn.invoke("RegisterCall", currentCallId, calleeUserId);

        await ensurePeer();

        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        await callConn.invoke("SendRtcOffer", currentCallId, offer.sdp);
    }

    async function hangupCall() {
        if (!currentCallId) return;

        await callConn.invoke("EndCall", currentCallId);

        pc?.close();
        pc = null;
        localStream?.getTracks().forEach(t => t.stop());

        currentCallId = null;
    }

    // -----------------------------
    // SIGNALING
    // -----------------------------
    callConn.on("RtcOffer", async ({ callId, sdp }) => {
        currentCallId = callId;
        await ensurePeer();

        await pc.setRemoteDescription({ type: "offer", sdp });
        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);

        await callConn.invoke("SendRtcAnswer", callId, answer.sdp);
    });

    callConn.on("RtcAnswer", async ({ sdp }) => {
        if (pc) await pc.setRemoteDescription({ type: "answer", sdp });
    });

    callConn.on("RtcIceCandidate", async ({ candidate }) => {
        if (pc && candidate) await pc.addIceCandidate(candidate);
    });

    callConn.on("RtcHangup", hangupCall);

    // =====================================================
    // 🔹 ADDITIONS — CHAT INPUT + SEND HANDLERS
    // =====================================================

    function getActiveChatContext() {
        return {
            type: document.body.dataset.chatType, // "direct" | "group"
            id: parseInt(document.body.dataset.chatId || "0")
        };
    }

    async function sendMessage(text) {
        if (!text) return;

        const ctx = getActiveChatContext();
        if (!ctx.id) return;

        const payload =
            ctx.type === "group"
                ? { GroupId: ctx.id, Text: text }
                : { RecipientUserId: ctx.id, Text: text };

        const url =
            ctx.type === "group"
                ? "/api/groups/send"
                : "/api/direct/send";

        await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Session-Id": AuthState.sessionId
            },
            body: JSON.stringify(payload)
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        const input = document.getElementById("chat-input");
        const btn = document.getElementById("chat-send");

        if (!input || !btn) return;

        btn.addEventListener("click", () => {
            const text = input.value.trim();
            if (!text) return;
            sendMessage(text);
            input.value = "";
        });

        input.addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();
                btn.click();
            }
        });
    });

    // =============================
    // EXPORTS
    // =============================
    window.MessageService = MessageService;
    window.loadDirectMessages = loadDirectMessages;
    window.loadGroupMessages = loadGroupMessages;
    window.appendMessage = appendMessage;
    window.startCall = startCall;
    window.hangupCall = hangupCall;
})();








