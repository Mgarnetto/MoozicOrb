// ---- GROUP MESSAGE ----
$(document).on("click", ".send-group", function () {
    const groupId = parseInt($(this).data("group-id"));
    const text = $(`#group-${groupId} .group-text`).val();

    if (!text) return;

    $.ajax({
        url: `/api/groups/${groupId}/messages`,
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({
            text: text
        }),
        success: function (res) {
            console.log("Group message created:", res.messageId);
            $(`#group-${groupId} .group-text`).val("");
        },
        error: function (err) {
            console.error("Group message failed", err);
        }
    });
});


// ---- DIRECT MESSAGE ----
$(document).on("click", ".send-direct", function () {
    const receiverId = parseInt($(this).data("receiver-id"));
    const text = $(`#dm-${receiverId} .direct-text`).val();

    if (!text) return;

    $.ajax({
        url: `/api/dm/${receiverId}`,
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({
            text: text
        }),
        success: function (res) {
            console.log("Direct message created:", res.messageId);
            $(`#dm-${receiverId} .direct-text`).val("");
        },
        error: function (err) {
            console.error("Direct message failed", err);
        }
    });
});
