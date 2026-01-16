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

            try {
                await messageConn.start();
                console.log("[SignalR] MessageHub connected");
            } catch (err) {
                console.error("[SignalR] MessageHub failed to start:", err);
            }

            try {
                await callConn.start();
                console.log("[SignalR] CallHub connected");
            } catch (err) {
                console.error("[SignalR] CallHub failed to start:", err);
            }

            if (AuthState.loggedIn) {
                try {
                    await messageConn.invoke("AttachUserSession", AuthState.userId);
                    await callConn.invoke("AttachUserSession", AuthState.userId);
                } catch (err) {
                    console.error("[SignalR] Failed to attach user session:", err);
                }
            }
        },

        async joinGroups(groupIds) {
            for (const id of groupIds) {
                try {
                    await messageConn.invoke("JoinGroup", Number(id));
                } catch (err) {
                    console.error(`Failed to join group ${id}:`, err);
                }
            }
        }
    };

    // -----------------------------
    // GROUP CHAT
    // -----------------------------
    async function loadGroupMessages(groupId) {
        try {
            const res = await fetch(`/api/groups/${groupId}/messages`);
            if (!res.ok) return;

            const messages = await res.json();
            const container = document.querySelector(`#group-${groupId} .messages`);
            if (!container) return;

            container.innerHTML = "";
            for (const msg of messages) appendMessage(msg, groupId);
        } catch (err) {
            console.error("Error loading group messages:", err);
        }
    }

    function appendMessage(msg, groupId) {
        const container = document.querySelector(`#group-${groupId} .messages`);
        if (!container) return;

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

    async function sendGroupMessage(groupId, text) {
        if (!text?.trim()) return;

        try {
            const res = await fetch(`/api/groups/${groupId}/messages`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text })
            });

            if (!res.ok) {
                console.error(`Failed to send message to group ${groupId}:`, res.statusText);
                return;
            }

            // Optionally, you can fetch the returned message immediately
            // const { messageId } = await res.json();
            // const msgRes = await fetch(`/api/groups/${groupId}/messages/${messageId}`);
            // const msg = await msgRes.json();
            // appendMessage(msg, groupId);

            // Otherwise, SignalR notification will fetch it automatically
        } catch (err) {
            console.error(`Error sending message to group ${groupId}:`, err);
        }
    }


    messageConn.on("OnGroupMessage", async ({ groupId, messageId }) => {
        try {
            const res = await fetch(`/api/groups/${groupId}/messages/${messageId}`);
            if (!res.ok) return;
            const msg = await res.json();
            appendMessage(msg, groupId);
        } catch (err) {
            console.error("Error receiving new group message:", err);
        }
    });

    // -----------------------------
    // CALL STATE
    // -----------------------------
    let pc = null;
    let localStream = null;
    let currentCallId = null;

    async function ensurePeer() {
        if (pc) return;

        try {
            // GET LOCAL STREAM (audio + video)
            localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
        } catch (err) {
            console.error("Failed to get local media:", err);
            alert("Unable to access microphone/camera.");
            return;
        }

        // ----- ATTACH LOCAL PREVIEW IMMEDIATELY -----
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

        // ADD LOCAL TRACKS TO PEER CONNECTION
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
        if (!calleeUserId || calleeUserId <= 0) {
            alert("Please select a valid target user");
            return;
        }

        try {
            const res = await fetch("/api/calls/start", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ CalleeUserId: calleeUserId, Type: "audio" })
            });

            if (!res.ok) {
                alert("Call could not be started.");
                return;
            }

            const { callId } = await res.json();
            currentCallId = callId.toString();

            await callConn.invoke("RegisterCall", currentCallId, calleeUserId);

            await ensurePeer();
            if (!pc) return;

            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            await callConn.invoke("SendRtcOffer", currentCallId, offer.sdp);

        } catch (err) {
            console.error("Error starting call:", err);
            alert("Failed to start call. See console for details.");
        }
    }

    async function hangupCall() {
        if (!currentCallId) return;

        try {
            await callConn.invoke("EndCall", currentCallId);
        } catch (err) {
            console.error("Failed to end call:", err);
        }

        pc?.close();
        pc = null;

        localStream?.getTracks().forEach(t => t.stop());
        localStream = null;

        currentCallId = null;
        console.log("Call hung up.");
    }

    // -----------------------------
    // SIGNALING
    // -----------------------------
    callConn.on("RtcOffer", async ({ callId, sdp }) => {
        currentCallId = callId;
        await ensurePeer();
        if (!pc) return;

        try {
            await pc.setRemoteDescription({ type: "offer", sdp });
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            await callConn.invoke("SendRtcAnswer", callId, answer.sdp);
        } catch (err) {
            console.error("Failed to handle incoming RTC offer:", err);
        }
    });

    callConn.on("RtcAnswer", async ({ sdp }) => {
        if (pc) {
            try {
                await pc.setRemoteDescription({ type: "answer", sdp });
            } catch (err) {
                console.error("Failed to set remote description on answer:", err);
            }
        }
    });

    callConn.on("RtcIceCandidate", async ({ candidate }) => {
        if (pc && candidate) {
            try {
                await pc.addIceCandidate(candidate);
            } catch (err) {
                console.error("Failed to add ICE candidate:", err);
            }
        }
    });

    callConn.on("RtcHangup", hangupCall);

    // -----------------------------
    // EXPORTS
    // -----------------------------
    window.MessageService = MessageService;
    window.loadGroupMessages = loadGroupMessages;
    window.sendGroupMessage = sendGroupMessage;
    window.startCall = startCall;
    window.hangupCall = hangupCall;

})();

