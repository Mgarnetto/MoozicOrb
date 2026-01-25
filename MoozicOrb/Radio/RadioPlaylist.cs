using System.Collections.Generic;

namespace MoozicOrb.Radio
{
    public class RadioPlaylist
    {
        private readonly List<Track> _tracks = new();
        private int _currentIndex = 0;
        private readonly object _syncLock = new();

        public void Add(string filePath)
        {
            lock (_syncLock)
            {
                _tracks.Add(new Track { FilePath = filePath });
            }
        }

        public Track? Next()
        {
            lock (_syncLock)
            {
                if (_tracks.Count == 0) return null;

                if (_currentIndex >= _tracks.Count) _currentIndex = 0;
                var track = _tracks[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _tracks.Count;

                return track;
            }
        }
    }
}
