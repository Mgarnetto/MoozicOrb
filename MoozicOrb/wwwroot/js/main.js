document.addEventListener('DOMContentLoaded', () => {

    // --- SIDEBAR TOGGLE ---
    const sidebar = document.getElementById('sidebar');
    const toggleBtn = document.getElementById('sidebarToggle');
    const body = document.body;

    if (toggleBtn) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('active');
            body.classList.toggle('sidebar-open');
        });
    }

    document.addEventListener('click', (e) => {
        if (sidebar && toggleBtn && !sidebar.contains(e.target) && !toggleBtn.contains(e.target) && sidebar.classList.contains('active')) {
            sidebar.classList.remove('active');
            body.classList.remove('sidebar-open');
        }
    });

    // --- CHAT OVERLAY LOGIC ---
    const messagesTrigger = document.getElementById('messages-trigger');
    const chatOverlay = document.getElementById('chatOverlay');
    const closeChatWindowBtn = document.getElementById('closeOverlayWindow');
    const closeChatSidebarBtn = document.getElementById('closeOverlaySidebar'); // NEW BUTTON
    const chatContainer = document.querySelector('.chat-app-container');
    const chatBackBtn = document.getElementById('chatBackBtn');
    const chatList = document.getElementById('chatList');
    const chatMessages = document.getElementById('chatMessages');
    const chatTitle = document.getElementById('chatTitle');
    const chatAvatar = document.getElementById('chatAvatar');

    const chats = [
        { id: 1, name: 'AI Assistant', avatar: 'https://placehold.co/100/00AEEF/fff?text=AI', msgs: [{ text: 'Welcome to MoozicOrb!', in: true }] },
        { id: 2, name: 'Producer Mike', avatar: 'https://placehold.co/100/ff0055/fff?text=PM', msgs: [{ text: 'Yo, send that beat.', in: true }, { text: 'Sent.', in: false }] }
    ];

    if (messagesTrigger && chatOverlay) {
        // Show Overlay
        messagesTrigger.addEventListener('click', (e) => {
            e.preventDefault();
            chatOverlay.classList.add('active');
            renderChatList();
            if (sidebar) sidebar.classList.remove('active');
        });

        // Hide Overlay (Window Button)
        if (closeChatWindowBtn) {
            closeChatWindowBtn.addEventListener('click', () => {
                chatOverlay.classList.remove('active');
                if (chatContainer) chatContainer.classList.remove('conversation-active');
            });
        }

        // Hide Overlay (Sidebar Button) - NEW
        if (closeChatSidebarBtn) {
            closeChatSidebarBtn.addEventListener('click', () => {
                chatOverlay.classList.remove('active');
                if (chatContainer) chatContainer.classList.remove('conversation-active');
            });
        }

        // Mobile Back Button
        if (chatBackBtn) {
            chatBackBtn.addEventListener('click', () => {
                if (chatContainer) chatContainer.classList.remove('conversation-active');
            });
        }
    }

    function renderChatList() {
        if (!chatList) return;
        chatList.innerHTML = chats.map(c => `
            <li class="chat-item" data-id="${c.id}">
                <img src="${c.avatar}" class="avatar-sm">
                <div class="chat-info"><h4>${c.name}</h4><p>Tap to chat...</p></div>
            </li>
        `).join('');

        document.querySelectorAll('.chat-item').forEach(item => {
            item.addEventListener('click', () => loadChat(item.dataset.id));
        });
    }

    function loadChat(id) {
        const chat = chats.find(c => c.id == id);
        if (!chat) return;

        if (chatTitle) chatTitle.textContent = chat.name;
        if (chatAvatar) chatAvatar.src = chat.avatar;
        if (chatMessages) {
            chatMessages.innerHTML = chat.msgs.map(m => `
                <div class="msg ${m.in ? 'in' : 'out'}">${m.text}</div>
            `).join('');
        }

        document.querySelectorAll('.chat-item').forEach(i => i.classList.remove('active'));
        const activeItem = document.querySelector(`.chat-item[data-id="${id}"]`);
        if (activeItem) activeItem.classList.add('active');

        if (chatContainer) chatContainer.classList.add('conversation-active');
    }

    // --- AMCHARTS GLOBE ---
    if (document.getElementById('chartdiv')) {
        am5.ready(function () {
            var root = am5.Root.new("chartdiv");
            root.setThemes([am5themes_Animated.new(root)]);
            var chart = root.container.children.push(am5map.MapChart.new(root, {
                panX: "rotateX", panY: "rotateY", projection: am5map.geoOrthographic(),
                paddingBottom: 20, paddingTop: 20, paddingLeft: 20, paddingRight: 20
            }));
            var polygonSeries = chart.series.push(am5map.MapPolygonSeries.new(root, { geoJSON: am5geodata_worldLow }));

            polygonSeries.mapPolygons.template.setAll({
                tooltipText: "{name}", toggleKey: "active", interactive: true,
                fill: am5.color(0x6794dc), stroke: am5.color(0xffffff), strokeWidth: 0.5
            });
            polygonSeries.mapPolygons.template.states.create("hover", { fill: root.interfaceColors.get("primaryButtonHover") });
            polygonSeries.mapPolygons.template.states.create("active", { fill: root.interfaceColors.get("primaryButtonHover") });

            var backgroundSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {}));
            backgroundSeries.mapPolygons.template.setAll({ fill: root.interfaceColors.get("alternativeBackground"), fillOpacity: 0.1, strokeOpacity: 0 });
            backgroundSeries.data.push({ geometry: am5map.getGeoRectangle(90, 180, -90, -180) });

            var graticuleSeries = chart.series.unshift(am5map.GraticuleSeries.new(root, { step: 10 }));
            graticuleSeries.mapLines.template.set("strokeOpacity", 0.1);

            let previousPolygon;
            polygonSeries.mapPolygons.template.on("active", function (active, target) {
                if (previousPolygon && previousPolygon !== target) previousPolygon.set("active", false);
                if (target.get("active")) {
                    var dataItem = polygonSeries.getDataItemById(target.dataItem.get("id"));
                    var t = dataItem.get("mapPolygon");
                    if (t) {
                        var centroid = t.geoCentroid();
                        if (centroid) {
                            chart.animate({ key: "rotationX", to: -centroid.longitude, duration: 1500, easing: am5.ease.inOut(am5.ease.cubic) });
                            chart.animate({ key: "rotationY", to: -centroid.latitude, duration: 1500, easing: am5.ease.inOut(am5.ease.cubic) });
                        }
                    }
                }
                previousPolygon = target;
            });
            chart.appear(1000, 100);
        });
    }

    // --- CALENDAR LOGIC ---
    const daysBox = document.querySelector('.cal-days');
    const monthLabel = document.querySelector('.cal-month-label');
    const prevBtn = document.querySelector('.prev-month');
    const nextBtn = document.querySelector('.next-month');
    let calendarDate = new Date();

    if (daysBox) {
        const eventsDB = [
            { date: '2026-01-15', time: '8:00 PM', title: 'Studio Session', loc: 'Atlanta, GA' },
            { date: '2026-01-22', time: '10:00 PM', title: 'Live Stream', loc: 'Twitch.tv' }
        ];

        function renderCalendar() {
            const year = calendarDate.getFullYear();
            const month = calendarDate.getMonth();
            if (monthLabel) monthLabel.textContent = calendarDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
            daysBox.innerHTML = '';

            const firstDayIndex = new Date(year, month, 1).getDay();
            const daysInMonth = new Date(year, month + 1, 0).getDate();

            for (let i = 0; i < firstDayIndex; i++) {
                const d = document.createElement('div');
                d.className = 'day-cell dim';
                daysBox.appendChild(d);
            }

            for (let i = 1; i <= daysInMonth; i++) {
                const d = document.createElement('div');
                d.className = 'day-cell';
                d.textContent = i;
                const checkDate = `${year}-${String(month + 1).padStart(2, '0')}-${String(i).padStart(2, '0')}`;
                if (eventsDB.some(e => e.date === checkDate)) {
                    d.classList.add('has-event');
                }
                d.addEventListener('click', () => {
                    document.querySelectorAll('.day-cell').forEach(c => c.classList.remove('active'));
                    d.classList.add('active');
                    updateEvents(year, month, i);
                });
                daysBox.appendChild(d);
            }
        }

        function updateEvents(y, m, d) {
            const dateStr = `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
            const list = document.querySelector('.events-list-container');
            const dateObj = new Date(y, m, d);
            document.querySelector('.sel-weekday').textContent = dateObj.toLocaleDateString('en-US', { weekday: 'long' });
            document.querySelector('.sel-full-date').textContent = dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
            list.innerHTML = '';
            const evts = eventsDB.filter(e => e.date === dateStr);
            if (evts.length === 0) {
                list.innerHTML = '<div style="color:#666">No events scheduled.</div>';
            } else {
                evts.forEach(e => {
                    list.innerHTML += `<div class="event-entry"><div class="evt-time">${e.time}</div><div class="evt-title">${e.title}</div><div style="font-size:0.8rem; color:#888">${e.loc}</div></div>`;
                });
            }
        }

        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                calendarDate.setMonth(calendarDate.getMonth() - 1);
                renderCalendar();
            });
        }
        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                calendarDate.setMonth(calendarDate.getMonth() + 1);
                renderCalendar();
            });
        }
        renderCalendar();
        updateEvents(calendarDate.getFullYear(), calendarDate.getMonth(), calendarDate.getDate());
    }
});