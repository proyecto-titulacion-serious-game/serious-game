using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz de programación por bloques simplificada para el Técnico.
/// Genera un "código visual" y prepara los datos para enviarlos al VR.
/// </summary>
public class ArduinoIDEUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector — Dropdowns
    // ─────────────────────────────────────────────
    [Header("Dropdowns")]
    public TMP_Dropdown pinDropdown;      // Pin 13, Pin 2, etc.
    public TMP_Dropdown modeDropdown;     // OUTPUT, INPUT
    public TMP_Dropdown actionDropdown;   // HIGH, LOW, BLINK
    public TMP_Dropdown extraDropdown;    // NONE, BLINK (extra toggle)

    [Header("Controles")]
    public Button            btnUploadCode;
    public TMP_InputField    blinkMsInput;

    [Header("Textos")]
    public TMP_Text txtCodePreview;
    public TMP_Text txtStatus;
    public TMP_Text txtFileNameLabel;
    public TMP_Text txtConsoleOutput;

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
    public TMP_Text       txtFileName    { get => txtFileNameLabel;  set => txtFileNameLabel   = value; }
    public TMP_Text       txtConsole     { get => txtConsoleOutput;  set => txtConsoleOutput   = value; }

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Start()
    {
        if (pinDropdown    != null) pinDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (modeDropdown   != null) modeDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (actionDropdown != null) actionDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());
        if (extraDropdown  != null) extraDropdown.onValueChanged.AddListener(_ => UpdateCodePreview());

        if (btnUploadCode != null) btnUploadCode.onClick.AddListener(CompileAndSend);

        UpdateCodePreview();
        SetStatus("Esperando código...", Color.white);
    }

    // ─────────────────────────────────────────────
    //  Preview de código
    // ─────────────────────────────────────────────
    void UpdateCodePreview()
    {
        if (txtCodePreview == null) return;

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
        string mode   = GetOption(modeDropdown, "OUTPUT");
        string action = GetOption(actionDropdown, "HIGH");
        bool   blink  = action == "BLINK" ||
                        (extraDropdown != null && GetOption(extraDropdown, "NONE") == "BLINK");

        int blinkMs = 500;
        if (blinkMsInput != null && int.TryParse(blinkMsInput.text, out int parsed)) blinkMs = parsed;

        bool isOutput = mode == "OUTPUT";
        bool isHigh   = action == "HIGH";

        var nb = bridge != null ? bridge : FindAnyObjectByType<ArduinoNetworkBridge>();

        if (nb != null)
        {
            nb.RPC_SubirCodigoArduino(isOutput, isHigh, blink ? blinkMs : 0f, blink);
            SetStatus("Código subido con éxito.", Color.green);
            if (txtConsoleOutput != null)
                txtConsoleOutput.text = "<color=#00FF7F>> Upload OK — no errors.</color>";
        }
        else
        {
            Debug.LogError("[ArduinoIDEUI] No se encontró ArduinoNetworkBridge.");
            SetStatus("Error: Placa no conectada.", Color.red);
            if (txtConsoleOutput != null)
                txtConsoleOutput.text = "<color=#FF4444>> ERROR: bridge not found.</color>";
        }
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
        if (txtStatus == null) return;
        txtStatus.text  = $"Estado: {msg}";
        txtStatus.color = c;
    }
}
