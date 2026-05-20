using UnityEngine;

public abstract class ElectricalComponent : MonoBehaviour
{
    public ElectricalNode nodeA;
    public ElectricalNode nodeB;

    public float current;
    public float voltageDrop;

    [Tooltip("Simula conexión abierta (cable cortado). GetResistance() devuelve 1 MΩ en las subclases que lo soporten.")]
    public bool isOpenCircuit = false;

    public abstract float GetResistance();
    public abstract void Calculate();
}