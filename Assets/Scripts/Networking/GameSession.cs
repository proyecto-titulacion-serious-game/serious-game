using Fusion;
using UnityEngine;

/// <summary>
/// Objeto de red compartido entre Técnico y Explorador.
///
/// SETUP EN UNITY:
///   1. Crear un GameObject vacío llamado "GameSession" en AMBAS escenas
///      (Tecnico.unity y Explorador.unity).
///   2. Añadirle este script + NetworkObject (Fusion).
///   3. Guardarlo como prefab en Resources/GameSession.prefab
///      (Fusion lo usa para sincronizarlo automáticamente).
///
/// FLUJO:
///   Técnico llama RPC_EnviarComponente → Explorador recibe OnComponenteRecibido
///   Explorador instala → llama RPC_ComponenteInstalado → Técnico recibe OnComponenteInstalado
///   Técnico avanza reto → llama RPC_CambiarReto → ambos reciben OnRetoChanged
///   Técnico repara cable → llama RPC_FixLooseCable → ambos reciben OnCableFixed
/// </summary>
public class GameSession : NetworkBehaviour
{
    // ─────────────────────────────────────────────
    //  Singleton de red
    // ─────────────────────────────────────────────
    public static GameSession Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Estado compartido
    // ─────────────────────────────────────────────
    [Networked] public int          RetoActual              { get; set; }
    [Networked] public NetworkBool  HayComponentePendiente  { get; set; }
    [Networked] public int          TipoComponentePendiente { get; set; }
    [Networked] public float        ValorComponentePendiente { get; set; }
    [Networked] public TickTimer    HeartbeatTimer          { get; set; }

    // Host resetea el timer cada N segundos; clientes detectan si supera el timeout.
    private const float HeartbeatInterval = 5f;
    private const float HeartbeatTimeout  = 10f;

    // ─────────────────────────────────────────────
    //  Eventos locales
    // ─────────────────────────────────────────────

    /// <summary>Explorador: el Técnico envió un componente.</summary>
    public static event System.Action<ComponentType, float> OnComponenteRecibido;

    /// <summary>Técnico: el Explorador instaló (o falló).</summary>
    public static event System.Action<bool>                 OnComponenteInstalado;

    /// <summary>Ambos: el reto cambió.</summary>
    public static event System.Action<int>                  OnRetoChanged;

    /// <summary>Reto 4: el Técnico reparó el cable suelto.</summary>
    public static event System.Action                       OnCableFixed;

    /// <summary>El Host no ha respondido en más de HeartbeatTimeout segundos.</summary>
    public static event System.Action                       OnHeartbeatTimeout;

    /// <summary>El Explorador solicitó validar el circuito.</summary>
    public static event System.Action                       OnValidacionSolicitada;

