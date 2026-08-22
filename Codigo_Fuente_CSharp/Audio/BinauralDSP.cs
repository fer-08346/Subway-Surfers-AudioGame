using System;

namespace SubwaySurfersAudioGame.Audio
{
    public static class BinauralDSP
    {
        public const float SpeedOfSound = 343.0f; // m/s
        public const float MinAudibleDistance = 2.0f; // d_min in meters
        public const float MaxAudibleDistance = 50.0f; // d_max in meters
        public const float FalloffFactor = 1.2f; // k factor

        /// <summary>
        /// Calculates Azimuth angle in degrees from player to sound source.
        /// Front = 0 deg, Right = +90 deg, Left = -90 deg, Behind = 180 deg.
        /// </summary>
        public static float CalculateAzimuth(float srcX, float srcZ, float listenerX, float listenerZ)
        {
            float dx = srcX - listenerX;
            float dz = srcZ - listenerZ;
            float rad = MathF.Atan2(dx, dz);
            return rad * (180.0f / MathF.PI);
        }

        /// <summary>
        /// Calculates Distance in meters from listener to source.
        /// </summary>
        public static float CalculateDistance(float srcX, float srcY, float srcZ, float listenerX, float listenerY, float listenerZ)
        {
            float dx = srcX - listenerX;
            float dy = srcY - listenerY;
            float dz = srcZ - listenerZ;
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Logarithmic Inverse Distance Attenuation as formulated in PDF formula (1):
        /// A(d) = 1.0 (d <= d_min), (d_min / (d_min + k*(d - d_min))) (d_min < d <= d_max), 0.0 (d > d_max)
        /// </summary>
        public static float CalculateDistanceAttenuation(float distance)
        {
            if (distance <= MinAudibleDistance) return 1.0f;
            if (distance >= MaxAudibleDistance) return 0.0f;

            float att = MinAudibleDistance / (MinAudibleDistance + FalloffFactor * (distance - MinAudibleDistance));
            return Math.Clamp(att, 0.0f, 1.0f);
        }

        /// <summary>
        /// Calculates Left and Right channel volumes and ITD delay samples for binaural spatialization.
        /// </summary>
        public static (float leftVol, float rightVol, int itdDelaySamples) CalculateBinauralGains(
            float azimuthDeg, float elevationDeg, float distance, int sampleRate = 44100)
        {
            float baseVol = CalculateDistanceAttenuation(distance);
            if (baseVol <= 0.0001f) return (0f, 0f, 0);

            // Normalize azimuth to [-180, 180]
            while (azimuthDeg > 180f) azimuthDeg -= 360f;
            while (azimuthDeg < -180f) azimuthDeg += 360f;

            float rad = azimuthDeg * (MathF.PI / 180.0f);
            float pan = MathF.Sin(rad); // -1.0 (pure left) to +1.0 (pure right)

            // Equal power panning curve
            float angle = (pan + 1.0f) * (MathF.PI / 4.0f); // 0 to PI/2
            float leftGain = MathF.Cos(angle);
            float rightGain = MathF.Sin(angle);

            // Pinna / Head Shadow filtering for sounds behind listener (|azimuth| > 90 deg)
            if (MathF.Abs(azimuthDeg) > 90.0f)
            {
                float rearFactor = (MathF.Abs(azimuthDeg) - 90.0f) / 90.0f;
                float rearAttenuation = 1.0f - (rearFactor * 0.25f); // subtle rear dampening
                leftGain *= rearAttenuation;
                rightGain *= rearAttenuation;
            }

            // ITD (Interaural Time Difference): maximum ~0.65 ms delay
            float maxDelaySec = 0.00065f;
            float timeDelaySec = MathF.Abs(pan) * maxDelaySec;
            int itdSamples = (int)(timeDelaySec * sampleRate);

            return (leftGain * baseVol, rightGain * baseVol, itdSamples);
        }

        /// <summary>
        /// Classical Doppler frequency shift calculation: f' = f * ((c + v_listener) / (c - v_source))
        /// </summary>
        public static float CalculateDopplerRatio(float listenerVelocityZ, float sourceVelocityZ)
        {
            float numerator = SpeedOfSound + listenerVelocityZ;
            float denominator = SpeedOfSound - sourceVelocityZ;
            if (MathF.Abs(denominator) < 0.1f) denominator = 0.1f;
            float ratio = numerator / denominator;
            return Math.Clamp(ratio, 0.5f, 2.0f);
        }

        /// <summary>
        /// Dolby Atmos object-based audio: minimum 3D distance (acoustic size) of a sound source.
        /// Point sources (coins, footsteps) are tight; volumetric bodies (long trains) keep their
        /// full presence across a wider radius, behaving like a large acoustic object in the scene.
        /// </summary>
        public static float GetObjectMinDistance(bool isVolumetric) => isVolumetric ? 6.0f : 2.0f;

        /// <summary>
        /// Dolby Atmos object-based audio: extra distance-dependent volume attenuation modeling
        /// physical air absorption. Below 14 m the air is transparent (factor 1.0); beyond that the
        /// brilliance of the object fades exponentially with distance, just like real-world propagation.
        /// </summary>
        public static float CalculateAirAbsorptionGain(float distance)
        {
            if (distance <= 14.0f) return 1.0f;
            float gain = MathF.Exp(-(distance - 14.0f) / 90.0f);
            return Math.Clamp(gain, 0.55f, 1.0f);
        }

        /// <summary>
        /// Dolby Atmos object-based audio: subtle pitch downshift that emulates the high-frequency
        /// muffling produced by air absorption on distant objects when no true low-pass filter is
        /// available (it is used as a safe fallback; the engine prefers a real DSP/BQF low-pass).
        /// </summary>
        public static float CalculateAirAbsorptionPitch(float distance)
        {
            if (distance <= 14.0f) return 1.0f;
            float pitch = 1.0f - Math.Min(0.10f, (distance - 14.0f) / 360.0f);
            return Math.Clamp(pitch, 0.9f, 1.0f);
        }

        /// <summary>
        /// Dolby Atmos object-based audio: real low-pass cutoff frequency (Hz) modeling the high-frequency
        /// roll-off caused by air absorption. Objects nearer than 14 m keep full brilliance (20 kHz);
        /// distant objects fade their highs exponentially, like real-world propagation.
        /// </summary>
        public static float CalculateAirAbsorptionCutoff(float distance)
        {
            if (distance <= 14.0f) return 20000.0f;
            float cutoff = 20000.0f * MathF.Exp(-(distance - 14.0f) / 26.0f);
            return Math.Clamp(cutoff, 1200.0f, 20000.0f);
        }

        /// <summary>
        /// Dolby Atmos object-based audio: elevation tilt applied to the effective gain of a source.
        /// The human auditory system perceives objects clearly above or below the listener as slightly
        /// more diffuse, so we apply a gentle spectral/elevation-aware attenuation for vertical offset.
        /// </summary>
        public static float CalculateElevationTilt(float sourceY, float listenerY)
        {
            float elevationDiff = MathF.Abs(sourceY - listenerY);
            if (elevationDiff <= 1.0f) return 1.0f;
            float tilt = 1.0f - MathF.Min(0.30f, (elevationDiff - 1.0f) * 0.05f);
            return Math.Clamp(tilt, 0.7f, 1.0f);
        }

        /// <summary>
        /// Dolby Atmos object-based audio: combined spatial gain of an object (air absorption + elevation).
        /// </summary>
        public static float CalculateObjectSpatialGain(float distance, float sourceY, float listenerY)
        {
            return CalculateAirAbsorptionGain(distance) * CalculateElevationTilt(sourceY, listenerY);
        }

        /// <summary>
        /// Butterworth Low-Pass filter coefficient calculator.
        /// </summary>
        public static (float a0, float a1, float a2, float b1, float b2) GetLowPassCoefficients(float cutoffHz, int sampleRate = 44100)
        {
            cutoffHz = Math.Clamp(cutoffHz, 200.0f, sampleRate * 0.45f);
            float ita = 1.0f / MathF.Tan(MathF.PI * cutoffHz / sampleRate);
            float q = 0.70710678f; // Butterworth Q
            float b0 = 1.0f / (1.0f + q * ita + ita * ita);
            float b1 = 2.0f * b0;
            float b2 = b0;
            float a1 = 2.0f * (ita * ita - 1.0f) * b0;
            float a2 = -(1.0f - q * ita + ita * ita) * b0;
            return (b0, b1, b2, a1, a2);
        }
    }
}
