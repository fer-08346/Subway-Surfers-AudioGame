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

        private readonly Random _rng = new();
        public IReadOnlyList<TrackEntity> ActiveEntities => _activeEntities;
        public IReadOnlyList<TunnelZone> ActiveTunnels => _activeTunnels;

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
            // Spawn new sections ahead of the player (keep track populated up to 120m ahead)
            while (_nextSpawnZ < playerZ + 130.0f)
            {
                SpawnSection(_nextSpawnZ);
                _nextSpawnZ += _rng.Next(25, 45);
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
                        audioEngine.Play3D(AudioMap.Obstacles.TrainApproaching, dt.X, dt.Y, dt.Z, velZ: -dt.Speed, gain: 0.9f);
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

        private void SpawnSection(float baseZ)
        {
            int pattern = _rng.Next(0, 100);

            if (pattern < 35)
            {
                // Pattern 1: Barrier and Coins (Low / High barriers)
                SpawnBarriersAndCoins(baseZ);
            }
            else if (pattern < 70)
            {
                // Pattern 2: Static Train with Ramp or Jump Arc
                SpawnStaticTrainSection(baseZ);
            }
            else if (pattern < 90)
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

        private void SpawnStaticTrainSection(float baseZ)
        {
            Lane trainLane = (Lane)_rng.Next(-1, 2);
            bool hasRamp = _rng.Next(2) == 0;

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
