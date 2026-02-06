class SpaRouter {
    constructor() {
        this.rootElement = document.getElementById("mainContent");
        this.currentGroup = null;

        window.addEventListener("popstate", () => {
            this.loadHtml(window.location.pathname);
        });

        document.body.addEventListener("click", (e) => {
            const link = e.target.closest("a");
            if (link &&
                link.href.startsWith(window.location.origin) &&
                !link.getAttribute("target") &&
                !link.href.includes("#") &&
                !link.classList.contains("no-spa")
            ) {
                e.preventDefault();
                this.navigate(link.getAttribute("href"));
            }
        });
    }

    navigate(url) {
        window.history.pushState(null, null, url);
        this.loadHtml(url);
    }

    async loadHtml(url) {
        this.rootElement.style.opacity = "0.5";
        this.rootElement.style.pointerEvents = "none";

        try {
            const res = await fetch(url, {
                headers: {
                    "X-Spa-Request": "true",
                    "X-Session-Id": window.AuthState?.sessionId || ""
                }
            });

            if (res.ok) {
                const html = await res.text();

                this.rootElement.innerHTML = html;
                this.rootElement.style.opacity = "1";
                this.rootElement.style.pointerEvents = "auto";
                window.scrollTo(0, 0);

                // 1. Close Sidebar on Navigation
                const sidebar = document.getElementById('sidebar');
                if (sidebar && sidebar.classList.contains('active')) {
                    sidebar.classList.remove('active');
                    document.body.classList.remove('sidebar-open');
                }

                // 2. Handle SignalR Context (FeedService)
                const contextEl = document.getElementById("page-signalr-context");
                if (contextEl && window.FeedService) {
                    const newGroup = contextEl.value;
                    if (this.currentGroup !== newGroup) {
                        if (this.currentGroup) window.FeedService.leaveGroup(this.currentGroup);
                        window.FeedService.joinGroup(newGroup);
                        this.currentGroup = newGroup;
                    }
                }

                // 3. Re-initialize page-specific scripts
                this.reinitScripts();

            } else {
                this.rootElement.innerHTML = "<div class='section'><h3>Error loading content.</h3></div>";
                this.rootElement.style.opacity = "1";
            }
        } catch (err) {
            console.error("Router Error:", err);
            this.rootElement.style.opacity = "1";
        }
    }

    reinitScripts() {
        // A. Globe
        if (document.getElementById("chartdiv") && window.initGlobe) {
            window.initGlobe();
        }

        // B. Calendar
        if (document.querySelector(".calendar-box") && window.initCalendar) {
            window.initCalendar();
        }

        // C. Settings Pages
        if (window.location.pathname.includes("/settings") && window.initSettings) {
            window.initSettings();
        }

        // D. Social Feed Loader (Standard Cards)
        const feedContainer = document.getElementById("feed-stream-container");
        const contextEl = document.getElementById("page-signalr-context");

        if (feedContainer && window.loadFeedHistory && contextEl) {
            const groupValue = contextEl.value; // e.g., "feed_global" or "user_105"

            if (groupValue === 'feed_global') {
                window.loadFeedHistory('global', '0');
            }
            else if (groupValue.startsWith('user_')) {
                const userId = groupValue.split('_')[1];
                window.loadFeedHistory('user', userId);
            }
        }

        // E. Audio Discovery Loader (Playlist) - NEW
        // Detects if the audio playlist container is present
        const audioContainer = document.getElementById("audio-feed-list");
        if (audioContainer && window.loadAudioPlaylist) {
            window.loadAudioPlaylist();
        }
    }
}

// Start Engine
window.AppRouter = new SpaRouter();