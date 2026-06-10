#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye el panel del editor de código Arduino dentro del TechnicianMonitorHUD.
///
/// Qué hace:
///   1. Encuentra TechnicianMonitorHUD en la escena (o lo crea desde el prefab).
///   2. Añade un Panel_CodeEditor hijo con InputField, botones y consola.
///   3. Añade ArduinoCodeEditor al panel y conecta todas las referencias.
///   4. Crea un GO [ArdityManager] en la escena con ArdityManager.
///   5. Marca la escena dirty.
///
/// Menú: Tools → TITA → Reto 4 → Setup Editor de Código Arduino
/// </summary>
public static class ArduinoCodeEditorSetupTool
{
    const string MENU = "Tools/TITA/Reto 4/Setup Editor de Código Arduino";

    [MenuItem(MENU)]
    static void Run()
    {
        var log = new System.Text.StringBuilder("=== ArduinoCodeEditorSetupTool ===\n\n");

        // ── 1. Encontrar o instanciar TechnicianMonitorHUD ────────────────
        var hud = FindHUD();
        if (hud == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TechnicianMonitorHUD.prefab");
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró TechnicianMonitorHUD en escena ni el prefab.\n" +
                    "Ejecuta primero: Tools → TITA → Reto 4 → Setup Monitor Arduino.", "OK");
                return;
            }
            hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(hud, "Instanciar TechnicianMonitorHUD");
            hud.SetActive(false);
            log.AppendLine("[OK] TechnicianMonitorHUD instanciado desde prefab.");
        }
        else
            log.AppendLine($"[--] TechnicianMonitorHUD encontrado: {hud.name}");

        var canvas = hud.GetComponent<Canvas>();
        if (canvas == null) { canvas = hud.GetComponentInChildren<Canvas>(true); }

        // ── 2. Eliminar panel viejo si ya existe ──────────────────────────
        var oldPanel = hud.transform.Find("Panel_CodeEditor");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
            log.AppendLine("[OK] Panel_CodeEditor anterior eliminado (se recreará).");
        }

        // ── 3. Crear Panel_CodeEditor ─────────────────────────────────────
        var panel = CreatePanel(hud.transform, "Panel_CodeEditor",
            new Vector2(0f, 0f), new Vector2(1920f, 1080f),
            new Color(0.05f, 0.06f, 0.1f, 0.97f));
        log.AppendLine("[OK] Panel_CodeEditor creado.");

        // ── 4. Título ─────────────────────────────────────────────────────
        CreateLabel(panel.transform, "Lbl_Title",
            "Editor de Codigo Arduino — Reto 4",
            new Vector2(0f, 460f), new Vector2(1800f, 70f), 36, Color.cyan, FontStyles.Bold);

        // ── 5. Línea separadora ───────────────────────────────────────────
        CreateImage(panel.transform, "Sep_Top",
            new Vector2(0f, 420f), new Vector2(1880f, 4f),
            new Color(0f, 1f, 0.7f, 0.6f));

        // ── 6. InputField de código (área principal) ──────────────────────
        var codeField = CreateCodeInputField(panel.transform,
            new Vector2(-430f, 50f), new Vector2(980f, 740f));
        log.AppendLine("[OK] InputField de código creado.");

        // ── 7. Panel derecho: Preview + Consola ───────────────────────────
        var rightBg = CreatePanel(panel.transform, "Panel_Right",
            new Vector2(490f, 50f), new Vector2(900f, 740f),
            new Color(0.03f, 0.05f, 0.08f, 0.8f));

        var txtPreview = CreateLabel(rightBg.transform, "Txt_Preview",
            "<color=#888><i>Haz clic en Verificar para analizar el código.</i></color>",
            new Vector2(0f, 130f), new Vector2(860f, 330f), 22, Color.white);
        txtPreview.alignment = TextAlignmentOptions.TopLeft;
        txtPreview.textWrappingMode = TMPro.TextWrappingModes.Normal;

        CreateImage(rightBg.transform, "Sep_Mid",
            new Vector2(0f, -40f), new Vector2(860f, 3f),
            new Color(0f, 0.7f, 1f, 0.4f));

        var txtConsole = CreateLabel(rightBg.transform, "Txt_Console",
            "<color=#888>Consola lista.</color>",
            new Vector2(0f, -220f), new Vector2(860f, 290f), 20, Color.white);
        txtConsole.alignment = TextAlignmentOptions.TopLeft;
        txtConsole.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // ── 8. Status bar ─────────────────────────────────────────────────
        var txtStatus = CreateLabel(panel.transform, "Txt_Status",
            "Listo.",
            new Vector2(0f, -460f), new Vector2(1400f, 50f), 24, Color.grey);
        txtStatus.alignment = TextAlignmentOptions.MidlineLeft;

        // ── 9. Botones ────────────────────────────────────────────────────
        var btnVerify = CreateButton(panel.transform, "Btn_Verificar", "Verificar",
            new Vector2(-200f, -460f), new Vector2(260f, 60f),
            new Color(0.1f, 0.4f, 0.8f, 1f));

        var btnUpload = CreateButton(panel.transform, "Btn_Subir", "Subir >>",
            new Vector2(120f, -460f), new Vector2(260f, 60f),
            new Color(0f, 0.6f, 0.2f, 1f));

        log.AppendLine("[OK] Botones Verificar y Subir creados.");

        // ── 10. Añadir ArduinoCodeEditor ──────────────────────────────────
        var editor = panel.GetComponent<ArduinoCodeEditor>();
        if (editor == null)
            editor = Undo.AddComponent<ArduinoCodeEditor>(panel);

        Undo.RecordObject(editor, "Configurar ArduinoCodeEditor");
        editor.inputCode  = codeField;
        editor.txtPreview = txtPreview;
        editor.txtConsole = txtConsole;
        editor.txtStatus  = txtStatus;
        editor.btnVerify  = btnVerify;
        editor.btnUpload  = btnUpload;
        log.AppendLine("[OK] ArduinoCodeEditor configurado con todas las referencias.");

        // ── 11. Crear ArdityManager en escena ────────────────────────────
        var existing = Object.FindAnyObjectByType<ArdityManager>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var ardityGO = new GameObject("[ArdityManager]");
            Undo.RegisterCreatedObjectUndo(ardityGO, "Crear ArdityManager");
            ardityGO.AddComponent<ArdityManager>();
            log.AppendLine("[OK] [ArdityManager] creado en escena.\n" +
                           "     → Arrastra el SerialController prefab a la escena y asígna lo al campo serialController.");
        }
        else
            log.AppendLine("[--] ArdityManager ya existe.");

        // ── 12. Asignar cámara al Canvas ──────────────────────────────────
        if (canvas != null && canvas.worldCamera == null)
        {
            Camera cam = GameObject.Find("Pc_Camera")?.GetComponent<Camera>()
                      ?? Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(canvas, "Asignar worldCamera");
                canvas.worldCamera = cam;
                log.AppendLine($"[OK] worldCamera → {cam.name}");
            }
        }

        // ── 13. Marcar dirty ──────────────────────────────────────────────
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine("\n✅ LISTO. Pasos finales:");
        log.AppendLine("  1. Arrastra Assets/Ardity/Prefabs/SerialController.prefab a la escena");
        log.AppendLine("  2. En SerialController: portName = COM# de tu Arduino, baudRate = 9600");
        log.AppendLine("  3. En SerialController: messageListener = GO [ArdityManager]");
        log.AppendLine("  4. Guarda la escena (Ctrl+S)");
        log.AppendLine("  5. Sube ArduinoReceptor_Reto4.ino al Arduino físico");

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Editor de Código Arduino configurado", log.ToString(), "OK");
    }

    // ─────────────────────────────────────────────
    //  Helpers de UI
    // ─────────────────────────────────────────────

    static GameObject FindHUD()
    {
        // Buscar por nombre en escena (activo e inactivo)
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == "TechnicianMonitorHUD" && t.GetComponent<Canvas>() != null)
                return t.gameObject;
        return null;
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {name}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta      = sizeDelta;

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = false;
        return go;
    }

    static Image CreateImage(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {name}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text CreateLabel(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, Color color,
        FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {name}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static TMP_InputField CreateCodeInputField(Transform parent,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        // Contenedor
        var go = new GameObject("InputField_Code");
        Undo.RegisterCreatedObjectUndo(go, "Crear InputField_Code");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 1f);

        var field = go.AddComponent<TMP_InputField>();
        field.lineType       = TMP_InputField.LineType.MultiLineNewline;
        field.contentType    = TMP_InputField.ContentType.Standard;
        field.characterLimit = 0;

        // Viewport
        var vpGO = new GameObject("Text Area");
        Undo.RegisterCreatedObjectUndo(vpGO, "Crear Text Area");
        vpGO.transform.SetParent(go.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(10, 10);
        vpRT.offsetMax = new Vector2(-10, -10);
        var mask = vpGO.AddComponent<RectMask2D>();

        // Text area
        var textGO = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(textGO, "Crear Text");
        textGO.transform.SetParent(vpGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.fontSize   = 22;
        textTMP.color      = new Color(0.85f, 0.95f, 0.85f);
        textTMP.alignment  = TextAlignmentOptions.TopLeft;
        textTMP.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        textTMP.richText   = false;     // código plano, sin rich text

        field.textViewport  = vpRT;
        field.textComponent = textTMP;

        return field;
    }

    static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {name}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor     = bgColor * 0.7f;
        btn.colors = colors;

        // Label
        var lblGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(lblGO, "Crear Label");
        lblGO.transform.SetParent(go.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var txt = lblGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 26;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;

        return btn;
    }
}
#endif
