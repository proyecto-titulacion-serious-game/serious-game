using UnityEngine;

/// <summary>
/// LED con simulación educativa: muestra verde (correcto), rojo (sobrecarga),
/// negro (sin corriente / polaridad invertida).
/// Usa MaterialPropertyBlock para evitar crear materiales duplicados en cada frame.
/// </summary>
public class LED : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Configuración eléctrica")]
    public float resistance = 50f;

    [Tooltip("Caída de voltaje directa del diodo (Vf). Un LED rojo típico ≈ 1.8–2.2 V. " +
             "El MNA la modela para que la corriente sea (V − Vf) / R, no V / R.")]
    public float forwardVoltage = 2f;

    [Tooltip("Si true, el LED tiene la polaridad invertida (falla del Reto 3).")]
    public bool polarityInverted = false;

    // ─────────────────────────────────────────────
    //  Topología del diodo (ánodo → cátodo)
    // ─────────────────────────────────────────────
    /// <summary>Nodo ánodo (patita larga). Por convención <c>nodeA</c>, salvo polaridad invertida.</summary>
    public ElectricalNode AnodeNode   => polarityInverted ? nodeB : nodeA;
    /// <summary>Nodo cátodo (patita corta).</summary>
    public ElectricalNode CathodeNode => polarityInverted ? nodeA : nodeB;

    [Header("Umbrales de corriente (Amperes)")]
    public float minOperatingCurrent = 0.005f;   // Corriente mínima para encender
    public float maxSafeCurrent      = 0.02f;    // Corriente máxima segura
    public float overloadCurrent     = 0.1f;     // Corriente de sobrecarga

    [Header("Colores educativos")]
    public Color colorOff      = Color.black;
    public Color colorCorrect  = Color.green;
    public Color colorOverload = Color.red;

    [Header("Brillo de victoria (cuando el LED está correcto)")]
    [Tooltip("Pulsa el brillo del LED mientras está correcto, para resaltar que todo quedó bien.")]
    public bool  victoryGlow = true;
    [Tooltip("Velocidad del pulso de brillo.")]
    public float glowSpeed   = 4f;
    [Tooltip("Cuánto sube/baja el brillo en el pulso.")]
    public float glowAmount  = 0.8f;

    [Header("Render de victoria (al COMPLETAR el reto)")]
    [Tooltip("Material que se aplica al LED cuando el reto se completa (asigna el verde 'led_Green' / " +
             "Mat_LED_Verde). Si queda vacío, el LED simplemente conserva su verde pulsante.")]
    public Material victoryMaterial;

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
    private static readonly int   _colorID    = Shader.PropertyToID("_BaseColor");
    private static readonly int   _emissionID = Shader.PropertyToID("_EmissionColor");

    // Si el LED es agarrable (token del sandbox/entrega), congelamos su visual mientras se
    // sostiene para que no parpadee de color por la simulación a 20 Hz al moverlo entre nodos.
    private GrabbableComponent _grab;
    private bool IsHeld => _grab != null && _grab.IsGrabbed;

    // Render de victoria: al completar el reto, el LED cambia al material verde y se congela.
    private bool     _victoryRender;
    private Material _origSharedMaterial;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        // Busca el primer Renderer activo (puede estar en el hijo Visual del FBX)
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.enabled) { _renderer = r; break; }
        _mpb = new MaterialPropertyBlock();
        if (_renderer != null) _origSharedMaterial = _renderer.sharedMaterial;

        _grab = GetComponentInParent<GrabbableComponent>();
    }

    void OnEnable()
    {
        GameManager.OnLevelCompleted += OnLevelCompletedHandler;
        GameManager.OnLevelLoaded    += OnLevelLoadedHandler;
    }

    void OnDisable()
    {
        GameManager.OnLevelCompleted -= OnLevelCompletedHandler;
        GameManager.OnLevelLoaded    -= OnLevelLoadedHandler;
    }

    // Al completar el reto con éxito, si este LED está encendido, cambia su render al verde de victoria.
    void OnLevelCompletedHandler(LevelType _, bool success)
    {
        if (success && isOn && victoryMaterial != null) ApplyVictoryRender();
    }

    // Al cargar un nuevo reto, restaura el material original.
    void OnLevelLoadedHandler(LevelType _) => RestoreRender();

    /// <summary>Cambia el LED al material verde de victoria y congela su visual.</summary>
    public void ApplyVictoryRender()
    {
        _victoryRender = true;
        if (_renderer != null && victoryMaterial != null)
            _renderer.material = victoryMaterial;
    }

    void RestoreRender()
    {
        if (!_victoryRender) return;
        _victoryRender = false;
        if (_renderer != null && _origSharedMaterial != null)
            _renderer.sharedMaterial = _origSharedMaterial;
    }

    // Brillo pulsante cuando el LED está CORRECTO (todo el circuito bien). Refuerzo visual de éxito.
    void Update()
    {
        if (_victoryRender) return;   // render de victoria fijo: no pulsar
        if (!victoryGlow || state != LEDState.Correct || IsHeld) return;
        if (_renderer == null || _mpb == null) return;

        float pulse = 1.5f + Mathf.Sin(Time.time * glowSpeed) * glowAmount;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_emissionID, colorCorrect * Mathf.Max(0.25f, pulse));
        _renderer.SetPropertyBlock(_mpb);
    }

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance() => isOpenCircuit ? 1_000_000f : resistance;

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        // Mientras se sostiene el LED, no parpadear: estado estable apagado.
        if (IsHeld)
        {
            current = 0f; voltageDrop = 0f; isOn = false;
            SetState(LEDState.Off);
            return;
        }

        if (resistance <= 0f)
        {
            current     = 0f;
            voltageDrop = 0f;
            isOn        = false;
            SetState(LEDState.Off);
            return;
        }

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

    /// <summary>
    /// Actualiza el estado visual (color/encendido) a partir de la corriente YA resuelta
    /// en <see cref="ElectricalComponent.current"/>, sin recalcularla desde los voltajes.
    ///
    /// Lo usa el sandbox del Reto 4 (<see cref="ProtoboardSimulator"/>) tras el MNA
    /// diodo-consciente: ahí los voltajes de nodo incluyen la caída directa Vf, así que
    /// recalcular con <see cref="Calculate"/> (modelo resistivo puro) daría una corriente
    /// inflada. El MNA ya entrega la corriente correcta (0 A en inversa) → solo clasificamos.
    /// </summary>
    public void ApplyResolvedCurrent()
    {
        // Mientras se sostiene el LED, no parpadear: estado estable apagado.
        if (IsHeld) { isOn = false; SetState(LEDState.Off); return; }

        float i = Mathf.Abs(current);

        if (i < minOperatingCurrent)      { isOn = false; SetState(LEDState.Off); }
        else if (i <= maxSafeCurrent)     { isOn = true;  SetState(LEDState.Correct); }
        else if (i <  overloadCurrent)    { isOn = true;  SetState(LEDState.NearOverload); }
        else                              { isOn = true;  SetState(LEDState.Overload); }
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

        if (_victoryRender) return;      // render de victoria fijo: no aplicar colores

        Color displayColor = newState switch
        {
            LEDState.Correct      => colorCorrect,
            LEDState.NearOverload => Color.yellow,
            LEDState.Overload     => colorOverload,
            _                     => colorOff
        };

        ApplyColor(displayColor, newState != LEDState.Off);
    }

    void ApplyColor(Color color, bool emissive)
    {
        if (_renderer == null || _mpb == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID,    color);
        _mpb.SetColor(_emissionID, emissive ? color * 1.5f : Color.black);
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
