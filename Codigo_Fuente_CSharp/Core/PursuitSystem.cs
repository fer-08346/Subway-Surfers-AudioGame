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
        public const float WarningDuration = 10.0f; // 10 seconds of close pursuit
        public const float CloseDistance = 1.5f;

        private BassSoundChannel? _proximitySoundInstance;

        public void Reset()
        {
            State = PursuitState.Safe;
            DistanceBehindPlayer = 20.0f;
            WarningTimer = 0.0f;
            _proximitySoundInstance = null;
        }

        public bool TriggerStumble(SpatialAudioEngine audioEngine)
        {
            if (State == PursuitState.WarningClose)
            {
                // Second stumble while inspector is close -> Fatal Capture!
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

                // Start guard and dog proximity audio loop behind the player
                if (_proximitySoundInstance == null)
                {
                    _proximitySoundInstance = audioEngine.PlayLoop3D(
                        "audioClip_GuardProximity",
                        0.0f,
                        0.0f,
                        -CloseDistance,
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

        public void Update(float dt, float playerZ, SpatialAudioEngine audioEngine)
        {
            if (State == PursuitState.WarningClose)
            {
                WarningTimer -= dt;
                DistanceBehindPlayer = CloseDistance;

                if (_proximitySoundInstance != null)
                {
                    audioEngine.UpdateChannel3DPosition(_proximitySoundInstance, audioEngine.ListenerX, audioEngine.ListenerY, playerZ - CloseDistance);
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
