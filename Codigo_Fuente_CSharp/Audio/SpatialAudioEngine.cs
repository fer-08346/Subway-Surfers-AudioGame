using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.DirectX8;

namespace SubwaySurfersAudioGame.Audio
{
    public class BassSoundChannel
    {
        public int ChannelHandle { get; set; }
        public bool Is3D { get; set; } = true;
        public bool IsLooping { get; set; } = false;
        public float BaseGain { get; set; } = 1.0f;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float VelZ { get; set; }
    }

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

        public BassSoundChannel? Play3D(string soundName, float x, float y, float z, float velZ = 0f, float pitch = 1.0f, float gain = 1.0f)
        {
            int sample = _soundLibrary.GetSample3D(soundName);
            if (sample == 0) sample = _soundLibrary.GetSample2D(soundName);
            if (sample == 0) return null;

            int channel = Bass.SampleGetChannel(sample, false);
            if (channel == 0) return null;

            float effVol = gain * MasterSfxVolume;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(effVol, 0.0f, 1.0f));

            if (pitch != 1.0f)
            {
                if (Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out float baseFreq))
                {
                    Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, baseFreq * pitch);
                }
            }

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            Bass.ChannelSet3DPosition(channel, pos, new Vector3D(0, 0, 0), vel);
            Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, 2.0f, 50.0f, 360, 360, 0);
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
                VelZ = velZ
            };

            lock (_lock)
            {
                _activeChannels.Add(sChan);
            }
            return sChan;
        }

        public BassSoundChannel? PlayLoop3D(string soundName, float x, float y, float z, float velZ = 0f, float pitch = 1.0f, float gain = 1.0f)
        {
            int sample = _soundLibrary.GetSample3D(soundName);
            if (sample == 0) sample = _soundLibrary.GetSample2D(soundName);
            if (sample == 0) return null;

            int channel = Bass.SampleGetChannel(sample, false);
            if (channel == 0) return null;

            Bass.ChannelFlags(channel, BassFlags.Loop, BassFlags.Loop);

            float effVol = gain * MasterSfxVolume;
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(effVol, 0.0f, 1.0f));

            var pos = new Vector3D(x, y, z);
            var vel = new Vector3D(0, 0, velZ);
            Bass.ChannelSet3DPosition(channel, pos, new Vector3D(0, 0, 0), vel);
            Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, 2.0f, 50.0f, 360, 360, 0);
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
                VelZ = velZ
            };

            lock (_lock)
            {
                _activeChannels.Add(sChan);
            }
            return sChan;
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
        }

        public void StopInstance(BassSoundChannel? instance)
        {
            if (instance == null || instance.ChannelHandle == 0) return;
            Bass.ChannelStop(instance.ChannelHandle);
            lock (_lock)
            {
                _activeChannels.Remove(instance);
            }
        }

        public void StopAll()
        {
            StopSpeedWind();
            lock (_lock)
            {
                foreach (var chan in _activeChannels)
                {
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
