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

    [Header("UI Textos")]
    public Text voltageText;
    public Text currentText;
    public Text componentsText;
    public Text diagnosisText;
    public Text performanceText;
    public Text objectiveText;
    public Text resultText;
    public Text instructionText;
    public TechnicianActions technicianActions;
    public Text selectedComponentText;
    public Text stepText;

    public TMP_Text multimeterText;

    private CircuitAnalyzer analyzer = new CircuitAnalyzer();

    void Update()
    {
        if (circuit == null) return;

        UpdateVoltage();
        UpdateCurrent();
        UpdateComponents();
        UpdateMultimeter();
        UpdateObjective();
        UpdateDiagnosis();
        UpdateResult();
        UpdatePerformance();
        UpdateInstructions();
        UpdateSelectedComponent();
        UpdateStepInfo();
    }

    void UpdateVoltage()
    {
        float voltage = 0f;

        foreach (var comp in circuit.components)
        {
            if (comp is VoltageSource source)
            {
                voltage = source.voltage;
                break;
            }
        }

        if (voltageText != null)
            voltageText.text = "Voltaje Fuente: " + voltage + " V";
    }

    void UpdateStepInfo()
    {
        if (instructionSystem == null || stepText == null) return;

        stepText.text = "Paso actual: " + (instructionSystem.currentStep + 1);
    }

    void UpdateCurrent()
    {
        if (currentText != null)
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
                sb.AppendLine("- Resistor: " + res.resistance + " Ω");
            }
            else if (comp is LED led)
            {
                string estado = led.current > 0 ? "Encendido" : "Apagado";
                sb.AppendLine("- LED: " + estado);
            }
        }

        componentsText.text = sb.ToString();
    }

    void UpdateSelectedComponent()
    {
        if (technicianActions == null || selectedComponentText == null) return;

        selectedComponentText.text = "Seleccionado: " + technicianActions.GetSelectedComponentName();
    }

    void UpdateMultimeter()
    {
        if (multimeter == null || multimeterText == null) return;

        multimeterText.text =
            "Voltaje: " + multimeter.measuredVoltage.ToString("F2") + " V\n" +
            "🔴 Punta Roja: " + (multimeter.probeA != null ? multimeter.probeA.name : "None") + "\n" +
            "⚫ Punta Negra: " + (multimeter.probeB != null ? multimeter.probeB.name : "None");
    }

    void UpdateObjective()
    {
        if (gameManager == null || objectiveText == null) return;
        objectiveText.text = GetObjectiveText();
    }

    string GetObjectiveText()
    {
        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                return "RETO 1:\nMide el voltaje correcto en el circuito serie y corrige la resistencia.";

            case LevelType.Parallel:
                return "RETO 2:\nDiagnostica la rama paralela fallando y repárala.";

            case LevelType.Mixed:
                return "RETO 3:\nDetecta errores de polaridad y valores.";

            case LevelType.Arduino:
                return "RETO 4:\nConfigura correctamente el sistema Arduino.";

            default:
                return "Sin objetivo";
        }
    }

    void UpdateDiagnosis()
    {
        if (gameManager == null || diagnosisText == null || multimeter == null) return;

        diagnosisText.text = analyzer.AnalyzeByLevel(
            gameManager.currentLevel,
            multimeter.measuredVoltage,
            gameManager.targetVoltage,
            gameManager.tolerance,
            circuit
        );
    }

    void UpdateResult()
    {
        if (gameManager == null || resultText == null) return;

        if (gameManager.levelCompleted)
            resultText.text = "✅ Correcto";
        else
            resultText.text = "❌ Aún no completado";
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
}