using System.Collections.Generic;
using UnityEngine;

public class DiagnosticSystem
{
    public string GetDiagnosis(List<ElectricalComponent> components, float totalCurrent)
    {
        foreach (var comp in components)
        {
            if (comp is LED led)
            {
                if (!led.isOn)
                {
                    return "El LED no enciende. Posible falta de voltaje o mala conexión.";
                }

                if (led.current > 0.1f)
                {
                    return "El LED está en sobrecarga. La resistencia es demasiado baja.";
                }

                if (led.current < 0.02f)
                {
                    return "El LED recibe poca corriente. La resistencia es demasiado alta.";
                }
            }
        }

        if (totalCurrent <= 0)
        {
            return "No hay corriente en el circuito. Verifica conexiones.";
        }

        return "El circuito funciona correctamente.";
    }

    public string GetDetailedAnalysis(List<ElectricalComponent> components, float totalCurrent)
    {
        string result = "Analisis detallado:\n";

        foreach (var comp in components)
        {
            if (comp is Resistor r)
            {
                result += "- Resistencia: " + r.resistance + " ohmios\n";
            }
            else if (comp is LED led)
            {
                result += "- LED corriente: " + led.current.ToString("F2") + " A\n";
            }
        }

        result += "- Corriente total: " + totalCurrent.ToString("F2") + " A\n";

        return result;
    }
}