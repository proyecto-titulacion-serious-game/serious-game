using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simula el circuito eléctrico para los 4 retos del Serious Game.
/// Topología configurable: Serie (Reto 1), Paralelo (Reto 2), Mixto (Reto 3).
/// Usa eventos en lugar de polling en Update() para evitar spam de Debug.Log.
/// </summary>
public class CircuitManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Componentes del circuito")]
    public List<ElectricalComponent> components = new List<ElectricalComponent>();

    [Header("Topología activa")]
    public CircuitTopology topology = CircuitTopology.Series;

    [Header("Resultados (solo lectura)")]
    [SerializeField] private float _totalCurrent;
    [SerializeField] private float _sourceVoltage;
    [SerializeField] private float _totalPower;       // NUEVO: Potencia total en Watts
    public bool isShortCircuited = false;             // NUEVO: Bandera de peligro

    [Header("Simulación")]
    [Tooltip("Segundos entre cada simulación. 0.05 = 20 veces por segundo.")]
    [SerializeField] private float simulationInterval = 0.05f;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public float totalCurrent  => _totalCurrent;
    public float sourceVoltage => _sourceVoltage;
    public float totalPower    => _totalPower;        // NUEVO: Acceso público a la potencia

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    /// <summary>Se dispara cada vez que el circuito es resimulado.</summary>
    public static event Action OnCircuitChanged;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool _dirty = true;   // Arrancar con simulación inicial

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        AutoDetectComponents();
    }

    public void AutoDetectComponents()
    {
        bool needsRefresh = components.Count == 0;

        if (!needsRefresh)
        {
            foreach (var c in components)
                if (c == null) { needsRefresh = true; break; }
        }

        if (needsRefresh)
        {
            components.Clear();
            var found = GetComponentsInChildren<ElectricalComponent>();

            foreach (var c in found)
                if (c is VoltageSource) components.Add(c);
            foreach (var c in found)
                if (!(c is VoltageSource)) components.Add(c);

            Debug.Log($"[CircuitManager] Auto-detectados {components.Count} componentes");
        }
    }

    void Start()
    {
        //Simula cada intervalo de segundo
        AutoDetectComponents();
        InvokeRepeating(nameof(SimulateIfDirty), 0f, simulationInterval);
    }

    void OnDestroy()
    {
        CancelInvoke();
    }

    // ─────────────────────────────────────────────
    //  API Pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Marca el circuito como modificado para que se resimule en el próximo tick.
    /// Llamar cuando se cambie una resistencia, se repare una conexión, etc.
    /// </summary>
    public void MarkDirty() => _dirty = true;

    /// <summary>Fuerza una simulación inmediata (útil al cargar un nivel).</summary>
    public void ForceSimulate()
    {
        RunSimulation();
        OnCircuitChanged?.Invoke();
    }

    /// <summary>Voltaje entre dos nodos (medición del multímetro).</summary>
    public float GetVoltageBetween(ElectricalNode a, ElectricalNode b)
    {
        if (a == null || b == null) return 0f;
        return a.voltage - b.voltage;
    }

    /// <summary>¿Todos los LEDs del circuito están encendidos? Retorna false si no hay LEDs.</summary>
    public bool AreAllLEDsOn()
    {
        bool foundAny = false;
        foreach (var comp in components)
        {
            if (comp is LED led)
            {
                foundAny = true;
                if (!led.isOn) return false;
            }
        }
        return foundAny;
    }

    /// <summary>Primer componente del tipo T encontrado en la lista.</summary>
    public T FindCircuitComponent<T>() where T : ElectricalComponent
    {
        foreach (var c in components)
            if (c is T found) return found;
        return null;
    }

    // ─────────────────────────────────────────────
    //  Simulación interna
    // ─────────────────────────────────────────────

    void SimulateIfDirty()
    {
        if (!_dirty) return;
        _dirty = false;
        RunSimulation();
        OnCircuitChanged?.Invoke();
    }

    void RunSimulation()
    {
        if (components.Count == 0) return;
        switch (topology)
        {
            case CircuitTopology.Series:   SimulateSeries();   break;
            case CircuitTopology.Parallel: SimulateParallel(); break;
            case CircuitTopology.Mixed:    SimulateMixed();    break;
        }
    }

    // ── SERIE ──────────────────────────────────
    void SimulateSeries()
    {
        VoltageSource source = GetFirstSource();
        if (source == null) return;

        _sourceVoltage = source.voltage;
        float totalR = 0f;

        // Sumar resistencia total (excepto la fuente)
        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;
            totalR += comp.GetResistance();
        }

        // 💥 DETECCIÓN DE CORTOCIRCUITO
        if (totalR <= 0.1f)
        {
            _totalCurrent = 0f;
            _totalPower = 0f;
            isShortCircuited = true;
            Debug.LogWarning("[CircuitManager] ¡CORTOCIRCUITO! Falta resistencia en el circuito.");
            return;
        }

        // Si todo está bien, calculamos la física real
        isShortCircuited = false;
        _totalCurrent = _sourceVoltage / totalR;           // Ley de Ohm
        _totalPower = _sourceVoltage * _totalCurrent;      // Ley de Watt

        // Nodos de la fuente: + = sourceVoltage, – = 0 V (referencia de tierra)
        if (source.nodeA != null) { source.nodeA.voltage = _sourceVoltage; source.nodeA.current = _totalCurrent; }
        if (source.nodeB != null) { source.nodeB.voltage = 0f;             source.nodeB.current = _totalCurrent; }

        // Aplicar caídas de voltaje en secuencia
        float voltageAtNode = _sourceVoltage;
        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;

            if (comp.nodeA != null)
            {
                comp.nodeA.voltage = voltageAtNode;
                comp.nodeA.current = _totalCurrent;
            }
            float drop = _totalCurrent * comp.GetResistance();
            voltageAtNode -= drop;
            if (comp.nodeB != null)
            {
                comp.nodeB.voltage = voltageAtNode;
                comp.nodeB.current = _totalCurrent;
            }

            comp.Calculate();
        }
    }

    // ── PARALELO ───────────────────────────────
    void SimulateParallel()
    {
        VoltageSource source = GetFirstSource();
        if (source == null) return;

        _sourceVoltage = source.voltage;
        if (source.nodeA != null) source.nodeA.voltage = _sourceVoltage;
        if (source.nodeB != null) source.nodeB.voltage = 0f;

        _totalCurrent = 0f;

        // Cada rama recibe el voltaje completo de la fuente
        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;

            if (comp.nodeA != null) comp.nodeA.voltage = _sourceVoltage;
            if (comp.nodeB != null) comp.nodeB.voltage = 0f;

            comp.Calculate();

            if (comp.nodeA != null) comp.nodeA.current = comp.current;
            if (comp.nodeB != null) comp.nodeB.current = comp.current;
            _totalCurrent += comp.current;
        }
    }

    // ── MIXTO (Serie-Paralelo) ─────────────────
    // Topología Reto 3: Resistor(es) en SERIE → bloque PARALELO (LED + Capacitor).
    // Recalcula voltajes de nodo dinámicamente en cada tick para reflejar
    // el estado actual de los componentes (reparaciones del jugador incluidas).
    void SimulateMixed()
    {
        VoltageSource source = GetFirstSource();
        if (source == null) return;
        _sourceVoltage = source.voltage;

        float seriesR           = 0f;
        float parallelConductance = 0f;
        var seriesComps   = new List<ElectricalComponent>();
        var parallelComps = new List<ElectricalComponent>();

        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;
            if (comp is Resistor)
            {
                seriesR += comp.GetResistance();
                seriesComps.Add(comp);
            }
            else
            {
                float r = comp.GetResistance();
                if (r > 0f) parallelConductance += 1f / r;
                parallelComps.Add(comp);
            }
        }

        float parallelEquivR = parallelConductance > 0f ? 1f / parallelConductance : 1_000_000f;
        float totalR         = seriesR + parallelEquivR;

        if (totalR <= 0.1f)
        {
            _totalCurrent    = 0f;
            _totalPower      = 0f;
            isShortCircuited = true;
            Debug.LogWarning("[CircuitManager] ¡CORTOCIRCUITO en circuito mixto!");
            return;
        }

        isShortCircuited = false;
        _totalCurrent    = _sourceVoltage / totalR;
        _totalPower      = _sourceVoltage * _totalCurrent;

        // Distribuir caídas de voltaje en el bloque serie
        float voltageAtNode = _sourceVoltage;
        foreach (var comp in seriesComps)
        {
            float drop = _totalCurrent * comp.GetResistance();
            if (comp.nodeA != null) { comp.nodeA.voltage = voltageAtNode; comp.nodeA.current = _totalCurrent; }
            voltageAtNode -= drop;
            if (comp.nodeB != null) { comp.nodeB.voltage = voltageAtNode; comp.nodeB.current = _totalCurrent; }
            comp.Calculate();
        }

        // Aplicar voltaje de unión al bloque paralelo
        float vParallel = voltageAtNode;
        foreach (var comp in parallelComps)
        {
            if (comp.nodeA != null) comp.nodeA.voltage = vParallel;
            if (comp.nodeB != null) comp.nodeB.voltage = 0f;
            comp.Calculate();
        }
    }

    VoltageSource GetFirstSource()
    {
        foreach (var c in components)
            if (c is VoltageSource vs) return vs;

        Debug.LogWarning("[CircuitManager] No se encontró VoltageSource en la lista de componentes.");
        return null;
    }
}

public enum CircuitTopology
{
    Series,    // Reto 1
    Parallel,  // Reto 2
    Mixed      // Reto 3 y 4
}
