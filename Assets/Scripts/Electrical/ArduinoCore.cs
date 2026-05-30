using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Emulador del procesador ATmega328P de una placa Arduino.
/// Simula el ciclo loop() de Arduino de forma continua mediante una corrutina.
///
/// Funciones soportadas:
///   - digitalWrite(pin, HIGH/LOW) → salida TTL / 0 V
///   - digitalRead(pin)            → lee estado de un pin digital
///   - analogRead(A0)              → ADC 10 bits: mapea 0–5 V a 0–1023
///   - delay(ms)                   → pausa el ciclo simulado
///
/// SETUP: añadir al GameObject del Arduino 3D en el Reto 4.
///        Arrastrar Nodo_P13, Nodo_GND y Nodo_A0 a sus campos en el Inspector.
/// </summary>
public class ArduinoCore : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Nodos eléctricos (pines físicos)")]
    [Tooltip("ElectricalNode del pin D13 (salida digital principal).")]
    public ElectricalNode nodoP13;
    [Tooltip("ElectricalNode de tierra (GND).")]
    public ElectricalNode nodoGND;
    [Tooltip("ElectricalNode del pin A0 (entrada analógica del sensor).")]
    public ElectricalNode nodoA0;

    [Header("Configuración del sketch cargado")]
    [Tooltip("Número de pin activo (configurado vía ArduinoIDEUI).")]
    public int      activePinNumber = 13;
    [Tooltip("Modo del pin activo.")]
    public PinMode  activePinMode   = PinMode.OUTPUT;
    [Tooltip("Estado del pin activo en modo OUTPUT.")]
    public PinState activePinState  = PinState.LOW;
    [Tooltip("Activar blink (alterna HIGH/LOW cada blinkInterval ms).")]
    public bool     blinkEnabled    = false;
    [SerializeField] private int _blinkIntervalMs = 500;

    [Header("Hardware")]
    [Tooltip("Voltaje TTL de salida cuando el pin está en HIGH (normalmente 5 V).")]
    public float outputVoltageTTL = 5f;

    [Header("Telemetría (solo lectura)")]
    [SerializeField] private int   _adcValue;
    [SerializeField] private float _outputVoltage;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public int   AdcValue      => _adcValue;
    public float OutputVoltage => _outputVoltage;

    // ─────────────────────────────────────────────
    //  Aliases de compatibilidad (API antigua)
    // ─────────────────────────────────────────────
    public ElectricalNode pin13Node { get => nodoP13; set => nodoP13 = value; }
    public ElectricalNode gndNode   { get => nodoGND; set => nodoGND = value; }
    public ElectricalNode a0Node    { get => nodoA0;  set => nodoA0  = value; }

    // ─────────────────────────────────────────────
    //  Evento
    // ─────────────────────────────────────────────
    public static event Action<int> OnAdcSampleReady;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool             _blinkState = false;
    private CircuitSimulator _sim;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        if (nodoGND != null) nodoGND.voltage = 0f;
        StartCoroutine(ArduinoLoop());
    }

    void OnDisable() => StopAllCoroutines();

    // ─────────────────────────────────────────────
    //  Ciclo simulado (equivale al loop() de Arduino)
    // ─────────────────────────────────────────────
    IEnumerator ArduinoLoop()
    {
        while (true)
        {
            _adcValue = AnalogRead();
            OnAdcSampleReady?.Invoke(_adcValue);

            if (activePinMode == PinMode.OUTPUT)
            {
                bool state = blinkEnabled ? _blinkState : (activePinState == PinState.HIGH);
                DigitalWrite(activePinNumber, state);
            }

            float waitSec = blinkEnabled ? _blinkIntervalMs / 1000f : 0.05f;
            yield return new WaitForSeconds(waitSec);

            if (blinkEnabled) _blinkState = !_blinkState;
        }
    }

    // ─────────────────────────────────────────────
    //  API de simulación de pines
    // ─────────────────────────────────────────────

    public void DigitalWrite(int pin, bool high)
    {
        float newVoltage = high ? outputVoltageTTL : 0f;
        if (Mathf.Approximately(_outputVoltage, newVoltage)) return;

        _outputVoltage = newVoltage;
        ElectricalNode target = PinToNode(pin);
        if (target != null) target.voltage = _outputVoltage;

        if (_sim == null) _sim = FindAnyObjectByType<CircuitSimulator>();
        _sim?.MarkDirty();
    }

    public bool DigitalRead(int pin)
    {
        ElectricalNode node = PinToNode(pin);
        return node != null && node.voltage >= 2.5f;
    }

    public int AnalogRead()
    {
        if (nodoA0 == null) return 0;
        float voltage = Mathf.Clamp(nodoA0.voltage, 0f, 5f);
        return Mathf.RoundToInt(voltage / 5f * 1023f);
    }

    // ─────────────────────────────────────────────
    //  API para ArduinoIDEUI / esquemas modernos
    // ─────────────────────────────────────────────

    public void LoadSketch(int pinNumber, PinMode mode, PinState state, bool blink, int blinkMs)
    {
        activePinNumber  = pinNumber;
        activePinMode    = mode;
        activePinState   = state;
        blinkEnabled     = blink;
        _blinkIntervalMs = blinkMs;

        Debug.Log($"[ArduinoCore] Sketch cargado: pin={pinNumber}, mode={mode}, " +
                  $"state={state}, blink={blink} ({blinkMs}ms)");
    }

    // ─────────────────────────────────────────────
    //  API de compatibilidad (ArduinoNetworkBridge)
    // ─────────────────────────────────────────────

    public void RecibirCodigoDePC(bool isOutput, bool isHigh, float delayMs, bool isBlink)
    {
        LoadSketch(
            pinNumber: activePinNumber,
            mode:      isOutput ? PinMode.OUTPUT : PinMode.INPUT,
            state:     isHigh   ? PinState.HIGH  : PinState.LOW,
            blink:     isBlink,
            blinkMs:   Mathf.RoundToInt(delayMs)
        );

        if (_sim == null) _sim = FindAnyObjectByType<CircuitSimulator>();
        _sim?.MarkDirty();
    }

    public int GetAnalogReadA0() => _adcValue;

    // ─────────────────────────────────────────────
    //  Utilidades
    // ─────────────────────────────────────────────
    ElectricalNode PinToNode(int pin)
    {
        if (pin == 13 || pin == activePinNumber) return nodoP13;
        return null;
    }
}

// ─────────────────────────────────────────────
//  Enums auxiliares
// ─────────────────────────────────────────────
public enum PinMode  { OUTPUT, INPUT, INPUT_PULLUP }
public enum PinState { LOW, HIGH }
