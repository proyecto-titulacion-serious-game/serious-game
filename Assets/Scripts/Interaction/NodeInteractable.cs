using UnityEngine;

public class NodeInteractable : MonoBehaviour
{
    public ElectricalNode node;
    public Multimeter multimeter;

    private static bool assignToA = true;

    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
            originalColor = rend.material.color;
    }

    void OnMouseDown()
    {
        if (multimeter == null || node == null) return;

        if (assignToA)
        {
            multimeter.probeA = node;
            Debug.Log("🔴 Cable rojo conectado a: " + node.name);
            SetColor(Color.red);
        }
        else
        {
            multimeter.probeB = node;
            Debug.Log("⚫ Cable negro conectado a: " + node.name);
            SetColor(Color.black);
        }

        assignToA = !assignToA;
    }

    void SetColor(Color color)
    {
        if (rend != null)
            rend.material.color = color;
    }

    // Opcional: reset visual
    public void ResetColor()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }
}