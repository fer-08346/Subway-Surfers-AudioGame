using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SubwaySurfersAudioGame.Accessibility;
using SubwaySurfersAudioGame.Audio;
using SubwaySurfersAudioGame.UI;

namespace SubwaySurfersAudioGame.Core
{
    public enum GameState
    {
        MainMenu,
        InGame,
        Paused,
        GameOver,
        MusicMenu,
        SettingsMenu,
        TutorialMenu,
        ShopMenu,
        CalibrationMenu
    }

    public class GameEngine
    {
        public GameState CurrentState { get; set; } = GameState.MainMenu;
        public Player Player { get; } = new();
        public WorldGenerator World { get; } = new();
        public PursuitSystem Pursuit { get; } = new();
        public Inventory Inventory { get; } = new();
        public WorldTourSystem WorldTour { get; } = new();

        public SpatialAudioEngine AudioEngine { get; }
        public MusicManager Music { get; }
        public AccessibilityEngine Accessibility { get; }
        public AccessibleMenu Menu { get; }
        public ShopMenu Shop { get; }
        public CalibrationMenu Calibration { get; }

        public float HighScore { get; private set; } = 0.0f;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;
        public UpdateInfo? PendingUpdate { get; private set; }
        public bool IsRunning { get; private set; } = true;
        public bool IsDebugModeEnabled { get; private set; } = false;
        public event Action? OnRequestExit;

        private BassSoundChannel? _jetpackLoopInstance;
        private BassSoundChannel? _magnetLoopInstance;
        private BassSoundChannel? _shieldLoopInstance;

        public GameEngine(string sfxDir, string musicDir)
        {
            IsDebugModeEnabled = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.txt")) ||
                                 File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".debug"));

            var saved = GameSettings.Load();
            HighScore = saved.HighScore;
            Difficulty = saved.Difficulty;

            // Load Inventory data
            Inventory.TotalCoins = saved.TotalCoins;
            Inventory.HoverboardCount = saved.HoverboardCount;
            Inventory.HeadstartCount = saved.HeadstartCount;
            Inventory.MagnetLevel = saved.MagnetLevel;
            Inventory.JetpackLevel = saved.JetpackLevel;
            Inventory.SuperSneakersLevel = saved.SuperSneakersLevel;
            Inventory.MultiplierLevel = saved.MultiplierLevel;
            Inventory.DailyWord = saved.DailyWord;
            if (saved.DailyLettersCollected != null && saved.DailyLettersCollected.Length == 7)
            {
                Inventory.DailyLettersCollected = saved.DailyLettersCollected;
            }
            Inventory.DailyWordCompleted = saved.DailyWordCompleted;

            Accessibility = new AccessibilityEngine();
            Accessibility.Initialize();
            Accessibility.SetMode(saved.SpeechMode);

            var soundLib = new SoundLibrary(sfxDir);

            AudioEngine = new SpatialAudioEngine(soundLib);
            AudioEngine.MasterSfxVolume = saved.SfxVolume;
            AudioEngine.Start(); // Initialize BASS first

            soundLib.LoadAllSounds(); // Load samples after BASS is initialized

            Music = new MusicManager(musicDir);
            Music.MasterMusicVolume = saved.MusicVolume;

            Menu = new AccessibleMenu(this);
            Shop = new ShopMenu(this);
            Calibration = new CalibrationMenu(this);

