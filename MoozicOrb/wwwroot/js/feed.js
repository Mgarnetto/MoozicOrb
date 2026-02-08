/* =========================================
   FEED & POST LOGIC (Public Layer)
   ========================================= */

// 1. PUBLIC CONNECTION
const feedConnection = new signalR.HubConnectionBuilder()
    .withUrl("/PostHub")
    .withAutomaticReconnect()
    .build();

feedConnection.start().catch(err => console.error("[Feed] Connection failed", err));

document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible" && feedConnection.state === "Disconnected") {
        feedConnection.start();
    }
});

// 2. FEED SERVICE
window.FeedService = {
    joinGroup: (g) => feedConnection.state === "Connected" && feedConnection.invoke("JoinGroup", g),
    leaveGroup: (g) => feedConnection.state === "Connected" && feedConnection.invoke("LeaveGroup", g)
};

// 3. LISTEN FOR POSTS
feedConnection.on("ReceivePost", function (message) {
    const contextInput = document.getElementById('page-signalr-context');
    const pageContext = contextInput ? contextInput.value : null;
    if (message.targetGroup === pageContext || message.targetGroup === "feed_global") {
        renderNewPost(message.data);
    }
});

// --- EXISTING SIGNALR LISTENERS ---

feedConnection.on("UpdatePost", function (msg) {
    // 1. Find the existing post card
    const card = document.getElementById(`post-${msg.postId}`);
    if (card) {
        const titleEl = card.querySelector('.post-title');
        const textEl = card.querySelector('.post-text');

        if (titleEl && msg.data.title) titleEl.innerText = msg.data.title;
        if (textEl && msg.data.text) textEl.innerHTML = msg.data.text;

        // Flash effect
        card.style.transition = "background-color 0.5s";
        card.style.backgroundColor = "#2a2a2a";
        setTimeout(() => card.style.backgroundColor = "", 500);
    }
});

feedConnection.on("RemovePost", function (msg) {
    const card = document.getElementById(`post-${msg.postId}`);
    if (card) {
        card.style.opacity = '0';
        setTimeout(() => card.remove(), 300); // Smooth fade out
    }
});

// --- SERVICE METHODS ---

window.FeedService.deletePost = async (id) => {
    if (!confirm("Are you sure you want to delete this post?")) return;

    try {
        const res = await fetch(`/api/posts/${id}`, {
            method: 'DELETE',
            headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
        });
        if (!res.ok) alert("Failed to delete post.");
    } catch (err) { console.error(err); }
};

// --- NEW: SINGLE POST MODAL (Notifications) ---
// UPDATED: Removed Bootstrap dependency. Uses simple CSS class toggling.
window.FeedService.openPostModal = async (postId, autoComment = false) => {
    const modalEl = document.getElementById('singlePostModal');
    const container = document.getElementById('singlePostContainer');

    if (!modalEl || !container) {
        console.error("Modal elements not found in Layout");
        return;
    }

    // 1. Show Modal (Custom CSS Toggle)
    // We strictly use the class 'active' to control visibility via site.css
    modalEl.classList.add('active');

    // REMOVED: modalEl.style.display = 'block'; (Breaks flex centering)
    // REMOVED: modalEl.style.opacity = '1';

    // 2. Show Loading State
    container.innerHTML = '<div class="text-center p-5"><i class="fas fa-spinner fa-spin fa-2x text-white"></i></div>';

    try {
        // 3. Fetch Partial HTML from Controller
        const res = await fetch(`/api/posts/${postId}/card`, {
            headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
        });

        if (!res.ok) throw new Error("Post not found");

        const html = await res.text();
        container.innerHTML = html;

        // 4. Auto-Open Comments if requested
        if (autoComment) {
            setTimeout(() => {
                const commentBtn = container.querySelector('.btn-comment-toggle');
                if (commentBtn) commentBtn.click();
            }, 300);
        }
    } catch (err) {
        console.error(err);
        container.innerHTML = '<div class="text-center p-4 text-danger">Failed to load post. It may have been deleted.</div>';
    }
};

// --- EDIT MODAL LOGIC (CSS Class Toggle) ---

