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

// 4. POST RENDERING (Match Server HTML)
function renderNewPost(post) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return;

    // Remove empty message if exists
    const emptyMsg = document.getElementById('empty-feed-msg');
    if (emptyMsg) emptyMsg.remove();

    const authorPic = post.authorPic && post.authorPic !== "null" ? post.authorPic : "/img/profile_default.jpg";

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
                    </ul>
                </div>
            </div>

            <div class="post-body">
                ${post.title ? `<h5 class="post-title">${post.title}</h5>` : ''}
                ${post.text ? `<div class="post-text text-break">${post.text}</div>` : ''}
                ${renderAttachments(post.attachments)}
            </div>

            <div class="post-footer">
                <button class="btn-post-action btn-like" data-id="${post.id}"><i class="far fa-heart"></i> Like</button>
                <button class="btn-post-action btn-comment-toggle" data-id="${post.id}"><i class="far fa-comment"></i> Comment</button>
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
// 8. INITIAL FEED LOADER
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
                    <i class="${post.isLiked ? 'fas text-danger' : 'far'} fa-heart"></i> Like
                </button>
                <button class="btn-post-action btn-comment-toggle" data-id="${post.id}">
                    <i class="far fa-comment"></i> Comment
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