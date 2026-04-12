using System.Collections.Generic;
using UnityEngine;

public class CircuitAnalyzer
{
    // 🔥 NUEVO (principal)
    public string AnalyzeVoltage(float measured, float target, float tolerance)
    {
        if (measured == 0)
            return "⚠ No hay medición. Conecta el multímetro.";

        if (Mathf.Abs(measured - target) <= tolerance)
            return "✅ Voltaje correcto. El circuito funciona bien.";

        if (measured < target)
            return "⚠ Voltaje bajo. Posible resistencia alta o mala conexión.";

        if (measured > target)
            return "⚠ Voltaje alto. Posible error en el circuito.";

        return "❓ Estado desconocido.";
    }

    // ⚙️ COMPATIBILIDAD (opcional)
    public string Diagnose(List<ElectricalComponent> components)
    {
        return "Diagnóstico básico disponible.";
    }

    public string GetDetailedAnalysis(List<ElectricalComponent> components, float totalCurrent)
    {
        return "Corriente total: " + totalCurrent.ToString("F2") + " A";
    }
}