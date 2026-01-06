// streamservice.js
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
            this.connection.on("StreamStarted", data => {
                console.log("Stream started:", data);
                this.onStreamStarted(data);
            });

            this.connection.on("StreamStopped", data => {
                console.log("Stream stopped:", data);
                this.onStreamStopped(data);
            });
        },

        joinStream() {
            this.connection.invoke("JoinStream", this.streamId)
                .catch(err => console.error(err));
        },

        startStream() {
            fetch(`/api/stream/start/${this.streamId}`, {
                method: "POST"
            });
        },

        stopStream() {
            fetch(`/api/stream/stop/${this.streamId}`, {
                method: "POST"
            });
        },

        // ---- UI hooks (safe to override later) ----
        onStreamStarted(data) {
            document.getElementById("stream-status").innerText =
                `LIVE (started by user ${data.startedBy})`;
        },

        onStreamStopped(data) {
            document.getElementById("stream-status").innerText =
                "OFFLINE";
        }
    };

    // expose ONE safe symbol
    window.StreamService = StreamService;

})();
