class SpaRouter {
    constructor() {
        this.rootElement = document.getElementById("mainContent");
        this.currentGroup = null;

        // 1. Back/Forward Buttons
        window.addEventListener("popstate", () => {
            this.loadHtml(window.location.pathname);
        });

        // 2. Click Interceptor
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
                    "X-Session-Id": window.AuthState?.sessionId
                }
            });

            if (res.ok) {
                const html = await res.text();

                this.rootElement.innerHTML = html;
                this.rootElement.style.opacity = "1";
                this.rootElement.style.pointerEvents = "auto";
                window.scrollTo(0, 0);

                // === THE FIX: Connect Router to SignalR Bridge ===
                const contextEl = document.getElementById("page-signalr-context");
                if (contextEl && window.MessageService) {
                    const newGroup = contextEl.value;

                    // Switch rooms if needed
                    if (this.currentGroup !== newGroup) {
                        if (this.currentGroup) window.MessageService.leaveGroup(this.currentGroup);
                        window.MessageService.joinGroup(newGroup);
                        this.currentGroup = newGroup;
                    }
                }

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
        if (document.getElementById("chartdiv") && window.initGlobe) {
            window.initGlobe();
        }
        if (document.querySelector(".calendar-box") && window.initCalendar) {
            window.initCalendar();
        }
    }
}

// Start Engine
window.AppRouter = new SpaRouter();