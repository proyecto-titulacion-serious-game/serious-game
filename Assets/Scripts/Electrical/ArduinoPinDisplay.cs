using UnityEngine;
using TMPro;

/// <summary>
/// Panel físico que muestra el estado del ArduinoPin en la escena del Explorador.
/// Cambia color y texto según el pin activo (correcto / incorrecto) y el cable suelto.
///
/// SETUP: añadir a cualquier GO con Renderer y TMP_Text hijo.
/// Asignar linkedPin → el ArduinoPin del circuito.
/// </summary>
public class ArduinoPinDisplay : MonoBehaviour
{
    [Header("Referencias")]
    public ArduinoPin linkedPin;

    [Tooltip("Texto TMP donde mostrar el estado del pin.")]
    public TMP_Text statusText;

    [Tooltip("Renderer del fondo del panel para cambiar color.")]
    public Renderer panelRenderer;

    [Header("Colores")]
    public Color colorCorrect = new Color(0.1f, 0.6f, 0.2f);   // verde
    public Color colorFault   = new Color(0.7f, 0.1f, 0.1f);   // rojo
    public Color colorLoose   = new Color(0.8f, 0.5f, 0.0f);   // naranja

    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (linkedPin == null) linkedPin = GetComponentInParent<ArduinoPin>();
        if (statusText == null) statusText = GetComponentInChildren<TMP_Text>();
    }

    void OnEnable()  => CircuitManager.OnCircuitChanged += UpdateDisplay;
    void OnDisable() => CircuitManager.OnCircuitChanged -= UpdateDisplay;

    void Start() => UpdateDisplay();

    void UpdateDisplay()
    {
        if (linkedPin == null) return;

        Color  c;
        string msg;

        if (linkedPin.hasLooseCable)
        {
            msg = $"PIN D{linkedPin.pinNumber}\nCABLE SUELTO";
            c   = colorLoose;
        }
        else if (linkedPin.hasFault)
        {
            msg = $"PIN D{linkedPin.pinNumber}\n[INCORRECTO]\nCorrecto: D{linkedPin.correctPinNumber}";
            c   = colorFault;
        }
        else
        {
            msg = $"PIN D{linkedPin.pinNumber}\n[CORRECTO]";
            c   = colorCorrect;
        }

        if (statusText != null) statusText.text = msg;

        if (panelRenderer != null)
        {
            panelRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorID, c);
            panelRenderer.SetPropertyBlock(_mpb);
        }
    }
}
