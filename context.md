# Informe Técnico del Proyecto: Subway Surfers Binaural Audiogame (.NET 8 C#)

**Fecha de Actualización:** 20 de Agosto de 2026  
**Versión Actual (`GameInfo.CurrentVersion`):** `1.0.0`  
**Tecnología Base:** C# .NET 8 (Windows) + Motor Nativo Un4seen BASS (`bass.dll`) + NVDA Controller Client (`nvdaControllerClient64.dll`)  
**Repositorio:** `fer-08346/Subway-Surfers-AudioGame` (GitHub)  
**Documento Fuente:** [`Diseño Audiogame Subway Surfers C#.pdf`](./Diseño%20Audiogame%20Subway%20Surfers%20C#.pdf)  
**Estado de Integración:** Dificultad escalonada + Actualizador automático **mergeados en `master`** vía PR #1 (commit `526bb71`).

---

## 1. Resumen Ejecutivo y Nuevos Módulos

El proyecto ha sido extendido siguiendo una arquitectura modular en **C# .NET 8** especializada en accesibilidad para personas ciegas o con baja visión:
- **Economía & Inventario Persistente con Auto-Migración a AppData**: Configuración y progreso guardados universalmente en `%APPDATA%\SubwaySurfersAudioGame\config.json`. Incluye **migración automática de guardados heredados**: si un usuario tenía una partida anterior con `config.json` en la carpeta del juego, el nuevo ejecutable lo detecta automáticamente en el primer inicio y lo copia a su `%APPDATA%`, garantizando que nadie pierda sus monedas, tablas ni mejoras de nivel 1 a 5 sin tener que mover nada a mano.
- **Cierre Instantáneo**: Optimización del ciclo de vida y apagado rápido del hilo de interfaz (`RequestExit` reducido a 350ms) para una salida ágil del juego.
- **Físicas y Consumibles Estrictos**: Limitación real de inventario de tablas, enfriamiento de uso, cancelación rápida de salto (Fast Roll), control en el aire (Mid-Air Steering) y rebote metálico lateral contra vagones (Wall Bump).
- **Progresión World Tour**: Metas de etapa cada 2,000 metros con anuncio hablado de destino y transición musical fluida del catálogo de 45 ciudades (menú simplificado a 6 opciones limpias).
- **Persecución del Inspector con Audio 3D & Periodo de Gracia**: Posicionamiento dinámico a 1.8 metros a la espalda del jugador y periodo de gracia de 1.2 segundos tras el primer tropezón para evitar capturas accidentales por doble contacto.
- **Audio Binaural Dinámico y Eliminación de Ducking**: Ducking 100% eliminado para estabilidad absoluta del volumen, capa de viento sintético en tiempo real dependiente de la velocidad $v(t)$, y procesado de túneles con reverberación DX8.
- **Caza de Letras Diaria & Modo Calibración HRTF**: Letras S-U-R-F-E-R-S procedimentales con escala armónica, resumen de muerte hablado (Post-Mortem), y Escuela de Audio interactiva para calibración de audífonos.
- **Teclas de Diagnóstico en Vivo**: Atajos `[F1]` a `[F4]` para pruebas rápidas de transición de música, retroceso de país, tropezón del inspector y monedas.
- **Dificultad Escalonada (Selector + Rama por Distancia)**: Nuevo `DifficultyLevel` (Fácil / Normal / Difícil) persistido en `config.json`. La densidad de obstáculos y la frecuencia de trenes escalan con la distancia recorrida (`WorldGenerator`) de forma fiel al juego original, donde el reto crece con la velocidad. En Difícil los trenes estáticos pierden rampa progresivamente, forzando salto o cambio de carril.
- **Actualizador Automático desde Releases de GitHub**: Al iniciar, el juego consulta silenciosamente `releases/latest` del repositorio `fer-08346/Subway-Surfers-AudioGame`. Si hay una versión superior a `GameInfo.CurrentVersion` (`1.0.0`), anuncia por NVDA/SAPI *"Pulsa U para actualizar"* mientras suena el tema principal, y al confirmar descarga el ZIP portable, lo extrae y reinicia reemplazando la instalación. **Requisito de empaquetado:** los releases deben etiquetarse en semver (`vX.Y.Z`) e incluir un `.zip` portable cuyos archivos estén en la **raíz** del ZIP (no dentro de una subcarpeta), para que el script de reemplazo funcione.

---

## 2. Arquitectura Modular del Código

```
c:\Users\maria\Desktop\juegos\Subway Surfers\
│
├── Codigo_Fuente_CSharp/             <- Código fuente modular .NET 8
│   ├── Core/
│   │   ├── GameEngine.cs             <- Bucle a 60 FPS, colisiones, Post-Mortem, salida rápida, selección de dificultad y flujo de actualización (check silencioso + tecla U)
│   │   ├── Player.cs                 <- Físicas, Fast Roll, Mid-Air Steering y Rebotes
│   │   ├── Inventory.cs              <- Consumibles, niveles de mejora y letras del día
│   │   ├── EconomySystem.cs          <- Tienda, precios, validación y mejoras
│   │   ├── WorldTourSystem.cs        <- Hitos cada 2000m y transición entre países (+ pruebas manuales)
│   │   ├── WorldGenerator.cs         <- Vallas, trenes, monedas, letras, túneles y rampa de dificultad por distancia
│   │   ├── Obstacles.cs              <- Modelos de entidades de pista y zonas de túnel
│   │   ├── PursuitSystem.cs          <- Inspector y perro (posicionamiento 3D real, alerta y gracia de 1.2s)
│   │   ├── UpdateChecker.cs          <- Consulta/descarga de Releases de GitHub y versión actual
│   │   └── GameSettings.cs           <- Serialización y persistencia en %APPDATA%\SubwaySurfersAudioGame\config.json
│   ├── Audio/
│   │   ├── SpatialAudioEngine.cs     <- BASS 3D, capa de viento sintético y reverb de túnel
│   │   ├── SoundLibrary.cs           <- Carga y fallback dual de 160 clips WAV
│   │   ├── MusicManager.cs           <- Streaming BASS sin ducking de 45 temas MP3
│   │   ├── BinauralDSP.cs            <- Fórmulas matemáticas de HRTF y Doppler
│   │   └── AudioMap.cs               <- Catálogo estático de eventos acústicos
│   ├── Accessibility/
│   │   └── AccessibilityEngine.cs    <- P/Invoke nativo con NVDA / SAPI (sin ducking)
│   └── UI/
│       ├── AccessibleMenu.cs         <- Menú principal (6 opciones), tutoriales, ajustes y selector de dificultad (tecla U para actualizar)
│       ├── ShopMenu.cs               <- Tienda interactiva con NVDA
│       └── CalibrationMenu.cs        <- Escuela de audio y calibración HRTF
│
└── Subway Surfers Audiogame/         <- Paquete portable listo para comprimir en ZIP
    ├── SubwaySurfersAudioGame.exe     <- Ejecutable autónomo (sin consola, sin requerir .NET)
    ├── LEEME_Instrucciones.txt        <- Manual completo de controles y mecánicas
    ├── bass.dll                      <- Motor de audio 3D Un4seen BASS (64-bit)
    ├── nvdaControllerClient64.dll    <- Puente nativo con NVDA
    ├── SAAPI32.dll / ZDSRAPI_x64.dll <- Compatibilidad con lectores universales
    ├── sfx/                          <- 160 archivos de sonido WAV
    └── música Subway Surfers/         <- 45 pistas musicales World Tour
```

---

## 3. Especificaciones de Mecánicas y Fórmulas Implementadas

| Mecánica | Valor / Implementación | Detalle |
|---|---|---|
| **Almacenamiento Universal** | `%APPDATA%\SubwaySurfersAudioGame\config.json` | Independiente de la carpeta de instalación. Seguro ante actualizaciones. |
| **Apagado del Juego** | Salida en 350 ms | Cierre rápido y sin bloqueos de la interfaz gráfica. |
| **Velocidad Longitudinal** | $v(t) = \min(30.0, 10.0 + 3.5 \cdot \ln(1 + 0.05 \cdot t))$ | Aceleración desde $10\,\text{m/s}$ ($36\,\text{km/h}$) hasta $30\,\text{m/s}$ ($108\,\text{km/h}$). |
| **Capa de Viento Sintético** | Volumen: $0.03 + \frac{v - 10}{20} \cdot 0.32$, Frecuencia: $44.1\,\text{kHz} \cdot (0.85 + \frac{v - 10}{20} \cdot 0.45)$ | Bucle continuo de ruido filtrado en tiempo real con BASS StreamProcedure. |
| **Túneles y Reverb DSP** | Tramos de 350m cada 1,800m | BASS `EffectType.DXReverb` con tiempo de reverberación de 2.2s. |
| **Fast Roll (Cancelar Salto)** | Impulso: $v_y = -22\,\text{m/s}$, transición a `VerticalState.Rolling` | Flecha Abajo / S en el aire cancela el salto y fuerza la caída inmediata. |
| **Mid-Air Steering** | Intercambio de carriles activo en `Jumping` y `JetpackFlying` | Control total del desplazamiento lateral aéreo. |
| **Rebote Lateral contra Trenes** | Snapback de carril + sonido metálico + alerta de persecución | Chocar de lado contra un vagón no mata instantáneamente; rebota al carril de origen. |
| **Consumo de Tablas (Hoverboard)** | Requiere $N > 0$ tablas en inventario, 30s de duración, 35s de enfriamiento | Protege contra 1 choque fatal y otorga 1.5s de invulnerabilidad. |
| **Headstart (Cohete Turbo)** | Requiere $N > 0$ en inventario, activable en los primeros 150m con tecla `H` | $35\,\text{m/s}$ durante 1,000 metros con invulnerabilidad. |
| **Mejoras de Potenciadores** | Niveles 1 a 5 (Costos: 500, 1500, 3000, 5000 monedas) | Cada nivel suma duración (+3s a Imán, Jetpack, Sneakers; +4s a Multiplicador). |
| **Caza de Letras Diaria** | S-U-R-F-E-R-S | Campanilla ascendente por letra; completar otorga 1,500 monedas. |
| **Dificultad Escalonada** | `DifficultyLevel` (Fácil / Normal / Difícil) persistido; rampa por distancia | Espaciado entre secciones de 38→26m (Fácil), 30→14m (Normal) y 24→10m (Difícil); pesos de trenes estáticos/dinámicos crecen con $Z$, y la rampa de trenes estáticos cae de ~50% a ~15% con la distancia. |
| **Actualizador Automático** | `UpdateChecker` contra `releases/latest` de GitHub (`fer-08346/Subway-Surfers-AudioGame`), versión base `1.0.0` | Al iniciar comprueba versión; si hay superior, habla *"Pulsa U para actualizar"* con el tema principal y, al confirmar, descarga el ZIP, lo extrae y reinicia reemplazando la instalación. **Requiere** release semver (`vX.Y.Z`) con `.zip` portable en la raíz. |
| **Post-Mortem Hablado** | Causa, carril, velocidad exacta en m/s y km/h, metros, monedas y letras | Verbalización completa de la partida vía NVDA/SAPI. |

---

## 4. Mapeo de Teclas y Controles

| Tecla / Combinación | Acción en el Juego | Respuesta Sonora / Sintetizador |
|---|---|---|
| `Flechas Izquierda / Derecha` / `A / D` | Cambiar de carril (en tierra o en el aire) | Esquiva paneada o rebote metálico si hay un tren |
| `Flecha Arriba` / `W` | Saltar (vallas bajas / trenes con rampa) | Impulso ascendente (o super impulso con Sneakers) |
| `Flecha Abajo` / `S` | Rodar o Fast Roll (cancelar salto) | Deslizamiento con filtro paso bajo |
| `Barra Espaciadora` | Activar Hoverboard del inventario | Escudo de tabla (si hay disponibles y no está en recarga) |
| `H` | Activar Cohete Headstart (primeros 150m) | Cohete turbo a ultra velocidad por 1,000m |
| `L` | Consultar carril actual | Anuncio: *"Carril Izquierdo"*, *"Centro"* o *"Carril Derecho"* |
| `C` | Consultar monedas de carrera y banco | Anuncio: *"Monedas en carrera: [X]. Total en banco: [Y]"* |
| `S` | Consultar distancia, velocidad y país | Anuncio: *"Puntuación: [X] metros a [Y] km/h en [Ciudad]"* |
| `P` | Consultar potenciadores activos | Anuncio de cada potenciador y segundos restantes |
| `Shift + S` | Consultar récord personal | Anuncio: *"Récord personal: [X] metros"* |
| `F1` | [Prueba] Siguiente ciudad y música | Transición de música y meta de país |
| `F2` | [Prueba] Ciudad anterior | Retroceso de música |
| `F3` | [Prueba] Tropezón del inspector | Alerta inmediata a 1.8m a la espalda |
| `F4` | [Prueba] +500 monedas | Recarga rápida de banco |
| `U` | Actualizar juego (si hay release nuevo) | Descarga el ZIP portable y reinicia instalando la actualización |
| `Escape` | Pausa de carrera / Volver al menú anterior | Pausa del motor de audio |

---

## 5. Ejecución

👉 **Ejecutable compilado portable:** [`Subway Surfers Audiogame/SubwaySurfersAudioGame.exe`](file:///c:/Users/maria/Desktop/juegos/Subway%20Surfers/Subway%20Surfers%20Audiogame/SubwaySurfersAudioGame.exe)
