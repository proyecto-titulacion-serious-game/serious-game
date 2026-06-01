using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// IDE de código real para el Reto 4.
/// El Técnico edita un sketch de Arduino con C++ simplificado y lo sube
/// a un Arduino físico (via Ardity) o a la simulación interna (ArduinoCore).
///
/// Sketch con falla (pre-cargado):
///   pinMode(4, OUTPUT)  ← el estudiante debe cambiar 4 → 2
///
/// Flujo:
///   1. Editar código en el InputField
///   2. Clic "Verificar" → análisis de errores / preview
///   3. Clic "Subir"    → envío a Arduino físico o simulación
///
/// SETUP en Inspector:
///   - inputCode    → TMP_InputField (multiline, monospace)
///   - txtConsole   → TMP_Text (salida de compilador/serial)
///   - txtPreview   → TMP_Text (resumen del sketch parseado)
///   - btnVerify    → Button "Verificar"
///   - btnUpload    → Button "Subir"
/// </summary>
public class ArduinoCodeEditor : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("UI (asignar en Inspector)")]
    public TMP_InputField inputCode;
    public TMP_Text       txtConsole;
    public TMP_Text       txtPreview;
    public TMP_Text       txtStatus;
    public Button         btnVerify;
    public Button         btnUpload;

    [Header("Sketch inicial (se carga al iniciar el reto)")]
    [TextArea(10, 20)]
    public string sketchInicial = DEFAULT_SKETCH;

    // ─────────────────────────────────────────────
    //  Sketch con la falla del Reto 4
    // ─────────────────────────────────────────────
    const string DEFAULT_SKETCH =
@"// Reto 4: Sistema de alerta con sensor
// TAREA: Encuentra y corrige el error en este sketch

void setup() {
  pinMode(4, OUTPUT);   // <- revisa este numero de pin
}