            // Silent background check for a newer release on GitHub (non-blocking).
            _ = Task.Run(async () =>
            {
                try
                {
                    var info = await UpdateChecker.CheckForUpdateAsync();
                    if (info != null)
                    {
                        PendingUpdate = info;
                        Music.PlayTrack(0); // Ensure main theme is playing
                        Accessibility.Speak(
                            $"¡Hay una actualización disponible! Versión {info.Version}. " +
                            $"Pulsa la tecla U para actualizar mientras suena la música.",
                            interrupt: false);
                    }
                }
                catch { }
            });
        }

        public void StartGame()
        {
            Player.Reset();
            World.Reset();
            World.Difficulty = Difficulty;
            Pursuit.Reset();
            WorldTour.Reset();

            CurrentState = GameState.InGame;
            AudioEngine.StopAll();
            AudioEngine.StartSpeedWind();

            // Play track 0 (2012 Main Theme)
            Music.PlayTrack(0);

            // Play start whistle and inspector shout
            AudioEngine.Play2D(AudioMap.Pursuit.GuardStartWhistle, gain: 1.0f);

            string introText = $"¡Comienza la carrera en {WorldTour.GetCurrentCountryName(Music)}! " +
                               $"Usa flechas para moverte. Pulsa H para usar un Cohete Headstart. " +
                               $"Palabra del día: {Inventory.GetDailyWordProgressString()}.";
            Accessibility.Speak(introText, interrupt: true);
        }

        public void PauseGame()
        {
            CurrentState = GameState.Paused;
            Music.Pause();
            AudioEngine.StopSpeedWind();
            AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap);
            Accessibility.Speak("Juego pausado. Pulsa Escape para reanudar o Q para volver al menú principal.", interrupt: true);
        }

        public void ResumeGame()
        {
            CurrentState = GameState.InGame;
            Music.Resume();
            AudioEngine.StartSpeedWind();
            AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap);
            Accessibility.Speak("Reanudando juego.", interrupt: true);
        }

        public void TriggerGameOver(string causeOfDeath)
        {
            CurrentState = GameState.GameOver;
            Pursuit.StopAudio(AudioEngine);
            StopPowerupLoops();
            AudioEngine.StopSpeedWind();
            AudioEngine.SetTunnelReverb(false);

            // Bank coins collected during the run
            Inventory.TotalCoins += Player.Coins;

            bool isNewRecord = Player.Score > HighScore;
            if (isNewRecord)
            {
                HighScore = Player.Score;
            }
            GameSettings.Save(this);

            AudioEngine.Play2D(AudioMap.Obstacles.FatalDeath, gain: 1.0f);
            AudioEngine.Play2D(AudioMap.Obstacles.StingerLose, gain: 0.8f);

            string laneStr = Player.CurrentLane == Lane.Left ? "carril izquierdo" :
                             Player.CurrentLane == Lane.Right ? "carril derecho" : "carril central";
            int speedKmh = (int)(Player.Speed * 3.6f);

            string postMortem = $"¡Fin del juego! Causa de muerte: {causeOfDeath} en {laneStr}. " +
                                $"Velocidad final: {Player.Speed:F1} metros por segundo ({speedKmh} kilómetros por hora). " +
                                $"Distancia recorrida: {(int)Player.Score} metros en {WorldTour.GetCurrentCountryName(Music)}. " +
                                $"Monedas obtenidas: {Player.Coins}. Total en banco: {Inventory.TotalCoins} monedas. " +
                                $"Palabra del día: {Inventory.GetDailyWordProgressString()}. ";

            if (isNewRecord)
            {
                postMortem += "¡NUEVO RÉCORD PERSONAL! ";
            }

            postMortem += "Pulsa Espacio o Enter para jugar de nuevo, o Escape para ir al Menú.";

            Accessibility.Speak(postMortem, interrupt: true);
        }

        public void Update(float dt)
        {
            if (CurrentState != GameState.InGame) return;

            // 1. Update Player Physics
            Player.Update(dt);

            // 2. Update 3D Listener Coords
            AudioEngine.UpdateListener(Player.CurrentX, Player.Y, Player.Z, Player.Speed);

            // 2.1 Refresh Dolby Atmos object treatment (air absorption / elevation) for all active 3D objects
            AudioEngine.UpdateSpatialObjects();

            // 3. Dynamic Speed Wind layer scaling with velocity
            AudioEngine.UpdateSpeedWind(Player.Speed);

            // 4. Environmental Tunnel Reverb DSP
            bool inTunnel = World.IsInsideTunnel(Player.Z);
            AudioEngine.SetTunnelReverb(inTunnel);

            // 5. World Tour Stage Progression (every 2,000m)
            WorldTour.Update(Player.Z, this);

            // 6. Determine if player is on a train roof
            CheckTrainRoofState();

            // 7. Footstep Sounds
            UpdateFootsteps(dt);

            // 8. Update World Generator & Obstacles
            World.Update(Player.Z, AudioEngine, Inventory);

            // 9. Update Pursuit (Inspector & Dog)
            Pursuit.Update(dt, Player.CurrentX, Player.Y, Player.Z, AudioEngine);

            // 10. Process Collisions & Power-ups
            CheckCollisions();

            // 11. Update Active Power-up Audio Loops
            UpdatePowerupAudioLoops();
        }

        private void CheckTrainRoofState()
        {
            bool onRoof = false;
            foreach (var entity in World.ActiveEntities)
            {
                if (entity is StaticTrain train)
                {
                    if (entity.Lane == Player.CurrentLane || MathF.Abs(Player.CurrentX - entity.X) < 1.4f)
                    {
                        if (Player.Z >= train.Z - 1.0f && Player.Z <= train.Z + train.Length)
                        {
                            if (Player.Y >= 3.0f || Player.BaseGroundY >= 3.0f)
                            {
                                onRoof = true;
                                Player.BaseGroundY = 3.8f;
                                break;
                            }
                        }
                    }
                }
            }
            if (!onRoof && Player.BaseGroundY > 0.0f)
            {
                Player.BaseGroundY = 0.0f; // Drop down to tracks
            }
        }

        private void UpdateFootsteps(float dt)
        {
            if (Player.State == VerticalState.Running)
            {
                Player.FootstepTimer += dt;
                float stepInterval = Math.Clamp(0.35f - (Player.Speed - 10.0f) * 0.007f, 0.16f, 0.35f);

                if (Player.FootstepTimer >= stepInterval)
                {
                    Player.FootstepTimer = 0.0f;
                    string soundName;

                    if (Player.BaseGroundY >= 3.0f)
                    {
                        // On train roof: Metallic resonance footsteps
                        soundName = Player.NextFootIsLeft ? AudioMap.Locomotion.FootstepRoofLeft : AudioMap.Locomotion.FootstepRoofRight;
                    }
                    else if (Player.HasSuperSneakers)
                    {
                        soundName = Player.NextFootIsLeft ? AudioMap.Locomotion.FootstepSneakersLeft : AudioMap.Locomotion.FootstepSneakersRight;
                    }
                    else
                    {
                        // Standard track footsteps
                        soundName = Player.NextFootIsLeft ? AudioMap.Locomotion.FootstepTrackLeft : AudioMap.Locomotion.FootstepTrackRight;
                    }

                    float pan = Player.NextFootIsLeft ? -0.15f : 0.15f;
                    AudioEngine.Play2D(soundName, pan: pan, gain: 0.5f);
                    Player.NextFootIsLeft = !Player.NextFootIsLeft;
                }
            }
        }

        private void CheckCollisions()
        {
            float pZ = Player.Z;
            float pX = Player.CurrentX;
            float pY = Player.Y;

            for (int i = World.ActiveEntities.Count - 1; i >= 0; i--)
            {
                var entity = World.ActiveEntities[i];
                if (entity.IsCollectedOrPassed) continue;

                // 1. Coins
                if (entity is CoinItem coin)
                {
                    bool inMagnetRange = Player.HasMagnet && BinauralDSP.CalculateDistance(coin.X, coin.Y, coin.Z, pX, pY, pZ) <= 14.0f;
                    bool inTouch = MathF.Abs(coin.Z - pZ) < 1.5f && MathF.Abs(coin.X - pX) < 1.4f && MathF.Abs(coin.Y - pY) < 1.8f;

                    if (inTouch || inMagnetRange)
                    {
                        coin.IsCollectedOrPassed = true;
                        Player.Coins++;
                        Player.CoinStreak++;
                        Player.CoinStreakResetTimer = 1.2f;

                        // Increment pitch on consecutive streak
                        float pitch = 1.0f + Math.Min(12, Player.CoinStreak) * 0.045f;
                        AudioEngine.Play2D(AudioMap.Collectibles.CoinPickup, pan: (coin.X - pX) / 3.0f, pitch: pitch, gain: 0.7f);
                    }
                }
                // 2. Daily Word Letters
                else if (entity is LetterItem letter)
                {
                    if (MathF.Abs(letter.Z - pZ) < 1.6f && MathF.Abs(letter.X - pX) < 1.4f && MathF.Abs(letter.Y - pY) < 2.0f)
                    {
                        letter.IsCollectedOrPassed = true;
                        bool collected = Inventory.CollectDailyLetter(letter.Letter, out int idx, out bool wordCompleted);
                        if (collected)
                        {
                            float pitch = 1.0f + idx * 0.08f;
                            AudioEngine.Play2D(AudioMap.Collectibles.LetterSlideIn, pitch: pitch, gain: 0.85f);
                            if (wordCompleted)
                            {
                                AudioEngine.Play2D(AudioMap.UI.TopRunCelebration, gain: 1.0f);
                                Accessibility.Speak($"¡PALABRA {Inventory.DailyWord} COMPLETADA! ¡Premio de 1,500 monedas!", interrupt: true);
                            }
                            else
                            {
                                Accessibility.Speak($"¡Letra {letter.Letter} recogida! ({Inventory.GetDailyWordProgressString()})", interrupt: false);
                            }
                            GameSettings.Save(this);
                        }
                    }
                }
                // 3. Power-ups
                else if (entity is PowerUpItem powerUp)
                {
                    if (MathF.Abs(powerUp.Z - pZ) < 1.6f && MathF.Abs(powerUp.X - pX) < 1.4f && MathF.Abs(powerUp.Y - pY) < 2.0f)
                    {
                        powerUp.IsCollectedOrPassed = true;
                        ActivatePowerUp(powerUp.Type);
                    }
                }
                // 4. Low Barrier (Jump Required)
                else if (entity is LowBarrier low)
                {
                    if (MathF.Abs(low.Z - pZ) < 0.8f && MathF.Abs(low.X - pX) < 1.2f)
                    {
                        if (pY < low.Height && !Player.IsInvulnerable && Player.State != VerticalState.JetpackFlying)
                        {
                            HandleObstacleHit("Colisión contra valla baja por no saltar", fatal: false);
                            low.IsCollectedOrPassed = true;
                        }
                    }
                }
                // 5. High Barrier (Roll Required)
                else if (entity is HighBarrier high)
                {
                    if (MathF.Abs(high.Z - pZ) < 0.8f && MathF.Abs(high.X - pX) < 1.2f)
                    {
                        if (pY >= high.BottomClearance && Player.State != VerticalState.Rolling && !Player.IsInvulnerable && Player.State != VerticalState.JetpackFlying)
                        {
                            HandleObstacleHit("Impacto contra valla alta por no rodar agachado", fatal: false);
                            high.IsCollectedOrPassed = true;
                        }
                    }
                }
                // 6. Static Train
                else if (entity is StaticTrain sTrain)
                {
                    if (pZ >= sTrain.Z - 0.5f && pZ <= sTrain.Z + sTrain.Length && MathF.Abs(sTrain.X - pX) < 1.2f)
                    {
                        if (pY < 3.0f && !Player.IsInvulnerable && Player.State != VerticalState.JetpackFlying)
                        {
                            if (sTrain.HasRamp && pZ <= sTrain.Z + sTrain.RampLength)
                            {
                                Player.BaseGroundY = 3.8f;
                            }
                            else
                            {
                                HandleObstacleHit("Choque frontal contra tren estático", fatal: true);
                                sTrain.IsCollectedOrPassed = true;
                            }
                        }
                    }
                }
                // 7. Dynamic Train
                else if (entity is DynamicTrain dTrain)
                {
                    if (MathF.Abs(dTrain.Z - pZ) < 2.0f && MathF.Abs(dTrain.X - pX) < 1.2f)
                    {
                        if (pY < 3.0f && !Player.IsInvulnerable && Player.State != VerticalState.JetpackFlying)
                        {
                            HandleObstacleHit("Choque frontal contra tren en movimiento", fatal: true);
                            dTrain.IsCollectedOrPassed = true;
                        }
                    }
                }
            }
        }

        private void HandleObstacleHit(string causeOfDeath, bool fatal)
        {
            if (Player.HasHoverboard)
            {
                // Hoverboard shield absorbs the fatal impact!
                Player.HoverboardTimer = 0.0f;
                Player.InvulnerabilityTimer = 1.5f;
                AudioEngine.Play2D(AudioMap.PowerUps.HoverboardCrash, gain: 1.0f);
                Accessibility.Speak("¡Escudo de tabla destruido! Invulnerabilidad temporal.", interrupt: false);
                return;
            }

            if (fatal)
            {
                TriggerGameOver(causeOfDeath);
            }
            else
            {
                AudioEngine.Play2D(AudioMap.Obstacles.StumbleLight, gain: 0.9f);
                bool captured = Pursuit.TriggerStumble(AudioEngine, Player.CurrentX, Player.Y, Player.Z);
                if (captured)
                {
                    TriggerGameOver("Capturado por el inspector tras tropezar dos veces");
                }
                else
                {
                    Accessibility.Speak("¡Tropezón! El inspector y el perro están detrás de ti.", interrupt: false);
                }
            }
        }

        private void ActivatePowerUp(PowerUpType type)
        {
            AudioEngine.Play2D(AudioMap.PowerUps.PowerUpCollect, gain: 0.9f);

            switch (type)
            {
                case PowerUpType.Magnet:
                    Player.MagnetTimer = Inventory.GetMagnetDuration();
                    Accessibility.Speak($"¡Imán activado! {(int)Player.MagnetTimer} segundos.", interrupt: false);
                    break;

                case PowerUpType.Jetpack:
                    Player.JetpackTimer = Inventory.GetJetpackDuration();
                    AudioEngine.Play2D(AudioMap.PowerUps.JetpackIgnite, gain: 1.0f);
                    Accessibility.Speak($"¡Mochila propulsora activada! {(int)Player.JetpackTimer} segundos.", interrupt: false);
                    break;

                case PowerUpType.SuperSneakers:
                    Player.SuperSneakersTimer = Inventory.GetSuperSneakersDuration();
                    Accessibility.Speak($"¡Super Zapatillas activadas! {(int)Player.SuperSneakersTimer} segundos.", interrupt: false);
                    break;

                case PowerUpType.Hoverboard:
                    Player.HoverboardTimer = 30.0f;
                    AudioEngine.Play2D(AudioMap.PowerUps.HoverboardActivate, gain: 1.0f);
                    Accessibility.Speak("¡Tabla aerodeslizadora activada!", interrupt: false);
                    break;

                case PowerUpType.Multiplier2x:
                    Player.Multiplier2xTimer = Inventory.GetMultiplierDuration();
                    AudioEngine.Play2D(AudioMap.PowerUps.Multiplier2xFly, gain: 0.9f);
                    Accessibility.Speak($"¡Multiplicador doble activado! {(int)Player.Multiplier2xTimer} segundos.", interrupt: false);
                    break;
            }
        }

        private void UpdatePowerupAudioLoops()
        {
            // Magnet loop
            if (Player.HasMagnet && _magnetLoopInstance == null)
            {
                _magnetLoopInstance = AudioEngine.PlayLoop3D(AudioMap.PowerUps.MagnetLoop, Player.CurrentX, Player.Y, Player.Z, gain: 0.6f);
            }
            else if (!Player.HasMagnet && _magnetLoopInstance != null)
            {
                AudioEngine.StopInstance(_magnetLoopInstance);
                _magnetLoopInstance = null;
            }

            // Jetpack loop
            if (Player.HasJetpack && _jetpackLoopInstance == null)
            {
                _jetpackLoopInstance = AudioEngine.PlayLoop3D(AudioMap.PowerUps.JetpackLoop, Player.CurrentX, Player.Y, Player.Z, gain: 0.7f);
            }
            else if (!Player.HasJetpack && _jetpackLoopInstance != null)
            {
                AudioEngine.StopInstance(_jetpackLoopInstance);
                _jetpackLoopInstance = null;
            }

            // Hoverboard shield ongoing hum
            if (Player.HasHoverboard && _shieldLoopInstance == null)
            {
                _shieldLoopInstance = AudioEngine.PlayLoop3D(AudioMap.PowerUps.HoverboardOngoingLoop, Player.CurrentX, Player.Y, Player.Z, gain: 0.4f);
            }
            else if (!Player.HasHoverboard && _shieldLoopInstance != null)
            {
                AudioEngine.StopInstance(_shieldLoopInstance);
                _shieldLoopInstance = null;
            }
        }

        private void StopPowerupLoops()
        {
            if (_magnetLoopInstance != null) { AudioEngine.StopInstance(_magnetLoopInstance); _magnetLoopInstance = null; }
            if (_jetpackLoopInstance != null) { AudioEngine.StopInstance(_jetpackLoopInstance); _jetpackLoopInstance = null; }
            if (_shieldLoopInstance != null) { AudioEngine.StopInstance(_shieldLoopInstance); _shieldLoopInstance = null; }
        }

        public void HandleInput(ConsoleKeyInfo key)
        {
            if (CurrentState == GameState.MainMenu || CurrentState == GameState.MusicMenu || CurrentState == GameState.SettingsMenu || CurrentState == GameState.TutorialMenu)
            {
                Menu.HandleMenuInput(key);
                return;
            }

            if (CurrentState == GameState.ShopMenu)
            {
                Shop.HandleInput(key);
                return;
            }

            if (CurrentState == GameState.CalibrationMenu)
            {
                Calibration.HandleInput(key);
                return;
            }

            if (CurrentState == GameState.GameOver)
            {
                if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
                {
                    StartGame();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    CurrentState = GameState.MainMenu;
                    Menu.OpenMainMenu();
                }
                return;
            }

            if (CurrentState == GameState.Paused)
            {
                if (key.Key == ConsoleKey.Escape)
                {
                    ResumeGame();
                }
                else if (key.Key == ConsoleKey.Q)
                {
                    CurrentState = GameState.MainMenu;
                    Menu.OpenMainMenu();
                }
                return;
            }

            if (CurrentState == GameState.InGame)
            {
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        HandleMoveLeft();
                        break;

                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        HandleMoveRight();
                        break;

                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        if (Player.Jump())
                        {
                            string jumpSnd = Player.HasSuperSneakers ? AudioMap.Locomotion.JumpSneakers : AudioMap.Locomotion.JumpStandard;
                            AudioEngine.Play2D(jumpSnd, gain: 0.8f);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        // Shift + S = Query High Score
                        if ((key.Modifiers & ConsoleModifiers.Shift) != 0)
                        {
                            Accessibility.Speak($"Récord personal: {(int)HighScore} metros.", interrupt: true);
                        }
                        else if (key.Key == ConsoleKey.S && (key.Modifiers & ConsoleModifiers.Shift) == 0 && key.KeyChar == 's')
                        {
                            // S query = Current distance and speed
                            int spd = (int)(Player.Speed * 3.6f);
                            Accessibility.Speak($"Puntuación: {(int)Player.Score} metros a {spd} kilómetros por hora en {WorldTour.GetCurrentCountryName(Music)}.", interrupt: true);
                        }
                        else
                        {
                            // Down Arrow / S for Fast Roll
                            if (Player.Roll())
                            {
                                AudioEngine.Play2D(AudioMap.Locomotion.RollSlide, gain: 0.75f);
                            }
                        }
                        break;

                    case ConsoleKey.Spacebar:
                        if (Player.TryActivateHoverboard(Inventory, out string hReason))
                        {
                            AudioEngine.Play2D(AudioMap.PowerUps.HoverboardActivate, gain: 0.9f);
                            Accessibility.Speak(hReason, interrupt: false);
                            GameSettings.Save(this);
                        }
                        else
                        {
                            Accessibility.Speak(hReason, interrupt: true);
                        }
                        break;

                    case ConsoleKey.H:
                        if (Player.TryActivateHeadstart(Inventory, out string hsReason))
                        {
                            AudioEngine.Play2D(AudioMap.PowerUps.HeadstartTurbo, gain: 1.0f);
                            Accessibility.Speak(hsReason, interrupt: false);
                            GameSettings.Save(this);
                        }
                        else
                        {
                            Accessibility.Speak(hsReason, interrupt: true);
                        }
                        break;

                    case ConsoleKey.L:
                        string laneStr = Player.CurrentLane == Lane.Left ? "Carril Izquierdo" :
                                         Player.CurrentLane == Lane.Right ? "Carril Derecho" : "Carril Central";
                        Accessibility.Speak(laneStr, interrupt: true);
                        break;

                    case ConsoleKey.C:
                        Accessibility.Speak($"Monedas en carrera: {Player.Coins}. Total en banco: {Inventory.TotalCoins}.", interrupt: true);
                        break;

                    case ConsoleKey.P:
                        string powerupsStr = "";
                        if (Player.HasMagnet) powerupsStr += $"Imán, {(int)Player.MagnetTimer} segundos. ";
                        if (Player.HasJetpack) powerupsStr += $"Mochila propulsora, {(int)Player.JetpackTimer} segundos. ";
                        if (Player.HasSuperSneakers) powerupsStr += $"Super Zapatillas, {(int)Player.SuperSneakersTimer} segundos. ";
                        if (Player.HasHoverboard) powerupsStr += $"Tabla aerodeslizadora, {(int)Player.HoverboardTimer} segundos. ";
                        if (Player.HasMultiplier2x) powerupsStr += $"Multiplicador doble, {(int)Player.Multiplier2xTimer} segundos. ";
                        if (Player.HasHeadstart) powerupsStr += $"Headstart, {(int)Player.HeadstartDistanceRemaining} metros restantes. ";
                        if (string.IsNullOrEmpty(powerupsStr)) powerupsStr = "Ningún potenciador activo.";
                        Accessibility.Speak(powerupsStr, interrupt: true);
                        break;

                    case ConsoleKey.F1:
                        if (IsDebugModeEnabled)
                        {
                            WorldTour.AdvanceStageManually(this);
                        }
                        break;

                    case ConsoleKey.F2:
                        if (IsDebugModeEnabled)
                        {
                            WorldTour.PreviousStageManually(this);
                        }
                        break;

                    case ConsoleKey.F3:
                        if (IsDebugModeEnabled)
                        {
                            HandleObstacleHit("Prueba de tropezón de depuración", fatal: false);
                        }
                        break;

                    case ConsoleKey.F4:
                        if (IsDebugModeEnabled)
                        {
                            Inventory.TotalCoins += 500;
                            Accessibility.Speak($"[Modo Desarrollador] 500 monedas agregadas. Total en banco: {Inventory.TotalCoins}", interrupt: true);
                        }
                        break;

                    case ConsoleKey.Escape:
                        PauseGame();
                        break;
                }
            }
        }

        private void HandleMoveLeft()
        {
            if ((int)Player.TargetLane > (int)Lane.Left)
            {
                Lane nextLane = (Lane)((int)Player.TargetLane - 1);
                // Check if lateral switch collides with a train
                if (World.CheckLateralTrainCollision(nextLane, Player.Z, Player.Y))
                {
                    // Wall Bump / Lateral Rebound!
                    Player.TriggerRebound(Player.CurrentLane);
                    AudioEngine.Play2D(AudioMap.Obstacles.StumbleSide, pan: -0.8f, gain: 0.9f);
                    bool captured = Pursuit.TriggerStumble(AudioEngine, Player.CurrentX, Player.Y, Player.Z);
                    if (captured)
                    {
                        TriggerGameOver("Capturado tras rebotar contra un tren");
                    }
                    else
                    {
                        Accessibility.Speak("¡Rebote contra el tren lateral! El inspector se acerca.", interrupt: false);
                    }
                }
                else
                {
                    if (Player.SwitchLaneLeft())
                    {
                        AudioEngine.Play2D(AudioMap.Locomotion.LaneDodge, pan: -0.8f, gain: 0.7f);
                    }
                }
            }
        }

        private void HandleMoveRight()
        {
            if ((int)Player.TargetLane < (int)Lane.Right)
            {
                Lane nextLane = (Lane)((int)Player.TargetLane + 1);
                // Check if lateral switch collides with a train
                if (World.CheckLateralTrainCollision(nextLane, Player.Z, Player.Y))
                {
                    // Wall Bump / Lateral Rebound!
                    Player.TriggerRebound(Player.CurrentLane);
                    AudioEngine.Play2D(AudioMap.Obstacles.StumbleSide, pan: 0.8f, gain: 0.9f);
                    bool captured = Pursuit.TriggerStumble(AudioEngine, Player.CurrentX, Player.Y, Player.Z);
                    if (captured)
                    {
                        TriggerGameOver("Capturado tras rebotar contra un tren");
                    }
                    else
                    {
                        Accessibility.Speak("¡Rebote contra el tren lateral! El inspector se acerca.", interrupt: false);
                    }
                }
                else
                {
                    if (Player.SwitchLaneRight())
                    {
                        AudioEngine.Play2D(AudioMap.Locomotion.LaneDodge, pan: 0.8f, gain: 0.7f);
                    }
                }
            }
        }

        public void RequestExit()
        {
            GameSettings.Save(this);
            Accessibility.Speak("Saliendo del juego. ¡Hasta pronto!", interrupt: true);
            Task.Delay(350).ContinueWith(_ =>
            {
                OnRequestExit?.Invoke();
            });
        }

        /// <summary>
        /// Downloads the pending update and prepares a restart that installs it over the current folder.
        /// </summary>
        public async Task StartUpdate()
        {
            if (PendingUpdate == null) return;

            Accessibility.Speak("Descargando actualización, por favor espera...", interrupt: true);
            string? extracted = await UpdateChecker.DownloadAndPrepareUpdateAsync(
                PendingUpdate.DownloadUrl,
                percent => Console.WriteLine($"[Update] Descarga de actualización: {percent}%"));

            if (extracted == null)
            {
                Accessibility.Speak("No se pudo descargar la actualización. Inténtalo de nuevo más tarde.", interrupt: true);
                return;
            }

            Accessibility.Speak("Actualización lista. El juego se reiniciará para instalarla.", interrupt: true);
            ApplyUpdateAndRestart(extracted);
        }

        /// <summary>
        /// Manually checks GitHub for a newer release (from the Settings menu) and reports the result
        /// by voice. If a newer release exists, it is queued as PendingUpdate so the U key can install it.
        /// </summary>
        public async Task ManualUpdateCheck()
        {
            if (CurrentState == GameState.InGame) return;

            Accessibility.Speak("Buscando actualizaciones en GitHub, por favor espera...", interrupt: true);
            var info = await UpdateChecker.CheckForUpdateAsync();
            if (info != null)
            {
                PendingUpdate = info;
                Music.PlayTrack(0);
                Accessibility.Speak(
                    $"¡Hay una actualización disponible! Versión {info.Version}. " +
                    $"Pulsa la tecla U para actualizar mientras suena la música.",
                    interrupt: false);
            }
            else
            {
                Accessibility.Speak($"Tu juego ya está actualizado a la versión {GameInfo.CurrentVersion}.", interrupt: true);
            }
        }

        private void ApplyUpdateAndRestart(string extractedDir)
        {
            try
            {
                string gameDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                string batPath = Path.Combine(Path.GetTempPath(), "SSAG_updater_" + Guid.NewGuid().ToString("N") + ".bat");

                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("timeout /t 1 /nobreak >nul");
                sb.AppendLine($"xcopy /y /e /q \"{extractedDir}\" \"{gameDir}\"");
                sb.AppendLine($"start \"\" \"{exePath}\"");
                sb.AppendLine($"rmdir /s /q \"{extractedDir}\"");
                sb.AppendLine("del \"%~f0\"");
                File.WriteAllText(batPath, sb.ToString());

                Process.Start(new ProcessStartInfo(batPath) { UseShellExecute = true, CreateNoWindow = true });
            }
            catch
            {
                Accessibility.Speak("No se pudo preparar la actualización automática.", interrupt: true);
                return;
            }

            RequestExit();
        }

        public void Shutdown()
        {
            GameSettings.Save(this);
            IsRunning = false;
            Pursuit.StopAudio(AudioEngine);
            StopPowerupLoops();
            Music.Dispose();
            AudioEngine.Dispose();
            Accessibility.Dispose();
        }
    }
}
