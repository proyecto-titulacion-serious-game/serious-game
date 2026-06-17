# Reto 4 — Notas técnicas: voltaje del Arduino, validación y modo online

> Documento técnico del Reto 4 (LevelType.Arduino, sandbox de protoboard).
> Cubre cómo completarlo, cómo genera voltaje el Arduino, el cálculo eléctrico,
> el camino de red, los fixes aplicados y el test de regresión.
> Última actualización: 2026-06-17.

## 1. Cómo entrar y completar el Reto 4

- **Entrar al nivel:** pulsar **F4** en Play Mode (`DebugLevelSkipper`, solo dev/Editor).
  - Confirmación en consola: `[Breadboard] Modo protoboard ON (Reto 4)`.
  - Si en su lugar aparece `[Slot ...] ¡Imán activado! ... succionado`, **NO** estás en el Reto 4:
    ese mensaje solo existe en `ComponentSlot.cs` (Retos 1‑3), que el modo Reto 4 desactiva.

- **Objetivo:** hacer parpadear un LED de forma segura desde un pin digital.

- **Las 7 condiciones de victoria** (`ProtoboardSimulator.EvaluateSandbox`):
  1. Pin en `OUTPUT`.
  2. `blinkEnabled` (el sketch debe tener patrón BLINK, no solo HIGH).
  3. El pin tiene nodo eléctrico y existe el nodo GND.
  4. Hay un camino dirigido **Pin → … → GND**.
  5. Hay un **LED** en el camino, con el **ánodo (pata larga) hacia el pin**
     (si está al revés → "LED invertido").
  6. Hay una **resistencia ≥ 100 Ω** en el camino (recomendado 330 Ω).
  7. La corriente estimada ≤ `maxSafeCurrent` del LED.

  Éxito → consola: `[Reto4] Validación: ✓ CIRCUITO COMPLETO`.

- **Cableado a armar (Explorador, VR):**
  `Pin Dx → Resistencia (≥100 Ω) → LED (ánodo al pin) → GND`.

## 2. Cómo genera voltaje el Arduino

En `ArduinoCore`: con el pin en `OUTPUT` y en HIGH/blink, `DigitalWrite()` pone el nodo del
pin a **5 V** (`outputVoltageTTL`) y a **0 V** en LOW. El motor del sandbox usa esa onda como
fuente: `srcV = _arduino.OutputVoltage`, `srcNodeA = nodo del pin`, `srcNodeB = GND`.

Sketch mínimo (el parser `ArduinoCodeParser` tolera mayúsculas, `D7`, `LED_BUILTIN`, etc.):

```cpp
void setup() {
  pinMode(13, OUTPUT);
}
void loop() {
  digitalWrite(13, HIGH);
  delay(500);
  digitalWrite(13, LOW);
  delay(500);
}
```

> Solo HIGH fijo **no** completa el reto: el objetivo es **parpadear** (condición 2).

**Prueba offline sin Técnico:** en el Inspector de `ArduinoCore`, poner
`activePinMode = OUTPUT` y `blinkEnabled = true`.

## 3. Cálculo eléctrico (voltaje en la resistencia)

El multímetro (Explorador) lee `nodo.voltage` / `corriente` tras resolver el MNA
(`CircuitGraphAnalyzer.SolveMNA`). El voltaje en la resistencia es `V_R = I × R`.

Con fuente 5 V, LED `Vf ≈ 2 V` + R interna 50 Ω, y R = 330 Ω:

```
I   = (5 − 2) / (330 + 50) ≈ 7.9 mA
V_R = I × 330              ≈ 2.6 V
V_LED ≈ 2.0 V (Vf) + I × 50 ≈ 2.4 V      (V_R + V_LED ≈ 5 V)
```

La telemetría del Técnico muestra **5 V fijos** a propósito
(`_sourceVoltage = blinkEnabled ? 5f : srcV`) para que el número no salte con el parpadeo.

## 4. Modo online (setup asimétrico de 2 escenas)

El `GameManager` + `ProtoboardSimulator` viven en la escena del **Explorador**, no en la del
Técnico. El flujo de red:

- **Sketch:** Técnico → `GameSession.RPC_SubirCodigoArduino` → `ArduinoNetworkBridge.DeliverSketch`
  → `ArduinoCore.RecibirCodigoDePC` en el Explorador.
- **Componentes:** Técnico (`ComponentSendingTray.EnviarComponente`) →
  `GameSession.RPC_EnviarComponente` → `OnComponenteRecibido` →
  `ExplorerComponentReceiver.SpawnComponente` (aplica `ProtoboardConnector`, escala, enderezado).
  - Resistor → valor escrito en el campo de ohms. LED/Cap → `+1` (correcta) / `-1` (invertida).
- **Comprobar circuito:** el botón del IDE llama `GameSession.SolicitarValidacion()` →
  el Explorador corre `OnNetworkValidacionSolicitada → EvaluarReto4` y publica el resultado por
  `RPC_PublicarDiagnostico`, que la consola del IDE muestra vía `PollDiagnosticoReto4`.

### Checklist de escena para que online funcione de punta a punta

1. Mesa del Técnico con `DeskComponent` para **resistencia y LED**.
2. `ExplorerComponentReceiver` con prefabs (`ledGreenPrefab`, `resistorPrefab`) y slots / `puntoDeEntrega`.
3. `ArduinoCore` con `pinNodeMap` poblado (pin→nodo) + `nodoGND` asignado.
4. Al jugar: escribir **≥100 Ω** (ej. 330) y dejar la **polaridad en CORRECTA**.

## 5. Fixes aplicados (2026-06-17)

- **`UI/ArduinoIDEUI.cs`** — `ComprobarCircuito()` ahora pide validación por red
  (`GameSession.SolicitarValidacion()`) cuando no hay `GameManager` local → funciona online.
- **`Gameplay/OfflineTestSpawner.cs`** — se auto-desactiva cuando hay red
  (`ConnectionManager.Instance != null && !modoOffline`) → no interfiere online; sigue sirviendo offline.
  Valores correctos: Resistor = 850, **LED = 1** (nunca ohmios ni negativo; LED valida `_pendingValue >= 0`).
- **`Desktop/ComponentSendingTray.cs`** — la etiqueta del toggle muestra CORRECTA/INVERTIDA al cambiarlo.

## 6. Test de regresión

`Assets/Scripts/Editor/Reto4VoltageTest.cs` arma el circuito en memoria y verifica el motor sin VR.

- **Editor:** menú **Tools → TITA → Reto 4 → Test de voltaje (headless)** (resultado en la Console).
- **Batch (con Unity cerrado):**
  ```
  Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod Reto4VoltageTest.Run -logFile -
  ```

Salida esperada (corrida OK el 2026-06-17, exit 0):

```
[1]  HIGH → OutputVoltage = 5.00 V | nodo pin = 5.00 V
[1b] LOW  → OutputVoltage = 0.00 V
[2]  MNA=True | I=7.89 mA | V_R=2.61 V | V_LED=2.39 V | LED.isOn=True | estado=Correct
[3]  LED invertido → I=0.000 mA | LED.isOn=False
===== RESULTADO: ✓ OK — el Arduino genera voltaje y el circuito del Reto 4 conduce y es seguro =====
```
