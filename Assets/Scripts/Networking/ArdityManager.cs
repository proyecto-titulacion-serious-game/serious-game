using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Puente entre el juego y un Arduino físico via Ardity (puerto serial).
///
/// Protocolo de comunicación (texto, terminado en \n):
///   Unity → Arduino : "PIN:2,MODE:O,STATE:H,BLINK:500\n"
///     MODE: O=OUTPUT I=INPUT
///     STATE: H=HIGH  L=LOW
///     BLINK: 0=sin blink, N=intervalo en ms
///
///   Arduino → Unity : "V:4.97,I:15.2,ADC:890,PIN:2\n"
///     V = voltaje (V), I = corriente (mA), ADC = valor A0 (0-1023), PIN = pin activo
///
/// Uso:
///   - Arrastra el prefab Ardity/Prefabs/SerialController a la escena Tecnico.unity
///   - Asigna ese GO al campo serialController de este script
///   - En el Inspector del SerialController establece el puerto COM correcto
///   - ArdityManager se auto-inicializa en la escena
/// </summary>
public class ArdityManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Ardity")]
    [Tooltip("GO con SerialController de Ardity. Si es null, se busca en escena.")]
    public SerialController serialController;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool   _connected;
    [SerializeField] private float  _lastVoltage;
    [SerializeField] private float  _lastCurrentMA;
    [SerializeField] private int    _lastADC;
    [SerializeField] private int    _lastPin;

    // ─────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────
    public static ArdityManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    /// <summary>Cuando el Arduino físico conecta (true) o desconecta (false).</summary>
    public static event Action<bool> OnArduinoConnection;

    /// <summary>Telemetría recibida del Arduino: (voltios, mA, ADC, pin).</summary>
    public static event Action<float, float, int, int> OnTelemetryReceived;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public bool  IsConnected    => _connected;
    public float LastVoltage    => _lastVoltage;
    public float LastCurrentMA  => _lastCurrentMA;
    public int   LastADC        => _lastADC;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (serialController == null)
            serialController = FindAnyObjectByType<SerialController>();

        if (serialController == null)
            Debug.LogWarning("[ArdityManager] SerialController no encontrado. " +
                "Arrastra el prefab Ardity/Prefabs/SerialController a la escena y asígnalo.");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────
    //  Callbacks desde SerialController (messageListener)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado por SerialController.messageListener → SendMessage("OnMessageArrived").
    /// Parsea el mensaje de telemetría del Arduino.
    /// Formato esperado: "V:4.97,I:15.2,ADC:890,PIN:2"
    /// </summary>
    public void OnMessageArrived(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        float v = 0f; float i = 0f; int adc = 0; int pin = 0;

        TryParseFloat(msg, "V",   ref v);
        TryParseFloat(msg, "I",   ref i);
        TryParseInt  (msg, "ADC", ref adc);
        TryParseInt  (msg, "PIN", ref pin);

        _lastVoltage   = v;
        _lastCurrentMA = i;
        _lastADC       = adc;
        _lastPin       = pin;

        OnTelemetryReceived?.Invoke(v, i, adc, pin);
    }

    /// <summary>
    /// Llamado por SerialController.messageListener → SendMessage("OnConnectionEvent").
    /// </summary>
    public void OnConnectionEvent(bool connected)
    {
        _connected = connected;
        Debug.Log($"[ArdityManager] Arduino físico {(connected ? "conectado" : "desconectado")}.");
        OnArduinoConnection?.Invoke(connected);
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Envía un sketch al Arduino físico via serial.
    /// </summary>
    /// <param name="pin">Número de pin D#.</param>
    /// <param name="isOutput">true = OUTPUT, false = INPUT.</param>
    /// <param name="isHigh">true = HIGH, false = LOW (ignorado si blink).</param>
    /// <param name="blinkMs">0 = sin blink, >0 = intervalo del blink en ms.</param>
    public bool SendSketch(int pin, bool isOutput, bool isHigh, int blinkMs)
    {
        if (serialController == null || !_connected)
            return false;

        string mode  = isOutput ? "O" : "I";
        string state = isHigh   ? "H" : "L";
        string msg   = $"PIN:{pin},MODE:{mode},STATE:{state},BLINK:{blinkMs}";

        serialController.SendSerialMessage(msg);
        Debug.Log($"[ArdityManager] Enviado → {msg}");
        return true;
    }

    // ─────────────────────────────────────────────
    //  Helpers de parseo
    // ─────────────────────────────────────────────
    static readonly Regex _floatRx = new Regex(@"(\w+):([\d.]+)");
    static readonly Regex _intRx   = new Regex(@"(\w+):(\d+)");

    static void TryParseFloat(string msg, string key, ref float val)
    {
        var m = Regex.Match(msg, key + @":([\d.]+)");
        if (m.Success) float.TryParse(m.Groups[1].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out val);
    }

    static void TryParseInt(string msg, string key, ref int val)
    {
        var m = Regex.Match(msg, key + @":(\d+)");
        if (m.Success) int.TryParse(m.Groups[1].Value, out val);
    }
}
