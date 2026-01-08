/*const groupName = `stream_${window.AppSession.userId}_${this.streamId}`;*/

(() => {
    const StreamService = {
        connection: null,
        streamId: null,
        listeners: 0, // track listener count locally

        init() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/StreamHub")
                .withAutomaticReconnect()
                .build();

            this.registerHandlers();

            this.connection.start()
                .then(() => console.log("StreamService connected"))
                .catch(err => console.error(err));
        },

        registerHandlers() {
            // Stream started/stopped updates
            this.connection.on("StreamStarted", data => this.onStreamStarted(data));
            this.connection.on("StreamStopped", data => this.onStreamStopped(data));

            // Join confirmation
            this.connection.on("JoinedStream", data => {
                console.log("Joined stream:", data);

                if (data.userId !== 1) { // TEMP: skip broadcaster
                    this.listeners++;
                    this.updateStatusUI();
                }
            });

            // Stream status updates for any listener join/leave
            this.connection.on("StreamStatusUpdate", data => {
                this.listeners = data.listenerCount;
                this.updateStatusUI(data.broadcaster);
            });
        },

        startStream() {
            fetch(`/api/stream/start`, { method: "POST" })
                .then(res => res.json())
                .then(data => {
                    this.streamId = data.streamId;

                    // TEMP dummy user
                    const userId = 1;
                    const groupName = `stream_${userId}_${this.streamId}`;

                    // Disable start button
                    const startBtn = document.querySelector(".btn-success");
                    if (startBtn) startBtn.disabled = true;

                    // Join the group
                    this.connection.invoke("JoinGroup", groupName)
                        .then(() => {
                            console.log(`Joined group: ${groupName}`);
                            this.updateStatusUI(userId); // broadcaster is userId
                        })
                        .catch(err => console.error(err));
                })
                .catch(err => console.error("Failed to start stream:", err));
        },

        stopStream() {
            if (!this.streamId) return;

            fetch(`/api/stream/stop/${this.streamId}`, { method: "POST" })
                .then(() => {
                    this.streamId = null;
                    this.listeners = 0;

                    // Re-enable start button
                    const startBtn = document.querySelector(".btn-success");
                    if (startBtn) startBtn.disabled = false;

                    this.updateStatusUI();
                })
                .catch(err => console.error(err));
        },

        updateStatusUI(broadcasterId = null) {
            const statusEl = document.getElementById("stream-status");
            if (!statusEl) return;

            let text = "OFFLINE";
            if (this.streamId) {
                text = `LIVE`;
                if (broadcasterId) text += ` (user ${broadcasterId})`;
                text += ` - ${this.listeners} listener${this.listeners !== 1 ? 's' : ''}`;
            }

            statusEl.innerText = text;
        },

        onStreamStarted(data) {
            this.streamId = data.streamId;
            this.listeners = 0;

            // Disable start button
            const startBtn = document.querySelector(".btn-success");
            if (startBtn) startBtn.disabled = true;

            this.updateStatusUI(data.startedBy);
        },

        onStreamStopped(data) {
            this.streamId = null;
            this.listeners = 0;

            // Re-enable start button
            const startBtn = document.querySelector(".btn-success");
            if (startBtn) startBtn.disabled = false;

            this.updateStatusUI();
        }
    };

    window.StreamService = StreamService;
})();
