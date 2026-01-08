
document.addEventListener("DOMContentLoaded", () => {
    const loginBtn = document.getElementById("login-button");
    const logoutBtn = document.getElementById("logout-button");
    const statusEl = document.getElementById("login-status");

    loginBtn.addEventListener("click", () => {
        const username = document.getElementById("login-username").value;
    const password = document.getElementById("login-password").value;

    LoginService.login(username, password)
            .then(data => {
        statusEl.style.color = "green";
    statusEl.innerText = `Logged in as user ${data.userId}`;
            })
            .catch(err => {
        statusEl.style.color = "red";
    statusEl.innerText = err.message;
            });
    });

    logoutBtn.addEventListener("click", () => {
        LoginService.logout().then(() => {
            statusEl.style.color = "black";
            statusEl.innerText = "Logged out";
        });
    });
});
