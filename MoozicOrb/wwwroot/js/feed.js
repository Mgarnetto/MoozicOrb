/* =========================================
   FEED & POST LOGIC (Public Layer)
   ========================================= */

// 1. PUBLIC CONNECTION (Connects to PostHub)
const feedConnection = new signalR.HubConnectionBuilder()
    .withUrl("/PostHub")
    .withAutomaticReconnect()
    .build();

feedConnection.start().catch(err => console.error("[Feed] Connection failed", err));

// 2. MOBILE LIFECYCLE: Reconnect on Wakeup
document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") {
        if (feedConnection.state === "Disconnected") {
            console.log("[Feed] Waking up SignalR...");
            feedConnection.start().catch(err => console.error("[Feed] Reconnect failed", err));
        }
    }
});

// 3. FEED SERVICE
window.FeedService = {
    currentGroup: null,

    joinGroup: function (groupName) {
        if (feedConnection.state !== "Connected") return;
        feedConnection.invoke("JoinGroup", groupName).catch(err => console.error(err));
        this.currentGroup = groupName;
    },

    leaveGroup: function (groupName) {
        if (feedConnection.state !== "Connected") return;
        feedConnection.invoke("LeaveGroup", groupName).catch(err => console.error(err));
    }
};

// 4. LISTEN FOR POSTS
feedConnection.on("ReceivePost", function (message) {
    const feedWrapper = document.querySelector('.feed-wrapper');

    if (feedWrapper) {
        const contextInput = document.getElementById('page-signalr-context');
        const pageContext = contextInput ? contextInput.value : null;

        if (message.targetGroup === pageContext || message.targetGroup === "feed_global") {
            renderNewPost(message.data);
        }
    }
});

// 5. FORM INTERCEPTOR (With Persistence)
document.addEventListener('submit', async function (e) {
    if (e.target && e.target.id === 'createPostForm') {
        e.preventDefault();

        const form = e.target;
        const submitBtn = form.querySelector('button[type="submit"]');
        const textArea = form.querySelector('textarea[name="Content"]');
        const fileInput = form.querySelector('input[name="mediaFile"]');

        // Grab Context
        const cType = form.querySelector('input[name="ContextType"]')?.value;
        const cId = form.querySelector('input[name="ContextId"]')?.value;

        if (!cType || !cId) {
            alert("Error: Page Context is missing. Please refresh.");
            return;
        }

        if (!textArea.value.trim() && (!fileInput.files || fileInput.files.length === 0)) {
            alert("Please enter text or select a file.");
            return;
        }

        const originalText = submitBtn.innerText;
        submitBtn.disabled = true;
        submitBtn.innerText = "Posting...";

        try {
            let attachments = [];

            // A. Upload
            if (fileInput.files.length > 0) {
                const uploadData = new FormData();
                uploadData.append("file", fileInput.files[0]);

                const uploadRes = await fetch('/api/upload', {
                    method: 'POST',
                    headers: { 'X-Session-Id': window.AuthState?.sessionId || '' },
                    body: uploadData
                });

                if (uploadRes.ok) {
                    const mediaResult = await uploadRes.json();
                    attachments.push({ MediaId: mediaResult.id, MediaType: mediaResult.type, Url: mediaResult.url });
                }
            }

            // B. Create
            const payload = {
                ContextType: cType,
                ContextId: cId,
                Type: "standard",
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
                form.reset();

                // CLEAR DRAFT ON SUCCESS
                localStorage.removeItem("moozic_post_draft");

                const preview = document.getElementById('mediaPreview');
                if (preview) {
                    preview.classList.add('d-none');
                    preview.innerHTML = '';
                }
            } else {
                const errText = await postRes.text();
                try {
                    const errJson = JSON.parse(errText);
                    const msg = errJson.title || "Validation Error";
                    alert("Failed: " + msg);
                } catch {
                    alert("Failed to create post. " + errText);
                }
            }

        } catch (error) {
            console.error("Network Error:", error);
            alert("Network Error. Your post has been saved to drafts.");
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = originalText;
        }
    }
});

// 6. DRAFT PERSISTENCE LOGIC
document.addEventListener("DOMContentLoaded", () => {
    const textArea = document.querySelector('#createPostForm textarea[name="Content"]');
    if (textArea) {
        // Restore Draft
        const savedDraft = localStorage.getItem("moozic_post_draft");
        if (savedDraft) {
            textArea.value = savedDraft;
        }

        // Save Draft on Input
        textArea.addEventListener("input", () => {
            localStorage.setItem("moozic_post_draft", textArea.value);
        });
    }
});

// 7. RENDER LOGIC (UPDATED WITH COMMENT HOOKS)
function renderNewPost(post) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return;

    // Determine Author Pic
    const authorPic = post.authorPic && post.authorPic !== "null" ? post.authorPic : "/img/profile_default.jpg";

    const div = document.createElement('div');
    div.innerHTML = `
        <div class="post-card" id="post-${post.id}" style="animation: fadeIn 0.5s ease;">
            
            <div class="post-header">
                <div class="d-flex align-items-center">
                    <a href="/creator/${post.authorId}" class="post-avatar-link">
                        <img src="${authorPic}" class="post-avatar-img" alt="${post.authorName}">
                    </a>
                    <div class="post-info-col">
                        <div class="d-flex align-items-center gap-2">
                            <a href="/creator/${post.authorId}" class="post-author-name">${post.authorName || 'User'}</a>
                        </div>
                        <div class="post-meta-line">
                            <span>Just now</span>
                        </div>
                    </div>
                </div>
                <div class="ms-auto">
                    <button class="btn btn-link text-muted p-0"><i class="fas fa-ellipsis-h"></i></button>
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
                <div class="d-flex">
                    <input type="text" id="comment-input-${post.id}" class="form-control form-control-sm bg-dark text-white border-secondary me-2" placeholder="Write a comment...">
                    <button class="btn btn-sm btn-primary" onclick="submitReply(${post.id}, null)">Post</button>
                </div>
            </div>
        </div>`;

    container.insertBefore(div.firstElementChild, container.firstChild);
}

