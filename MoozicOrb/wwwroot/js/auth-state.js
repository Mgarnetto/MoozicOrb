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

            const chatList = document.getElementById("chatList");
            if (chatList) chatList.innerHTML = '';
        },

        // ============================================
        // 2. BOOTSTRAP (THE FIX)
        // ============================================
        async bootstrap() {
            if (!this.sessionId) throw new Error("Missing session");

            // 1. Start SignalR
            if (window.MessageService) {
                try { await window.MessageService.start(); }
                catch (e) { console.error("SignalR start failed", e); }
            }

            // 2. Clear List
            const chatList = document.getElementById("chatList");
            if (chatList) chatList.innerHTML = "";

            const groupsToJoin = [];

            try {
                // --- STEP A: Fetch Groups (from Login Controller) ---
                const resBoot = await fetch("/api/login/bootstrap", {
                    headers: { "X-Session-Id": this.sessionId }
                });
                if (resBoot.ok) {
                    const dataBoot = await resBoot.json();

                    if (Array.isArray(dataBoot.groups)) {
                        for (const groupId of dataBoot.groups) {
                            this.ensureThread({
                                id: groupId,
                                name: `Group ${groupId}`,
                                type: "group",
                                img: "/images/default-group.png"
                            });
                            groupsToJoin.push(groupId);
                        }
                    }
                }

                // --- STEP B: Fetch DMs (from DirectMessages Controller) ---
                // This guarantees we get the full message objects with names
                const resDms = await fetch("/api/direct/messages", {
                    headers: { "X-Session-Id": this.sessionId }
                });

                if (resDms.ok) {
                    const dataDms = await resDms.json();

                    // Handle Casing (messages vs Messages)
                    const conversations = dataDms.messages || dataDms.Messages || {};

                    for (const otherUserId in conversations) {
                        const msgs = conversations[otherUserId];
                        let displayName = `User ${otherUserId}`;
                        let displayPic = "/images/default-user.png";

                        // EXTRACT NAME LOGIC
                        if (msgs && msgs.length > 0) {
                            // Grab the most recent message
                            const sample = msgs[msgs.length - 1];

                            // Robust Casing Check
                            const sName = sample.senderName || sample.SenderName;
                            const rName = sample.receiverName || sample.ReceiverName;
                            const sPic = sample.senderProfilePicUrl || sample.SenderProfilePicUrl;
                            const rPic = sample.receiverProfilePicUrl || sample.ReceiverProfilePicUrl;

                            if (sample.senderId == this.userId) {
                                // I sent it -> Show Receiver Name
                                if (rName) displayName = rName;
                                if (rPic) displayPic = rPic;
                            } else {
                                // They sent it -> Show Sender Name
                                if (sName) displayName = sName;
                                if (sPic) displayPic = sPic;
                            }
                        }

                        this.ensureThread({
                            id: parseInt(otherUserId),
                            name: displayName,
                            type: "direct",
                            img: displayPic
                        });
                    }
                }

            } catch (err) {
                console.error("[AuthState] Bootstrap error:", err);
            }

            // 3. Join SignalR Groups
            if (groupsToJoin.length > 0 && window.MessageService) {
                window.MessageService.joinGroups(groupsToJoin)
                    .catch(err => console.error("Failed to join background groups:", err));
            }
        },

        // ============================================
        // 3. UI GENERATOR
        // ============================================
        ensureThread({ id, name, type, img }) {
            const domId = `thread-${type}-${id}`;
            const existing = document.getElementById(domId);

            // If exists, update name/image
            if (existing) {
                const titleEl = existing.querySelector("h4");
                if (titleEl) titleEl.innerText = name;
                return;
            }

            // Fallback for null images
            if (!img || img === "null") img = type === "group" ? "/images/default-group.png" : "/images/default-user.png";

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

            // If image is a URL, use img tag, otherwise use icon fallback
            let avatarHtml;
            if (img.includes("/") && !img.includes("default")) {
                avatarHtml = `<img src="${img}" style="width: 40px; height: 40px; border-radius: 50%; margin-right: 15px; object-fit: cover;">`;
            } else {
                const iconClass = type === "group" ? "fa-users" : "fa-user";
                const avatarColor = type === "group" ? "#6c5ce7" : "#0984e3";
                avatarHtml = `
                    <div class="avatar" style="
                        width: 40px; height: 40px; background: ${avatarColor}; 
                        border-radius: 50%; display: flex; align-items: center; 
                        justify-content: center; margin-right: 15px; color: white;">
                        <i class="fas ${iconClass}"></i>
                    </div>`;
            }

            li.innerHTML = `
                ${avatarHtml}
                <div class="info" style="flex: 1;">
                    <h4 style="margin: 0; font-size: 14px; font-weight: 600; color: #fff;">${name}</h4>
                    <p style="margin: 0; font-size: 12px; color: #aaa;">Click to chat</p>
                </div>
            `;

            li.addEventListener("click", () => {
                Array.from(chatList.children).forEach(c => c.style.background = "transparent");
                li.style.background = "rgba(255,255,255,0.1)";

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