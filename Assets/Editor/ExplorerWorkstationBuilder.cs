#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera los 3 elementos de UI espacial del Explorador:
///   1. Portapapeles de Objetivos (ExplorerTaskClipboard)
///   2. Zona de Recepción de Componentes (DeliveryTrayIndicator)
///   3. Estación de Validación (ValidationStationUI)
///
/// Menú: Tools → TITA → Explorador → Construir UI Espacial Explorador
/// </summary>
public static class ExplorerWorkstationBuilder
{
    [MenuItem("Tools/TITA/Explorador/Construir UI Espacial Explorador")]
    static void Build()
    {
        var mesa = FindMesa();
        int created = 0;
        var log = new System.Text.StringBuilder();

        log.AppendLine("=== Explorador Workstation UI ===\n");

        // ── 1. Clipboard ─────────────────────────────────────────────────
        if (FindAnyObjectOfType<ExplorerTaskClipboard>() == null)
        {
            var clipGO = BuildClipboard(mesa);
            log.AppendLine($"✅ Clipboard creado: {clipGO.name}");
            created++;
        }
        else
            log.AppendLine("✓  ExplorerTaskClipboard ya existe.");

        // ── 2. Delivery Tray ─────────────────────────────────────────────
        if (FindAnyObjectOfType<DeliveryTrayIndicator>() == null)
        {
            var trayGO = BuildDeliveryTray(mesa);
            log.AppendLine($"✅ DeliveryTray creada: {trayGO.name}");
            created++;
        }
        else
            log.AppendLine("✓  DeliveryTrayIndicator ya existe.");

        // ── 3. Validation Station ─────────────────────────────────────────
        if (FindAnyObjectOfType<ValidationStationUI>() == null)
        {
            var btn = Object.FindAnyObjectByType<VRValidationButton>();
            if (btn != null)
            {
                var stGO = BuildValidationStation(btn.gameObject);
                log.AppendLine($"✅ ValidationStation creada: {stGO.name}");
                created++;
            }
            else
                log.AppendLine("⚠  VRValidationButton no encontrado — " +
                    "ejecuta 'Tools→TITA→Setup Escena Explorador' primero.");
        }
        else
            log.AppendLine("✓  ValidationStationUI ya existe.");

        // ── Conectar referencias ──────────────────────────────────────────
        ConnectReferences();
        log.AppendLine("\n✅ Referencias conectadas automáticamente.");

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            created > 0 ? $"UI Espacial Explorador — {created} elemento(s) creado(s)" : "Sin cambios",
            log.ToString() +
            "\n\nPasos manuales:\n" +
            "  • Posiciona 'Clipboard_VR' en la esquina izquierda de la mesa\n" +
            "  • Posiciona 'DeliveryTray_VR' en el centro de la mesa\n" +
            "  • Comprueba la escala del canvas (1/1000 por defecto)",
            "OK");

        Debug.Log("[ExplorerWorkstationBuilder]\n" + log);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUILDERS
    // ═════════════════════════════════════════════════════════════════════

