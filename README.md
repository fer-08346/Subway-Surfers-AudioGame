# 🏃 Subway Surfers (2012) - Audiogame Binaural 3D 🎧

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Audio Engine](https://img.shields.io/badge/Audio-Un4seen%20BASS-FF6F00?style=flat)](http://www.un4seen.com/)
[![Accessibility](https://img.shields.io/badge/Accessibility-NVDA%20%7C%20JAWS%20%7C%20SAPI5-0078D4?style=flat)](https://www.nvaccess.org/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Adaptación completa en formato **Audiogame Binaural 3D** del clásico **Subway Surfers (2012)** para Windows, desarrollada en **C# .NET 8** y diseñada para ser **100% accesible** mediante audífonos estéreo y lectores de pantalla (**NVDA**, **JAWS** y **SAPI5**).

---

## ✨ Características Principales

- 🎧 **Motor de Audio Espacial 3D Nativo (Un4seen BASS)**: Posicionamiento tridimensional estéreo de 3 carriles ($-3\,\text{m}$, $0\,\text{m}$, $+3\,\text{m}$), atenuación logarítmica por distancia y efecto Doppler psicoacústico en trenes en movimiento.
- 🗣️ **Accesibilidad Universal y Cero Ducking**:
  - Enlace directo por P/Invoke con `nvdaControllerClient64.dll` y fallback a `SAPI5`.
  - Sin ducking / sin bajadas de volumen: el juego mantiene un volumen sólido y constante durante toda la partida.
- 💨 **Capa Dinámica de Viento (Speed Wind)**: Generador procedural de viento en tiempo real que acelera en volumen y frecuencia según la velocidad ($10\,\text{m/s}$ a $30\,\text{m/s}$).
- 🚇 **Túneles con Reverberación Ambiental**: Efectos DSP de reverberación DX8 al ingresar a tramos subterráneos de la vía.
- 🛍️ **Tienda y Economía Completa**:
  - Compra de tablas Hoverboard y cohetes Headstart con monedas de carrera.
  - Mejoras permanentes de Nivel 1 a 5 para Imán, Jetpack, Super Zapatillas y Multiplicador.
  - Persistencia automática de progreso en `config.json`.
- 🛹 **Físicas Avanzadas y Consumibles**:
  - **Fast Roll**: Cancelación inmediata de salto en el aire al presionar `Abajo` para caer rodando.
  - **Mid-Air Steering**: Cambio de carril habilitado durante el salto.
  - **Rebote Lateral (Wall Bump)**: Chocar contra el costado de un tren rebota al carril de origen sin causar muerte instantánea.
- 🌍 **Progresión World Tour**:
  - Transición fluida cada 2,000 metros a través de las 45 ciudades de la gira mundial.
- 🔡 **Caza de Letras Diaria (Daily Word Hunt)**: Recolección de `S-U-R-F-E-R-S` con escala armónica y bonificación de 1,500 monedas.
- 🎙️ **Post-Mortem Hablado Detallado**: Resumen completo de causa de muerte, carril, velocidad en km/h, metros y saldo de monedas.
- 🔊 **Escuela de Audio y Calibración HRTF**: Modo interactivo para probar el audio 3D de cada elemento en aislamiento.

---

## 🎮 Controles del Juego

| Tecla / Combinación | Acción en el Juego |
|---|---|
| `Flecha Izquierda` / `A` | Moverse al carril izquierdo (en tierra o en el aire) |
| `Flecha Derecha` / `D` | Moverse al carril derecho (en tierra o en el aire) |
| `Flecha Arriba` / `W` | Saltar (vallas bajas / subir a trenes con rampa) |
| `Flecha Abajo` / `S` | Rodar por el suelo o **Fast Roll** (cancelar salto en el aire) |
| `Barra Espaciadora` | Activar Tabla Hoverboard del inventario |
| `H` | Activar Cohete Headstart (en los primeros 150m) |
| `L` | Consultar carril actual |
| `C` | Consultar monedas en carrera y saldo en banco |
| `S` | Consultar distancia, velocidad (km/h) y ciudad actual |
| `P` | Consultar potenciadores activos y segundos restantes |
| `Shift + S` | Consultar récord personal de distancia |
| `Escape` | Pausar la carrera / Volver al menú anterior |

---

## 📂 Arquitectura del Proyecto

```
Subway Surfers/
│
├── Codigo_Fuente_CSharp/             <- Proyecto C# .NET 8 modular
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
├── audio_map.md                      <- Catálogo de los 160 efectos de sonido WAV
├── context.md                        <- Informe técnico exhaustivo del proyecto
└── Diseño Audiogame Subway Surfers C#.pdf
```

---

## 🚀 Compilación y Ejecución

### Requisitos
- Windows 10 / 11 (64-bit).
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### Compilar Ejecutable Autocontenido
```powershell
dotnet publish Codigo_Fuente_CSharp/SubwaySurfersAudioGame.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./Subway Surfers Audiogame"
```

---

## 📜 Licencia
Este proyecto es de código abierto con propósitos educativos y de accesibilidad. Todos los derechos de los efectos de sonido y música original pertenecen a sus respectivos creadores (SYBO Games / Kiloo).
