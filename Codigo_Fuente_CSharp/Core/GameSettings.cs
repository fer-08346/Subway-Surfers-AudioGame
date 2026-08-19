using System;
using System.IO;
using System.Text.Json;
using SubwaySurfersAudioGame.Accessibility;
using SubwaySurfersAudioGame.Audio;

namespace SubwaySurfersAudioGame.Core
{
    public class GameSettingsData
    {
        // Audio & Accessibility Settings
        public float MusicVolume { get; set; } = 0.50f;
        public float SfxVolume { get; set; } = 0.75f;
        public SpeechMode SpeechMode { get; set; } = SpeechMode.Auto;
        public float HighScore { get; set; } = 0.0f;

        // Inventory & Economy
        public int TotalCoins { get; set; } = 1000;
        public int HoverboardCount { get; set; } = 3;
        public int HeadstartCount { get; set; } = 1;

        // Power-up Upgrades
        public int MagnetLevel { get; set; } = 1;
        public int JetpackLevel { get; set; } = 1;
        public int SuperSneakersLevel { get; set; } = 1;
        public int MultiplierLevel { get; set; } = 1;

        // Daily Word Hunt
        public string DailyWord { get; set; } = "SURFERS";
        public bool[] DailyLettersCollected { get; set; } = new bool[7];
        public bool DailyWordCompleted { get; set; } = false;
    }

    public static class GameSettings
    {
        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        }

        public static GameSettingsData Load()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<GameSettingsData>(json);
                    if (data != null) return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameSettings] Error loading config: {ex.Message}");
            }
            return new GameSettingsData();
        }

        public static void Save(GameEngine engine)
        {
            try
            {
                var inv = engine.Inventory;
                var data = new GameSettingsData
                {
                    MusicVolume = engine.Music.MasterMusicVolume,
                    SfxVolume = engine.AudioEngine.MasterSfxVolume,
                    SpeechMode = engine.Accessibility.Mode,
                    HighScore = engine.HighScore,

                    TotalCoins = inv.TotalCoins,
                    HoverboardCount = inv.HoverboardCount,
                    HeadstartCount = inv.HeadstartCount,

                    MagnetLevel = inv.MagnetLevel,
                    JetpackLevel = inv.JetpackLevel,
                    SuperSneakersLevel = inv.SuperSneakersLevel,
                    MultiplierLevel = inv.MultiplierLevel,

                    DailyWord = inv.DailyWord,
                    DailyLettersCollected = inv.DailyLettersCollected,
                    DailyWordCompleted = inv.DailyWordCompleted
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetConfigPath(), json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameSettings] Error saving config: {ex.Message}");
            }
        }
    }
}
