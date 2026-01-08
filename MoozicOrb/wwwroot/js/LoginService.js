(() => {
    const LoginService = {
        sessionId: null,
        userId: null,

        // Call API to login
        login(username, password) {
            return fetch("/api/login", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: `username=${encodeURIComponent(username)}&password=${encodeURIComponent(password)}`
            })
                .then(async res => {
                    if (!res.ok) {
                        const err = await res.json();
                        throw new Error(err?.message || "Login failed");
                    }
                    return res.json();
                })
                .then(data => {
                    this.sessionId = data.sessionId;
                    this.userId = data.userId;

                    // Store globally for other services
                    window.AppSession = { sessionId: this.sessionId, userId: this.userId };

                    return data;
                });
        },

        // Call API to logout
        logout() {
            if (!this.sessionId) return Promise.resolve();

            return fetch("/api/login/logout", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: `sessionId=${encodeURIComponent(this.sessionId)}`
            })
                .then(() => {
                    this.sessionId = null;
                    this.userId = null;
                    window.AppSession = null;
                });
        },

        // Check if logged in (session exists)
        isLoggedIn() {
            return !!this.sessionId;
        }
    };

    window.LoginService = LoginService;
})();
