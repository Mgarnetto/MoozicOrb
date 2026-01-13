(() => {
    const LoginService = {
        async loginAsync(username, password) {
            const res = await fetch("/api/login", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: `username=${encodeURIComponent(username)}&password=${encodeURIComponent(password)}`
            });

            if (!res.ok) {
                const err = await res.json();
                throw new Error(err?.message || "Login failed");
            }

            return await res.json();
        },

        async logout(sessionId) {
            if (!sessionId) return;

            await fetch("/api/login/logout", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: `sessionId=${encodeURIComponent(sessionId)}`
            });
        }
    };

    window.LoginService = LoginService;
})();




