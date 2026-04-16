using UnityEngine;

public class CircuitAnalyzer
{
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

    public string AnalyzeByLevel(LevelType level, float measured, float target, float tolerance, CircuitManager circuit)
    {
        switch (level)
        {
            case LevelType.OhmLaw:
                return AnalyzeVoltage(measured, target, tolerance);

            case LevelType.Parallel:
                return AnalyzeParallel(circuit);

            default:
                return "ℹ Nivel en desarrollo.";
        }
    }

    string AnalyzeParallel(CircuitManager circuit)
    {
        bool anyLedOff = false;

        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                if (!led.isOn)
                {
                    anyLedOff = true;
                }
            }
        }

        if (anyLedOff)
            return "⚠ Hay una rama del circuito paralelo sin funcionar. Revisa conexiones o componentes de esa rama.";

        return "✅ Todas las ramas del circuito paralelo están funcionando.";
    }
}