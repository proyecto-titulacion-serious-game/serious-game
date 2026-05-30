using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Motor matemático de la protoboard sandbox (Reto 4).
/// Monitorea la matriz de ProtoboardSlots, construye el grafo eléctrico por railId
/// y calcula V, I y P mediante análisis nodal simplificado.
///
/// Diferencia con CircuitSimulator: la topología se deduce dinámicamente de qué
/// componentes/cables están colocados en qué slots, sin listas hardcodeadas.
///
/// SETUP: añadir este script al GameObject padre de la protoboard y rellenar
/// todosLosSlots usando Tools > TITA > Generador de Slots.
/// </summary>
public class ProtoboardSimulator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Protoboard")]
    [Tooltip("Todos los ProtoboardSlots de la cuadrícula. Rellenar con el generador de Editor.")]
    public List<ProtoboardSlot> todosLosSlots = new List<ProtoboardSlot>();

    [Header("Telemetría (solo lectura)")]
    [SerializeField] private float _sourceVoltage;
    [SerializeField] private float _totalCurrentmA;   // miliamperios
    [SerializeField] private float _totalPowerW;      // Watts
    [SerializeField] private bool  _isShortCircuited;
    [SerializeField] private bool  _isOpenCircuit;

    [Header("Simulación")]
    [SerializeField] private float _interval = 0.05f; // 20 Hz

    // ─────────────────────────────────────────────
    //  Propiedades públicas (lectura de telemetría)
    // ─────────────────────────────────────────────
    public float sourceVoltage    => _sourceVoltage;
    /// <summary>Corriente total en miliamperios (mA).</summary>
    public float totalCurrentmA   => _totalCurrentmA;
    /// <summary>Potencia total disipada en Watts (W).</summary>
    public float totalPowerW      => _totalPowerW;
    public bool  isShortCircuited => _isShortCircuited;
    public bool  isOpenCircuit    => _isOpenCircuit;

    // ─────────────────────────────────────────────
    //  Evento
    // ─────────────────────────────────────────────
    /// <summary>Dispara cada vez que el simulador recalcula el circuito.</summary>
    public static event Action OnCircuitChanged;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool _dirty = true;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        StartCoroutine(SimLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────
    /// <summary>Solicita una nueva simulación en el próximo tick.</summary>
    public void MarkDirty() => _dirty = true;

    // ─────────────────────────────────────────────
    //  Bucle de simulación
    // ─────────────────────────────────────────────
    IEnumerator SimLoop()
    {
        var wait = new WaitForSeconds(_interval);
        while (true)
        {
            if (_dirty)
            {
                _dirty = false;
                RunSimulation();
                OnCircuitChanged?.Invoke();
            }
            yield return wait;
        }
    }

    // ─────────────────────────────────────────────
    //  Núcleo: construcción del mapa de nodos
    // ─────────────────────────────────────────────

    /// <summary>
    /// Agrupa los slots por railId. El primer slot de cada grupo actúa como nodo
    /// representativo (se le añade o recicla su ElectricalNode component).
    /// </summary>
    void BuildNodeMap()
    {
        var representatives = new Dictionary<string, ElectricalNode>();

        foreach (var slot in todosLosSlots)
        {
            if (string.IsNullOrEmpty(slot.railId)) continue;

            if (!representatives.TryGetValue(slot.railId, out ElectricalNode node))
            {
                node = slot.GetComponent<ElectricalNode>();
                if (node == null) node = slot.gameObject.AddComponent<ElectricalNode>();
                node.voltage = 0f;
                node.current = 0f;
                representatives[slot.railId] = node;
            }

            slot.assignedNode = node;
        }
    }

    // ─────────────────────────────────────────────
    //  Núcleo: simulación
    // ─────────────────────────────────────────────
    void RunSimulation()
    {
        BuildNodeMap();

        var allComps = GetComponentsInChildren<ElectricalComponent>(true)
            .Where(c => c.nodeA != null && c.nodeB != null)
            .ToList();

        var source = allComps.OfType<VoltageSource>().FirstOrDefault();
        float srcV;
        ElectricalNode srcNodeA = null, srcNodeB = null;

        if (source != null)
        {
            srcV     = source.voltage;
            srcNodeA = source.nodeA;
            srcNodeB = source.nodeB;
        }
        else
        {
            // Sandbox Arduino: ArduinoCore actúa como fuente (Pin 13 → GND)
            var arduino = GetComponentInChildren<ArduinoCore>(true)
                       ?? FindAnyObjectByType<ArduinoCore>();
            if (arduino == null || arduino.activePinMode != PinMode.OUTPUT || arduino.activePinNumber != 13)
            {
                ClearTelemetry(openCircuit: true);
                return;
            }
            // Usar 5 V cuando blink activo (simula HIGH para telemetría educativa estable)
            srcV     = arduino.blinkEnabled ? 5f : arduino.OutputVoltage;
            srcNodeA = arduino.nodoP13;
            srcNodeB = arduino.nodoGND;
        }

        if (srcV <= 0.001f || allComps.Count <= 1)
        {
            ClearTelemetry(openCircuit: true);
            return;
        }

        _sourceVoltage = srcV;

        float totalR = allComps
            .Where(c => !(c is VoltageSource))
            .Sum(c => c.GetResistance());

        _isShortCircuited = totalR <= 0.1f;
        _isOpenCircuit    = totalR >= 999_000f;

        if (_isShortCircuited)
        {
            float faultI = srcV / 0.1f;
            _totalCurrentmA = faultI * 1000f;
            _totalPowerW    = faultI * faultI * 0.1f;
            foreach (var c in allComps) { c.current = faultI; c.voltageDrop = 0f; }
            return;
        }

        if (_isOpenCircuit)
        {
            ClearTelemetry(openCircuit: true);
            return;
        }

        float I = srcV / totalR;
        _totalCurrentmA = I * 1000f;
        _totalPowerW    = I * I * totalR;

        if (srcNodeA != null) { srcNodeA.voltage = srcV; srcNodeA.current = I; }
        if (srcNodeB != null) { srcNodeB.voltage = 0f;   srcNodeB.current = I; }

        float runningV = srcV;
        foreach (var comp in allComps.Where(c => !(c is VoltageSource)))
        {
            comp.current     = I;
            comp.voltageDrop = I * comp.GetResistance();
            runningV        -= comp.voltageDrop;
            if (comp.nodeB != null) { comp.nodeB.voltage = runningV; comp.nodeB.current = I; }
            comp.Calculate();
        }
    }

    void ClearTelemetry(bool openCircuit)
    {
        _sourceVoltage    = 0f;
        _totalCurrentmA   = 0f;
        _totalPowerW      = 0f;
        _isShortCircuited = false;
        _isOpenCircuit    = openCircuit;
    }
}
