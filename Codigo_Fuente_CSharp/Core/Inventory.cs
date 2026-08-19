using System;

namespace SubwaySurfersAudioGame.Core
{
    public class Inventory
    {
        // Consumables
        public int HoverboardCount { get; set; } = 3; // Start with 3 free hoverboards
        public int HeadstartCount { get; set; } = 1;   // Start with 1 free headstart

        // Power-up Upgrade Levels (1 to 5)
        public int MagnetLevel { get; set; } = 1;
        public int JetpackLevel { get; set; } = 1;
        public int SuperSneakersLevel { get; set; } = 1;
        public int MultiplierLevel { get; set; } = 1;

        // Accumulated Economy
        public int TotalCoins { get; set; } = 1000; // Starting bonus for new players

        // Daily Word Hunt (e.g. "SURFERS")
        public string DailyWord { get; set; } = "SURFERS";
        public bool[] DailyLettersCollected { get; set; } = new bool[7];
        public bool DailyWordCompleted { get; set; } = false;

        public float GetMagnetDuration() => 10.0f + (MagnetLevel - 1) * 3.0f;        // 10s to 22s
        public float GetJetpackDuration() => 12.0f + (JetpackLevel - 1) * 3.0f;       // 12s to 24s
        public float GetSuperSneakersDuration() => 12.0f + (SuperSneakersLevel - 1) * 3.0f; // 12s to 24s
        public float GetMultiplierDuration() => 15.0f + (MultiplierLevel - 1) * 4.0f;  // 15s to 31s

        public bool CollectDailyLetter(char letter, out int letterIndex, out bool wordCompleted)
        {
            letterIndex = -1;
            wordCompleted = false;
            letter = char.ToUpper(letter);

            for (int i = 0; i < DailyWord.Length; i++)
            {
                if (DailyWord[i] == letter && !DailyLettersCollected[i])
                {
                    DailyLettersCollected[i] = true;
                    letterIndex = i;

                    // Check if entire word is complete
                    bool allDone = true;
                    for (int j = 0; j < DailyWord.Length; j++)
                    {
                        if (!DailyLettersCollected[j]) { allDone = false; break; }
                    }

                    if (allDone && !DailyWordCompleted)
                    {
                        DailyWordCompleted = true;
                        wordCompleted = true;
                        TotalCoins += 1500; // Big completion bonus!
                    }
                    return true;
                }
            }
            return false;
        }

        public string GetDailyWordProgressString()
        {
            char[] display = new char[DailyWord.Length];
            for (int i = 0; i < DailyWord.Length; i++)
            {
                display[i] = DailyLettersCollected[i] ? DailyWord[i] : '_';
            }
            return new string(display);
        }
    }
}
