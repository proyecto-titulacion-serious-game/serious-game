using UnityEngine;
using TMPro;

/// <summary>
/// Panel de monitoreo en tiempo real para el Técnico (PC).
/// Extrae datos de la red y del motor matemático para visualizar la física del circuito.
/// </summary>
public class TechnicianTelemetryUI : MonoBehaviour
{
    [Header("Monitores de Pantalla (TextMeshPro)")]
    public TextMeshProUGUI txtVoltage;
    public TextMeshProUGUI txtCurrent;
    public TextMeshProUGUI txtPower;
    public TextMeshProUGUI txtSensorA0;
    public TextMeshProUGUI txtSystemStatus;

    // Aliases de compatibilidad con scripts de editor (aceptan TMP_Text base)
    public TMP_Text lblVoltaje   { get => txtVoltage;      set => txtVoltage      = value as TextMeshProUGUI ?? txtVoltage; }
    public TMP_Text lblCorriente { get => txtCurrent;      set => txtCurrent      = value as TextMeshProUGUI ?? txtCurrent; }
    public TMP_Text lblPotencia  { get => txtPower;        set => txtPower        = value as TextMeshProUGUI ?? txtPower; }
    public TMP_Text lblAdc       { get => txtSensorA0;     set => txtSensorA0     = value as TextMeshProUGUI ?? txtSensorA0; }
    public TMP_Text lblAlerta    { get => txtSystemStatus; set => txtSystemStatus = value as TextMeshProUGUI ?? txtSystemStatus; }

    [Header("Telemetría detallada (HUD Builder)")]
    public TMP_Text txtValorVoltaje;
    public TMP_Text txtValorCorriente;
    public TMP_Text txtValorPotencia;
    public TMP_Text txtValorAdc;
    public Image    imgCardVoltaje;
    public Image    imgCardCorriente;
    public Image    bannerStripe;

    [Header("Referencias (auto-detectadas si se dejan vacías)")]
    public CircuitSimulator circuit;
    public ArduinoNetworkBridge arduinoBridge;

    // Renderer del panel de alerta (opcional, solo visual)
    public Image panelAlerta;

    void Update()
    {
        ActualizarTelemetriaGeneral();
        ActualizarTelemetriaArduino();
    }

    void ActualizarTelemetriaGeneral()
    {
        if (circuit == null)
        {
            circuit = FindAnyObjectByType<CircuitSimulator>();
            if (circuit == null) return;
        }

        if (txtVoltage != null)
            txtVoltage.text = circuit.sourceVoltage.ToString("F2") + " V";

        if (txtCurrent != null)
            txtCurrent.text = (circuit.totalCurrent * 1000f).ToString("F1") + " mA";

        if (txtPower != null)
            txtPower.text = circuit.totalPower.ToString("F3") + " W";

        if (txtSystemStatus != null)
        {
            if (circuit.isShortCircuited)
            {
                txtSystemStatus.color = Color.red;
                txtSystemStatus.text  = "¡ALERTA: CORTOCIRCUITO DETECTADO!";
            }
            else if (circuit.totalCurrent == 0)
            {
                txtSystemStatus.color = new Color(1f, 0.5f, 0f);
                txtSystemStatus.text  = "ESTADO: CIRCUITO ABIERTO (0 mA)";
            }
            else
            {
                txtSystemStatus.color = Color.green;
                txtSystemStatus.text  = "ESTADO: OPERACIÓN SEGURA";
            }
        }
    }

    void ActualizarTelemetriaArduino()
    {
        if (arduinoBridge == null)
        {
            arduinoBridge = FindAnyObjectByType<ArduinoNetworkBridge>();
            if (arduinoBridge == null)
            {
                if (txtSensorA0 != null) txtSensorA0.text = "NO CONECTADO";
                return;
            }
        }

        if (txtSensorA0 != null)
            txtSensorA0.text = arduinoBridge.NetworkedAnalogValue.ToString();
    }
}
