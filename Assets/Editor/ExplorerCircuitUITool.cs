using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configura el UI de observación del circuito para el Explorador VR.
/// Menú: TITA → Setup Explorador: Circuit UI
///
/// Crea UN panel por zona de reto, parentado dentro de cada zona.
/// Cuando GameManager desactiva una zona, su panel se desactiva automáticamente.
/// Cada panel apunta al CircuitManager de su propia zona.
/// </summary>
public class ExplorerCircuitUITool : EditorWindow
{
    // ─────────────────────────────────────────────
    //  Parámetros configurables
    // ─────────────────────────────────────────────
    [Tooltip("Desplazamiento respecto al origen de cada zona (metros).")]
    Vector3 _panelOffset   = new Vector3(1.5f, 1.5f, 0f);
    Vector3 _panelRotation = new Vector3(0f, -90f, 0f);   // mirando hacia el jugador por defecto
    float   _panelScale    = 0.001f;
    float   _canvasWidth   = 500f;
    float   _canvasHeight  = 680f;
    bool    _replaceExisting = false;

    // ─────────────────────────────────────────────
    //  Ventana
    // ─────────────────────────────────────────────
    [MenuItem("TITA/Setup Explorador: Circuit UI")]
    static void Open() =>
        GetWindow<ExplorerCircuitUITool>("Explorer Circuit UI").minSize = new Vector2(390, 480);

