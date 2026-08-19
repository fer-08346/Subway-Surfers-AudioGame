using System;

namespace SubwaySurfersAudioGame.Core
{
    public enum Lane
    {
        Left = -1,
        Center = 0,
        Right = 1
    }

    public enum VerticalState
    {
        Running,
        Jumping,
        Rolling,
        JetpackFlying
    }

    public class Player
    {
        public const float LaneDistance = 3.0f; // Distance between lanes in meters (-3m, 0m, +3m)
        public const float Gravity = -19.6f; // m/s^2

        // Lane Position & Smooth Interpolation
        public Lane CurrentLane { get; private set; } = Lane.Center;
        public Lane TargetLane { get; private set; } = Lane.Center;
        public float CurrentX { get; private set; } = 0.0f;
        public float TargetX => (float)TargetLane * LaneDistance;
        private float _laneTransitionTimer = 0.0f;
        private float _laneTransitionDuration = 0.18f; // 180 ms nominal
        private float _laneStartX = 0.0f;

        // Rebound / Wall Bump State
        public bool IsRebounding { get; private set; } = false;
        public float ReboundTimer { get; private set; } = 0.0f;

        // Longitudinal (Z) Progression
        public float Z { get; set; } = 0.0f;
        public float Speed { get; set; } = 10.0f; // Starts at 10.0 m/s (36 km/h)
        public float ElapsedGameTime { get; set; } = 0.0f;
        public float Score { get; set; } = 0.0f;
        public int Coins { get; set; } = 0;

        // Vertical (Y) Physics
        public float Y { get; private set; } = 0.0f;
        public float BaseGroundY { get; set; } = 0.0f; // 0 on tracks, 3.8 on train roof
        public float VelocityY { get; private set; } = 0.0f;
        public VerticalState State { get; private set; } = VerticalState.Running;
        public float RollTimer { get; private set; } = 0.0f;
        public const float RollDuration = 0.50f; // 500 ms

        // Footstep timing
        public float FootstepTimer { get; set; } = 0.0f;
        public bool NextFootIsLeft { get; set; } = true;

        // Power-ups
        public float MagnetTimer { get; set; } = 0.0f;
        public float JetpackTimer { get; set; } = 0.0f;
        public float SuperSneakersTimer { get; set; } = 0.0f;
        public float HoverboardTimer { get; set; } = 0.0f;
        public float HoverboardCooldown { get; set; } = 0.0f;
        public float Multiplier2xTimer { get; set; } = 0.0f;
        public float InvulnerabilityTimer { get; set; } = 0.0f;
        public float HeadstartDistanceRemaining { get; set; } = 0.0f;

        public bool HasMagnet => MagnetTimer > 0;
        public bool HasJetpack => JetpackTimer > 0;
        public bool HasSuperSneakers => SuperSneakersTimer > 0;
        public bool HasHoverboard => HoverboardTimer > 0;
        public bool HasMultiplier2x => Multiplier2xTimer > 0;
        public bool HasHeadstart => HeadstartDistanceRemaining > 0;
        public bool IsInvulnerable => InvulnerabilityTimer > 0 || HasHeadstart;

        // Coin streak pitch ladder
        public int CoinStreak { get; set; } = 0;
        public float CoinStreakResetTimer { get; set; } = 0.0f;

        public void Reset()
        {
            CurrentLane = Lane.Center;
            TargetLane = Lane.Center;
            CurrentX = 0.0f;
            _laneStartX = 0.0f;
            _laneTransitionTimer = 0.0f;
            IsRebounding = false;
            ReboundTimer = 0.0f;

            Z = 0.0f;
            Speed = 10.0f;
            ElapsedGameTime = 0.0f;
            Score = 0.0f;
            Coins = 0;
            Y = 0.0f;
            BaseGroundY = 0.0f;
            VelocityY = 0.0f;
            State = VerticalState.Running;
            RollTimer = 0.0f;
            FootstepTimer = 0.0f;
            NextFootIsLeft = true;

            MagnetTimer = 0.0f;
            JetpackTimer = 0.0f;
            SuperSneakersTimer = 0.0f;
            HoverboardTimer = 0.0f;
            HoverboardCooldown = 0.0f;
            Multiplier2xTimer = 0.0f;
            InvulnerabilityTimer = 0.0f;
            HeadstartDistanceRemaining = 0.0f;
            CoinStreak = 0;
            CoinStreakResetTimer = 0.0f;
        }

