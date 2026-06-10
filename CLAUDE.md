# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Unity 6 VR *serious game* ("TITA", UDLA thesis) to teach basic electronics in the course *Computación Ubicua*. It is a **two-player asymmetric co-op** game: an **Explorador** in VR (Meta Quest 3 + KAT VR treadmill + haptic vest) physically inspects, wires the protoboard and repairs circuits, while a **Técnico** on PC reads manuals/diagrams, programs the virtual Arduino, and guides the diagnosis over voice. Neither role has the other's information — the dependency forces precise technical communication, which is the pedagogical core. The design goal is a "digital twin": victory requires an *electrically valid* circuit (Ohm/Kirchhoff), not just parts touching trigger points.

Gameplay is one continuous level of **4 sequential challenges (retos)** of rising difficulty, driven by the `LevelType` enum: `OhmLaw` (series) → `Parallel` → `Mixed` (polarity) → `Arduino` (sensor/actuator).

- Engine: **Unity 6000.4.3f1**, **URP 17.4.0**, **C#** (CI runner uses editor **6000.4.5f1** — keep both in sync)
- VR: **OpenXR 1.16.1** + **XR Interaction Toolkit 3.4.1** + New **Input System 1.19.0** (+ Meta OpenXR, Oculus, XR Hands)
- Networking: **Photon Fusion** (vendored under `Assets/Photon/Fusion`, NOT a UPM package) — Técnico = Host/StateAuthority, Explorador = Client
- Periféricos: KAT VR SDK (`Assets/KAT`) with automatic joystick fallback; haptic vest. Arduino serial via Ardity (`Assets/Ardity`).

## Two working copies (read first)

There are **two full git clones of the same remote** (`github.com/Proyecto-titulacion-Serious-Game/Serious-Game`) on this machine:

- `Proyecto-TITA/` — this clone (branch `development`).
- `Proyecto-TITA/Serious-Game/` — a nested second clone with its own `.git`. As of late May 2026 it held *newer* code (more scripts, more recent edits) and is where the Capstone PDF and planning docs live (`Plan-de-Desarrollo.md`, `Protocolo-Pruebas-Usuarios.md`).

Before editing, confirm which clone you are in and which has the work you expect (`git -C <path> log -1`, compare file mtimes). Don't assume changes in one clone are visible to the other — they sync only through the GitHub remote. The nested `Serious-Game/` is *not* gitignored by the outer clone.

## Build / run / docs

This is an Editor-driven Unity project: no lint step, and tests run from the Editor (not CI).

- **Open in Editor:** Unity Hub → open the clone with editor **6000.4.3f1**. Main scenes in `Assets/Scenes/`: `Tecnico.unity` (PC host), `Explorador.unity` (VR client), `IntegratedDemo.unity` (both roles, for end-to-end testing). `Tecnico` additively loads `NoonA.unity` at runtime via `TecnicoBootstrapper`. (`Assets/MapVR.unity` and `Assets/serious game.unity` are legacy.)
- **CI** (`.github/workflows/main.yml`, self-hosted CachyOS Linux runner, on push to `main`/`development`): builds a Windows64 player, runs Doxygen, regenerates `README.md`, deploys docs to GitHub Pages.
  ```bash
  "$UNITY_EDITOR" -batchmode -nographics -silent-crashes -quit \
    -projectPath . -buildWindows64Player "build/SeriousGame.exe" -logFile /dev/stdout
  doxygen Doxyfile                  # INPUT = ./Assets/Scripts README.md  → ./docs/html → GitHub Pages
  python generate_readme.py         # rewrites README.md from the Doxygen dump
  ```
  `UNITY_EDITOR` defaults to a hardcoded runner path, overridable via repo variable `UNITY_EDITOR_PATH`.
- **`README.md` is auto-generated on every push** by `generate_readme.py` (along with `progreso.txt`). Do not hand-edit it — changes are overwritten. Put durable docs elsewhere.
- **Explorador (Quest)** ships as an Android APK built from the Editor; the **Técnico** build is the Windows64 player (the only one CI produces).
- **Tests:** Unity Test Framework — EditMode for the electrical engine, PlayMode + a Photon sandbox for integration — run via the Editor Test Runner.

