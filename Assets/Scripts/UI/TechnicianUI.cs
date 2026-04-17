using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class TechnicianUI : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;
    public Multimeter multimeter;
    public GameManager gameManager;
    public PerformanceTracker performance;
    public InstructionSystem instructionSystem;
    public TechnicianActions technicianActions;

    [Header("UI Textos")]
    public Text voltageText;
    public Text currentText;
    public Text componentsText;
    public Text diagnosisText;
    public Text detailedAnalysisText;
    public Text performanceText;
    public Text objectiveText;
    public Text resultText;
    public Text instructionText;
    public Text selectedComponentText;
    public Text stepText;
    public Text feedbackText;

    public TMP_Text multimeterText;

    private DiagnosticSystem diagnostic = new DiagnosticSystem();

    void Update()
    {
        if (circuit == null) return;

        UpdateVoltage();
        UpdateCurrent();
        UpdateComponents();
        UpdateMultimeter();
        UpdateObjective();
        UpdateDiagnosis();
        UpdateDetailedAnalysis();
        UpdateResult();
        UpdatePerformance();
        UpdateInstructions();
        UpdateSelectedComponent();
        UpdateStepInfo();
    }

    void UpdateVoltage()
    {
        if (voltageText == null) return;

        float voltage = 0f;

        foreach (var comp in circuit.components)
        {
            if (comp is VoltageSource source)
            {
                voltage = source.voltage;
                break;
            }
        }

        voltageText.text = "Voltaje Fuente: " + voltage + " V";
    }

    void UpdateCurrent()
    {
        if (currentText == null) return;

        currentText.text = "Corriente Total: " + circuit.totalCurrent.ToString("F2") + " A";
    }

    void UpdateComponents()
    {
        if (componentsText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Componentes:");

        foreach (var comp in circuit.components)
        {
            if (comp is Resistor res)
            {
                sb.AppendLine("- Resistor: " + res.resistance + " ohmios");
            }
            else if (comp is LED led)
            {
                string estado = led.current > 0 ? "Encendido" : "Apagado";
                sb.AppendLine("- LED: " + estado);
            }
        }

        componentsText.text = sb.ToString();
    }

    void UpdateMultimeter()
    {
        if (multimeter == null || multimeterText == null) return;

        multimeterText.text =
            "Voltaje: " + multimeter.measuredVoltage.ToString("F2") + " V\n" +
            "Punta Roja: " + (multimeter.probeA != null ? multimeter.probeA.name : "None") + "\n" +
            "Punta Negra: " + (multimeter.probeB != null ? multimeter.probeB.name : "None");
    }

    void UpdateObjective()
    {
        if (gameManager == null || objectiveText == null) return;

        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                objectiveText.text = "RETO 1:\nMide el voltaje correcto del circuito serie y corrige la resistencia.";
                break;

            case LevelType.Parallel:
                objectiveText.text = "RETO 2:\nDiagnostica la rama paralela con falla y reparala.";
                break;

            case LevelType.Mixed:
                objectiveText.text = "RETO 3:\nDetecta errores de polaridad y valor.";
                break;

            case LevelType.Arduino:
                objectiveText.text = "RETO 4:\nConfigura correctamente el sistema Arduino.";
                break;

            default:
                objectiveText.text = "Sin objetivo";
                break;
        }
    }

    void UpdateDiagnosis()
    {
        if (diagnosisText == null) return;

        diagnosisText.text = "Diagnostico:\n" +
            diagnostic.GetDiagnosis(circuit.components, circuit.totalCurrent);
    }

    void UpdateDetailedAnalysis()
    {
        if (detailedAnalysisText == null) return;

        detailedAnalysisText.text = diagnostic.GetDetailedAnalysis(
            circuit.components,
            circuit.totalCurrent
        );
    }

    void UpdateResult()
    {
        if (resultText == null || gameManager == null || performance == null) return;

        if (gameManager.levelCompleted)
        {
            resultText.text = "Correcto\n" + performance.GetEvaluation();
        }
        else
        {
            resultText.text = "Aun no completado";
        }
    }

    void UpdatePerformance()
    {
        if (performance == null || performanceText == null) return;

        performanceText.text =
            "Tiempo: " + performance.GetTime().ToString("F1") + " s\n" +
            "Errores: " + performance.GetErrors();
    }

    void UpdateInstructions()
    {
        if (instructionSystem == null || instructionText == null) return;

        instructionText.text = instructionSystem.GetCurrentInstruction();
    }

    void UpdateSelectedComponent()
    {
        if (technicianActions == null || selectedComponentText == null) return;

        selectedComponentText.text = "Seleccionado: " + technicianActions.GetSelectedComponentName();
    }

    void UpdateStepInfo()
    {
        if (instructionSystem == null || stepText == null) return;

        stepText.text = "Paso actual: " + (instructionSystem.currentStep + 1);
    }
}