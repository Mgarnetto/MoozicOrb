// Global array to hold media IDs while drafting a post
let currentAttachments = [];

document.addEventListener("DOMContentLoaded", function () {
    // Initialize SignalR listener for Posts if not already done in main.js
    if (typeof connection !== 'undefined') {
        connection.on("ReceivePost", function (message) {
            // message.data is the PostDto
            // message.targetGroup is the context (e.g., "user_105")

            // Check if this post belongs in the feed we are currently looking at
            const feedContainer = document.getElementById("feed-stream-container");
            const currentContext = document.querySelector(".feed-wrapper")?.dataset.contextId;

            // Basic check: If we are on the page where the post happened, prepend it
            // (In a real app, you might want to fetch the HTML partial instead of raw JSON)
            if (feedContainer && message.targetGroup.includes(currentContext)) {
                renderNewPost(message.data);
            }
        });
    }
});

// 1. HANDLE FILE SELECTION
function handleFileSelect(input) {
    const container = document.getElementById('media-preview-area');
    const files = input.files;

    if (files.length === 0) return;

    Array.from(files).forEach(file => {
        uploadSingleFile(file, container);
    });
}

// 2. UPLOAD FILE (AJAX)
async function uploadSingleFile(file, container) {
    // A. Show Loading Spinner
    const previewId = "temp-" + Date.now();
    const previewDiv = document.createElement('div');
    previewDiv.id = previewId;
    previewDiv.className = "position-relative d-inline-block me-2";
    previewDiv.innerHTML = `
        <div class="ratio ratio-1x1 rounded border bg-light d-flex align-items-center justify-content-center" style="width:80px;">
            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
        </div>`;
    container.appendChild(previewDiv);

    // B. Send to Server
    const formData = new FormData();
    formData.append("file", file);

    try {
        const response = await fetch('/api/upload', { method: 'POST', body: formData });

        if (!response.ok) throw new Error("Upload failed");

        const result = await response.json();
        // Expected JSON: { id: 505, type: 3, url: "..." }

        // C. Update Array
        currentAttachments.push({
            MediaId: result.id,
            MediaType: result.type
        });

        // D. Update UI (Replace spinner with thumbnail)
        let htmlContent = "";
        if (result.type === 3) { // Image
            htmlContent = `<img src="${result.url}" class="rounded border object-fit-cover" style="width:80px; height:80px;">`;
        } else { // Audio/Video placeholder
            htmlContent = `<div class="rounded border bg-secondary text-white d-flex align-items-center justify-content-center" style="width:80px; height:80px;"><i class="fas fa-file-video"></i></div>`;
        }

        previewDiv.innerHTML = `
            ${htmlContent}
            <button onclick="removeAttachment('${previewId}', ${result.id})" class="btn btn-danger btn-sm position-absolute top-0 end-0 p-0 rounded-circle" style="width:20px;height:20px;line-height:1;">&times;</button>
        `;

    } catch (err) {
        console.error(err);
        previewDiv.remove();
        alert("Could not upload " + file.name);
    }
}

// 3. REMOVE ATTACHMENT
function removeAttachment(elementId, mediaId) {
    currentAttachments = currentAttachments.filter(x => x.MediaId !== mediaId);
    document.getElementById(elementId).remove();
}

// 4. SUBMIT POST
async function submitPost() {
    const txtInput = document.getElementById('post-text-input');
    const wrapper = document.querySelector('.feed-wrapper');

    if (!txtInput || !wrapper) return;

    const payload = {
        ContextType: wrapper.dataset.contextType,
        ContextId: wrapper.dataset.contextId,
        Type: "standard",
        Title: null,
        Text: txtInput.value,
        ImageUrl: null,
        MediaAttachments: currentAttachments // The list of IDs we collected
    };

    if (!payload.Text.trim() && payload.MediaAttachments.length === 0) {
        return; // Don't post empty
    }

    try {
        const response = await fetch('/api/posts', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            // Clear inputs
            txtInput.value = "";
            document.getElementById('media-preview-area').innerHTML = "";
            currentAttachments = [];

            // Optional: Fetch the single HTML card for this new post and prepend it immediately
            // For now, we rely on SignalR or a reload.
            // window.location.reload(); 
        } else {
            alert("Failed to post. Please try again.");
        }
    } catch (err) {
        console.error(err);
    }
}

// Helper to render HTML from JSON (If you don't want to fetch partials)
function renderNewPost(post) {
    // In a perfect world, we fetch the PartialView string from the server.
    // For V1, a simple reload is often safer to ensure consistency.
    // Or, we rely on the PostHub to trigger a UI refresh function.
    window.location.reload();
}