        public bool SwitchLaneLeft()
        {
            if ((int)TargetLane > (int)Lane.Left)
            {
                _laneStartX = CurrentX;
                TargetLane = (Lane)((int)TargetLane - 1);
                _laneTransitionTimer = 0.0f;
                // Lane transition duration reduces with speed (200ms down to 150ms)
                _laneTransitionDuration = Math.Clamp(0.20f - (Speed - 10.0f) * 0.0025f, 0.15f, 0.20f);
                return true;
            }
            return false;
        }

        public bool SwitchLaneRight()
        {
            if ((int)TargetLane < (int)Lane.Right)
            {
                _laneStartX = CurrentX;
                TargetLane = (Lane)((int)TargetLane + 1);
                _laneTransitionTimer = 0.0f;
                _laneTransitionDuration = Math.Clamp(0.20f - (Speed - 10.0f) * 0.0025f, 0.15f, 0.20f);
                return true;
            }
            return false;
        }

        public void TriggerRebound(Lane originLane)
        {
            TargetLane = originLane;
            CurrentLane = originLane;
            CurrentX = (float)originLane * LaneDistance;
            _laneStartX = CurrentX;
            _laneTransitionTimer = _laneTransitionDuration;
            IsRebounding = true;
            ReboundTimer = 0.35f;
        }

        public bool Jump()
        {
            if (HasJetpack) return false;

            if (State == VerticalState.Running || State == VerticalState.Rolling)
            {
                State = VerticalState.Jumping;
                RollTimer = 0.0f;
                if (HasSuperSneakers)
                {
                    // 4.5m max height in 900ms: v_y0 = 8.82 m/s
                    VelocityY = 8.82f;
                }
                else
                {
                    // 2.5m max height in 600ms: v_y0 = 5.88 m/s
                    VelocityY = 5.88f;
                }
                return true;
            }
            return false;
        }

        public bool Roll()
        {
            if (HasJetpack) return false;

            if (State == VerticalState.Jumping || Y > BaseGroundY)
            {
                // FAST ROLL: Cancel jump elevation immediately and slam down
                VelocityY = -22.0f;
                State = VerticalState.Rolling;
                RollTimer = RollDuration;
                return true;
            }
            else if (State == VerticalState.Running)
            {
                State = VerticalState.Rolling;
                RollTimer = RollDuration;
                return true;
            }
            return false;
        }

        public bool TryActivateHoverboard(Inventory inventory, out string reason)
        {
            if (HasHoverboard)
            {
                reason = $"La tabla ya está activa. {(int)HoverboardTimer} segundos restantes.";
                return false;
            }

            if (HoverboardCooldown > 0)
            {
                reason = $"Tabla en recarga: {(int)HoverboardCooldown + 1} segundos para poder usar otra.";
                return false;
            }

            if (inventory.HoverboardCount <= 0)
            {
                reason = "No tienes tablas Hoverboard en inventario. Cómpralas en la Tienda.";
                return false;
            }

            inventory.HoverboardCount--;
            HoverboardTimer = 30.0f;
            HoverboardCooldown = 35.0f; // 30s active + 5s cooldown
            reason = $"¡Tabla Hoverboard activada! Te quedan {inventory.HoverboardCount} en inventario.";
            return true;
        }

        public bool TryActivateHeadstart(Inventory inventory, out string reason)
        {
            if (Z > 150.0f)
            {
                reason = "El cohete Headstart solo puede activarse en los primeros 150 metros de la carrera.";
                return false;
            }

            if (HasHeadstart)
            {
                reason = "Headstart ya está activo.";
                return false;
            }

            if (inventory.HeadstartCount <= 0)
            {
                reason = "No tienes cohetes Headstart en inventario. Cómpralos en la Tienda.";
                return false;
            }

            inventory.HeadstartCount--;
            HeadstartDistanceRemaining = 1000.0f; // 1,000 meters turbo boost
            InvulnerabilityTimer = 1000.0f / 35.0f + 2.0f;
            reason = $"¡Cohete Headstart activado! Volando 1,000 metros a ultra velocidad. Te quedan {inventory.HeadstartCount}.";
            return true;
        }

