using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SubwaySurfersAudioGame.Core;

namespace SubwaySurfersAudioGame
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string sfxDir = FindResourceDirectory("sfx");
            string musicDir = FindResourceDirectory("música Subway Surfers");

            var engine = new GameEngine(sfxDir, musicDir);

            using var form = new GameWindowForm(engine);
            Application.Run(form);

            engine.Shutdown();
        }

        private static string FindResourceDirectory(string folderName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();

            string[] candidates = new string[]
            {
                Path.Combine(baseDir, folderName),
                Path.Combine(currentDir, folderName),
                Path.Combine(baseDir, "..", folderName),
                Path.Combine(baseDir, "..", "..", folderName),
                Path.Combine(baseDir, "..", "..", "..", folderName),
                Path.Combine(baseDir, "..", "..", "..", "..", folderName),
                Path.Combine(currentDir, "SubwaySurfersAudioGame", folderName)
            };

            foreach (var candidate in candidates)
            {
                string fullPath = Path.GetFullPath(candidate);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.Combine(baseDir, folderName);
        }
    }

    public class GameWindowForm : Form
    {
        private readonly GameEngine _engine = null!;
        private readonly System.Windows.Forms.Timer _gameLoopTimer = new() { Interval = 16 };
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _prevTicks;

        public GameWindowForm(GameEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            Text = "Subway Surfers - Audiogame Binaural 3D";
            ClientSize = new Size(640, 360);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(20, 24, 32);
            KeyPreview = true;

            var label = new Label
            {
                Text = "Subway Surfers Audiogame Binaural\n\nEl juego está activo y sincronizado con tu lector de pantalla.\nUsa las Flechas / WASD para jugar.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular)
            };
            Controls.Add(label);

            KeyDown += OnGameKeyDown;
            FormClosing += (s, e) => _gameLoopTimer.Stop();

            _engine.OnRequestExit += () =>
            {
                try
                {
                    if (IsHandleCreated) BeginInvoke(new Action(Close));
                    else Application.Exit();
                }
                catch { }
            };

            _prevTicks = _stopwatch.ElapsedTicks;

            // High precision game loop timer at 60 FPS (~16ms)
            _gameLoopTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _gameLoopTimer.Tick += OnGameTick;
            _gameLoopTimer.Start();

            // Open main menu with speech & music
            _engine?.Menu?.OpenMainMenu();
        }

        private void OnGameTick(object? sender, EventArgs e)
        {
            long currentTicks = _stopwatch.ElapsedTicks;
            float dt = (float)(currentTicks - _prevTicks) / Stopwatch.Frequency;
            if (dt > 0.1f) dt = 0.1f;
            _prevTicks = currentTicks;

            _engine.Update(dt);
        }

        private void OnGameKeyDown(object? sender, KeyEventArgs e)
        {
            ConsoleKey key = e.KeyCode switch
            {
                Keys.Left => ConsoleKey.LeftArrow,
                Keys.Right => ConsoleKey.RightArrow,
                Keys.Up => ConsoleKey.UpArrow,
                Keys.Down => ConsoleKey.DownArrow,
                Keys.A => ConsoleKey.A,
                Keys.D => ConsoleKey.D,
                Keys.W => ConsoleKey.W,
                Keys.S => ConsoleKey.S,
                Keys.Space => ConsoleKey.Spacebar,
                Keys.Enter => ConsoleKey.Enter,
                Keys.Escape => ConsoleKey.Escape,
                Keys.L => ConsoleKey.L,
                Keys.C => ConsoleKey.C,
                Keys.P => ConsoleKey.P,
                Keys.H => ConsoleKey.H,
                Keys.Q => ConsoleKey.Q,
                Keys.U => ConsoleKey.U,
                Keys.F1 => ConsoleKey.F1,
                Keys.F2 => ConsoleKey.F2,
                Keys.F3 => ConsoleKey.F3,
                Keys.F4 => ConsoleKey.F4,
                _ => ConsoleKey.NoName
            };

            if (key != ConsoleKey.NoName)
            {
                ConsoleModifiers modifiers = 0;
                if (e.Shift) modifiers |= ConsoleModifiers.Shift;
                if (e.Control) modifiers |= ConsoleModifiers.Control;
                if (e.Alt) modifiers |= ConsoleModifiers.Alt;

                char keyChar = (char)e.KeyValue;
                if (!e.Shift && keyChar >= 'A' && keyChar <= 'Z')
                {
                    keyChar = char.ToLower(keyChar);
                }

                var keyInfo = new ConsoleKeyInfo(keyChar, key, e.Shift, e.Alt, e.Control);
                _engine.HandleInput(keyInfo);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}
