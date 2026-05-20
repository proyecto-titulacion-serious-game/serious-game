using UnityEngine;

public enum VoltageSourceFaultMode { None, Overvoltage, Undervoltage }

/// <summary>
/// Fuente de voltaje ideal con soporte de fallas educativas (sobrevoltaje / voltaje insuficiente).
/// Incluye feedback visual para que el Explorador observe el estado de la fuente en VR.
/// </summary>
public class VoltageSource : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Voltaje nominal")]
    [Tooltip("Voltaje correcto de la fuente. Se usa cuando faultMode = None.")]
    public float voltage = 9f;

    [Header("Fallas de fuente")]
    public VoltageSourceFaultMode faultMode = VoltageSourceFaultMode.None;
    [Tooltip("Voltaje cuando faultMode = Overvoltage.")]
    public float overvoltageValue  = 15f;
    [Tooltip("Voltaje cuando faultMode = Undervoltage.")]
    public float undervoltageValue = 3f;

    [Header("Colores educativos")]
    public Color colorNormal      = new Color(0.15f, 0.55f, 1.00f);  // azul — fuente sana
    public Color colorOvervoltage = new Color(1.00f, 0.20f, 0.05f);  // rojo — peligro
    public Color colorUndervoltage= new Color(1.00f, 0.75f, 0.00f);  // amarillo — insuficiente

    // ─────────────────────────────────────────────
    //  Propiedades
    // ─────────────────────────────────────────────
    public bool hasFault => faultMode != VoltageSourceFaultMode.None;

    public float GetEffectiveVoltage() => faultMode switch
    {
        VoltageSourceFaultMode.Overvoltage  => overvoltageValue,
        VoltageSourceFaultMode.Undervoltage => undervoltageValue,
        _                                   => voltage
    };

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private VoltageSourceFaultMode _lastMode = VoltageSourceFaultMode.None;

    private static readonly int _colorID    = Shader.PropertyToID("_BaseColor");
    private static readonly int _emissionID = Shader.PropertyToID("_EmissionColor");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.enabled) { _renderer = r; break; }
        _mpb = new MaterialPropertyBlock();
    }

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance() => isOpenCircuit ? 1_000_000f : 0f;

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        if (isOpenCircuit)
        {
            nodeA.voltage = 0f;
            nodeB.voltage = 0f;
            UpdateVisual();
            return;
        }

        float v = GetEffectiveVoltage();
        nodeA.voltage = v;
        nodeB.voltage = 0f;
        UpdateVisual();
    }

    // ─────────────────────────────────────────────
    //  API de fallas
    // ─────────────────────────────────────────────
    public void ApplyOvervoltageFault()  => faultMode = VoltageSourceFaultMode.Overvoltage;
    public void ApplyUndervoltageFault() => faultMode = VoltageSourceFaultMode.Undervoltage;
    public void RestoreNominalVoltage()  => faultMode = VoltageSourceFaultMode.None;

    // ─────────────────────────────────────────────
    //  Feedback visual
    // ─────────────────────────────────────────────
    void UpdateVisual()
    {
        if (faultMode == _lastMode) return;
        _lastMode = faultMode;

        Color c = faultMode switch
        {
            VoltageSourceFaultMode.Overvoltage  => colorOvervoltage,
            VoltageSourceFaultMode.Undervoltage => colorUndervoltage,
            _                                   => colorNormal
        };

        bool emissive = faultMode != VoltageSourceFaultMode.None;
        ApplyColor(c, emissive);
    }

    void ApplyColor(Color color, bool emissive)
    {
        if (_renderer == null || _mpb == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID,    color);
        _mpb.SetColor(_emissionID, emissive ? color * 2f : Color.black);
        _renderer.SetPropertyBlock(_mpb);
    }
}
