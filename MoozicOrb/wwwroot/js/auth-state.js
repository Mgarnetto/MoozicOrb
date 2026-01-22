(() => {
    const AuthState = {
        loggedIn: false,
        userId: null,
        sessionId: null,

        setLoggedIn(userId, sessionId) {
            this.loggedIn = true;
            this.userId = userId;
            this.sessionId = sessionId;

            document.body.classList.add("auth-on");
            document.body.classList.remove("auth-off");

            document.querySelectorAll(".auth-required")
                .forEach(btn => btn.disabled = false);

            console.log("[AuthState] Logged in:", userId);
        },

        setLoggedOut() {
            this.loggedIn = false;
            this.userId = null;
            this.sessionId = null;

            document.body.classList.add("auth-off");
            document.body.classList.remove("auth-on");

            document.querySelectorAll(".auth-required")
                .forEach(btn => btn.disabled = true);

            console.log("[AuthState] Logged out");
        },

        requireAuth() {
            if (!this.loggedIn)
                throw new Error("Auth required");
        },

        async bootstrap() {
            if (!this.sessionId)
                throw new Error("Missing session");

            let data;
            try {
                const res = await fetch("/api/login/bootstrap", {
                    headers: { "X-Session-Id": this.sessionId }
                });

                if (!res.ok) throw new Error("Bootstrap failed");

                data = await res.json();
                console.log("[AuthState] Bootstrap data:", data);
            } catch (err) {
                console.error("[AuthState] Bootstrap error:", err);
                throw err;
            }

            // 1️⃣ Load group messages first
            if (Array.isArray(data.groups)) {
                for (const groupId of data.groups) {
                    try {
                        await loadGroupMessages(groupId);
                    } catch (err) {
                        console.error(`Failed to load group ${groupId}`, err);
                    }
                }
            }

            // 2️⃣ Load direct messages
            if (Array.isArray(data.directUsers)) {
                for (const userId of data.directUsers) {
                    try {
                        await loadDirectMessages(userId);
                    } catch (err) {
                        console.error(`Failed to load DM with user ${userId}`, err);
                    }
                }
            }

            // 3️⃣ Start SignalR connections
            await MessageService.start();

            // 4️⃣ Join groups after messages loaded
            if (Array.isArray(data.groups)) {
                try {
                    await MessageService.joinGroups(data.groups);
                } catch (err) {
                    console.error("[AuthState] Failed to join groups", err);
                }
            }
        },

        // optional helper: dynamically create a new DM container/thread
        createDMThread(userId) {
            if (!MessageService.appendMessage) return;

            const container = document.querySelector(`#chat-container .dm-threads`);
            if (!container) return;

            let thread = document.querySelector(`#dm-thread-${userId}`);
            if (!thread) {
                thread = document.createElement("div");
                thread.id = `dm-thread-${userId}`;
                thread.className = "dm-chat card mb-3";
                thread.innerHTML = `
                    <div class="card-header bg-secondary text-white">
                        DM: User #${userId}
                    </div>
                    <div class="card-body messages" style="height:200px;overflow:auto;"></div>
                `;
                container.appendChild(thread);
            }

            return thread.querySelector(".messages");
        }
    };

    window.AuthState = AuthState;

    document.addEventListener("DOMContentLoaded", () => {
        AuthState.setLoggedOut();
    });
})();

