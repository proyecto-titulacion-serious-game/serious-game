using UnityEngine;

/// <summary>
/// Capacitor electrolítico para el Reto 3.
/// Simula fallo por polaridad invertida (humo + vibración háptica).
/// En CC actúa como circuito abierto (resistencia muy alta) excepto en cortocircuito.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Capacitor : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Configuración eléctrica")]
    [Tooltip("Capacitancia en Faradios (valor educativo, no afecta simulación DC).")]
    public float capacitance = 0.0001f;    // 100µF típico

    [Header("Falla de polaridad")]
    public bool polarityInverted = false;
    [Tooltip("Resistencia simulada en cortocircuito (polaridad invertida).")]
    public float shortCircuitResistance = 0.1f;
    [Tooltip("Resistencia en operación normal DC (casi circuito abierto).")]
    public float normalDCResistance = 1_000_000f;

    [Header("Efectos visuales de falla")]
    public ParticleSystem smokeEffect;     // Arrastrar desde inspector
    public float smokeCurrentThreshold = 5f;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    [Header("Estado (solo lectura)")]
    public CapacitorState state = CapacitorState.Normal;

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance()
    {
        // Con polaridad invertida actúa como cortocircuito
        return polarityInverted ? shortCircuitResistance : normalDCResistance;
    }

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        float voltageDiff = nodeA.voltage - nodeB.voltage;

        // En polaridad correcta, en DC casi no pasa corriente
        current     = voltageDiff / GetResistance();
        voltageDrop = voltageDiff;

        // Clasificar estado
        if (polarityInverted && Mathf.Abs(current) > smokeCurrentThreshold)
        {
            SetState(CapacitorState.ShortCircuit);
        }
        else if (polarityInverted)
        {
            SetState(CapacitorState.Reversed);
        }
        else
        {
            SetState(CapacitorState.Normal);
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    public void SetPolarityInverted(bool inverted)
    {
        polarityInverted = inverted;
    }

    void SetState(CapacitorState newState)
    {
        if (state == newState) return;
        state = newState;

        // Activar/desactivar humo
        if (smokeEffect != null)
        {
            if (newState == CapacitorState.ShortCircuit)
            {
                if (!smokeEffect.isPlaying) smokeEffect.Play();
            }
            else
            {
                smokeEffect.Stop();
            }
        }
    }
}

public enum CapacitorState
{
    Normal,
    Reversed,
    ShortCircuit
}