window.FeedService.openEditModal = async (id) => {
    try {
        const res = await fetch(`/api/posts/${id}`, { headers: { "X-Session-Id": window.AuthState?.sessionId || "" } });
        const post = await res.json();

        document.getElementById('editPostId').value = post.id;
        document.getElementById('editPostTitle').value = post.title || "";
        document.getElementById('editPostText').value = post.text || "";

        // Render Media for Deletion
        const mediaContainer = document.getElementById('editMediaList');
        mediaContainer.innerHTML = '';
        post.media?.forEach(m => {
            const div = document.createElement('div');
            div.className = "position-relative";
            // Check type to render img or audio icon
            div.innerHTML = `
                <img src="${m.url}" class="rounded" style="width:60px;height:60px;object-fit:cover;">
                <button onclick="window.deleteMedia(${post.id}, ${m.id}, this)" class="btn btn-sm btn-danger position-absolute top-0 start-100 translate-middle badge rounded-pill">X</button>
            `;
            mediaContainer.appendChild(div);
        });

        // Toggle CSS class instead of using Bootstrap JS
        const modal = document.getElementById('editPostModal');
        modal.classList.add('active');
        // If your edit modal works with these, leave them, otherwise remove if centering is an issue there too.
        modal.style.display = 'block';
        modal.style.opacity = '1';

    } catch (err) { console.error(err); }
};

window.FeedService.submitEdit = async () => {
    const id = document.getElementById('editPostId').value;

    // CHANGED: Keys capitalized to strictly match C# UpdatePostDto
    const body = {
        Title: document.getElementById('editPostTitle').value,
        Text: document.getElementById('editPostText').value
    };

    const res = await fetch(`/api/posts/${id}`, {
        method: 'PUT',
        headers: {
            "Content-Type": "application/json",
            "X-Session-Id": window.AuthState?.sessionId || ""
        },
        body: JSON.stringify(body)
    });

    if (res.ok) {
        // Close Modal
        closeAllModals();
    } else {
        alert("Update failed");
    }
};

// --- Helper for Edit Modal ---
window.deleteMedia = async (postId, mediaId, btnElement) => {
    if (!confirm("Remove this attachment?")) return;

    // UI feedback
    const wrapper = btnElement.closest('.position-relative');
    wrapper.style.opacity = '0.5';

    try {
        const res = await fetch(`/api/posts/${postId}/media/${mediaId}`, {
            method: 'DELETE',
            headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
        });

        if (res.ok) {
            wrapper.remove();
        } else {
            alert("Failed to delete media.");
            wrapper.style.opacity = '1';
        }
    } catch (err) {
        console.error(err);
        wrapper.style.opacity = '1';
    }
};

// --- GLOBAL MODAL CLOSER (UPDATED) ---
// Handles closing ANY modal with class 'active' when clicking background or close buttons
document.addEventListener('click', function (e) {
    // 1. Close Button Click (looks for data-bs-dismiss attribute or close-btn class)
    if (e.target.matches('.btn-close') || e.target.matches('[data-bs-dismiss="modal"]') || e.target.closest('[data-bs-dismiss="modal"]')) {
        closeAllModals();
    }

    // 2. Click Outside (Overlay)
    // We check if the click target IS the modal wrapper itself (the dark overlay)
    // AND if it has the 'active' class
    if (e.target.classList.contains('modal') && e.target.classList.contains('active')) {
        closeAllModals();
    }
});

// Helper to cleanly close all custom modals
function closeAllModals() {
    const modals = document.querySelectorAll('.modal.active');
    modals.forEach(m => {
        m.classList.remove('active');
        // Reset manual styles we applied (just in case they were set elsewhere)
        m.style.display = '';
        m.style.opacity = '';
    });
}


