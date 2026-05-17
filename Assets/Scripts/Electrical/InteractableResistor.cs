using UnityEngine;

public class InteractableResistor : MonoBehaviour
{
    public Resistor resistor;

    public float[] values = { 10f, 50f, 100f, 200f };
    private int index = 0;

    void OnMouseDown()
    {
        if (resistor == null)
        {
            Debug.LogWarning("[InteractableResistor] Campo 'resistor' no asignado en el Inspector.", this);
            return;
        }

        index = (index + 1) % values.Length;
        resistor.resistance = values[index];

        Debug.Log("Nueva resistencia: " + resistor.resistance);
    }
}