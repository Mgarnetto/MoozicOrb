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

    // Remove loading placeholder if it exists
    const placeholder = container.querySelector('.loading-placeholder');
    if (placeholder) placeholder.remove();

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
        html += `<div class="${colClass}"><div class="post-media-container">`;
        if (media.mediaType === 3) html += `<img src="${media.url}" class="img-fluid full-media" loading="lazy">`;
        else if (media.mediaType === 2) html += `<video src="${media.url}" controls class="img-fluid full-media"></video>`;
        else if (media.mediaType === 1) html += `<audio src="${media.url}" controls class="w-100 p-2"></audio>`;
        html += `</div></div>`;
    });
    return html + `</div>`;
}

// ============================================
// 5. EVENT LISTENERS
// ============================================

document.addEventListener('click', async (e) => {

    // A. HAMBURGER MENU (Manual Toggle)
    const optBtn = e.target.closest('.btn-post-options');
    if (optBtn) {
        e.stopPropagation();
        const menu = optBtn.nextElementSibling; // The <ul>
        // Close others
        document.querySelectorAll('.post-options-menu').forEach(el => {
            if (el !== menu) el.classList.remove('show');
        });
        // Toggle current
        if (menu) menu.classList.toggle('show');
        return;
    }
    // Close menus if clicking anywhere else
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

    // C. COMMENT TOGGLE (FIXED LOGIC)
    const commentBtn = e.target.closest('.btn-comment-toggle');
    if (commentBtn) {
        const postId = commentBtn.dataset.id;
        const section = document.getElementById(`comments-${postId}`);
        if (section) {
            section.classList.toggle('d-none');

            // Logic: If we just removed 'd-none', it means it is now visible. Load comments.
            if (!section.classList.contains('d-none')) {
                loadComments(postId);
            }
        }
    }
});

// ============================================
// 6. COMMENT SYSTEM (FIXED FLOW)
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
                        <input type="text" id="reply-input-${c.commentId}" 
                               placeholder="Reply to ${c.authorName}..." 
                               autocomplete="off">
                        <button class="btn-comment-post" onclick="submitReply(${c.postId}, ${c.commentId})">Reply</button>
                    </div>
                </div>
            </div>
        </div>
    `;

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

document.addEventListener('submit', async function (e) {
    if (e.target && e.target.id === 'createPostForm') {
        e.preventDefault();
        const form = e.target;
        const submitBtn = form.querySelector('button[type="submit"]');
        const textArea = form.querySelector('textarea[name="Content"]');
        const fileInput = form.querySelector('input[name="mediaFile"]');
        const cType = form.querySelector('input[name="ContextType"]')?.value;
        const cId = form.querySelector('input[name="ContextId"]')?.value;

        if (!cType || !cId) { alert("Error: Page Context is missing."); return; }
        if (!textArea.value.trim() && (!fileInput.files || fileInput.files.length === 0)) { alert("Please enter text or select a file."); return; }

        const originalText = submitBtn.innerText;
        submitBtn.disabled = true;
        submitBtn.innerText = "Posting...";

        try {
            let attachments = [];
            if (fileInput.files.length > 0) {
                const uploadData = new FormData();
                uploadData.append("file", fileInput.files[0]);
                const uploadRes = await fetch('/api/upload', { method: 'POST', headers: { 'X-Session-Id': window.AuthState?.sessionId || '' }, body: uploadData });
                if (uploadRes.ok) {
                    const mediaResult = await uploadRes.json();
                    attachments.push({ MediaId: mediaResult.id, MediaType: mediaResult.type, Url: mediaResult.url });
                }
            }

            const payload = { ContextType: cType, ContextId: cId, Type: "standard", Text: textArea.value, MediaAttachments: attachments };
            const postRes = await fetch('/api/posts', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Session-Id': window.AuthState?.sessionId || '' }, body: JSON.stringify(payload) });

            if (postRes.ok) {
                form.reset();
                const preview = document.getElementById('mediaPreview');
                if (preview) { preview.classList.add('d-none'); preview.innerHTML = ''; }
            } else {
                const errText = await postRes.text();
                alert("Failed to post: " + errText);
            }
        } catch (error) { console.error(error); }
        finally { submitBtn.disabled = false; submitBtn.innerText = originalText; }
    }
});

// ============================================
// 7. INITIAL FEED LOADER (NEW)
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

            // Clear loading spinner
            container.innerHTML = '';

            if (posts.length === 0) {
                container.innerHTML = '<div class="text-center text-muted p-5"><h3>No signals found here yet.</h3><p>Be the first to broadcast.</p></div>';
                return;
            }

            // Append historical posts
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

    // Identical HTML to renderNewPost but without animation for initial load
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