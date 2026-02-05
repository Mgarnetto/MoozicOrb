/* =========================================
   SIDEBAR MANAGER
   Handles population of profile data
   ========================================= */

const SidebarManager = {
    async init() {
        // Check if we have a session token
        // (Assuming AuthState stores this, or we check localStorage directly)
        const session = localStorage.getItem("moozic_session");

        if (session) {
            await this.updateProfile();
        }
    },

    async updateProfile() {
        try {
            const sidObj = JSON.parse(localStorage.getItem("moozic_session") || "{}");
            const sid = sidObj.sessionId;

            if (!sid) return;

            const res = await fetch("/api/creator/sidebar-info", {
                headers: { "X-Session-Id": sid }
            });

            if (res.ok) {
                const data = await res.json();

                // Populate DOM Elements
                const imgEl = document.getElementById("sidebar-img");
                const nameEl = document.getElementById("sidebar-name");
                const followersEl = document.getElementById("sidebar-followers");
                const followingEl = document.getElementById("sidebar-following");

                if (imgEl) imgEl.style.backgroundImage = `url('${data.pic}')`;
                if (nameEl) nameEl.innerText = data.name;
                if (followersEl) followersEl.innerHTML = `<strong>${data.followers}</strong> Followers`;
                if (followingEl) followingEl.innerHTML = `<strong>${data.following}</strong> Following`;
            }
        } catch (err) {
            console.error("Failed to update sidebar:", err);
        }
    }
};

// Run on Load
document.addEventListener("DOMContentLoaded", () => {
    SidebarManager.init();
});

// Expose globally so Login-Init.js can call it after login
window.SidebarManager = SidebarManager;