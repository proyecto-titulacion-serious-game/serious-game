#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera Assets/Prefabs/TechnicianMonitorHUD.prefab.
/// Canvas WorldSpace 1920×1080 con estilo Dark Industrial / Cyberpunk.
///
/// Menú: Tools → TITA → Crear HUD Monitor Técnico (Reto 4)
///
/// Uso: arrastra el prefab como hijo de PC_Arduino > Monitor en la escena.
/// La escala default (1/1600) produce un canvas de ~1.2m × 0.675m en world-space.
/// Ajusta en el Inspector para que encaje con la malla del monitor.
/// </summary>
public static class TechnicianMonitorHUDBuilder
{
    // ─────────────────────────────────────────────────────────────
    //  Configuración
    // ─────────────────────────────────────────────────────────────
    const string PREFAB_PATH  = "Assets/Prefabs/TechnicianMonitorHUD.prefab";
    const float  CW           = 1920f;
    const float  CH           = 1080f;
    const float  CANVAS_SCALE = 1f / 1600f;   // ~1.2m × 0.675m en world space

    // ─────────────────────────────────────────────────────────────
    //  Paleta de colores
    // ─────────────────────────────────────────────────────────────
    static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

    static readonly Color BgBase     = C("#080C12");
    static readonly Color BgPanel    = C("#0D1117");
    static readonly Color BgTerminal = C("#050709");
    static readonly Color BgCard     = C("#111820");
    static readonly Color BgHeader   = C("#0A0F18");
    static readonly Color BgFooter   = C("#06090E");
    static readonly Color Accent     = C("#00FFB2");
    static readonly Color AccentBlue = C("#00B8FF");
    static readonly Color Border     = C("#1E2D3D");
    static readonly Color TextMuted  = C("#546E7A");
    static readonly Color TextCode   = C("#A6ACCD");
    static readonly Color TextPrimary= C("#E8EAF6");
    static readonly Color StateOk    = C("#00FF7F");
    static readonly Color StateOkBg  = C("#002B1A");
    static readonly Color StateErr   = C("#FF2D55");
    static readonly Color StateErrBg = C("#2B000D");
    static readonly Color BtnGreen   = C("#00E676");
    static readonly Color BtnGreenBg = C("#002B1A");

