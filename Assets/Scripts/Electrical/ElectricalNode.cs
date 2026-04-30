using System.Collections.Generic;
using UnityEngine;

public class ElectricalNode : MonoBehaviour
{
    public float voltage;
    public float current = 0f;   

    public List<ElectricalComponent> connectedComponents = new List<ElectricalComponent>();
}