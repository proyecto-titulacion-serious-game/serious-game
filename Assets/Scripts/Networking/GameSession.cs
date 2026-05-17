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
/// </summary>
public class GameSession : NetworkBehaviour
{
    // ─────────────────────────────────────────────
    //  Singleton de red — acceso fácil desde cualquier script
    // ─────────────────────────────────────────────
    public static GameSession Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Estado compartido (sincronizado por Fusion)
    // ─────────────────────────────────────────────
    [Networked] public int           RetoActual          { get; set; }
    [Networked] public NetworkBool   HayComponentePendiente { get; set; }
    [Networked] public int           TipoComponentePendiente { get; set; }   // cast a ComponentType
    [Networked] public float         ValorComponentePendiente { get; set; }

    // ─────────────────────────────────────────────
    //  Eventos locales — suscríbete desde otros scripts
    // ─────────────────────────────────────────────

    /// <summary>Explorador: se dispara cuando el Técnico envía un componente.</summary>
    public static event System.Action<ComponentType, float> OnComponenteRecibido;

    /// <summary>Técnico: se dispara cuando el Explorador instala (o falla).</summary>
    public static event System.Action<bool>                 OnComponenteInstalado;

    /// <summary>Ambos: el reto cambió.</summary>
    public static event System.Action<int>                  OnRetoChanged;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    public override void Spawned()
    {
        Instance = this;
        Debug.Log($"[GameSession] Spawned. IsMine={Object.HasStateAuthority}  Reto={RetoActual}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────
    //  API del Técnico → Explorador
    // ─────────────────────────────────────────────

    /// <summary>Solo el Técnico (Host/StateAuthority) puede enviar componentes.</summary>
    public void EnviarComponente(ComponentType tipo, float valor)
    {
        if (!Object.HasStateAuthority) return;
        RPC_EnviarComponente((int)tipo, valor);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EnviarComponente(int tipo, float valor)
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
    //  API del Explorador → Técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Explorador llama esto al instalar un componente.
    /// </summary>
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
    //  Cambio de reto (Host lo autoriza)
    // ─────────────────────────────────────────────

    public void AvanzarReto(int nuevoReto)
    {
        if (!Object.HasStateAuthority) return;   // solo el Host (Técnico) puede avanzar
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
}
