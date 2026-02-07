(() => {
    // =============================
    // 0. RINGTONE GENERATOR (Web Audio API)
    // =============================
    const RingtoneService = {
        audioCtx: null,
        intervalId: null,

        start() {
            if (this.intervalId) return; // Already ringing

            // Create Context (Safe to do on event)
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            this.audioCtx = new AudioContext();

            const playPulse = () => {
                if (!this.audioCtx) return;
                const t = this.audioCtx.currentTime;

                // Oscillator 1 (Main Tone 800Hz)
                const osc1 = this.audioCtx.createOscillator();
                osc1.type = "sine";
                osc1.frequency.value = 800;

                // Oscillator 2 (Modulator 650Hz)
                const osc2 = this.audioCtx.createOscillator();
                osc2.type = "sine";
                osc2.frequency.value = 650;

                // Volume Envelope (Fade In/Out)
                const gain = this.audioCtx.createGain();
                gain.gain.setValueAtTime(0, t);
                gain.gain.linearRampToValueAtTime(0.3, t + 0.1);
                gain.gain.linearRampToValueAtTime(0, t + 1.5);

                osc1.connect(gain);
                osc2.connect(gain);
                gain.connect(this.audioCtx.destination);

                osc1.start(t);
                osc2.start(t);
                osc1.stop(t + 1.6);
                osc2.stop(t + 1.6);
            };

            // Play immediately, then repeat every 2.5 seconds
            playPulse();
            this.intervalId = setInterval(playPulse, 2500);
        },

        stop() {
            if (this.intervalId) {
                clearInterval(this.intervalId);
                this.intervalId = null;
            }
            if (this.audioCtx) {
                this.audioCtx.close().catch(e => console.error(e));
                this.audioCtx = null;
            }
        }
    };

    // =============================
    // 1. SIGNALR CONNECTIONS (PRIVATE - MESSAGING ONLY)
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
    // 2. STATE
    // =============================
    const AppState = { activeChat: { type: null, id: null } };

    // =============================
    // 3. UI HELPERS (Chat)
    // =============================
    const chatMessagesContainer = document.getElementById("chatMessages");
    const chatTitle = document.getElementById("chatTitle");

    function clearChatWindow() {
        if (chatMessagesContainer) chatMessagesContainer.innerHTML = '';
    }

    function appendMessage(msg, isHistory = false) {
        if (!chatMessagesContainer) return;

        const sName = msg.senderName || msg.SenderName || "User";
        const isForCurrentChat =
            (AppState.activeChat.type === "direct" && (msg.senderId == AppState.activeChat.id || msg.receiverId == AppState.activeChat.id)) ||
            (AppState.activeChat.type === "group" && msg.groupId == AppState.activeChat.id);

        if (!isForCurrentChat && !isHistory) return;

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

        const res = await fetch(`/api/direct/messages/with/${userId}`, { headers: { "X-Session-Id": AuthState.sessionId } });
        if (res.ok) {
            (await res.json()).forEach(m => appendMessage(m, true));
        }
        document.getElementById("chatOverlay").classList.add("active");
        document.querySelector('.chat-app-container')?.classList.add('conversation-active');
    }

    async function loadGroupMessages(groupId, groupName) {
        AppState.activeChat = { type: "group", id: groupId };
        if (chatTitle) chatTitle.innerText = groupName || `Group #${groupId}`;
        clearChatWindow();

        const res = await fetch(`/api/groups/${groupId}/messages`, { headers: { "X-Session-Id": AuthState.sessionId } });
        if (res.ok) {
            (await res.json()).forEach(m => appendMessage(m, true));
        }
        document.getElementById("chatOverlay").classList.add("active");
        document.querySelector('.chat-app-container')?.classList.add('conversation-active');
    }

    // =============================
    // 5. SIGNALR EVENTS (Chat)
    // =============================
    messageConn.on("OnDirectMessage", async ({ senderId, messageId }) => {
        const res = await fetch(`/api/direct/messages/single/${messageId}`, { headers: { "X-Session-Id": AuthState.sessionId } });
        if (res.ok) {
            const msg = await res.json();
            const partnerId = (msg.senderId == AuthState.userId) ? msg.receiverId : msg.senderId;
            const partnerName = (msg.senderId == AuthState.userId) ? (msg.receiverName || msg.ReceiverName) : (msg.senderName || msg.SenderName);

            // NEW: Extract image so we can pass it to ensureThread
            // This ensures that when the chat is created/moved to top, it has the right pic
            const partnerImg = (msg.senderId == AuthState.userId) ? (msg.receiverProfilePicUrl || msg.ReceiverProfilePicUrl) : (msg.senderProfilePicUrl || msg.SenderProfilePicUrl);

            // Trigger Sidebar Update (Create or Move to Top)
            if (window.AuthState?.ensureThread) {
                window.AuthState.ensureThread({
                    id: partnerId,
                    name: partnerName || `User ${partnerId}`,
                    type: "direct",
                    img: partnerImg
                });
            }
            appendMessage(msg);
        }
    });

    messageConn.on("OnGroupMessage", async ({ groupId, messageId }) => {
        const res = await fetch(`/api/groups/${groupId}/messages/${messageId}`, { headers: { "X-Session-Id": AuthState.sessionId } });
        if (res.ok) {
            const msg = await res.json();
            // Trigger Sidebar Update (Create or Move to Top)
            if (window.AuthState?.ensureThread) {
                window.AuthState.ensureThread({ id: groupId, name: `Group ${groupId}`, type: "group" });
            }
            appendMessage(msg);
        }
    });

    // =============================
    // 6. VIDEO CALL LOGIC (H264 & STATE)
    // =============================
    let pc = null;
    let localStream = null;
    let currentCallId = null;
    let incomingCallId = null; // Temp ID for incoming ring
    let incomingCallerId = null;

    // --- HELPER: Force H264 Codec ---
    function forceH264(sdp) {
        const sdpLines = sdp.split('\r\n');
        let mLineIndex = -1;
        for (let i = 0; i < sdpLines.length; i++) {
            if (sdpLines[i].startsWith('m=video')) { mLineIndex = i; break; }
        }
        if (mLineIndex === -1) return sdp;

        const h264Map = [];
        const regex = /a=rtpmap:(\d+)\s+H264/gi;
        for (const line of sdpLines) {
            let match;
            while ((match = regex.exec(line)) !== null) h264Map.push(match[1]);
        }
        if (h264Map.length === 0) return sdp;

        const mLine = sdpLines[mLineIndex];
        const elements = mLine.split(' ');
        const header = elements.slice(0, 3);
        const payloads = elements.slice(3);
        const nonH264 = payloads.filter(p => !h264Map.includes(p));
        const newPayloads = [...h264Map, ...nonH264];
        sdpLines[mLineIndex] = [...header, ...newPayloads].join(' ');
        return sdpLines.join('\r\n');
    }

    async function ensurePeer() {
        if (pc) return;
        try {
            localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
        } catch (err) {
            console.error(err);
            alert("Camera access denied.");
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

    // --- CALLER: STARTS CALL ---
    async function startCall(calleeUserId) {
        if (!calleeUserId) return;

        // 1. Show Video Modal (Local Preview)
        const modal = document.getElementById("videoCallModal");
        if (modal) modal.style.display = "flex";
        await ensurePeer();

        // 2. Notify API
        const res = await fetch("/api/calls/start", {
            method: "POST",
            headers: { "Content-Type": "application/json", "X-Session-Id": AuthState.sessionId },
            body: JSON.stringify({ CalleeUserId: calleeUserId, Type: "audio" })
        });

        if (res.ok) {
            const { callId } = await res.json();
            currentCallId = callId.toString();
            console.log("Call started. Waiting for answer...");
        } else {
            alert("Failed to start call (User busy or offline).");
            hangupCall();
        }
    }

    // --- CALLEE: RECEIVES RING ---
    callConn.on("IncomingCall", ({ callId, fromUserId }) => {
        incomingCallId = callId;
        incomingCallerId = fromUserId;

        const modal = document.getElementById("incomingCallModal");
        const nameEl = document.getElementById("incomingCallerName");

        if (nameEl) nameEl.innerText = `User ${fromUserId}`;
        if (modal) modal.classList.add("active");

        // Start Ringing
        RingtoneService.start();
    });

    async function acceptIncomingCall() {
        RingtoneService.stop();
        if (!incomingCallId) return;

        // 1. Close Ringing Modal
        document.getElementById("incomingCallModal").classList.remove("active");

        // 2. Open Video Modal & Get Camera
        currentCallId = incomingCallId;
        const vidModal = document.getElementById("videoCallModal");
        if (vidModal) vidModal.style.display = "flex";
        await ensurePeer();

        // 3. Notify API
        await fetch("/api/calls/accept", {
            method: "POST",
            headers: { "Content-Type": "application/json", "X-Session-Id": AuthState.sessionId },
            body: JSON.stringify({ CallId: currentCallId, CallerUserId: incomingCallerId })
        });
    }

    async function rejectIncomingCall() {
        RingtoneService.stop();
        if (!incomingCallId) return;

        await fetch("/api/calls/reject", {
            method: "POST",
            headers: { "Content-Type": "application/json", "X-Session-Id": AuthState.sessionId },
            body: JSON.stringify({ CallId: incomingCallId, CallerUserId: incomingCallerId })
        });

        document.getElementById("incomingCallModal").classList.remove("active");
        incomingCallId = null;
    }

    // --- SIGNALING EVENTS ---

    // 1. Caller receives this when Callee clicks "Accept"
    callConn.on("CallAccepted", async ({ callId }) => {
        if (currentCallId !== callId) return;
        console.log("Call Accepted! Sending Offer...");

        const offer = await pc.createOffer();
        offer.sdp = forceH264(offer.sdp);
        await pc.setLocalDescription(offer);
        await callConn.invoke("SendRtcOffer", currentCallId, offer.sdp);
    });

    callConn.on("CallRejected", () => {
        alert("User busy or rejected the call.");
        hangupCall();
    });

    callConn.on("RtcOffer", async ({ callId, sdp }) => {
        currentCallId = callId;
        await ensurePeer();

        await pc.setRemoteDescription({ type: "offer", sdp });
        const answer = await pc.createAnswer();
        answer.sdp = forceH264(answer.sdp);
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

    async function hangupCall() {
        RingtoneService.stop(); // Safety
        if (currentCallId) await callConn.invoke("EndCall", currentCallId);

        if (pc) { pc.close(); pc = null; }
        if (localStream) { localStream.getTracks().forEach(t => t.stop()); localStream = null; }

        currentCallId = null;
        document.getElementById("videoCallModal").style.display = "none";
        document.getElementById("incomingCallModal").classList.remove("active");
    }

    // =============================
    // 7. INIT & EVENT BINDING
    // =============================
    const MessageService = {
        started: false,
        async start() {
            if (this.started) return;
            this.started = true;
            try {
                await messageConn.start();
                await callConn.start();
                if (AuthState?.userId) {
                    await messageConn.invoke("AttachUserSession", AuthState.userId);
                    await callConn.invoke("AttachUserSession", AuthState.userId);
                }
            } catch (e) { console.error(e); }
        },
        async joinGroups(groupIds) {
            for (const id of groupIds) await messageConn.invoke("JoinGroup", Number(id));
        }
    };

    // NEW: Uses API to get User Info for the Header only
    // DOES NOT force the thread into the sidebar list until a message is sent
    async function startChat(userId) {
        // 1. Auth Check
        if (typeof AuthState === 'undefined' || !AuthState.loggedIn) {
            const loginBtn = document.getElementById("loginToggleBtn");
            if (loginBtn) loginBtn.click();
            return;
        }

        let name = `User ${userId}`;

        // 2. Fetch User Info from API (For Window Title)
        try {
            const res = await fetch(`/api/direct/messages/user-info/${userId}`, {
                headers: { "X-Session-Id": AuthState.sessionId }
            });
            if (res.ok) {
                const info = await res.json();
                name = info.name;
            }
        } catch (e) {
            console.error("Failed to fetch user info for chat header", e);
        }

        // NOTE: We REMOVED ensureThread() here. 
        // The chat will appear in the sidebar only after 'OnDirectMessage' fires.

        // 3. UI Overlay Logic
        const chatOverlay = document.getElementById("chatOverlay");
        const sidebar = document.getElementById('sidebar');

        if (sidebar) sidebar.classList.remove('active');
        document.body.classList.remove('sidebar-open');
        if (chatOverlay) chatOverlay.classList.add("active");

        // 4. Load the conversation
        await loadDirectMessages(userId, name);
    }

    document.addEventListener("DOMContentLoaded", () => {
        // Send Msg
        const input = document.getElementById("msgInput");
        const sendBtn = document.getElementById("msgSendBtn");
        const triggerSend = async () => {
            if (!input.value.trim()) return;
            const payload = AppState.activeChat.type === "group"
                ? { text: input.value.trim() }
                : { receiverId: AppState.activeChat.id, text: input.value.trim() };
            const url = AppState.activeChat.type === "group"
                ? `/api/groups/${AppState.activeChat.id}/messages`
                : "/api/direct/messages";

            await fetch(url, { method: "POST", headers: { "Content-Type": "application/json", "X-Session-Id": AuthState.sessionId }, body: JSON.stringify(payload) });
            input.value = "";
        };
        if (sendBtn) sendBtn.onclick = triggerSend;
        if (input) input.onkeydown = (e) => { if (e.key === "Enter") { e.preventDefault(); triggerSend(); } };

        // Start Call
        const vidBtn = document.getElementById("startVideoCallBtn");
        if (vidBtn) vidBtn.onclick = () => {
            if (AppState.activeChat.type === "direct") startCall(AppState.activeChat.id);
            else alert("Direct messages only.");
        };

        // Hangup
        const hangBtn = document.getElementById("hangupBtn");
        if (hangBtn) hangBtn.onclick = (e) => { e.preventDefault(); hangupCall(); };

        // Answer / Reject Buttons
        const acceptBtn = document.getElementById("btnAcceptCall");
        const rejectBtn = document.getElementById("btnRejectCall");
        if (acceptBtn) acceptBtn.onclick = acceptIncomingCall;
        if (rejectBtn) rejectBtn.onclick = rejectIncomingCall;
    });

    window.MessageService = MessageService;
    window.loadDirectMessages = loadDirectMessages;
    window.loadGroupMessages = loadGroupMessages;
    window.appendMessage = appendMessage;
    window.startCall = startCall;
    window.hangupCall = hangupCall;
    window.startChat = startChat; // <-- EXPORTED HERE
})();