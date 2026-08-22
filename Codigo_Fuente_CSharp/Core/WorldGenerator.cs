using System;
using System.Collections.Generic;
using SubwaySurfersAudioGame.Audio;

namespace SubwaySurfersAudioGame.Core
{
    public class WorldGenerator
    {
        private readonly List<TrackEntity> _activeEntities = new();
        private readonly List<TunnelZone> _activeTunnels = new();
        private float _nextSpawnZ = 30.0f;
        private float _nextLetterSpawnZ = 120.0f;
        private int _letterSpawnIndex = 0;
        private float _nextTunnelSpawnZ = 800.0f;

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

        private readonly Random _rng = new();
        public IReadOnlyList<TrackEntity> ActiveEntities => _activeEntities;
        public IReadOnlyList<TunnelZone> ActiveTunnels => _activeTunnels;

        /// <summary>
        /// Tunables that shape how the obstacle density and train frequency ramp up with distance.
        /// maxGap/minGap = spacing between obstacle sections (meters); rampDistance = Z at which max difficulty is reached.
        /// The four weights are the relative spawning probabilities for each pattern family.
        /// </summary>
        private readonly struct DifficultyProfile
        {
            public float MaxGap { get; }
            public float MinGap { get; }
            public float RampDistance { get; }
            public float BarrierWeight { get; }
            public float StaticTrainWeight { get; }
            public float DynamicTrainWeight { get; }
            public float PowerUpWeight { get; }

            public DifficultyProfile(float maxGap, float minGap, float rampDistance,
                float barrier, float staticTrain, float dynamicTrain, float powerUp)
            {
                MaxGap = maxGap;
                MinGap = minGap;
                RampDistance = rampDistance;
                BarrierWeight = barrier;
                StaticTrainWeight = staticTrain;
                DynamicTrainWeight = dynamicTrain;
                PowerUpWeight = powerUp;
            }
        }

        private DifficultyProfile GetProfile()
        {
            return Difficulty switch
            {
                DifficultyLevel.Easy => new DifficultyProfile(38f, 26f, 5000f, 30f, 22f, 12f, 12f),
                DifficultyLevel.Normal => new DifficultyProfile(30f, 14f, 3000f, 28f, 35f, 20f, 10f),
                DifficultyLevel.Hard => new DifficultyProfile(24f, 10f, 1800f, 22f, 45f, 30f, 8f),
                _ => new DifficultyProfile(30f, 14f, 3000f, 28f, 35f, 20f, 10f)
            };
        }

        public void Reset()
        {
            _activeEntities.Clear();
            _activeTunnels.Clear();
            _nextSpawnZ = 30.0f;
            _nextLetterSpawnZ = 120.0f;
            _letterSpawnIndex = 0;
            _nextTunnelSpawnZ = 800.0f;
        }

        public bool IsInsideTunnel(float playerZ)
        {
            foreach (var t in _activeTunnels)
            {
                if (t.Contains(playerZ)) return true;
            }
            return false;
        }

