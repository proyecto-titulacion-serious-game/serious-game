using UnityEngine;

public class InstructionSystem : MonoBehaviour
{
    [Header("Estado")]
    public int currentStep = 0;

    [Header("Referencias")]
    public Multimeter multimeter;
    public GameManager gameManager;
    public TechnicianActions technicianActions;

    [Header("Flags de progreso")]
    public bool hasMeasuredCorrectly = false;
    public bool hasSelectedCorrectComponent = false;
    public bool hasAppliedFix = false;

    [Header("Instrucciones")]
    public string[] instructions;

    void Start()
    {
        ResetInstructions();
        BuildInstructions();
    }

    void Update()
    {
        ValidateCurrentStep();
    }

    void BuildInstructions()
    {
        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                instructions = new string[]
                {
                    "Paso 1: Conecta la punta roja y negra del multímetro a dos nodos.",
                    "Paso 2: Observa la medición y verifica si el voltaje es el esperado.",
                    "Paso 3: Selecciona la resistencia defectuosa.",
                    "Paso 4: Reemplaza la resistencia por el valor correcto."
                };
                break;

            case LevelType.Parallel:
                instructions = new string[]
                {
                    "Paso 1: Mide el circuito para identificar una rama fallando.",
                    "Paso 2: Analiza qué componente está provocando la falla.",
                    "Paso 3: Aplica la reparación del circuito paralelo."
                };
                break;

            default:
                instructions = new string[]
                {
                    "Nivel en desarrollo."
                };
                break;
        }
    }

    void ValidateCurrentStep()
    {
        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                ValidateOhmLaw();
                break;

            case LevelType.Parallel:
                ValidateParallel();
                break;
        }
    }

    void ValidateOhmLaw()
    {
        switch (currentStep)
        {
            case 0:
                if (multimeter != null && multimeter.probeA != null && multimeter.probeB != null)
                {
                    NextStep();
                }
                break;

            case 1:
                if (multimeter != null && multimeter.measuredVoltage > 0f)
                {
                    hasMeasuredCorrectly = true;
                    NextStep();
                }
                break;

            case 2:
                if (technicianActions != null && technicianActions.HasSelectedResistor())
                {
                    hasSelectedCorrectComponent = true;
                    NextStep();
                }
                break;

            case 3:
                if (gameManager != null && gameManager.levelCompleted)
                {
                    hasAppliedFix = true;
                    NextStep();
                }
                break;
        }
    }

    void ValidateParallel()
    {
        switch (currentStep)
        {
            case 0:
                if (multimeter != null && multimeter.probeA != null && multimeter.probeB != null)
                {
                    NextStep();
                }
                break;

            case 1:
                if (multimeter != null && multimeter.measuredVoltage > 0f)
                {
                    NextStep();
                }
                break;

            case 2:
                if (gameManager != null && gameManager.levelCompleted)
                {
                    NextStep();
                }
                break;
        }
    }

    public string GetCurrentInstruction()
    {
        if (instructions == null || instructions.Length == 0)
            return "Sin instrucciones.";

        if (currentStep < instructions.Length)
            return instructions[currentStep];

        return "✔ Procedimiento completado.";
    }

    public void NextStep()
    {
        currentStep++;
        Debug.Log("➡ Paso actual: " + currentStep);
    }

    public void ResetInstructions()
    {
        currentStep = 0;
        hasMeasuredCorrectly = false;
        hasSelectedCorrectComponent = false;
        hasAppliedFix = false;
    }

    public bool CanRepairResistor()
    {
        return currentStep >= 3 && hasMeasuredCorrectly && hasSelectedCorrectComponent;
    }

    public bool CanRepairParallel()
    {
        return currentStep >= 2;
    }
}