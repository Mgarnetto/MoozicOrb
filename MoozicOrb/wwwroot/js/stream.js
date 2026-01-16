// =============================
// STREAM.JS (Production-Ready)
// =============================
(() => {

    const StreamService = {
        connection: null,
        streamId: null,
        role: null,
        pc: null,
        localStream: null,

        // -----------------------------
        // INIT
        // -----------------------------
        async init() {
            if (this.connection) return;

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/StreamHub")
                .withAutomaticReconnect()
                .build();

            this.registerHubHandlers();
            await this.connection.start();

            console.log("[StreamService] Connected");
        },

        // -----------------------------
        // START BROADCAST
        // -----------------------------
        async startBroadcast({ audio = true, video = true } = {}) {
            await this.init();
            this.role = "broadcaster";

            // Create stream server-side
            const res = await fetch("/api/streams", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: new URLSearchParams({ sessionId: AuthState.sessionId })
            });

            if (!res.ok)
                throw new Error("Failed to create stream");

            const { streamId } = await res.json();
            this.streamId = streamId;

            console.log("[StreamService] StreamId:", streamId);

            this.createPeerConnection();

            // Get media
            this.localStream = await navigator.mediaDevices.getUserMedia({
                audio,
                video
            });

            // ---- LOCAL PREVIEW (IMMEDIATE) ----
            const preview = document.getElementById("stream-local-video");
            if (preview) {
                preview.srcObject = this.localStream;
                preview.muted = true;
                preview.playsInline = true;
                preview.autoplay = true;
                preview.style.display = "block";
            }

            this.localStream.getTracks().forEach(t =>
                this.pc.addTrack(t, this.localStream)
            );

            await this.connection.invoke(
                "JoinStream",
                this.streamId,
                "broadcaster"
            );

            const offer = await this.pc.createOffer();
            await this.pc.setLocalDescription(offer);

            await this.connection.invoke(
                "SendOffer",
                this.streamId,
                offer
            );

            this.setLiveStatus(true);
        },

        // -----------------------------
        // JOIN STREAM (LISTENER)
        // -----------------------------
        async joinStream(streamId) {
            await this.init();
            this.role = "listener";
            this.streamId = streamId;

            this.createPeerConnection();

            await this.connection.invoke(
                "JoinStream",
                streamId,
                "listener"
            );

            console.log("[StreamService] Joined stream:", streamId);
        },

        // -----------------------------
        // PEER CONNECTION
        // -----------------------------
        createPeerConnection() {
            if (this.pc) return;

            this.pc = new RTCPeerConnection({
                iceServers: [{ urls: "stun:stun.l.google.com:19302" }]
            });

            this.pc.onicecandidate = e => {
                if (e.candidate) {
                    this.connection.invoke(
                        "SendIceCandidate",
                        this.streamId,
                        e.candidate
                    );
                }
            };

            this.pc.ontrack = e => {
                const audio = document.getElementById("audio-player");
                if (audio) {
                    audio.srcObject = e.streams[0];
                    audio.play();
                }
            };
        },

        // -----------------------------
        // HUB HANDLERS
        // -----------------------------
        registerHubHandlers() {
            this.connection.on("ReceiveOffer", async offer => {
                await this.pc.setRemoteDescription(offer);

                const answer = await this.pc.createAnswer();
                await this.pc.setLocalDescription(answer);

                await this.connection.invoke(
                    "SendAnswer",
                    this.streamId,
                    answer
                );
            });

            this.connection.on("ReceiveAnswer", async answer => {
                await this.pc.setRemoteDescription(answer);
            });

            this.connection.on("ReceiveIceCandidate", async candidate => {
                await this.pc.addIceCandidate(candidate);
            });

            this.connection.on("StreamEnded", () => {
                this.stop();
                alert("Stream ended");
            });
        },

        // -----------------------------
        // STOP
        // -----------------------------
        stop() {
            this.pc?.close();
            this.pc = null;

            this.localStream?.getTracks().forEach(t => t.stop());
            this.localStream = null;

            const preview = document.getElementById("stream-local-video");
            if (preview) {
                preview.srcObject = null;
                preview.style.display = "none";
            }

            this.setLiveStatus(false);
            console.log("[StreamService] Stopped");
        },

        setLiveStatus(live) {
            const badge = document.getElementById("stream-status");
            if (!badge) return;

            badge.classList.toggle("bg-success", live);
            badge.classList.toggle("bg-secondary", !live);
            badge.textContent = live ? "LIVE" : "OFFLINE";
        }
    };

    window.StreamService = StreamService;
})();





