// StreamService.js
(() => {
    const StreamService = {
        connection: null,
        streamId: null,

        init(streamId) {
            this.streamId = streamId;
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/StreamHub")
                .withAutomaticReconnect()
                .build();

            this.registerHandlers();

            this.connection.start()
                .then(() => {
                    console.log("StreamService connected");
                    this.joinStream();
                })
                .catch(err => console.error(err));
        },

        registerHandlers() {
            this.connection.on("StreamStarted", data => this.onStreamStarted(data));
            this.connection.on("StreamStopped", data => this.onStreamStopped(data));

            this.connection.on("JoinedStream", data => {
                console.log("Joined stream:", data);
            });

            // NEW: update HTML when listener joins/leaves
            this.connection.on("StreamStatusUpdate", data => {
                const statusEl = document.getElementById("stream-status");
                if (!statusEl) return;

                let text = "OFFLINE";
                if (data.broadcaster) text = `LIVE (user ${data.broadcaster})`;

                text += ` - ${data.listenerCount} listener${data.listenerCount !== 1 ? 's' : ''}`;
                statusEl.innerText = text;
            });
        },

        joinStream() {
            if (!this.streamId) return;
            this.connection.invoke("JoinStream", this.streamId)
                .catch(err => console.error(err));
        },

        startStream() {
            if (!this.streamId) return;
            fetch(`/api/stream/start/${this.streamId}`, { method: "POST" });
        },

        stopStream() {
            if (!this.streamId) return;
            fetch(`/api/stream/stop/${this.streamId}`, { method: "POST" });
        },

        onStreamStarted(data) {
            const status = document.getElementById("stream-status");
            if (status) status.innerText = `LIVE (started by user ${data.startedBy})`;
        },

        onStreamStopped(data) {
            const status = document.getElementById("stream-status");
            if (status) status.innerText = "OFFLINE";
        }
    };

    window.StreamService = StreamService;
})();

