(() => {
    const StreamService = {
        connection: null,
        pc: null,
        streamId: null,
        role: null,

        init(streamId) {
            this.streamId = streamId;

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/StreamHub")
                .withAutomaticReconnect()
                .build();

            this.registerHubHandlers();
            this.connection.start();
        },

        async startBroadcast() {
            this.role = "broadcaster";
            this.createPeerConnection();

            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream.getTracks().forEach(t => this.pc.addTrack(t, stream));

            await this.connection.invoke("JoinStream", this.streamId, "broadcaster");

            const offer = await this.pc.createOffer();
            await this.pc.setLocalDescription(offer);
            await this.connection.invoke("SendOffer", this.streamId, offer);
        },

        async joinStream() {
            this.role = "listener";
            this.createPeerConnection();

            await this.connection.invoke("JoinStream", this.streamId, "listener");
        },

        createPeerConnection() {
            this.pc = new RTCPeerConnection();

            this.pc.onicecandidate = e => {
                if (e.candidate) {
                    this.connection.invoke("SendIceCandidate", this.streamId, e.candidate);
                }
            };

            this.pc.ontrack = e => {
                const audio = document.getElementById("audio-player");
                audio.srcObject = e.streams[0];
            };
        },

        registerHubHandlers() {
            this.connection.on("ReceiveOffer", async offer => {
                await this.pc.setRemoteDescription(offer);
                const answer = await this.pc.createAnswer();
                await this.pc.setLocalDescription(answer);
                await this.connection.invoke("SendAnswer", this.streamId, answer);
            });

            this.connection.on("ReceiveAnswer", async answer => {
                await this.pc.setRemoteDescription(answer);
            });

            this.connection.on("ReceiveIceCandidate", c => {
                this.pc.addIceCandidate(c);
            });
        }
    };

    window.StreamService = StreamService;
})();
