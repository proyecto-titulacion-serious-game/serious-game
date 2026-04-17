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
        if (components == null || components.Count == 0) return;

        VoltageSource source = null;
        Resistor resistor = null;
        LED led = null;

        foreach (var comp in components)
        {
            if (comp is VoltageSource vs) source = vs;
            if (comp is Resistor r) resistor = r;
            if (comp is LED l) led = l;
        }

        if (source == null || resistor == null || led == null) return;

        if (source.nodeA == null || source.nodeB == null ||
            resistor.nodeA == null || resistor.nodeB == null ||
            led.nodeA == null || led.nodeB == null)
        {
            return;
        }

        // 🔥 Aplicar fuente PRIMERO
        source.nodeA.voltage = source.voltage;
        source.nodeB.voltage = 0;

        float totalResistance = resistor.resistance + led.resistance;
        if (totalResistance <= 0) return;

        float current = source.voltage / totalResistance;

        float voltageDropResistor = current * resistor.resistance;

        resistor.nodeA.voltage = source.voltage;
        resistor.nodeB.voltage = source.voltage - voltageDropResistor;

        led.nodeA.voltage = resistor.nodeB.voltage;
        led.nodeB.voltage = 0;

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
