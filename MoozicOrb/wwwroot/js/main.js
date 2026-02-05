// =========================================
// GLOBAL VARIABLES
// =========================================
let globeRoot = null; // Track the amCharts instance to dispose of it later

// =========================================
// GLOBAL INIT FUNCTIONS (Called by Router)
// =========================================

window.initGlobe = function () {
    const chartDiv = document.getElementById('chartdiv');

    // 1. Safety Checks
    if (!chartDiv) return;
    if (typeof am5 === 'undefined') return;

    // 2. CRITICAL: Dispose of the previous instance if it exists.
    if (globeRoot) {
        globeRoot.dispose();
        globeRoot = null;
    }

    // 3. Prevent double-initialization
    if (chartDiv.innerHTML !== "") return;

    am5.ready(function () {
        var root = am5.Root.new("chartdiv");
        globeRoot = root;

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

        polygonSeries.mapPolygons.template.setAll({
            fill: am5.color(0x333333),
            stroke: am5.color(0x00AEEF),
            strokeWidth: 0.5,
            interactive: true
        });

        polygonSeries.mapPolygons.template.states.create("hover", {
            fill: am5.color(0x00AEEF),
            stroke: am5.color(0xffffff)
        });

        var backgroundSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {}));
        backgroundSeries.mapPolygons.template.setAll({
            fill: am5.color(0x000000),
            fillOpacity: 0.1,
            strokeOpacity: 0
        });
        backgroundSeries.data.push({
            geometry: am5map.getGeoRectangle(90, 180, -90, -180)
        });

        chart.animate({
            key: "rotationX",
            from: 0,
            to: 360,
            duration: 30000,
            loops: Infinity
        });

        chart.appear(1000, 100);
    });
};

window.initCalendar = function () {
    const daysBox = document.querySelector('.cal-days');
    const monthLabel = document.querySelector('.cal-month-label');
    const prevBtn = document.querySelector('.prev-month');
    const nextBtn = document.querySelector('.next-month');

    if (!daysBox) return;

    let calendarDate = new Date();

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

            d.onclick = () => {
                document.querySelectorAll('.day-cell').forEach(c => c.classList.remove('active'));
                d.classList.add('active');
                updateEvents(year, month, i);
            };

            daysBox.appendChild(d);
        }
    }

    function updateEvents(y, m, d) {
        const dateStr = `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
        const list = document.querySelector('.events-list-container');

        const selWeekday = document.querySelector('.sel-weekday');
        const selFullDate = document.querySelector('.sel-full-date');
        const dateObj = new Date(y, m, d);

        if (selWeekday) selWeekday.textContent = dateObj.toLocaleDateString('en-US', { weekday: 'long' });
        if (selFullDate) selFullDate.textContent = dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        if (!list) return;

        list.innerHTML = '';
        const evts = eventsDB.filter(e => e.date === dateStr);

        if (evts.length === 0) {
            list.innerHTML = '<div style="color:#666">No events scheduled.</div>';
        } else {
            evts.forEach(e => {
                list.innerHTML += `
                    <div class="event-entry">
                        <div class="evt-time">${e.time}</div>
                        <div class="evt-title">${e.title}</div>
                        <div style="font-size:0.8rem; color:#888">${e.loc}</div>
                    </div>`;
            });
        }
    }

    if (prevBtn) {
        prevBtn.onclick = () => {
            calendarDate.setMonth(calendarDate.getMonth() - 1);
            renderCalendar();
        };
    }
    if (nextBtn) {
        nextBtn.onclick = () => {
            calendarDate.setMonth(calendarDate.getMonth() + 1);
            renderCalendar();
        };
    }

    renderCalendar();
    updateEvents(calendarDate.getFullYear(), calendarDate.getMonth(), calendarDate.getDate());
};

// =========================================
// DOM CONTENT LOADED (Runs Once)
// =========================================
document.addEventListener('DOMContentLoaded', () => {

    // 1. SIDEBAR TOGGLE
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

    // 2. LOGIN DROPDOWN LOGIC
    const loginBtn = document.getElementById('loginToggleBtn');
    const loginDropdown = document.getElementById('loginDropdown');
    const signupRadio = document.getElementById('signupRadio');
    const loginRadio = document.getElementById('loginRadio');
    const formInner = document.querySelector('.form-inner');
    const switchToSignupLink = document.getElementById('switchToSignup');

    if (loginBtn && loginDropdown) {
        loginBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            loginDropdown.classList.toggle('active');
        });

        document.addEventListener('click', (e) => {
            if (!loginDropdown.contains(e.target) && e.target !== loginBtn) {
                loginDropdown.classList.remove('active');
            }
        });

        if (signupRadio && loginRadio && formInner) {
            signupRadio.addEventListener('change', () => { formInner.style.marginLeft = "-100%"; });
            loginRadio.addEventListener('change', () => { formInner.style.marginLeft = "0%"; });

            if (switchToSignupLink) {
                switchToSignupLink.addEventListener('click', (e) => {
                    e.preventDefault();
                    signupRadio.checked = true;
                    formInner.style.marginLeft = "-100%";
                });
            }
        }
    }

    // 3. CHAT OVERLAY LOGIC
    const messagesTrigger = document.getElementById('messages-trigger');
    const chatOverlay = document.getElementById('chatOverlay');
    const closeChatWindowBtn = document.getElementById('closeOverlayWindow');
    const closeChatSidebarBtn = document.getElementById('closeOverlaySidebar');
    const chatContainer = document.querySelector('.chat-app-container');
    const chatBackBtn = document.getElementById('chatBackBtn');

    if (messagesTrigger && chatOverlay) {
        messagesTrigger.addEventListener('click', (e) => {
            e.preventDefault();
            chatOverlay.classList.add('active');
            if (sidebar) {
                sidebar.classList.remove('active');
                body.classList.remove('sidebar-open');
            }
        });

        const closeAll = () => {
            chatOverlay.classList.remove('active');
            if (chatContainer) chatContainer.classList.remove('conversation-active');
        };

        if (closeChatWindowBtn) closeChatWindowBtn.addEventListener('click', closeAll);
        if (closeChatSidebarBtn) closeChatSidebarBtn.addEventListener('click', closeAll);

        if (chatBackBtn) {
            chatBackBtn.addEventListener('click', () => {
                if (chatContainer) chatContainer.classList.remove('conversation-active');
            });
        }
    }

    // 4. INITIALIZE VIEW SCRIPTS (First Load)
    window.initGlobe();
    window.initCalendar();
});

// =========================================
// GLOBAL SEARCH FUNCTIONS
// =========================================

// Triggered by "GO" button or Enter Key
window.triggerGlobalSearch = function () {
    const input = document.getElementById('globalSearchInput');
    const term = input.value.trim();

    if (term) {
        // 1. Navigate
        if (window.AppRouter) {
            window.AppRouter.navigate(`/discover/search?q=${encodeURIComponent(term)}`);
        } else {
            window.location.href = `/discover/search?q=${encodeURIComponent(term)}`;
        }

        // 2. FORCE CLOSE SIDEBAR (Mobile Fix)
        const sidebar = document.getElementById('sidebar');
        const toggle = document.getElementById('sidebarToggle');

        if (sidebar) sidebar.classList.remove('active');
        if (toggle) toggle.classList.remove('active'); // Reset hamburger icon
        document.body.classList.remove('sidebar-open');

        // Blur input to hide mobile keyboard
        input.blur();
    }
};

// Bind to Input Keydown
window.handleGlobalSearch = function (e) {
    if (e.key === 'Enter') {
        window.triggerGlobalSearch();
    }
};