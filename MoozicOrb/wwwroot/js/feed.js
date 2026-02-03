/* =========================================
   FEED & POST LOGIC (Public Layer)
   ========================================= */

// 1. PUBLIC CONNECTION (Connects to PostHub)
// This runs immediately so guests can see the feed.
const feedConnection = new signalR.HubConnectionBuilder()
    .withUrl("/PostHub")
    .withAutomaticReconnect()
    .build();

feedConnection.start().catch(err => console.error("[Feed] Connection failed", err));

// 2. FEED SERVICE (Renamed from MessageService to avoid conflicts)
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

// 3. LISTEN FOR POSTS
feedConnection.on("ReceivePost", function (message) {
    // We look for a wrapper class to know if we are on a feed page
    const feedWrapper = document.querySelector('.feed-wrapper');

    if (feedWrapper) {
        // Read the Context ID from the hidden input on the page
        const contextInput = document.getElementById('page-signalr-context');
        const pageContext = contextInput ? contextInput.value : null;

        // Render if Global or Specific Match
        if (message.targetGroup === pageContext || message.targetGroup === "feed_global") {
            renderNewPost(message.data);
        }
    }
});

// 4. FORM INTERCEPTOR
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
                const preview = document.getElementById('mediaPreview');
                if (preview) {
                    preview.classList.add('d-none');
                    preview.innerHTML = '';
                }
            } else {
                const errText = await postRes.text();
                // Parse generic 400 errors for better alerts
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
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = originalText;
        }
    }
});

function renderNewPost(post) {
    const container = document.getElementById('feed-stream-container');
    if (!container) return; // This ID must exist in your HTML!

    const div = document.createElement('div');
    // Using simple HTML string - ensure this matches your _PostCard logic if possible
    div.innerHTML = `
        <div class="card mb-3 shadow-sm border-0 post-card" style="animation: fadeIn 0.5s ease;">
            <div class="card-header bg-white border-0 d-flex align-items-center pt-3">
                <img src="${post.authorPic || '/img/default.png'}" class="rounded-circle me-2 object-fit-cover" width="40" height="40">
                <div>
                    <span class="fw-bold d-block">${post.authorName || 'User'}</span>
                    <small class="text-muted">Just now</small>
                </div>
            </div>
            <div class="card-body">
                <p class="card-text">${post.text || ''}</p>
                ${renderAttachments(post.attachments)}
            </div>
        </div>`;

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