        public bool CheckLateralTrainCollision(Lane targetLane, float playerZ, float playerY)
        {
            // If player is on train roof level (Y >= 3.5m), lateral switch onto another train roof or empty air is allowed
            if (playerY >= 3.5f) return false;

            foreach (var entity in _activeEntities)
            {
                if (entity.Lane == targetLane)
                {
                    if (entity is StaticTrain st)
                    {
                        if (playerZ >= (st.Z - 1.0f) && playerZ <= (st.Z + st.Length + 1.0f))
                        {
                            return true;
                        }
                    }
                    else if (entity is DynamicTrain dt)
                    {
                        if (playerZ >= (dt.Z - 2.0f) && playerZ <= (dt.Z + dt.Length + 2.0f))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void Update(float playerZ, SpatialAudioEngine audioEngine, Inventory inventory)
        {
            var profile = GetProfile();
            float progress = Math.Clamp(playerZ / profile.RampDistance, 0.0f, 1.0f);
            float gap = profile.MaxGap + (profile.MinGap - profile.MaxGap) * progress;

            // Spawn new sections ahead of the player (keep track populated up to 130m ahead).
            // Section spacing shrinks as difficulty/distance grows, increasing obstacle density.
            while (_nextSpawnZ < playerZ + 130.0f)
            {
                SpawnSection(_nextSpawnZ, progress);
                float variance = 0.85f + (float)_rng.NextDouble() * 0.3f;
                _nextSpawnZ += gap * variance;
            }

            // Spawn Daily Word Letters (e.g. S-U-R-F-E-R-S) every ~350m
            if (playerZ >= _nextLetterSpawnZ && !inventory.DailyWordCompleted)
            {
                SpawnLetter(playerZ + 60.0f, inventory);
                _nextLetterSpawnZ = playerZ + _rng.Next(300, 450);
            }

            // Spawn Tunnels every ~1800m lasting ~350m
            if (playerZ >= _nextTunnelSpawnZ)
            {
                _activeTunnels.Add(new TunnelZone
                {
                    StartZ = playerZ + 80.0f,
                    EndZ = playerZ + 80.0f + 350.0f
                });
                _nextTunnelSpawnZ = playerZ + 1800.0f;
            }

            // Cleanup old tunnels
            _activeTunnels.RemoveAll(t => t.EndZ < playerZ - 50.0f);

            // Update dynamic trains and sound spatialization
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];

                if (entity is DynamicTrain dt)
                {
                    // Move train towards player (12 m/s opposite)
                    dt.Z -= dt.Speed * (1.0f / 60.0f);

                    // Train horn audio cue when getting close (within 45m)
                    if (!dt.HornPlayed && (dt.Z - playerZ) < 45.0f && (dt.Z - playerZ) > 0f)
                    {
                        dt.HornPlayed = true;
                        audioEngine.Play3D(AudioMap.Obstacles.TrainApproaching, dt.X, dt.Y, dt.Z, velZ: -dt.Speed, gain: 0.9f, isVolumetric: true);
                    }

                    // Update engine loop instance position if active
                    if (dt.EngineLoopInstance != null)
                    {
                        audioEngine.UpdateChannel3DPosition(dt.EngineLoopInstance, dt.X, dt.Y, dt.Z, -dt.Speed);
                    }
                }

                // Cleanup entities left far behind
                if (entity.Z < playerZ - 30.0f)
                {
                    if (entity is DynamicTrain dynTrain && dynTrain.EngineLoopInstance != null)
                    {
                        audioEngine.StopInstance(dynTrain.EngineLoopInstance);
                        dynTrain.EngineLoopInstance = null;
                    }
                    _activeEntities.RemoveAt(i);
                }
            }
        }

        private void SpawnLetter(float spawnZ, Inventory inventory)
        {
            // Find next uncollected letter in the word
            for (int i = 0; i < inventory.DailyWord.Length; i++)
            {
                int checkIdx = (_letterSpawnIndex + i) % inventory.DailyWord.Length;
                if (!inventory.DailyLettersCollected[checkIdx])
                {
                    _letterSpawnIndex = (checkIdx + 1) % inventory.DailyWord.Length;
                    Lane lLane = (Lane)_rng.Next(-1, 2);
                    _activeEntities.Add(new LetterItem
                    {
                        Lane = lLane,
                        Z = spawnZ,
                        Y = 1.2f,
                        Letter = inventory.DailyWord[checkIdx],
                        LetterIndex = checkIdx
                    });
                    break;
                }
            }
        }

        private void SpawnSection(float baseZ, float progress)
        {
            var profile = GetProfile();

            // Scale train probabilities up with progress (distance) so the run gets harder over time,
            // faithful to the original game where density ramps with distance/speed.
            float barrierW = profile.BarrierWeight * (1.0f - progress * 0.20f);
            float staticW = profile.StaticTrainWeight * (1.0f + progress * 0.80f);
            float dynamicW = profile.DynamicTrainWeight * (1.0f + progress * 1.20f);
            float powerW = profile.PowerUpWeight;

            float total = barrierW + staticW + dynamicW + powerW;
            float roll = (float)_rng.NextDouble() * total;

            if (roll < barrierW)
            {
                // Pattern 1: Barrier and Coins (Low / High barriers)
                SpawnBarriersAndCoins(baseZ);
            }
            else if (roll < barrierW + staticW)
            {
                // Pattern 2: Static Train with Ramp or Jump Arc
                SpawnStaticTrainSection(baseZ, progress);
            }
            else if (roll < barrierW + staticW + dynamicW)
            {
                // Pattern 3: Dynamic Incoming Train on one lane
                SpawnDynamicTrainSection(baseZ);
            }
            else
            {
                // Pattern 4: Power-up Alley with Coins
                SpawnPowerUpSection(baseZ);
            }
        }

        private void SpawnBarriersAndCoins(float baseZ)
        {
            Lane blockedLane1 = (Lane)_rng.Next(-1, 2);
            Lane blockedLane2 = (Lane)_rng.Next(-1, 2);
            if (blockedLane1 == blockedLane2)
            {
                blockedLane2 = (Lane)(((int)blockedLane1 + 2) % 3 - 1);
            }

            // Spawn barrier 1
            if (_rng.Next(2) == 0)
            {
                _activeEntities.Add(new LowBarrier { Lane = blockedLane1, Z = baseZ });
                // Parabolic coin arc over low barrier
                for (int c = -2; c <= 2; c++)
                {
                    float arcY = MathF.Max(0.5f, 2.2f - (c * c) * 0.4f);
                    _activeEntities.Add(new CoinItem { Lane = blockedLane1, Z = baseZ + c * 2.0f, Y = arcY });
                }
            }
            else
            {
                _activeEntities.Add(new HighBarrier { Lane = blockedLane1, Z = baseZ });
            }

            // Spawn ground coin trail on a clear lane
            Lane clearLane = (Lane)(-((int)blockedLane1 + (int)blockedLane2));
            for (int i = 0; i < 6; i++)
            {
                _activeEntities.Add(new CoinItem { Lane = clearLane, Z = baseZ + i * 2.5f, Y = 0.5f });
            }
        }

        private void SpawnStaticTrainSection(float baseZ, float progress)
        {
            Lane trainLane = (Lane)_rng.Next(-1, 2);

            // Higher difficulty/distance -> fewer ramps, forcing the player to jump or switch lanes
            // instead of simply mounting the train roof. Ramp chance drops from ~50% down to ~15%.
            float rampChance = Math.Max(0.15f, 0.5f - progress * 0.35f);
            bool hasRamp = (float)_rng.NextDouble() < rampChance;

            _activeEntities.Add(new StaticTrain
            {
                Lane = trainLane,
                Z = baseZ,
                HasRamp = hasRamp
            });

            // Coins along the train roof
            for (int i = 0; i < 7; i++)
            {
                _activeEntities.Add(new CoinItem
                {
                    Lane = trainLane,
                    Z = baseZ + 2.0f + i * 2.2f,
                    Y = 4.3f // Above the 3.8m train roof
                });
            }

            // Barrier on adjacent lane
            Lane adjLane = (Lane)(((int)trainLane + 2) % 3 - 1);
            _activeEntities.Add(new LowBarrier { Lane = adjLane, Z = baseZ + 5.0f });
        }

        private void SpawnDynamicTrainSection(float baseZ)
        {
            Lane trainLane = (Lane)_rng.Next(-1, 2);
            _activeEntities.Add(new DynamicTrain
            {
                Lane = trainLane,
                Z = baseZ + 25.0f,
                Speed = 12.0f
            });

            // Coins on safe lane
            Lane safeLane = (Lane)(((int)trainLane + 2) % 3 - 1);
            for (int i = 0; i < 6; i++)
            {
                _activeEntities.Add(new CoinItem { Lane = safeLane, Z = baseZ + i * 2.5f, Y = 0.5f });
            }
        }

        private void SpawnPowerUpSection(float baseZ)
        {
            Lane pLane = (Lane)_rng.Next(-1, 2);
            PowerUpType[] types = { PowerUpType.Magnet, PowerUpType.Jetpack, PowerUpType.SuperSneakers, PowerUpType.Hoverboard, PowerUpType.Multiplier2x };
            PowerUpType selected = types[_rng.Next(types.Length)];

            _activeEntities.Add(new PowerUpItem
            {
                Lane = pLane,
                Z = baseZ,
                Y = 1.0f,
                Type = selected
            });

            // Trail of coins leading to power-up
            for (int i = -3; i <= 3; i++)
            {
                if (i != 0)
                {
                    _activeEntities.Add(new CoinItem { Lane = pLane, Z = baseZ + i * 2.5f, Y = 0.5f });
                }
            }
        }
    }
}
