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
        this.ui.playBtn = document.getElementById("playBtn");
        this.ui.playIcon = this.ui.playBtn?.querySelector('i');
        this.ui.timeDisplay = document.querySelector(".scrubber-container .time:first-child");
        this.ui.durationDisplay = document.querySelector(".scrubber-container .time:last-child");
        this.ui.progressBar = document.querySelector(".track-line");
        this.ui.progressFill = document.querySelector(".track-fill");

        if (!this.ui.playBtn) return;

        // Lazy load AudioContext
        this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();

        // SignalR Setup
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/teststream")
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveAudio", (base64Data) => this.handleLiveAudio(base64Data));

        // Track Listeners
        this.trackAudio.addEventListener('timeupdate', () => this.updateScrubber());
        this.trackAudio.addEventListener('ended', () => this.stopTrack());

        // Controls
        this.ui.playBtn.onclick = () => this.togglePlay();
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
            // PAUSE
            if (this.mode === 'LIVE') await this.stopLive();
            else if (this.mode === 'TRACK') this.pauseTrack();
        } else {
            // RESUME
            if (this.mode === 'TRACK') this.resumeTrack();
            else await this.startLive(); // Default to Live
        }
    },

    async playTrack(url, meta) {
        // 1. Stop Live Stream if active
        if (this.mode === 'LIVE') await this.stopLive();

        // 2. Play Track
        this.mode = 'TRACK';
        this.currentTrackMeta = meta;
        this.trackAudio.src = url;
        this.trackAudio.play();
        this.isPlaying = true;

        this.updateUIState(true);
        if (this.ui.durationDisplay) this.ui.durationDisplay.innerText = meta.duration || "--:--";
    },

    // =========================================
    // LIVE STREAM LOGIC
    // =========================================

    async startLive() {
        try {
            // 1. Stop Track if active
            if (this.mode === 'TRACK') {
                this.stopTrack();
            }

            this.mode = 'LIVE';
            await this.audioCtx.resume();

            if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                await this.connection.start();
            }

            this.isPlaying = true;
            this.isBuffering = true;
            this.updateUIState(true);

            // UI Reset
            if (this.ui.progressFill) this.ui.progressFill.style.width = '100%';
            if (this.ui.timeDisplay) this.ui.timeDisplay.innerText = "LIVE";
            if (this.ui.durationDisplay) this.ui.durationDisplay.innerText = "ON AIR";

        } catch (err) {
            console.error("Live Start Error:", err);
        }
    },

    async stopLive() {
        this.isPlaying = false;
        this.activeSources.forEach(s => { try { s.stop(); } catch (e) { } });
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
    // TRACK LOGIC
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
    },

    updateScrubber() {
        if (this.mode !== 'TRACK') return;
        const pct = (this.trackAudio.currentTime / this.trackAudio.duration) * 100;
        if (this.ui.progressFill) this.ui.progressFill.style.width = `${pct}%`;
        if (this.ui.timeDisplay) this.ui.timeDisplay.innerText = this.formatTime(this.trackAudio.currentTime);
    },

    seek(e) {
        if (this.mode !== 'TRACK' || !this.trackAudio.duration) return;
        const rect = this.ui.progressBar.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const width = rect.width;
        this.trackAudio.currentTime = (clickX / width) * this.trackAudio.duration;
    },

    formatTime(seconds) {
        const min = Math.floor(seconds / 60);
        const sec = Math.floor(seconds % 60);
        return `${min}:${sec < 10 ? '0' : ''}${sec}`;
    },

    updateUIState(playing) {
        if (playing) {
            this.ui.playIcon.classList.remove('fa-play');
            this.ui.playIcon.classList.add('fa-stop');
            this.ui.playBtn.style.boxShadow = "0 0 30px #00ff88";
        } else {
            this.ui.playIcon.classList.remove('fa-stop');
            this.ui.playIcon.classList.remove('fa-pause');
            this.ui.playIcon.classList.add('fa-play');
            this.ui.playBtn.style.boxShadow = "";
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    AudioPlayer.init();
    window.AudioPlayer = AudioPlayer;
});

/* =========================================
DISCOGRAPHY UI HELPERS
(Required because ViewComponents injected via SPA cannot run inline scripts)
========================================= */

window.toggleCollection = function (header) {
    // 1. Find the parent box
    const box = header.closest('.collection-box');
    if (box) {
        // 2. Toggle the expanded class
        box.classList.toggle('expanded');
    }
};

window.playCollection = function (event, collectionId) {
    // 1. Stop the click from bubbling up (preventing the toggle)
    event.stopPropagation();

    // 2. Find the container
    const box = event.target.closest('.collection-box');
    if (!box) return;

    // 3. Find the first track in this collection
    const firstTrack = box.querySelector('.track-row');

    if (firstTrack) {
        // 4. Simulate a click on the first track to start playing
        // (This triggers the existing onclick logic on the track row)
        firstTrack.click();
    }
};