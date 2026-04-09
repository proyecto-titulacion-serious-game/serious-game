using System.Collections.Generic;
using System.Text;

public class CircuitAnalyzer
{
    public string Diagnose(List<ElectricalComponent> components)
    {
        foreach (var comp in components)
        {
            if (comp is LED led)
            {
                if (led.current <= 0)
                    return "❌ No hay corriente en el circuito.";
                
                if (led.current > 0.1f)
                    return "⚠️ Sobrecarga detectada.";
            }
        }

        return "✅ Circuito funcionando correctamente.";
    }

    public string GetDetailedAnalysis(List<ElectricalComponent> components, float totalCurrent)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("ANÁLISIS TÉCNICO:");

        foreach (var comp in components)
        {
            if (comp is Resistor res)
            {
                if (res.resistance < 20)
                {
                    sb.AppendLine("⚠️ Resistencia muy baja → aumenta el valor.");
                }
                else if (res.resistance > 300)
                {
                    sb.AppendLine("⚠️ Resistencia muy alta → reduce el valor.");
                }
                else
                {
                    sb.AppendLine("✅ Resistencia en rango correcto.");
                }
            }

            if (comp is LED led)
            {
                if (led.current > 0.1f)
                {
                    sb.AppendLine("🔥 LED en sobrecarga → riesgo de daño.");
                }
                else if (led.current <= 0)
                {
                    sb.AppendLine("❌ LED no recibe corriente.");
                }
                else
                {
                    sb.AppendLine("✅ LED funcionando correctamente.");
                }
            }
        }

        // análisis global
        if (totalCurrent > 0.1f)
        {
            sb.AppendLine("💡 Consejo: Aumenta la resistencia total del circuito.");
        }
        else if (totalCurrent < 0.02f)
        {
            sb.AppendLine("💡 Consejo: Disminuye la resistencia o revisa conexiones.");
        }

        return sb.ToString();

}
}