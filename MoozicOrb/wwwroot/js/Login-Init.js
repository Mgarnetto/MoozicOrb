document.addEventListener("DOMContentLoaded", () => {
    const loginBtn = document.getElementById("loginSubmitBtn");
    const logoutBtn = document.getElementById("logoutBtn");
    const statusEl = document.getElementById("loginStatus");

    // LOGIN LOGIC
    if (loginBtn) {
        loginBtn.addEventListener("click", async () => {
            const usernameInput = document.getElementById("loginUser");
            const passwordInput = document.getElementById("loginPass");

            const username = usernameInput.value;
            const password = passwordInput.value;

            try {
                // 1. Perform the Login
                const data = await LoginService.loginAsync(username, password);

                // 2. Update the App State (Show UI, Load Chats)
                AuthState.setLoggedIn(data.userId, data.sessionId);

                // 3. Save Session so it survives a refresh (See Step 2)
                localStorage.setItem("moozic_session", JSON.stringify(data));

                // 4. Bootstrap (Load data)
                await AuthState.bootstrap();

                if (statusEl) {
                    statusEl.style.color = "green";
                    statusEl.innerText = `Logged in!`;
                }

                // ❌ DELETED: location.reload();  <-- THIS WAS THE PROBLEM

                // Optional: Close the dropdown if you have code for it
                // document.getElementById("loginDropdown").classList.remove("active");

            } catch (err) {
                if (statusEl) {
                    statusEl.style.color = "red";
                    statusEl.innerText = err.message;
                }
            }
        });
    }

    // LOGOUT LOGIC
    if (logoutBtn) {
        logoutBtn.addEventListener("click", async () => {
            try {
                if (AuthState.sessionId) {
                    await LoginService.logout(AuthState.sessionId);
                }

                // Clear storage and state
                localStorage.removeItem("moozic_session");
                AuthState.setLoggedOut();

                if (statusEl) {
                    statusEl.style.color = "black";
                    statusEl.innerText = "Logged out";
                }

                // Reload is okay on logout to clear sensitive data from memory
                location.reload();

            } catch (err) {
                console.error("Logout failed", err);
            }
        });
    }
});



