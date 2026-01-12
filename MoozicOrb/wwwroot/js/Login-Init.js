document.addEventListener("DOMContentLoaded", () => {
    const loginBtn = document.getElementById("login-button");
    const logoutBtn = document.getElementById("logout-button");
    const statusEl = document.getElementById("login-status");

    loginBtn.addEventListener("click", async () => {
        const username = document.getElementById("login-username").value;
        const password = document.getElementById("login-password").value;

        try {
            const data = await LoginService.loginAsync(username, password);

            // Update status
            statusEl.style.color = "green";
            statusEl.innerText = `Logged in as user ${data.userId}`;

            // Update AuthState
            AuthState.setLoggedIn(data.userId, data.sessionId);

            // Attach user to SignalR hub and join default group
            if (messageconn.state === "Connected") {
                await messageconn.invoke("AttachUserSession", data.userId);
                await messageconn.invoke("JoinGroup", 9);
            }

        } catch (err) {
            statusEl.style.color = "red";
            statusEl.innerText = err.message;
        }
    });

    logoutBtn.addEventListener("click", async () => {
        try {
            await LoginService.logout();

            statusEl.style.color = "black";
            statusEl.innerText = "Logged out";

            AuthState.setLoggedOut();
        } catch (err) {
            statusEl.style.color = "red";
            statusEl.innerText = err.message;
        }
    });
});


