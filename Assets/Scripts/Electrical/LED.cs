using UnityEngine;

public class LED : ElectricalComponent
{
    public float resistance = 50f;
    public Renderer ledRenderer;

    public override float GetResistance()
    {
        return resistance;
    }

    public override void Calculate()
    {
        float voltageDiff = nodeA.voltage - nodeB.voltage;

        current = voltageDiff / resistance;
        voltageDrop = voltageDiff;

        if (current > 0.02f && current < 0.1f)
            ledRenderer.material.color = Color.green;
        else if (current >= 0.1f)
            ledRenderer.material.color = Color.red;
        else
            ledRenderer.material.color = Color.black;
    }
}