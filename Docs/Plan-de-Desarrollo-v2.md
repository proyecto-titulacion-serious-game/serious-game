# Plan de Desarrollo v2 — Serious Game VR "TITA"
### Ajustado con assets ya importados en el proyecto

> Revisión del Plan original (31-may-2026) basada en inventario real de `Assets/`.
> **Principio:** todo lo que se puede hacer con assets ya presentes en el proyecto, se hace sin importar nada nuevo.
> Los únicos recursos externos pendientes son **clips de audio** (no existen en el proyecto ningún .wav/.mp3 de circuitos).

---

## Inventario de assets clave disponibles

| Categoría | Ruta | Qué ofrece |
|-----------|------|-----------|
| **Modelos electrónicos HD** | `Assets/Resources Vol.2 - Electronics/` | 20+ FBX listos: Battery 9v, Capacitor, Coil, Controller Board, LED (A-E), LED Matrix, Microchip, Potentiometer, Relay, Segment Display, Transistor, Wire, Button, Stepper Motor, Fan… |
| **Prefabs de componentes** | `Assets/circuit/prefabs/` | resistor, resistorVertical, LED (G/R/Y), capacitor (3 colores), transistor, wire1/2 |
| **Prefabs entregados** | `Assets/Prefabs/Delivered/` | Resistor, LED (4 variantes), Capacitor (3 variantes), ArduinoPin (V1/V2) |
| **Prefabs de juego** | `Assets/Prefabs/` | CableBox_VR, Cable_Jumper, ExplorerHUD, ExplorerWorkstation, GameManager_System, Multimeter_VR_Art, TechnicianHUD, TechnicianMonitorHUD, Technician_Workstation, Explorer_Player, Player_Network |
| **VFX — CFXR** | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/` | **Electric:** `CFXR Electrified 3`, `CFXR2 Sparks Rain`, `CFXR3 Hit Electric C` · **Smoke:** `CFXR Smoke Source 3D`, `CFXR Explosion Smoke 2 Solo` · **Flash/Luz:** `CFXR Flash`, `CFXR3 LightGlow A (Loop)`, `CFXR3 Hit Light B` |
| **Entorno nave (Explorador)** | `Assets/sFuture Modules Pro/` | Panels, frames, walls, ramps, railing (45°, modular). Materiales azul sci-fi con emission. |
| **Entorno oficina (Técnico)** | `Assets/UnityJapanOffice/` | NoonA.unity ya integrada. Prefabs: AirVent, EmergencyLight, ExitLight, LanOutlet, LightSwitch, Outlet, SmokeDetector |
| **Skyboxes espaciales** | `Assets/Free Skyboxes - Space/` | 5 variantes Space (SBS 1-5) |
| **Arte propio** | `Assets/Art/` | Arduino 3D .obj (Meshy AI), MutliMeter.obj + materiales, kenney_furniture-kit FBX |
| **Materiales de componentes** | `Assets/Materials/` | LED (normal/broken/quemado), Resistor (R1-R4), Capacitor, ArduinoPin, VoltageSource, Cables (CableBlack/CableRed), Slots por reto, Nodos por reto |
| **Audio disponible** | `Assets/Samples/XRI/…` + `SpaceRobotKyle/Sfx/` | ButtonClick.wav, ButtonHover.wav, Button Pop.wav, Player_Footstep_01-10.wav, Player_Land.wav |
| **Video** | `Assets/Materials/Video_de_Fondos_Electrónica_Básica.mp4` | Video instructivo electrónica básica |
| **Scripts VFX ya escritos** | `Assets/Scripts/Electrical/` + `Gameplay/` | `ComponentSmokeEffect.cs`, `ShortCircuitSparkEffect.cs`, `AutoSmokeSetup.cs`, `SpaceshipAmbientSystem.cs`, `CircuitAudioManager.cs` |
| **Scripts UI ya escritos** | `Assets/Scripts/UI/` | `DeliveryTrayIndicator.cs`, `ValidationStationUI.cs`, `ExplorerOnboarding.cs`, `ExplorerResultsPanel.cs`, `SessionResultsPanel.cs` |

---

## Lo que NO tiene el proyecto (y dónde conseguirlo gratis)

| Falta | Fuente CC0 | Urgencia |
|-------|-----------|----------|
| SFX de circuito (componente instalado, cortocircuito, beep multímetro) | freesound.org IDs: 263133 (multimeter), 253173 (spark), 399095 (capacitor whine) | Alta — CircuitAudioManager los necesita |
| Música / jingles (victoria, fallo) | kenney.nl/assets/music-jingles | Media |
| Sonidos UI electrónica | kenney.nl/assets/interface-sounds | Media |

---

## Workstream A — Estabilización y wiring de escena
**Prioridad crítica — bloquea las pruebas con usuarios.**

### A1 — Verificar modoOffline (5 min)
- En Unity Inspector, `GameManager_System` → `ConnectionManager` → confirmar `modoOffline = false` antes de cualquier sesión multijugador real.

### A2 — Wiring de escena con Editor Tools (30-60 min, sin código nuevo)
Ejecutar en orden los siguientes menús `Tools → TITA`:

| # | Menú | Qué conecta |
|---|------|-------------|
| 1 | `Tools → TITA → Reparar UI` (`UISetupFixer`) | Pc_Camera y Recep_Camera en WorldSpace canvas |
| 2 | `Tools → TITA → Red → Rellenar referencias escena Tecnico` (`TecnicoSceneFixer`) | ConnectionManager.gameManager, GameManager.multimeter/protoSim/circuit, InstructionSystem, HUD |
| 3 | `Tools → TITA → Red → Limpiar NetworkManagers duplicados` (`NetworkManagerCleanupTool`) | Eliminar CM en posición (1322,0,-8) sin playerPrefab |
| 4 | `Tools → TITA → Reto 4 → Auto-Setup Completo` (`Reto4AutoSetup`) | ArduinoCore, ProtoboardSimulator, ArduinoIDEUI, TechnicianTelemetryUI, nodos, cables |
| 5 | `Tools → TITA → Reto 4 → Setup Monitor Arduino` (`ArduinoMonitorSetupTool`) | Display_Arduino ↔ TechnicianMonitorHUD, ArduinoIDEUI.bridge |

**Referencias manuales restantes** (Inspector, no automatizables):
- `CableBoxSpawner.cablePrefab` → asignar **`Assets/Prefabs/Cable_Jumper.prefab`**
- `CircuitAudioManager` — instanciar GO en escena Tecnico, asignar AudioClips cuando se descarguen (ver sección Audio más abajo)
- `TechnicianHUDController.panelValidacion / txtValidacionEstado / imgValidacionBg` — conectar desde el prefab `TechnicianHUD.prefab`
- `VRValidationButton.sfxPress/sfxPass/sfxFail` — AudioClips pendientes de descarga
- `Multimeter_VR_Art.prefab` → asignar `MultimeterUI.multimeter` en la escena Explorador

### A3 — Smoke y chispas en componentes (usa assets ya presentes)
`ComponentSmokeEffect.cs` se auto-genera si no hay ParticleSystem asignado, pero con los CFXR obtienes mayor calidad visual:

- **Smoke:** asignar `Assets/JMO Assets/.../Misc/CFXR Smoke Source 3D.prefab` → campo `smokeEffect` de cada componente en zona de reto.
- **Short-circuit sparks:** `ShortCircuitSparkEffect.cs` ya usa ParticleSystem procedural; opcionalmente reemplazar con `CFXR2 Sparks Rain.prefab` → campo `sparksPrefab`.
- **Auto-setup masivo:** `Assets/Scripts/Electrical/AutoSmokeSetup.cs` — añadir a un GO vacío en la escena y ejecutar en PlayMode para aplicar automáticamente a todos los `ElectricalComponent` de las zonas.

### A4 — SpaceshipAmbientSystem (usa assets ya presentes)
El script `SpaceshipAmbientSystem.cs` ya está escrito. Solo falta conectarlo en la escena Explorador:
- Añadir GO `[AmbientSystem]` en Explorador.unity
- Por zona (reto1Zone…reto4Zone): asignar sus `zoneLights[]`, opcionalmente `CFXR3 LightGlow A (Loop).prefab` como partícula de atmósfera, y un URP Volume si tienes post-process configurado.
- En **Fault state** → activar `CFXR Electrified 3.prefab` sobre el panel averiado.
- En **Repaired state** → activar `CFXR3 Hit Light B (Air).prefab` breve como flash de confirmación.

### A5 — Smoke detector como narrativa (bonus visual, 10 min)
`Assets/UnityJapanOffice/Prefabs/Facilities/SmokeDetector_01.prefab` — colocar en techo de zonas 3 y 4 de la nave. Al detectar cortocircuito (via `CircuitManager.OnCircuitChanged` + `isShortCircuited`), activar parpadeo de `EmergencyLight_01.prefab` y `CFXR Flash.prefab`. Refuerza narrativa de la nave averiada sin código nuevo.

---

## Workstream B — Cierre de Retos 3 y 4

### B1 — Reto 3 (Mixto): validación y feedback visual
El circuito mixto ya existe; faltan dos detalles:
1. **Condición de victoria triple:** verificar que `InstructionSystem.ValidateMixed()` comprueba los 3 fallos en orden (LED invertido → capacitor invertido → resistencia 470→220Ω). Si alguno no está en `InstructionSystem.cs`, añadir las verificaciones alineadas al patrón de los retos 1 y 2.
2. **Efecto humo en capacitor mal polarizado** (ya documentado en Capstone): usar `ComponentSmokeEffect` en el GO del capacitor → smoke intenso al inicio del reto → se apaga al reparar. Smoke prefab → `CFXR Smoke Source 3D`.

### B2 — Reto 4 (Arduino): wiring y visuales
Tras ejecutar `Reto4AutoSetup` (A2), los pendientes concretos son:

**Protoboard visual:**
- `BreadboardGridGenerator.cs` ya existe en `Assets/Scripts/Electrical/`. Ejecutar `ProtoboardModelCreator` (`Tools → TITA → Reto 4 → Crear modelo protoboard`) para generar el mesh 3D.
- Para decorar los rieles VCC/GND del protoboard: usar los materiales **`Resources Vol.2 - Electronics/Materials/Bareboard.mat`** (base) y las franjas de color ya definidas por el generador.

**Cables visuales:**
- `VRCableRenderer.cs` + `FlexWire.cs` ya escritos. El prefab `Cable_Jumper.prefab` ya existe. Asignar `cablePrefab` en `CableBoxSpawner`.
- Material del cable: `Assets/Materials/Materials/CableRed.mat` y `CableBlack.mat` ya disponibles.

**Monitor Arduino:**
- `TechnicianMonitorHUD.prefab` ya existe. Ejecutar `Tools → TITA → Reto 4 → Setup Monitor Arduino` para vincularlo al `Display_Arduino` mesh 3D.

**Modelo 3D Arduino (visual):**
- Ya existe: `Assets/Art/Arduino_Modelo/Meshy_AI_Arduino_Uno_Board_Ill_0529053438_texture.obj`
- Ejecutar `Tools → TITA → Reto 4 → Crear modelo Arduino` (`ArduinoModelCreator`) para posicionar los pines D2, P13, GND, A0 en sus posiciones físicas reales.

**Visuales de Reto 4:**
- Cuando ArduinoCore recibe un sketch correcto → activar `CFXR3 LightGlow A (Loop).prefab` en el pin D2 como indicador LED pulsante.
- Cable suelto → activar `CFXR Electrified 3.prefab` en el pin defectuoso.
- Monitor serial → `SensorStatusDisplay.cs` ya existe en `Electrical/`; conectar a `TechnicianTelemetryUI`.

---

## Workstream C — Retroalimentación visual y ambiental

### C1 — Retroalimentación LED (0 código nuevo)
Los materiales ya existen:
- LED encendido: `Mat_LED_Verde.mat`, `LED2_Normal_R2_Mat.mat`
- LED apagado/roto: `LED1_Broken_R2_Mat.mat`, `LED_Quemado.mat`
- Prefabs con variantes: `Delivered_LED_Green/Red/Yellow.prefab`

El sistema ya cambia el material via `MaterialPropertyBlock` en `DeskComponent.cs` y `LED.cs`. Solo verificar que los materiales correctos están asignados en Inspector para cada reto.

### C2 — Feedback de entrega y validación (scripts ya escritos)
- `DeliveryTrayIndicator.cs` → añadir GO `DeliveryTrayIndicator` como hijo de `Bandeja_Recepcion` en Explorador.unity. Escucha `ComponentDeliverySystem.OnComponentSent/OnComponentInstalled` automáticamente.
- `ValidationStationUI.cs` → instanciar junto al `VRValidationButton` en Explorador.unity. Se conecta sola.
- Usar `CFXR3 Hit Light B (Air).prefab` como flash en `VRValidationButton` al presionar (campo `successEffect`).

### C3 — Onboarding del Explorador
- `ExplorerOnboarding.cs` → añadir como GO `ExplorerOnboarding` en Explorador.unity. Ya tiene 5 slides, se destruye al completar.
- Usar `Assets/Materials/Video_de_Fondos_Electrónica_Básica.mp4` como slide 1 del onboarding (intro visual de electrónica básica) con `VideoPlayer` Unity.

### C4 — Ambiente sonoro (usa ButtonClick.wav existente + downloads CC0)
`CircuitAudioManager.cs` ya está escrito. Asignación de clips:

| Campo del Inspector | Audio disponible | Si no hay: descargar |
|--------------------|----------------|--------------------|
| `sfxComponentInstalled` | `Button Pop.wav` (XRI) | — |
| `sfxUIClick` | `ButtonClick.wav` (XRI) | — |
| `sfxShortCircuit` | *(descargar)* | freesound.org #253173 |
| `sfxMultimeterBeep` | *(descargar)* | freesound.org #263133 |
| `sfxCapacitorCharge` | *(descargar)* | freesound.org #399095 |
| `sfxLevelComplete` | *(descargar)* | kenney.nl/music-jingles |
| `sfxGameOver` | *(descargar)* | kenney.nl/music-jingles |

**Pasos de audio:** (1) Descargar los 5 archivos CC0 indicados (~10 min). (2) Importar en `Assets/Audio/` (crear la carpeta). (3) Asignar en el Inspector del GO `CircuitAudioManager`.

### C5 — Ambiente espacial del Explorador
- Skybox: `Assets/Free Skyboxes - Space/SBS Space 3/` — asignar en Explorador.unity como skybox material de la escena (Lighting → Environment → Skybox Material).
- Decoración de pasillos: módulos de `sFuture Modules Pro` (Panel 1x1 Plain, Frame 1x1, paredes 45°) para paneles de la nave en las zonas entre retos.
- Luces de emergencia narrativas: `UnityJapanOffice/Prefabs/Facilities/EmergencyLight_01.prefab` en techos de Reto 3 y 4, activadas via `SpaceshipAmbientSystem`.

### C6 — Resultados finales
`ExplorerResultsPanel.cs` y `SessionResultsPanel.cs` ya están escritos. Solo requieren ser instanciados y conectados al evento `ObjectiveSystem.OnSessionEnded`. Sin código nuevo.

---

## Workstream D — Pruebas con usuarios (PT-A)

Ver `Protocolo-Pruebas-Usuarios.md` (ya creado). Los instrumentos están listos (Anexos A–G).

**Checklist pre-PT-A:**
- [ ] A1 completado (`modoOffline = false`)
- [ ] A2 completado (todas las referencias cableadas)
- [ ] B1 completado (Reto 3 validable de inicio a fin)
- [ ] B2 completado (Reto 4 funciona en E2E con red real)
- [ ] C4 completado (al menos los 2 AudioClips de ButtonPop y ButtonClick asignados como placeholder)
- [ ] Smoke test en hardware real (Quest 3 + PC en LAN)

---

## Workstream E — Documento Capstone

Secciones en blanco que completar con datos reales:

| Sección | Input necesario |
|---------|----------------|
| Resumen + Abstract | Redactar con O1–O5 cumplidos |
| §7.3.2–7.3.3 (tablas de pruebas) | Ya están en el documento — solo formatear bien |
| §7.4 Resultados y Discusión | Datos de PT-A (Workstream D) |
| §7.5 Implicaciones éticas | Privacidad (no se guardan datos identificables), seguridad KAT VR, accesibilidad |
| §8 Conclusiones | Alcanzar los 5 objetivos específicos |
| §9 Trabajo futuro | Dashboard docente, más circuitos AC, modo individual, Android APK público |
| §10 Referencias | Ya presentes — verificar formato APA 7 |
| §11 Anexos | Adjuntar datos PT-A anonimizados, screenshots del juego, diagramas C4 |

---

## Cronograma ajustado — 7 semanas (desde 31-may-2026)

| Semana | Bloques | Assets usados | Entregable |
|--------|---------|---------------|-----------|
| **1** (02–06 jun) | A1, A2, A3 | Editor tools existentes, CFXR Smoke, Cable_Jumper.prefab | Todas las referencias cableadas; smoke+sparks funcionando |
| **2** (09–13 jun) | A4, A5, B1 | CFXR Electrified/Flash/LightGlow, SpaceshipAmbientSystem, SmokeDetector | Reto 3 validable E2E; ambiente de nave reactivo |
| **3** (16–20 jun) | B2, C1, C2 | Arduino 3D OBJ, CableJumper, CFXR LightGlow, DeliveryTrayIndicator | Reto 4 funcional; feedback entrega/validación |
| **4** (23–27 jun) | C3, C4, C5 | ExplorerOnboarding, video MP4, SBS Space skybox, sFuture panels, AudioClips CC0 | Onboarding + audio + ambiente nave completados |
| **5** (30 jun–04 jul) | D (piloto) | Build congelada | Sesiones piloto 1-2 parejas + ajustes |
| **6** (07–11 jul) | D (formal), E parcial | — | Sesiones formales; §7.4 Resultados redactada |
| **7** (14–18 jul) | E completo, builds finales | — | Documento sin placeholders; APK Quest + Win64; demo grabada |

---

## Resumen de cero descargas requeridas para el núcleo del juego

| Tarea | Asset ya disponible |
|-------|-------------------|
| Modelo Arduino VR | `Assets/Art/Arduino_Modelo/…texture.obj` |
| Modelo Multímetro VR | `Assets/Art/MutliMeter.obj` |
| Cables visuales | `Assets/Prefabs/Cable_Jumper.prefab` + materiales Red/Black |
| Humo componentes | `CFXR Smoke Source 3D.prefab` |
| Chispas cortocircuito | `CFXR2 Sparks Rain.prefab` + `CFXR Electrified 3.prefab` |
| Flash reparación | `CFXR3 Hit Light B (Air).prefab` + `CFXR Flash.prefab` |
| Ambiente nave (luces dinámicas) | `SpaceshipAmbientSystem.cs` + CFXR LightGlow Loop |
| Skybox nave espacial | `Free Skyboxes - Space/SBS Space 3` |
| Paneles sci-fi nave | `sFuture Modules Pro/Models/Core/` |
| Entorno Técnico | `UnityJapanOffice/NoonA.unity` (ya integrado) |
| Luces emergencia narrativas | `UnityJapanOffice/Prefabs/Facilities/EmergencyLight_01.prefab` |
| Sensor temperatura narrativa | `UnityJapanOffice/Prefabs/Facilities/SmokeDetector_01.prefab` |
| Decoración circuitos | `Resources Vol.2 - Electronics/Prefabs/` (Relay, Transistor, Segment Display…) |
| Sonidos básicos UI | `ButtonClick.wav`, `ButtonHover.wav`, `Button Pop.wav` (XRI Samples) |
| Pasos Explorador | `Player_Footstep_01-10.wav` (SpaceRobotKyle) |
| Video intro | `Video_de_Fondos_Electrónica_Básica.mp4` |
| Resultados finales UI | `ExplorerResultsPanel.cs` + `SessionResultsPanel.cs` (ya escritos) |
| Onboarding | `ExplorerOnboarding.cs` (ya escrito) |
