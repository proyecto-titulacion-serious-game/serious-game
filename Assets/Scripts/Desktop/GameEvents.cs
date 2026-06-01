using System;

/// <summary>
/// Bus de eventos global desacoplado para la estacion de trabajo del Tecnico.
/// Cualquier modulo (bandeja, manual, luces, diagnostico) se suscribe
/// a los eventos que le interesan sin conocer a los demas.
///
/// Patron Observer — ningun modulo referencia directamente a otro.
/// </summary>
public static class GameEvents
{
    // ── Circuito ──────────────────────────────────────────────────────
    /// <summary>El motor electrico recalculo el circuito.</summary>
    public static event Action OnCircuitUpdated;

    // ── Componentes ───────────────────────────────────────────────────
    /// <summary>El Tecnico selecciono un componente de la bandeja.</summary>
    public static event Action<ComponentType, float> OnComponentSelected;

    /// <summary>El Tecnico envio un componente al Explorador.</summary>
    public static event Action<ComponentType, float> OnComponentSent;

    // ── Retos ─────────────────────────────────────────────────────────
    /// <summary>Cambio de reto activo.</summary>
    public static event Action<LevelType> OnLevelChanged;

    /// <summary>El sistema emitio un mensaje de diagnostico.</summary>
    public static event Action<string> OnDiagnosticMessage;

    // ── Dispatchers (llamar desde los sistemas existentes) ────────────

    public static void RaiseCircuitUpdated()
        => OnCircuitUpdated?.Invoke();

    public static void RaiseComponentSelected(ComponentType tipo, float valor)
        => OnComponentSelected?.Invoke(tipo, valor);

    public static void RaiseComponentSent(ComponentType tipo, float valor)
        => OnComponentSent?.Invoke(tipo, valor);

    public static void RaiseLevelChanged(LevelType level)
        => OnLevelChanged?.Invoke(level);

    public static void RaiseDiagnosticMessage(string msg)
        => OnDiagnosticMessage?.Invoke(msg);
}
