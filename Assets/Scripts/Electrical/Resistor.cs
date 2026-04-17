using UnityEngine;

/// <summary>
/// Resistencia con soporte de código de colores (Reto 3).
/// Permite establecer el valor "correcto" vs el valor "defectuoso" para gamificación.
/// </summary>
public class Resistor : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Valor de resistencia")]
    public float resistance = 100f;

    [Header("Código de colores (educativo)")]
    [Tooltip("Valor correcto que el Técnico debe calcular e indicar.")]
    public float correctResistance = 100f;
    [Tooltip("Valor defectuoso que aparece al inicio del reto.")]
    public float faultyResistance  = 10f;
    [Tooltip("True si la resistencia tiene el valor incorrecto (falla activa).")]
    public bool  hasFault          = false;

    [Header("Tolerancia (%)")]
    [Range(1f, 20f)]
    public float tolerancePercent  = 5f;

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance() => resistance;

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;
        if (resistance <= 0f)
        {
            current = 0f;
            voltageDrop = 0f;
            return;
        }

        float voltageDiff = nodeA.voltage - nodeB.voltage;
        current     = (resistance > 0f) ? voltageDiff / resistance : 0f;
        voltageDrop = voltageDiff;
    }

    // ─────────────────────────────────────────────
    //  API de juego
    // ─────────────────────────────────────────────

    /// <summary>Aplica la falla: pone el valor defectuoso.</summary>
    public void ApplyFault()
    {
        resistance = faultyResistance;
        hasFault   = true;
    }

    /// <summary>Repara la resistencia con el valor correcto.</summary>
    public void Repair()
    {
        resistance = correctResistance;
        hasFault   = false;
    }

    /// <summary>
    /// Verifica si el valor propuesto por el Técnico es correcto (dentro de tolerancia).
    /// </summary>
    public bool IsValueCorrect(float proposedValue)
    {
        float margin = correctResistance * (tolerancePercent / 100f);
        return Mathf.Abs(proposedValue - correctResistance) <= margin;
    }

    /// <summary>
    /// Devuelve las bandas de colores como string educativo.
    /// Ejemplo: "Marrón-Negro-Marrón-Oro" para 100Ω 5%
    /// </summary>
    public string GetColorBandString()
    {
        return ResistorColorCode.GetBandString((int)correctResistance, tolerancePercent);
    }
}

/// <summary>
/// Utilidad para calcular el código de colores de una resistencia.
/// </summary>
public static class ResistorColorCode
{
    private static readonly string[] bands =
        { "Negro", "Marrón", "Rojo", "Naranja", "Amarillo",
          "Verde",  "Azul",   "Violeta", "Gris",  "Blanco" };

    public static string GetBandString(int value, float tolerance)
    {
        if (value <= 0) return "Valor inválido";

        int digits    = value;
        int multiplier = 0;

        while (digits >= 100) { digits /= 10; multiplier++; }

        int band1 = digits / 10;
        int band2 = digits % 10;

        string tolBand = tolerance <= 1f  ? "Marrón" :
                         tolerance <= 2f  ? "Rojo"   :
                         tolerance <= 5f  ? "Oro"    :
                         tolerance <= 10f ? "Plata"  : "Sin banda";

        if (band1 >= bands.Length || band2 >= bands.Length || multiplier >= bands.Length)
            return "Fuera de rango";

        return $"{bands[band1]} – {bands[band2]} – {bands[multiplier]} – {tolBand}";
    }
}
