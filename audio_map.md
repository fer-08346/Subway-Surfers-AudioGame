# Mapa de Audio y Catálogo de Efectos (SFX Audio Map)
**Subway Surfers Audiogame Binaural 3D**  
**Total de archivos analizados:** 168 archivos WAV en `/sfx`  
**Última actualización:** 20 de Agosto de 2026

> **Leyenda:** ✅ = Sonido ya cableado y activo en el código (`AudioMap.cs` / `GameEngine.cs`).  
> 🟡 = Archivo presente en `/sfx` pero aún no asignado a un evento del juego (candidato para futuras mecánicas como modos especiales, dificultad o eventos de temporada).

---

## 1. Locomoción y Movimiento ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Paso Izquierdo (Vías)** | `Hr_run_leftFoot.wav` | Gravilla/balasto, paneo estéreo -0.15 |
| **Paso Derecho (Vías)** | `Hr_run_rightFoot.wav` | Gravilla/balasto, paneo estéreo +0.15 |
| **Paso Techo Tren (Izq)** | `Sub01_SFX_FootstepsTrainTop_LeftFoot.wav` | Resonancia metálica hueca sobre vagón |
| **Paso Techo Tren (Der)** | `Sub01_SFX_FootstepsTrainTop_RightFoot.wav` | Resonancia metálica hueca sobre vagón |
| **Paso con Super Sneakers** | `Hr_superSneakers_leftFoot.wav` / `Hr_superSneakers_rightFoot.wav` | Impulso elástico y neumático |
| **Paso Techo con Sneakers** | `Sub01_SFX_FootstepsTrainTop_SuperSneakers_LeftFoot.wav` / `..._RightFoot.wav` | Rebote elástico sobre metal |
| **Aterrizaje Sneakers Suelo** | `Sub01_SFX_Character_SuperSneakerLandingGround_01.wav` | Impacto elástico al tocar vías |
| **Aterrizaje Sneakers Tren** | `Sub01_SFX_Character_SuperSneakerLandingTrain_01.wav` | Impacto elástico sobre chapa |
| **Salto Estándar** | `Hr_run_jump.wav` | Impulso dinámico ascendente (2.5m) |
| **Salto Super Sneakers** | `Hr_superSneakers_jump.wav` | Impulso neumático de gran altitud (4.5m) |
| **Aterrizaje en Suelo** | `Hr_landing.wav` / `Hr_H_land.wav` | Contacto firme con el suelo |
| **Aterrizaje en Vagón** | `Sub01_SFX_FootstepsTrainTop_Landing.wav` | Impacto sordo sobre chapa metálica |
| **Rodar / Agacharse** | `Hr_run_roll.wav` | Deslizamiento con filtro paso bajo (3500Hz) |
| **Cambio de Carril** | `Hr_run_dodge.wav` / `Hr_swishCShort.wav` / `Hr_swishFMid.wav` / `Hr_swishDLong.wav` | Paneo rápido hacia carril (-0.8 / +0.8) |

---

## 2. Coleccionables y Monedas ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Recolección de Monedas** | `Hr_coin.wav` / `Hr_gui_coin.wav` | Incremento progresivo de tono por racha (1.0x a 1.5x) |
| **Caja Misteriosa** | `Hr_mysteryBoxOpen.wav` | Apertura con destellos armónicos |
| **Recompensa de Misión** | `Hr_missionReward.wav` | Fanfarria corta de logro |
| **Bling Box** | `blingbox_3_open_01.wav` | Apertura especial de cofre |
| **Desbloqueo** | `Hr_unlock.wav` | Confirmación de desbloqueo de contenido |
| **Letras de la Palabra** | `Hr_slideInLetters.wav` | Campanilla ascendente de letra diaria |
| **Intro Headstart (lata)** | `Hr_intro_canShake.wav` / `Hr_intro_canSpray.wav` | Agitado y pulverizado de lata previo al turbo |

---

## 3. Potenciadores (Power-ups) ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Recoger Power-Up** | `Hr_powerUp.wav` | Tono ascendente de activación |
| **Fin de Power-Up** | `Hr_powerDown.wav` | Tono descendente de expiración |
| **Imán (Magnet)** | `Hr_magnet_mainLOOP.wav` | Bucle electromagnético continuo en 3D (10s) |
| **Mochila Jetpack** | `Subway_TLR_JetpackPause.wav` + `Hr_jetPack_mainLOOP.wav` | Ignición y vuelo espacial a 10m (12s) |
| **Tabla Hoverboard** | `Sub01_SFX_BubbleShield_Activation.wav` + `Ongoing` / `Ongoing_Loud` | Despliegue y escudo protector continuo |
| **Destrucción de Tabla** | `Sub01_SFX_BubbleShield_Crashed.wav` | Disipación de energía e invulnerabilidad 1.5s |
| **Multiplicador 2X** | `Subway_Metronome_MultiplierFly_Audio.wav` | Vuelo de multiplicador de puntos |
| **Cohete Headstart** | `Hr_turboheadstart.wav` | Ignición turbo a ultra velocidad |
| **Carga de Tabla (race)** | `Sub01_SFX_Race_Pickup_BoardCharge.wav` | Carga de escudo en modo carrera 🟡 |

