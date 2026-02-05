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

                // 3. Save Session so it survives a refresh
                localStorage.setItem("moozic_session", JSON.stringify(data));

                // 4. Bootstrap (Load data)
                await AuthState.bootstrap();

                // 5. UPDATE SIDEBAR (NEW)
                // This fetches the user's name/pic and updates the DOM immediately
                if (window.SidebarManager) {
                    await window.SidebarManager.updateProfile();
                }

                if (statusEl) {
                    statusEl.style.color = "green";
                    statusEl.innerText = `Logged in!`;
                }

                // Close the dropdown visually
                const dropdown = document.getElementById("loginDropdown");
                if (dropdown) dropdown.classList.remove("active");

                // Note: No reload here, providing a seamless SPA login experience

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

                // This re-applies 'auth-off' class, which CSS uses to HIDE the sidebar profile
                AuthState.setLoggedOut();

                if (statusEl) {
                    statusEl.style.color = "black";
                    statusEl.innerText = "Logged out";
                }

                // Reload is required to fully clear memory/scripts
                location.reload();

            } catch (err) {
                console.error("Logout failed", err);
                location.reload(); // Fallback to ensure logout happens visually
            }
        });
    }
});



