/* =========================================
   SETTINGS MANAGER
   Handles Uploads & Form Submissions
   ========================================= */

const SettingsManager = {

    // --- UPLOAD HANDLER (Reused for Cover & Avatar) ---
    async uploadImage(fileInput) {
        if (!fileInput.files || fileInput.files.length === 0) return null;

        const formData = new FormData();
        formData.append("file", fileInput.files[0]);

        try {
            const res = await fetch('/api/upload', {
                method: 'POST',
                headers: { 'X-Session-Id': window.AuthState?.sessionId || '' },
                body: formData
            });

            if (res.ok) {
                const data = await res.json();
                return data.url; // Returns "/media/Image/guid.jpg"
            }
        } catch (err) {
            console.error("Upload error:", err);
        }
        return null;
    },

    // --- PAGE SETTINGS (Cover, Bio) ---
    async initPageSettings() {
        // Bind Cover Upload
        const coverInput = document.getElementById('coverUploadInput');
        if (coverInput) {
            coverInput.addEventListener('change', async (e) => {
                const url = await this.uploadImage(e.target);
                if (url) {
                    document.getElementById('coverPreview').style.backgroundImage = `url('${url}')`;
                    window.newCoverUrl = url; // Stash for save
                }
            });
        }

        // Bind Form Save
        const form = document.getElementById('pageSettingsForm');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const payload = {
                    Bio: document.getElementById('inputBio').value,
                    BookingEmail: document.getElementById('inputEmail').value,
                    CoverImage: window.newCoverUrl || document.getElementById('coverPreview').getAttribute('data-current')
                };
                await this.submitJson('/settings/update-page', payload);
            });
        }
    },

    // --- ACCOUNT SETTINGS (Avatar, Name) ---
    async initAccountSettings() {
        // Bind Avatar Upload
        const avatarInput = document.getElementById('avatarUploadInput');
        if (avatarInput) {
            avatarInput.addEventListener('change', async (e) => {
                const url = await this.uploadImage(e.target);
                if (url) {
                    // Update preview
                    document.getElementById('avatarPreview').style.backgroundImage = `url('${url}')`;

                    // Save immediately for Avatar (UX choice)
                    await this.submitJson('/settings/update-avatar', { url: url });

                    // Update Sidebar immediately
                    if (window.SidebarManager) window.SidebarManager.updateProfile();
                }
            });
        }

        // Bind Name Save
        const form = document.getElementById('accountSettingsForm');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const payload = {
                    DisplayName: document.getElementById('inputDisplayName').value,
                    Email: document.getElementById('inputEmailDisplay').value
                };
                await this.submitJson('/settings/update-account', payload);
            });
        }
    },

    // --- HELPER ---
    async submitJson(url, payload) {
        const btn = document.querySelector('button[type="submit"]');
        if (btn) { btn.disabled = true; btn.innerText = "Saving..."; }

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Session-Id': window.AuthState?.sessionId || ''
                },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert("Saved successfully!");
            } else {
                alert("Failed to save.");
            }
        } catch (err) {
            console.error(err);
            alert("Error saving settings.");
        } finally {
            if (btn) { btn.disabled = false; btn.innerText = "Save Changes"; }
        }
    }
};

// Router calls this when loading settings pages
window.initSettings = function () {
    SettingsManager.initPageSettings();
    SettingsManager.initAccountSettings();
};