    /// <summary>El sistema reportó el resultado de la validación (paso, codigoMotivo).</summary>
    public static event System.Action<bool, int>            OnResultadoValidacion;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    public override void Spawned()
    {
        Instance = this;
        Debug.Log($"[GameSession] Spawned. IsMine={Object.HasStateAuthority}  Reto={RetoActual}");

        if (Object.HasStateAuthority)
            HeartbeatTimer = TickTimer.CreateFromSeconds(Runner, HeartbeatTimeout);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            float? remaining = HeartbeatTimer.RemainingTime(Runner);
            if (remaining == null || remaining < HeartbeatTimeout - HeartbeatInterval)
                HeartbeatTimer = TickTimer.CreateFromSeconds(Runner, HeartbeatTimeout);
        }
        else
        {
            if (HeartbeatTimer.Expired(Runner))
            {
                Debug.LogWarning("[GameSession] Heartbeat timeout — Host no responde.");
                OnHeartbeatTimeout?.Invoke();
                // Silenciar hasta el próximo ciclo real para no spamear
                HeartbeatTimer = TickTimer.CreateFromSeconds(Runner, HeartbeatTimeout * 100f);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Técnico → Explorador: enviar componente
    // ─────────────────────────────────────────────

    public void EnviarComponente(ComponentType tipo, float valor)
    {
        if (!Object.HasStateAuthority) return;
        RPC_EnviarComponente((int)tipo, valor);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
public void RPC_EnviarComponente(int tipo, float valor)
{
        if (Object.HasStateAuthority)
        {
            HayComponentePendiente   = true;
            TipoComponentePendiente  = tipo;
            ValorComponentePendiente = valor;
        }
        OnComponenteRecibido?.Invoke((ComponentType)tipo, valor);
        Debug.Log($"[GameSession] Componente enviado: {(ComponentType)tipo} = {valor}");
    }

    // ─────────────────────────────────────────────
    //  Explorador → Técnico: instalación
    // ─────────────────────────────────────────────

    public void ReportarInstalacion(bool exito)
    {
        RPC_ComponenteInstalado(exito);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ComponenteInstalado(NetworkBool exito)
    {
        if (Object.HasStateAuthority)
            HayComponentePendiente = false;
        OnComponenteInstalado?.Invoke(exito);
        Debug.Log($"[GameSession] Instalación: {(exito ? "correcta" : "incorrecta")}");
    }

    // ─────────────────────────────────────────────
    //  Reto 4: cable suelto
    // ─────────────────────────────────────────────

    /// <summary>Solo el Técnico (Host/StateAuthority) puede reparar el cable remotamente.</summary>
    public void ReportarCableReparado()
    {
        if (!Object.HasStateAuthority) return;
        RPC_FixLooseCable();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FixLooseCable()
    {
        OnCableFixed?.Invoke();
        Debug.Log("[GameSession] Cable suelto reparado (Reto 4).");
    }

    // ─────────────────────────────────────────────
    //  Cambio de reto
    // ─────────────────────────────────────────────

    public void AvanzarReto(int nuevoReto)
    {
        if (!Object.HasStateAuthority) return;
        RPC_CambiarReto(nuevoReto);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CambiarReto(int reto)
    {
        if (Object.HasStateAuthority)
        {
            RetoActual             = reto;
            HayComponentePendiente = false;
        }
        OnRetoChanged?.Invoke(reto);
        Debug.Log($"[GameSession] Nuevo reto: {reto}");
    }

    // ─────────────────────────────────────────────
    //  Validación del circuito
    // ─────────────────────────────────────────────

    /// <summary>Explorador solicita validación — notifica a todos los clientes.</summary>
    public void SolicitarValidacion() => RPC_SolicitarValidacion();

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SolicitarValidacion()
    {
        OnValidacionSolicitada?.Invoke();
        Debug.Log("[GameSession] Validación solicitada.");
    }

    /// <summary>Reporta el resultado de la validación a todos los clientes.</summary>
    public void ReportarResultado(bool paso, int codigoMotivo) =>
        RPC_ReportarResultado(paso, codigoMotivo);

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ReportarResultado(NetworkBool paso, int codigoMotivo)
    {
        OnResultadoValidacion?.Invoke(paso, codigoMotivo);
        Debug.Log($"[GameSession] Resultado validación: {(paso ? "✅" : "❌")} cod={codigoMotivo}");
    }

    // ─────────────────────────────────────────────
    //  Reto 4: código Arduino (canal COMPARTIDO)
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Técnico sube un sketch. Viaja por GameSession (objeto spawneado por el Host y
    /// replicado al Explorador) en lugar del ArduinoNetworkBridge de escena, que no se
    /// replica entre escenas distintas. El Explorador, que tiene el ArduinoCore real, lo
    /// aplica vía ArduinoNetworkBridge.DeliverSketch (que además dispara OnSketchReceived,
    /// por lo que telemetría/validación/monitor siguen reaccionando sin cambios).
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SubirCodigoArduino(int pin, NetworkBool isOutput, NetworkBool isHigh, float delayMs, NetworkBool isBlink)
    {
        ArduinoNetworkBridge.DeliverSketch(pin, isOutput, isHigh, delayMs, isBlink);
        Debug.Log($"[GameSession] Sketch RPC — pin D{pin}, output={isOutput}, blink={isBlink}.");
    }

    // ─────────────────────────────────────────────
    //  Reto 4: telemetría Explorador → Técnico
    // ─────────────────────────────────────────────
    //  La simulación del sandbox (ProtoboardSimulator + ArduinoCore) corre en el Explorador.
    //  El Técnico (Host) NO tiene esos motores localmente, así que recibe la telemetría por
    //  RPC. Lo publica TelemetryPublisher desde el Explorador a ~5 Hz.

    /// <summary>Último voltaje de fuente del sandbox (V).</summary>
    public float TelemVoltage   { get; private set; }
    /// <summary>Última corriente total (mA).</summary>
    public float TelemCurrentmA { get; private set; }
    /// <summary>Última potencia total (W).</summary>
    public float TelemPowerW    { get; private set; }
    /// <summary>Última lectura ADC del A0 (0–1023).</summary>
    public int   TelemAdc       { get; private set; }
    /// <summary>0 = operación segura, 1 = cortocircuito, 2 = circuito abierto.</summary>
    public int   TelemStatus    { get; private set; }
    /// <summary>True tras recibir al menos una muestra por red (distingue "sin datos" de 0 V real).</summary>
    public bool  TelemHasData   { get; private set; }

    /// <summary>El Explorador publica la telemetría del sandbox; llega a todos (incl. Host/Técnico).</summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PublicarTelemetria(float voltage, float currentmA, float powerW, int adc, int status)
    {
        TelemVoltage   = voltage;
        TelemCurrentmA = currentmA;
        TelemPowerW    = powerW;
        TelemAdc       = adc;
        TelemStatus    = status;
        TelemHasData   = true;
    }
}
