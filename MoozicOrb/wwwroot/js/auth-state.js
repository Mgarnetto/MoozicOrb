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

            console.log("[AuthState] Logged in", userId);
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

            const res = await fetch("/api/login/bootstrap", {
                headers: {
                    "X-Session-Id": this.sessionId
                }
            });

            if (!res.ok)
                throw new Error("Bootstrap failed");

            const data = await res.json();
            console.log("[AuthState] Bootstrap data", data);

            // 1️⃣ Start SignalR
            await MessageService.start();

            // 2️⃣ Load messages first
            for (const groupId of data.groups) {
                await loadGroupMessages(groupId);
            }

            // 3️⃣ Join groups for realtime updates
            await MessageService.joinGroups(data.groups);
        }
    };

    window.AuthState = AuthState;

    document.addEventListener("DOMContentLoaded", () => {
        AuthState.setLoggedOut();
    });
})();