    void OnGUI()
    {
        GUILayout.Label("Setup UI Circuito — Explorador VR", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Crea un panel por cada zona de reto (reto1Zone … reto4Zone).\n" +
            "Cada panel se activa/desactiva con su zona de forma automática.\n" +
            "También añade CircuitNodeDisplay a todos los ElectricalNodes.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Posición del panel (relativa al origen de cada zona)",
            EditorStyles.miniBoldLabel);

        _panelOffset   = EditorGUILayout.Vector3Field("Offset (m)",    _panelOffset);
        _panelRotation = EditorGUILayout.Vector3Field("Rotación (°)",  _panelRotation);
        _panelScale    = EditorGUILayout.FloatField  ("Escala WS",     _panelScale);
        _canvasWidth   = EditorGUILayout.FloatField  ("Ancho UI",      _canvasWidth);
        _canvasHeight  = EditorGUILayout.FloatField  ("Alto  UI",      _canvasHeight);

        EditorGUILayout.HelpBox(
            $"Tamaño real: {_canvasWidth  * _panelScale * 100f:F0} cm " +
            $"× {_canvasHeight * _panelScale * 100f:F0} cm\n" +
            "Rotación Y=-90° = panel mira hacia el eje -X (ajusta según la orientación de tu escena).",
            MessageType.None);

        EditorGUILayout.Space(4);
        _replaceExisting = EditorGUILayout.Toggle("Reemplazar paneles existentes", _replaceExisting);

        EditorGUILayout.Space(8);

        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
        if (GUILayout.Button("✓  Configurar todo", GUILayout.Height(36)))
            RunFullSetup();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Solo: NodeDisplays"))  SetupNodeDisplays();
            if (GUILayout.Button("Solo: Paneles x zona")) SetupPanelsPerZone();
        }
    }

    // ─────────────────────────────────────────────
    //  Entrada principal
    // ─────────────────────────────────────────────
    void RunFullSetup()
    {
        SetupNodeDisplays();
        int count = SetupPanelsPerZone();

        EditorUtility.DisplayDialog("¡Listo!",
            $"CircuitNodeDisplay añadido a todos los ElectricalNodes.\n" +
            $"{count} panel(es) ExplorerCircuitPanel creados (uno por zona).\n\n" +
            "Ajusta el offset del panel en esta ventana si la posición no es correcta.\n" +
            "Tip: selecciona un panel y muévelo en la escena con W.",
            "OK");
    }

    // ─────────────────────────────────────────────
    //  1. CircuitNodeDisplay en todos los nodos
    // ─────────────────────────────────────────────
    void SetupNodeDisplays()
    {
        var nodes = Object.FindObjectsByType<ElectricalNode>(
            FindObjectsInactive.Include);

        var ownerMap = BuildNodeOwnerMap();
        int added = 0, skipped = 0;

        foreach (var node in nodes)
        {
            if (node.GetComponent<CircuitNodeDisplay>() != null) { skipped++; continue; }

            Undo.RegisterFullObjectHierarchyUndo(node.gameObject, "Add CircuitNodeDisplay");
            var display  = Undo.AddComponent<CircuitNodeDisplay>(node.gameObject);
            display.nodeName = BuildNodeName(node, ownerMap);
            EditorUtility.SetDirty(node.gameObject);
            added++;
        }

        Debug.Log($"[CircuitUI] NodeDisplays — añadidos: {added}, ya existían: {skipped}");
    }

    Dictionary<ElectricalNode, ElectricalComponent> BuildNodeOwnerMap()
    {
        var map   = new Dictionary<ElectricalNode, ElectricalComponent>();
        var comps = Object.FindObjectsByType<ElectricalComponent>(
            FindObjectsInactive.Include);

        foreach (var c in comps)
        {
            if (c.nodeA != null && !map.ContainsKey(c.nodeA)) map[c.nodeA] = c;
            if (c.nodeB != null && !map.ContainsKey(c.nodeB)) map[c.nodeB] = c;
        }
        return map;
    }

    string BuildNodeName(ElectricalNode node,
                         Dictionary<ElectricalNode, ElectricalComponent> ownerMap)
    {
        if (!ownerMap.TryGetValue(node, out var owner))
            return node.gameObject.name;

        bool isA = (owner.nodeA == node);

        if (owner is VoltageSource)
            return isA ? "Fuente +" : "GND";

        string compName = owner.name
            .Replace("_Reto1","").Replace("_Reto2","")
            .Replace("_Reto3","").Replace("_Reto4","").Trim();

        return $"{compName} — Nodo {(isA ? "A" : "B")}";
    }

    // ─────────────────────────────────────────────
    //  2. Un panel por zona de reto
    // ─────────────────────────────────────────────
    int SetupPanelsPerZone()
    {
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró GameManager en la escena.\n" +
                "Asegúrate de que el GameManager esté presente antes de ejecutar esta herramienta.",
                "OK");
            return 0;
        }

        // Recopilar las 4 zonas
        var zones = new (GameObject zone, string label, LevelType level)[]
        {
            (gm.reto1Zone, "Reto 1 — Ley de Ohm",  LevelType.OhmLaw),
            (gm.reto2Zone, "Reto 2 — Paralelo",     LevelType.Parallel),
            (gm.reto3Zone, "Reto 3 — Mixto",        LevelType.Mixed),
            (gm.reto4Zone, "Reto 4 — Arduino",      LevelType.Arduino),
        };

        int count = 0;

        foreach (var (zone, label, _) in zones)
        {
            if (zone == null)
            {
                Debug.LogWarning($"[CircuitUI] Zona '{label}' no asignada en GameManager. Saltando.");
                continue;
            }

            // Buscar si ya existe un panel en esta zona
            var existing = zone.GetComponentInChildren<ExplorerCircuitPanel>(true);
            if (existing != null)
            {
                if (!_replaceExisting)
                {
                    Debug.Log($"[CircuitUI] '{label}' ya tiene panel. Activa 'Reemplazar' para rehacerlo.");
                    continue;
                }
                Undo.DestroyObjectImmediate(existing.transform.parent.gameObject);
            }

            // CircuitManager de esta zona
            var cm = zone.GetComponentInChildren<CircuitManager>(true);
            if (cm == null)
                Debug.LogWarning($"[CircuitUI] '{label}' no tiene CircuitManager hijo. El panel quedará sin referencia.");

            // Multímetro (único en escena)
            var meter = Object.FindAnyObjectByType<Multimeter>(FindObjectsInactive.Include);

            BuildPanel(zone.transform, label, cm, gm, meter);
            count++;
        }

        Debug.Log($"[CircuitUI] Paneles creados: {count}");
        return count;
    }

    // ─────────────────────────────────────────────
    //  Construir un panel individual
    // ─────────────────────────────────────────────
    void BuildPanel(Transform zoneRoot, string label,
                    CircuitManager cm, GameManager gm, Multimeter meter)
    {
        // ── Canvas raíz ──────────────────────────
        var canvasGO = new GameObject($"CircuitPanel_{label}");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create ExplorerCircuitPanel");
        canvasGO.transform.SetParent(zoneRoot, false);

        // Posición relativa al origen de la zona
        canvasGO.transform.localPosition    = _panelOffset;
        canvasGO.transform.localEulerAngles = _panelRotation;
        canvasGO.transform.localScale       = Vector3.one * _panelScale;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null) canvas.worldCamera = Camera.main;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(_canvasWidth, _canvasHeight);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ────────────────────────────────
        var bg = CreateUIImage(canvasGO, "Background",
            Vector2.zero, new Vector2(_canvasWidth, _canvasHeight));
        bg.color = new Color(0.05f, 0.05f, 0.12f, 0.90f);

        // ── Borde superior de color ───────────────
        var border = CreateUIImage(canvasGO, "TopBorder",
            new Vector2(0f, _canvasHeight * 0.5f - 6f), new Vector2(_canvasWidth, 6f));
        border.color = new Color(0.35f, 0.85f, 1f);

        // ── Título ───────────────────────────────
        var txtTitle = CreateTMP(canvasGO, "Title", label, 18, FontStyles.Bold,
            new Vector2(0f, _canvasHeight * 0.5f - 30f), new Vector2(_canvasWidth - 20f, 36f));
        txtTitle.alignment = TextAlignmentOptions.Center;
        txtTitle.color     = new Color(0.35f, 0.85f, 1f);

        // ── Banner falla ──────────────────────────
        var txtBanner = CreateTMP(canvasGO, "FaultBanner", "", 17, FontStyles.Bold,
            new Vector2(0f, _canvasHeight * 0.5f - 72f), new Vector2(_canvasWidth - 20f, 32f));
        txtBanner.alignment = TextAlignmentOptions.Center;
        txtBanner.color     = new Color(1f, 0.3f, 0.1f);

        // ── Separador ────────────────────────────
        CreateUIImage(canvasGO, "Sep1",
            new Vector2(0f, _canvasHeight * 0.5f - 95f), new Vector2(_canvasWidth - 20f, 1f))
            .color = new Color(1f, 1f, 1f, 0.12f);

        // ── LEDs (izquierda) ─────────────────────
        float colL = -_canvasWidth * 0.255f;
        float colR =  _canvasWidth * 0.255f;
        float topY =  _canvasHeight * 0.5f - 120f;

        CreateSectionLabel(canvasGO, "LEDs", colL, topY);
        var txtLEDs = CreateTMP(canvasGO, "LEDStates", "", 13, FontStyles.Normal,
            new Vector2(colL, topY - 100f), new Vector2(_canvasWidth * 0.46f, 185f));
        txtLEDs.alignment = TextAlignmentOptions.TopLeft;

        // ── Nodos (derecha) ───────────────────────
        CreateSectionLabel(canvasGO, "Nodos (V)", colR, topY);
        var txtNodes = CreateTMP(canvasGO, "NodeVoltages", "", 13, FontStyles.Normal,
            new Vector2(colR, topY - 100f), new Vector2(_canvasWidth * 0.46f, 185f));
        txtNodes.alignment = TextAlignmentOptions.TopLeft;

        // ── Separador central ────────────────────
        CreateUIImage(canvasGO, "Sep2",
            new Vector2(0f, topY - 115f), new Vector2(1f, 210f))
            .color = new Color(1f, 1f, 1f, 0.10f);

        // ── Resistencias ─────────────────────────
        float midY = topY - 220f;
        CreateSectionLabel(canvasGO, "Resistencias", 0f, midY);
        var txtRes = CreateTMP(canvasGO, "Resistors", "", 13, FontStyles.Normal,
            new Vector2(0f, midY - 65f), new Vector2(_canvasWidth - 20f, 105f));
        txtRes.alignment = TextAlignmentOptions.TopLeft;

        // ── Multímetro ───────────────────────────
        CreateUIImage(canvasGO, "Sep3",
            new Vector2(0f, midY - 195f), new Vector2(_canvasWidth - 20f, 1f))
            .color = new Color(1f, 1f, 1f, 0.12f);

        var txtMulti = CreateTMP(canvasGO, "Multimeter",
            "Multímetro: sin contacto", 14, FontStyles.Normal,
            new Vector2(0f, midY - 228f), new Vector2(_canvasWidth - 20f, 46f));
        txtMulti.alignment = TextAlignmentOptions.Center;
        txtMulti.color     = new Color(0.65f, 0.65f, 0.65f);

        // ── ExplorerCircuitPanel ──────────────────
        var panel = canvasGO.AddComponent<ExplorerCircuitPanel>();
        panel.txtFaultBanner  = txtBanner;
        panel.txtLEDStates    = txtLEDs;
        panel.txtNodeVoltages = txtNodes;
        panel.txtResistors    = txtRes;
        panel.txtMultimeter   = txtMulti;
        panel.gameManager     = gm;
        panel.circuitManager  = cm;    // fijo a esta zona
        panel.multimeter      = meter;

        EditorUtility.SetDirty(canvasGO);
    }

    // ─────────────────────────────────────────────
    //  Helpers de UI
    // ─────────────────────────────────────────────
    static Image CreateUIImage(GameObject parent, string name,
                               Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        var rt  = img.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return img;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
        string text, float fontSize, FontStyles style,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.fontStyle       = style;
        tmp.color           = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        var rt = tmp.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    static void CreateSectionLabel(GameObject parent, string text, float x, float y)
    {
        var lbl = CreateTMP(parent, $"Hdr_{text}", text, 15, FontStyles.Bold,
            new Vector2(x, y), new Vector2(220f, 26f));
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Left;

        // Línea bajo el encabezado
        var line = CreateUIImage(parent, $"Line_{text}",
            new Vector2(x, y - 17f), new Vector2(220f, 1f));
        line.color = new Color(0.35f, 0.85f, 1f, 0.5f);
    }
}
