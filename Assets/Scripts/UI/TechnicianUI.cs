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

    [Header("UI Textos")]
    public Text voltageText;
    public Text currentText;
    public Text componentsText;
    public Text diagnosisText;
    public Text performanceText;

    public TMP_Text multimeterText;

    public Text objectiveText;
    public Text resultText;
    public InstructionSystem instructionSystem;
    public Text instructionText;

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
        instructionText.text = instructionSystem.GetCurrentInstruction();
    }

    // 🔌 VOLTAJE DE FUENTE
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

        voltageText.text = "Voltaje Fuente: " + voltage + " V";
    }

    void UpdateInstructions()
    {
        if (instructionSystem == null || instructionText == null) return;

        instructionText.text = instructionSystem.GetCurrentInstruction();
    }


    // ⚡ CORRIENTE
    void UpdateCurrent()
    {
        currentText.text = "Corriente Total: " + circuit.totalCurrent.ToString("F2") + " A";
    }

    // 🔧 COMPONENTES
    void UpdateComponents()
    {
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

    // 🔌 MULTÍMETRO (MEJORADO)
    void UpdateMultimeter()
    {
        multimeterText.text =
            "Voltaje: " + multimeter.measuredVoltage.ToString("F2") + " V\n" +
            "🔴 Punta Roja: " + (multimeter.probeA != null ? multimeter.probeA.name : "None") + "\n" +
            "⚫ Punta Negra: " + (multimeter.probeB != null ? multimeter.probeB.name : "None");
    }

    // 🎯 OBJETIVO
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
                return "RETO 1:\nMedir el voltaje correcto en el circuito serie.";

            case LevelType.Parallel:
                return "RETO 2:\nDiagnosticar circuito en paralelo.";

            case LevelType.Mixed:
                return "RETO 3:\nDetectar errores de polaridad.";

            case LevelType.Arduino:
                return "RETO 4:\nConfigurar sistema Arduino.";

            default:
                return "Sin objetivo";
        }
    }

    // 🧠 DIAGNÓSTICO INTELIGENTE
    void UpdateDiagnosis()
    {
        if (gameManager == null) return;

        diagnosisText.text = analyzer.AnalyzeVoltage(
            multimeter.measuredVoltage,
            gameManager.targetVoltage,
            gameManager.tolerance
        );
    }

    // ✅ RESULTADO
    void UpdateResult()
    {
        if (gameManager == null || resultText == null) return;

        if (gameManager.levelCompleted)
        {
            resultText.text = "✅ Correcto";
        }
        else
        {
            resultText.text = "❌ Aún no completado";
        }
    }

    // ⏱ PERFORMANCE
    void UpdatePerformance()
    {
        if (performance == null) return;

        performanceText.text =
            "Tiempo: " + performance.GetTime().ToString("F1") + " s\n" +
            "Errores: " + performance.errors;
    }
}