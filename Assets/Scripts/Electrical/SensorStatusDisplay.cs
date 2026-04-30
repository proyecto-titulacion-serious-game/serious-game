using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el estado del sensor en el panel físico de la nave.
/// Se suscribe a OnCircuitChanged y actualiza el texto automáticamente.
///
/// SETUP en Unity — agregar a Panel_SensorA, Panel_SensorB, Panel_SensorC:
///   1. Seleccionar Panel_SensorA → Add Component → SensorStatusDisplay
///   2. Arrastrar el LED correspondiente al campo "linkedLED"
///      - Panel_SensorA → LED_A
///      - Panel_SensorB → LED_B
///      - Panel_SensorC → LED_C
///   3. Arrastrar el Text (TMP) del panel al campo "statusText"
///   4. Repetir para Panel_SensorB y Panel_SensorC
/// </summary>
public class SensorStatusDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El LED del circuito que corresponde a este panel.")]
    public LED linkedLED;

    [Tooltip("Texto TMP donde mostrar ACTIVO / SIN SEÑAL.")]
    public TMP_Text statusText;

    [Tooltip("Renderer del fondo del panel para cambiar color.")]
    public Renderer panelRenderer;

    [Header("Colores del panel")]
    public Color colorActive  = new Color(0.1f, 0.6f, 0.2f);   // verde
    public Color colorFaulty  = new Color(0.7f, 0.1f, 0.1f);   // rojo

    [Header("Textos")]
    public string textActive = "ACTIVO";
    public string textFaulty = "SIN SEÑAL";

    // ─────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged += UpdateDisplay;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= UpdateDisplay;
    }

    void Start()
    {
        UpdateDisplay();
    }

    // ─────────────────────────────────────────────
    //  Actualización
    // ─────────────────────────────────────────────

    void UpdateDisplay()
    {
        if (linkedLED == null) return;

        bool active = linkedLED.isOn;

        // Texto
        if (statusText != null)
            statusText.text = active ? textActive : textFaulty;

        // Color del fondo del panel
        if (panelRenderer != null)
        {
            panelRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorID, active ? colorActive : colorFaulty);
            panelRenderer.SetPropertyBlock(_mpb);
        }
    }
}