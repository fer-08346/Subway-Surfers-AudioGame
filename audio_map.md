# Mapa de Audio y Catálogo de Efectos (SFX Audio Map)
**Subway Surfers Audiogame Binaural 3D**  
**Total de archivos analizados:** 168 archivos WAV en `/sfx`

---

## 1. Locomoción y Movimiento
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Paso Izquierdo (Vías)** | `Hr_run_leftFoot.wav` | Gravilla/balasto, paneo estéreo -0.15 |
| **Paso Derecho (Vías)** | `Hr_run_rightFoot.wav` | Gravilla/balasto, paneo estéreo +0.15 |
| **Paso Techo Tren (Izq)** | `Sub01_SFX_FootstepsTrainTop_LeftFoot.wav` | Resonancia metálica hueca sobre vagón |
| **Paso Techo Tren (Der)** | `Sub01_SFX_FootstepsTrainTop_RightFoot.wav` | Resonancia metálica hueca sobre vagón |
| **Paso con Super Sneakers** | `Hr_superSneakers_leftFoot.wav` | Impulso elástico y neumático |
| **Paso Techo con Sneakers** | `Sub01_SFX_FootstepsTrainTop_SuperSneakers_...` | Rebote elástico sobre metal |
| **Salto Estándar** | `Hr_run_jump.wav` | Impulso dinámico ascendente (2.5m) |
| **Salto Super Sneakers** | `Hr_superSneakers_jump.wav` | Impulso neumático de gran altitud (4.5m) |
| **Aterrizaje en Suelo** | `Hr_landing.wav` / `Hr_H_land.wav` | Contacto firme con el suelo |
| **Aterrizaje en Vagón** | `Sub01_SFX_FootstepsTrainTop_Landing.wav` | Impacto sordo sobre chapa metálica |
| **Rodar / Agacharse** | `Hr_run_roll.wav` | Deslizamiento con filtro paso bajo (3500Hz) |
| **Cambio de Carril** | `Hr_run_dodge.wav` / `Hr_swish...` | Paneo rápido hacia carril izquierdo (-0.8) o derecho (+0.8) |

---

## 2. Coleccionables y Monedas
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Recolección de Monedas** | `Hr_coin.wav` / `Hr_gui_coin.wav` | Incremento progresivo de tono por racha (1.0x a 1.5x) |
| **Caja Misteriosa** | `Hr_mysteryBoxOpen.wav` | Apertura con destellos armónicos |
| **Recompensa de Misión** | `Hr_missionReward.wav` | Fanfarria corta de logro |
| **Bling Box** | `blingbox_3_open_01.wav` | Apertura especial de cofre |

---

## 3. Potenciadores (Power-ups)
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Recoger Power-Up** | `Hr_powerUp.wav` | Tono ascendente de activación |
| **Fin de Power-Up** | `Hr_powerDown.wav` | Tono descendente de expiración |
| **Imán (Magnet)** | `Hr_magnet_mainLOOP.wav` | Bucle electromagnético continuo en 3D (10s) |
| **Mochila Jetpack** | `Subway_TLR_JetpackPause.wav` + `Hr_jetPack_mainLOOP.wav` | Ignición y vuelo espacial a 10m de altitud (12s) |
| **Tabla Hoverboard** | `Sub01_SFX_BubbleShield_Activation.wav` + `Ongoing` | Despliegue de escudo protector |
| **Destrucción de Tabla** | `Sub01_SFX_BubbleShield_Crashed.wav` | Disipación de energía e invulnerabilidad de 1.5s |
| **Multiplicador 2X** | `Subway_Metronome_MultiplierFly_Audio.wav` | Vuelo de multiplicador de puntos |

---

## 4. Persecución (Inspector y Perro)
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Inicio de Carrera** | `audioClip_GuardGameStart.wav` | Silbato y grito de persecución |
| **Alerta de Tropezón (1.5m)** | `audioClip_GuardProximity.wav` | Bucle en 3D posterior (180°): pisadas pesadas y jadeos del perro (10s) |

---

## 5. Trenes y Obstáculos
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Tren en Movimiento** | `Hr_trainPass.wav` | Bocina y rodadura con compresión Doppler a 12m/s |
| **Tropezón Lateral** | `Hr_stumble.wav` / `Hr_stumble_side.wav` | Impacto leve con advertencia de persecución |
| **Impacto Fatal / Captura** | `Hr_death.wav` / `Subway_BS_Stingers_Lose.wav` | Choque frontal definitivo y captura |

---

## 6. Interfaz y Menús
| Evento de Juego | Archivo WAV Asignado | Comportamiento Acústico |
|---|---|---|
| **Navegación de Menú** | `Hr_gui_tap.wav` | Clic suave en cambio de opción |
| **Menú de Ajustes** | `ui_audio_settings_open_audio.wav` | Apertura de configuración |
| **Selector de Ciudades** | `ui_audio_store_open_audio.wav` | Transición entre ciudades de la gira |
