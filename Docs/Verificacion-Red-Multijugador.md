# Verificación de Red Multijugador (Photon Fusion 2) — TITA

**Fecha:** 2026-06-06
**Alcance:** Confirmar que tras los cambios del ComponentReceiver/caja, la capa de red sigue
conectada en ambos roles, que ambos apuntan al mismo ID de sesión, y que las cadenas de
envío de componentes y Arduino siguen cableadas.

> Resultado global: ✅ **La red quedó intacta.** Los cambios de la sesión solo tocaron
> `ExplorerComponentReceiver.cs` y la escena del Explorador; no se modificó `GameSession`,
> los RPCs, el `ConnectionManager`, el `ComponentSendingTray` ni el Arduino.

---

## 1. Conexión Photon y "mismo ID"

El `ConnectionManager` vive dentro del prefab **`GameManager_System`**; cada escena lo
instancia y sobreescribe su rol.

| Parámetro | Técnico (`Scenes/Tecnico/Tecnico.unity`) | Explorador (`Scenes/Explorador.unity`) |
|---|---|---|
| `rolAutomatico` | **2 = Tecnico → `GameMode.Host`** | **1 = Explorador → `GameMode.Client`** |
| `modoOffline` | 0 (online) | 0 (online, override explícito) |
| Auto-conecta en `Start()` | Sí (crea servidor/Host) | Sí (se une como Client) |

### Coincidencia de "ID" (triple)

| Nivel | Valor | ¿Igual en ambos? |
|---|---|---|
| **Sala / sesión** | `SessionName = "LaboratorioUbicua"` (hardcodeado en `ConnectionManager.StartSimulation`) | ✅ idéntico y garantizado |
| **Photon AppId (Fusion)** | `6f6e0e8b-3e79-4dca-a5a5-64f9ec7b7526` (región `us`, `PhotonAppSettings.asset`) | ✅ mismo proyecto |
| **Player prefab de red** | `RawGuidValue` 344616935362728148 / 4017228263911491400 | ✅ mismo override en ambas escenas |

Como el nombre de sala es la **misma constante** para Host y Client, se encuentran sí o sí en
la misma sesión del mismo AppId. Si Fusion falla (AppId/red), `StartSimulation` cae a offline
con try/catch (no crashea).

- `ConnectionManager` enum: `AutoConnectRole { Ninguno=0, Explorador=1, Tecnico=2 }`.
- Singleton en `Awake()`: si hay CMs duplicados, conserva el que tiene `playerPrefab` y destruye el resto.
- `connectionTimeoutSeconds: 12`, `gameManager` cableado.

---

## 2. Cadena: Envío de componentes (Técnico → Explorador)

```
Técnico: ComponentSendingTray.EnviarComponente   (Technician_Workstation.prefab)
   ├─ red:     GameSession.Instance.RPC_EnviarComponente(tipo, valor)
   │              └─ GameSession.RPC_EnviarComponente → OnComponenteRecibido.Invoke
   └─ offline: ComponentSendingTray.RaiseOnComponentSentLocal → OnComponentSentLocal.Invoke
                          ▼
Explorador: ExplorerComponentReceiver  (se suscribe a AMBOS eventos en OnEnable)
   → SpawnComponente → el componente aparece en la CAJA (ComponentReceiver_Caja)
```

**Estado del `ComponentSendingTray` (Técnico):** presente y activo (vía
`Technician_Workstation.prefab`). Cableado: `btnEnviar` ✓, `inputValor` ✓, `togglePolaridad` ✓,
`delivery` ✓, textos ✓. Los campos `gameManager`/`technicianActions` están en null pero son
**campos muertos** (declarados, nunca usados) → sin impacto.

**Estado en el Explorador:** la nueva caja independiente `ComponentReceiver_Caja` es un
`ExplorerComponentReceiver` activo y se suscribe a los mismos eventos. El receiver que venía
anidado en `Explorer_Player` quedó **desactivado** → exactamente UN receiver recibe (sin doble-spawn).

---

## 3. Cadena: Validación (botón → Host → resultado)

```
SolicitarValidacion → GameSession.RPC_SolicitarValidacion → OnValidacionSolicitada
   → el Host evalúa → RPC_PublicarDiagnostico / OnResultadoValidacion
```

`VRValidationButton` (Explorador) y el botón "COMPROBAR (F5)" del IDE (Técnico) disparan la
evaluación. En offline el `VRValidationButton` evalúa localmente (fix de esta sesión).

---

## 4. Cadena: Arduino (Reto 4)

```
Técnico: ArduinoIDEUI.SendToBoard
   ├─ red:     GameSession.Instance.RPC_SubirCodigoArduino(...)   (canal compartido)
   ├─ bridge:  ArduinoNetworkBridge.RPC_SubirCodigoArduino(...)
   └─ offline: ArduinoCore local
Explorador: ArduinoNetworkBridge.Spawned() → OnBridgeReady.Invoke
   → ArduinoIDEUI / TechnicianTelemetryUI se auto-conectan (Opción A, sin refs cross-scene)
```

- `TechnicianMonitorHUD.prefab` (ArduinoIDEUI + TechnicianTelemetryUI) **instanciado** en
  `Tecnico.unity`; el `btnComprobarCircuito` quedó cableado en sesiones previas.
- `ArduinoCore` + `ArduinoNetworkBridge` presentes y cableados en `Reto4_Zone` del Explorador
  (`pinNodeMap` pines 2–13, `nodoGND`/`nodoA0`/`nodoP13`).
- `RPC_SubirCodigoArduino` existe en `GameSession` y en `ArduinoNetworkBridge`.

---

## 5. Tabla resumen

| Cadena | Técnico (Tecnico.unity) | Explorador (Explorador.unity) | Estado |
|---|---|---|---|
| Conexión Photon | Host, online, sala `LaboratorioUbicua` | Client, online, misma sala | ✅ Conectado |
| Envío de componentes | `ComponentSendingTray` ✓ | `ComponentReceiver_Caja` recibe ✓ | ✅ Conectado |
| Validación | botón/F5 → SolicitarValidacion | VRValidationButton / resultado | ✅ Conectado |
| Arduino | `ArduinoIDEUI`+Telemetría (HUD) ✓ | `ArduinoCore`+`ArduinoNetworkBridge` ✓ | ✅ Conectado |

---

## 6. Pendiente de prueba EN VIVO (no verificable por código)

- Que el **AppId esté activo** en el dashboard de Photon y haya internet al probar.
- Con Host + Cliente reales: que el componente enviado **aparezca en la caja** del Explorador,
  y que el **sketch llegue al Arduino**.
- Comportamiento VR de la caja híbrida: agarrarla, dejarla en otro sitio (debe quedarse),
  recibir componente (pegado), agarrarlo (se suelta) e instalar.

> El código y el cableado están correctos para que ambos roles se conecten al mismo ID; solo
> falta la prueba en red/Play.
