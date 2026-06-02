using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz de programación para el Técnico (Reto 4 Sandbox).
///
/// Dos modos:
///   - Modo Bloques: 4 dropdowns (pin, mode, state, extra) → generan preview de código.
///   - Modo Código Libre: TMP_InputField con parser Regex (<see cref="ArduinoCodeParser"/>).
///
/// Al compilar, extrae (pin, mode, state, blink, blinkMs) y envía al Explorador
/// via ArduinoNetworkBridge (Fusion) o ArduinoCore local (modo offline).
/// </summary>
public class ArduinoIDEUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector — Modo Bloques
    // ─────────────────────────────────────────────
    [Header("Modo Bloques — Dropdowns")]
    public TMP_Dropdown pinDropdown;
    public TMP_Dropdown modeDropdown;
    public TMP_Dropdown actionDropdown;
    public TMP_Dropdown extraDropdown;

    [Header("Modo Bloques — Controles")]
    public Button         btnUploadCode;
    public TMP_InputField blinkMsInput;

    [Header("Modo Bloques — Textos")]
    public TMP_Text txtCodePreview;
    public TMP_Text txtStatus;
    public TMP_Text txtFileNameLabel;
    public TMP_Text txtConsoleOutput;

    // ─────────────────────────────────────────────
    //  Inspector — Modo Texto Libre (Sandbox)
    // ─────────────────────────────────────────────
    [Header("Modo Texto Libre (Sandbox)")]
    [Tooltip("Campo de texto multi-línea donde el técnico escribe código Arduino libre.")]
    public TMP_InputField codeEditor;
    [Tooltip("Botón que alterna entre Modo Bloques y Modo Texto Libre.")]
    public Button         btnToggleFreeText;
    [Tooltip("Label en btnToggleFreeText — se actualiza al cambiar modo.")]
    public TMP_Text       txtToggleLabel;

    [Header("Red (opcional — auto-detectado si vacío)")]
    public ArduinoNetworkBridge bridge;

    // ─────────────────────────────────────────────
    //  Aliases para compatibilidad con scripts de editor
    // ─────────────────────────────────────────────
    public TMP_Dropdown   dropdownPin    { get => pinDropdown;    set => pinDropdown    = value; }
    public TMP_Dropdown   dropdownMode   { get => modeDropdown;   set => modeDropdown   = value; }
    public TMP_Dropdown   dropdownState  { get => actionDropdown; set => actionDropdown = value; }
    public TMP_Dropdown   dropdownExtra  { get => extraDropdown;  set => extraDropdown  = value; }
    public Button         btnCompilar    { get => btnUploadCode;  set => btnUploadCode  = value; }
    public TMP_InputField inputBlinkMs   { get => blinkMsInput;   set => blinkMsInput   = value; }
    public TMP_Text       txtFileName    { get => txtFileNameLabel; set => txtFileNameLabel  = value; }
    public TMP_Text       txtConsole     { get => txtConsoleOutput; set => txtConsoleOutput  = value; }

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool _freeTextMode = true;  // Reto 4 sandbox: arrancar siempre en código libre

    // ─────────────────────────────────────────────
    //  Unity — Opción A: suscripción a evento de red
    // ─────────────────────────────────────────────

    void OnEnable()
    {
        ArduinoNetworkBridge.OnBridgeReady     += OnBridgeConnected;
        ArduinoNetworkBridge.OnBridgeDestroyed += OnBridgeDisconnected;

        if (bridge == null)
            bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
    }

    void OnDisable()
    {
        ArduinoNetworkBridge.OnBridgeReady     -= OnBridgeConnected;
        ArduinoNetworkBridge.OnBridgeDestroyed -= OnBridgeDisconnected;
    }

    void OnBridgeConnected(ArduinoNetworkBridge b)
    {
        bridge = b;
        SetStatus("Placa conectada.", new Color(0f, 1f, 0.7f));
        Debug.Log("[ArduinoIDEUI] ArduinoNetworkBridge conectado automáticamente.");
    }

    void OnBridgeDisconnected(ArduinoNetworkBridge _)
    {
        bridge = null;
        SetStatus("Placa desconectada.", Color.yellow);
    }

    void Start()
    {
        if (pinDropdown    != null) pinDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (modeDropdown   != null) modeDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (actionDropdown != null) actionDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (extraDropdown  != null) extraDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());

        if (btnUploadCode    != null) btnUploadCode.onClick.AddListener(CompileAndSend);
        if (btnToggleFreeText != null) btnToggleFreeText.onClick.AddListener(ToggleFreeTextMode);

        // Iniciar en modo bloques
        ApplyModeVisibility();
        UpdateCodePreview();
        SetStatus("Esperando conexión de placa...", Color.grey);
    }

    // ─────────────────────────────────────────────
    //  Toggle de modo
    // ─────────────────────────────────────────────

    /// <summary>
    /// Alterna entre Modo Bloques y Modo Texto Libre.
    /// Al entrar al modo libre por primera vez, carga la plantilla starter.
    /// </summary>
    public void ToggleFreeTextMode()
    {
        _freeTextMode = !_freeTextMode;

        if (_freeTextMode && codeEditor != null &&
            string.IsNullOrWhiteSpace(codeEditor.text))
        {
            codeEditor.text = ArduinoCodeParser.StarterTemplate;
        }

        ApplyModeVisibility();
        Log(_freeTextMode
            ? "<color=#AAFFFF>> Modo Texto Libre activado. Edita el sketch directamente.</color>"
            : "<color=#AAFFFF>> Modo Bloques activado.</color>");
    }

    void ApplyModeVisibility()
    {
        // Elementos solo del modo bloques
        SetActiveIfNotNull(pinDropdown?.gameObject,    !_freeTextMode);
        SetActiveIfNotNull(modeDropdown?.gameObject,   !_freeTextMode);
        SetActiveIfNotNull(actionDropdown?.gameObject, !_freeTextMode);
        SetActiveIfNotNull(extraDropdown?.gameObject,  !_freeTextMode);
        SetActiveIfNotNull(blinkMsInput?.gameObject,   !_freeTextMode);
        SetActiveIfNotNull(txtCodePreview?.gameObject, !_freeTextMode);

        // Elemento solo del modo libre
        SetActiveIfNotNull(codeEditor?.gameObject, _freeTextMode);

        if (txtToggleLabel != null)
            txtToggleLabel.text = _freeTextMode ? "Modo Bloques" : "Codigo Libre";
    }

    static void SetActiveIfNotNull(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  Preview de código (solo Modo Bloques)
    // ─────────────────────────────────────────────
    void UpdateCodePreview()
    {
        if (txtCodePreview == null || _freeTextMode) return;

        string pin    = GetOption(pinDropdown, "13");
        string mode   = GetOption(modeDropdown, "OUTPUT");
        string action = GetOption(actionDropdown, "HIGH");
        bool   blink  = action == "BLINK" ||
                        (extraDropdown != null && GetOption(extraDropdown, "NONE") == "BLINK");

        int blinkMs = 500;
        if (blinkMsInput != null && int.TryParse(blinkMsInput.text, out int parsed)) blinkMs = parsed;

        string loopCode = blink
            ? $"  digitalWrite({pin}, HIGH);\n  delay({blinkMs});\n  digitalWrite({pin}, LOW);\n  delay({blinkMs});"
            : $"  digitalWrite({pin}, {action});";

        txtCodePreview.text =
            $"void setup() {{\n" +
            $"  pinMode({pin}, {mode});\n" +
            $"}}\n\n" +
            $"void loop() {{\n" +
            $"{loopCode}\n" +
            $"}}";
    }

    // ─────────────────────────────────────────────
    //  Compilar y enviar
    // ─────────────────────────────────────────────
    void CompileAndSend()
    {
        int  pinNum   = 13;
        bool isOutput = true;
        bool isHigh   = true;
        bool blink    = false;
        int  blinkMs  = 500;

        if (_freeTextMode)
        {
            // ── Ruta texto libre: parsear con ArduinoCodeParser ──────────
            if (codeEditor == null || string.IsNullOrWhiteSpace(codeEditor.text))
            {
                SetStatus("El editor está vacío.", Color.red);
                Log("<color=#FF4444>> ERROR: Escribe un sketch antes de compilar.</color>");
                return;
            }

            var data = ArduinoCodeParser.Parse(codeEditor.text);

            // Mostrar resultado del compilador en la consola del IDE
            bool isOk = data.log != null && data.log.StartsWith("OK");
            Log(isOk
                ? $"<color=#00FF7F>> {data.log}</color>"
                : $"<color=#FFAA00>> {data.log}</color>");

            if (!data.isValid) return;

            pinNum   = data.pinNumber;
            isOutput = data.mode  == PinMode.OUTPUT;
            isHigh   = data.state == PinState.HIGH;
            blink    = data.blink;
            blinkMs  = data.blinkMs;
        }
        else
        {
            // ── Ruta modo bloques ────────────────────────────────────────
            string pinStr  = GetOption(pinDropdown, "13");
            string mode    = GetOption(modeDropdown, "OUTPUT");
            string action  = GetOption(actionDropdown, "HIGH");
            blink   = action == "BLINK" ||
                      (extraDropdown != null && GetOption(extraDropdown, "NONE") == "BLINK");

            if (!int.TryParse(pinStr, out pinNum))
            {
                string digits = Regex.Replace(pinStr, @"[^\d]", "");
                int.TryParse(digits, out pinNum);
            }

            if (blinkMsInput != null && int.TryParse(blinkMsInput.text, out int parsed)) blinkMs = parsed;
            isOutput = mode == "OUTPUT";
            isHigh   = action == "HIGH";
        }

        // ── Enviar por el canal de red COMPARTIDO (GameSession) ──────────
        // GameSession la spawnea el Host y se replica al Explorador, así que su RPC cruza
        // entre escenas distintas. El bridge de escena solo existe en Explorador.unity y no
        // llega al Técnico, por eso se prioriza GameSession en el setup asimétrico.
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RPC_SubirCodigoArduino(pinNum, isOutput, isHigh, blink ? blinkMs : 0f, blink);
            SetStatus($"Codigo subido — Pin D{pinNum}.", Color.green);
            if (!_freeTextMode)
                Log($"<color=#00FF7F>> Upload OK — Pin D{pinNum}. No errors.</color>");
            return;
        }

        // ── Bridge directo (escena única / IntegratedDemo, sin GameSession) ──
        var nb = bridge != null ? bridge : FindAnyObjectByType<ArduinoNetworkBridge>();
        if (nb != null)
        {
            nb.RPC_SubirCodigoArduino(pinNum, isOutput, isHigh, blink ? blinkMs : 0f, blink);
            SetStatus($"Codigo subido — Pin D{pinNum}.", Color.green);
            if (!_freeTextMode)
                Log($"<color=#00FF7F>> Upload OK — Pin D{pinNum}. No errors.</color>");
            return;
        }

        // ── Fallback offline: aplicar directo al ArduinoCore local ───────
        var core = FindAnyObjectByType<ArduinoCore>();
        if (core != null)
        {
            core.RecibirCodigoDePC(pinNum, isOutput, isHigh, blinkMs, blink);
            SetStatus($"Codigo subido (offline) — Pin D{pinNum}.", Color.green);
            if (!_freeTextMode)
                Log($"<color=#00FF7F>> Upload OK (offline) — Pin D{pinNum}. No errors.</color>");
            return;
        }

        Debug.LogWarning("[ArduinoIDEUI] No hay ArduinoNetworkBridge ni ArduinoCore en escena.");
        SetStatus("Sin conexión con Arduino.", Color.red);
        Log("<color=#FF4444>> ERROR: Arduino no encontrado en escena.</color>");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    static string GetOption(TMP_Dropdown dd, string fallback)
    {
        if (dd == null || dd.options.Count == 0) return fallback;
        return dd.options[dd.value].text;
    }

    void SetStatus(string msg, Color c)
    {
        if (txtStatus != null)
        {
            txtStatus.text  = $"Estado: {msg}";
            txtStatus.color = c;
        }
        Debug.Log($"[ArduinoIDEUI] {msg}");
    }

    void Log(string richText)
    {
        if (txtConsoleOutput != null)
            txtConsoleOutput.text = richText;
    }
}
