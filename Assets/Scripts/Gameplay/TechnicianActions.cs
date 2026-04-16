using UnityEngine;

public class TechnicianActions : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;
    public Multimeter multimeter;
    public InstructionSystem instructionSystem;

    [Header("Selección actual")]
    public ElectricalComponent selectedComponent;
    private SelectableComponent selectedVisual;

    [Header("Valores correctos")]
    public float correctResistance = 100f;
    public float normalLedResistance = 50f;

    public void SelectComponent(ElectricalComponent component, SelectableComponent visual)
    {
        // reset visual anterior
        if (selectedVisual != null)
        {
            selectedVisual.ResetHighlight();
        }

        selectedComponent = component;
        selectedVisual = visual;

        if (selectedVisual != null)
        {
            selectedVisual.Highlight();
        }

        Debug.Log("🔍 Técnico seleccionó: " + component.name);
    }

    public void ReplaceSelectedResistor()
    {
        if (instructionSystem == null)
        {
            Debug.Log("❌ InstructionSystem no asignado");
            return;
        }

        if (!instructionSystem.CanRepairResistor())
        {
            Debug.Log("❌ Aún no puedes reparar. Primero mide y selecciona la resistencia.");
            return;
        }

        if (selectedComponent == null)
        {
            Debug.Log("❌ No hay componente seleccionado");
            return;
        }

        if (selectedComponent is Resistor r)
        {
            r.resistance = correctResistance;
            Debug.Log("🔧 Resistencia reemplazada correctamente");
        }
        else
        {
            Debug.Log("❌ El componente seleccionado no es una resistencia");
        }
    }

    public void FixParallelCircuit()
    {
        if (instructionSystem == null)
        {
            Debug.Log("❌ InstructionSystem no asignado");
            return;
        }

        if (!instructionSystem.CanRepairParallel())
        {
            Debug.Log("❌ Primero debes medir y diagnosticar la rama fallando.");
            return;
        }

        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                led.resistance = normalLedResistance;
            }
        }

        Debug.Log("🔧 Circuito paralelo reparado");
    }

    public string GetSelectedComponentName()
    {
        return selectedComponent == null ? "None" : selectedComponent.name;
    }

    public bool HasSelectedResistor()
    {
        return selectedComponent is Resistor;
    }
}