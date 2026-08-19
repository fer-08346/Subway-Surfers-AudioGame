using System;
using System.Threading.Tasks;
using SubwaySurfersAudioGame.Audio;
using SubwaySurfersAudioGame.Core;

namespace SubwaySurfersAudioGame.UI
{
    public class CalibrationMenu
    {
        private readonly GameEngine _engine;
        private int _selectedIndex = 0;

        private readonly string[] _tests = new string[]
        {
            "Carril Izquierdo (Posición 3D a menos 3 metros)",
            "Carril Central (Posición 3D al frente)",
            "Carril Derecho (Posición 3D a más 3 metros)",
            "Salto y Elevación Acústica",
            "Rodar por el Suelo con Filtro Paso Bajo",
            "Tren en Movimiento con Efecto Doppler",
            "Arco de Monedas con Escala Tonal Ascendente",
            "Inspector y Perro Detrás (180 grados a 1.5 metros)",
            "Reverberación Ambiental de Túnel Subterráneo",
            "Volver al Menú Principal"
        };

        public CalibrationMenu(GameEngine engine)
        {
            _engine = engine;
        }

        public void Open()
        {
            _selectedIndex = 0;
            _engine.AudioEngine.Play2D(AudioMap.UI.SettingsOpen, gain: 0.7f);
            SpeakCurrentItem();
        }

        public void SpeakCurrentItem()
        {
            string text = $"{_tests[_selectedIndex]}. Pulsa Enter o Espacio para escuchar la prueba acústica. Opción {_selectedIndex + 1} de {_tests.Length}.";
            _engine.Accessibility.Speak(text, interrupt: true);
        }

        public void HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    _selectedIndex = (_selectedIndex - 1 + _tests.Length) % _tests.Length;
                    _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                    SpeakCurrentItem();
                    break;

                case ConsoleKey.DownArrow:
                    _selectedIndex = (_selectedIndex + 1) % _tests.Length;
                    _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                    SpeakCurrentItem();
                    break;

                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    ExecuteTest(_selectedIndex);
                    break;

                case ConsoleKey.Escape:
                    _engine.CurrentState = GameState.MainMenu;
                    _engine.Menu.OpenMainMenu();
                    break;
            }
        }

        private void ExecuteTest(int index)
        {
            switch (index)
            {
                case 0: // Left Lane (-3m)
                    _engine.Accessibility.Speak("Probando Carril Izquierdo...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play3D(AudioMap.Locomotion.LaneDodge, -3.0f, 0.0f, 2.0f, gain: 1.0f);
                    });
                    break;

                case 1: // Center Lane (0m)
                    _engine.Accessibility.Speak("Probando Carril Central...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play3D(AudioMap.Locomotion.LaneDodge, 0.0f, 0.0f, 2.0f, gain: 1.0f);
                    });
                    break;

                case 2: // Right Lane (+3m)
                    _engine.Accessibility.Speak("Probando Carril Derecho...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play3D(AudioMap.Locomotion.LaneDodge, 3.0f, 0.0f, 2.0f, gain: 1.0f);
                    });
                    break;

                case 3: // Jump & Elevation
                    _engine.Accessibility.Speak("Probando Salto y Aterrizaje...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play2D(AudioMap.Locomotion.JumpStandard, gain: 0.9f);
                        Task.Delay(600).ContinueWith(__ =>
                        {
                            _engine.AudioEngine.Play2D(AudioMap.Locomotion.LandingGround, gain: 0.8f);
                        });
                    });
                    break;

                case 4: // Roll & Low pass filter
                    _engine.Accessibility.Speak("Probando Rodar por el Suelo...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play2D(AudioMap.Locomotion.RollSlide, gain: 0.9f);
                    });
                    break;

                case 5: // Incoming Train Doppler
                    _engine.Accessibility.Speak("Probando Tren con Efecto Doppler en carril derecho...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.Play3D(AudioMap.Obstacles.TrainApproaching, 3.0f, 0.0f, 25.0f, velZ: -12.0f, gain: 1.0f);
                    });
                    break;

                case 6: // Coin Arc Streak
                    _engine.Accessibility.Speak("Probando Racha de Monedas Ascendente...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int step = i;
                            Task.Delay(step * 160).ContinueWith(__ =>
                            {
                                float pitch = 1.0f + (step * 0.06f);
                                _engine.AudioEngine.Play2D(AudioMap.Collectibles.CoinPickup, pan: 0.0f, pitch: pitch, gain: 0.85f);
                            });
                        }
                    });
                    break;

                case 7: // Guard Proximity Rear (180 deg)
                    _engine.Accessibility.Speak("Probando Inspector y Perro a 1.5 metros detrás de ti...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        var chan = _engine.AudioEngine.Play3D(AudioMap.Pursuit.GuardProximityLoop, 0.0f, 0.0f, -1.5f, gain: 0.9f);
                        Task.Delay(2500).ContinueWith(__ =>
                        {
                            _engine.AudioEngine.StopInstance(chan);
                        });
                    });
                    break;

                case 8: // Tunnel Reverb
                    _engine.Accessibility.Speak("Probando Reverberación de Túnel...", interrupt: true);
                    Task.Delay(800).ContinueWith(_ =>
                    {
                        _engine.AudioEngine.SetTunnelReverb(true);
                        _engine.AudioEngine.Play3D(AudioMap.Locomotion.FootstepRoofLeft, 0.0f, 3.8f, 1.0f, gain: 1.0f);
                        Task.Delay(400).ContinueWith(__ =>
                        {
                            _engine.AudioEngine.Play3D(AudioMap.Locomotion.FootstepRoofRight, 0.0f, 3.8f, 1.0f, gain: 1.0f);
                            Task.Delay(1500).ContinueWith(___ =>
                            {
                                _engine.AudioEngine.SetTunnelReverb(false);
                            });
                        });
                    });
                    break;

                case 9: // Return to Main Menu
                    _engine.CurrentState = GameState.MainMenu;
                    _engine.Menu.OpenMainMenu();
                    break;
            }
        }
    }
}
