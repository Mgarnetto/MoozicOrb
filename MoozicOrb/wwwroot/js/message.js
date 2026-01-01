const GROUP_ID = 9;
let lastMessageId = 0;

const messageconn = new signalR.HubConnectionBuilder()
    .withUrl("/MessageHub")
    .withAutomaticReconnect()
    .build();

// -----------------------------
// START CONNECTION
// -----------------------------
messageconn.start()
    .then(() => {
        console.log("SignalR connected");

        // Join SignalR group
        messageconn.invoke("JoinGroup", GROUP_ID);

        // Pull all existing messages
        loadGroupMessages();
    })
    .catch(console.error);

// -----------------------------
// SIGNALR NOTIFY → PULL SINGLE
// -----------------------------
messageconn.on("OnGroupMessage", data => {
    if (data.groupId !== GROUP_ID) return;

    if (data.messageId <= lastMessageId) return;

    fetch(`/api/groups/${GROUP_ID}/messages/${data.messageId}`)
        .then(r => r.json())
        .then(m => {
            appendMessage(m);
            lastMessageId = m.messageId;
        });
});

// -----------------------------
// LOAD ALL (ON CONNECT)
// -----------------------------
function loadGroupMessages() {
    fetch(`/api/groups/${GROUP_ID}/messages`)
        .then(r => r.json())
        .then(messages => {
            const container = $(`#group-${GROUP_ID} .messages`);
            container.empty();

            messages.forEach(m => {
                appendMessage(m);
                lastMessageId = Math.max(lastMessageId, m.messageId);
            });

            container.scrollTop(container[0].scrollHeight);
        });
}

// -----------------------------
// SEND GROUP MESSAGE (ONLY ONE)
// -----------------------------
$(document).on("click", ".send-group", function () {
    const groupId = $(this).data("group-id");
    const input = $(`#group-${groupId} .group-text`);
    const text = input.val();

    if (!text) return;

    fetch(`/api/groups/${groupId}/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text })
    })
        .then(r => {
            if (!r.ok) throw "POST failed";
            input.val("");
        })
        .catch(console.error);
});

// -----------------------------
// RENDER
// -----------------------------
function appendMessage(m) {
    $(`#group-${GROUP_ID} .messages`).append(`
        <div class="mb-1">
            <strong>${m.senderName ?? m.senderId}</strong>:
            ${m.text}
            <span class="text-muted small ms-2">
                ${m.timestamp}
            </span>
        </div>
    `);
}

