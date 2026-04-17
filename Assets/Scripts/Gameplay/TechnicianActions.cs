using UnityEngine;

public class TechnicianActions : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;
    public Multimeter multimeter;
    public InstructionSystem instructionSystem;
    public GameManager gameManager;
    public PerformanceTracker performance;

    [Header("Selección actual")]
    public ElectricalComponent selectedComponent;
    private SelectableComponent selectedVisual;

    [Header("Valores correctos")]
    public float correctResistance = 100f;
    public float normalLedResistance = 50f;

    public void SelectComponent(ElectricalComponent component, SelectableComponent visual)
    {
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

        // Penalización contextual solo para Reto 1
        if (gameManager != null &&
            gameManager.currentLevel == LevelType.OhmLaw &&
            !(component is Resistor))
        {
            RegisterError("Seleccion de componente incorrecto en Reto 1");
            Debug.Log("❌ Seleccionaste un componente incorrecto. En este reto debes elegir la resistencia.");
        }
        else
        {
            Debug.Log("🔍 Técnico seleccionó: " + component.name);
        }
    }

    public void ReplaceSelectedResistor()
    {
        if (instructionSystem == null)
        {
            Debug.Log("❌ InstructionSystem no asignado");
            RegisterError("InstructionSystem no asignado");
            return;
        }

        if (!instructionSystem.CanRepairResistor())
        {
            Debug.Log("❌ Aún no puedes reparar. Primero mide y selecciona la resistencia.");
            RegisterError("Intento de reparar antes de tiempo");
            return;
        }

        if (selectedComponent == null)
        {
            Debug.Log("❌ No hay componente seleccionado");
            RegisterError("Sin componente seleccionado");
            return;
        }

        if (selectedComponent is Resistor r)
        {
            r.resistance = correctResistance;

            if (gameManager != null)
                gameManager.RegisterRepairAction();

            Debug.Log("🔧 Resistencia reemplazada correctamente");
        }
        else
        {
            Debug.Log("❌ El componente seleccionado no es una resistencia");
            RegisterError("Intento de reparar componente incorrecto");
        }
    }

    public void FixParallelCircuit()
    {
        if (instructionSystem == null)
        {
            Debug.Log("❌ InstructionSystem no asignado");
            RegisterError("InstructionSystem no asignado");
            return;
        }

        if (!instructionSystem.CanRepairParallel())
        {
            Debug.Log("❌ Primero debes medir y diagnosticar la rama fallando.");
            RegisterError("Intento de reparar paralelo antes de medir");
            return;
        }

        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                led.resistance = normalLedResistance;
            }
        }

        if (gameManager != null)
            gameManager.RegisterRepairAction();

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

    void RegisterError(string reason)
    {
        if (performance != null)
        {
            performance.AddError(reason);
        }
    }
}