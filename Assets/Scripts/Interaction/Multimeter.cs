using UnityEngine;

public class Multimeter : MonoBehaviour
{
    [Header("Referencias")]
    // NUEVO: Ahora referenciamos al GameManager en lugar del CircuitManager
    public GameManager gameManager; 

    [Header("Probes (modo realista)")]
    public ElectricalNode probeA; // punta roja
    public ElectricalNode probeB; // punta negra

    [Header("Resultado")]
    public float measuredVoltage;

    private bool _needsUpdate = true;

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged += MarkDirty;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= MarkDirty;
    }

    void Update()
    {
        if (_needsUpdate)
        {
            MeasureVoltage();
            _needsUpdate = false;
        }
    }

    private void MarkDirty()
    {
        _needsUpdate = true;
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

        // NUEVO: Verificamos que el GameManager y su circuito existan antes de medir
        if (gameManager != null && gameManager.circuit != null)
        {
            // Le pedimos la medición al circuito que el GameManager tiene activo en este momento
            measuredVoltage = gameManager.circuit.GetVoltageBetween(probeA, probeB);
        }
        else
        {
            measuredVoltage = 0f;
        }
    }

    // 🔴 Conectar punta roja
    public void SetProbeA(ElectricalNode node)
    {
        probeA = node;
        _needsUpdate = true;
        Debug.Log("Punta roja conectada a: " + node.name);
    }

    // ⚫ Conectar punta negra
    public void SetProbeB(ElectricalNode node)
    {
        probeB = node;
        _needsUpdate = true;
        Debug.Log("Punta negra conectada a: " + node.name);
    }

    // 🔄 Reset (útil para gameplay)
    public void ResetProbes()
    {
        probeA = null;
        probeB = null;
        measuredVoltage = 0f;
        _needsUpdate = true;

        Debug.Log("🔄 Multímetro reiniciado");
    }
}