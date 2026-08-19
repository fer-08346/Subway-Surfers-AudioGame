using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;

namespace SubwaySurfersAudioGame.Audio
{
    public class MusicTrack
    {
        public string Title { get; set; } = "";
        public string CityName { get; set; } = "";
        public string YearOrEra { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public class MusicManager : IDisposable
    {
        private readonly List<MusicTrack> _tracks = new();
        private int _currentStreamHandle = 0;

        public float MasterMusicVolume { get; set; } = 0.50f;
        public int CurrentTrackIndex { get; private set; } = 0;

        public IReadOnlyList<MusicTrack> Tracks => _tracks;
        public MusicTrack? CurrentTrack => (_tracks.Count > 0 && CurrentTrackIndex >= 0 && CurrentTrackIndex < _tracks.Count) ? _tracks[CurrentTrackIndex] : null;

        public MusicManager(string musicDirectory)
        {
            ScanAndOrganizeTracks(musicDirectory);
        }

        private void ScanAndOrganizeTracks(string musicDirectory)
        {
            if (!Directory.Exists(musicDirectory))
            {
                Console.WriteLine($"[MusicManager] Music directory not found: {musicDirectory}");
                return;
            }

            string[] mp3Files = Directory.GetFiles(musicDirectory, "*.mp3", SearchOption.TopDirectoryOnly);

            string[] preferredOrder = new string[]
            {
                "Main Theme",
                "New York",
                "Rio",
                "Tokyo",
                "Miami v3",
                "Paris",
                "Beijing",
                "London",
                "Mumbai",
                "Cairo",
                "Chicago v3",
                "Hawaii v4",
                "Venice Beach",
                "Las Vegas",
                "Edinburgh v3 Final",
                "St Petersburg",
                "HongKong v2",
                "New Orleans",
                "Bali v2",
                "Barcelona v2",
                "Berlin",
                "Buenos Aires v4",
                "Copenhagen",
                "Havana",
                "Houston",
                "Lunar New Year",
                "Oxford",
                "Peru 2020 FINAL",
                "Seattle v7 FINAL 2",
                "Space Station v2 FINAL",
                "Vancouver",
                "Mexico v5",
                "Mexico Halloween",
                "Xmas",
                "The North Pole"
            };

            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in preferredOrder)
            {
                foreach (var file in mp3Files)
                {
                    string baseName = Path.GetFileNameWithoutExtension(file);
                    if (baseName.Equals(name, StringComparison.OrdinalIgnoreCase) && !added.Contains(file))
                    {
                        _tracks.Add(new MusicTrack
                        {
                            Title = baseName,
                            CityName = CleanTrackName(baseName),
                            FilePath = file
                        });
                        added.Add(file);
                        break;
                    }
                }
            }

            foreach (var file in mp3Files)
            {
                if (!added.Contains(file))
                {
                    string baseName = Path.GetFileNameWithoutExtension(file);
                    _tracks.Add(new MusicTrack
                    {
                        Title = baseName,
                        CityName = CleanTrackName(baseName),
                        FilePath = file
                    });
                    added.Add(file);
                }
            }

            Console.WriteLine($"[MusicManager] Indexed {_tracks.Count} music tracks in chronological World Tour order.");
        }

        private static string CleanTrackName(string raw)
        {
            string clean = raw.Replace(" v2", "").Replace(" v3", "").Replace(" v4", "").Replace(" v5", "").Replace(" v7", "")
                              .Replace(" FINAL", "").Replace(" Final", "").Replace(" 2019", "").Replace(" 2020", "")
                              .Replace(" NFLPA", "").Replace(" 2", "");
            return clean;
        }

        public void PlayTrack(int index)
        {
            if (_tracks.Count == 0) return;
            CurrentTrackIndex = Math.Clamp(index, 0, _tracks.Count - 1);
            PlayCurrent();
        }

        public void NextTrack()
        {
            if (_tracks.Count == 0) return;
            CurrentTrackIndex = (CurrentTrackIndex + 1) % _tracks.Count;
            PlayCurrent();
        }

        public void PreviousTrack()
        {
            if (_tracks.Count == 0) return;
            CurrentTrackIndex = (CurrentTrackIndex - 1 + _tracks.Count) % _tracks.Count;
            PlayCurrent();
        }

        public void PlayCurrent()
        {
            Stop();
            if (CurrentTrack == null) return;

            try
            {
                _currentStreamHandle = Bass.CreateStream(CurrentTrack.FilePath, 0, 0, BassFlags.Loop | BassFlags.Prescan);
                if (_currentStreamHandle != 0)
                {
                    UpdateVolume();
                    Bass.ChannelPlay(_currentStreamHandle, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MusicManager] Error playing music: {ex.Message}");
            }
        }

        public void UpdateVolume()
        {
            if (_currentStreamHandle != 0)
            {
                float vol = Math.Clamp(MasterMusicVolume, 0.0f, 1.0f);
                Bass.ChannelSetAttribute(_currentStreamHandle, ChannelAttribute.Volume, vol);
            }
        }

        public void Pause()
        {
            if (_currentStreamHandle != 0) Bass.ChannelPause(_currentStreamHandle);
        }

        public void Resume()
        {
            if (_currentStreamHandle != 0) Bass.ChannelPlay(_currentStreamHandle, false);
        }

        public void Stop()
        {
            if (_currentStreamHandle != 0)
            {
                Bass.ChannelStop(_currentStreamHandle);
                Bass.StreamFree(_currentStreamHandle);
                _currentStreamHandle = 0;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
