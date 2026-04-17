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
        if (resistance <= 0f)
        {
            current = 0f;
            voltageDrop = 0f;
            return;
        }

        float voltageDiff = nodeA.voltage - nodeB.voltage;

        current = voltageDiff / resistance;
        voltageDrop = voltageDiff;
    }
}
