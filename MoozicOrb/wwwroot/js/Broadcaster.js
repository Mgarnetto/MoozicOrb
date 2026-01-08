const pc = new RTCPeerConnection();
let streamId = "radio-1";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/StreamHub")
    .withAutomaticReconnect()
    .build();

navigator.mediaDevices.getUserMedia({ audio: true })
    .then(stream => {
        stream.getTracks().forEach(track => pc.addTrack(track, stream));
    });

pc.onicecandidate = e => {
    if (e.candidate) {
        connection.invoke("SendIceCandidate", streamId, e.candidate);
    }
};

connection.on("ReceiveAnswer", async answer => {
    await pc.setRemoteDescription(answer);
});

connection.start().then(async () => {
    await connection.invoke("JoinStream", streamId, "broadcaster");

    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);

    await connection.invoke("SendOffer", streamId, offer);
});
