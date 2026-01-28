document.addEventListener('DOMContentLoaded', () => {

    // --- SIDEBAR ---
    const sidebar = document.getElementById('sidebar');
    const toggleBtn = document.getElementById('sidebarToggle');
    const body = document.body;

    toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('active');
        body.classList.toggle('sidebar-open');
    });

    document.addEventListener('click', (e) => {
        if (!sidebar.contains(e.target) && !toggleBtn.contains(e.target) && sidebar.classList.contains('active')) {
            sidebar.classList.remove('active');
            body.classList.remove('sidebar-open');
        }
    });

    // --- AMCHARTS GLOBE (Restored & Fixed) ---
    am5.ready(function () {
        var root = am5.Root.new("chartdiv");
        root.setThemes([am5themes_Animated.new(root)]);

        var chart = root.container.children.push(am5map.MapChart.new(root, {
            panX: "rotateX",
            panY: "rotateY",
            projection: am5map.geoOrthographic(),
            paddingBottom: 20, paddingTop: 20, paddingLeft: 20, paddingRight: 20
        }));

        var polygonSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {
            geoJSON: am5geodata_worldLow
        }));

        // ORIGINAL COLORS
        polygonSeries.mapPolygons.template.setAll({
            tooltipText: "{name}",
            toggleKey: "active",
            interactive: true,
            fill: am5.color(0x6794dc),
            stroke: am5.color(0xffffff),
            strokeWidth: 0.5
        });

        polygonSeries.mapPolygons.template.states.create("hover", {
            fill: root.interfaceColors.get("primaryButtonHover")
        });

        polygonSeries.mapPolygons.template.states.create("active", {
            fill: root.interfaceColors.get("primaryButtonHover")
        });

        var backgroundSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {}));
        backgroundSeries.mapPolygons.template.setAll({
            fill: root.interfaceColors.get("alternativeBackground"),
            fillOpacity: 0.1,
            strokeOpacity: 0
        });
        backgroundSeries.data.push({
            geometry: am5map.getGeoRectangle(90, 180, -90, -180)
        });

        var graticuleSeries = chart.series.unshift(am5map.GraticuleSeries.new(root, { step: 10 }));
        graticuleSeries.mapLines.template.set("strokeOpacity", 0.1);

        let previousPolygon;
        polygonSeries.mapPolygons.template.on("active", function (active, target) {
            if (previousPolygon && previousPolygon !== target) previousPolygon.set("active", false);
            if (target.get("active")) selectCountry(target.dataItem.get("id"));
            previousPolygon = target;
        });

        function selectCountry(id) {
            var dataItem = polygonSeries.getDataItemById(id);
            var target = dataItem.get("mapPolygon");
            if (target) {
                var centroid = target.geoCentroid();
                if (centroid) {
                    chart.animate({ key: "rotationX", to: -centroid.longitude, duration: 1500, easing: am5.ease.inOut(am5.ease.cubic) });
                    chart.animate({ key: "rotationY", to: -centroid.latitude, duration: 1500, easing: am5.ease.inOut(am5.ease.cubic) });
                }
            }
        }
        chart.appear(1000, 100);
    });

    // --- CALENDAR ---
    const daysBox = document.querySelector('.cal-days');
    const monthLabel = document.querySelector('.cal-month-label');
    const selWeekday = document.querySelector('.sel-weekday');
    const selFullDate = document.querySelector('.sel-full-date');
    const eventsList = document.querySelector('.events-list-container');

    let currentDate = new Date();
    const eventsDB = [
        { date: '2026-01-15', time: '8:00 PM', title: 'Studio Session', loc: 'Atlanta, GA' },
        { date: '2026-01-22', time: '10:00 PM', title: 'Live Stream', loc: 'Twitch.tv' },
        { date: '2026-02-05', time: '9:00 PM', title: 'Album Launch', loc: 'New York, NY' }
    ];

    function renderCalendar() {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        monthLabel.textContent = currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
        daysBox.innerHTML = '';

        const firstDay = new Date(year, month, 1).getDay();
        const daysInMonth = new Date(year, month + 1, 0).getDate();

        for (let i = 0; i < firstDay; i++) {
            const d = document.createElement('div');
            d.className = 'day-cell dim';
            daysBox.appendChild(d);
        }

        for (let i = 1; i <= daysInMonth; i++) {
            const d = document.createElement('div');
            d.className = 'day-cell';
            d.textContent = i;

            const checkDate = `${year}-${String(month + 1).padStart(2, '0')}-${String(i).padStart(2, '0')}`;
            if (eventsDB.some(e => e.date === checkDate)) d.classList.add('has-event');

            d.addEventListener('click', () => {
                document.querySelectorAll('.day-cell').forEach(c => c.classList.remove('active'));
                d.classList.add('active');
                updateEventPanel(year, month, i);
            });
            daysBox.appendChild(d);
        }
    }

    function updateEventPanel(year, month, day) {
        const dateObj = new Date(year, month, day);
        const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;

        selWeekday.textContent = dateObj.toLocaleDateString('en-US', { weekday: 'long' });
        selFullDate.textContent = dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        const daysEvents = eventsDB.filter(e => e.date === dateStr);
        eventsList.innerHTML = '';

        if (daysEvents.length === 0) {
            eventsList.innerHTML = '<div style="color:#666">No events.</div>';
        } else {
            daysEvents.forEach(e => {
                eventsList.innerHTML += `
                    <div class="event-entry">
                        <div class="evt-time">${e.time}</div>
                        <div class="evt-title">${e.title}</div>
                        <div style="font-size:0.8rem; color:#888">${e.loc}</div>
                    </div>`;
            });
        }
    }

    document.querySelector('.prev-month').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });
    document.querySelector('.next-month').addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    renderCalendar();
    updateEventPanel(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate());


    // --- AUDIO LOGIC (SignalR + AudioContext) ---
    // Wired to the UI Play Button
    // --------------------------------------------
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
        .withUrl("/hubs/teststream")
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

        // Note: Visualizer connection removed as requested
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
            // START
            try {
                await audioCtx.resume();
                if (connection.state === signalR.HubConnectionState.Disconnected) {
                    await connection.start();
                }
                isPlaying = true;
                isBuffering = true;

                // Visual Update: Switch Icon
                playIcon.classList.remove('fa-play');
                playIcon.classList.add('fa-stop');
                playBtn.classList.add("active");

            } catch (err) {
                console.error(err);
            }
        } else {
            // STOP
            isPlaying = false;

            // Stop Sources
            activeSources.forEach(source => {
                try { source.stop(); } catch (e) { }
            });
            activeSources = [];
            nextStartTime = 0;

            await connection.stop();

            // Visual Update: Switch Back
            playIcon.classList.remove('fa-stop');
            playIcon.classList.add('fa-play');
            playBtn.classList.remove("active");
        }
    };
});