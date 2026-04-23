using UnityEngine;

public class VoltageSource : ElectricalComponent
{
    public float voltage = 9f;

    public override float GetResistance()
    {
        return 0;
    }

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        // 🔥 IMPORTANTE: forzar valores cada frame
        nodeA.voltage = voltage;
        nodeB.voltage = 0;

    }
}