## Architecture (the parts that span files)

Game scripts live in `Assets/Scripts/`, in 8 SRP modules: `Electrical`, `Gameplay`, `Interaction`, `Networking`, `Player`, `UI`, `Desktop`, `Core` (+ `Editor` tooling, `NPC`, `InputReferences`).

**Challenge state machine + event bus.** `Gameplay/GameManager` orchestrates the 4 retos via the `LevelType` enum, activating/deactivating per-reto zone GameObjects (`reto1Zone…reto4Zone`), each with its own circuit. Modules **communicate through static C# events** (`GameManager` and the simulators publish; `ObjectiveSystem`, `PerformanceTracker`, `InstructionSystem`, UI subscribe) with almost no direct references into core logic. To change reto flow or win conditions, follow the event subscriptions, not call sites.

**Three circuit classes — and two separate `OnCircuitChanged` events** (this trips people up):
- `Assets/Scripts/Gameplay/CircuitSimulator.cs` → class **`CircuitSimulator`**: `ComponentSlot` orchestration for **Retos 1–3**; `GameManager.circuit` points here. Has an implicit operator to `CircuitManager` for legacy compat.
- `Assets/Scripts/Electrical/CircuitManager.cs` → class **`CircuitManager`**: the **actual Retos 1–3 solver** (series/parallel/mixed) that paints the LEDs and fires **`CircuitManager.OnCircuitChanged`**. This is what the multimeter and the win auto-check read — not the Gameplay `CircuitSimulator`.
- `Assets/Scripts/Electrical/CircuitSimulator.cs` → class **`ProtoboardSimulator`**: the **Reto 4** sandbox; `GameManager.protoSim` points here; fires **`ProtoboardSimulator.OnCircuitChanged`**. Solves with `Electrical/CircuitGraphAnalyzer.SolveMNA` (a diode-aware Modified Nodal Analysis with fixed-point iteration). Paired with `Electrical/ArduinoCore` (an ATmega328P emulator).

A subscriber that must react in **all** retos has to listen to **both** `CircuitManager.OnCircuitChanged` *and* `ProtoboardSimulator.OnCircuitChanged` (see `InstructionSystem` for the correct pattern; the particle FX once only listened to the first and went dead in Reto 4).

The `Electrical` module holds the component model: abstract `ElectricalComponent` (Template Method — subclasses implement `GetResistance()`/`Calculate()`) with `Resistor`, `LED`, `Capacitor`, `VoltageSource`, `ArduinoPin`. Topology selection is a Strategy (`SimulateSeries/Parallel/Mixed`). Simulation runs at **20 Hz behind a dirty flag** (`MarkDirty()` → recompute → `OnCircuitChanged` event) — a value won't update unless something marks the circuit dirty. Short circuit = `R_total ≤ 0.1 Ω`. Note `LED.Calculate()` is pure-resistive (Retos 1–3 feed it node voltages with no diode drop); the Reto 4 MNA models the LED's Vf and direction itself, then paints via `LED.ApplyResolvedCurrent()` — do **not** add Vf to `Calculate()` or you break Retos 1–3.

**Asymmetric networking.** `Networking/ConnectionManager` starts Fusion as Host (Técnico) or Client (Explorador); `Networking/GameSession : NetworkBehaviour` holds `[Networked]` state and typed RPCs. Delivery flow: Técnico picks a component → `GameSession.EnviarComponente()` RPC → Explorador's `ExplorerComponentReceiver` spawns the prefab in a tray → Explorador installs into a `ComponentSlot` → validation applies the repair → `MarkDirty()` → win check → `ReportarInstalacion()` RPC back.

**`modoOffline` flag (frequent footgun).** `ConnectionManager.modoOffline` lets you test one role without a host: RPCs are bypassed and local fallback static events (`ComponentSendingTray.OnComponentSentLocal`) carry delivery instead. Leaving `modoOffline = true` in a real two-player session silently breaks component delivery — verify it is `false` before any multiplayer/user test.

## Editor tooling (use it instead of hand-wiring scenes)

