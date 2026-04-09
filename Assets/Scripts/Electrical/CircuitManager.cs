using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    [Header("Estructura del circuito")]
    public List<ElectricalNode> nodes;
    public List<ElectricalComponent> components;

    [Header("Datos globales")]
    public float totalCurrent;

    void Update()
    {
        Simulate();
    }

    void Simulate()
    {
        // 1. Reset nodos
        foreach (var node in nodes)
        {
            node.voltage = 0f;
        }

        VoltageSource source = null;
        Resistor resistor = null;
        LED led = null;

        // 2. Identificar componentes
        foreach (var comp in components)
        {
            if (comp is VoltageSource vs) source = vs;
            if (comp is Resistor r) resistor = r;
            if (comp is LED l) led = l;
        }

        if (source == null || resistor == null || led == null) return;

        // 3. Aplicar fuente
        source.Calculate();

        float totalResistance = resistor.resistance + led.resistance;

        if (totalResistance <= 0) return;

        // 4. Corriente
        float current = source.voltage / totalResistance;

        // 5. Caída de voltaje
        float voltageDropResistor = current * resistor.resistance;

        // 6. Propagar voltajes
        resistor.nodeA.voltage = source.voltage;
        resistor.nodeB.voltage = source.voltage - voltageDropResistor;

        led.nodeA.voltage = resistor.nodeB.voltage;
        led.nodeB.voltage = 0;

        // 7. Aplicar cálculos
        resistor.Calculate();
        led.Calculate();

        totalCurrent = current;
    }

    public float GetVoltageBetween(ElectricalNode a, ElectricalNode b)
    {
        if (a == null || b == null) return 0f;
        return a.voltage - b.voltage;
    }
}