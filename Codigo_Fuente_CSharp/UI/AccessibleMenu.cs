using System;
using SubwaySurfersAudioGame.Accessibility;
using SubwaySurfersAudioGame.Audio;
using SubwaySurfersAudioGame.Core;

namespace SubwaySurfersAudioGame.UI
{
    public class AccessibleMenu
    {
        private readonly GameEngine _engine;
        private int _selectedMainMenuIndex = 0;
        private int _selectedMusicMenuIndex = 0;
        private int _selectedSettingsIndex = 0;
        private int _selectedTutorialIndex = 0;

        private readonly string[] _mainMenuItems = new string[]
        {
            "Jugar Carrera",
            "Tienda de Mejoras y Consumibles",
            "Escuela de Audio y Calibración HRTF",
            "Seleccionar Música de la Gira",
            "Guía de Cómo Jugar",
            "Ajustes de Volumen y Voz",
            "Salir del Juego"
        };

        private readonly string[] _settingsItems = new string[]
        {
            "Volumen de Música",
            "Volumen de Efectos de Sonido",
            "Sistema de Voz y Lector de Pantalla",
            "Volver al Menú Principal"
        };

        private readonly string[] _tutorialTopics = new string[]
        {
            "1. Objetivo del Juego, Carriles y Movimiento Aéreo",
            "2. Cómo Esquivar Vallas Bajas y Vallas Altas",
            "3. Trenes, Techos y Rebote Lateral contra Vagones",
            "4. Potenciadores, Tienda y Uso de Tablas",
            "5. Cohete Headstart y Caza de Letras Diaria",
            "6. El Inspector y su Perro",
            "7. Teclas de Consulta Rápida en Carrera",
            "Volver al Menú Principal"
        };

        public AccessibleMenu(GameEngine engine)
        {
            _engine = engine;
        }

        public void OpenMainMenu()
        {
            _engine.CurrentState = GameState.MainMenu;
            _selectedMainMenuIndex = 0;
            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap);
            _engine.Music.PlayCurrent();
            SpeakCurrentMainMenuItem();
        }

