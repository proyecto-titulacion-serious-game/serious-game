using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Motor de diagnóstico del circuito para el panel del Técnico.
/// Clase pura (sin MonoBehaviour) — se instancia desde TechnicianUI.cs con:
///     private DiagnosticSystem _diagnostic = new DiagnosticSystem();
///
/// El DiagnosticSystemHolder en la jerarquía puede estar vacío —
/// este script NO necesita estar adjunto a ningún GameObject.
/// </summary>
public class DiagnosticSystem
{
    // ─────────────────────────────────────────────
    //  API Principal
    // ─────────────────────────────────────────────

    /// <summary>
    /// Diagnóstico principal — texto corto que aparece en TMP_Diagnostico.
    /// </summary>
    public string GetDiagnosis(List<ElectricalComponent> components, float totalCurrent)
    {
        if (components == null || components.Count == 0)
            return "[!] Sin componentes en el circuito.";

        if (totalCurrent <= 0f)
            return "[!] Sin corriente. Verifica conexiones y fuente de voltaje.";

        var sb = new StringBuilder();

        foreach (var comp in components)
        {
            // ── LED ─────────────────────────────────────────
            if (comp is LED led)
            {
                if (led.polarityInverted)
                {
                    sb.AppendLine("[X] LED con polaridad invertida -> apagado.");
                    sb.AppendLine("   Indica al Explorador girar el LED 180.");
                    continue;
                }

                if (!led.isOn)
                {
                    sb.AppendLine("[.] LED apagado -- corriente insuficiente.");
                    sb.AppendLine($"   Corriente actual: {led.current * 1000f:F1} mA");
                    sb.AppendLine("   Minimo para encender: 5 mA");
                    continue;
                }

                switch (led.state)
                {
                    case LEDState.Overload:
                        sb.AppendLine("[X] LED en SOBRECARGA -- resistencia demasiado baja.");
                        sb.AppendLine($"   Corriente: {led.current * 1000f:F1} mA (max. seguro: 20 mA)");
                        float rNeed = (led.nodeA != null ? led.nodeA.voltage : 9f) / 0.01f;
                        sb.AppendLine($"   R necesaria aprox {rNeed:F0} Ohm");
                        break;

                    case LEDState.NearOverload:
                        sb.AppendLine("[~] LED cerca del limite.");
                        sb.AppendLine($"   Corriente: {led.current * 1000f:F1} mA");
                        break;

                    case LEDState.Correct:
                        sb.AppendLine("[OK] LED funcionando correctamente.");
                        sb.AppendLine($"   Corriente: {led.current * 1000f:F1} mA");
                        break;
                }
            }

            // ── RESISTOR ────────────────────────────────────
            else if (comp is Resistor r)
            {
                if (r.hasFault)
                {
                    sb.AppendLine($"[!] Resistencia INCORRECTA: {r.resistance:F0} Ohm");
                    sb.AppendLine($"   Valor correcto: {r.correctResistance:F0} Ohm");
                    sb.AppendLine($"   Codigo de colores correcto: {r.GetColorBandString()}");
                }
            }

            // ── CAPACITOR ───────────────────────────────────
            else if (comp is Capacitor cap)
            {
                if (cap.polarityInverted)
                {
                    if (cap.state == CapacitorState.ShortCircuit)
                    {
                        sb.AppendLine("[X] Capacitor en CORTOCIRCUITO -- polaridad invertida.");
                        sb.AppendLine("   PRIORIDAD 1: girar el capacitor inmediatamente.");
                    }
                    else
                    {
                        sb.AppendLine("[!] Capacitor con polaridad invertida.");
                        sb.AppendLine("   Indica al Explorador girar el capacitor 180.");
                    }
                }
            }

            // ── ARDUINO PIN ─────────────────────────────────
            else if (comp is ArduinoPin pin)
            {
                if (pin.hasFault)
                {
                    sb.AppendLine($"[!] Sensor en pin D{pin.pinNumber} -- incorrecto.");
                    sb.AppendLine($"   Pin correcto: D{pin.correctPinNumber}");
                }
                if (pin.hasLooseCable)
                {
                    sb.AppendLine("[!] Cable suelto detectado en protoboard.");
                    sb.AppendLine("   Indica al Explorador reconectar el cable.");
                }
            }
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "OK Circuito sin fallas detectadas.";
    }

    // ─────────────────────────────────────────────
    //  Análisis detallado — para panel del Técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// Análisis técnico completo con valores numéricos.
    /// Aparece en la sección de datos del panel.
    /// </summary>
    public string GetDetailedAnalysis(List<ElectricalComponent> components, float totalCurrent)
    {
        if (components == null || components.Count == 0)
            return "Sin datos.";

        var sb = new StringBuilder();
        sb.AppendLine("-- ANALISIS DETALLADO --");

        float sourceVoltage = 0f;

        foreach (var comp in components)
        {
            if (comp is VoltageSource vs)
            {
                sourceVoltage = vs.voltage;
                sb.AppendLine($"Fuente:      {vs.voltage:F1} V");
            }
            else if (comp is Resistor r)
            {
                sb.AppendLine($"Resistencia: {r.resistance:F0} Ohm {(r.hasFault ? "[!] FALLA" : "OK")}");
                if (r.hasFault)
                    sb.AppendLine($"  Correcto:  {r.correctResistance:F0} Ohm");

                float vDrop = comp.current * r.resistance;
                sb.AppendLine($"  Caida V:   {vDrop:F2} V");
                sb.AppendLine($"  Corriente: {comp.current * 1000f:F1} mA");
            }
            else if (comp is LED led)
            {
                sb.AppendLine($"LED:         {(led.isOn ? "ENCENDIDO" : "APAGADO")} {GetLEDStateIcon(led.state)}");
                sb.AppendLine($"  Corriente: {led.current * 1000f:F1} mA");
                sb.AppendLine($"  Caida V:   {led.voltageDrop:F2} V");

                if (led.polarityInverted)
                    sb.AppendLine("  [!] Polaridad INVERTIDA");
            }
            else if (comp is Capacitor cap)
            {
                sb.AppendLine($"Capacitor:   {(cap.polarityInverted ? "[!] INVERTIDO" : "OK")}");
                sb.AppendLine($"  Estado:    {cap.state}");
            }
            else if (comp is ArduinoPin pin)
            {
                sb.AppendLine($"Arduino D{pin.pinNumber}: {(pin.hasFault ? $"[!] Pin incorrecto (correcto: D{pin.correctPinNumber})" : "OK")}");
                if (pin.hasLooseCable)
                    sb.AppendLine("  [!] Cable suelto");
            }
        }

        sb.AppendLine($"--------------------");
        sb.AppendLine($"I total:     {totalCurrent * 1000f:F1} mA");

        if (totalCurrent > 0.1f)
            sb.AppendLine("Estado:      [!] SOBRECARGA");
        else if (totalCurrent > 0.005f && totalCurrent <= 0.02f)
            sb.AppendLine("Estado:      OK CORRECTO");
        else if (totalCurrent > 0)
            sb.AppendLine("Estado:      [!] Fuera de rango");

        return sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────
    //  Pista para el Técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// Devuelve la próxima acción que el Técnico debe indicar al Explorador.
    /// </summary>
    public string GetNextAction(List<ElectricalComponent> components, float totalCurrent)
    {
        if (components == null) return "Carga el circuito primero.";

        // Prioridad 1: Capacitor en cortocircuito
        foreach (var comp in components)
            if (comp is Capacitor cap && cap.polarityInverted && cap.state == CapacitorState.ShortCircuit)
                return "[!!] URGENTE: Di al Explorador que gire el capacitor 180.";

        // Prioridad 2: Sin corriente
        if (totalCurrent <= 0f)
            return "Di al Explorador que verifique que todos los cables estan conectados.";

        // Prioridad 3: LED en sobrecarga
        foreach (var comp in components)
            if (comp is LED led && led.state == LEDState.Overload)
            {
                float vSource = GetSourceVoltage(components);
                float rTarget = vSource / 0.01f - led.resistance;
                return $"Resistencia incorrecta. Calcula: R = {vSource:F0}V / 10mA - {led.resistance:F0}Ohm aprox {rTarget:F0}Ohm\nEscribe {Mathf.Round(rTarget / 10) * 10:F0} y pulsa ENVIAR.";
            }

        // Prioridad 4: LED con polaridad invertida
        foreach (var comp in components)
            if (comp is LED led2 && led2.polarityInverted)
                return "Di al Explorador que gire el LED 180 para corregir la polaridad.";

        // Prioridad 5: Resistencia incorrecta
        foreach (var comp in components)
            if (comp is Resistor r && r.hasFault)
                return $"Resistencia incorrecta ({r.resistance:F0}Ohm).\nEscribe {r.correctResistance:F0} en el campo y pulsa ENVIAR.";

        // Prioridad 6: Arduino
        foreach (var comp in components)
            if (comp is ArduinoPin pin && pin.hasFault)
                return $"Di al Explorador: mover cable del pin D{pin.pinNumber} al pin D{pin.correctPinNumber}.";

        return "OK El circuito esta correcto. Reto completado!";
    }

    // ─────────────────────────────────────────────
    //  Helpers privados
    // ─────────────────────────────────────────────

    string GetLEDStateIcon(LEDState state) => state switch
    {
        LEDState.Correct      => "[OK]",
        LEDState.NearOverload => "[~]",
        LEDState.Overload     => "[X]",
        _                     => "[.]"
    };

    float GetSourceVoltage(List<ElectricalComponent> components)
    {
        foreach (var c in components)
            if (c is VoltageSource vs) return vs.voltage;
        return 9f;
    }
}
