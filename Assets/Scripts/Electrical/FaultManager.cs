using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// API centralizada para aplicar y limpiar los 6 tipos de fallas del Serious Game.
///
/// TIPOS DE FALLA SOPORTADOS:
///   1. ShortCircuit       — Cortocircuito por conexión incorrecta
///   2. PolarityError      — Error de polaridad (LED / Capacitor)
///   3. WrongResistor      — Resistencia incorrecta (código de colores)
///   4. PowerSupplyFault   — Falla de fuente (sobrevoltaje / voltaje insuficiente)
///   5. OpenCircuit        — Conexión abierta en circuito serie
///   6. ComponentOverload  — Sobrecarga de componente (resistencia con potencia insuficiente)
///
/// DISEÑO: No reemplaza la lógica de GameManager.SetupRetoX() —
/// es una capa de utilidades que puede usarse desde GameManager, pruebas de Unity Editor,
/// o nuevos retos que quieran configurar fallas de forma declarativa.
/// </summary>
public class FaultManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Tipos de falla
    // ─────────────────────────────────────────────
    public enum FaultType
    {
        ShortCircuit,
        PolarityError,
        WrongResistor,
        PowerSupplyFault,
        OpenCircuit,
        ComponentOverload
    }

    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public CircuitManager circuit;

    [Header("Fallas activas (solo lectura)")]
    [SerializeField] private List<string> _activeFaults = new List<string>();

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    public static event Action<FaultType, string> OnFaultApplied;
    public static event Action<FaultType>          OnFaultCleared;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (circuit == null)
            circuit = FindAnyObjectByType<CircuitManager>();
    }

    // ─────────────────────────────────────────────
    //  1. Cortocircuito
    // ─────────────────────────────────────────────

    /// <summary>Fuerza cortocircuito invirtiendo la polaridad del capacitor.</summary>
    public void ApplyShortCircuit(Capacitor cap)
    {
        cap.polarityInverted = true;
        MarkAndNotify(FaultType.ShortCircuit,
            $"Cortocircuito: {cap.name} — polaridad invertida (R ≈ {cap.shortCircuitResistance} Ω)");
    }

    /// <summary>
    /// Cortocircuito directo en un resistor (R→0 Ω).
    /// Simula un cable suelto que une dos nodos.
    /// </summary>
    public void ApplyWireShort(Resistor r)
    {
        r.resistance = 0f;
        r.hasFault   = true;
        MarkAndNotify(FaultType.ShortCircuit,
            $"Cortocircuito: cable en cortocircuito entre nodos de {r.name}");
    }

    // ─────────────────────────────────────────────
    //  2. Error de polaridad
    // ─────────────────────────────────────────────

    public void ApplyPolarityError(LED led, bool invert = true)
    {
        led.polarityInverted = invert;
        MarkAndNotify(FaultType.PolarityError,
            $"Polaridad incorrecta en LED {led.name}");
    }

    public void ApplyPolarityError(Capacitor cap, bool invert = true)
    {
        cap.polarityInverted = invert;
        MarkAndNotify(FaultType.PolarityError,
            $"Polaridad incorrecta en Capacitor {cap.name}");
    }

    // ─────────────────────────────────────────────
    //  3. Resistencia incorrecta
    // ─────────────────────────────────────────────

    /// <param name="wrongValue">Valor defectuoso en Ω que verá el Explorador.</param>
    public void ApplyWrongResistor(Resistor r, float wrongValue)
    {
        r.faultyResistance = wrongValue;
        r.ApplyFault();
        MarkAndNotify(FaultType.WrongResistor,
            $"Resistencia incorrecta: {r.name} = {wrongValue} Ω (correcto: {r.correctResistance} Ω)\n" +
            $"Código de colores: {r.GetColorBandString()}");
    }

    // ─────────────────────────────────────────────
    //  4. Falla en fuente de alimentación
    // ─────────────────────────────────────────────

    public void ApplyOvervoltageFault(VoltageSource source)
    {
        source.ApplyOvervoltageFault();
        MarkAndNotify(FaultType.PowerSupplyFault,
            $"Sobrevoltaje: fuente '{source.name}' aplica {source.overvoltageValue} V " +
            $"(nominal: {source.voltage} V)");
    }

    public void ApplyUndervoltageFault(VoltageSource source)
    {
        source.ApplyUndervoltageFault();
        MarkAndNotify(FaultType.PowerSupplyFault,
            $"Voltaje insuficiente: fuente '{source.name}' aplica {source.undervoltageValue} V " +
            $"(nominal: {source.voltage} V)");
    }

    public void RestorePowerSupply(VoltageSource source)
    {
        source.RestoreNominalVoltage();
        ClearFault(FaultType.PowerSupplyFault);
    }

    // ─────────────────────────────────────────────
    //  5. Conexión abierta
    // ─────────────────────────────────────────────

    /// <summary>Simula un cable cortado en cualquier componente.</summary>
    public void ApplyOpenCircuit(ElectricalComponent comp)
    {
        comp.isOpenCircuit = true;
        MarkAndNotify(FaultType.OpenCircuit,
            $"Conexión abierta en {comp.name} — sin continuidad");
    }

    /// <summary>Cable suelto en pin Arduino (usa la lógica nativa de ArduinoPin).</summary>
    public void ApplyLooseCable(ArduinoPin pin)
    {
        pin.hasLooseCable = true;
        MarkAndNotify(FaultType.OpenCircuit,
            $"Cable suelto: {pin.name} (pin {pin.pinNumber}) — circuito abierto");
    }

    public void RestoreConnection(ElectricalComponent comp)
    {
        comp.isOpenCircuit = false;
        ClearFault(FaultType.OpenCircuit);
    }

    // ─────────────────────────────────────────────
    //  6. Sobrecarga de componente
    // ─────────────────────────────────────────────

    /// <summary>
    /// Asigna una potencia nominal insuficiente para las condiciones del circuito.
    /// El resistor seguirá funcionando, pero <c>isOverloaded</c> se activará en la simulación.
    /// </summary>
    public void ApplyInsufficientPowerRating(Resistor r, float tooLowRatingWatts)
    {
        r.powerRatingWatts = tooLowRatingWatts;
        MarkAndNotify(FaultType.ComponentOverload,
            $"Sobrecarga: {r.name} tiene potencia nominal {tooLowRatingWatts} W — insuficiente para el circuito");
    }

    // ─────────────────────────────────────────────
    //  Limpiar todas las fallas
    // ─────────────────────────────────────────────

    /// <summary>
    /// Elimina todas las fallas del circuito y restaura el estado nominal.
    /// </summary>
    public void ClearAllFaults()
    {
        if (circuit == null) return;

        var comps = circuit.GetComponentsInChildren<ElectricalComponent>(true);
        foreach (var comp in comps)
        {
            comp.isOpenCircuit = false;

            if (comp is Resistor r)
            {
                r.Repair();
                r.powerRatingWatts = 0.25f;
            }
            if (comp is LED led)     led.polarityInverted = false;
            if (comp is Capacitor c) c.polarityInverted   = false;
            if (comp is VoltageSource vs) vs.RestoreNominalVoltage();
            if (comp is ArduinoPin pin) { pin.hasFault = false; pin.hasLooseCable = false; }
        }

        _activeFaults.Clear();
        circuit.MarkDirty();

        Debug.Log("[FaultManager] Todas las fallas eliminadas.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void MarkAndNotify(FaultType type, string description)
    {
        _activeFaults.Add($"[{type}] {description}");
        circuit?.MarkDirty();
        OnFaultApplied?.Invoke(type, description);
        GameManager.RaiseFaultDetected(description);
        Debug.Log($"[FaultManager] {type}: {description}");
    }

    void ClearFault(FaultType type)
    {
        _activeFaults.RemoveAll(f => f.StartsWith($"[{type}]"));
        circuit?.MarkDirty();
        OnFaultCleared?.Invoke(type);
    }

    public bool HasActiveFaults() => _activeFaults.Count > 0;
    public IReadOnlyList<string> GetActiveFaults() => _activeFaults.AsReadOnly();
}
