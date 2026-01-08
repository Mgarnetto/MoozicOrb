const pc = new RTCPeerConnection();
let streamId = "radio-1";

const audio = document.createElement("audio");
audio.autoplay = true;

pc.ontrack = e => {
    audio.srcObject = e.streams[0];
};

pc.onicecandidate = e => {
    if (e.candidate) {
        connection.invoke("SendIceCandidate", streamId, e.candidate);
    }
};

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/StreamHub")
    .withAutomaticReconnect()
    .build();

connection.on("BroadcasterReady", async () => {
    console.log("Broadcaster ready");
});

connection.on("ReceiveOffer", async offer => {
    await pc.setRemoteDescription(offer);
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    await connection.invoke("SendAnswer", streamId, answer);
});

connection.on("ReceiveIceCandidate", async candidate => {
    await pc.addIceCandidate(candidate);
});

connection.start().then(() => {
    connection.invoke("JoinStream", streamId, "listener");
});
