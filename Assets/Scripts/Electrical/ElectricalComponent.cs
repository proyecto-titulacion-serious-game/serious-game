using UnityEngine;

public abstract class ElectricalComponent : MonoBehaviour
{
    public ElectricalNode nodeA;
    public ElectricalNode nodeB;

    public float current;
    public float voltageDrop;

    public abstract float GetResistance();
    public abstract void Calculate();
}