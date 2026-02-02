// Global State for the current draft
let currentAttachments = [];

document.addEventListener("DOMContentLoaded", function () {
    // 1. SIGNALR LISTENER (Real-time updates)
    // Checks if the main 'connection' object exists (from site.js/main.js)
    if (typeof connection !== 'undefined') {

        connection.on("ReceivePost", function (message) {
            // message.data = PostDto
            // message.targetGroup = "user_105", "state_GA", etc.

            // Check if we are currently looking at the feed this post belongs to
            const feedWrapper = document.querySelector('.feed-wrapper');
            if (!feedWrapper) return;

            // Construct the expected group ID for the current page
            // e.g. "user_" + "105" or "state_" + "GA"
            const pageContext = feedWrapper.dataset.contextType + "_" + feedWrapper.dataset.contextId;

            // If the post matches this page (or is global), prepend it!
            if (message.targetGroup === pageContext || message.targetGroup === "feed_global") {
                renderNewPost(message.data);
            }
        });
    }
});

// ==========================================
// 2. UPLOAD LOGIC (Immediate Upload)
// ==========================================

function handleFileSelect(input) {
    const container = document.getElementById('media-preview-area');
    const files = input.files;

    if (files.length === 0) return;

    // Loop through selected files and upload each one
    Array.from(files).forEach(file => {
        uploadSingleFile(file, container);
    });

    // Reset input so you can select the same file again if needed
    input.value = "";
}

async function uploadSingleFile(file, container) {
    // A. Create Temporary UI (Spinner)
    const tempId = "temp-" + Date.now() + Math.random().toString(16).slice(2);
    const previewDiv = document.createElement('div');
    previewDiv.id = tempId;
    previewDiv.className = "position-relative d-inline-block me-2";
    previewDiv.innerHTML = `
        <div class="ratio ratio-1x1 rounded border bg-light d-flex align-items-center justify-content-center" style="width:80px;height:80px;">
            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
        </div>`;
    container.appendChild(previewDiv);

    // B. Send to Server
    const formData = new FormData();
    formData.append("file", file);

    try {
        // Calls the "Universal" endpoint we made in Part 1
        const response = await fetch('/api/upload', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) throw new Error("Upload failed");

        const result = await response.json();
        // Expected JSON: { id: 505, type: 3, url: "/media/Image/..." }

        // C. Success: Store ID in our global array
        currentAttachments.push({
            MediaId: result.id,
            MediaType: result.type
        });

        // D. Success: Replace Spinner with Preview
        let htmlContent = "";

        if (result.type === 3) { // Image
            // Uses the mapped URL (e.g. /media/Image/abc.jpg)
            htmlContent = `<img src="${result.url}" class="rounded border object-fit-cover" style="width:80px; height:80px;">`;
        } else if (result.type === 2) { // Video
            htmlContent = `<div class="rounded border bg-dark text-white d-flex align-items-center justify-content-center" style="width:80px; height:80px;"><i class="fas fa-video"></i></div>`;
        } else { // Audio
            htmlContent = `<div class="rounded border bg-warning text-dark d-flex align-items-center justify-content-center" style="width:80px; height:80px;"><i class="fas fa-music"></i></div>`;
        }

        previewDiv.innerHTML = `
            ${htmlContent}
            <button onclick="removeAttachment('${tempId}', ${result.id})" class="btn btn-danger btn-sm position-absolute top-0 end-0 p-0 rounded-circle shadow-sm" style="width:20px;height:20px;line-height:1;transform:translate(30%,-30%);">&times;</button>
        `;

    } catch (err) {
        console.error(err);
        previewDiv.remove(); // Remove failed upload slot
        alert("Failed to upload " + file.name);
    }
}

function removeAttachment(elementId, mediaId) {
    // Remove from array
    currentAttachments = currentAttachments.filter(x => x.MediaId !== mediaId);
    // Remove from DOM
    const el = document.getElementById(elementId);
    if (el) el.remove();
}

// ==========================================
// 3. SUBMIT POST (Final Step)
// ==========================================