// 4. POST RENDERING (Match Server HTML)
function renderNewPost(post) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return;

    // Remove empty message if exists
    const emptyMsg = document.getElementById('empty-feed-msg');
    if (emptyMsg) emptyMsg.remove();

    const authorPic = post.authorPic && post.authorPic !== "null" ? post.authorPic : "/img/profile_default.jpg";

    // Logic to check ownership
    const isOwner = window.AuthState && String(window.AuthState.userId) === String(post.authorId);

    const div = document.createElement('div');

    div.innerHTML = `
        <div class="post-card" id="post-${post.id}" style="animation: fadeIn 0.5s ease;">
            <div class="post-header">
                <div class="d-flex align-items-center">
                    <a href="/creator/${post.authorId}" class="post-avatar-link">
                        <img src="${authorPic}" class="post-avatar-img" alt="${post.authorName}" onerror="this.src='/img/profile_default.jpg'">
                    </a>
                    <div class="post-info-col">
                        <div class="d-flex align-items-center gap-2">
                            <a href="/creator/${post.authorId}" class="post-author-name">${post.authorName || 'User'}</a>
                        </div>
                        <div class="post-meta-line"><span>Just now</span></div>
                    </div>
                </div>
                <div class="ms-auto position-relative">
                    <button class="btn btn-link text-muted p-0 btn-post-options" type="button"><i class="fas fa-ellipsis-h"></i></button>
                    <ul class="post-options-menu">
                        <li><a href="#"><i class="fas fa-flag me-2"></i> Report Post</a></li>
                        <li><a href="#"><i class="fas fa-link me-2"></i> Copy Link</a></li>
                        ${isOwner ? `
                        <li>
                            <a href="#" class="dropdown-item" onclick="window.FeedService.openEditModal(${post.id}); return false;">
                                <i class="fas fa-edit me-2"></i> Edit
                            </a>
                        </li>
                        <li>
                            <a href="#" class="dropdown-item text-danger" onclick="window.FeedService.deletePost(${post.id}); return false;">
                                <i class="fas fa-trash me-2"></i> Delete
                            </a>
                        </li>` : ''}
                    </ul>
                </div>
            </div>

            <div class="post-body">
                ${post.title ? `<h5 class="post-title">${post.title}</h5>` : ''}
                ${post.text ? `<div class="post-text text-break">${post.text}</div>` : ''}
                ${renderAttachments(post.attachments)}
            </div>

            <div class="post-footer">
                <button class="btn-post-action btn-like" data-id="${post.id}">
                    <i class="far fa-heart"></i> Like ${post.likesCount > 0 ? `(${post.likesCount})` : ''}
                </button>
                <button class="btn-post-action btn-comment-toggle" data-id="${post.id}">
                    <i class="far fa-comment"></i> Comment ${post.commentsCount > 0 ? `(${post.commentsCount})` : ''}
                </button>
                <button class="btn-post-action"><i class="far fa-share-square"></i> Share</button>
            </div>

            <div id="comments-${post.id}" class="d-none border-top border-secondary p-3">
                <div id="comments-list-${post.id}" class="mb-3"></div>
                
                <div class="d-flex align-items-center gap-2">
                    <img src="/img/profile_default.jpg" class="input-avatar" alt="Me">
                    <div class="comment-input-area">
                        <input type="text" id="comment-input-${post.id}" 
                               placeholder="Write a comment..." 
                               autocomplete="off">
                        <button class="btn-comment-post" onclick="submitReply(${post.id}, null)">Post</button>
                    </div>
                </div>
            </div>
        </div>`;

    container.insertBefore(div.firstElementChild, container.firstChild);
}

