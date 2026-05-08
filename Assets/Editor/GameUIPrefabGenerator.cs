using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera los dos prefabs de UI del juego:
///
///   Assets/Prefabs/ExplorerHUD.prefab
///     Canvas WorldSpace pegado a Main Camera (VR).
///     Contiene MultimeterUI y PlayerFeedbackUI con todas las referencias internas cableadas.
///
///   Assets/Prefabs/TechnicianHUD.prefab
///     Canvas ScreenSpaceOverlay para el monitor del Técnico.
///     Contiene UIButtonController y TechnicianHUDController con las referencias internas cableadas.
///
/// Menú: Tools → TITA → Generar Prefabs de UI
///
/// DESPUÉS DE GENERAR:
///   ExplorerHUD  → arrastrar como hijo de Main Camera del XR Origin.
///   TechnicianHUD → arrastrar a la escena del Técnico (cualquier posición).
///   Asignar manualmente: GameManager, InstructionSystem, Multimeter, ComponentDeliverySystem.
/// </summary>
public static class GameUIPrefabGenerator
{
    private const string EXPLORER_HUD_PATH    = "Assets/Prefabs/ExplorerHUD.prefab";
    private const string TECHNICIAN_HUD_PATH  = "Assets/Prefabs/TechnicianHUD.prefab";

