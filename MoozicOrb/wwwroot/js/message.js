(() => {
    // =============================
    // 1. SIGNALR CONNECTIONS
    // =============================
    const messageConn = new signalR.HubConnectionBuilder()
        .withUrl("/MessageHub")
        .withAutomaticReconnect()
        .build();

    const callConn = new signalR.HubConnectionBuilder()
        .withUrl("/CallHub")
        .withAutomaticReconnect()
        .build();

    // =============================
    // 2. STATE MANAGEMENT
    // =============================
    const AppState = {
        activeChat: { type: null, id: null }
    };

    const MessageService = {
        started: false,

        async start() {
            if (this.started) return;
            this.started = true;

            try {
                await messageConn.start();
                await callConn.start();
                console.log("SignalR Connected");

                if (typeof AuthState !== 'undefined' && AuthState.userId) {
                    await messageConn.invoke("AttachUserSession", AuthState.userId);
                    await callConn.invoke("AttachUserSession", AuthState.userId);
                }
            } catch (err) {
                console.error("SignalR Connection Error: ", err);
            }
        },

        async joinGroups(groupIds) {
            if (!this.started) return;
            for (const id of groupIds) {
                await messageConn.invoke("JoinGroup", Number(id));
            }
        }
    };

    // =============================
    // 3. UI HELPERS
    // =============================
    const chatMessagesContainer = document.getElementById("chatMessages");
    const chatTitle = document.getElementById("chatTitle");

    function clearChatWindow() {
        if (chatMessagesContainer) chatMessagesContainer.innerHTML = '';
    }

    function appendMessage(msg, isHistory = false) {
        if (!chatMessagesContainer) return;

        // Robust name check for both casing styles
        const sName = msg.senderName || msg.SenderName || "User";

        const isForCurrentChat =
            (AppState.activeChat.type === "direct" && (msg.senderId == AppState.activeChat.id || msg.receiverId == AppState.activeChat.id)) ||
            (AppState.activeChat.type === "group" && msg.groupId == AppState.activeChat.id);

        if (!isForCurrentChat && !isHistory) {
            console.log("Background message:", msg);
            return;
        }

        const div = document.createElement("div");
        const isMe = (typeof AuthState !== 'undefined') && msg.senderId == AuthState.userId;
        div.className = isMe ? "message-row me" : "message-row them";

        div.innerHTML = `
            <div class="msg-bubble">
                <small><strong>${sName}</strong></small>
                <div>${msg.text}</div>
            </div>
        `;

        chatMessagesContainer.appendChild(div);
        chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
    }

    // =============================
    // 4. LOAD MESSAGES
    // =============================
    async function loadDirectMessages(userId, username) {
        AppState.activeChat = { type: "direct", id: userId };
        if (chatTitle) chatTitle.innerText = username || `User #${userId}`;
        clearChatWindow();

        const res = await fetch(`/api/direct/messages/with/${userId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (res.ok) {
            const messages = await res.json();
            messages.forEach(m => appendMessage(m, true));
        }

        document.getElementById("chatOverlay").classList.add("active");
        const container = document.querySelector('.chat-app-container');
        if (container) container.classList.add('conversation-active');
    }

    async function loadGroupMessages(groupId, groupName) {
        AppState.activeChat = { type: "group", id: groupId };
        if (chatTitle) chatTitle.innerText = groupName || `Group #${groupId}`;
        clearChatWindow();

        const res = await fetch(`/api/groups/${groupId}/messages`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (res.ok) {
            const messages = await res.json();
            messages.forEach(m => appendMessage(m, true));
        }

        document.getElementById("chatOverlay").classList.add("active");
        const container = document.querySelector('.chat-app-container');
        if (container) container.classList.add('conversation-active');
    }

    // =============================
    // 5. SIGNALR EVENTS (THE FIX)
    // =============================
    messageConn.on("OnDirectMessage", async ({ senderId, messageId }) => {
        const res = await fetch(`/api/direct/messages/single/${messageId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (res.ok) {
            const msg = await res.json();

            // 1. Identify Partner ID
            const partnerId = (msg.senderId == AuthState.userId) ? msg.receiverId : msg.senderId;

            // 2. Identify Partner Name (Check CamelCase AND PascalCase)
            // If I am sender, I need ReceiverName. If I am receiver, I need SenderName.
            const rName = msg.receiverName || msg.ReceiverName;
            const sName = msg.senderName || msg.SenderName;

            let partnerName = (msg.senderId == AuthState.userId) ? rName : sName;

            // Fallback to "User ID" only if name is strictly missing
            if (!partnerName) partnerName = `User ${partnerId}`;

            if (window.AuthState && window.AuthState.ensureThread) {
                window.AuthState.ensureThread({
                    id: partnerId,
                    name: partnerName,
                    type: "direct"
                });
            }
            appendMessage(msg);
        }
    });

    messageConn.on("OnGroupMessage", async ({ groupId, messageId }) => {
        const res = await fetch(`/api/groups/${groupId}/messages/${messageId}`, {
            headers: { "X-Session-Id": AuthState.sessionId }
        });

        if (res.ok) {
            const msg = await res.json();
            if (window.AuthState && window.AuthState.ensureThread) {
                window.AuthState.ensureThread({
                    id: groupId,
                    name: `Group ${groupId}`,
                    type: "group"
                });
            }
            appendMessage(msg);
        }
    });

    // =============================
    // 6. SENDING MESSAGES
    // =============================
    async function handleSendMessage(text) {
        if (!AppState.activeChat.id) return;

        const url = AppState.activeChat.type === "group"
            ? `/api/groups/${AppState.activeChat.id}/messages`
            : "/api/direct/messages";

        const payload = AppState.activeChat.type === "group"
            ? { text: text }
            : { receiverId: AppState.activeChat.id, text: text };

        await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Session-Id": AuthState.sessionId
            },
            body: JSON.stringify(payload)
        });
    }

    // =============================
    // 7. INITIALIZATION
    // =============================
    document.addEventListener("DOMContentLoaded", () => {
        const input = document.getElementById("msgInput");
        const btn = document.getElementById("msgSendBtn");

        if (input && btn) {
            const sendTrigger = async () => {
                const text = input.value.trim();
                if (!text) return;
                await handleSendMessage(text);
                input.value = "";
            };

            btn.addEventListener("click", sendTrigger);
            input.addEventListener("keydown", (e) => {
                if (e.key === "Enter") {
                    e.preventDefault();
                    sendTrigger();
                }
            });
        }

        const startVideoBtn = document.getElementById("startVideoCallBtn");
        if (startVideoBtn) {
            startVideoBtn.addEventListener("click", () => {
                if (AppState.activeChat.type === "direct") {
                    startCall(AppState.activeChat.id);
                } else {
                    alert("Video calls are only available in Direct Messages.");
                }
            });
        }

        const hangupBtn = document.getElementById("hangupBtn");
        if (hangupBtn) {
            hangupBtn.addEventListener("click", (e) => {
                e.preventDefault();
                hangupCall();
            });
        }
    });

    // =============================
    // 8. VIDEO CALL LOGIC
    // =============================
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

        pc = new RTCPeerConnection({ iceServers: [{ urls: "stun:stun.l.google.com:19302" }] });
        localStream.getTracks().forEach(t => pc.addTrack(t, localStream));

        pc.onicecandidate = e => {
            if (e.candidate && currentCallId) {
                callConn.invoke("SendRtcIceCandidate", currentCallId, e.candidate).catch(console.error);
            }
        };

        pc.ontrack = e => {
            const remote = document.getElementById("remote-video");
            if (remote) remote.srcObject = e.streams[0];
        };
    }

    async function startCall(calleeUserId) {
        if (!calleeUserId || calleeUserId <= 0) return;

        const modal = document.getElementById("videoCallModal");
        if (modal) modal.style.display = "flex";

        const res = await fetch("/api/calls/start", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Session-Id": AuthState.sessionId
            },
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
        if (currentCallId) {
            await callConn.invoke("EndCall", currentCallId);
        }
        if (pc) {
            pc.close();
            pc = null;
        }
        if (localStream) {
            localStream.getTracks().forEach(t => t.stop());
            localStream = null;
        }
        currentCallId = null;

        const modal = document.getElementById("videoCallModal");
        if (modal) modal.style.display = "none";
    }

    // =============================
    // 9. SIGNALING LISTENERS
    // =============================
    callConn.on("RtcOffer", async ({ callId, sdp }) => {
        currentCallId = callId;
        const modal = document.getElementById("videoCallModal");
        if (modal) modal.style.display = "flex";

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

    // =============================
    // 10. EXPORTS
    // =============================
    window.MessageService = MessageService;
    window.loadDirectMessages = loadDirectMessages;
    window.loadGroupMessages = loadGroupMessages;
    window.appendMessage = appendMessage;
    window.startCall = startCall;
    window.hangupCall = hangupCall;

})();