---

## 4. Persecución (Inspector y Perro) ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Inicio de Carrera** | `audioClip_GuardGameStart.wav` | Silbato y grito de persecución |
| **Alerta de Tropezón** | `audioClip_GuardProximity.wav` | Bucle en 3D posterior (180°): pisadas pesadas y jadeos (10s) |
| **Inicio Vine / Planta** | `audioClip_VineGameStart.wav` | Grito de la invasión de plantas 🟡 |
| **Proximidad Vine** | `audioClip_VineProximity.wav` | Bucle de peligro de plantas 🟡 |

---

## 5. Trenes y Obstáculos ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Tren en Movimiento** | `Hr_trainPass.wav` | Bocina y rodadura con compresión Doppler a 12m/s |
| **Tropezón Lateral** | `Hr_stumble.wav` / `Hr_stumble_side.wav` / `Hr_stumble_bush.wav` | Impacto leve con advertencia de persecución |
| **Impacto Fatal / Captura** | `Hr_death.wav` / `Hr_death_bodyfall.wav` / `Hr_death_hitCam.wav` / `Subway_BS_Stingers_Lose.wav` | Choque frontal definitivo y captura |
| **Patada / Kick** | `Hr_H_kick.wav` | Golpe seco de impulso |
| **Morph del Subway** | `Hr_subway morph.wav` | Transición de escenario |

---

## 6. Interfaz y Menús ✅
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Navegación de Menú** | `Hr_gui_tap.wav` | Clic suave en cambio de opción |
| **Menú de Ajustes** | `ui_audio_settings_open_audio.wav` | Apertura de configuración |
| **Selector de Tienda** | `ui_audio_store_open_audio.wav` | Transición a tienda |
| **Misiones** | `ui_audio_missions_open_audio.wav` / `ui_audio_reward_meter_*.wav` | Apertura y medidor de recompensas 🟡 |
| **Otros menús** | `ui_audio_mailbox_open_audio.wav` / `ui_audio_me_open_audio.wav` / `ui_audio_seasonhunt_open_audio.wav` / `ui_audio_toprun_open_audio.wav` | Apertura de secciones 🟡 |
| **Claim / Evento** | `Sub01_SFX_UI_ClaimQuest_01.wav` / `Sub01_SFX_UI_GeneralClaim_01.wav` / `Sub01_SFX_UI_OpenEvent_01.wav` | Reclamar recompensa 🟡 |
| **Cámara / Compartir** | `Sub01_SFX_ShareFeature_CameraFlash_01.wav` | Flash de cámara al compartir 🟡 |
| **Celebración TopRun** | `audioClipInfo_TournamentTopRun_LadderCelebration.wav` / `..._LadderMoveUp.wav` / `..._LadderPopIn.wav` / `..._LadderPopOut.wav` | Subida de escalera torneo 🟡 |

---

## 7. Catálogo de Eventos Especiales (Disponibles 🟡, no cableados)
Estos clips provienen de modos de juego del Subway Surfers original (Temporada, Chaos, Batalla, Floor Is Lava, Plant Invasion, TOT). Están disponibles para extender el audiogame con modos especiales, rampas de dificultad más agresivas o eventos de temporada.

### 7.1 Chaos Mode (`Subway_BS_*`)
`Subway_BS_Chaos_Finished_v1` · `Subway_BS_Chaos_GlitchLoop_v2` · `Subway_BS_Chaos_Laugh_v1` · `Subway_BS_Chaos_NegativeArrow_v1` · `Subway_BS_Chaos_StartAttack_v3` · `Subway_BS_FirstPlayerAttack_Impact_v1` · `Subway_BS_FirstPlayerAttack_Launch_v1` · `Subway_BS_IntroAmbience` · `Subway_BS_Intro_CountDown_v1` · `Subway_BS_JumpAhead_Jump_v1` · `Subway_BS_JumpAhead_Land_v1` · `Subway_BS_LaneStumble_Launch` · `Subway_BS_LaneStumble_RollLoop_v1` · `Subway_BS_ObjectDespawn_v1` · `Subway_BS_PowerOutPoof` · `Subway_BS_Power_Randomization_v1` · `Subway_BS_ShieldScreen_Flag_v2` · `Subway_BS_SpeedDrink_v2` · `Subway_BS_SpeedPad_Up_v1` · `Subway_BS_SpeedPad_Down_v1` · `Subway_BS_Stingers_Win` · `Subway_BS_Stingers_Podium` · `Subway_BS_Stumble_Var_1` · `Subway_BS_Stumble_Var_2` · `Subway_BS_ThrowSlow_Land_v1` · `Subway_BS_ThrowSlow_Spawn_v1` · `Subway_BS_ThrowSlow_Throw_v1` · `Subway_BS_VisualImpairment_Affected_v1` · `Subway_BS_VisualImpairment_Launch_v1` · `Subway_BS_Battery_Charge_v1`

