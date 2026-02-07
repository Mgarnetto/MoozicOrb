(() => {
    const NotificationService = {
        initialized: false,

        // Called by AuthState.bootstrap()
        init() {
            if (this.initialized) {
                // If re-initializing (e.g. wake up), just refresh
                this.fetchNotifications();
                return;
            }

            const btn = document.getElementById("notifToggleBtn");
            const dropdown = document.getElementById("notifDropdown");
            const markReadBtn = document.getElementById("markAllReadBtn");

            // If UI elements aren't there, abort
            if (!btn || !dropdown) return;

            // 1. Initial Fetch
            this.fetchNotifications();

            // 2. Toggle Handler
            btn.addEventListener("click", (e) => {
                e.stopPropagation();
                const isOpen = dropdown.classList.contains("show");
                if (isOpen) this.closeDropdown();
                else this.openDropdown();
            });

            // 3. Outside Click Handler
            document.addEventListener("click", (e) => {
                if (!dropdown.contains(e.target) && !btn.contains(e.target)) {
                    this.closeDropdown();
                }
            });

            // 4. Mark Read Handler
            if (markReadBtn) {
                markReadBtn.addEventListener("click", () => this.markAllRead());
            }

            this.initialized = true;
        },

        openDropdown() {
            const btn = document.getElementById("notifToggleBtn");
            const dropdown = document.getElementById("notifDropdown");
            if (!dropdown) return;

            dropdown.style.display = "block";
            // Tiny delay to allow CSS transition if needed
            setTimeout(() => dropdown.classList.add("show"), 10);
            if (btn) btn.classList.add("active");

            // Refresh list on open
            this.fetchNotifications();
        },

        closeDropdown() {
            const btn = document.getElementById("notifToggleBtn");
            const dropdown = document.getElementById("notifDropdown");
            if (!dropdown) return;

            dropdown.classList.remove("show");
            setTimeout(() => dropdown.style.display = "none", 200);
            if (btn) btn.classList.remove("active");
        },

        async fetchNotifications() {
            if (!window.AuthState?.sessionId) return;

            const list = document.getElementById("notification-list");

            // Show loading spinner only if the list is currently empty
            if (list && list.children.length === 0) {
                list.innerHTML = '<div style="padding:20px;text-align:center;color:#666;"><i class="fas fa-spinner fa-spin"></i> Loading...</div>';
            }

            try {
                const res = await fetch("/api/notifications", {
                    headers: { "X-Session-Id": window.AuthState.sessionId }
                });

                if (res.ok) {
                    const data = await res.json();
                    this.renderList(data);
                    this.updateBadge(data.length);
                } else if (list) {
                    list.innerHTML = '<div class="notif-empty">Failed to load</div>';
                }
            } catch (e) {
                console.error(e);
                if (list) list.innerHTML = '<div class="notif-empty">Error loading</div>';
            }
        },

        async markAllRead() {
            if (!window.AuthState?.sessionId) return;
            const badge = document.getElementById("nav-notif-badge");
            const list = document.getElementById("notification-list");

            try {
                await fetch("/api/notifications/mark-read", {
                    method: "POST",
                    headers: { "X-Session-Id": window.AuthState.sessionId }
                });

                // Update UI immediately
                if (badge) {
                    badge.innerText = "0";
                    badge.style.display = "none";
                }
                if (list) {
                    const items = list.querySelectorAll(".notif-item.unread");
                    items.forEach(i => i.classList.remove("unread"));
                }
            } catch (e) {
                console.error("Failed to mark read", e);
            }
        },

        updateBadge(count) {
            const badge = document.getElementById("nav-notif-badge");
            if (!badge) return;

            if (count > 0) {
                badge.innerText = count > 9 ? "9+" : count;
                badge.style.display = "flex";
            } else {
                badge.style.display = "none";
            }
        },

        renderList(notifications) {
            const list = document.getElementById("notification-list");
            if (!list) return;

            if (!notifications || notifications.length === 0) {
                list.innerHTML = '<div class="notif-empty">No new notifications</div>';
                return;
            }

            list.innerHTML = '';
            notifications.forEach(n => {
                const li = document.createElement("a");
                li.className = `notif-item ${n.isRead ? '' : 'unread'}`;

                // Determine Link Destination
                let href = "#";
                if (n.type === "post_new" || n.type === "like" || n.type === "comment") {
                    href = `/home/feed?highlight=${n.referenceId}`;
                } else if (n.type === "follow") {
                    href = `/creator/${n.actorId}`;
                }

                li.href = href;

                li.innerHTML = `
                    <img src="${n.actorPic || '/img/profile_default.jpg'}" class="notif-img">
                    <div class="notif-content">
                        <div class="notif-text">
                            <strong>${n.actorName}</strong> ${n.message}
                        </div>
                        <div class="notif-time">${n.createdAgo || 'Just now'}</div>
                    </div>
                `;

                // Handle Message Clicks (open chat instead of navigating)
                if (n.type === "message") {
                    li.onclick = (e) => {
                        e.preventDefault();
                        if (window.startChat) window.startChat(n.actorId);
                        this.closeDropdown();
                    };
                }

                list.appendChild(li);
            });
        }
    };

    // Expose Global Service
    window.NotificationService = NotificationService;
})();