        public void OpenMusicMenu()
        {
            _engine.CurrentState = GameState.MusicMenu;
            _selectedMusicMenuIndex = _engine.Music.CurrentTrackIndex;
            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap);
            SpeakCurrentMusicItem();
        }

        public void OpenSettingsMenu()
        {
            _engine.CurrentState = GameState.SettingsMenu;
            _selectedSettingsIndex = 0;
            _engine.AudioEngine.Play2D(AudioMap.UI.SettingsOpen);
            SpeakCurrentSettingsItem();
        }

        public void OpenTutorialMenu()
        {
            _engine.CurrentState = GameState.TutorialMenu;
            _selectedTutorialIndex = 0;
            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap);
            SpeakCurrentTutorialItem();
        }

        private void SpeakCurrentMainMenuItem()
        {
            string item = _mainMenuItems[_selectedMainMenuIndex];
            _engine.Accessibility.Speak($"{item}. Opción {_selectedMainMenuIndex + 1} de {_mainMenuItems.Length}", interrupt: true);
        }

        private void SpeakCurrentMusicItem()
        {
            if (_engine.Music.Tracks.Count == 0)
            {
                _engine.Accessibility.Speak("No se encontraron pistas de música.", interrupt: true);
                return;
            }

            var track = _engine.Music.Tracks[_selectedMusicMenuIndex];
            _engine.Accessibility.Speak($"Pista {_selectedMusicMenuIndex + 1} de {_engine.Music.Tracks.Count}: {track.CityName} ({track.Title}). Pulsa Enter para escuchar y seleccionar.", interrupt: true);
        }

        private void SpeakCurrentSettingsItem()
        {
            string item = _settingsItems[_selectedSettingsIndex];
            string val = "";

            if (_selectedSettingsIndex == 0)
            {
                val = $": {(int)(_engine.Music.MasterMusicVolume * 100)}%";
            }
            else if (_selectedSettingsIndex == 1)
            {
                val = $": {(int)(_engine.AudioEngine.MasterSfxVolume * 100)}%";
            }
            else if (_selectedSettingsIndex == 2)
            {
                string modeStr = _engine.Accessibility.Mode switch
                {
                    SpeechMode.Auto => "Automático (Detectar NVDA / SAPI)",
                    SpeechMode.NvdaOnly => "Solo Lector de Pantalla NVDA",
                    SpeechMode.SapiOnly => "Solo Voz del Sistema SAPI5",
                    SpeechMode.Disabled => "Voz Desactivada",
                    _ => ""
                };
                val = $": {modeStr}";
            }

            _engine.Accessibility.Speak($"{item}{val}. Usa flechas izquierda y derecha para cambiar. Opción {_selectedSettingsIndex + 1} de {_settingsItems.Length}", interrupt: true);
        }

        private void SpeakCurrentTutorialItem()
        {
            string topic = _tutorialTopics[_selectedTutorialIndex];
            _engine.Accessibility.Speak($"{topic}. Pulsa Enter para escuchar la lección.", interrupt: true);
        }

        public void HandleMenuInput(ConsoleKeyInfo key)
        {
            if (_engine.CurrentState == GameState.MainMenu)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        _selectedMainMenuIndex = (_selectedMainMenuIndex - 1 + _mainMenuItems.Length) % _mainMenuItems.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentMainMenuItem();
                        break;

                    case ConsoleKey.DownArrow:
                        _selectedMainMenuIndex = (_selectedMainMenuIndex + 1) % _mainMenuItems.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentMainMenuItem();
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        ExecuteMainMenuSelection();
                        break;
                }
            }
            else if (_engine.CurrentState == GameState.MusicMenu)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (_engine.Music.Tracks.Count > 0)
                        {
                            _selectedMusicMenuIndex = (_selectedMusicMenuIndex - 1 + _engine.Music.Tracks.Count) % _engine.Music.Tracks.Count;
                            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                            SpeakCurrentMusicItem();
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (_engine.Music.Tracks.Count > 0)
                        {
                            _selectedMusicMenuIndex = (_selectedMusicMenuIndex + 1) % _engine.Music.Tracks.Count;
                            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                            SpeakCurrentMusicItem();
                        }
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (_engine.Music.Tracks.Count > 0)
                        {
                            _engine.Music.PlayTrack(_selectedMusicMenuIndex);
                            _engine.Accessibility.Speak($"Reproduciendo {_engine.Music.Tracks[_selectedMusicMenuIndex].CityName}. Pulsa Escape para volver al menú.", interrupt: true);
                        }
                        break;

                    case ConsoleKey.Escape:
                        OpenMainMenu();
                        break;
                }
            }
            else if (_engine.CurrentState == GameState.SettingsMenu)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        _selectedSettingsIndex = (_selectedSettingsIndex - 1 + _settingsItems.Length) % _settingsItems.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentSettingsItem();
                        break;

                    case ConsoleKey.DownArrow:
                        _selectedSettingsIndex = (_selectedSettingsIndex + 1) % _settingsItems.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentSettingsItem();
                        break;

                    case ConsoleKey.LeftArrow:
                        if (_selectedSettingsIndex == 0) // Music Volume
                        {
                            _engine.Music.MasterMusicVolume = Math.Clamp(_engine.Music.MasterMusicVolume - 0.05f, 0.0f, 1.0f);
                            _engine.Music.UpdateVolume();
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        else if (_selectedSettingsIndex == 1) // SFX Volume
                        {
                            _engine.AudioEngine.MasterSfxVolume = Math.Clamp(_engine.AudioEngine.MasterSfxVolume - 0.05f, 0.0f, 1.0f);
                            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.8f);
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        else if (_selectedSettingsIndex == 2) // Speech Mode
                        {
                            int prevMode = ((int)_engine.Accessibility.Mode - 1 + 4) % 4;
                            _engine.Accessibility.SetMode((SpeechMode)prevMode);
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (_selectedSettingsIndex == 0) // Music Volume
                        {
                            _engine.Music.MasterMusicVolume = Math.Clamp(_engine.Music.MasterMusicVolume + 0.05f, 0.0f, 1.0f);
                            _engine.Music.UpdateVolume();
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        else if (_selectedSettingsIndex == 1) // SFX Volume
                        {
                            _engine.AudioEngine.MasterSfxVolume = Math.Clamp(_engine.AudioEngine.MasterSfxVolume + 0.05f, 0.0f, 1.0f);
                            _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.8f);
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        else if (_selectedSettingsIndex == 2) // Speech Mode
                        {
                            int nextMode = ((int)_engine.Accessibility.Mode + 1) % 4;
                            _engine.Accessibility.SetMode((SpeechMode)nextMode);
                            GameSettings.Save(_engine);
                            SpeakCurrentSettingsItem();
                        }
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        if (_selectedSettingsIndex == 3) OpenMainMenu();
                        break;

                    case ConsoleKey.Escape:
                        OpenMainMenu();
                        break;
                }
            }
            else if (_engine.CurrentState == GameState.TutorialMenu)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        _selectedTutorialIndex = (_selectedTutorialIndex - 1 + _tutorialTopics.Length) % _tutorialTopics.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentTutorialItem();
                        break;

                    case ConsoleKey.DownArrow:
                        _selectedTutorialIndex = (_selectedTutorialIndex + 1) % _tutorialTopics.Length;
                        _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                        SpeakCurrentTutorialItem();
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        ExecuteTutorialTopic();
                        break;

                    case ConsoleKey.Escape:
                        OpenMainMenu();
                        break;
                }
            }
        }

        private void ExecuteMainMenuSelection()
        {
            switch (_selectedMainMenuIndex)
            {
                case 0: // Jugar Carrera
                    _engine.StartGame();
                    break;

                case 1: // Tienda de Mejoras
                    _engine.CurrentState = GameState.ShopMenu;
                    _engine.Shop.Open();
                    break;

                case 2: // Escuela de Audio y Calibración HRTF
                    _engine.CurrentState = GameState.CalibrationMenu;
                    _engine.Calibration.Open();
                    break;

                case 3: // Seleccionar Música
                    OpenMusicMenu();
                    break;

                case 4: // Guía de Cómo Jugar
                    _engine.CurrentState = GameState.TutorialMenu;
                    OpenTutorialMenu();
                    break;

                case 5: // Ajustes de Volumen y Voz
                    _engine.CurrentState = GameState.SettingsMenu;
                    OpenSettingsMenu();
                    break;

                case 6: // Salir
                    _engine.RequestExit();
                    break;
            }
        }

        private void ExecuteTutorialTopic()
        {
            switch (_selectedTutorialIndex)
            {
                case 0:
                    _engine.Accessibility.Speak(
                        "Lección 1: Objetivo, Carriles y Movimiento Aéreo. El juego se desarrolla en tres carriles continuos: Carril Izquierdo, Carril Central y Carril Derecho. " +
                        "Usa la Flecha Izquierda o la tecla A para moverte a la izquierda, y Flecha Derecha o D para moverte a la derecha. " +
                        "Ahora puedes cambiar de carril incluso mientras estás en el aire durante un salto. " +
                        "Si estás en medio de un salto y necesitas caer rápido, pulsa la Flecha Abajo o la tecla S para cancelar el salto y rodar inmediatamente al suelo. " +
                        "Pulsa L en cualquier momento para que la voz te diga en qué carril estás.",
                        interrupt: true
                    );
                    break;

                case 1:
                    _engine.Accessibility.Speak(
                        "Lección 2: Vallas Bajas y Altas. Las vallas bajas tienen un metro de altura y bloquean el suelo; debes saltar pulsando Flecha Arriba o tecla W. " +
                        "Las vallas altas bloquean el aire; debes rodar por el suelo pulsando Flecha Abajo o tecla S. " +
                        "Al rodar escucharás el sonido de deslizamiento filtrado con paso bajo.",
                        interrupt: true
                    );
                    break;

                case 2:
                    _engine.Accessibility.Speak(
                        "Lección 3: Trenes, Techos y Rebote Lateral. En las vías encontrarás trenes estáticos y trenes dinámicos que vienen hacia ti a 12 metros por segundo. " +
                        "Si un tren tiene rampa al frente, puedes subirte corriendo directo a su techo y escucharás pisadas metálicas estruendosas. " +
                        "Si intentas cambiarte lateralmente a un carril ocupado por el costado de un tren, no morirás de inmediato: escucharás un fuerte choque metálico y rebotarás a tu carril original, alertando al inspector.",
                        interrupt: true
                    );
                    break;

                case 3:
                    _engine.Accessibility.Speak(
                        "Lección 4: Potenciadores y Tienda. En las vías recogerás 5 potenciadores: Imán, Jetpack, Super Zapatillas, Multiplicador 2X y Tabla Hoverboard. " +
                        "Ahora puedes ingresar a la Tienda desde el Menú Principal para comprar inventario de tablas y mejorar de Nivel 1 a Nivel 5 la duración de tus potenciadores usando tus monedas acumuladas. " +
                        "Para activar una tabla durante la carrera, pulsa la Barra Espaciadora. Te protegerá contra 1 choque fatal.",
                        interrupt: true
                    );
                    break;

                case 4:
                    _engine.Accessibility.Speak(
                        "Lección 5: Cohete Headstart y Caza de Letras Diaria. Puedes comprar Cohetes Headstart en la Tienda. " +
                        "Al inicio de una carrera, pulsa la tecla H para activar el Headstart y volar los primeros 1,000 metros a ultra velocidad invulnerable. " +
                        "Además, a lo largo de las vías aparecerán las letras flotantes de la palabra SURFERS. " +
                        "Al recoger una letra escucharás una campanilla ascendente. ¡Completa la palabra del día para ganar 1,500 monedas de bonificación!",
                        interrupt: true
                    );
                    break;

                case 5:
                    _engine.Accessibility.Speak(
                        "Lección 6: El Inspector y el Perro. Al inicio de la partida el inspector te persigue con su perro a distancia segura. " +
                        "Si tropiezas con un obstáculo o rebotas contra un tren, el inspector se colocará a 1.5 metros detrás de ti. " +
                        "Escucharás sus pisadas pesadas, respiración y ladridos en 3D justo a tu espalda. " +
                        "Corre limpiamente durante 10 segundos para dejarlo atrás. Si tropiezas de nuevo estando cerca, ¡serás atrapado y terminará la partida!",
                        interrupt: true
                    );
                    break;

                case 6:
                    _engine.Accessibility.Speak(
                        "Lección 7: Teclas de Consulta en Carrera. Durante la carrera puedes pulsar: " +
                        "L para consultar tu carril actual. " +
                        "C para saber cuántas monedas llevas y tu saldo total en banco. " +
                        "S para consultar los metros recorridos, velocidad en kilómetros por hora y el país actual. " +
                        "P para saber qué potenciadores tienes activos y sus segundos restantes. " +
                        "H para activar un Headstart al inicio. " +
                        "Shift más S para conocer tu récord personal. " +
                        "Escape para pausar el juego.",
                        interrupt: true
                    );
                    break;

                case 7:
                    OpenMainMenu();
                    break;
            }
        }
    }
}
