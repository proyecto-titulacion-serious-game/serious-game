using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// HUD mínimo del Explorador — muestra SOLO la lectura del multímetro.
/// Aparece en el casco VR (World Space Canvas pegado a Main Camera).
///
/// El Explorador NO ve manuales ni fórmulas — solo el valor medido
/// y el color de los LEDs del circuito. Debe comunicar lo que ve
/// al Técnico verbalmente para que este pueda diagnosticar.
///
/// JERARQUÍA EN UNITY:
///   XR Origin
///     └─ Camera Offset
///         └─ Main Camera
///             └─ ExplorerHUD  [Canvas World Space]
///                 ├─ Panel_Multimetro    ← este script va aquí
///                 └─ Panel_Instruccion  ← PlayerFeedbackUI va aquí
/// </summary>
public class MultimeterUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public Multimeter multimeter;

    [Header("Textos del HUD (TMP)")]
    public TMP_Text txtVoltaje;        // Valor grande central: "8.18 V"
    public TMP_Text txtProbeRoja;      // "🔴 Nodo_Positivo"
    public TMP_Text txtProbeNegra;     // "⚫ Nodo_Medio"
    public TMP_Text txtEstado;         // "Midiendo..." / "Sin conexión"

    [Header("Indicadores visuales")]
    public Image  iconoProbeRoja;      // Ícono de punta roja (Image UI)
    public Image  iconoProbeNegra;     // Ícono de punta negra
    public Image  fondoVoltaje;        // Fondo que cambia color según la lectura

    [Header("Colores del fondo según estado")]
    public Color colorSinConexion = new Color(0.1f, 0.1f, 0.15f, 0.85f);
    public Color colorMidiendo   = new Color(0.05f, 0.2f, 0.05f, 0.85f);
    public Color colorAlerta     = new Color(0.25f, 0.1f, 0.0f, 0.85f);

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private float _updateTimer;
    private const float INTERVAL = 0.1f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Update()
    {
        _updateTimer += Time.deltaTime;
        if (_updateTimer < INTERVAL) return;
        _updateTimer = 0f;
        Refresh();
    }

    // ─────────────────────────────────────────────
    //  Actualización
    // ─────────────────────────────────────────────
    void Refresh()
    {
        if (multimeter == null) return;

        bool probeAok = multimeter.probeA != null;
        bool probeBok = multimeter.probeB != null;
        bool midiendo = probeAok && probeBok;

        // Textos de sondas
        SetTMP(txtProbeRoja,   probeAok ? multimeter.probeA.name : "—");
        SetTMP(txtProbeNegra,  probeBok ? multimeter.probeB.name : "—");

        if (!midiendo)
        {
            SetTMP(txtVoltaje, "—");
            SetTMP(txtEstado,  "Conecta ambas puntas");
            SetFondo(colorSinConexion);
            return;
        }

        float v = multimeter.measuredVoltage;

        // Voltaje con formato limpio
        SetTMP(txtVoltaje, $"{v:F2} V");

        // Estado y color según la lectura
        if (v > 8.5f)
        {
            SetTMP(txtEstado, "Voltaje alto");
            SetFondo(colorAlerta);
        }
        else if (v > 0.1f)
        {
            SetTMP(txtEstado, "Midiendo");
            SetFondo(colorMidiendo);
        }
        else
        {
            SetTMP(txtEstado, "Sin voltaje");
            SetFondo(colorSinConexion);
        }
    }

    void SetTMP(TMP_Text t, string s) { if (t != null) t.text = s; }
    void SetFondo(Color c)            { if (fondoVoltaje != null) fondoVoltaje.color = c; }
}