function renderAttachments(attachments) {
    if (!attachments || attachments.length === 0) return '';
    let html = `<div class="row g-2 mt-3">`;

    attachments.forEach(media => {
        const colClass = attachments.length === 1 ? "col-12" : "col-6";
        html += `<div class="${colClass}">`;

        if (media.mediaType === 3) {
            // IMAGE
            html += `
            <div class="post-media-container">
                <img src="${media.url}" class="img-fluid full-media" loading="lazy">
            </div>`;
        }
        else if (media.mediaType === 2) {
            // VIDEO (CUSTOM PLAYER UI)
            html += `
            <div class="custom-video-wrapper">
                <video src="${media.url}" class="custom-video" preload="metadata"></video>
                
                <div class="video-overlay-play">
                    <i class="fas fa-play"></i>
                </div>

                <div class="video-controls">
                    <button class="v-btn v-play-toggle"><i class="fas fa-play"></i></button>
                    
                    <div class="v-progress-container">
                        <div class="v-progress-fill"></div>
                    </div>
                    
                    <button class="v-btn v-fullscreen-toggle"><i class="fas fa-expand"></i></button>
                </div>
            </div>`;
        }
        else if (media.mediaType === 1) {
            // AUDIO - TRACK CARD
            const trackTitle = "Track";
            const trackUrl = media.url;

            html += `
            <div class="track-card">
                <button class="btn-track-play" 
                        onclick="if(window.AudioPlayer) window.AudioPlayer.playTrack('${trackUrl}', { title: '${trackTitle}' })">
                    <i class="fas fa-play"></i>
                </button>
                <div class="track-info">
                    <div class="track-title">${trackTitle}</div>
                    <div class="track-artist">Audio</div>
                </div>
                <div class="track-wave"><span></span><span></span><span></span><span></span><span></span></div>
            </div>`;
        }

        html += `</div>`;
    });
    return html + `</div>`;
}

// ============================================
// 5. CREATE POST LOGIC
// ============================================

// State tracking
let activeFileInput = null;
let activeMediaType = null; // 'image', 'video', 'audio'

window.handleFileSelect = function (input, type) {
    // 1. Clear other inputs to ensure only one file is selected
    document.querySelectorAll('input[type="file"]').forEach(el => {
        if (el !== input) el.value = '';
    });

    const preview = document.getElementById('mediaPreview');
    // CHANGED: ID updated to 'postTitleGroup'
    const titleGroup = document.getElementById('postTitleGroup');
    const titleInput = document.getElementById('postTitle');

    // 2. Handle Logic
    if (input.files && input.files[0]) {
        activeFileInput = input;
        activeMediaType = type;
        const file = input.files[0];

        // UI Setup
        preview.classList.remove('d-none');
        let icon = 'fa-paperclip';
        let color = 'text-white';

        if (type === 'audio') {
            icon = 'fa-music';
            color = 'text-warning';
            titleGroup.style.display = 'block';
            titleInput.placeholder = "Track Title (Required)";
            titleInput.focus();
        }
        else if (type === 'video') {
            icon = 'fa-video';
            color = 'text-primary';
            // NEW: Show title input for Video too
            titleGroup.style.display = 'block';
            titleInput.placeholder = "Video Title (Optional)";
        }
        else {
            if (type === 'image') { icon = 'fa-image'; color = 'text-success'; }
            titleGroup.style.display = 'none';
        }

        // Render Preview
        preview.innerHTML = `
            <div class="d-flex align-items-center bg-dark p-2 rounded border border-secondary">
                <i class="fas ${icon} ${color} me-3 fs-4"></i>
                <div class="flex-grow-1 text-truncate">
                    <span class="text-white small">${file.name}</span>
                </div>
                <button type="button" onclick="clearAttachment()" class="btn btn-sm text-muted hover-danger">
                    <i class="fas fa-times"></i>
                </button>
            </div>`;
    } else {
        clearAttachment();
    }
};

window.clearAttachment = function () {
    if (activeFileInput) activeFileInput.value = '';
    activeFileInput = null;
    activeMediaType = null;

    document.getElementById('mediaPreview').classList.add('d-none');
    document.getElementById('mediaPreview').innerHTML = '';

    // CHANGED: ID updated to 'postTitleGroup'
    document.getElementById('postTitleGroup').style.display = 'none';
    document.getElementById('postTitle').value = '';
};

