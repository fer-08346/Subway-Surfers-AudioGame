using System;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace SubwaySurfersAudioGame.Accessibility
{
    public enum SpeechMode
    {
        Auto,
        NvdaOnly,
        SapiOnly,
        Disabled
    }

    public class AccessibilityEngine : IDisposable
    {
        private SpeechSynthesizer? _sapiVoice;
        private bool _isNvdaDetected = false;

        public SpeechMode Mode { get; set; } = SpeechMode.Auto;

        // Native NVDA Controller Client P/Invoke
        [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_speakText", CharSet = CharSet.Unicode)]
        private static extern int NvdaSpeakText64(string text);

        [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_cancelSpeech")]
        private static extern int NvdaCancelSpeech64();

        [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_testIfRunning")]
        private static extern int NvdaTestRunning64();

        [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_speakText", CharSet = CharSet.Unicode)]
        private static extern int NvdaSpeakText32(string text);

        [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_cancelSpeech")]
        private static extern int NvdaCancelSpeech32();

        [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_testIfRunning")]
        private static extern int NvdaTestRunning32();

        public bool IsNvdaActive => _isNvdaDetected;

        public void Initialize()
        {
            _isNvdaDetected = false;

            // Check if NVDA is running
            try
            {
                int status = Environment.Is64BitProcess ? NvdaTestRunning64() : NvdaTestRunning32();
                if (status == 0)
                {
                    _isNvdaDetected = true;
                }
            }
            catch
            {
                _isNvdaDetected = false;
            }

            // Only initialize SAPI if NVDA is NOT present and mode allows it
            if (!_isNvdaDetected && Mode != SpeechMode.NvdaOnly && Mode != SpeechMode.Disabled)
            {
                InitSapi();
            }
            else
            {
                DisposeSapi();
            }
        }

        private void InitSapi()
        {
            if (_sapiVoice != null) return;
            try
            {
                _sapiVoice = new SpeechSynthesizer();
                _sapiVoice.SetOutputToDefaultAudioDevice();
                _sapiVoice.Rate = 2; // Rapid response rate for gaming
                _sapiVoice.Volume = 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Accessibility] SAPI init warning: {ex.Message}");
                _sapiVoice = null;
            }
        }

        private void DisposeSapi()
        {
            if (_sapiVoice != null)
            {
                try
                {
                    _sapiVoice.SpeakAsyncCancelAll();
                    _sapiVoice.Dispose();
                }
                catch { }
                _sapiVoice = null;
            }
        }

        public void SetMode(SpeechMode newMode)
        {
            Mode = newMode;
            if (Mode == SpeechMode.SapiOnly)
            {
                InitSapi();
            }
            else if (Mode == SpeechMode.NvdaOnly || (_isNvdaDetected && Mode == SpeechMode.Auto))
            {
                DisposeSapi();
            }
            else if (Mode == SpeechMode.Disabled)
            {
                DisposeSapi();
            }
        }

        public void Speak(string text, bool interrupt = false)
        {
            if (string.IsNullOrWhiteSpace(text) || Mode == SpeechMode.Disabled) return;

            Task.Run(() =>
            {
                bool useNvda = (Mode == SpeechMode.NvdaOnly) || (Mode == SpeechMode.Auto && _isNvdaDetected);

                if (useNvda)
                {
                    try
                    {
                        if (interrupt)
                        {
                            if (Environment.Is64BitProcess) NvdaCancelSpeech64();
                            else NvdaCancelSpeech32();
                        }
                        if (Environment.Is64BitProcess) NvdaSpeakText64(text);
                        else NvdaSpeakText32(text);
                        return;
                    }
                    catch
                    {
                        _isNvdaDetected = false;
                    }
                }

                // SAPI Fallback ONLY if NVDA is not being used
                if (_sapiVoice != null && (Mode == SpeechMode.SapiOnly || (Mode == SpeechMode.Auto && !_isNvdaDetected)))
                {
                    try
                    {
                        if (interrupt)
                        {
                            _sapiVoice.SpeakAsyncCancelAll();
                        }
                        _sapiVoice.SpeakAsync(text);
                    }
                    catch { }
                }
            });
        }

        public void StopSpeech()
        {
            if (_isNvdaDetected || Mode == SpeechMode.NvdaOnly)
            {
                try
                {
                    if (Environment.Is64BitProcess) NvdaCancelSpeech64();
                    else NvdaCancelSpeech32();
                }
                catch { }
            }
            if (_sapiVoice != null)
            {
                try
                {
                    _sapiVoice.SpeakAsyncCancelAll();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            DisposeSapi();
        }
    }
}