// Updated Attachment Renderer
function renderAttachments(attachments) {
    if (!attachments || attachments.length === 0) return '';

    let html = `<div class="row g-2 mt-3">`;

    attachments.forEach(media => {
        const colClass = attachments.length === 1 ? "col-12" : "col-6";

        html += `<div class="${colClass}">`;

        if (media.mediaType === 3) { // Image
            html += `
                <div class="post-media-container">
                    <img src="${media.url}" class="img-fluid full-media" loading="lazy">
                </div>`;
        }
        else if (media.mediaType === 2) { // Video
            html += `
                <div class="post-media-container">
                    <video src="${media.url}" controls class="img-fluid full-media"></video>
                </div>`;
        }
        else if (media.mediaType === 1) { // Audio
            html += `
                <div class="p-3 border border-secondary rounded bg-dark mt-1">
                    <audio src="${media.url}" controls class="w-100"></audio>
                </div>`;
        }

        html += `</div>`;
    });

    html += `</div>`;
    return html;
}

window.previewMedia = function (input) {
    const preview = document.getElementById('mediaPreview');
    if (input.files && input.files[0]) {
        preview.classList.remove('d-none');
        preview.innerHTML = `<div class="text-white small p-2"><i class="fas fa-paperclip"></i> ${input.files[0].name}</div>`;
    }
};

/* ============================================
   8. NEW: COMMENTS & LIKES LOGIC
   ============================================ */

document.addEventListener('click', async (e) => {

    // LIKE BUTTON
    const likeBtn = e.target.closest('.btn-like');
    if (likeBtn) {
        const postId = likeBtn.dataset.id;
        try {
            const res = await fetch(`/api/posts/${postId}/like`, {
                method: "POST",
                headers: { "X-Session-Id": window.AuthState?.sessionId || "" }
            });
            if (res.ok) {
                const data = await res.json();
                const icon = likeBtn.querySelector('i');
                if (data.liked) {
                    icon.classList.remove('far');
                    icon.classList.add('fas', 'text-danger');
                } else {
                    icon.classList.remove('fas', 'text-danger');
                    icon.classList.add('far');
                }
            }
        } catch (err) { console.error(err); }
    }

    // COMMENT TOGGLE
    const commentBtn = e.target.closest('.btn-comment-toggle');
    if (commentBtn) {
        const postId = commentBtn.dataset.id;
        const section = document.getElementById(`comments-${postId}`);
        if (section) {
            section.classList.toggle('d-none');
            // Only load if we are opening it and it's not already populated? 
            // For now, load every time to get fresh data
            if (!section.classList.contains('d-none')) {
                loadComments(postId);
            }
        }
    }
});

// Fetch & Render Recursive Comments
async function loadComments(postId) {
    const listContainer = document.getElementById(`comments-list-${postId}`);
    listContainer.innerHTML = '<div class="text-muted small">Loading...</div>';

    try {
        const res = await fetch(`/api/posts/${postId}/comments`);
        if (res.ok) {
            const comments = await res.json();
            listContainer.innerHTML = '';

            if (comments.length === 0) {
                listContainer.innerHTML = '<div class="text-muted small">No comments yet.</div>';
                return;
            }

            comments.forEach(c => {
                listContainer.appendChild(createCommentElement(c));
            });
        }
    } catch (err) { console.error(err); }
}

function createCommentElement(c) {
    const wrapper = document.createElement('div');
    wrapper.className = "mb-3";
    wrapper.id = `comment-${c.commentId}`;

    let html = `
        <div class="d-flex">
            <img src="${c.authorPic}" class="rounded-circle me-2" width="30" height="30" style="object-fit:cover;">
            <div class="flex-grow-1">
                <div class="bg-dark border border-secondary p-2 rounded">
                    <div class="d-flex justify-content-between">
                        <span class="fw-bold text-white small">${c.authorName}</span>
                        <small class="text-muted" style="font-size:0.75rem">${c.createdAgo}</small>
                    </div>
                    <div class="text-light small mt-1">${c.content}</div>
                </div>
                <div class="mt-1 ms-1">
                    <button class="btn btn-link btn-sm p-0 text-muted text-decoration-none" 
                            style="font-size:0.75rem;" 
                            onclick="toggleReplyBox(${c.commentId})">Reply</button>
                </div>
                <div id="reply-box-${c.commentId}" class="d-none mt-2">
                    <div class="d-flex">
                        <input type="text" id="reply-input-${c.commentId}" class="form-control form-control-sm bg-black text-white border-secondary me-2" placeholder="Reply...">
                        <button class="btn btn-sm btn-primary" onclick="submitReply(${c.postId}, ${c.commentId})">Send</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // RECURSION: Nested Replies
    const repliesContainer = document.createElement('div');
    repliesContainer.className = "ms-5 mt-2 border-start border-secondary ps-2"; // Indentation

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
            headers: {
                'Content-Type': 'application/json',
                'X-Session-Id': window.AuthState?.sessionId || ''
            },
            body: JSON.stringify({ PostId: postId, ParentId: parentId, Content: content })
        });

        if (res.ok) {
            input.value = '';
            if (parentId) document.getElementById(`reply-box-${parentId}`).classList.add('d-none');
            loadComments(postId); // Refresh tree
        }
    } catch (err) { console.error(err); }
};