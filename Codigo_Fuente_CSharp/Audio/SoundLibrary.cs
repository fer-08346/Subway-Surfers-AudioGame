using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;

namespace SubwaySurfersAudioGame.Audio
{
    public class SoundLibrary : IDisposable
    {
        private readonly Dictionary<string, int> _sounds3D = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sounds2D = new(StringComparer.OrdinalIgnoreCase);
        public string SfxDirectory { get; private set; }

        public SoundLibrary(string sfxDirectory)
        {
            SfxDirectory = sfxDirectory;
        }

        public void LoadAllSounds()
        {
            if (!Directory.Exists(SfxDirectory))
            {
                Console.WriteLine($"[SoundLibrary] SFX Directory not found: {SfxDirectory}");
                return;
            }

            string[] wavFiles = Directory.GetFiles(SfxDirectory, "*.wav", SearchOption.AllDirectories);
            Console.WriteLine($"[SoundLibrary] Preloading {wavFiles.Length} sound effects with BASS...");

            foreach (var filePath in wavFiles)
            {
                string soundName = Path.GetFileNameWithoutExtension(filePath);
                try
                {
                    int handle2D = Bass.SampleLoad(filePath, 0, 0, 16, BassFlags.Default);
                    int handle3D = Bass.SampleLoad(filePath, 0, 0, 16, BassFlags.Bass3D | BassFlags.MuteMax);

                    if (handle3D == 0) handle3D = handle2D;
                    if (handle2D == 0) handle2D = handle3D;

                    if (handle3D != 0) _sounds3D[soundName] = handle3D;
                    if (handle2D != 0) _sounds2D[soundName] = handle2D;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundLibrary] Error loading {soundName}: {ex.Message}");
                }
            }

            Console.WriteLine($"[SoundLibrary] Loaded {_sounds2D.Count} BASS audio samples.");
        }

        public int GetSample3D(string soundName)
        {
            return _sounds3D.TryGetValue(soundName, out int handle) ? handle : 0;
        }

        public int GetSample2D(string soundName)
        {
            return _sounds2D.TryGetValue(soundName, out int handle) ? handle : 0;
        }

        public void Dispose()
        {
            foreach (var handle in _sounds3D.Values) Bass.SampleFree(handle);
            foreach (var handle in _sounds2D.Values) Bass.SampleFree(handle);
            _sounds3D.Clear();
            _sounds2D.Clear();
        }
    }
}
