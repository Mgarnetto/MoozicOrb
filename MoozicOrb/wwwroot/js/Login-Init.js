document.addEventListener("DOMContentLoaded", () => {
    const loginBtn = document.getElementById("login-button");
    const logoutBtn = document.getElementById("logout-button");
    const statusEl = document.getElementById("login-status");

    loginBtn.addEventListener("click", async () => {
        const username = document.getElementById("login-username").value;
        const password = document.getElementById("login-password").value;

        try {
            const data = await LoginService.loginAsync(username, password);

            AuthState.setLoggedIn(data.userId, data.sessionId);
            await AuthState.bootstrap();

            statusEl.style.color = "green";
            statusEl.innerText = `Logged in as user ${data.userId}`;
        } catch (err) {
            statusEl.style.color = "red";
            statusEl.innerText = err.message;
        }
    });

    logoutBtn.addEventListener("click", async () => {
        try {
            await LoginService.logout(AuthState.sessionId);
            AuthState.setLoggedOut();

            statusEl.style.color = "black";
            statusEl.innerText = "Logged out";
        } catch (err) {
            statusEl.style.color = "red";
            statusEl.innerText = err.message;
        }
    });
});



