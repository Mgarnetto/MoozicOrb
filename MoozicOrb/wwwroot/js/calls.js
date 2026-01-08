// -----------------------------
// CALLS.JS (Controller + SignalR + Scoped)
// -----------------------------
(function () {
    let localStream;
    let remoteStream;
    let callConn;
    let pc;
    let currentCallId;
    let currentCalleeId;

    const CALL_HUB_URL = "/MessageHub";
    const configuration = { iceServers: [{ urls: "stun:stun.l.google.com:19302" }] };

    function setCallStatus(msg) {
        const el = document.getElementById("call-status");
        if (el) el.textContent = `Status: ${msg}`;
    }

    callConn = new signalR.HubConnectionBuilder()
        .withUrl(CALL_HUB_URL)
        .withAutomaticReconnect()
        .build();

    callConn.start()
        .then(() => { console.log("MessageHub connected for calls"); setCallStatus("idle"); })
        .catch(err => console.error("SignalR connection failed:", err));

    async function startLocalStream() {
        if (!localStream) {
            try {
                localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
                const localVideo = document.getElementById("local-video");
                if (localVideo) localVideo.srcObject = localStream;
            } catch (err) {
                console.error("Error accessing local media:", err);
                setCallStatus("Failed to access media");
            }
        }
        return localStream;
    }

    function createPeerConnection(toUserId) {
        pc = new RTCPeerConnection(configuration);

        pc.onicecandidate = event => {
            if (event.candidate)
                callConn.invoke("SendRtcIceCandidate", toUserId, event.candidate);
        };

        pc.ontrack = event => {
            remoteStream = event.streams[0];
            const remoteVideo = document.getElementById("remote-video");
            if (remoteVideo) remoteVideo.srcObject = remoteStream;
        };

        if (localStream) {
            localStream.getTracks().forEach(track => pc.addTrack(track, localStream));
        }
    }

    async function startCall(calleeUserId = 2, type = "audio") {
        setCallStatus("Calling...");
        await startLocalStream();

        try {
            const response = await fetch("/api/calls/start", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ CalleeUserId: calleeUserId, Type: type })
            });
            const data = await response.json();
            currentCallId = data.callId;
            currentCalleeId = calleeUserId;

            createPeerConnection(calleeUserId);
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);

            await callConn.invoke("SendRtcOffer", calleeUserId, offer.sdp);
        } catch (err) {
            console.error("Failed to start call:", err);
            setCallStatus("Failed to start call");
        }
    }

    async function hangupCall(calleeUserId = currentCalleeId) {
        if (pc) { pc.close(); pc = null; }
        setCallStatus("Call ended");
        if (!calleeUserId) return;

        try {
            await callConn.invoke("SendRtcHangup", calleeUserId);
        } catch (err) { console.error("Error sending hangup:", err); }
    }

    callConn.on("RtcOffer", async ({ fromUserId, sdp, callId }) => {
        setCallStatus("Incoming call...");
        await startLocalStream();
        createPeerConnection(fromUserId);

        await pc.setRemoteDescription({ type: "offer", sdp });
        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);

        await callConn.invoke("SendRtcAnswer", fromUserId, answer.sdp);
        setCallStatus("Call connected");
    });

    callConn.on("RtcAnswer", async ({ fromUserId, sdp }) => {
        if (!pc) return;
        await pc.setRemoteDescription({ type: "answer", sdp });
        setCallStatus("Call connected");
    });

    callConn.on("RtcIceCandidate", async ({ fromUserId, candidate }) => {
        if (pc && candidate) {
            try { await pc.addIceCandidate(candidate); }
            catch (err) { console.error("Error adding ICE candidate:", err); }
        }
    });

    callConn.on("RtcHangup", ({ fromUserId }) => { hangupCall(fromUserId); });

    window.startCall = startCall;
    window.hangupCall = hangupCall;
})();



