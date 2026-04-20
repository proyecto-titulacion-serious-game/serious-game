using UnityEngine;

/// <summary>
/// LED con simulación educativa: muestra verde (correcto), rojo (sobrecarga),
/// negro (sin corriente / polaridad invertida).
/// Usa MaterialPropertyBlock para evitar crear materiales duplicados en cada frame.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class LED : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Configuración eléctrica")]
    public float resistance = 50f;

    [Tooltip("Si true, el LED tiene la polaridad invertida (falla del Reto 3).")]
    public bool polarityInverted = false;

    [Header("Umbrales de corriente (Amperes)")]
    public float minOperatingCurrent = 0.005f;   // Corriente mínima para encender
    public float maxSafeCurrent      = 0.02f;    // Corriente máxima segura
    public float overloadCurrent     = 0.1f;     // Corriente de sobrecarga

    [Header("Colores educativos")]
    public Color colorOff      = Color.black;
    public Color colorCorrect  = Color.green;
    public Color colorOverload = Color.red;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    [Header("Estado (solo lectura)")]
    public bool isOn = false;
    public LEDState state = LEDState.Off;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private Renderer       _renderer;
    private MaterialPropertyBlock _mpb;      // ← Evita memory leak
    private static readonly int   _colorID = Shader.PropertyToID("_Color");
    private static readonly int   _emissionID = Shader.PropertyToID("_EmissionColor");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
    }

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance() => resistance;

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        float voltageDiff = nodeA.voltage - nodeB.voltage;

        // Polaridad: si está invertida el voltaje efectivo es negativo
        if (polarityInverted) voltageDiff = -voltageDiff;

        // Sin polaridad directa → LED apagado
        if (voltageDiff <= 0f)
        {
            current     = 0f;
            voltageDrop = 0f;
            isOn        = false;
            SetState(LEDState.Off);
            return;
        }

        // Ley de Ohm
        current     = voltageDiff / resistance;
        voltageDrop = voltageDiff;

        // Clasificar estado educativo
        if (current < minOperatingCurrent)
        {
            isOn = false;
            SetState(LEDState.Off);
        }
        else if (current <= maxSafeCurrent)
        {
            isOn = true;
            SetState(LEDState.Correct);
        }
        else if (current < overloadCurrent)
        {
            isOn = true;
            SetState(LEDState.NearOverload);
        }
        else
        {
            isOn = true;
            SetState(LEDState.Overload);
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    /// <summary>Invierte la polaridad del LED (útil para GameManager en Reto 3).</summary>
    public void SetPolarityInverted(bool inverted)
    {
        polarityInverted = inverted;
    }

    void SetState(LEDState newState)
    {
        if (state == newState) return;   // Sin cambio → no redibujar
        state = newState;

        Color displayColor = newState switch
        {
            LEDState.Correct      => colorCorrect,
            LEDState.NearOverload => Color.yellow,
            LEDState.Overload     => colorOverload,
            _                     => colorOff
        };

        ApplyColor(displayColor);
    }

    /// <summary>
    /// Aplica color con MaterialPropertyBlock — NO crea materiales nuevos.
    /// </summary>
    void ApplyColor(Color color)
    {
        if (_renderer == null || _mpb == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID,    color);
        _mpb.SetColor(_emissionID, color * (color == colorOff ? 0f : 1.5f));
        _renderer.SetPropertyBlock(_mpb);
    }
}

public enum LEDState
{
    Off,
    Correct,
    NearOverload,
    Overload
}