using UnityEngine;

public class Resistor : ElectricalComponent
{
    public float resistance = 100f;

    public override float GetResistance()
    {
        return resistance;
    }

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        float voltageDiff = nodeA.voltage - nodeB.voltage;

        current = voltageDiff / resistance;
        voltageDrop = voltageDiff;
    }
}