    static GameObject BuildClipboard(GameObject mesa)
    {
        var go = new GameObject("Clipboard_VR");
        Undo.RegisterCreatedObjectUndo(go, "Crear Clipboard_VR");
        if (mesa != null) go.transform.SetParent(mesa.transform, false);

        // Posición: esquina izquierda-trasera de la mesa, inclinado -15° en X
        go.transform.localPosition = new Vector3(-0.18f, 0.005f, 0.10f);
        go.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

        // Canvas WorldSpace 400×260 px → 40×26 cm
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(400f, 260f);
        rt.localScale = Vector3.one * 0.001f;

        // Fondo
        var bgGO  = CreateRect("Background", go.transform, 0f, 0f, 400f, 260f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Col("#08111E");

        // Borde cyan
        AddBorder(bgGO, Col("#00E5FF"), 1.5f);

        // Acento superior (barra de 4px)
        var accentGO = CreateRect("Accent_Top", go.transform, 0f, 122f, 400f, 4f);
        accentGO.AddComponent<Image>().color = Col("#00E5FF");

        // Header reto
        var hdrGO  = CreateRect("Txt_RetoHeader", go.transform, 0f, 108f, 380f, 22f);
        var hdrTMP = hdrGO.AddComponent<TMPro.TextMeshProUGUI>();
        hdrTMP.text       = "RETO 4: SINCRONIZACIÓN HARDWARE/SOFTWARE";
        hdrTMP.fontSize   = 18f;
        hdrTMP.color      = Col("#00E5FF");
        hdrTMP.fontStyle  = TMPro.FontStyles.Bold;
        hdrTMP.alignment  = TMPro.TextAlignmentOptions.Center;

        // Divisor
        var divGO = CreateRect("Divider", go.transform, 0f, 94f, 360f, 1f);
        divGO.AddComponent<Image>().color = new Color(0f, 0.9f, 1f, 0.2f);

        // Paso + Timer
        var pasoGO  = CreateRect("Txt_PasoTimer", go.transform, 0f, 80f, 380f, 14f);
        var pasoTMP = pasoGO.AddComponent<TMPro.TextMeshProUGUI>();
        pasoTMP.text      = "Paso 1 de 5  ·  15:00";
        pasoTMP.fontSize  = 10f;
        pasoTMP.color     = Col("#6A8FA8");
        pasoTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // Status grande
        var statusGO  = CreateRect("Txt_Status", go.transform, 0f, 30f, 380f, 70f);
        var statusTMP = statusGO.AddComponent<TMPro.TextMeshProUGUI>();
        statusTMP.text      = "Esperando código\ndel Técnico...";
        statusTMP.fontSize  = 14f;
        statusTMP.color     = Col("#D8EEFF");
        statusTMP.fontStyle = TMPro.FontStyles.Bold;
        statusTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // Detalle
        var detGO  = CreateRect("Txt_Detalle", go.transform, 0f, -18f, 380f, 20f);
        var detTMP = detGO.AddComponent<TMPro.TextMeshProUGUI>();
        detTMP.text      = "El Técnico programará el Arduino";
        detTMP.fontSize  = 9f;
        detTMP.color     = Col("#6A8FA8");
        detTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // Barra de progreso
        var barBgGO = CreateRect("BarraBg", go.transform, 0f, -44f, 360f, 8f);
        barBgGO.AddComponent<Image>().color = Col("#0D1520");
        var barFillGO = CreateRect("BarraFill", barBgGO.transform, 0f, 0f, 0f, 8f);
        var barFill   = barFillGO.AddComponent<Image>();
        barFill.color      = Col("#00E5FF");
        barFill.type       = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        var barFRT = barFillGO.GetComponent<RectTransform>();
        barFRT.anchorMin = Vector2.zero; barFRT.anchorMax = new Vector2(0f, 1f);
        barFRT.offsetMin = barFRT.offsetMax = Vector2.zero;

        // Panel de evento de red (inactivo por defecto)
        var netGO  = CreateRect("Panel_NetEvento", go.transform, 0f, -80f, 380f, 26f);
        var netImg = netGO.AddComponent<Image>();
        netImg.color = new Color(0f, 0.9f, 1f, 0.08f);
        AddBorder(netGO, Col("#00E5FF"), 1f);
        var netTxt      = CreateRect("Txt_Net", netGO.transform, 0f, 0f, 370f, 24f).AddComponent<TMPro.TextMeshProUGUI>();
        netTxt.text     = "— evento de red —";
        netTxt.fontSize = 9f;
        netTxt.color    = Col("#00E5FF");
        netTxt.alignment = TMPro.TextAlignmentOptions.Center;
        netGO.SetActive(false);

        // Script
        var clip = go.AddComponent<ExplorerTaskClipboard>();
        clip.txtRetoHeader   = hdrTMP;
        clip.txtPasoTimer    = pasoTMP;
        clip.txtStatus       = statusTMP;
        clip.txtDetalle      = detTMP;
        clip.barraProgreso   = barFill;
        clip.panelNetEvento  = netGO;
        clip.txtNetEvento    = netTxt;
        clip.imgNetEventoBg  = netImg;
        clip.fondoPanel      = bgImg;

        EditorUtility.SetDirty(go);
        return go;
    }

    static GameObject BuildDeliveryTray(GameObject mesa)
    {
        var go = new GameObject("DeliveryTray_VR");
        Undo.RegisterCreatedObjectUndo(go, "Crear DeliveryTray_VR");
        if (mesa != null) go.transform.SetParent(mesa.transform, false);

        // Centro de la mesa, superficie de la mesa
        go.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        go.transform.localRotation = Quaternion.identity;

        var lr    = go.AddComponent<LineRenderer>();
        var tray  = go.AddComponent<DeliveryTrayIndicator>();

        // Luz opcional
        var lightGO = new GameObject("Tray_Light");
        Undo.RegisterCreatedObjectUndo(lightGO, "Crear Tray_Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var light    = lightGO.AddComponent<Light>();
        light.type   = LightType.Point;
        light.range  = 0.25f;
        light.intensity = 0.15f;
        light.color  = new Color(0f, 0.9f, 1f, 1f);
        tray.trayLight = light;

        EditorUtility.SetDirty(go);
        return go;
    }

    static GameObject BuildValidationStation(GameObject buttonGO)
    {
        var go = new GameObject("ValidationStation_VR");
        Undo.RegisterCreatedObjectUndo(go, "Crear ValidationStation_VR");
        go.transform.SetParent(buttonGO.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0f);

        var station = go.AddComponent<ValidationStationUI>();

        // Luz del botón
        var lightGO    = new GameObject("Station_Light");
        Undo.RegisterCreatedObjectUndo(lightGO, "Crear Station_Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var light      = lightGO.AddComponent<Light>();
        light.type     = LightType.Point;
        light.range    = 0.3f;
        light.intensity = 0.6f;
        light.color    = new Color(0.16f, 0.47f, 1f);
        station.stationLight       = light;
        station.lightMaxIntensity  = 0.6f;

        EditorUtility.SetDirty(go);
        return go;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Conectar referencias
    // ═════════════════════════════════════════════════════════════════════

    static void ConnectReferences()
    {
        var clip   = Object.FindAnyObjectByType<ExplorerTaskClipboard>();
        var gm     = Object.FindAnyObjectByType<GameManager>();
        var ins    = Object.FindAnyObjectByType<InstructionSystem>();
        var haptic = Object.FindAnyObjectByType<HapticFeedback>();
        var tray   = Object.FindAnyObjectByType<DeliveryTrayIndicator>();
        var station= Object.FindAnyObjectByType<ValidationStationUI>();

        if (clip != null)
        {
            if (clip.gameManager       == null && gm  != null) { clip.gameManager       = gm;  EditorUtility.SetDirty(clip); }
            if (clip.instructionSystem == null && ins != null) { clip.instructionSystem = ins; EditorUtility.SetDirty(clip); }
        }

        if (tray != null && tray.haptics == null && haptic != null)
        {
            tray.haptics = haptic; EditorUtility.SetDirty(tray);
        }

        if (station != null && station.haptics == null && haptic != null)
        {
            station.haptics = haptic; EditorUtility.SetDirty(station);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════

    static GameObject FindMesa()
    {
        foreach (var name in new[] { "Mesa_Explorador", "ExplorerTable", "Workbench_VR", "Mesa_VR" })
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        return null;
    }

    static T FindAnyObjectOfType<T>() where T : UnityEngine.Object
        => Object.FindAnyObjectByType<T>();

    static GameObject CreateRect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    static Color Col(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

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
            rt.anchorMin = new Vector2(ax0, ay0); rt.anchorMax = new Vector2(ax1, ay1);
            rt.sizeDelta = new Vector2(sw, sh);   rt.anchoredPosition = Vector2.zero;
        }
        Line("_BT", 0f, 1f, 1f, 1f,  0f, thickness);
        Line("_BB", 0f, 0f, 1f, 0f,  0f, thickness);
        Line("_BL", 0f, 0f, 0f, 1f,  thickness, 0f);
        Line("_BR", 1f, 0f, 1f, 1f,  thickness, 0f);
    }
}
#endif
