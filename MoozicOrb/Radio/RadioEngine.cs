using System;
using System.IO;

namespace MoozicOrb.Radio
{
    public static class RadioEngine
    {
        public static RadioBroadcaster Broadcaster;
        public static RadioWebRtcSession WebRtc;

        public static void Start()
        {
            if (Broadcaster != null) return;

            // Load your MP3 track
            var basePath = AppContext.BaseDirectory;
            var trackPath = Path.Combine(basePath, "Audio", "WePaid.mp3");

            if (!File.Exists(trackPath))
                throw new FileNotFoundException("Audio file not found", trackPath);

            // Playlist setup
            var playlist = new RadioPlaylist();
            playlist.Add(trackPath);

            // Broadcaster
            //Broadcaster = new RadioBroadcaster(playlist);
            Broadcaster = new RadioBroadcaster();
            Broadcaster.Start();

            // WebRTC session
            WebRtc = new RadioWebRtcSession(Broadcaster);

            Console.WriteLine("🎵 Radio engine started");
        }
    }
}

