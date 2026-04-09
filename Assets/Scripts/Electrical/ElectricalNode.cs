using System.Collections.Generic;
using UnityEngine;

public class ElectricalNode : MonoBehaviour
{
    public float voltage;

    public List<ElectricalComponent> connectedComponents = new List<ElectricalComponent>();
}