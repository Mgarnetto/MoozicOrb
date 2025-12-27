const messageconn = new signalR.HubConnectionBuilder()
    .withUrl("/MessageHub")
    .withAutomaticReconnect()
    .build();

messageconn.start().catch(console.error);

// ---- RECEIVE GROUP ----
messageconn.on("OnGroupMessage", m => {
    $(`#group-${m.groupId} .messages`)
        .append(render(m.senderId, m.text, m.timestamp));
});

// ---- RECEIVE DIRECT ----
messageconn.on("OnDirectMessage", m => {
    const chatUser =
        m.senderId === CURRENT_USER_ID
            ? m.receiverId
            : m.senderId;

    $(`#dm-${chatUser} .messages`)
        .append(render(m.senderId, m.text, m.timestamp));
});

// ---- SEND ----
$(document).on("click", ".send-group", function () {
    const groupId = parseInt($(this).data("group-id"));
    const text = $(`#group-${groupId} input`).val();

    messageconn.invoke("SendGroupMessage", groupId, text);
});

$(document).on("click", ".send-direct", function () {
    const receiverId = parseInt($(this).data("receiver-id"));
    const text = $(`#dm-${receiverId} input`).val();

    messageconn.invoke("SendDirectMessage", receiverId, text);
});

function sendDirect(userId) {
    const text = $(`#dm-${userId} input`).val();
    messageconn.invoke("SendDirectMessage", userId, text);
}

function render(sender, text, time) {
    return `<div><b>${sender}</b> ${time}: ${text}</div>`;
}