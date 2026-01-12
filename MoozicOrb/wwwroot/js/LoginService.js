(() => {
    const LoginService = {
        sessionId: null,
        userId: null,

        loginAsync(username, password) {
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
                    this.userId = data.userId;
                    this.sessionId = data.sessionId;
                    window.AppSession = { userId: this.userId, sessionId: this.sessionId };

                    enableFeaturesAfterLogin();

                    // Attach user session to SignalR hub
                    if (messageconn.state === "Connected") {
                        messageconn.invoke("AttachUserSession", this.userId);
                        messageconn.invoke("JoinGroup", 9); // join default group
                    }

                    return data;
                });
        },

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

                    disableFeaturesBeforeLogin();
                });
        },

        isLoggedIn() {
            return !!this.sessionId;
        }
    };

    window.LoginService = LoginService;

    function disableFeaturesBeforeLogin() {
        document.querySelectorAll(".send-group, #start-broadcast, #join-stream, #start-call, #hangup-call")
            .forEach(el => el.disabled = true);
    }

    function enableFeaturesAfterLogin() {
        document.querySelectorAll(".send-group, #start-broadcast, #join-stream, #start-call, #hangup-call")
            .forEach(el => el.disabled = false);
    }

    document.addEventListener("DOMContentLoaded", disableFeaturesBeforeLogin);
})();



