using UnityEngine;

/// <summary>
/// Simula un pin del Arduino para el Reto 4.
/// Permite fallas: pin incorrecto y cable suelto en la protoboard.
/// </summary>
public class ArduinoPin : ElectricalComponent
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Configuración del pin")]
    [Tooltip("Número de pin en el Arduino (educativo).")]
    public int  pinNumber         = 0;
    public int  correctPinNumber  = 2;   // Pin correcto según el diagrama del Técnico
    public bool isDigital         = true;

    [Header("Estado de fallas")]
    public bool hasFault      = false;   // Pin incorrecto
    public bool hasLooseCable = false;   // Cable suelto en protoboard

    [Header("Señal")]
    [Range(0f, 5f)]
    public float signalVoltage = 0f;     // 0V = LOW, 5V = HIGH

    // ─────────────────────────────────────────────
    //  ElectricalComponent
    // ─────────────────────────────────────────────
    public override float GetResistance()
    {
        // Pin incorrecto o cable suelto = circuito abierto
        return (hasLooseCable || hasFault) ? 1_000_000f : 10f;
    }

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) return;

        // Sin señal si hay falla de cable o pin incorrecto
        if (hasLooseCable || hasFault)
        {
            current     = 0f;
            voltageDrop = 0f;
            signalVoltage = 0f;
            return;
        }

        signalVoltage = nodeA.voltage;
        float voltageDiff = nodeA.voltage - nodeB.voltage;
        current     = voltageDiff / GetResistance();
        voltageDrop = voltageDiff;
    }

    // ─────────────────────────────────────────────
    //  API de juego
    // ─────────────────────────────────────────────

    /// <summary>Aplica falla: pone el pin en número incorrecto.</summary>
    public void ApplyFault()
    {
        hasFault  = true;
        // Poner en un pin incorrecto
        pinNumber = correctPinNumber == 2 ? 4 : 2;
    }

    /// <summary>Repara el pin al número correcto.</summary>
    public void RepairPin(int proposedPin)
    {
        if (proposedPin == correctPinNumber)
        {
            pinNumber = correctPinNumber;
            hasFault  = false;
            Debug.Log($"[ArduinoPin] Pin {correctPinNumber} conectado correctamente.");
        }
        else
        {
            Debug.Log($"[ArduinoPin] Pin {proposedPin} incorrecto. Correcto: {correctPinNumber}.");
        }
    }

    /// <summary>Conecta el cable suelto.</summary>
    public void FixLooseCable()
    {
        hasLooseCable = false;
        Debug.Log("[ArduinoPin] Cable reconectado en la protoboard.");
    }

    public bool IsFullyOperational() => !hasFault && !hasLooseCable;
}