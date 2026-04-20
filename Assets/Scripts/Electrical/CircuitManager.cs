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

    [Header("Simulación")]
    [Tooltip("Segundos entre cada simulación. 0.05 = 20 veces por segundo.")]
    [SerializeField] private float simulationInterval = 0.05f;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public float totalCurrent  => _totalCurrent;
    public float sourceVoltage => _sourceVoltage;

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
    void Start()
    {
        // Simular cada simulationInterval segundos, NO en Update()
        InvokeRepeating(nameof(SimulateIfDirty), 0f, simulationInterval);
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

    /// <summary>¿Todos los LEDs del circuito están encendidos?</summary>
    public bool AreAllLEDsOn()
    {
        foreach (var comp in components)
            if (comp is LED led && !led.isOn) return false;
        return true;
    }

    /// <summary>Primer componente del tipo T encontrado en la lista.</summary>
    public T GetComponent<T>() where T : ElectricalComponent
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

        // Resistencia total = suma de todos los componentes (excepto la fuente)
        float totalR = 0f;
        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;
            totalR += comp.GetResistance();
        }

        if (totalR <= 0f) { _totalCurrent = 0f; return; }

        _totalCurrent = _sourceVoltage / totalR;

        // Aplicar caídas de voltaje en secuencia
        float voltageAtNode = _sourceVoltage;
        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;

            if (comp.nodeA != null) comp.nodeA.voltage = voltageAtNode;
            float drop = _totalCurrent * comp.GetResistance();
            voltageAtNode -= drop;
            if (comp.nodeB != null) comp.nodeB.voltage = voltageAtNode;

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
            _totalCurrent += comp.current;
        }
    }

    // ── MIXTO (Serie-Paralelo) ─────────────────
    // Para Reto 3: grupos de ramas asignados por GameManager
    // Cada ElectricalNode ya tiene su voltaje seteado por GameManager.SetupMixed()
    void SimulateMixed()
    {
        _totalCurrent = 0f;

        foreach (var comp in components)
        {
            if (comp is VoltageSource) continue;
            comp.Calculate();
            _totalCurrent += comp.current;
        }

        VoltageSource source = GetFirstSource();
        if (source != null) _sourceVoltage = source.voltage;
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