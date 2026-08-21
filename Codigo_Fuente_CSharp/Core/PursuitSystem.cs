using System;
using SubwaySurfersAudioGame.Audio;

namespace SubwaySurfersAudioGame.Core
{
    public enum PursuitState
    {
        Safe,
        WarningClose,
        Captured
    }

    public class PursuitSystem
    {
        public PursuitState State { get; private set; } = PursuitState.Safe;
        public float DistanceBehindPlayer { get; private set; } = 20.0f; // Nominal safe distance
        public float WarningTimer { get; private set; } = 0.0f;
        public float StumbleGraceTimer { get; private set; } = 0.0f;
        public const float WarningDuration = 10.0f; // 10 seconds of close pursuit to evade
        public const float CloseDistance = 1.8f;
        public const float GracePeriod = 1.2f; // 1.2s buffer so multiple contacts in 1 stumble don't double count

        private BassSoundChannel? _proximitySoundInstance;

        public void Reset()
        {
            State = PursuitState.Safe;
            DistanceBehindPlayer = 20.0f;
            WarningTimer = 0.0f;
            StumbleGraceTimer = 0.0f;
            _proximitySoundInstance = null;
        }

        public bool TriggerStumble(SpatialAudioEngine audioEngine, float playerX, float playerY, float playerZ)
        {
            if (StumbleGraceTimer > 0.0f)
            {
                // In grace period right after first stumble: do not double-trigger
                return false;
            }

            if (State == PursuitState.WarningClose)
            {
                // Second stumble while inspector is in pursuit -> Fatal Capture!
                State = PursuitState.Captured;
                audioEngine.StopInstance(_proximitySoundInstance);
                _proximitySoundInstance = null;
                return true; // Fatal capture
            }
            else
            {
                // First stumble -> Enter Warning State
                State = PursuitState.WarningClose;
                DistanceBehindPlayer = CloseDistance;
                WarningTimer = WarningDuration;
                StumbleGraceTimer = GracePeriod;

                // Start guard and dog proximity audio loop behind the player at current Z
                if (_proximitySoundInstance == null)
                {
                    _proximitySoundInstance = audioEngine.PlayLoop3D(
                        AudioMap.Pursuit.GuardProximityLoop,
                        playerX,
                        playerY,
                        playerZ - CloseDistance,
                        gain: 1.0f
                    );
                }
                return false;
            }
        }

        public void TriggerFatalCrash(SpatialAudioEngine audioEngine)
        {
            State = PursuitState.Captured;
            audioEngine.StopInstance(_proximitySoundInstance);
            _proximitySoundInstance = null;
        }

        public void Update(float dt, float playerX, float playerY, float playerZ, SpatialAudioEngine audioEngine)
        {
            if (StumbleGraceTimer > 0.0f)
            {
                StumbleGraceTimer -= dt;
            }

            if (State == PursuitState.WarningClose)
            {
                WarningTimer -= dt;
                DistanceBehindPlayer = CloseDistance;

                if (_proximitySoundInstance != null)
                {
                    audioEngine.UpdateChannel3DPosition(_proximitySoundInstance, playerX, playerY, playerZ - CloseDistance);
                }

                if (WarningTimer <= 0.0f)
                {
                    // Successfully evaded after 10 seconds of clean running!
                    State = PursuitState.Safe;
                    DistanceBehindPlayer = 20.0f;
                    audioEngine.StopInstance(_proximitySoundInstance);
                    _proximitySoundInstance = null;
                }
            }
        }

        public void StopAudio(SpatialAudioEngine audioEngine)
        {
            if (_proximitySoundInstance != null)
            {
                audioEngine.StopInstance(_proximitySoundInstance);
                _proximitySoundInstance = null;
            }
        }
    }
}