    [MenuItem("Tools/TITA/Generar Prefabs de UI")]
    public static void Generate()
    {
        GenerateExplorerHUD();
        GenerateTechnicianHUD();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Prefabs de UI generados",
            "Se crearon dos prefabs:\n\n" +
            "  ExplorerHUD.prefab\n" +
            "  TechnicianHUD.prefab\n\n" +
            "PASOS SIGUIENTES:\n" +
            "1. Arrastra ExplorerHUD como hijo de Main Camera.\n" +
            "2. Arrastra TechnicianHUD a la escena del Técnico.\n" +
            "3. Asigna en el Inspector:\n" +
            "   MultimeterUI     → multimeter\n" +
            "   PlayerFeedbackUI → gameManager, instructionSystem,\n" +
            "                      multimeter, delivery\n" +
            "   UIButtonController → gameManager, instructionSystem,\n" +
            "                        technicianActions\n" +
            "   TechnicianHUDController → gameManager",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExplorerHUD — World Space Canvas (VR, hijo de Main Camera)
    // ─────────────────────────────────────────────────────────────────────────

    static void GenerateExplorerHUD()
    {
        bool overwrite = ConfirmOverwrite(EXPLORER_HUD_PATH, "ExplorerHUD");
        if (!overwrite) return;

        // ── Raíz ────────────────────────────────────────────────────────────
        var root = new GameObject("ExplorerHUD");

        var canvas        = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>();

        var rt         = root.GetComponent<RectTransform>();
        rt.sizeDelta   = new Vector2(280f, 170f);
        rt.localPosition = new Vector3(0f, -0.05f, 0.5f);
        rt.localScale    = Vector3.one * 0.001f;

        // ── Panel_Multimetro (mitad superior) ────────────────────────────────
        var panelMult = CreatePanel(root, "Panel_Multimetro",
            new Vector2(0f, 0.55f), new Vector2(1f, 1f),
            new Vector2(0f, -10f), new Vector2(0f, 0f),
            new Color(0.04f, 0.07f, 0.16f, 0.9f));

        var txtVoltaje  = CreateTMP(panelMult, "TMP_Voltaje",
            "—.— V", 22f, Color.green,
            new Vector2(0f, 10f), new Vector2(260f, 30f),
            TextAlignmentOptions.Center);

        var txtEstado   = CreateTMP(panelMult, "TMP_Estado",
            "Conecta ambas puntas", 7f, new Color(0.7f, 0.9f, 0.7f),
            new Vector2(0f, -10f), new Vector2(260f, 14f),
            TextAlignmentOptions.Center);

        var txtProbeRoja  = CreateTMP(panelMult, "TMP_ProbeRoja",
            "🔴 —", 6f, new Color(1f, 0.5f, 0.5f),
            new Vector2(-65f, -22f), new Vector2(120f, 12f),
            TextAlignmentOptions.Left);

        var txtProbeNegra = CreateTMP(panelMult, "TMP_ProbeNegra",
            "⚫ —", 6f, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(65f, -22f), new Vector2(120f, 12f),
            TextAlignmentOptions.Right);

        // Imagen de fondo del voltaje (cambia color con estado)
        var fondoVoltaje = panelMult.GetComponent<Image>();

        // ── MultimeterUI ─────────────────────────────────────────────────────
        var multimeterUI         = panelMult.AddComponent<MultimeterUI>();
        multimeterUI.txtVoltaje  = txtVoltaje;
        multimeterUI.txtEstado   = txtEstado;
        multimeterUI.txtProbeRoja  = txtProbeRoja;
        multimeterUI.txtProbeNegra = txtProbeNegra;
        multimeterUI.fondoVoltaje  = fondoVoltaje;
        // multimeter se asigna manualmente (depende de la escena)

        // ── Panel_Instruccion (mitad inferior) ───────────────────────────────
        var panelInstr = CreatePanel(root, "Panel_Instruccion",
            new Vector2(0f, 0f), new Vector2(1f, 0.52f),
            new Vector2(0f, 8f), new Vector2(0f, 0f),
            new Color(0.05f, 0.05f, 0.15f, 0.88f));

        var fondoPanel = panelInstr.GetComponent<Image>();

        var txtInstruccion = CreateTMP(panelInstr, "TMP_Instruccion",
            "Instrucción principal", 8f, Color.white,
            new Vector2(0f, 22f), new Vector2(258f, 36f),
            TextAlignmentOptions.Center);
        txtInstruccion.enableWordWrapping = true;

        var txtSubInstruccion = CreateTMP(panelInstr, "TMP_SubInstruccion",
            "Detalle adicional", 6.5f, new Color(0.85f, 0.85f, 0.6f),
            new Vector2(0f, 3f), new Vector2(258f, 26f),
            TextAlignmentOptions.Center);
        txtSubInstruccion.enableWordWrapping = true;

        var txtPaso = CreateTMP(panelInstr, "TMP_Paso",
            "Paso 1 de 4", 6f, new Color(0.5f, 0.8f, 1f),
            new Vector2(-80f, -16f), new Vector2(90f, 12f),
            TextAlignmentOptions.Left);

        // Barra de progreso
        var barraGO = new GameObject("Barra_Progreso");
        barraGO.transform.SetParent(panelInstr.transform, false);
        var barraRT            = barraGO.AddComponent<RectTransform>();
        barraRT.anchorMin      = new Vector2(0.5f, 0f);
        barraRT.anchorMax      = new Vector2(0.5f, 0f);
        barraRT.pivot          = new Vector2(0.5f, 0f);
        barraRT.anchoredPosition = new Vector2(0f, 6f);
        barraRT.sizeDelta      = new Vector2(200f, 6f);
        var barraImg           = barraGO.AddComponent<Image>();
        barraImg.color         = new Color(0.2f, 0.8f, 0.4f);
        barraImg.type          = Image.Type.Filled;
        barraImg.fillMethod    = Image.FillMethod.Horizontal;
        barraImg.fillOrigin    = 0;
        barraImg.fillAmount    = 0f;

        // Fondo de la barra
        var barraFondoGO = new GameObject("Barra_Fondo");
        barraFondoGO.transform.SetParent(panelInstr.transform, false);
        var barraFondoRT        = barraFondoGO.AddComponent<RectTransform>();
        barraFondoRT.anchorMin  = new Vector2(0.5f, 0f);
        barraFondoRT.anchorMax  = new Vector2(0.5f, 0f);
        barraFondoRT.pivot      = new Vector2(0.5f, 0f);
        barraFondoRT.anchoredPosition = new Vector2(0f, 6f);
        barraFondoRT.sizeDelta  = new Vector2(200f, 6f);
        var barraFondoImg       = barraFondoGO.AddComponent<Image>();
        barraFondoImg.color     = new Color(0.15f, 0.15f, 0.25f);
        barraFondoGO.transform.SetSiblingIndex(barraGO.transform.GetSiblingIndex() - 1);

        var txtProgreso = CreateTMP(panelInstr, "TMP_Progreso",
            "0%", 6f, new Color(0.5f, 0.8f, 1f),
            new Vector2(60f, -16f), new Vector2(50f, 12f),
            TextAlignmentOptions.Right);

        // ── Panel_Notificacion (inactivo al inicio) ──────────────────────────
        var panelNotif = new GameObject("Panel_Notificacion");
        panelNotif.transform.SetParent(panelInstr.transform, false);
        var notifRT        = panelNotif.AddComponent<RectTransform>();
        notifRT.anchorMin  = Vector2.zero;
        notifRT.anchorMax  = Vector2.one;
        notifRT.offsetMin  = Vector2.zero;
        notifRT.offsetMax  = Vector2.zero;
        var notifImg       = panelNotif.AddComponent<Image>();
        notifImg.color     = new Color(0.1f, 0.3f, 0.1f, 0.95f);

        var txtNotificacion = CreateTMP(panelNotif, "TMP_Notificacion",
            "¡El Técnico te envió un componente!\nAgárralo con el Grip derecho.", 8f,
            new Color(0.3f, 1f, 0.5f),
            new Vector2(20f, 0f), new Vector2(200f, 70f),
            TextAlignmentOptions.Center);
        txtNotificacion.enableWordWrapping = true;

        var iconoGO = new GameObject("Image_Icono");
        iconoGO.transform.SetParent(panelNotif.transform, false);
        var iconoRT        = iconoGO.AddComponent<RectTransform>();
        iconoRT.anchoredPosition = new Vector2(-90f, 0f);
        iconoRT.sizeDelta        = new Vector2(40f, 40f);
        var imgIcono = iconoGO.AddComponent<Image>();
        imgIcono.color = Color.white;

        panelNotif.SetActive(false);

        // ── PlayerFeedbackUI ─────────────────────────────────────────────────
        var feedbackUI                  = panelInstr.AddComponent<PlayerFeedbackUI>();
        feedbackUI.txtInstruccion       = txtInstruccion;
        feedbackUI.txtSubInstruccion    = txtSubInstruccion;
        feedbackUI.txtPaso              = txtPaso;
        feedbackUI.barraProgreso        = barraImg;
        feedbackUI.txtProgresoPorcentaje = txtProgreso;
        feedbackUI.panelNotificacion    = panelNotif;
        feedbackUI.txtNotificacion      = txtNotificacion;
        feedbackUI.imgIconoComponente   = imgIcono;
        feedbackUI.fondoPanel           = fondoPanel;
        // gameManager, instructionSystem, multimeter, delivery → asignar manualmente

        // ── Guardar prefab ───────────────────────────────────────────────────
        SavePrefab(root, EXPLORER_HUD_PATH, "ExplorerHUD");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TechnicianHUD — Screen Space Overlay (monitor del Técnico)
    // ─────────────────────────────────────────────────────────────────────────

    static void GenerateTechnicianHUD()
    {
        bool overwrite = ConfirmOverwrite(TECHNICIAN_HUD_PATH, "TechnicianHUD");
        if (!overwrite) return;

        // ── Raíz ────────────────────────────────────────────────────────────
        var root   = new GameObject("TechnicianHUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var hudCtrl = root.AddComponent<TechnicianHUDController>();

        // ── Panel_Info — franja superior izquierda ───────────────────────────
        var panelInfo = CreatePanel(root, "Panel_Info",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(165f, -40f), new Vector2(330f, 80f),
            new Color(0.04f, 0.06f, 0.14f, 0.9f));

        var txtReto = CreateTMP(panelInfo, "TMP_Reto",
            "RETO 1 — Ley de Ohm", 11f, new Color(0.4f, 0.8f, 1f),
            new Vector2(0f, 18f), new Vector2(310f, 22f),
            TextAlignmentOptions.Left);

        var txtTimer = CreateTMP(panelInfo, "TMP_Timer",
            "8:00", 10f, Color.white,
            new Vector2(-50f, -8f), new Vector2(100f, 20f),
            TextAlignmentOptions.Left);

        var txtErrores = CreateTMP(panelInfo, "TMP_Errores",
            "Errores: 0", 9f, new Color(1f, 0.7f, 0.4f),
            new Vector2(80f, -8f), new Vector2(130f, 20f),
            TextAlignmentOptions.Left);

        hudCtrl.txtReto    = txtReto;
        hudCtrl.txtTimer   = txtTimer;
        hudCtrl.txtErrores = txtErrores;

        // ── Panel_Botones — esquina inferior derecha ─────────────────────────
        var panelBotones = CreatePanel(root, "Panel_Botones",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-158f, 75f), new Vector2(316f, 150f),
            new Color(0.04f, 0.06f, 0.14f, 0.88f));

        var btnFixRes  = CreateButton(panelBotones, "Button_FixResistor",
            new Vector2(0f, 52f), new Vector2(290f, 40f),
            "ENVIAR RESISTENCIA", new Color(0.15f, 0.4f, 0.15f),
            out var lblFixRes);

        var btnFixPar  = CreateButton(panelBotones, "Button_FixParallel",
            new Vector2(0f, 6f), new Vector2(290f, 40f),
            "REPARAR PARALELO", new Color(0.15f, 0.25f, 0.45f),
            out var lblFixPar);

        var btnNext    = CreateButton(panelBotones, "Button_NextStep",
            new Vector2(0f, -40f), new Vector2(290f, 40f),
            "Siguiente paso (automático)", new Color(0.2f, 0.2f, 0.2f),
            out var lblNext);
        btnNext.interactable = false;

        // ── UIButtonController ────────────────────────────────────────────────
        var btnCtrl                = panelBotones.AddComponent<UIButtonController>();
        btnCtrl.fixResistorButton  = btnFixRes;
        btnCtrl.fixParallelButton  = btnFixPar;
        btnCtrl.nextStepButton     = btnNext;
        btnCtrl.fixResistorLabel   = lblFixRes;
        btnCtrl.fixParallelLabel   = lblFixPar;
        // gameManager, instructionSystem, technicianActions → asignar manualmente

        // ── Panel_Transicion — overlay de transición de zona ─────────────────
        var panelTrans = new GameObject("Panel_Transicion");
        panelTrans.transform.SetParent(root.transform, false);
        var transRT       = panelTrans.AddComponent<RectTransform>();
        transRT.anchorMin = Vector2.zero;
        transRT.anchorMax = Vector2.one;
        transRT.offsetMin = Vector2.zero;
        transRT.offsetMax = Vector2.zero;
        var transImg      = panelTrans.AddComponent<Image>();
        transImg.color    = new Color(0f, 0f, 0f, 0.75f);

        var txtTransTitulo = CreateTMP(panelTrans, "TMP_TransicionTitulo",
            "RETO COMPLETADO", 36f, new Color(0.3f, 1f, 0.5f),
            new Vector2(0f, 40f), new Vector2(700f, 60f),
            TextAlignmentOptions.Center);

        var txtTransSub = CreateTMP(panelTrans, "TMP_TransicionSub",
            "Cargando siguiente zona...", 18f, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0f, -20f), new Vector2(600f, 40f),
            TextAlignmentOptions.Center);

        panelTrans.SetActive(false);

        hudCtrl.panelTransicion     = panelTrans;
        hudCtrl.txtTransicionTitulo = txtTransTitulo;
        hudCtrl.txtTransicionSub    = txtTransSub;

        // ── Guardar prefab ───────────────────────────────────────────────────
        SavePrefab(root, TECHNICIAN_HUD_PATH, "TechnicianHUD");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject CreatePanel(GameObject parent, string name,
                                   Vector2 anchorMin, Vector2 anchorMax,
                                   Vector2 anchoredPos, Vector2 offsetCorrection,
                                   Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt        = go.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.offsetMin += offsetCorrection;
        rt.offsetMax += offsetCorrection;

        var img   = go.AddComponent<Image>();
        img.color = bgColor;

        return go;
    }

    static TMP_Text CreateTMP(GameObject parent, string name, string text,
                               float fontSize, Color color,
                               Vector2 anchoredPos, Vector2 sizeDelta,
                               TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var tmp               = go.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = fontSize;
        tmp.color             = color;
        tmp.alignment         = alignment;
        tmp.enableWordWrapping = false;

        var rt              = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        return tmp;
    }

    static Button CreateButton(GameObject parent, string name,
                                Vector2 anchoredPos, Vector2 sizeDelta,
                                string label, Color bgColor,
                                out TMP_Text outLabel)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt              = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        var img   = go.AddComponent<Image>();
        img.color = bgColor;

        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Color block más visible
        var cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor     = bgColor * 0.7f;
        cb.disabledColor    = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors          = cb;

        outLabel = CreateTMP(go, "TMP_Label", label, 9f, Color.white,
                             Vector2.zero, sizeDelta - new Vector2(10f, 0f),
                             TextAlignmentOptions.Center);

        return btn;
    }

    static bool ConfirmOverwrite(string path, string prefabName)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return true;

        return EditorUtility.DisplayDialog(
            "Prefab ya existe",
            $"Ya existe:\n{path}\n\n¿Sobreescribir {prefabName}?",
            "Sí, sobreescribir", "Cancelar");
    }

    static void SavePrefab(GameObject root, string path, string label)
    {
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[GameUIPrefabGenerator] {label} guardado en {path}");
        }
        else
        {
            Debug.LogError($"[GameUIPrefabGenerator] Error al guardar {label} en {path}");
        }
    }
}