// SUBMIT HANDLER
document.addEventListener('submit', async function (e) {
    if (e.target && e.target.id === 'createPostForm') {
        e.preventDefault();

        const form = e.target;
        const submitBtn = form.querySelector('button[type="submit"]');
        const textArea = document.getElementById('postContent');
        const titleInput = document.getElementById('postTitle');
        const cType = form.querySelector('input[name="ContextType"]')?.value;
        const cId = form.querySelector('input[name="ContextId"]')?.value;

        // --- VALIDATION ---
        if (!cType || !cId) { alert("Error: Page Context is missing."); return; }

        const hasText = textArea.value.trim().length > 0;
        const hasFile = activeFileInput && activeFileInput.files.length > 0;

        if (!hasText && !hasFile) {
            alert("Please enter text or select a file.");
            return;
        }

        // Only enforce title for Audio
        if (activeMediaType === 'audio' && !titleInput.value.trim()) {
            alert("Please enter a Title for your track.");
            titleInput.focus();
            return;
        }

        // --- EXECUTION ---
        const originalText = submitBtn.innerText;
        submitBtn.disabled = true;
        submitBtn.innerText = "Posting...";

        try {
            let attachments = [];

            // 1. Upload File (if exists)
            if (hasFile) {
                const uploadData = new FormData();
                uploadData.append("file", activeFileInput.files[0]);

                const uploadRes = await fetch('/api/upload', {
                    method: 'POST',
                    headers: { 'X-Session-Id': window.AuthState?.sessionId || '' },
                    body: uploadData
                });

                if (uploadRes.ok) {
                    const mediaResult = await uploadRes.json();
                    attachments.push({
                        MediaId: mediaResult.id,
                        MediaType: mediaResult.type,
                        Url: mediaResult.url
                    });
                } else {
                    const errText = await uploadRes.text();
                    throw new Error("Upload failed: " + errText);
                }
            }

            // 2. Post Data
            const payload = {
                ContextType: cType,
                ContextId: cId,
                Type: "standard",
                Title: titleInput.value.trim(),
                Text: textArea.value,
                MediaAttachments: attachments
            };

            const postRes = await fetch('/api/posts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Session-Id': window.AuthState?.sessionId || ''
                },
                body: JSON.stringify(payload)
            });

            if (postRes.ok) {
                textArea.value = '';
                clearAttachment();
            } else {
                const errText = await postRes.text();
                alert("Failed to post: " + errText);
            }

        } catch (error) {
            console.error(error);
            alert("Error: " + error.message);
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = originalText;
        }
    }
});

// ============================================
// 6. EVENT LISTENERS (Menu, Like, Comment)
// ============================================

document.addEventListener('click', async (e) => {
    // A. HAMBURGER MENU
    const optBtn = e.target.closest('.btn-post-options');
    if (optBtn) {
        e.stopPropagation();
        const menu = optBtn.nextElementSibling;
        document.querySelectorAll('.post-options-menu').forEach(el => {
            if (el !== menu) el.classList.remove('show');
        });
        if (menu) menu.classList.toggle('show');
        return;
    }
    if (!e.target.closest('.post-options-menu')) {
        document.querySelectorAll('.post-options-menu').forEach(el => el.classList.remove('show'));
    }

    // B. LIKE BUTTON
    const likeBtn = e.target.closest('.btn-like');
    if (likeBtn) {
        const postId = likeBtn.dataset.id;
        try {
            const res = await fetch(`/api/posts/${postId}/like`, { method: "POST", headers: { "X-Session-Id": window.AuthState?.sessionId || "" } });
            if (res.ok) {
                const data = await res.json();
                const icon = likeBtn.querySelector('i');
                if (data.liked) {
                    icon.classList.remove('far'); icon.classList.add('fas', 'text-danger');
                } else {
                    icon.classList.remove('fas', 'text-danger'); icon.classList.add('far');
                }
            }
        } catch (err) { console.error(err); }
    }

    // C. COMMENT TOGGLE
    const commentBtn = e.target.closest('.btn-comment-toggle');
    if (commentBtn) {
        const postId = commentBtn.dataset.id;
        const section = document.getElementById(`comments-${postId}`);
        if (section) {
            section.classList.toggle('d-none');
            if (!section.classList.contains('d-none')) {
                loadComments(postId);
            }
        }
    }
});

// ============================================
// 7. COMMENT SYSTEM
// ============================================

