#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera el prefab completo del Explorador con los 3 elementos de UI espacial.
///
/// SALIDAS:
///   1. Assets/Prefabs/Explorer_Player.prefab  (modificado in-place)
///      • Bandeja_Recepcion     → + LineRenderer + DeliveryTrayIndicator
///      • Clipboard_VR          → nuevo hijo con ExplorerTaskClipboard (Canvas 40×26 cm)
///      • ValidationStation_VR  → nuevo hijo con ValidationStationUI + PointLight
///
///   2. Assets/Prefabs/ExplorerWorkstation.prefab  (bundle standalone)
///      Los mismos 3 elementos como prefab independiente para instanciar
///      en cualquier posición sobre la mesa.
///
/// Menú: Tools → TITA → Explorador → Generar Prefab Explorador Completo
/// </summary>
public static class ExplorerPrefabBuilder
{
    const string PLAYER_PATH    = "Assets/Prefabs/Explorer_Player.prefab";
    const string WORKSTATION_PATH = "Assets/Prefabs/ExplorerWorkstation.prefab";

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Explorador/Generar Prefab Explorador Completo")]
    public static void Build()
    {
        int changes = 0;

        // 1. Modifica Explorer_Player.prefab in-place
        changes += PatchExplorerPlayerPrefab();

        // 2. Genera ExplorerWorkstation.prefab desde cero
        BuildWorkstationPrefab();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Prefab Explorador generado",
            $"✅ Explorer_Player.prefab actualizado ({changes} elemento(s) añadido(s))\n" +
            $"✅ ExplorerWorkstation.prefab creado\n\n" +
            "Próximos pasos:\n" +
            "1. Abre la escena Explorador.unity (o MapVR.unity)\n" +
            "2. Arrastra ExplorerWorkstation.prefab sobre la mesa\n" +
            "3. Mueve ValidationStation_VR junto al VRValidationButton\n" +
            "4. Ejecuta Tools → TITA → Explorador → Construir UI Espacial Explorador\n" +
            "   para conectar GameManager e InstructionSystem",
            "OK");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  1. PATCH Explorer_Player.prefab
    // ═════════════════════════════════════════════════════════════════════
    static int PatchExplorerPlayerPrefab()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PATH);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"[ExplorerPrefabBuilder] No encontrado: {PLAYER_PATH}");
            return 0;
        }

        int added = 0;

        using var scope = new PrefabUtility.EditPrefabContentsScope(PLAYER_PATH);
        var root = scope.prefabContentsRoot;

        // ── A. DeliveryTrayIndicator en Bandeja_Recepcion ─────────────────
        var bandejaTF = root.transform.Find("Bandeja_Recepcion");
        if (bandejaTF != null)
        {
            var go = bandejaTF.gameObject;
            if (go.GetComponent<LineRenderer>() == null)
            {
                go.AddComponent<LineRenderer>();
                Debug.Log("[ExplorerPrefabBuilder] LineRenderer añadido a Bandeja_Recepcion.");
                added++;
            }
            if (go.GetComponent<DeliveryTrayIndicator>() == null)
            {
                var tray = go.AddComponent<DeliveryTrayIndicator>();
                // Ajustar dimensiones a la bandeja existente
                tray.width  = 0.20f;
                tray.depth  = 0.14f;
                tray.height = 0.001f;

                // Luz puntual hija
                var lightGO = new GameObject("Tray_Light");
                lightGO.transform.SetParent(go.transform, false);
                lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                var lt = lightGO.AddComponent<Light>();
                lt.type      = LightType.Point;
                lt.range     = 0.22f;
                lt.intensity = 0.15f;
                lt.color     = new Color(0f, 0.9f, 1f);
                tray.trayLight = lt;

                Debug.Log("[ExplorerPrefabBuilder] DeliveryTrayIndicator añadido a Bandeja_Recepcion.");
                added++;
            }
        }
        else
            Debug.LogWarning("[ExplorerPrefabBuilder] Bandeja_Recepcion no encontrada.");

        // ── B. Clipboard_VR ──────────────────────────────────────────────
        if (root.transform.Find("Clipboard_VR") == null)
        {
            var clip = BuildClipboardGO(root.transform);
            // Posición: esquina izquierda-delantera de la mesa (local al prefab raíz)
            clip.transform.localPosition = new Vector3(-0.18f, 0.005f, 0.10f);
            clip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Debug.Log("[ExplorerPrefabBuilder] Clipboard_VR añadido.");
            added++;
        }
        else
            Debug.Log("[ExplorerPrefabBuilder] Clipboard_VR ya existe.");

        // ── C. ValidationStation_VR ──────────────────────────────────────
        if (root.transform.Find("ValidationStation_VR") == null)
        {
            var station = BuildValidationStationGO(root.transform);
            // Posición: a la derecha de la bandeja, sobre la mesa
            station.transform.localPosition = new Vector3(0.15f, 0.005f, -0.06f);
            Debug.Log("[ExplorerPrefabBuilder] ValidationStation_VR añadido.");
            added++;
        }
        else
            Debug.Log("[ExplorerPrefabBuilder] ValidationStation_VR ya existe.");

        return added;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  2. ExplorerWorkstation.prefab  (bundle standalone)
    // ═════════════════════════════════════════════════════════════════════
    static void BuildWorkstationPrefab()
    {
        var root = new GameObject("ExplorerWorkstation");

        // ── Clipboard ────────────────────────────────────────────────────
        var clip = BuildClipboardGO(root.transform);
        clip.transform.localPosition = new Vector3(-0.18f, 0.005f, 0.10f);
        clip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

        // ── Delivery Tray ─────────────────────────────────────────────────
        var trayGO = new GameObject("DeliveryTray_VR");
        trayGO.transform.SetParent(root.transform, false);
        trayGO.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        trayGO.AddComponent<LineRenderer>();
        var tray = trayGO.AddComponent<DeliveryTrayIndicator>();
        tray.width  = 0.22f;
        tray.depth  = 0.16f;
        tray.height = 0.002f;
        // Luz
        var tLightGO = new GameObject("Tray_Light");
        tLightGO.transform.SetParent(trayGO.transform, false);
        tLightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var tLight = tLightGO.AddComponent<Light>();
        tLight.type      = LightType.Point;
        tLight.range     = 0.25f;
        tLight.intensity = 0.15f;
        tLight.color     = new Color(0f, 0.9f, 1f);
        tray.trayLight   = tLight;

        // ── Validation Station ────────────────────────────────────────────
        var station = BuildValidationStationGO(root.transform);
        station.transform.localPosition = new Vector3(0.15f, 0.005f, -0.06f);

        // ── Guardar prefab ────────────────────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(root, WORKSTATION_PATH);
        Object.DestroyImmediate(root);
        Debug.Log($"[ExplorerPrefabBuilder] ExplorerWorkstation.prefab guardado.");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CLIPBOARD GO
    // ═════════════════════════════════════════════════════════════════════
    static GameObject BuildClipboardGO(Transform parent)
    {
        var go = new GameObject("Clipboard_VR");
        go.transform.SetParent(parent, false);

        // Canvas WorldSpace  400 × 260 px  →  40 × 26 cm  (escala 1/1000)
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<GraphicRaycaster>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(400f, 260f);
        rt.localScale = Vector3.one * 0.001f;

        // ── Fondo ──
        var bg    = Rect("Background", go.transform, 0, 0, 400, 260);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color         = Col("#08111E");
        bgImg.raycastTarget = false;
        Border(bg, Col("#00E5FF"), 1.5f);

        // Barra acento superior
        Img("Accent_Top", go.transform, 0, 122, 400, 4, Col("#00E5FF"));

        // Header reto
        var hdrTMP = TMP("Txt_RetoHeader", go.transform,
            "RETO 4: SINCRONIZACIÓN HARDWARE/SOFTWARE",
            18f, Col("#00E5FF"), FontStyles.Bold, 0, 108, 380, 22);
        hdrTMP.alignment = TextAlignmentOptions.Center;

        // Divisor
        Img("Divider", go.transform, 0, 94, 360, 1, new Color(0f, 0.9f, 1f, 0.2f));

        // Paso + Timer
        var pasoTMP = TMP("Txt_PasoTimer", go.transform,
            "Paso 1 de 5  ·  15:00",
            10f, Col("#6A8FA8"), FontStyles.Normal, 0, 80, 380, 14);
        pasoTMP.alignment = TextAlignmentOptions.Center;

        // Status grande
        var statusTMP = TMP("Txt_Status", go.transform,
            "Esperando código\ndel Técnico...",
            14f, Col("#D8EEFF"), FontStyles.Bold, 0, 30, 380, 70);
        statusTMP.alignment = TextAlignmentOptions.Center;

        // Detalle
        var detTMP = TMP("Txt_Detalle", go.transform,
            "El Técnico programará el Arduino",
            9f, Col("#6A8FA8"), FontStyles.Normal, 0, -18, 380, 20);
        detTMP.alignment = TextAlignmentOptions.Center;

        // Barra de progreso
        var barBg  = Img("BarraBg",   go.transform, 0, -44, 360, 8, Col("#0D1520"));
        var barFillGO = new GameObject("BarraFill");
        barFillGO.transform.SetParent(barBg.transform, false);
        var barFill       = barFillGO.AddComponent<Image>();
        barFill.color      = Col("#00E5FF");
        barFill.type       = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = 0.2f;
        var bfRT = barFillGO.GetComponent<RectTransform>();
        bfRT.anchorMin = Vector2.zero;
        bfRT.anchorMax = new Vector2(0.2f, 1f);
        bfRT.offsetMin = bfRT.offsetMax = Vector2.zero;

        // Panel evento de red (inactivo)
        var netGO  = Img("Panel_NetEvento", go.transform, 0, -80, 380, 26,
                         new Color(0f, 0.9f, 1f, 0.08f));
        Border(netGO, Col("#00E5FF"), 1f);
        var netTxt = TMP("Txt_NetEvento", netGO.transform,
            "— sin eventos de red —",
            9f, Col("#00E5FF"), FontStyles.Normal, 0, 0, 370, 24);
        netTxt.alignment = TextAlignmentOptions.Center;
        netGO.SetActive(false);

        // Script ExplorerTaskClipboard
        var clip            = go.AddComponent<ExplorerTaskClipboard>();
        clip.txtRetoHeader  = hdrTMP;
        clip.txtPasoTimer   = pasoTMP;
        clip.txtStatus      = statusTMP;
        clip.txtDetalle     = detTMP;
        clip.barraProgreso  = barFill;
        clip.panelNetEvento = netGO;
        clip.txtNetEvento   = netTxt;
        clip.imgNetEventoBg = netGO.GetComponent<Image>();
        clip.fondoPanel     = bgImg;

        return go;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  VALIDATION STATION GO
    // ═════════════════════════════════════════════════════════════════════
    static GameObject BuildValidationStationGO(Transform parent)
    {
        var go = new GameObject("ValidationStation_VR");
        go.transform.SetParent(parent, false);

        var station = go.AddComponent<ValidationStationUI>();

        // PointLight del botón
        var lightGO = new GameObject("Station_Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var lt        = lightGO.AddComponent<Light>();
        lt.type       = LightType.Point;
        lt.range      = 0.30f;
        lt.intensity  = 0.6f;
        lt.color      = new Color(0.16f, 0.47f, 1f);
        station.stationLight      = lt;
        station.lightMaxIntensity = 0.6f;

        return go;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers de construcción de UI
    // ═════════════════════════════════════════════════════════════════════

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

    static Color Col(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }

    static GameObject Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    static GameObject Img(string name, Transform parent,
        float x, float y, float w, float h, Color color)
    {
        var go  = Rect(name, parent, x, y, w, h);
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return go;
    }

    static TextMeshProUGUI TMP(string name, Transform parent,
        string text, float size, Color color, FontStyles style,
        float x, float y, float w, float h)
    {
        var go = Rect(name, parent, x, y, w, h);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = size;
        t.color         = color;
        t.fontStyle     = style;
        t.raycastTarget = false;
        if (Font != null) t.font = Font;
        return t;
    }

    static void Border(GameObject parent, Color color, float thickness)
    {
        void Line(string n, float ax0, float ay0, float ax1, float ay1, float sw, float sh)
        {
            var go  = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color         = color;
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
