document.addEventListener('DOMContentLoaded', () => {
    // --- AUDIO LOGIC (SignalR + AudioContext) ---
    const audioCtx = new (window.AudioContext || window.webkitAudioContext)();

    // Config
    const SAMPLE_RATE = 44100;
    const CHANNELS = 2;
    const BUFFER_DELAY = 0.5;

    let nextStartTime = 0;
    let isPlaying = false;
    let isBuffering = true;
    let activeSources = [];

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/teststream") // Ensure this matches your endpoint
        .withAutomaticReconnect()
        .build();

    function decodePcm(buffer) {
        const numSamples = buffer.length / 2 / CHANNELS;
        const leftChannel = new Float32Array(numSamples);
        const rightChannel = new Float32Array(numSamples);
        let dataView = new DataView(buffer.buffer);

        for (let i = 0; i < numSamples; i++) {
            let offset = i * 2 * CHANNELS;
            let s1 = dataView.getInt16(offset, true);
            leftChannel[i] = s1 / 32768;
            let s2 = dataView.getInt16(offset + 2, true);
            rightChannel[i] = s2 / 32768;
        }
        return [leftChannel, rightChannel];
    }

    connection.on("ReceiveAudio", (base64Data) => {
        if (!isPlaying) return;

        const binaryString = window.atob(base64Data);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) { bytes[i] = binaryString.charCodeAt(i); }

        const [leftData, rightData] = decodePcm(bytes);

        const audioBuffer = audioCtx.createBuffer(CHANNELS, leftData.length, SAMPLE_RATE);
        audioBuffer.copyToChannel(leftData, 0);
        audioBuffer.copyToChannel(rightData, 1);

        const source = audioCtx.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(audioCtx.destination);
        activeSources.push(source);

        source.onended = () => {
            activeSources = activeSources.filter(s => s !== source);
        };

        const currentTime = audioCtx.currentTime;
        if (isBuffering) {
            nextStartTime = currentTime + BUFFER_DELAY;
            isBuffering = false;
        } else if (nextStartTime < currentTime) {
            nextStartTime = currentTime + 0.05;
        }

        source.start(nextStartTime);
        nextStartTime += audioBuffer.duration;
    });

    // Wire up to the design's Play Button
    const playBtn = document.getElementById("playBtn");
    const playIcon = playBtn.querySelector('i');

    playBtn.onclick = async () => {
        if (!isPlaying) {
            try {
                await audioCtx.resume();
                if (connection.state === signalR.HubConnectionState.Disconnected) {
                    await connection.start();
                }
                isPlaying = true;
                isBuffering = true;

                // UI update
                playIcon.classList.remove('fa-play');
                playIcon.classList.add('fa-stop');
                playBtn.style.boxShadow = "0 0 30px #00ff88"; // Visual cue active

            } catch (err) {
                console.error(err);
            }
        } else {
            isPlaying = false;
            activeSources.forEach(source => { try { source.stop(); } catch (e) { } });
            activeSources = [];
            nextStartTime = 0;
            await connection.stop();

            // UI update
            playIcon.classList.remove('fa-stop');
            playIcon.classList.add('fa-play');
            playBtn.style.boxShadow = "";
        }
    };
});