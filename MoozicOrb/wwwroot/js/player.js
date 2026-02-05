/* =========================================
   AUDIO PLAYER MANAGER
   Handles switching between:
   1. Live Station (SignalR + Web Audio API)
   2. On-Demand Tracks (HTML5 Audio Element)
   ========================================= */

const AudioPlayer = {
    // --- STATE ---
    mode: 'IDLE', // 'IDLE', 'LIVE', 'TRACK'
    isPlaying: false,

    // --- LIVE CONFIG ---
    audioCtx: null,
    connection: null,
    nextStartTime: 0,
    isBuffering: true,
    activeSources: [],
    SAMPLE_RATE: 44100,
    CHANNELS: 2,
    BUFFER_DELAY: 0.5,

    // --- TRACK CONFIG ---
    trackAudio: new Audio(), // HTML5 Audio Element
    currentTrackMeta: null,

    // --- UI ELEMENTS ---
    ui: {
        playBtn: null,
        playIcon: null,
        timeDisplay: null,
        durationDisplay: null,
        progressBar: null,
        progressFill: null
    },

    init() {
        // 1. DOM Elements
        this.ui.playBtn = document.getElementById("playBtn");
        this.ui.playIcon = this.ui.playBtn?.querySelector('i');
        this.ui.timeDisplay = document.querySelector(".scrubber-container .time:first-child");
        this.ui.durationDisplay = document.querySelector(".scrubber-container .time:last-child");
        this.ui.progressBar = document.querySelector(".track-line");
        this.ui.progressFill = document.querySelector(".track-fill");

        if (!this.ui.playBtn) return; // Guard clause if player bar missing

        // 2. Audio Context (Lazy Load)
        this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();

        // 3. SignalR Setup
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/teststream")
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveAudio", (base64Data) => this.handleLiveAudio(base64Data));

        // 4. Track Listeners
        this.trackAudio.addEventListener('timeupdate', () => this.updateScrubber());
        this.trackAudio.addEventListener('ended', () => this.stopTrack());

        // 5. Main Button Click
        this.ui.playBtn.onclick = () => this.togglePlay();

        // 6. Scrubber Click
        if (this.ui.progressBar) {
            this.ui.progressBar.addEventListener('click', (e) => this.seek(e));
        }

        console.log("Audio Player Initialized");
    },

    // =========================================
    // CONTROL LOGIC
    // =========================================

    async togglePlay() {
        if (this.isPlaying) {
            // PAUSE / STOP
            if (this.mode === 'LIVE') await this.stopLive();
            else if (this.mode === 'TRACK') this.pauseTrack();
        } else {
            // PLAY / RESUME
            if (this.mode === 'TRACK') this.resumeTrack();
            else await this.startLive(); // Default to Live if IDLE
        }
    },

    // Public API called by Post Cards
    async playTrack(url, meta) {
        // 1. Stop whatever is currently playing
        if (this.mode === 'LIVE') await this.stopLive();

        // 2. Set Mode
        this.mode = 'TRACK';
        this.currentTrackMeta = meta;

        // 3. Load & Play
        this.trackAudio.src = url;
        this.trackAudio.play();
        this.isPlaying = true;

        // 4. Update UI
        this.updateUIState(true);
        if (this.ui.durationDisplay) this.ui.durationDisplay.innerText = meta.duration || "--:--";

        // Optional: Update a title in the player bar if you have an element for it
        // document.getElementById("player-track-title").innerText = meta.title;
    },

    // =========================================
    // LIVE STREAM LOGIC (Existing SignalR)
    // =========================================

    async startLive() {
        try {
            this.mode = 'LIVE';
            await this.audioCtx.resume();

            if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                await this.connection.start();
            }

            this.isPlaying = true;
            this.isBuffering = true;
            this.updateUIState(true);

            // Reset scrubber for live
            if (this.ui.progressFill) this.ui.progressFill.style.width = '100%';
            if (this.ui.timeDisplay) this.ui.timeDisplay.innerText = "LIVE";

        } catch (err) {
            console.error("Live Start Error:", err);
        }
    },

    async stopLive() {
        this.isPlaying = false;
        this.activeSources.forEach(source => { try { source.stop(); } catch (e) { } });
        this.activeSources = [];
        this.nextStartTime = 0;

        if (this.connection.state === signalR.HubConnectionState.Connected) {
            await this.connection.stop();
        }

        this.updateUIState(false);
        this.mode = 'IDLE';
    },

    handleLiveAudio(base64Data) {
        if (!this.isPlaying || this.mode !== 'LIVE') return;

        const binaryString = window.atob(base64Data);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) { bytes[i] = binaryString.charCodeAt(i); }

        const [leftData, rightData] = this.decodePcm(bytes);

        const audioBuffer = this.audioCtx.createBuffer(this.CHANNELS, leftData.length, this.SAMPLE_RATE);
        audioBuffer.copyToChannel(leftData, 0);
        audioBuffer.copyToChannel(rightData, 1);

        const source = this.audioCtx.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(this.audioCtx.destination);
        this.activeSources.push(source);

        source.onended = () => {
            this.activeSources = this.activeSources.filter(s => s !== source);
        };

        const currentTime = this.audioCtx.currentTime;
        if (this.isBuffering) {
            this.nextStartTime = currentTime + this.BUFFER_DELAY;
            this.isBuffering = false;
        } else if (this.nextStartTime < currentTime) {
            this.nextStartTime = currentTime + 0.05;
        }

        source.start(this.nextStartTime);
        this.nextStartTime += audioBuffer.duration;
    },

    decodePcm(buffer) {
        const numSamples = buffer.length / 2 / this.CHANNELS;
        const leftChannel = new Float32Array(numSamples);
        const rightChannel = new Float32Array(numSamples);
        let dataView = new DataView(buffer.buffer);

        for (let i = 0; i < numSamples; i++) {
            let offset = i * 2 * this.CHANNELS;
            leftChannel[i] = dataView.getInt16(offset, true) / 32768;
            rightChannel[i] = dataView.getInt16(offset + 2, true) / 32768;
        }
        return [leftChannel, rightChannel];
    },

    // =========================================
    // TRACK LOGIC (HTML5 Audio)
    // =========================================

    pauseTrack() {
        this.trackAudio.pause();
        this.isPlaying = false;
        this.updateUIState(false);
    },

    resumeTrack() {
        this.trackAudio.play();
        this.isPlaying = true;
        this.updateUIState(true);
    },

    stopTrack() {
        this.trackAudio.pause();
        this.trackAudio.currentTime = 0;
        this.isPlaying = false;
        this.updateUIState(false);
        // Don't reset mode to IDLE, allows Replay
    },

    updateScrubber() {
        if (this.mode !== 'TRACK') return;

        const pct = (this.trackAudio.currentTime / this.trackAudio.duration) * 100;
        if (this.ui.progressFill) this.ui.progressFill.style.width = `${pct}%`;

        if (this.ui.timeDisplay) {
            this.ui.timeDisplay.innerText = this.formatTime(this.trackAudio.currentTime);
        }
    },

    seek(e) {
        if (this.mode !== 'TRACK' || !this.trackAudio.duration) return;

        const rect = this.ui.progressBar.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const width = rect.width;
        const seekTime = (clickX / width) * this.trackAudio.duration;

        this.trackAudio.currentTime = seekTime;
    },

    formatTime(seconds) {
        const min = Math.floor(seconds / 60);
        const sec = Math.floor(seconds % 60);
        return `${min}:${sec < 10 ? '0' : ''}${sec}`;
    },

    // =========================================
    // UI UPDATES
    // =========================================

    updateUIState(playing) {
        if (playing) {
            this.ui.playIcon.classList.remove('fa-play');
            this.ui.playIcon.classList.add('fa-stop'); // Or fa-pause for tracks
            this.ui.playBtn.style.boxShadow = "0 0 30px #00ff88"; // Glow
        } else {
            this.ui.playIcon.classList.remove('fa-stop');
            this.ui.playIcon.classList.remove('fa-pause');
            this.ui.playIcon.classList.add('fa-play');
            this.ui.playBtn.style.boxShadow = "";
        }
    }
};

// Initialize on Load
document.addEventListener('DOMContentLoaded', () => {
    AudioPlayer.init();
    // Expose globally
    window.AudioPlayer = AudioPlayer;
});