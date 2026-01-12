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

            console.log("[AuthState] Logged in", userId);

            // Enable auth-required buttons
            document.querySelectorAll(".auth-required").forEach(btn => btn.disabled = false);
        },

        setLoggedOut() {
            this.loggedIn = false;
            this.userId = null;
            this.sessionId = null;

            document.body.classList.add("auth-off");
            document.body.classList.remove("auth-on");

            console.log("[AuthState] Logged out");

            // Disable auth-required buttons
            document.querySelectorAll(".auth-required").forEach(btn => btn.disabled = true);
        },

        requireAuth() {
            if (!this.loggedIn) {
                throw new Error("Auth required");
            }
        }
    };

    window.AuthState = AuthState;

    // Initial disable buttons until login
    document.addEventListener("DOMContentLoaded", () => {
        AuthState.setLoggedOut();
    });
})();