async function loadComments(postId) {
    const container = document.getElementById(`comments-list-${postId}`);
    container.innerHTML = '<div class="text-muted small ps-2">Loading comments...</div>';
    try {
        const res = await fetch(`/api/posts/${postId}/comments`);
        if (res.ok) {
            const comments = await res.json();
            container.innerHTML = '';
            if (comments.length === 0) {
                container.innerHTML = '<div class="text-muted small ps-2">No comments yet.</div>';
                return;
            }
            comments.forEach(c => container.appendChild(createCommentElement(c)));
        }
    } catch (err) { container.innerHTML = '<div class="text-danger small">Error loading comments.</div>'; }
}

function createCommentElement(c) {
    const wrapper = document.createElement('div');
    wrapper.className = "comment-item";
    wrapper.id = `comment-${c.commentId}`;
    const picUrl = c.authorPic && c.authorPic !== "null" ? c.authorPic : "/img/profile_default.jpg";

    let html = `
        <div class="d-flex align-items-start">
            <img src="${picUrl}" class="comment-avatar" onerror="this.src='/img/profile_default.jpg'">
            <div class="flex-grow-1">
                <div class="comment-content-box">
                    <span class="comment-author">${c.authorName || 'User'}</span>
                    <span class="comment-text">${c.content}</span>
                </div>
                <div class="comment-meta-line">
                    <span class="comment-time">${c.createdAgo}</span>
                    <button class="btn-reply-toggle" onclick="toggleReplyBox(${c.commentId})">Reply</button>
                </div>
                <div id="reply-box-${c.commentId}" class="reply-input-wrapper d-none">
                     <div class="comment-input-area">
                        <input type="text" id="reply-input-${c.commentId}" placeholder="Reply to ${c.authorName}..." autocomplete="off">
                        <button class="btn-comment-post" onclick="submitReply(${c.postId}, ${c.commentId})">Reply</button>
                    </div>
                </div>
            </div>
        </div>`;

    const repliesContainer = document.createElement('div');
    repliesContainer.className = "replies-container";
    if (c.replies && c.replies.length > 0) {
        c.replies.forEach(reply => {
            repliesContainer.appendChild(createCommentElement(reply));
        });
    }
    wrapper.innerHTML = html;
    wrapper.appendChild(repliesContainer);
    return wrapper;
}

window.toggleReplyBox = function (id) {
    const box = document.getElementById(`reply-box-${id}`);
    if (box) {
        box.classList.toggle('d-none');
        if (!box.classList.contains('d-none')) {
            const input = document.getElementById(`reply-input-${id}`);
            if (input) input.focus();
        }
    }
};

window.submitReply = async function (postId, parentId) {
    const inputId = parentId ? `reply-input-${parentId}` : `comment-input-${postId}`;
    const input = document.getElementById(inputId);
    const content = input.value;
    if (!content) return;

    try {
        const res = await fetch('/api/posts/comment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-Session-Id': window.AuthState?.sessionId || '' },
            body: JSON.stringify({ PostId: postId, ParentId: parentId, Content: content })
        });
        if (res.ok) {
            input.value = '';
            if (parentId) {
                const box = document.getElementById(`reply-box-${parentId}`);
                if (box) box.classList.add('d-none');
            }
            loadComments(postId);
        }
    } catch (err) { console.error(err); }
};

// ============================================
// 8. INITIAL FEED LOADER (Standard Cards)
// ============================================

window.loadFeedHistory = async function (contextType, contextId) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return;

    try {
        const res = await fetch(`/api/posts?contextType=${contextType}&contextId=${contextId}&page=1`, {
            headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
        });

        if (res.ok) {
            const posts = await res.json();
            container.innerHTML = '';
            if (posts.length === 0) {
                container.innerHTML = '<div class="text-center text-muted p-5"><h3>No signals found here yet.</h3><p>Be the first to broadcast.</p></div>';
                return;
            }
            posts.forEach(post => {
                appendHistoricalPost(post, container);
            });
        } else {
            container.innerHTML = '<div class="text-danger text-center p-3">Failed to load feed.</div>';
        }
    } catch (err) {
        console.error(err);
        container.innerHTML = '<div class="text-danger text-center p-3">Connection error.</div>';
    }
};

