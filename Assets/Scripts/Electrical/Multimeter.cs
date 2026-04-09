using UnityEngine;

public class Multimeter : MonoBehaviour
{
    public CircuitManager circuit;

    [Header("Nodos seleccionados")]
    public ElectricalNode nodeA;
    public ElectricalNode nodeB;

    [Header("Resultado")]
    public float measuredVoltage;

    void Update()
    {
        if (nodeA != null && nodeB != null)
        {
            measuredVoltage = circuit.GetVoltageBetween(nodeA, nodeB);
        }
    }

    public void SetNodeA(ElectricalNode node)
    {
        nodeA = node;
    }

    public void SetNodeB(ElectricalNode node)
    {
        nodeB = node;
    }
}