using UnityEngine;

public class NodeInteractable : MonoBehaviour
{
    public ElectricalNode node;
    public Multimeter multimeter;

    private bool isFirst = true;

    void OnMouseDown()
    {
        if (isFirst)
        {
            multimeter.SetNodeA(node);
            Debug.Log("Nodo A seleccionado");
        }
        else
        {
            multimeter.SetNodeB(node);
            Debug.Log("Nodo B seleccionado");
        }

        isFirst = !isFirst;
    }
}