function appendHistoricalPost(post, container) {
    const authorPic = post.authorPic && post.authorPic !== "null" ? post.authorPic : "/img/profile_default.jpg";

    // Logic to check ownership
    const isOwner = window.AuthState && String(window.AuthState.userId) === String(post.authorId);

    const div = document.createElement('div');

    div.innerHTML = `
        <div class="post-card" id="post-${post.id}">
            <div class="post-header">
                <div class="d-flex align-items-center">
                    <a href="/creator/${post.authorId}" class="post-avatar-link">
                        <img src="${authorPic}" class="post-avatar-img" alt="${post.authorName}" onerror="this.src='/img/profile_default.jpg'">
                    </a>
                    <div class="post-info-col">
                        <div class="d-flex align-items-center gap-2">
                            <a href="/creator/${post.authorId}" class="post-author-name">${post.authorName || 'User'}</a>
                        </div>
                        <div class="post-meta-line"><span>${post.createdAgo || 'Just now'}</span></div>
                    </div>
                </div>
                <div class="ms-auto position-relative">
                    <button class="btn btn-link text-muted p-0 btn-post-options" type="button"><i class="fas fa-ellipsis-h"></i></button>
                    <ul class="post-options-menu">
                        <li><a href="#"><i class="fas fa-flag me-2"></i> Report Post</a></li>
                        <li><a href="#"><i class="fas fa-link me-2"></i> Copy Link</a></li>
                        ${isOwner ? `
                        <li>
                            <a href="#" class="dropdown-item" onclick="window.FeedService.openEditModal(${post.id}); return false;">
                                <i class="fas fa-edit me-2"></i> Edit
                            </a>
                        </li>
                        <li>
                            <a href="#" class="dropdown-item text-danger" onclick="window.FeedService.deletePost(${post.id}); return false;">
                                <i class="fas fa-trash me-2"></i> Delete
                            </a>
                        </li>` : ''}
                    </ul>
                </div>
            </div>

            <div class="post-body">
                ${post.title ? `<h5 class="post-title">${post.title}</h5>` : ''}
                ${post.text ? `<div class="post-text text-break">${post.text}</div>` : ''}
                ${renderAttachments(post.attachments)}
            </div>

            <div class="post-footer">
                <button class="btn-post-action btn-like" data-id="${post.id}">
                    <i class="${post.isLiked ? 'fas text-danger' : 'far'} fa-heart"></i> Like ${post.likesCount > 0 ? `(${post.likesCount})` : ''}
                </button>
                <button class="btn-post-action btn-comment-toggle" data-id="${post.id}">
                    <i class="far fa-comment"></i> Comment ${post.commentsCount > 0 ? `(${post.commentsCount})` : ''}
                </button>
                <button class="btn-post-action"><i class="far fa-share-square"></i> Share</button>
            </div>

            <div id="comments-${post.id}" class="d-none border-top border-secondary p-3">
                <div id="comments-list-${post.id}" class="mb-3"></div>
                <div class="d-flex align-items-center gap-2">
                    <img src="/img/profile_default.jpg" class="input-avatar" alt="Me">
                    <div class="comment-input-area">
                        <input type="text" id="comment-input-${post.id}" placeholder="Write a comment..." autocomplete="off">
                        <button class="btn-comment-post" onclick="submitReply(${post.id}, null)">Post</button>
                    </div>
                </div>
            </div>
        </div>`;

    container.appendChild(div.firstElementChild);
}

// ============================================
// 9. AUDIO DISCOVERY LOADER (Playlist View) - UPDATED
// ============================================

