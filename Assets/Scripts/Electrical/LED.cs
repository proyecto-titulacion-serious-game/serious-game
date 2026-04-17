using UnityEngine;

public class LED : ElectricalComponent
{
    [Header("Configuración")]
    public float resistance = 50f;
    public Renderer ledRenderer;

    [Header("Estado")]
    public bool isOn;

    public override float GetResistance()
    {
        return resistance;
    }

    public override void Calculate()
    {
        // Seguridad
        if (nodeA == null || nodeB == null || ledRenderer == null) return;
        if (resistance <= 0f)
        {
            current = 0f;
            voltageDrop = 0f;
            isOn = false;
            SetColor(Color.black);
            return;
        }

        float voltageDiff = nodeA.voltage - nodeB.voltage;

        // 🔴 POLARIDAD (muy importante en enseñanza)
        if (voltageDiff <= 0)
        {
            current = 0;
            voltageDrop = 0;
            isOn = false;

            SetColor(Color.black);
            return;
        }

        // ⚡ Ley de Ohm
        current = voltageDiff / resistance;
        voltageDrop = voltageDiff;

        // 🎯 ESTADOS DEL LED (educativo)
        if (current > 0.02f && current < 0.1f)
        {
            // Funcionamiento correcto
            isOn = true;
            SetColor(Color.green);
        }
        else if (current >= 0.1f)
        {
            // Sobrecarga
            isOn = true;
            SetColor(Color.red);
        }
        else
        {
            // Corriente insuficiente
            isOn = false;
            SetColor(Color.black);
        }
    }

    void SetColor(Color color)
    {
        // Mejor práctica: evitar crear materiales duplicados
        ledRenderer.material.color = color;
    }
}
