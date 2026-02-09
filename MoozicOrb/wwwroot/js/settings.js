const SettingsManager = {

    // ... [Existing uploadImage method] ...
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
                return data.url;
            }
        } catch (err) { console.error("Upload error:", err); }
        return null;
    },

    // --- PAGE SETTINGS (Cover, Bio, AND Layout) ---
    async initPageSettings() {
        // 1. Bind Cover Upload
        const coverInput = document.getElementById('coverUploadInput');
        if (coverInput) {
            coverInput.addEventListener('change', async (e) => {
                const url = await this.uploadImage(e.target);
                if (url) {
                    document.getElementById('coverPreview').style.backgroundImage = `url('${url}')`;
                    window.newCoverUrl = url;
                }
            });
        }

        // 2. Initialize Drag & Drop (The Fix)
        this.initDraggableLayout();

        // 3. Bind Form Save
        const form = document.getElementById('pageSettingsForm');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();

                // Scrape the new order from the DOM
                const layoutOrder = [];
                document.querySelectorAll('#layoutSortContainer .sort-item').forEach(el => {
                    layoutOrder.push(el.getAttribute('data-key'));
                });

                const payload = {
                    Bio: document.getElementById('inputBio').value,
                    BookingEmail: document.getElementById('inputEmail').value,
                    CoverImage: window.newCoverUrl || document.getElementById('coverPreview').getAttribute('data-current'),
                    LayoutOrder: layoutOrder // Include the list
                };

                await this.submitJson('/settings/update-page', payload);
            });
        }
    },

    // --- NEW: Draggable Logic ---
    initDraggableLayout() {
        const container = document.getElementById('layoutSortContainer');
        if (!container) return;

        let draggedItem = null;

        // Event: Drag Start
        container.addEventListener('dragstart', (e) => {
            if (!e.target.classList.contains('sort-item')) return;
            draggedItem = e.target;
            e.target.style.opacity = '0.5';
            e.target.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            // Required for Firefox to allow dragging
            e.dataTransfer.setData('text/plain', '');
        });

        // Event: Drag End
        container.addEventListener('dragend', (e) => {
            if (!e.target.classList.contains('sort-item')) return;
            e.target.style.opacity = '1';
            e.target.classList.remove('dragging');
            draggedItem = null;
        });

        // Event: Drag Over (CRITICAL: preventDefault allows the drop)
        container.addEventListener('dragover', (e) => {
            e.preventDefault();
            const afterElement = getDragAfterElement(container, e.clientY);
            const currentDraggable = document.querySelector('.dragging');
            if (!currentDraggable) return;

            if (afterElement == null) {
                container.appendChild(currentDraggable);
            } else {
                container.insertBefore(currentDraggable, afterElement);
            }
        });

        // Helper to find position
        function getDragAfterElement(container, y) {
            const draggableElements = [...container.querySelectorAll('.sort-item:not(.dragging)')];

            return draggableElements.reduce((closest, child) => {
                const box = child.getBoundingClientRect();
                const offset = y - box.top - box.height / 2;
                if (offset < 0 && offset > closest.offset) {
                    return { offset: offset, element: child };
                } else {
                    return closest;
                }
            }, { offset: Number.NEGATIVE_INFINITY }).element;
        }
    },

    // ... [Existing initAccountSettings] ...
    async initAccountSettings() {
        // ... (Keep existing logic unchanged) ...
        const avatarInput = document.getElementById('avatarUploadInput');
        if (avatarInput) {
            avatarInput.addEventListener('change', async (e) => {
                const url = await this.uploadImage(e.target);
                if (url) {
                    document.getElementById('avatarPreview').style.backgroundImage = `url('${url}')`;
                    await this.submitJson('/settings/update-avatar', { url: url });
                    if (window.SidebarManager) window.SidebarManager.updateProfile();
                }
            });
        }
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

    // ... [Existing submitJson] ...
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
            if (res.ok) alert("Saved successfully!");
            else alert("Failed to save.");
        } catch (err) {
            console.error(err);
            alert("Error saving settings.");
        } finally {
            if (btn) { btn.disabled = false; btn.innerText = "Save Changes"; }
        }
    }
};

window.initSettings = function () {
    SettingsManager.initPageSettings();
    SettingsManager.initAccountSettings();
};