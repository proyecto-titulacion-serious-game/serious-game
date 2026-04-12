using UnityEngine;

public class Multimeter : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;

    [Header("Probes (modo realista)")]
    public ElectricalNode probeA; // punta roja
    public ElectricalNode probeB; // punta negra

    [Header("Resultado")]
    public float measuredVoltage;

    void Update()
    {
        MeasureVoltage();
    }

    void MeasureVoltage()
    {
        // Si no están conectadas ambas puntas
        if (probeA == null || probeB == null)
        {
            measuredVoltage = 0f;
            return;
        }

        // Evitar medir el mismo nodo
        if (probeA == probeB)
        {
            measuredVoltage = 0f;
            return;
        }

        // Medición real
        measuredVoltage = circuit.GetVoltageBetween(probeA, probeB);
    }

    // 🔴 Conectar punta roja
    public void SetProbeA(ElectricalNode node)
    {
        probeA = node;
        Debug.Log("🔴 Punta roja conectada a: " + node.name);
    }

    // ⚫ Conectar punta negra
    public void SetProbeB(ElectricalNode node)
    {
        probeB = node;
        Debug.Log("⚫ Punta negra conectada a: " + node.name);
    }

    // 🔄 Reset (útil para gameplay)
    public void ResetProbes()
    {
        probeA = null;
        probeB = null;
        measuredVoltage = 0f;

        Debug.Log("🔄 Multímetro reiniciado");
    }
}