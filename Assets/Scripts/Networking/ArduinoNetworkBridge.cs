using System;
using UnityEngine;
using Fusion;

/// <summary>
/// Enlace de red asimétrico. Comunica la interfaz del Técnico (PC) con el Arduino físico en VR.
///
/// OPCIÓN A — Auto-detección en runtime:
///   Cuando Fusion spawnea este NetworkBehaviour en cualquier cliente, dispara
///   OnBridgeReady. TechnicianTelemetryUI y ArduinoIDEUI se suscriben a ese evento
///   y se conectan solos sin necesitar referencias en el Inspector.
/// </summary>
public class ArduinoNetworkBridge : NetworkBehaviour
{
    [SerializeField] private ArduinoCore _arduinoCore;

    // ─── EVENTOS ESTÁTICOS ───────────────────────────────────────────────
    /// <summary>Dispara en todos los clientes cuando el bridge está listo en red.</summary>
    public static event Action<ArduinoNetworkBridge> OnBridgeReady;
    /// <summary>Dispara en todos los clientes cuando el bridge se despawnea.</summary>
    public static event Action<ArduinoNetworkBridge> OnBridgeDestroyed;
    /// <summary>Dispara en todos los clientes cuando el Técnico sube un nuevo sketch.</summary>
    public static event Action<int, PinMode, PinState, bool, int> OnSketchReceived;

    // ─── TELEMETRÍA CONTINUA (VR → PC) ──────────────────────────────────
    [Networked]
    public int NetworkedAnalogValue { get; set; }

    // ─────────────────────────────────────────────
    //  Fusion lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        _arduinoCore = GetComponent<ArduinoCore>();
    }

    /// <summary>
    /// Llamado por Fusion en TODOS los clientes cuando el objeto entra en la red.
    /// Notifica a los listeners (TechnicianTelemetryUI, ArduinoIDEUI, etc.) para
    /// que se conecten dinámicamente sin depender del Inspector.
    /// </summary>
    public override void Spawned()
    {
        _arduinoCore ??= GetComponent<ArduinoCore>();
        Debug.Log($"[ArduinoNetworkBridge] Spawned en {(HasStateAuthority ? "Host" : "Client")}. " +
                  "Notificando listeners...");
        OnBridgeReady?.Invoke(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        OnBridgeDestroyed?.Invoke(this);
    }

    // ─────────────────────────────────────────────
    //  Fusion update
    // ─────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && _arduinoCore != null)
            NetworkedAnalogValue = _arduinoCore.GetAnalogReadA0();
    }

    // ─────────────────────────────────────────────
    //  RPC: Técnico → todos los clientes
    // ─────────────────────────────────────────────

    /// <summary>
    /// El parámetro <paramref name="pin"/> es el número de pin seleccionado por el Técnico
    /// en el ArduinoIDEUI. Se usa int en lugar de NetworkInt porque Fusion requiere tipos básicos.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SubirCodigoArduino(int pin, NetworkBool isOutput, NetworkBool isHigh,float delayMs, NetworkBool isBlink)
    {
        Debug.Log($"[NetworkBridge] Sketch recibido — pin D{pin}, output={isOutput}, blink={isBlink}.");
        DeliverSketch(pin, isOutput, isHigh, delayMs, isBlink);
    }

    /// <summary>
    /// Aplica un sketch al ArduinoCore de la escena y notifica a los listeners (OnSketchReceived).
    /// Punto ÚNICO de entrega: lo usa tanto el RPC de este bridge (escena única / IntegratedDemo)
    /// como GameSession.RPC_SubirCodigoArduino, que es el canal COMPARTIDO del setup asimétrico
    /// — el bridge es un objeto de escena que solo existe en Explorador.unity y NO se replica al
    /// Host, así que en escenas separadas el código debe viajar por GameSession (spawneada por el
    /// Host y replicada a ambos).
    /// </summary>
    public static void DeliverSketch(int pin, bool isOutput, bool isHigh, float delayMs, bool isBlink)
    {
        // UnityEngine.Object completo: NetworkBehaviour tiene una propiedad de instancia
        // 'Object' (el NetworkObject de Fusion) que ocultaría a UnityEngine.Object aquí.
        var core = UnityEngine.Object.FindAnyObjectByType<ArduinoCore>();
        if (core != null)
            core.RecibirCodigoDePC(pin, isOutput, isHigh, delayMs, isBlink);

        PinMode  mode    = isOutput ? PinMode.OUTPUT : PinMode.INPUT;
        PinState state   = isHigh   ? PinState.HIGH  : PinState.LOW;
        int      blinkMs = Mathf.RoundToInt(delayMs);
        OnSketchReceived?.Invoke(pin, mode, state, isBlink, blinkMs);
    }

    // ─────────────────────────────────────────────
    //  Modo offline: simular spawn para pruebas sin Fusion
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llama esto manualmente en modoOffline para que los listeners se conecten
    /// aunque Fusion no haya spawnado el objeto.
    /// </summary>
    public void SimularSpawnOffline()
    {
        _arduinoCore ??= GetComponent<ArduinoCore>();
        Debug.Log("[ArduinoNetworkBridge] Simulating spawn (offline mode).");
        OnBridgeReady?.Invoke(this);
    }
}