window.loadAudioPlaylist = async () => {
    const container = document.getElementById('audio-feed-list');
    if (!container) return;

    try {
        const res = await fetch('/api/posts?contextType=discover&contextId=0', {
            headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
        });

        if (!res.ok) throw new Error("Failed to load audio");
        const posts = await res.json();

        if (!posts || posts.length === 0) {
            container.innerHTML = `<div class="text-center py-5 text-muted">No audio tracks found recently.</div>`;
            return;
        }

        let html = '';
        posts.forEach((post) => {
            const audio = post.attachments && post.attachments.find(a => a.mediaType === 1);
            if (!audio) return;

            const trackSrc = audio.url;
            const imageSrc = post.authorPic && post.authorPic !== "null" ? post.authorPic : '/img/profile_default.jpg';
            const title = post.title || 'Untitled Track';
            const titleEscaped = title.replace(/'/g, "\\'");
            const artist = post.authorName || 'Unknown Artist';
            const profileLink = `/creator/${post.authorId}`;
            const timeAgo = post.createdAgo || 'Just now';

            html += `
<div class="audio-row">
    <div class="audio-meter"><span></span><span></span><span></span><span></span></div>

    <button class="btn-track-play" 
            onclick="window.playTrackInFeed('${trackSrc}', '${titleEscaped}', this)">
        <i class="fas fa-play"></i>
    </button>

    <div class="audio-track-info">
        <div class="text-white fw-bold text-truncate" title="${title}">${title}</div>
        <a href="${profileLink}" class="text-muted small">${artist}</a>
    </div>

    <div class="audio-time-stamp text-muted small d-none d-md-block">
        <i class="far fa-clock me-1"></i> ${timeAgo}
    </div>

    <div class="audio-right-artwork">
        <a href="${profileLink}">
            <img src="${imageSrc}" alt="${artist}">
        </a>
    </div>
</div>`;
        });

        container.innerHTML = html;
    } catch (err) {
        console.error(err);
        container.innerHTML = `<div class="text-center py-5 text-danger">Error loading playlist.</div>`;
    }
};

// HELPER: Handles playing logic + Meter animation toggling
window.playTrackInFeed = function (url, title, element) {
    // 1. Trigger the Global Player (if exists)
    if (window.AudioPlayer) {
        window.AudioPlayer.playTrack(url, { title: title });
    }

    // 2. Manage Visual State (The bouncing bars)
    // Remove .playing from all other rows
    const allRows = document.querySelectorAll('.audio-row');
    allRows.forEach(row => row.classList.remove('playing'));

    // Add .playing to the clicked row
    const currentRow = element.closest('.audio-row');
    if (currentRow) {
        currentRow.classList.add('playing');
    }
};

// ============================================
// 10. SHUFFLE ANIMATION & TRIGGER - FINAL FIX
// ============================================

window.triggerShuffleAnimation = async function () {
    const disc = document.getElementById('shuffle-disc');

    // FIX: Scroll to top immediately when clicked
    window.scrollTo({ top: 0, behavior: 'smooth' });

    // 1. Start Spin (Pure rotation, no color change)
    if (disc) disc.classList.add('fa-spin');

    // 2. Define the tasks: Data Load AND Minimum Timer (1s)
    const minTimer = new Promise(resolve => setTimeout(resolve, 1000));
    // Check if the loader exists, otherwise resolve immediately to avoid errors
    const dataLoad = window.loadAudioPlaylist ? window.loadAudioPlaylist() : Promise.resolve();

    // 3. Wait for BOTH tasks to finish
    try {
        await Promise.all([dataLoad, minTimer]);
    } catch (e) {
        console.error("Shuffle failed", e);
    }

    // 4. Stop Spin
    if (disc) disc.classList.remove('fa-spin');
};

// ============================================
// 11. SOCIAL FEED SHUFFLE ANIMATION
// ============================================

window.triggerSocialShuffle = async function () {
    // 1. Scroll to top immediately
    window.scrollTo({ top: 0, behavior: 'smooth' });

    const globe = document.getElementById('social-globe-icon');

    // 2. Start Spin (Y-Axis rotation using custom CSS class)
    if (globe) globe.classList.add('spin-y-axis');

    // 3. Define tasks: Minimum animation time (0.8s) AND Data Load
    const minTimer = new Promise(resolve => setTimeout(resolve, 800));

    // Call the existing load history function
    // Note: We use 'global' and '0' as defined in your Partial View context
    const dataLoad = window.loadFeedHistory ? window.loadFeedHistory('global', '0') : Promise.resolve();

    // 4. Wait for both to finish
    try {
        await Promise.all([dataLoad, minTimer]);
    } catch (e) {
        console.error("Social shuffle failed", e);
    }

    // 5. Stop Spin
    if (globe) globe.classList.remove('spin-y-axis');
};