void loop() {
  digitalWrite(4, HIGH);
  delay(500);
  digitalWrite(4, LOW);
  delay(500);
}";

    // ─────────────────────────────────────────────
    //  Resultado del parseo
    // ─────────────────────────────────────────────
    struct SketchData
    {
        public bool valid;
        public int  pin;
        public bool isOutput;
        public bool isHigh;
        public bool blink;
        public int  blinkMs;
        public string error;
    }

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        if (inputCode != null)
        {
            inputCode.text = sketchInicial;
            inputCode.onValueChanged.AddListener(_ => OnCodeChanged());
        }

        if (btnVerify != null) btnVerify.onClick.AddListener(Verify);
        if (btnUpload != null) btnUpload.onClick.AddListener(Upload);

        // Suscribir telemetría de Ardity
        ArdityManager.OnTelemetryReceived += OnTelemetry;
        ArdityManager.OnArduinoConnection += OnArduinoConnect;

        SetStatus("Listo. Edita el sketch y haz clic en Verificar.", Color.white);
        UpdatePreview();
    }

    void OnDestroy()
    {
        ArdityManager.OnTelemetryReceived -= OnTelemetry;
        ArdityManager.OnArduinoConnection -= OnArduinoConnect;
    }

    // ─────────────────────────────────────────────
    //  Acciones de los botones
    // ─────────────────────────────────────────────

    public void Verify()
    {
        string code = inputCode != null ? inputCode.text : "";
        var data = ParseSketch(code);

        if (!data.valid)
        {
            Log($"<color=#FF6B6B>Error de compilación:\n{data.error}</color>");
            SetStatus("Errores encontrados.", Color.red);
            return;
        }

        string modeStr  = data.isOutput ? "OUTPUT" : "INPUT";
        string stateStr = data.blink    ? $"BLINK cada {data.blinkMs} ms"
                                        : (data.isHigh ? "HIGH (5V)" : "LOW (0V)");

        Log($"<color=#00FF7F>✓ Compilación exitosa.\n\n" +
            $"  Pin activo : D{data.pin}\n" +
            $"  Modo       : {modeStr}\n" +
            $"  Estado     : {stateStr}</color>");

        SetStatus($"OK — Pin D{data.pin}, {modeStr}, {stateStr}", Color.green);
        UpdatePreview(data);
    }

    public void Upload()
    {
        string code = inputCode != null ? inputCode.text : "";
        var data = ParseSketch(code);

        if (!data.valid)
        {
            Log($"<color=#FF6B6B>No se puede subir: {data.error}</color>");
            SetStatus("Corrige los errores antes de subir.", Color.red);
            return;
        }

        // ── Ruta 1: Arduino físico via Ardity ────────────────────────────
        var ardity = ArdityManager.Instance;
        if (ardity != null && ardity.IsConnected)
        {
            bool ok = ardity.SendSketch(data.pin, data.isOutput, data.isHigh,
                                        data.blink ? data.blinkMs : 0);
            if (ok)
            {
                Log($"<color=#00FF7F>▶ Sketch subido al Arduino físico.\n" +
                    $"  Pin D{data.pin} | {(data.isOutput ? "OUTPUT" : "INPUT")} | " +
                    $"{(data.blink ? $"BLINK {data.blinkMs}ms" : (data.isHigh ? "HIGH" : "LOW"))}</color>\n" +
                    $"<color=#888>Esperando telemetría serial...</color>");
                SetStatus($"Subido → Arduino físico. Pin D{data.pin}.", Color.green);
                ApplyToArduinoCore(data);
                return;
            }
        }

        // ── Ruta 2: ArduinoNetworkBridge (Fusion multijugador) ───────────
        var bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
        if (bridge != null)
        {
            bridge.RPC_SubirCodigoArduino(data.pin, data.isOutput, data.isHigh,
                                          data.blink ? data.blinkMs : 0f, data.blink);
            Log($"<color=#00FF7F>▶ Sketch subido por red (Fusion).\n  Pin D{data.pin}.</color>");
            SetStatus($"Subido → Red. Pin D{data.pin}.", Color.green);
            ApplyToArduinoCore(data);
            return;
        }

        // ── Ruta 3: modo offline/test — ArduinoCore local ────────────────
        ApplyToArduinoCore(data);
        Log($"<color=#FFAA00>▶ Sketch subido en modo offline (simulación).\n" +
            $"  Pin D{data.pin} | {(data.isOutput ? "OUTPUT" : "INPUT")} | " +
            $"{(data.blink ? $"BLINK {data.blinkMs}ms" : (data.isHigh ? "HIGH" : "LOW"))}\n\n" +
            $"  Para conectar un Arduino físico:\n" +
            $"  1. Sube el sketch receptor al Arduino (ver RECEPTOR en el HUD)\n" +
            $"  2. Asigna el puerto COM en el SerialController de la escena</color>");
        SetStatus($"Subido (offline). Pin D{data.pin}.", new Color(1f, 0.7f, 0f));
    }

    // ─────────────────────────────────────────────
    //  Parser de código Arduino (C++ simplificado)
    // ─────────────────────────────────────────────

    static readonly Regex RxPinMode      = new Regex(@"pinMode\s*\(\s*(\d+)\s*,\s*(OUTPUT|INPUT|INPUT_PULLUP)\s*\)");
    static readonly Regex RxDigitalWrite = new Regex(@"digitalWrite\s*\(\s*(\d+)\s*,\s*(HIGH|LOW)\s*\)");
    static readonly Regex RxDelay        = new Regex(@"delay\s*\(\s*(\d+)\s*\)");

    static SketchData ParseSketch(string code)
    {
        var result = new SketchData();

        if (string.IsNullOrWhiteSpace(code))
        {
            result.error = "El sketch está vacío.";
            return result;
        }

        // Eliminar comentarios de línea antes de parsear
        string clean = Regex.Replace(code, @"//[^\n]*", "");

        // pinMode
        var pmMatch = RxPinMode.Match(clean);
        if (!pmMatch.Success)
        {
            result.error = "No se encontró pinMode(pin, MODE) en setup().";
            return result;
        }
        result.pin      = int.Parse(pmMatch.Groups[1].Value);
        result.isOutput = pmMatch.Groups[2].Value == "OUTPUT";

        // digitalWrite — busca todas las ocurrencias
        var dwMatches = RxDigitalWrite.Matches(clean);
        if (dwMatches.Count == 0)
        {
            result.error = "No se encontró digitalWrite(pin, STATE) en loop().";
            return result;
        }

        // Verificar que el pin en digitalWrite coincide con pinMode
        foreach (Match m in dwMatches)
        {
            int dwPin = int.Parse(m.Groups[1].Value);
            if (dwPin != result.pin)
            {
                result.error = $"El pin en digitalWrite ({dwPin}) no coincide con pinMode ({result.pin}).";
                return result;
            }
        }

        result.isHigh = dwMatches[0].Groups[2].Value == "HIGH";

        // Blink: si hay 2+ digitalWrite con delay entre ellos
        var delayMatches = RxDelay.Matches(clean);
        if (dwMatches.Count >= 2 && delayMatches.Count >= 1)
        {
            result.blink   = true;
            result.blinkMs = int.Parse(delayMatches[0].Groups[1].Value);
        }

        result.valid = true;
        return result;
    }

    // ─────────────────────────────────────────────
    //  Aplicar al ArduinoCore interno
    // ─────────────────────────────────────────────

    static void ApplyToArduinoCore(SketchData data)
    {
        var core = FindAnyObjectByType<ArduinoCore>();
        if (core == null)
        {
            Debug.LogWarning("[ArduinoCodeEditor] ArduinoCore no encontrado en escena.");
            return;
        }
        core.RecibirCodigoDePC(
            pin:      data.pin,
            isOutput: data.isOutput,
            isHigh:   data.isHigh,
            delayMs:  data.blinkMs,
            isBlink:  data.blink);
    }

    // ─────────────────────────────────────────────
    //  Callbacks de Ardity
    // ─────────────────────────────────────────────

    void OnTelemetry(float v, float mA, int adc, int pin)
    {
        Log($"<color=#00BFFF>◉ Telemetría Arduino físico:\n" +
            $"  Voltaje  : {v:F2} V\n" +
            $"  Corriente: {mA:F1} mA\n" +
            $"  ADC A0   : {adc} ({adc / 1023f * 5f:F2}V)\n" +
            $"  Pin activo: D{pin}</color>");
    }

    void OnArduinoConnect(bool connected)
    {
        if (connected)
        {
            SetStatus("Arduino físico conectado.", Color.green);
            Log("<color=#00FF7F>◉ Arduino físico detectado en puerto serial.\n" +
                "  Ahora puedes subir el sketch y recibir telemetría real.</color>");
        }
        else
        {
            SetStatus("Arduino físico desconectado.", Color.yellow);
            Log("<color=#FFAA00>◉ Arduino físico desconectado. Modo simulación activo.</color>");
        }
    }

    // ─────────────────────────────────────────────
    //  UI helpers
    // ─────────────────────────────────────────────

    void OnCodeChanged()
    {
        // Limpiar el preview mientras se escribe (no parsear en cada keystroke)
        if (txtPreview != null) txtPreview.text = "<color=#888><i>Haz clic en Verificar para analizar el código.</i></color>";
    }

    void UpdatePreview(SketchData? data = null)
    {
        if (txtPreview == null) return;
        if (data == null || !data.Value.valid)
        {
            txtPreview.text = "<color=#888><i>Haz clic en Verificar para analizar el código.</i></color>";
            return;
        }
        var d = data.Value;
        txtPreview.text =
            $"<b>Sketch parseado:</b>\n" +
            $"  <color=#FFD700>pin</color>    = D{d.pin}\n" +
            $"  <color=#FFD700>modo</color>   = {(d.isOutput ? "OUTPUT" : "INPUT")}\n" +
            $"  <color=#FFD700>estado</color> = {(d.blink ? $"BLINK cada {d.blinkMs}ms" : (d.isHigh ? "HIGH (5V)" : "LOW (0V)"))}";
    }

    void Log(string richText)
    {
        if (txtConsole == null) return;
        txtConsole.text = richText;
    }

    void SetStatus(string msg, Color c)
    {
        if (txtStatus == null) return;
        txtStatus.text  = msg;
        txtStatus.color = c;
    }
}
