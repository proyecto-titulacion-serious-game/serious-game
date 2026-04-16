using UnityEngine;

public class SelectableComponent : MonoBehaviour
{
    public ElectricalComponent component;
    public TechnicianActions technicianActions;

    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    void OnMouseDown()
    {
        if (component == null || technicianActions == null) return;

        technicianActions.SelectComponent(component, this);
        Debug.Log("🔍 Componente seleccionado: " + component.name);
    }

    public void Highlight()
    {
        if (rend != null)
        {
            rend.material.color = Color.yellow;
        }
    }

    public void ResetHighlight()
    {
        if (rend != null)
        {
            rend.material.color = originalColor;
        }
    }
}