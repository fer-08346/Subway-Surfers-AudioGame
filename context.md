# Informe Técnico del Proyecto: Subway Surfers Binaural Audiogame (.NET 8 C#)

**Fecha de Actualización:** 18 de Agosto de 2026  
**Tecnología Base:** C# .NET 8 (Windows) + Motor Nativo Un4seen BASS (`bass.dll`) + NVDA Controller Client (`nvdaControllerClient64.dll`)  
**Documento Fuente:** [`Diseño Audiogame Subway Surfers C#.pdf`](./Diseño%20Audiogame%20Subway%20Surfers%20C#.pdf)

---

## 1. Resumen Ejecutivo y Nuevos Módulos

El proyecto ha sido extendido siguiendo una arquitectura modular en **C# .NET 8** especializada en accesibilidad para personas ciegas o con baja visión:
- **Economía & Inventario Persistente**: Tienda hablada con compra de consumibles (Hoverboards, Headstarts) y mejoras de Nivel 1 a 5 para Imán, Jetpack, Super Sneakers y Multiplicador.
- **Físicas y Consumibles Estrictos**: Limitación real de inventario de tablas, enfriamiento de uso, cancelación rápida de salto (Fast Roll), control en el aire (Mid-Air Steering) y rebote metálico lateral contra vagones (Wall Bump).
- **Progresión World Tour**: Metas de etapa cada 2,000 metros con anuncio hablado de destino y transición musical fluida del catálogo de 45 ciudades.
- **Audio Binaural Dinámico y Eliminación de Ducking**: Ducking 100% eliminado para estabilidad absoluta del volumen, capa de viento sintético en tiempo real dependiente de la velocidad $v(t)$, y procesado de túneles con reverberación DX8.
- **Caza de Letras Diaria & Modo Calibración HRTF**: Letras S-U-R-F-E-R-S procedimentales con escala armónica, resumen de muerte hablado (Post-Mortem), y Escuela de Audio interactiva para calibración de audífonos.

---

## 2. Arquitectura Modular del Código

```
c:\Users\maria\Desktop\juegos\Subway Surfers\
│
├── Codigo_Fuente_CSharp/             <- Código fuente modular .NET 8
│   ├── Core/
│   │   ├── GameEngine.cs             <- Bucle a 60 FPS, colisiones, Post-Mortem y estados
│   │   ├── Player.cs                 <- Físicas, Fast Roll, Mid-Air Steering y Rebotes
│   │   ├── Inventory.cs              <- Consumibles, niveles de mejora y letras del día
│   │   ├── EconomySystem.cs          <- Tienda, precios, validación y mejoras
│   │   ├── WorldTourSystem.cs        <- Hitos cada 2000m y transición entre países
│   │   ├── WorldGenerator.cs         <- Vallas, trenes, monedas, letras y túneles
│   │   ├── Obstacles.cs              <- Modelos de entidades de pista y zonas de túnel
│   │   ├── PursuitSystem.cs          <- Inspector y perro (alerta y ventana de 10s)
│   │   └── GameSettings.cs           <- Serialización y persistencia en config.json
│   ├── Audio/
│   │   ├── SpatialAudioEngine.cs     <- BASS 3D, capa de viento sintético y reverb de túnel
│   │   ├── SoundLibrary.cs           <- Carga y fallback dual de 160 clips WAV
│   │   ├── MusicManager.cs           <- Streaming BASS sin ducking de 45 temas MP3
│   │   ├── BinauralDSP.cs            <- Fórmulas matemáticas de HRTF y Doppler
│   │   └── AudioMap.cs               <- Catálogo estático de eventos acústicos
│   ├── Accessibility/
│   │   └── AccessibilityEngine.cs    <- P/Invoke nativo con NVDA / SAPI (sin ducking)
│   └── UI/
│       ├── AccessibleMenu.cs         <- Menú principal, selector de gira y tutoriales
│       ├── ShopMenu.cs               <- Tienda interactiva con NVDA
│       └── CalibrationMenu.cs        <- Escuela de audio y calibración HRTF
│
└── Subway Surfers Audiogame/         <- Paquete portable listo para comprimir en ZIP
    ├── SubwaySurfersAudioGame.exe     <- Ejecutable autónomo (sin consola)
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
| `Escape` | Pausa de carrera / Volver al menú anterior | Pausa del motor de audio |

---

## 5. Ejecución

👉 **Ejecutable compilado portable:** [`Subway Surfers Audiogame/SubwaySurfersAudioGame.exe`](file:///c:/Users/maria/Desktop/juegos/Subway%20Surfers/Subway%20Surfers%20Audiogame/SubwaySurfersAudioGame.exe)
