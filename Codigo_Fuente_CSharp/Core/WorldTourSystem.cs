using System;
using SubwaySurfersAudioGame.Accessibility;
using SubwaySurfersAudioGame.Audio;

namespace SubwaySurfersAudioGame.Core
{
    public class WorldTourSystem
    {
        public const float MilestoneDistance = 2000.0f; // Stage progression every 2,000 meters
        public int CurrentStageIndex { get; private set; } = 0;
        private float _nextMilestoneZ = MilestoneDistance;

        public void Reset()
        {
            CurrentStageIndex = 0;
            _nextMilestoneZ = MilestoneDistance;
        }

        public void Update(float playerZ, GameEngine engine)
        {
            if (playerZ >= _nextMilestoneZ)
            {
                _nextMilestoneZ += MilestoneDistance;
                AdvanceToNextCountry(playerZ, engine);
            }
        }

        public void AdvanceStageManually(GameEngine engine)
        {
            AdvanceToNextCountry(engine.Player.Z, engine);
        }

        public void PreviousStageManually(GameEngine engine)
        {
            int trackCount = engine.Music.Tracks.Count;
            if (trackCount == 0) return;
            CurrentStageIndex = (CurrentStageIndex - 1 + trackCount) % trackCount;
            var track = engine.Music.Tracks[CurrentStageIndex];
            engine.Music.PlayTrack(CurrentStageIndex);
            engine.AudioEngine.Play2D(AudioMap.UI.TopRunCelebration, gain: 0.85f);
            engine.Accessibility.Speak($"Etapa de prueba: ¡Bienvenidos a {track.CityName}!", interrupt: true);
        }

        private void AdvanceToNextCountry(float currentDistance, GameEngine engine)
        {
            CurrentStageIndex++;
            int trackCount = engine.Music.Tracks.Count;
            if (trackCount == 0) return;

            int trackIndex = CurrentStageIndex % trackCount;
            var track = engine.Music.Tracks[trackIndex];

            // Change background music to the new destination
            engine.Music.PlayTrack(trackIndex);

            // Audio celebration cue
            engine.AudioEngine.Play2D(AudioMap.UI.TopRunCelebration, gain: 0.85f);

            // Spoken announcement
            string announcement = $"¡Meta de {(int)currentDistance} metros alcanzada! ¡Bienvenidos a {track.CityName}!";
            engine.Accessibility.Speak(announcement, interrupt: false);
        }

        public string GetCurrentCountryName(MusicManager music)
        {
            if (music.Tracks.Count == 0) return "Subway Clásico (2012)";
            int idx = CurrentStageIndex % music.Tracks.Count;
            return music.Tracks[idx].CityName;
        }
    }
}