Scene setup is heavily automated. `Assets/Scripts/Editor/` (~27 tools) exposes generators and fixers under the Unity menu **`Tools → TITA → …`** (Reto 4 auto-setup, network-reference fillers, canvas/UI repair, duplicate `ConnectionManager` cleanup, Quest Link config, art/prefab generators). Many Inspector references are auto-resolved at runtime in `Awake()` even when serialized null, so a `{fileID: 0}` in YAML is not necessarily a bug. Prefer running the relevant `Tools → TITA` command over manually re-wiring references.

## Non-obvious traps (learned debugging this codebase)

- **Editing scene/prefab/asset YAML requires Unity CLOSED.** With the Editor open, saving a scene/prefab overwrites your on-disk YAML edits. Editing `.cs` while open is fine (Unity recompiles on focus). Verify with `Get-Process Unity` before touching any `.unity`/`.prefab`/`.asset`/`ProjectSettings/*.asset`.
- **Retos 1–3 components are FIXED scene pieces, not the delivered tokens.** The wired circuit `Resistor`/`LED` are soldered into the reto zone (with nodes); the component the Explorador installs from the tray is just a *trigger*. On a correct install the repair **transfers the token's value to the fixed piece** (`ComponentDeliverySystem.BuscarResistorDelReto`/`BuscarLEDDelReto`), then destroys the token. So the multimeter/sim always read the fixed piece — if a repair "doesn't take", the value never transferred.
- **Win for Retos 1–3 is auto, not a button.** `GameManager.OnCircuitChangedAutoCheck` completes the reto only if it was seen *incorrect first* (`_vistoIncorrectoEnReto`) and then becomes correct. `PlayerFeedbackUI` shows "¡FELICIDADES!" on `OnLevelCompleted`. Reto 4 instead validates via the physical button → `EvaluarReto4`.
- **Several singletons auto-create at runtime via `RuntimeInitializeOnLoadMethod`** and are NOT in any scene: `NetworkDemoOverlay`, `TelemetryPublisher`, `RoomCodeEntryUI`, `ExplorerLinkOverlay`, `SoloTechnicianDebug`. Grepping scene YAML for them will falsely report "missing".
- **Solo testing (offline, no Técnico):** the delivery token defaults to 100 Ω (the Técnico injects the real value over the network), so placing it solo never matches e.g. Reto 1's 850 Ω. Use the dev-only helper **F8** (`Gameplay/SoloTechnicianDebug`, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`) to apply the current reto's correct fix directly. F1–F4 are the `DebugLevelSkipper` (switch reto), F5 = validate (IDE), F9 = network overlay.
- **Per-role build scene list:** the **Técnico** (Windows) build needs `Tecnico` (index 0) **and** `NoonA` enabled (NoonA is additively loaded by name at runtime); the **Explorador** (Android/Quest) build needs only `Explorador`. Use Unity 6 Build Profiles with a per-role Scene List override.

## Conventions

- C#: PascalCase for public types/methods, `_camelCase` for private fields, read-only properties for Inspector-observable state.
- Unity 6 API: use `FindObjectsByType<T>(FindObjectsInactive.Include)` / `FindAnyObjectByType<T>()` (not obsolete `FindObjectOfType`); prefix with `Object.` inside static Editor classes.
- New Input System only — `Keyboard.current.*` / device APIs, never legacy `Input.GetKey`.
- TMP buttons: `LiberationSans SDF` lacks `▶` (U+25B6); use `>>`.
- Do **not** patch third-party package source (Fusion/Unity) to silence the cosmetic "named GUIStyle without a current skin" warning — it's a known Fusion 2 + Unity 6 Inspector-repaint issue and patching it has repeatedly caused worse breakage.

## Further docs

- `Documentacion_Tecnica_v2.md` — long-form technical documentation (all scripts, setup guide, 3D protoboard/Arduino model generation).
- Topic guides at repo root: `VR_SETUP_GUIDE.md`, `VR_STATUS_SUMMARY.md`, and several `*_RESOLUTION.md` / `QUICK_FIX_*.md` notes.
- Online API docs (Doxygen): https://proyecto-titulacion-serious-game.github.io/Serious-Game/
- Capstone thesis PDF and the development/testing plans live under the `Serious-Game/` clone.
