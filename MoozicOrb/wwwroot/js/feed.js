/* =========================================
   FEED & POST LOGIC (Public Layer)
   ========================================= */

// 1. PUBLIC CONNECTION (Connects to PostHub)
const feedConnection = new signalR.HubConnectionBuilder()
    .withUrl("/PostHub")
    .withAutomaticReconnect() // Helps, but explicit check on wakeup is better
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
// Initializes when the DOM is ready
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

// 7. RENDER LOGIC (VISUALLY UPDATED)
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
                <button class="btn-post-action"><i class="far fa-heart"></i> Like</button>
                <button class="btn-post-action"><i class="far fa-comment"></i> Comment</button>
                <button class="btn-post-action"><i class="far fa-share-square"></i> Share</button>
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