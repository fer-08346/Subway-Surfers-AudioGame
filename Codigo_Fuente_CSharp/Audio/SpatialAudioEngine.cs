using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.DirectX8;
using ManagedBass.Fx;

namespace SubwaySurfersAudioGame.Audio
{
    public class BassSoundChannel
    {
        public int ChannelHandle { get; set; }
        public bool Is3D { get; set; } = true;
        public bool IsLooping { get; set; } = false;
        public float BaseGain { get; set; } = 1.0f;
        public float BaseFrequency { get; set; } = 44100.0f;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float VelZ { get; set; }

        // Dolby Atmos object-based audio metadata
        public bool IsVolumetric { get; set; } = false;
        public bool AirAbsorption { get; set; } = true;
        public float MinDistance { get; set; } = 2.0f;
        public int LowPassFx { get; set; } = 0;
    }

    /// <summary>
    /// A spatial audio object living in the 3D scene (Dolby Atmos paradigm): each sound is an
    /// independent object with a position, velocity, elevation and acoustic size rather than a
    /// flat pan. The engine keeps every active object registered so it can continuously refresh
    /// its air absorption and elevation treatment as the listener moves through the world.
    /// </summary>

    public class SpatialAudioEngine : IDisposable
    {
        private readonly SoundLibrary _soundLibrary;
        private readonly List<BassSoundChannel> _activeChannels = new();
        private readonly object _lock = new();

        public float ListenerX { get; set; } = 0.0f;
        public float ListenerY { get; set; } = 0.0f;
        public float ListenerZ { get; set; } = 0.0f;
        public float ListenerVelZ { get; set; } = 0.0f;

        public float MasterSfxVolume { get; set; } = 0.75f;

        // Dynamic Speed Wind
        private int _windStreamHandle = 0;
        private StreamProcedure? _windStreamProc;
        private double _b0, _b1, _b2, _b3, _b4, _b5, _b6;
        private readonly Random _windRng = new(42);

        // Tunnel Environmental Reverb FX
        private readonly List<int> _activeReverbFxHandles = new();
        public bool IsInTunnel { get; private set; } = false;

        // Dolby Atmos object-based air absorption: per-object low-pass state.
        // A real BASS DSP biquad low-pass is applied in C# (works with only bass.dll), and is
        // automatically upgraded to the native BASS_FX BQF low-pass if bass_fx.dll is present.
        private readonly Dictionary<int, LowPassState> _lpStates = new();
        private DSPProcedure? _dspProc;

        private struct LowPassState
        {
            public float YL;
            public float YR;
            public float Alpha;
            public int DspHandle;
            public int BqfHandle;
        }

        public SpatialAudioEngine(SoundLibrary soundLibrary)
        {
            _soundLibrary = soundLibrary;
        }

        public void Start()
        {
            // Initialize BASS with 3D capability
            if (!Bass.Init(-1, 44100, DeviceInitFlags.Default | DeviceInitFlags.Device3D))
            {
                Console.WriteLine($"[SpatialAudioEngine] BASS Init warning: {Bass.LastError}");
            }
            else
            {
                Console.WriteLine("[SpatialAudioEngine] BASS 3D Sound Engine initialized successfully.");
            }

            // Set 3D world factors: meters, logarithmic rolloff, Doppler enabled
            Bass.Set3DFactors(1.0f, 1.2f, 1.0f);

            InitSpeedWindStream();
        }