        public void Update(float dt)
        {
            ElapsedGameTime += dt;

            // Rebound timer
            if (IsRebounding)
            {
                ReboundTimer -= dt;
                if (ReboundTimer <= 0) IsRebounding = false;
            }

            // Headstart turbo mechanics
            if (HasHeadstart)
            {
                Speed = 35.0f;
                HeadstartDistanceRemaining -= Speed * dt;
                if (HeadstartDistanceRemaining <= 0)
                {
                    HeadstartDistanceRemaining = 0.0f;
                }
            }
            else
            {
                // Logarithmic Speed Scaling Formula: v(t) = min(30.0, 10.0 + 3.5 * ln(1 + 0.05 * t))
                float calculatedSpeed = 10.0f + 3.5f * MathF.Log(1.0f + 0.05f * ElapsedGameTime);
                Speed = Math.Clamp(calculatedSpeed, 10.0f, 30.0f);
            }

            // Longitudinal movement
            float dz = Speed * dt;
            Z += dz;
            float multiplier = HasMultiplier2x ? 2.0f : 1.0f;
            Score += dz * multiplier;

            // Lane Interpolation (Ease-Out)
            if (CurrentX != TargetX && !IsRebounding)
            {
                _laneTransitionTimer += dt;
                float progress = Math.Clamp(_laneTransitionTimer / _laneTransitionDuration, 0.0f, 1.0f);
                float easeOut = 1.0f - (1.0f - progress) * (1.0f - progress);
                CurrentX = _laneStartX + (TargetX - _laneStartX) * easeOut;
                if (progress >= 1.0f)
                {
                    CurrentLane = TargetLane;
                    CurrentX = TargetX;
                }
            }

            // Power-ups countdown
            if (MagnetTimer > 0) MagnetTimer = Math.Max(0, MagnetTimer - dt);
            if (SuperSneakersTimer > 0) SuperSneakersTimer = Math.Max(0, SuperSneakersTimer - dt);
            if (HoverboardTimer > 0) HoverboardTimer = Math.Max(0, HoverboardTimer - dt);
            if (HoverboardCooldown > 0) HoverboardCooldown = Math.Max(0, HoverboardCooldown - dt);
            if (Multiplier2xTimer > 0) Multiplier2xTimer = Math.Max(0, Multiplier2xTimer - dt);
            if (InvulnerabilityTimer > 0) InvulnerabilityTimer = Math.Max(0, InvulnerabilityTimer - dt);

            // Jetpack Physics
            if (JetpackTimer > 0)
            {
                JetpackTimer = Math.Max(0, JetpackTimer - dt);
                State = VerticalState.JetpackFlying;
                // Ascend or stay at 10.0m altitude
                if (Y < 10.0f) Y = Math.Min(10.0f, Y + 12.0f * dt);
                else Y = 10.0f;
                VelocityY = 0.0f;
                return;
            }
            else if (State == VerticalState.JetpackFlying && JetpackTimer <= 0)
            {
                // Descend back from jetpack
                Y -= 10.0f * dt;
                if (Y <= BaseGroundY)
                {
                    Y = BaseGroundY;
                    State = VerticalState.Running;
                }
                return;
            }

            // Vertical Physics & Rolling
            if (State == VerticalState.Jumping)
            {
                Y += VelocityY * dt;
                VelocityY += Gravity * dt;

                if (Y <= BaseGroundY)
                {
                    Y = BaseGroundY;
                    VelocityY = 0.0f;
                    State = VerticalState.Running;
                }
            }
            else if (State == VerticalState.Rolling)
            {
                RollTimer -= dt;
                if (Y > BaseGroundY)
                {
                    Y += VelocityY * dt;
                    VelocityY += Gravity * dt;
                    if (Y <= BaseGroundY)
                    {
                        Y = BaseGroundY;
                        VelocityY = 0.0f;
                    }
                }
                if (RollTimer <= 0.0f)
                {
                    State = VerticalState.Running;
                }
            }
            else
            {
                // Running on ground / train roof
                if (Y > BaseGroundY)
                {
                    Y += Gravity * dt;
                    if (Y <= BaseGroundY) Y = BaseGroundY;
                }
                else
                {
                    Y = BaseGroundY;
                }
            }

            // Coin streak reset timer
            if (CoinStreakResetTimer > 0)
            {
                CoinStreakResetTimer -= dt;
                if (CoinStreakResetTimer <= 0) CoinStreak = 0;
            }
        }
    }
}
