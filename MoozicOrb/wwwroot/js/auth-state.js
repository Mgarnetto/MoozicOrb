(() => {
    const AuthState = {
        loggedIn: false,
        userId: null,
        sessionId: null,

        // ============================================
        // 1. LOGIN STATE & UI TOGGLING
        // ============================================
        setLoggedIn(userId, sessionId) {
            this.loggedIn = true;
            this.userId = userId;
            this.sessionId = sessionId;

            document.body.classList.add("auth-on");
            document.body.classList.remove("auth-off");

            const guestNav = document.getElementById("auth-guest");
            const userNav = document.getElementById("auth-user");

            if (guestNav) guestNav.style.display = "none";
            if (userNav) {
                userNav.style.display = "flex";
                // Hide the "Welcome User" text, keep the logout button
                const welcomeText = userNav.querySelector(".welcome-text");
                if (welcomeText) welcomeText.style.display = "none";
            }

            const dropdown = document.getElementById("loginDropdown");
            if (dropdown) {
                dropdown.style.display = "none";
                dropdown.classList.remove("active");
            }
        },

        setLoggedOut() {
            this.loggedIn = false;
            this.userId = null;
            this.sessionId = null;

            document.body.classList.add("auth-off");
            document.body.classList.remove("auth-on");

            const guestNav = document.getElementById("auth-guest");
            const userNav = document.getElementById("auth-user");

            if (guestNav) guestNav.style.display = "block";
            if (userNav) userNav.style.display = "none";

            // Clear sidebar on logout
            const chatList = document.getElementById("chatList");
            if (chatList) chatList.innerHTML = '';
        },

        // ============================================
        // 2. BOOTSTRAP (The Fix is Here)
        // ============================================
        async bootstrap() {
            if (!this.sessionId) throw new Error("Missing session");

            // 1. Start SignalR connection FIRST (non-blocking if possible)
            if (window.MessageService) {
                // We await this ensuring we are connected before trying to join groups later
                try { await window.MessageService.start(); }
                catch (e) { console.error("SignalR start failed", e); }
            }

            let data;
            try {
                const res = await fetch("/api/login/bootstrap", {
                    headers: { "X-Session-Id": this.sessionId }
                });
                if (!res.ok) throw new Error("Bootstrap failed");
                data = await res.json();
            } catch (err) {
                console.error("[AuthState] Bootstrap error:", err);
                throw err;
            }

            // 2. Clear List
            const chatList = document.getElementById("chatList");
            if (chatList) chatList.innerHTML = "";

            // 3. RENDER ALL THREADS INSTANTLY (Don't wait for network!)
            const groupsToJoin = [];

            if (Array.isArray(data.groups)) {
                for (const groupId of data.groups) {
                    this.ensureThread({
                        id: groupId,
                        name: `Group ${groupId}`,
                        type: "group",
                        img: "/images/default-group.png"
                    });
                    groupsToJoin.push(groupId);
                }
            }

            if (Array.isArray(data.directUsers)) {
                for (const userId of data.directUsers) {
                    this.ensureThread({
                        id: userId,
                        name: `User ${userId}`,
                        type: "direct",
                        img: "/images/default-user.png"
                    });
                }
            }

            // 4. JOIN SIGNALR GROUPS IN BACKGROUND
            // We do this AFTER the UI is drawn so the user sees their list immediately.
            if (groupsToJoin.length > 0 && window.MessageService) {
                // No 'await' here lets the UI finish loading while this happens in background
                window.MessageService.joinGroups(groupsToJoin)
                    .catch(err => console.error("Failed to join background groups:", err));
            }
        },

        // ============================================
        // 3. UI GENERATOR
        // ============================================
        ensureThread({ id, name, type, img }) {
            // Check for duplicates
            const domId = `thread-${type}-${id}`;
            if (document.getElementById(domId)) return;

            // Defaults
            if (!img) img = type === "group" ? "/images/default-group.png" : "/images/default-user.png";

            this.renderThreadItem(domId, { id, name, type, img });
        },

        renderThreadItem(domId, { id, name, type, img }) {
            const chatList = document.getElementById("chatList");
            if (!chatList) return;

            const li = document.createElement("li");
            li.id = domId;
            li.className = "chat-contact-item";

            li.style.cssText = `
                display: flex;
                align-items: center;
                padding: 15px;
                cursor: pointer;
                border-bottom: 1px solid rgba(255,255,255,0.1);
                transition: background 0.2s;
            `;

            li.onmouseover = () => li.style.background = "rgba(255,255,255,0.05)";
            li.onmouseout = () => li.style.background = "transparent";

            const iconClass = type === "group" ? "fa-users" : "fa-user";
            const avatarColor = type === "group" ? "#6c5ce7" : "#0984e3";

            li.innerHTML = `
                <div class="avatar" style="
                    width: 40px; height: 40px; background: ${avatarColor}; 
                    border-radius: 50%; display: flex; align-items: center; 
                    justify-content: center; margin-right: 15px; color: white;">
                    <i class="fas ${iconClass}"></i>
                </div>
                <div class="info" style="flex: 1;">
                    <h4 style="margin: 0; font-size: 14px; font-weight: 600; color: #fff;">${name}</h4>
                    <p style="margin: 0; font-size: 12px; color: #aaa;">Click to chat</p>
                </div>
            `;

            li.addEventListener("click", () => {
                // Visual Highlight
                Array.from(chatList.children).forEach(c => c.style.background = "transparent");
                li.style.background = "rgba(255,255,255,0.1)";

                // Trigger Load
                if (type === "group") {
                    if (window.loadGroupMessages) window.loadGroupMessages(id, name);
                } else {
                    if (window.loadDirectMessages) window.loadDirectMessages(id, name);
                }
            });

            chatList.appendChild(li);
        }
    };

    // ============================================
    // 4. INIT & RESTORE SESSION
    // ============================================
    window.AuthState = AuthState;

    document.addEventListener("DOMContentLoaded", async () => {
        const savedSession = localStorage.getItem("moozic_session");

        if (savedSession) {
            try {
                const data = JSON.parse(savedSession);
                AuthState.setLoggedIn(data.userId, data.sessionId);
                await AuthState.bootstrap();
            } catch (e) {
                console.error("Failed to restore session", e);
                localStorage.removeItem("moozic_session");
                AuthState.setLoggedOut();
            }
        } else {
            AuthState.setLoggedOut();
        }

        // Toggle button logic
        const toggleBtn = document.getElementById("loginToggleBtn");
        const dropdown = document.getElementById("loginDropdown");
        if (toggleBtn && dropdown) {
            toggleBtn.addEventListener("click", () => {
                const isHidden = dropdown.style.display === "none" || dropdown.style.display === "";
                dropdown.style.display = isHidden ? "block" : "none";
            });
        }
    });
})();