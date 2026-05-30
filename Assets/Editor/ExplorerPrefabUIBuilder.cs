#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera dos prefabs para la UI espacial del Explorador:
///
///   1. Assets/Prefabs/ExplorerWorkstation.prefab
///      Contiene los 3 elementos de la mesa de trabajo:
///        • Clipboard_VR    (ExplorerTaskClipboard)
///        • DeliveryTray_VR (DeliveryTrayIndicator + LineRenderer)
///        • ValidationStation_VR (ValidationStationUI + luces)
///      → Instanciar en la escena Explorador, posicionarlo sobre la mesa.
///
///   2. Actualiza Assets/Prefabs/Explorer_Player.prefab
///      Añade DeliveryTrayIndicator a Bandeja_Recepcion si no existe.
///
/// Menú: Tools → TITA → Explorador → Generar Prefab UI Workstation
/// </summary>
public static class ExplorerPrefabUIBuilder
{
    const string WS_PREFAB_PATH = "Assets/Prefabs/ExplorerWorkstation.prefab";
    const string EX_PREFAB_PATH = "Assets/Prefabs/Explorer_Player.prefab";

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Explorador/Generar Prefab UI Workstation")]
    public static void Build()
    {
        var root = BuildWorkstationPrefab();
        UpdateExplorerPlayerPrefab();

        EditorUtility.DisplayDialog(
            "Prefab UI Explorador generado",
            $"Prefab guardado en:\n  {WS_PREFAB_PATH}\n\n" +
            "Pasos en Unity:\n" +
            "1. Arrastra ExplorerWorkstation.prefab a la escena Explorador\n" +
            "2. Posiciónalo encima de la mesa del Explorador (Mesa_Explorador)\n" +
            "3. Ejecuta Tools→TITA→Explorador→Conectar Referencias Workstation\n" +
            "4. Arrastra VRValidationButton al campo 'Validation Station > Station Target'",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PREFAB WORKSTATION
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildWorkstationPrefab()
    {
        var root = new GameObject("ExplorerWorkstation");

        // ── 1. Clipboard ─────────────────────────────────────────────────
        var clipboard = BuildClipboard(root.transform);

        // ── 2. Delivery Tray ─────────────────────────────────────────────
        var tray = BuildDeliveryTray(root.transform);

        // ── 3. Validation Station ─────────────────────────────────────────
        var station = BuildValidationStation(root.transform);

        // ── Guardar prefab ────────────────────────────────────────────────
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, WS_PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[ExplorerPrefabUIBuilder] Prefab guardado: {WS_PREFAB_PATH}");
        return prefab;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. CLIPBOARD
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildClipboard(Transform parent)
    {
        var go = new GameObject("Clipboard_VR");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(-0.18f, 0.005f, 0.10f);
        go.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

        // Canvas WorldSpace  400 × 260 px  →  40 × 26 cm  (escala 1/1000)
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<GraphicRaycaster>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(400f, 260f);
        rt.localScale = Vector3.one * 0.001f;

        // ── Fondo ──
        var bg = MakeImg("Background", go.transform, 0f, 0f, 400f, 260f, Col("#08111E"));
        AddBorder(bg, Col("#00E5FF"), 1.5f);

        // Acento superior
        MakeImg("Accent_Top", go.transform, 0f, 122f, 400f, 4f, Col("#00E5FF"));

        // Header del reto
        var hdrTMP = MakeTMP("Txt_RetoHeader", go.transform,
            "RETO 4: SINCRONIZACIÓN HARDWARE/SOFTWARE",
            18f, Col("#00E5FF"), FontStyles.Bold,
            new Vector2(0f, 108f), new Vector2(380f, 22f));
        hdrTMP.alignment = TextAlignmentOptions.Center;

        // Divisor
        MakeImg("Divider", go.transform, 0f, 94f, 360f, 1f,
            new Color(0f, 0.9f, 1f, 0.2f));

        // Paso + Timer
        var pasoTMP = MakeTMP("Txt_PasoTimer", go.transform,
            "Paso 1 de 5  ·  15:00",
            10f, Col("#6A8FA8"), FontStyles.Normal,
            new Vector2(0f, 80f), new Vector2(380f, 14f));
        pasoTMP.alignment = TextAlignmentOptions.Center;

        // Status grande
        var statusTMP = MakeTMP("Txt_Status", go.transform,
            "Esperando código\ndel Técnico...",
            14f, Col("#D8EEFF"), FontStyles.Bold,
            new Vector2(0f, 30f), new Vector2(380f, 70f));
        statusTMP.alignment = TextAlignmentOptions.Center;

        // Detalle
        var detTMP = MakeTMP("Txt_Detalle", go.transform,
            "El Técnico programará el Arduino",
            9f, Col("#6A8FA8"), FontStyles.Normal,
            new Vector2(0f, -18f), new Vector2(380f, 20f));
        detTMP.alignment = TextAlignmentOptions.Center;

        // Barra de progreso
        var barBg = MakeImg("BarraBg", go.transform, 0f, -44f, 360f, 8f, Col("#0D1520"));
        var barFillGO = new GameObject("BarraFill");
        barFillGO.transform.SetParent(barBg.transform, false);
        var barFill = barFillGO.AddComponent<Image>();
        barFill.color      = Col("#00E5FF");
        barFill.type       = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = 0.3f;
        var bRT = barFillGO.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = new Vector2(0.3f, 1f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        // Panel evento de red
        var netGO  = MakeImg("Panel_NetEvento", go.transform, 0f, -80f, 380f, 26f,
            new Color(0f, 0.9f, 1f, 0.08f));
        AddBorder(netGO, Col("#00E5FF"), 1f);
        var netTxt = MakeTMP("Txt_NetEvento", netGO.transform,
            "— sin eventos —",
            9f, Col("#00E5FF"), FontStyles.Normal,
            Vector2.zero, new Vector2(370f, 24f));
        netTxt.alignment = TextAlignmentOptions.Center;
        netGO.SetActive(false);

        // Script
        var clip = go.AddComponent<ExplorerTaskClipboard>();
        clip.txtRetoHeader  = hdrTMP;
        clip.txtPasoTimer   = pasoTMP;
        clip.txtStatus      = statusTMP;
        clip.txtDetalle     = detTMP;
        clip.barraProgreso  = barFill;
        clip.panelNetEvento = netGO;
        clip.txtNetEvento   = netTxt;
        clip.imgNetEventoBg = netGO.GetComponent<Image>();
        clip.fondoPanel     = bg.GetComponent<Image>();

        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. DELIVERY TRAY
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildDeliveryTray(Transform parent)
    {
        var go = new GameObject("DeliveryTray_VR");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.001f, 0f);

        // LineRenderer (DeliveryTrayIndicator lo configura en Awake)
        go.AddComponent<LineRenderer>();
        var tray = go.AddComponent<DeliveryTrayIndicator>();

        // Luz puntual
        var lightGO = new GameObject("Tray_Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var lt = lightGO.AddComponent<Light>();
        lt.type       = LightType.Point;
        lt.range      = 0.25f;
        lt.intensity  = 0.15f;
        lt.color      = new Color(0f, 0.9f, 1f);
        tray.trayLight = lt;

        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. VALIDATION STATION
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildValidationStation(Transform parent)
    {
        var go = new GameObject("ValidationStation_VR");
        go.transform.SetParent(parent, false);
        // Posición placeholder — en la escena se moverá junto al botón físico
        go.transform.localPosition = new Vector3(0.15f, 0.03f, 0f);

        var station = go.AddComponent<ValidationStationUI>();

        // Luz de ambiente del botón
        var lightGO = new GameObject("Station_Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var lt = lightGO.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.range     = 0.30f;
        lt.intensity = 0.6f;
        lt.color     = new Color(0.16f, 0.47f, 1f);
        station.stationLight      = lt;
        station.lightMaxIntensity = 0.6f;

        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE Explorer_Player.prefab
    // ─────────────────────────────────────────────────────────────────────
    static void UpdateExplorerPlayerPrefab()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EX_PREFAB_PATH);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[ExplorerPrefabUIBuilder] No se encontró: {EX_PREFAB_PATH}");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(EX_PREFAB_PATH);
        var root = scope.prefabContentsRoot;

        // Buscar Bandeja_Recepcion
        var trayTF = root.transform.Find("Bandeja_Recepcion");
        if (trayTF == null)
        {
            Debug.LogWarning("[ExplorerPrefabUIBuilder] Bandeja_Recepcion no encontrada en Explorer_Player.");
            return;
        }

        // Añadir LineRenderer + DeliveryTrayIndicator si no existe
        if (trayTF.GetComponent<DeliveryTrayIndicator>() == null)
        {
            trayTF.gameObject.AddComponent<LineRenderer>();
            trayTF.gameObject.AddComponent<DeliveryTrayIndicator>();
            Debug.Log("[ExplorerPrefabUIBuilder] DeliveryTrayIndicator añadido a Bandeja_Recepcion.");
        }
        else
            Debug.Log("[ExplorerPrefabUIBuilder] DeliveryTrayIndicator ya existe en Bandeja_Recepcion.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers de construcción
    // ─────────────────────────────────────────────────────────────────────
    static Color Col(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }

    static GameObject MakeImg(string name, Transform parent,
        float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent,
        string text, float size, Color color, FontStyles style,
        Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = style;
        t.raycastTarget = false;

        // Intentar cargar LiberationSans SDF
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) t.font = font;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = sizeDelta;
        return t;
    }

    static void AddBorder(GameObject parent, Color color, float thickness)
    {
        void Line(string n, float ax0, float ay0, float ax1, float ay1, float sw, float sh)
        {
            var go  = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax0, ay0);
            rt.anchorMax = new Vector2(ax1, ay1);
            rt.sizeDelta = new Vector2(sw, sh);
            rt.anchoredPosition = Vector2.zero;
        }
        Line("_BT", 0f, 1f, 1f, 1f, 0f, thickness);
        Line("_BB", 0f, 0f, 1f, 0f, 0f, thickness);
        Line("_BL", 0f, 0f, 0f, 1f, thickness, 0f);
        Line("_BR", 1f, 0f, 1f, 1f, thickness, 0f);
    }
}
#endif
