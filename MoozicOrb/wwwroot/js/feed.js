/* =========================================
   FEED & POST LOGIC (Client Side)
   ========================================= */

// 1. SIGNALR BRIDGE (Exposed for Router.js)
window.MessageService = {
    currentGroup: null,

    joinGroup: function (groupName) {
        if (!connection || connection.state !== "Connected") return;

        console.log(`[SignalR] Joining: ${groupName}`);
        connection.invoke("JoinGroup", groupName).catch(err => console.error(err));
        this.currentGroup = groupName;
    },

    leaveGroup: function (groupName) {
        if (!connection || connection.state !== "Connected") return;

        console.log(`[SignalR] Leaving: ${groupName}`);
        connection.invoke("LeaveGroup", groupName).catch(err => console.error(err));
    }
};

document.addEventListener("DOMContentLoaded", function () {
    // 2. SIGNALR LISTENER (Receives live posts)
    if (typeof connection !== 'undefined') {
        connection.on("ReceivePost", function (message) {
            const feedWrapper = document.querySelector('.feed-wrapper');
            // Guard: Only render if we are on a page with a feed
            if (feedWrapper) {
                const pageContext = feedWrapper.dataset.contextType + "_" + feedWrapper.dataset.contextId;
                // Render if Global or Specific Match
                if (message.targetGroup === pageContext || message.targetGroup === "feed_global") {
                    renderNewPost(message.data);
                }
            }
        });
    }
});

// 3. FORM INTERCEPTOR (Fixes the 404 Error)
document.addEventListener('submit', async function (e) {
    if (e.target && e.target.id === 'createPostForm') {
        e.preventDefault(); // Stop Browser Navigation

        const form = e.target;
        const submitBtn = form.querySelector('button[type="submit"]');
        const textArea = form.querySelector('textarea[name="Content"]');
        const fileInput = form.querySelector('input[name="mediaFile"]');

        // Validation
        if (!textArea.value.trim() && (!fileInput.files || fileInput.files.length === 0)) {
            alert("Please enter text or select a file.");
            return;
        }

        // Lock UI
        const originalText = submitBtn.innerText;
        submitBtn.disabled = true;
        submitBtn.innerText = "Posting...";

        try {
            let attachments = [];

            // A. Upload File First (if exists)
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
                    attachments.push({
                        MediaId: mediaResult.id,
                        MediaType: mediaResult.type
                    });
                } else {
                    console.warn("File upload failed");
                    alert("Media upload failed, posting text only.");
                }
            }

            // B. Create Post (Send JSON to PostController)
            const payload = {
                ContextType: form.querySelector('input[name="ContextType"]').value,
                ContextId: form.querySelector('input[name="ContextId"]').value,
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
                // Success! SignalR will handle the render
                form.reset();
                const preview = document.getElementById('mediaPreview');
                if (preview) {
                    preview.classList.add('d-none');
                    preview.innerHTML = '';
                }
            } else {
                alert("Failed to create post.");
            }

        } catch (error) {
            console.error("Error:", error);
            alert("Connection error.");
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = originalText;
        }
    }
});

// 4. HELPER FUNCTIONS
function renderNewPost(post) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return;

    const div = document.createElement('div');
    // Simplified Card Structure
    div.innerHTML = `
        <div class="card mb-3 shadow-sm border-0 post-card">
            <div class="card-header bg-white border-0 d-flex align-items-center pt-3">
                <img src="${post.authorPic}" class="rounded-circle me-2 object-fit-cover" width="40" height="40">
                <div>
                    <span class="fw-bold d-block">${post.authorName}</span>
                    <small class="text-muted">Just now</small>
                </div>
            </div>
            <div class="card-body">
                <p class="card-text">${post.text || ''}</p>
                ${renderAttachments(post.attachments)}
            </div>
        </div>
    `;

    // Animation
    div.firstElementChild.style.animation = "fadeIn 0.5s ease";
    container.insertBefore(div.firstElementChild, container.firstChild);
}

function renderAttachments(attachments) {
    if (!attachments || attachments.length === 0) return '';
    const media = attachments[0];
    if (media.mediaType === 3) return `<img src="${media.url}" class="img-fluid rounded w-100 mb-2">`;
    if (media.mediaType === 2) return `<video src="${media.url}" controls class="img-fluid rounded w-100 mb-2"></video>`;
    if (media.mediaType === 1) return `<audio src="${media.url}" controls class="w-100 mb-2"></audio>`;
    return '';
}

window.previewMedia = function (input) {
    const preview = document.getElementById('mediaPreview');
    if (input.files && input.files[0]) {
        preview.classList.remove('d-none');
        preview.innerHTML = `<div class="text-white small p-2"><i class="fas fa-paperclip"></i> ${input.files[0].name}</div>`;
    }
};