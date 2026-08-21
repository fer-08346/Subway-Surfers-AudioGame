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
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;
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
        public static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string gameFolder = Path.Combine(appData, "SubwaySurfersAudioGame");
            if (!Directory.Exists(gameFolder))
            {
                Directory.CreateDirectory(gameFolder);
            }
            return Path.Combine(gameFolder, "config.json");
        }

        public static GameSettingsData Load()
        {
            try
            {
                string appDataPath = GetConfigPath();
                string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

                // Auto-Migration: If no config exists in AppData yet, but an old config exists in the game folder,
                // automatically migrate it to AppData so the user preserves all their coins and upgrades seamlessly!
                if (!File.Exists(appDataPath) && File.Exists(legacyPath))
                {
                    try
                    {
                        File.Copy(legacyPath, appDataPath, overwrite: true);
                    }
                    catch { }
                }

                if (File.Exists(appDataPath))
                {
                    string json = File.ReadAllText(appDataPath);
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
                    Difficulty = engine.Difficulty,
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
