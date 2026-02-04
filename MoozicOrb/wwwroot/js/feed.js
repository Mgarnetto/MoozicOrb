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

    const authorPic = post.authorPic && post.authorPic !== "null" ? post.authorPic : "/img/profile_default.jpg";

    const div = document.createElement('div');
    // Using structure from _PostCard.cshtml (Updated Inputs)
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
                <div class="d-flex flex-column align-items-end gap-2">
                    <div class="d-flex w-100 align-items-center">
                        <img src="/img/profile_default.jpg" class="rounded-circle me-3" width="32" height="32" style="opacity:0.8;">
                        <input type="text" id="comment-input-${post.id}" 
                               class="form-control bg-dark text-white border-secondary rounded-pill" 
                               placeholder="Write a comment...">
                    </div>
                    <button class="btn btn-sm btn-primary rounded-pill px-4" onclick="submitReply(${post.id}, null)">Post</button>
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

    // C. COMMENT TOGGLE
    const commentBtn = e.target.closest('.btn-comment-toggle');
    if (commentBtn) {
        const postId = commentBtn.dataset.id;
        const section = document.getElementById(`comments-${postId}`);
        if (section) {
            section.classList.toggle('d-none');
            // Only load if it's opening
            if (!section.classList.contains('d-none')) {
                loadComments(postId);
            }
        }
    }
});

// ============================================
// 6. COMMENT SYSTEM
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
                    <div class="comment-text">${c.content}</div>
                </div>

                <div class="comment-meta-line">
                    <span class="comment-time">${c.createdAgo}</span>
                    <button class="btn-reply-toggle" onclick="toggleReplyBox(${c.commentId})">Reply</button>
                </div>

                <div id="reply-box-${c.commentId}" class="reply-input-wrapper d-none">
                    <div class="d-flex flex-column align-items-end gap-2">
                        <input type="text" id="reply-input-${c.commentId}" 
                               class="form-control form-control-sm bg-black text-white border-secondary" 
                               placeholder="Reply to ${c.authorName}...">
                        <button class="btn btn-sm btn-primary rounded-pill px-3" onclick="submitReply(${c.postId}, ${c.commentId})">
                            Reply
                        </button>
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
    box.classList.toggle('d-none');
    if (!box.classList.contains('d-none')) {
        const input = document.getElementById(`reply-input-${id}`);
        if (input) input.focus();
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
            if (parentId) document.getElementById(`reply-box-${parentId}`).classList.add('d-none');
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