*Rol inferido:* ataques entre jugadores, cuenta regresiva, stumbles alternativos, pads de velocidad, power randomization y stingers de victoria/derrota para modos competitivos.

### 7.2 Floor Is Lava (`Subway_SFX_FloorIsLava_*` / `SubwayCityFloorIsLava_*`)
`Subway_SFX_FloorIsLava_BlockerUnfold` · `Subway_SFX_FloorIsLava_DangerousSurface` · `Subway_SFX_FloorIsLava_FallingTrain_Drag` · `Subway_SFX_FloorIsLava_IncreaseDifficulty_01/02/04` · `SubwayCityFloorIsLava_AudioClip_LavaBoard`

*Rol inferido:* superficie de lava que aumenta dificultad progresivamente (útil para alimentar la rampa de dificultad por distancia).

### 7.3 Plant Invasion / Vine (`Subway_SFX_PlantInvasion_*` / `SubwayCityPlantInvasion_*` / `audioClip_Vine*`)
`Subway_SFX_PlantInvasion_BlockerUnfold` · `Subway_SFX_PlantInvasion_DangerousSurface` · `Subway_SFX_PlantInvasion_DifficultySectionUp` · `Subway_SFX_PlantInvasion_FallingTrain_Drag` · `Subway_SFX_PlantInvasion_FallingTrain_Vine` · `SubwayCityPlantInvasion_AudioClip_IntroVine` · `SubwayCityPlantInvasion_AudioClip_ShakingLegs` · `SubwayCityPlantInvasion_AudioClip_VineBoard` · `SubwayCityPlantInvasion_AudioClip_VinePoke` · `audioClip_VineGameStart` · `audioClip_VineProximity`

*Rol inferido:* eventos de invasión de plantas con secciones de dificultad creciente.

### 7.4 TOT / Battle Pass (`Sub01_SFX_TOT_*`)
`Sub01_SFX_TOT_BigBarrierDrop` · `Sub01_SFX_TOT_HalfTrainDrop` · `Sub01_SFX_TOT_SmallBarrierDrop` · `Sub01_SFX_TOT_TrainDisappear` · `Sub01_SFX_TOT_Bob_Intro` · `Sub01_SFX_TOT_Bob_Taunt_Crash` · `Sub01_SFX_TOT_Bob_Taunt_Death` · `Sub01_SFX_TOT_Bob_Teleport` · `Sub01_SFX_TOT_Pickup_BigTreat` · `Sub01_SFX_TOT_Pickup_SmallTreat` · `Sub01_SFX_TOT_Pickup_StinkyFish`

*Rol inferido:* caída de barreras y trenes, apareción/desaparición, y recolectables de temporada (Treats).

### 7.5 Misterio / Puntuación / Progreso (`Subway_SFX_Mystery_*` / `Subway_Metronome_*` / `Subway_ProgressBars_*` / `Subway_Gauntlet_*` / `Subway_PvP_Pop`)
`Subway_SFX_Mystery_BarrierRetacting_v2` · `Subway_SFX_Mystery_DifficultySectionUp` · `Subway_SFX_Mystery_Point` · `Subway_SFX_Close_v1` · `Subway_Metronome_Open_Audio` · `Subway_Metronome_Pop1..Pop5_Audio` · `Subway_Metronome_RewardAppearance_Audio` · `Subway_Metronome_SprayTransition_Audio` · `Subway_Metronome_Wheel_Audio` · `Subway_ProgressBars_Increase_Mono` · `Subway_ProgressBars_Stop_Mono` · `Subway_ProgressBars_Unlock_v2Mono` · `Subway_Gauntlet_Collect_v1` · `Subway_Gauntlet_Complete_v1` · `Subway_PvP_Pop`

*Rol inferido:* popups de multiplicador, barras de progreso, recolectar/completar gauntlet y duelos PvP.

### 7.6 Utilidades y miscelánea
`music_winterxtreme.wav` (tema invernal loop) · `deat mystery combo.wav` (combo de muerte) · `pling inbetween longer high.wav` (transición aguda) · `silence_sound.wav` (silencio/separador de eventos) · `Hr_gui_tap.wav` (ya usado en UI) · `Subway_TLR_HourglassPickUp` · `Subway_TLR_Tick` · `Subway_TLR_TimesUp_Whistle` (reloj de modo TLR)

---

## 8. Resumen de cobertura
- **✅ Cableados y activos:** ~45 clips (locomoción, monedas, power-ups, persecución, trenes, UI básica).
- **🟡 Disponibles (modos especiales / temporada):** ~120 clips listos para extender el juego con modos Chaos, Floor Is Lava, Plant Invasion, TOT y rampas de dificultad más ricas.