        private void InitSpeedWindStream()
        {
            try
            {
                _windStreamProc = new StreamProcedure(GenerateWindSamples);
                _windStreamHandle = Bass.CreateStream(44100, 2, BassFlags.Default, _windStreamProc, IntPtr.Zero);
                if (_windStreamHandle != 0)
                {
                    Bass.ChannelSetAttribute(_windStreamHandle, ChannelAttribute.Volume, 0.0f);
                    Bass.ChannelPlay(_windStreamHandle, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SpatialAudioEngine] Wind stream init error: {ex.Message}");
            }
        }

        private int GenerateWindSamples(int handle, IntPtr buffer, int length, IntPtr user)
        {
            int numShorts = length / sizeof(short);
            short[] tempBuf = new short[numShorts];

            for (int i = 0; i < numShorts / 2; i++)
            {
                double white = (_windRng.NextDouble() * 2.0 - 1.0);
                _b0 = 0.99886 * _b0 + white * 0.0555179;
                _b1 = 0.99332 * _b1 + white * 0.0750759;
                _b2 = 0.96900 * _b2 + white * 0.1538520;
                _b3 = 0.86650 * _b3 + white * 0.3104856;
                _b4 = 0.55000 * _b4 + white * 0.5329522;
                _b5 = -0.7616 * _b5 - white * 0.0168980;
                double pink = _b0 + _b1 + _b2 + _b3 + _b4 + _b5 + _b6 + white * 0.5362;
                _b6 = white * 0.115926;
                pink *= 0.12;

                short sampleVal = (short)Math.Clamp(pink * 32767.0, -32767, 32767);
                tempBuf[i * 2] = sampleVal;     // Left
                tempBuf[i * 2 + 1] = sampleVal; // Right
            }

            Marshal.Copy(tempBuf, 0, buffer, numShorts);
            return length;
        }

        public void StartSpeedWind()
        {
            if (_windStreamHandle != 0)
            {
                Bass.ChannelPlay(_windStreamHandle, false);
            }
        }

        public void UpdateSpeedWind(float speed)
        {
            if (_windStreamHandle != 0)
            {
                // Speed scales from 10 m/s (quiet) to 30 m/s (intense rush)
                float speedNorm = Math.Clamp((speed - 10.0f) / 20.0f, 0.0f, 1.0f);
                float targetVol = (0.03f + speedNorm * 0.32f) * MasterSfxVolume;
                Bass.ChannelSetAttribute(_windStreamHandle, ChannelAttribute.Volume, Math.Clamp(targetVol, 0.0f, 1.0f));

                float baseFreq = 44100.0f;
                float targetFreq = baseFreq * (0.85f + speedNorm * 0.45f);
                Bass.ChannelSetAttribute(_windStreamHandle, ChannelAttribute.Frequency, targetFreq);
            }
        }

        public void StopSpeedWind()
        {
            if (_windStreamHandle != 0)
            {
                Bass.ChannelSetAttribute(_windStreamHandle, ChannelAttribute.Volume, 0.0f);
            }
        }

        public void SetTunnelReverb(bool enabled)
        {
            if (IsInTunnel == enabled) return;
            IsInTunnel = enabled;

            lock (_lock)
            {
                if (enabled)
                {
                    // Apply DXReverb effect to all active 3D channels
                    foreach (var chan in _activeChannels)
                    {
                        ApplyReverbToChannel(chan.ChannelHandle);
                    }
                }
                else
                {
                    // Remove reverb effects
                    foreach (var fx in _activeReverbFxHandles)
                    {
                        Bass.ChannelRemoveFX(fx, fx);
                    }
                    _activeReverbFxHandles.Clear();
                }
            }
        }

        private void ApplyReverbToChannel(int channelHandle)
        {
            if (channelHandle == 0) return;
            try
            {
                int fx = Bass.ChannelSetFX(channelHandle, EffectType.DXReverb, 1);
                if (fx != 0)
                {
                    var revParam = new DXReverbParameters
                    {
                        fInGain = 0.0f,
                        fReverbMix = -4.0f,
                        fReverbTime = 2200.0f,
                        fHighFreqRTRatio = 0.7f
                    };
                    Bass.FXSetParameters(fx, revParam);
                    _activeReverbFxHandles.Add(fx);
                }
            }
            catch { }
        }

        public void UpdateListener(float x, float y, float z, float velZ)
        {
            ListenerX = x;
            ListenerY = y;
            ListenerZ = z;
            ListenerVelZ = velZ;

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            var front = new Vector3D(0, 0, 1.0f);
            var top = new Vector3D(0, 1.0f, 0);

            Bass.Set3DPosition(pos, vel, front, top);
            Bass.Apply3D();
        }

        public BassSoundChannel? Play3D(string soundName, float x, float y, float z, float velZ = 0f, float pitch = 1.0f, float gain = 1.0f, bool isVolumetric = false, bool airAbsorption = true)
        {
            int sample = _soundLibrary.GetSample3D(soundName);
            if (sample == 0) sample = _soundLibrary.GetSample2D(soundName);
            if (sample == 0) return null;

            int channel = Bass.SampleGetChannel(sample, false);
            if (channel == 0) return null;

            float effVol = gain * MasterSfxVolume;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(effVol, 0.0f, 1.0f));

            if (pitch != 1.0f && Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq0))
            {
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, baseFreq0 * pitch);
            }

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            float minDist = BinauralDSP.GetObjectMinDistance(isVolumetric);
            Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, minDist, 50.0f, 360, 360, 0);
            Bass.ChannelSet3DPosition(channel, pos, new Vector3D(0, 0, 0), vel);
            Bass.Apply3D();

            if (IsInTunnel)
            {
                ApplyReverbToChannel(channel);
            }

            Bass.ChannelPlay(channel, true);

            var sChan = new BassSoundChannel
            {
                ChannelHandle = channel,
                Is3D = true,
                IsLooping = false,
                BaseGain = gain,
                X = x,
                Y = y,
                Z = z,
                VelZ = velZ,
                IsVolumetric = isVolumetric,
                AirAbsorption = airAbsorption,
                MinDistance = minDist
            };

            if (Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq))
            {
                sChan.BaseFrequency = baseFreq;
            }

            if (airAbsorption) AttachAirAbsorption(channel, sChan);

            lock (_lock)
            {
                _activeChannels.Add(sChan);
            }
            return sChan;
        }

        public BassSoundChannel? PlayLoop3D(string soundName, float x, float y, float z, float velZ = 0f, float pitch = 1.0f, float gain = 1.0f, bool isVolumetric = false, bool airAbsorption = true)
        {
            int sample = _soundLibrary.GetSample3D(soundName);
            if (sample == 0) sample = _soundLibrary.GetSample2D(soundName);
            if (sample == 0) return null;

            int channel = Bass.SampleGetChannel(sample, false);
            if (channel == 0) return null;

            Bass.ChannelFlags(channel, BassFlags.Loop, BassFlags.Loop);

            float effVol = gain * MasterSfxVolume;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(effVol, 0.0f, 1.0f));

            if (pitch != 1.0f && Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq0))
            {
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, baseFreq0 * pitch);
            }

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            float minDist = BinauralDSP.GetObjectMinDistance(isVolumetric);
            Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, minDist, 50.0f, 360, 360, 0);
            Bass.ChannelSet3DPosition(channel, pos, new Vector3D(0, 0, 0), vel);
            Bass.Apply3D();

            if (IsInTunnel)
            {
                ApplyReverbToChannel(channel);
            }

            Bass.ChannelPlay(channel, true);

            var sChan = new BassSoundChannel
            {
                ChannelHandle = channel,
                Is3D = true,
                IsLooping = true,
                BaseGain = gain,
                X = x,
                Y = y,
                Z = z,
                VelZ = velZ,
                IsVolumetric = isVolumetric,
                AirAbsorption = airAbsorption,
                MinDistance = minDist
            };

            if (Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq))
            {
                sChan.BaseFrequency = baseFreq;
            }

            if (airAbsorption) AttachAirAbsorption(channel, sChan);

            lock (_lock)
            {
                _activeChannels.Add(sChan);
            }
            return sChan;
        }

        /// <summary>
        /// Attaches the Dolby Atmos air-absorption low-pass to a freshly created spatial object.
        /// Prefers the native BASS_FX BQF low-pass when bass_fx.dll is available; otherwise falls back
        /// to a real C# BASS DSP biquad low-pass (works with only bass.dll). The cutoff is then refreshed.
        /// </summary>
        private void AttachAirAbsorption(int channel, BassSoundChannel sChan)
        {
            int bqf = 0;
            try
            {
                bqf = Bass.ChannelSetFX(channel, EffectType.BQF, 0);
                if (bqf != 0)
                {
                    var p = new BQFParameters
                    {
                        lFilter = (int)BQFType.LowPass,
                        fCenter = 20000.0f,
                        fGain = 0.0f,
                        fBandwidth = 0.0f,
                        fQ = 0.707f,
                        fS = 0.0f,
                        lChannel = (FXChannelFlags)(-1)
                    };
                    Bass.FXSetParameters(bqf, p);
                }
            }
            catch
            {
                bqf = 0;
            }

            int dsp = 0;
            if (bqf == 0)
            {
                if (_dspProc == null) _dspProc = DspLowPass;
                dsp = Bass.ChannelSetDSP(channel, _dspProc, IntPtr.Zero, 0);
            }

            sChan.LowPassFx = bqf;
            _lpStates[channel] = new LowPassState
            {
                YL = 0.0f,
                YR = 0.0f,
                Alpha = 1.0f,
                DspHandle = dsp,
                BqfHandle = bqf
            };

            RefreshSpatialAir(sChan);
        }

        /// <summary>
        /// BASS DSP callback implementing a one-pole (RC) low-pass in C# over the 16-bit stereo buffer.
        /// The per-object cutoff (Alpha) is updated by RefreshSpatialAir based on air absorption.
        /// </summary>
        private void DspLowPass(int handle, int channel, IntPtr buffer, int length, IntPtr user)
        {
            if (!_lpStates.TryGetValue(channel, out var st) || st.DspHandle == 0) return;

            int frames = length / 4; // 16-bit stereo => 4 bytes per frame
            if (frames <= 0) return;

            float yl = st.YL;
            float yr = st.YR;
            float a = st.Alpha;

            for (int i = 0; i < frames; i++)
            {
                float fl = Marshal.ReadInt16(buffer, i * 4) / 32768.0f;
                float fr = Marshal.ReadInt16(buffer, i * 4 + 2) / 32768.0f;
                yl += a * (fl - yl);
                yr += a * (fr - yr);
                Marshal.WriteInt16(buffer, i * 4, (short)(yl * 32767.0f));
                Marshal.WriteInt16(buffer, i * 4 + 2, (short)(yr * 32767.0f));
            }

            st.YL = yl;
            st.YR = yr;
            _lpStates[channel] = st;
        }

        /// <summary>
        /// Refreshes the Dolby Atmos object treatment of a single spatial object: distance-dependent
        /// air absorption (gain + real low-pass cutoff) and elevation-aware tilt, computed against the
        /// live listener position. Safe to call every frame for moving or static objects.
        /// </summary>
        private void RefreshSpatialAir(BassSoundChannel sChan)
        {
            if (sChan.ChannelHandle == 0 || !sChan.Is3D || !sChan.AirAbsorption) return;

            float distance = BinauralDSP.CalculateDistance(sChan.X, sChan.Y, sChan.Z, ListenerX, ListenerY, ListenerZ);
            float spatialGain = BinauralDSP.CalculateObjectSpatialGain(distance, sChan.Y, ListenerY);
            float effVol = Math.Clamp(sChan.BaseGain * MasterSfxVolume * spatialGain, 0.0f, 1.0f);
            Bass.ChannelSetAttribute(sChan.ChannelHandle, ChannelAttribute.Volume, effVol);

            if (sChan.LowPassFx != 0)
            {
                // Native BASS_FX BQF low-pass: set the -3 dB cutoff frequency directly.
                try
                {
                    var p = new BQFParameters
                    {
                        lFilter = (int)BQFType.LowPass,
                        fCenter = BinauralDSP.CalculateAirAbsorptionCutoff(distance),
                        fGain = 0.0f,
                        fBandwidth = 0.0f,
                        fQ = 0.707f,
                        fS = 0.0f,
                        lChannel = (FXChannelFlags)(-1)
                    };
                    Bass.FXSetParameters(sChan.LowPassFx, p);
                }
                catch { }
            }
            else if (_lpStates.TryGetValue(sChan.ChannelHandle, out var st) && st.DspHandle != 0)
            {
                // C# DSP biquad low-pass: convert cutoff to a one-pole coefficient.
                float cutoff = BinauralDSP.CalculateAirAbsorptionCutoff(distance);
                float dt = 1.0f / 44100.0f;
                float rc = 1.0f / (2.0f * MathF.PI * cutoff);
                st.Alpha = dt / (rc + dt);
                _lpStates[sChan.ChannelHandle] = st;
            }
            else
            {
                // Last-resort fallback: subtle pitch downshift emulation.
                float airPitch = BinauralDSP.CalculateAirAbsorptionPitch(distance);
                Bass.ChannelSetAttribute(sChan.ChannelHandle, ChannelAttribute.Frequency, sChan.BaseFrequency * airPitch);
            }
        }

        /// <summary>
        /// Continuously refreshes air absorption and elevation treatment for every active 3D object,
        /// since the listener is always moving through the world even when a sound source is static.
        /// </summary>
        public void UpdateSpatialObjects()
        {
            lock (_lock)
            {
                foreach (var chan in _activeChannels)
                {
                    if (chan.Is3D && chan.AirAbsorption)
                    {
                        RefreshSpatialAir(chan);
                    }
                }
            }
        }

        public BassSoundChannel? Play2D(string soundName, float pan = 0.0f, float pitch = 1.0f, float gain = 1.0f)
        {
            int sample = _soundLibrary.GetSample2D(soundName);
            if (sample == 0) sample = _soundLibrary.GetSample3D(soundName);
            if (sample == 0) return null;

            int channel = Bass.SampleGetChannel(sample, false);
            if (channel == 0) return null;

            float effVol = gain * MasterSfxVolume;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(effVol, 0.0f, 1.0f));
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, Math.Clamp(pan, -1.0f, 1.0f));

            if (pitch != 1.0f)
            {
                if (Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq))
                {
                    Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, baseFreq * pitch);
                }
            }

            Bass.ChannelPlay(channel, true);

            var sChan = new BassSoundChannel
            {
                ChannelHandle = channel,
                Is3D = false,
                IsLooping = false,
                BaseGain = gain
            };

            lock (_lock)
            {
                _activeChannels.Add(sChan);
            }
            return sChan;
        }

        public void UpdateChannel3DPosition(BassSoundChannel? sChan, float x, float y, float z, float velZ = 0f)
        {
            if (sChan == null || sChan.ChannelHandle == 0) return;
            sChan.X = x;
            sChan.Y = y;
            sChan.Z = z;
            sChan.VelZ = velZ;

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            Bass.ChannelSet3DPosition(sChan.ChannelHandle, pos, new Vector3D(0, 0, 0), vel);
            Bass.Apply3D();

            if (sChan.AirAbsorption) RefreshSpatialAir(sChan);
        }

        public void StopInstance(BassSoundChannel? instance)
        {
            if (instance == null || instance.ChannelHandle == 0) return;
            RemoveAirAbsorption(instance.ChannelHandle);
            Bass.ChannelStop(instance.ChannelHandle);
            lock (_lock)
            {
                _activeChannels.Remove(instance);
            }
        }

        private void RemoveAirAbsorption(int channelHandle)
        {
            if (_lpStates.TryGetValue(channelHandle, out var st))
            {
                if (st.DspHandle != 0)
                {
                    try { Bass.ChannelRemoveDSP(channelHandle, st.DspHandle); } catch { }
                }
                if (st.BqfHandle != 0)
                {
                    try { Bass.ChannelRemoveFX(channelHandle, st.BqfHandle); } catch { }
                }
                _lpStates.Remove(channelHandle);
            }
        }

        public void StopAll()
        {
            StopSpeedWind();
            lock (_lock)
            {
                foreach (var chan in _activeChannels)
                {
                    RemoveAirAbsorption(chan.ChannelHandle);
                    Bass.ChannelStop(chan.ChannelHandle);
                }
                _activeChannels.Clear();
                _activeReverbFxHandles.Clear();
            }
        }

        public void Dispose()
        {
            StopAll();
            if (_windStreamHandle != 0)
            {
                Bass.StreamFree(_windStreamHandle);
                _windStreamHandle = 0;
            }
            Bass.Free();
        }
    }
}
