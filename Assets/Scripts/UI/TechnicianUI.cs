using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class TechnicianUI : MonoBehaviour

{

    [Header("Referencias")]
    public CircuitManager circuit;

    [Header("UI Textos")]
    public Text voltageText;
    public Text currentText;
    public Text componentsText;
    public Text diagnosisText;
    public Text detailedAnalysisText;
    public Text performanceText;
    public TMP_Text multimeterText;
    public Multimeter multimeter;
    
    private CircuitAnalyzer analyzer = new CircuitAnalyzer();
    private PerformanceTracker performance;


    void Start()
    {
        performance = FindObjectOfType<PerformanceTracker>();
    }

    void Update()
    {
        if (circuit == null) return;

        UpdateVoltage();
        UpdateCurrent();
        UpdateComponents();
        UpdateDiagnosis();
        UpdateDetailedAnalysis();
        UpdatePerformance();
        
        multimeterText.text = "Voltaje medido: " + multimeter.measuredVoltage.ToString("F2") + " V";

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

        voltageText.text = "Voltaje Fuente: " + voltage + " V";
    }

    void UpdateCurrent()
    {
        currentText.text = "Corriente Total: " + circuit.totalCurrent.ToString("F2") + " A";
    }

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
                string estado = led.current > 0 ? "Activo" : "Apagado";
                sb.AppendLine("- LED: " + estado);
            }
        }

        componentsText.text = sb.ToString();
    }

    void UpdateDiagnosis()
    {
        diagnosisText.text = "Diagnóstico:\n" + analyzer.Diagnose(circuit.components);
    }

    void UpdateDetailedAnalysis()
    {
        detailedAnalysisText.text = analyzer.GetDetailedAnalysis(
            circuit.components,
            circuit.totalCurrent
        );
    }

    void UpdatePerformance()
    {
        if (performance == null) return;

        performanceText.text =
            "Tiempo: " + performance.GetTime().ToString("F1") + " s\n" +
            "Errores: " + performance.errors;
    }
}