async function submitPost() {
    const txtInput = document.getElementById('post-text-input');
    const wrapper = document.querySelector('.feed-wrapper');
    const btn = document.querySelector('.post-input-area button.btn-primary'); // The "Post" button

    if (!txtInput || !wrapper) return;

    // A. Validation
    if (!txtInput.value.trim() && currentAttachments.length === 0) {
        alert("Please write something or attach media.");
        return;
    }

    // B. Disable button
    if (btn) btn.disabled = true;

    // C. Construct Payload
    const payload = {
        ContextType: wrapper.dataset.contextType, // e.g. "user"
        ContextId: wrapper.dataset.contextId,     // e.g. "105"
        Type: "standard",
        Title: null,
        Text: txtInput.value,
        ImageUrl: null,
        MediaAttachments: currentAttachments // Our list of uploaded IDs
    };

    try {
        const response = await fetch('/api/posts', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            // Success!

            // 1. Reset Form
            txtInput.value = "";
            document.getElementById('media-preview-area').innerHTML = "";
            currentAttachments = [];

            // 2. Feedback (Optional toast or sound)
            console.log("Post created successfully");

            // Note: We don't manually add the HTML here because SignalR 
            // (listener at the top) should receive the event and add it.
            // If SignalR fails, you can uncomment this:
            // window.location.reload();

        } else {
            const errText = await response.text();
            alert("Error creating post: " + errText);
        }
    } catch (err) {
        console.error(err);
        alert("Network error.");
    } finally {
        if (btn) btn.disabled = false;
    }
}

// ==========================================
// 4. RENDERING HELPER (SignalR)
// ==========================================

function renderNewPost(postDto) {
    const stream = document.getElementById('feed-stream-container');
    if (!stream) return;

    // Remove "empty state" placeholder if it exists
    const emptyState = stream.querySelector('.text-muted.py-5');
    if (emptyState) emptyState.remove();

    // Create a container to hold the HTML
    const div = document.createElement('div');

    // OPTION A: Fetch the Partial View HTML from server (Cleaner)
    // fetch(`/api/posts/render/${postDto.id}`)...

    // OPTION B: Quick Client-Side Template (Faster for now)
    // Matches _PostCard.cshtml structure roughly
    div.innerHTML = `
        <div class="card mb-3 shadow-sm border-0 post-card highlight-new" id="post-${postDto.id}">
            <div class="card-header bg-white border-0 d-flex align-items-center pt-3 pb-0">
                <a href="/creator/${postDto.authorId}" class="text-decoration-none">
                    <img src="${postDto.authorPic || '/img/default.png'}" class="rounded-circle me-2 object-fit-cover" width="40" height="40">
                </a>
                <div class="lh-sm">
                    <a href="/creator/${postDto.authorId}" class="fw-bold text-dark text-decoration-none">${postDto.authorName}</a>
                    <div class="text-muted small">Just now</div>
                </div>
            </div>
            <div class="card-body">
                ${postDto.text ? `<div class="card-text mb-2">${postDto.text}</div>` : ''}
                ${renderAttachmentsJS(postDto.attachments)}
            </div>
            <div class="card-footer bg-white border-top-0 d-flex justify-content-between pt-0 pb-3">
                 <button class="btn btn-sm btn-link text-muted"><i class="far fa-heart"></i> Like</button>
                 <button class="btn btn-sm btn-link text-muted"><i class="far fa-comment"></i> Comment</button>
            </div>
        </div>
    `;

    // Prepend to top of feed
    stream.insertBefore(div.firstElementChild, stream.firstChild);
}

function renderAttachmentsJS(attachments) {
    if (!attachments || attachments.length === 0) return '';

    // Simplified grid logic for JS render
    let html = '<div class="row g-1 mt-2 rounded overflow-hidden">';
    const colClass = attachments.length === 1 ? "col-12" : "col-6";

    attachments.forEach(media => {
        html += `<div class="${colClass}">`;
        if (media.mediaType === 3) {
            html += `<img src="${media.url}" class="img-fluid w-100 h-100 object-fit-cover" style="max-height:500px;">`;
        } else if (media.mediaType === 2) {
            html += `<video src="${media.url}" controls class="img-fluid w-100"></video>`;
        } else {
            html += `<audio src="${media.url}" controls class="w-100 mt-2"></audio>`;
        }
        html += `</div>`;
    });
    html += '</div>';
    return html;
}