    // ─────────────────────────────────────────────────────────────
    //  Font (LiberationSans SDF — siempre disponible en el proyecto)
    // ─────────────────────────────────────────────────────────────
    static TMP_FontAsset _font;
    static TMP_FontAsset Font
    {
        get
        {
            if (_font != null) return _font;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _font;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Entry point
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Crear HUD Monitor Técnico (Reto 4)")]
    public static void Build()
    {
        _font = null; // reset cache

        // ── Canvas root ──────────────────────────────────────────
        var root   = NewGO("TechnicianMonitorHUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 2;
        root.AddComponent<GraphicRaycaster>();

        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta  = new Vector2(CW, CH);
        rootRT.localScale = Vector3.one * CANVAS_SCALE;

        var bgImg = root.AddComponent<Image>();
        bgImg.color         = BgBase;
        bgImg.raycastTarget = false;

        // ── Agregar scripts al root ──────────────────────────────
        var hud   = root.AddComponent<TechnicianHUDController>();
        var telem = root.AddComponent<TechnicianTelemetryUI>();
        var ide   = root.AddComponent<ArduinoIDEUI>();

        // ── Construir secciones ──────────────────────────────────
        BuildHeader(root, hud);
        BuildFooter(root);

        var main = NewGO("MainArea", root);
        var mainRT = main.AddComponent<RectTransform>();
        SetAnchors(mainRT, 0f, 0.06f, 1f, 0.96f);
        Zero(mainRT);

        // Divisor vertical entre paneles
        var divGO = NewImg("Divider_V", main, Border);
        var divRT = divGO.GetComponent<RectTransform>();
        SetAnchors(divRT, 0.55f, 0f, 0.55f, 1f);
        divRT.sizeDelta       = new Vector2(1f, 0f);
        divRT.anchoredPosition = Vector2.zero;

        BuildIDEPanel(main, ide);
        BuildTelemetryPanel(main, telem);
        BuildValidationOverlay(root, hud);

        // ── Guardar prefab ───────────────────────────────────────
        string dir = Path.GetDirectoryName(PREFAB_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("HUD Monitor Técnico",
            "Prefab creado en:\n" + PREFAB_PATH + "\n\n" +
            "Pasos:\n" +
            "1. Arrastra el prefab como hijo de PC_Arduino > Monitor.\n" +
            "2. Ajusta la escala del Canvas si el tamaño no encaja.\n" +
            "3. Asigna GameManager y ArduinoNetworkBridge en el Inspector.\n" +
            "4. Asigna CircuitSimulator en TechnicianTelemetryUI.",
            "OK");

        Debug.Log("[TechnicianMonitorHUDBuilder] Prefab guardado: " + PREFAB_PATH);
    }

    // ─────────────────────────────────────────────────────────────
    //  HEADER BAR (96%–100%)
    // ─────────────────────────────────────────────────────────────
    static void BuildHeader(GameObject root, TechnicianHUDController hud)
    {
        var hdr = NewImg("Header", root, BgHeader);
        SetAnchors(hdr.GetComponent<RectTransform>(), 0f, 0.96f, 1f, 1f);
        Zero(hdr.GetComponent<RectTransform>());
        AddBorderLine(hdr, "BorderBot", false, false, Border);

        // Logo
        var logo = NewTxt("Txt_Logo", hdr, ">>  TITA LAB v2.4", 18f, Accent, bold: true);
        PinLeft(logo.rectTransform, 20f);

        // Separadores visuales
        var sep1 = NewTxt("Sep1", hdr, "|", 14f, Border);
        PinLeft(sep1.rectTransform, 210f);

        // Reto
        var txtReto = NewTxt("Txt_Reto", hdr, "RETO 4 — ARDUINO BLINK", 13f, TextPrimary, bold: true);
        txtReto.fontStyle       |= FontStyles.UpperCase;
        txtReto.characterSpacing = 2f;
        PinLeft(txtReto.rectTransform, 232f);
        hud.txtReto = txtReto;

        // Timer (derecha)
        var txtTimer = NewTxt("Txt_Timer", hdr, "15:00", 20f, AccentBlue, bold: true);
        PinRight(txtTimer.rectTransform, 24f);
        hud.txtTimer = txtTimer;

        // Errores
        var txtErr = NewTxt("Txt_Errores", hdr, "ERR: 0", 12f, TextMuted);
        PinRight(txtErr.rectTransform, 130f);
        hud.txtErrores = txtErr;

        // Dot conectado
        var dotGO = NewImg("Dot_Conn", hdr, StateOk);
        var dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = dotRT.anchorMax = dotRT.pivot = new Vector2(1f, 0.5f);
        dotRT.anchoredPosition = new Vector2(-256f, 0f);
        dotRT.sizeDelta = new Vector2(8f, 8f);

        var txtConn = NewTxt("Txt_Conn", hdr, "CONECTADO", 11f, TextMuted);
        PinRight(txtConn.rectTransform, 270f);
    }

    // ─────────────────────────────────────────────────────────────
    //  FOOTER BAR (0%–6%)
    // ─────────────────────────────────────────────────────────────
    static void BuildFooter(GameObject root)
    {
        var foot = NewImg("Footer", root, BgFooter);
        SetAnchors(foot.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.06f);
        Zero(foot.GetComponent<RectTransform>());
        AddBorderLine(foot, "BorderTop", true, false, Border);

        var txt = NewTxt("Txt_Footer", foot,
            "[UDP: 127.0.0.1:7000]   [FPS: --]   [Fusion: HOST]   Sketch: READY",
            10f, TextMuted);
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        StretchFull(txt.rectTransform);
        txt.margin = new Vector4(16f, 0f, 16f, 0f);
    }

    // ─────────────────────────────────────────────────────────────
    //  IDE PANEL (izquierda 55%)
    // ─────────────────────────────────────────────────────────────
    static void BuildIDEPanel(GameObject parent, ArduinoIDEUI ide)
    {
        var panel = NewImg("Panel_IDE", parent, BgPanel);
        SetAnchors(panel.GetComponent<RectTransform>(), 0f, 0f, 0.55f, 1f);
        Zero(panel.GetComponent<RectTransform>());

        // ── Label bar (top 8%) ──
        var lblBar = NewImg("LabelBar", panel, BgPanel);
        SetAnchors(lblBar.GetComponent<RectTransform>(), 0f, 0.92f, 1f, 1f);
        Zero(lblBar.GetComponent<RectTransform>());
        AddBorderLine(lblBar, "Bot", false, false, Border);

        var icoFile = NewTxt("Ico_File", lblBar, "#", 14f, Accent);
        PinLeft(icoFile.rectTransform, 16f);

        var txtFile = NewTxt("Txt_FileName", lblBar, "sketch_reto4_blink.ino", 13f, TextCode);
        PinLeft(txtFile.rectTransform, 40f);
        ide.txtFileName = txtFile;

        // SAVED badge
        var badgeGO = NewImg("Badge_Saved", lblBar, StateOkBg);
        var badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = badgeRT.anchorMax = badgeRT.pivot = new Vector2(1f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(-12f, 0f);
        badgeRT.sizeDelta = new Vector2(80f, 24f);
        AddBorder4(badgeGO, new Color(StateOk.r, StateOk.g, StateOk.b, 0.5f), 1f);
        var txtSaved = NewTxt("Txt_Saved", badgeGO, "SAVED ●", 10f, StateOk);
        txtSaved.alignment = TextAlignmentOptions.Center;
        StretchFull(txtSaved.rectTransform);

        // ── Dropdown row (8%–20%) ──
        var ddRow = NewImg("DropdownRow", panel, BgPanel);
        SetAnchors(ddRow.GetComponent<RectTransform>(), 0f, 0.80f, 1f, 0.92f);
        Zero(ddRow.GetComponent<RectTransform>());
        AddBorderLine(ddRow, "Bot", false, false, Border);

        // PIN  MODO  ESTADO  EXTRA  [blinkMs]
        ide.dropdownPin   = MakeDropdown(ddRow, "PIN",    0.01f, 0.18f,
            new[]{"D2","D4","D9","D10","D13"}, 4);
        ide.dropdownMode  = MakeDropdown(ddRow, "MODO",   0.21f, 0.19f,
            new[]{"OUTPUT","INPUT","INPUT_PULLUP"}, 0);
        ide.dropdownState = MakeDropdown(ddRow, "ESTADO", 0.42f, 0.19f,
            new[]{"LOW","HIGH"}, 1);
        ide.dropdownExtra = MakeDropdown(ddRow, "EXTRA",  0.63f, 0.19f,
            new[]{"NONE","BLINK"}, 1);

        // BlinkMs input
        var msGO = NewImg("Input_BlinkMs_Bg", ddRow, BgCard);
        SetAnchors(msGO.GetComponent<RectTransform>(), 0.84f, 0.12f, 0.99f, 0.88f);
        Zero(msGO.GetComponent<RectTransform>());
        AddBorder4(msGO, Border, 1f);

        var msIF = msGO.AddComponent<TMP_InputField>();
        var msText = NewTxt("Text", msGO, "500", 12f, AccentBlue);
        msText.alignment = TextAlignmentOptions.Center;
        StretchFull(msText.rectTransform);
        msIF.textComponent = msText;
        msIF.text = "500";
        ide.inputBlinkMs = msIF;

        // ── Terminal (20%–82%) ──
        var term = NewImg("Terminal", panel, BgTerminal);
        SetAnchors(term.GetComponent<RectTransform>(), 0f, 0.18f, 1f, 0.80f);
        Zero(term.GetComponent<RectTransform>());
        AddBorderLine(term, "Top", true, false, new Color(Accent.r, Accent.g, Accent.b, 0.1f));

        // Gutter de números de línea
        var gutter = NewImg("Gutter", term, new Color(0.03f, 0.05f, 0.09f, 1f));
        SetAnchors(gutter.GetComponent<RectTransform>(), 0f, 0f, 0.055f, 1f);
        Zero(gutter.GetComponent<RectTransform>());
        AddBorderLine(gutter, "Right", false, true, Border);
        var lineNums = NewTxt("Txt_LineNums", gutter,
            "001\n002\n003\n004\n005\n006\n007\n008\n009\n010", 11f, TextMuted);
        lineNums.alignment = TextAlignmentOptions.TopRight;
        StretchFull(lineNums.rectTransform);
        lineNums.margin = new Vector4(2f, 8f, 4f, 8f);

        // Código preview
        const string defaultCode =
            "<color=#C792EA>void</color> <color=#82AAFF>setup</color>() {\n" +
            "  <color=#82AAFF>pinMode</color>(<color=#F07178>13</color>, <color=#F78C6C>OUTPUT</color>);\n" +
            "}\n\n" +
            "<color=#C792EA>void</color> <color=#82AAFF>loop</color>() {\n" +
            "  <color=#82AAFF>digitalWrite</color>(<color=#F07178>13</color>, <color=#F78C6C>HIGH</color>);\n" +
            "  <color=#82AAFF>delay</color>(<color=#F07178>500</color>);\n" +
            "  <color=#82AAFF>digitalWrite</color>(<color=#F07178>13</color>, <color=#F78C6C>LOW</color>);\n" +
            "  <color=#82AAFF>delay</color>(<color=#F07178>500</color>);\n" +
            "}";
        var codeTxt = NewTxt("Txt_CodePreview", term, defaultCode, 12f, TextCode);
        codeTxt.richText  = true;
        codeTxt.alignment = TextAlignmentOptions.TopLeft;
        SetAnchors(codeTxt.rectTransform, 0.06f, 0f, 1f, 1f);
        Zero(codeTxt.rectTransform);
        codeTxt.margin = new Vector4(10f, 8f, 8f, 8f);
        codeTxt.overflowMode = TextOverflowModes.ScrollRect;
        ide.txtCodePreview = codeTxt;

        // ── Console (82%–90%) ──
        var console = NewImg("Console", panel, new Color(0.03f, 0.05f, 0.07f, 1f));
        SetAnchors(console.GetComponent<RectTransform>(), 0f, 0.10f, 1f, 0.18f);
        Zero(console.GetComponent<RectTransform>());
        AddBorderLine(console, "Top", true, false, Border);

        var conTxt = NewTxt("Txt_Console", console,
            "<color=#00FF7F>> No errors found. Ready to upload.</color>", 11f, TextMuted);
        conTxt.richText   = true;
        conTxt.alignment  = TextAlignmentOptions.MidlineLeft;
        StretchFull(conTxt.rectTransform);
        conTxt.margin = new Vector4(16f, 0f, 8f, 0f);
        ide.txtConsole = conTxt;

        // ── Botón Compilar (bottom 10%) ──
        var btnRow = NewImg("ButtonRow", panel, BgPanel);
        SetAnchors(btnRow.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.10f);
        Zero(btnRow.GetComponent<RectTransform>());
        AddBorderLine(btnRow, "Top", true, false, Border);

        var btnGO = NewImg("Btn_Compilar", btnRow, BtnGreenBg);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = btnRT.pivot = new Vector2(1f, 0.5f);
        btnRT.anchoredPosition = new Vector2(-20f, 0f);
        btnRT.sizeDelta = new Vector2(300f, 52f);
        AddBorder4(btnGO, BtnGreen, 2f);

        // Acento izquierdo del botón
        var accentBar = NewImg("AccentLeft", btnGO, BtnGreen);
        var abRT = accentBar.GetComponent<RectTransform>();
        SetAnchors(abRT, 0f, 0f, 0f, 1f);
        abRT.sizeDelta = new Vector2(4f, 0f);
        abRT.anchoredPosition = Vector2.zero;

        var btnComp = btnGO.AddComponent<Button>();
        btnComp.targetGraphic = btnGO.GetComponent<Image>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor      = BtnGreenBg;
        cb.highlightedColor = new Color(0f, 0.22f, 0.13f, 1f);
        cb.pressedColor     = BtnGreen;
        cb.selectedColor    = BtnGreenBg;
        btnComp.colors = cb;

        var btnTxt = NewTxt("Txt_Btn", btnGO, "COMPILAR Y ENVIAR  >>", 13f, BtnGreen, bold: true);
        btnTxt.alignment  = TextAlignmentOptions.Center;
        btnTxt.fontStyle |= FontStyles.UpperCase;
        StretchFull(btnTxt.rectTransform);
        ide.btnCompilar = btnComp;
    }

    // ─────────────────────────────────────────────────────────────
    //  TELEMETRÍA PANEL (derecha 45%)
    // ─────────────────────────────────────────────────────────────
    static void BuildTelemetryPanel(GameObject parent, TechnicianTelemetryUI telem)
    {
        var panel = NewImg("Panel_Telemetria", parent, BgPanel);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.55f, 0f, 1f, 1f);
        Zero(panel.GetComponent<RectTransform>());

        // Header telemetría (top 8%)
        var hdr = NewImg("Header_Telem", panel, BgHeader);
        SetAnchors(hdr.GetComponent<RectTransform>(), 0f, 0.92f, 1f, 1f);
        Zero(hdr.GetComponent<RectTransform>());
        AddBorderLine(hdr, "Bot", false, false, new Color(Accent.r, Accent.g, Accent.b, 0.15f));

        var txtHdr = NewTxt("Txt_HdrTelem", hdr, "~  TELEMETRIA EN TIEMPO REAL", 12f, Accent, bold: true);
        txtHdr.fontStyle       |= FontStyles.UpperCase;
        txtHdr.characterSpacing = 1.5f;
        PinLeft(txtHdr.rectTransform, 16f);

        // Cards area (20%–92%) → 2×2 grid
        var cardsArea = NewGO("CardsArea", panel);
        var caRT = cardsArea.AddComponent<RectTransform>();
        SetAnchors(caRT, 0f, 0.20f, 1f, 0.92f);
        Zero(caRT);

        var (cardV,   valV,   _)   = MakeTelemCard(cardsArea, "Card_Voltaje",   "VOLTAJE",   "V",     AccentBlue);
        var (cardI,   valI,   _)   = MakeTelemCard(cardsArea, "Card_Corriente", "CORRIENTE", "mA",    AccentBlue);
        var (cardW,   valW,   _)   = MakeTelemCard(cardsArea, "Card_Potencia",  "POTENCIA",  "mW",    Accent);
        var (cardADC, valADC, _)   = MakeTelemCard(cardsArea, "Card_ADC",       "ADC  A0",   "/ 1023",Accent);

        // 2×2 layout: top row arriba, bottom row abajo, separador en medio
        const float gap = 0.01f;
        SetAnchors(cardV.GetComponent<RectTransform>(),   0f,          0.51f + gap, 0.5f - gap, 1f);
        SetAnchors(cardI.GetComponent<RectTransform>(),   0.5f + gap,  0.51f + gap, 1f,         1f);
        SetAnchors(cardW.GetComponent<RectTransform>(),   0f,          0f,          0.5f - gap, 0.49f - gap);
        SetAnchors(cardADC.GetComponent<RectTransform>(), 0.5f + gap,  0f,          1f,         0.49f - gap);
        Zero(cardV.GetComponent<RectTransform>());
        Zero(cardI.GetComponent<RectTransform>());
        Zero(cardW.GetComponent<RectTransform>());
        Zero(cardADC.GetComponent<RectTransform>());

        telem.txtValorVoltaje   = valV;
        telem.txtValorCorriente = valI;
        telem.txtValorPotencia  = valW;
        telem.txtValorAdc       = valADC;
        telem.imgCardVoltaje    = cardV.GetComponent<Image>();
        telem.imgCardCorriente  = cardI.GetComponent<Image>();

        // Alert banner (bottom 20%)
        var banner = NewImg("Banner_Estado", panel, StateOkBg);
        SetAnchors(banner.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.20f);
        Zero(banner.GetComponent<RectTransform>());
        AddBorderLine(banner, "Top", true, false, new Color(StateOk.r, StateOk.g, StateOk.b, 0.25f));

        // Stripe izquierda del banner
        var stripe = NewImg("Banner_Stripe", banner, StateOk);
        var strRT = stripe.GetComponent<RectTransform>();
        SetAnchors(strRT, 0f, 0f, 0f, 1f);
        strRT.sizeDelta = new Vector2(5f, 0f);
        strRT.anchoredPosition = Vector2.zero;
        telem.bannerStripe = stripe.GetComponent<Image>();

        var txtAlert = NewTxt("Txt_Alerta", banner,
            "OK  ESTADO: OPERACION SEGURA", 14f, StateOk, bold: true);
        txtAlert.fontStyle  |= FontStyles.UpperCase;
        txtAlert.alignment   = TextAlignmentOptions.Center;
        StretchFull(txtAlert.rectTransform);
        telem.lblAlerta   = txtAlert;
        telem.panelAlerta = banner.GetComponent<Image>();
    }

    // ─────────────────────────────────────────────────────────────
    //  VALIDATION OVERLAY (modal flotante)
    // ─────────────────────────────────────────────────────────────
    static void BuildValidationOverlay(GameObject root, TechnicianHUDController hud)
    {
        // Dim backdrop
        var overlay = NewImg("Panel_Validacion", root, new Color(0f, 0f, 0f, 0.65f));
        StretchFull(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().raycastTarget = true;
        overlay.SetActive(false);
        hud.panelValidacion = overlay;

        // Modal card  60% × 38%  centrado
        var modal = NewImg("Modal", overlay, BgPanel);
        SetAnchors(modal.GetComponent<RectTransform>(), 0.20f, 0.31f, 0.80f, 0.69f);
        Zero(modal.GetComponent<RectTransform>());
        AddBorder4(modal, AccentBlue, 2f);
        hud.imgValidacionBg = modal.GetComponent<Image>();

        // Accent top line
        var topLine = NewImg("TopLine", modal, AccentBlue);
        var tlRT = topLine.GetComponent<RectTransform>();
        SetAnchors(tlRT, 0f, 1f, 1f, 1f);
        tlRT.sizeDelta = new Vector2(0f, 2f);
        tlRT.anchoredPosition = Vector2.zero;

        // Título / estado
        var txtEstado = NewTxt("Txt_ValidacionEstado", modal,
            "⏳  EVALUANDO CIRCUITO...", 22f, AccentBlue, bold: true);
        txtEstado.alignment = TextAlignmentOptions.Center;
        SetAnchors(txtEstado.rectTransform, 0f, 0.60f, 1f, 0.92f);
        Zero(txtEstado.rectTransform);
        hud.txtValidacionEstado = txtEstado;

        // Progress bar background
        var progBg = NewImg("ProgressBg", modal, Border);
        SetAnchors(progBg.GetComponent<RectTransform>(), 0.05f, 0.50f, 0.95f, 0.60f);
        Zero(progBg.GetComponent<RectTransform>());

        var progFill = NewImg("ProgressFill", progBg, AccentBlue);
        SetAnchors(progFill.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f); // starts empty
        Zero(progFill.GetComponent<RectTransform>());
        hud.progressFill = progFill.GetComponent<Image>();

        // Checklist panel
        var chkPanel = NewGO("Panel_Checklist", modal);
        var chkRT = chkPanel.AddComponent<RectTransform>();
        SetAnchors(chkRT, 0.04f, 0.12f, 0.96f, 0.50f);
        Zero(chkRT);
        hud.panelChecklist = chkPanel;

        var checkLabels = new[]
        {
            "⏳  Verificando continuidad...",
            "⏳  Midiendo corriente...",
            "⏳  Validando pin Arduino..."
        };
        float[] yMins = { 0.66f, 0.33f, 0.00f };
        float[] yMaxs = { 1.00f, 0.66f, 0.33f };

        var chkTexts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            var item = NewTxt($"Chk_{i + 1}", chkPanel, checkLabels[i], 12f, TextMuted);
            item.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(item.rectTransform, 0f, yMins[i], 1f, yMaxs[i]);
            Zero(item.rectTransform);
            item.margin = new Vector4(12f, 0f, 4f, 0f);
            chkTexts[i] = item;
        }
        hud.txtCheck1 = chkTexts[0];
        hud.txtCheck2 = chkTexts[1];
        hud.txtCheck3 = chkTexts[2];

        // Botón Cerrar (oculto hasta que hay resultado)
        var btnCloseGO = NewImg("Btn_Cerrar", modal, StateErrBg);
        AddBorder4(btnCloseGO, StateErr, 1f);
        SetAnchors(btnCloseGO.GetComponent<RectTransform>(), 0.55f, 0.06f, 0.75f, 0.18f);
        Zero(btnCloseGO.GetComponent<RectTransform>());
        var btnCloseTxt = NewTxt("Txt_Cerrar", btnCloseGO, "✕  CERRAR", 11f, StateErr, bold: true);
        btnCloseTxt.alignment = TextAlignmentOptions.Center;
        StretchFull(btnCloseTxt.rectTransform);
        var btnClose = btnCloseGO.AddComponent<Button>();
        btnClose.targetGraphic = btnCloseGO.GetComponent<Image>();
        btnCloseGO.SetActive(false);
        hud.btnCerrarValidacion = btnClose;
    }

    // ─────────────────────────────────────────────────────────────
    //  TMP_Dropdown con template funcional
    // ─────────────────────────────────────────────────────────────
    static TMP_Dropdown MakeDropdown(GameObject parent, string label,
        float anchorXMin, float width, string[] options, int defaultVal = 0)
    {
        // Container con label encima
        var container = NewImg($"Container_{label}", parent, BgCard);
        var cRT = container.GetComponent<RectTransform>();
        SetAnchors(cRT, anchorXMin, 0.08f, anchorXMin + width, 0.92f);
        Zero(cRT);
        AddBorder4(container, Border, 1f);

        // Label
        var lbl = NewTxt("Lbl", container, label, 9f, TextMuted);
        lbl.fontStyle        |= FontStyles.UpperCase;
        lbl.characterSpacing  = 2f;
        lbl.alignment         = TextAlignmentOptions.Top;
        SetAnchors(lbl.rectTransform, 0f, 0.62f, 1f, 1f);
        Zero(lbl.rectTransform);
        lbl.margin = new Vector4(2f, 4f, 2f, 0f);

        // Dropdown GO
        var ddGO = NewGO("Dropdown", container);
        ddGO.AddComponent<RectTransform>();
        var ddBg = ddGO.AddComponent<Image>();
        ddBg.color = BgPanel;
        SetAnchors(ddGO.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.62f);
        Zero(ddGO.GetComponent<RectTransform>());

        var dd = ddGO.AddComponent<TMP_Dropdown>();

        // Caption text
        var captionGO = NewGO("Label", ddGO);
        captionGO.AddComponent<RectTransform>();
        var capTxt = captionGO.AddComponent<TextMeshProUGUI>();
        capTxt.font      = Font;
        capTxt.fontSize  = 13f;
        capTxt.color     = AccentBlue;
        capTxt.fontStyle = FontStyles.Bold;
        capTxt.alignment = TextAlignmentOptions.Center;
        capTxt.raycastTarget = false;
        var capRT = captionGO.GetComponent<RectTransform>();
        SetAnchors(capRT, 0f, 0f, 1f, 1f);
        capRT.offsetMin = new Vector2(6f, 4f);
        capRT.offsetMax = new Vector2(-24f, -4f);
        dd.captionText = capTxt;

        // Arrow
        var arrowGO = NewImg("Arrow", ddGO, Accent);
        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = arrowRT.anchorMax = arrowRT.pivot = new Vector2(1f, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-6f, 0f);
        arrowRT.sizeDelta = new Vector2(12f, 8f);

        // ── Template (inactive) ──────────────────────────────────
        var templateGO = NewImg("Template", ddGO, BgCard);
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.anchorMin        = new Vector2(0f, 0f);
        templateRT.anchorMax        = new Vector2(1f, 0f);
        templateRT.pivot            = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0f, 2f);
        templateRT.sizeDelta        = new Vector2(0f, 120f);
        templateGO.SetActive(false);

        var sr = templateGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.scrollSensitivity = 10f;

        // Viewport
        var viewGO = NewGO("Viewport", templateGO);
        var viewRT = viewGO.AddComponent<RectTransform>();
        SetAnchors(viewRT, 0f, 0f, 1f, 1f);
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;
        var viewMask = viewGO.AddComponent<Mask>();
        viewMask.showMaskGraphic = false;
        var viewImg = viewGO.AddComponent<Image>();
        viewImg.color = Color.white;
        sr.viewport = viewRT;

        // Content
        var contentGO = NewGO("Content", viewGO);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = new Vector2(0f, 0f);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRT;

        // Item template
        var itemGO = NewGO("Item", contentGO);
        var itemRT = itemGO.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0f, 0.5f);
        itemRT.anchorMax = new Vector2(1f, 0.5f);
        itemRT.sizeDelta = new Vector2(0f, 28f);
        var le = itemGO.AddComponent<LayoutElement>();
        le.minHeight       = 28f;
        le.preferredHeight = 28f;
        var toggle = itemGO.AddComponent<Toggle>();

        var itemBgGO = NewImg("Item Background", itemGO, BgCard);
        StretchFull(itemBgGO.GetComponent<RectTransform>());
        toggle.targetGraphic = itemBgGO.GetComponent<Image>();
        var tcb = toggle.colors;
        tcb.normalColor      = BgCard;
        tcb.highlightedColor = Border;
        tcb.selectedColor    = new Color(AccentBlue.r, AccentBlue.g, AccentBlue.b, 0.25f);
        toggle.colors = tcb;

        var chkGO = NewImg("Item Checkmark", itemGO, Accent);
        var chkRT = chkGO.GetComponent<RectTransform>();
        chkRT.anchorMin = chkRT.anchorMax = chkRT.pivot = new Vector2(0f, 0.5f);
        chkRT.anchoredPosition = new Vector2(8f, 0f);
        chkRT.sizeDelta = new Vector2(10f, 10f);
        toggle.graphic = chkGO.GetComponent<Image>();

        var itemLblGO = NewGO("Item Label", itemGO);
        var itemLblRT = itemLblGO.AddComponent<RectTransform>();
        SetAnchors(itemLblRT, 0f, 0f, 1f, 1f);
        itemLblRT.offsetMin = new Vector2(24f, 2f);
        itemLblRT.offsetMax = new Vector2(-4f, -2f);
        var itemTxt = itemLblGO.AddComponent<TextMeshProUGUI>();
        itemTxt.font      = Font;
        itemTxt.fontSize  = 13f;
        itemTxt.color     = TextCode;
        itemTxt.alignment = TextAlignmentOptions.MidlineLeft;
        itemTxt.raycastTarget = false;
        dd.itemText = itemTxt;

        // Wire template
        dd.template = templateRT;

        // Add options
        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>();
        foreach (var o in options) opts.Add(new TMP_Dropdown.OptionData(o));
        dd.AddOptions(opts);
        dd.value = Mathf.Clamp(defaultVal, 0, options.Length - 1);

        return dd;
    }

    // ─────────────────────────────────────────────────────────────
    //  Tarjeta de telemetría
    // ─────────────────────────────────────────────────────────────
    static (GameObject card, TMP_Text value, TMP_Text unit) MakeTelemCard(
        GameObject parent, string name, string label, string unit, Color accentColor)
    {
        var card = NewImg(name, parent, BgCard);

        // Accent top border (2px)
        var topLine = NewImg("TopLine", card, accentColor);
        var tlRT = topLine.GetComponent<RectTransform>();
        SetAnchors(tlRT, 0f, 1f, 1f, 1f);
        tlRT.sizeDelta = new Vector2(0f, 2f);
        tlRT.anchoredPosition = Vector2.zero;

        // Live dot
        var dot = NewImg("Dot_Live", card, Accent);
        var dotRT = dot.GetComponent<RectTransform>();
        dotRT.anchorMin = dotRT.anchorMax = dotRT.pivot = new Vector2(1f, 1f);
        dotRT.anchoredPosition = new Vector2(-10f, -10f);
        dotRT.sizeDelta = new Vector2(8f, 8f);

        // Label
        var lblTxt = NewTxt("Txt_Label", card, label, 10f, TextMuted);
        lblTxt.fontStyle        |= FontStyles.UpperCase;
        lblTxt.characterSpacing  = 1.5f;
        lblTxt.alignment         = TextAlignmentOptions.TopLeft;
        SetAnchors(lblTxt.rectTransform, 0f, 0.72f, 0.85f, 1f);
        Zero(lblTxt.rectTransform);
        lblTxt.margin = new Vector4(12f, 6f, 4f, 0f);

        // Valor grande
        var valTxt = NewTxt("Txt_Value", card, "0.00", 40f, accentColor, bold: true);
        valTxt.alignment = TextAlignmentOptions.Center;
        SetAnchors(valTxt.rectTransform, 0f, 0.32f, 1f, 0.75f);
        Zero(valTxt.rectTransform);

        // Unidad
        var unitTxt = NewTxt("Txt_Unit", card, unit, 14f, TextMuted);
        unitTxt.alignment = TextAlignmentOptions.Center;
        SetAnchors(unitTxt.rectTransform, 0f, 0.10f, 1f, 0.34f);
        Zero(unitTxt.rectTransform);

        // Min/Max
        var mmTxt = NewTxt("Txt_MinMax", card, "MIN: 0.0  |  MAX: 0.0", 9f,
            new Color(0.16f, 0.25f, 0.31f, 1f));
        mmTxt.alignment = TextAlignmentOptions.Bottom;
        SetAnchors(mmTxt.rectTransform, 0f, 0f, 1f, 0.14f);
        Zero(mmTxt.rectTransform);
        mmTxt.margin = new Vector4(4f, 0f, 4f, 4f);

        return (card, valTxt, unitTxt);
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers de construcción
    // ─────────────────────────────────────────────────────────────
    static GameObject NewGO(string name, GameObject parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject NewImg(string name, GameObject parent, Color color)
    {
        var go = NewGO(name, parent);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static TextMeshProUGUI NewTxt(string name, GameObject parent, string text,
        float size, Color color, bool bold = false)
    {
        var go = NewGO(name, parent);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font      = Font;
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.overflowMode   = TextOverflowModes.Overflow;
        t.raycastTarget  = false;
        return t;
    }

    // ── RectTransform helpers ─────────────────────────────────────
    static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
    }

    static void Zero(RectTransform rt)
    {
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
    }

    // Pin a la izquierda (pivot y anchor izquierdo-centro)
    static void PinLeft(RectTransform rt, float x)
    {
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(0f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    // Pin a la derecha (pivot y anchor derecho-centro)
    static void PinRight(RectTransform rt, float xFromRight)
    {
        rt.anchorMin        = new Vector2(1f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-xFromRight, 0f);
    }

    // Línea de borde horizontal (top o bottom) de 1px
    static void AddBorderLine(GameObject parent, string childName,
        bool isTop, bool isRight, Color color)
    {
        var go = NewImg(childName, parent, color);
        var rt = go.GetComponent<RectTransform>();
        if (!isRight)
        {
            // Horizontal
            SetAnchors(rt, 0f, isTop ? 1f : 0f, 1f, isTop ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 1f);
        }
        else
        {
            // Vertical right
            SetAnchors(rt, 1f, 0f, 1f, 1f);
            rt.sizeDelta = new Vector2(1f, 0f);
        }
        rt.anchoredPosition = Vector2.zero;
    }

    // Marco de 4 bordes
    static void AddBorder4(GameObject parent, Color color, float thickness)
    {
        void Line(string n, float ax0, float ay0, float ax1, float ay1, float sw, float sh)
        {
            var go = NewImg(n, parent, color);
            var rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, ax0, ay0, ax1, ay1);
            rt.sizeDelta        = new Vector2(sw, sh);
            rt.anchoredPosition = Vector2.zero;
        }
        Line("_BT", 0f, 1f, 1f, 1f,  0f, thickness);  // top
        Line("_BB", 0f, 0f, 1f, 0f,  0f, thickness);  // bottom
        Line("_BL", 0f, 0f, 0f, 1f,  thickness, 0f);  // left
        Line("_BR", 1f, 0f, 1f, 1f,  thickness, 0f);  // right
    }
}
#endif
