#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Ejecuta los 4 pasos del Reto 4 Setup de forma automática.
/// Auto-detecta GameObjects existentes en la escena por nombre/componente.
/// Tools > TITA > Reto 4 — Auto-Setup Completo
/// </summary>
public static class Reto4AutoSetup
{
    [MenuItem("Tools/TITA/Reto 4 — Auto-Setup Completo")]
    static void RunAll()
    {
        int created = 0;

        // ── Auto-detectar o crear el GO del Arduino ──────────────────────
        GameObject arduinoGO = FindGOByNameContains("Arduino")
                            ?? UnityEngine.Object.FindAnyObjectByType<ArduinoCore>()?.gameObject;

        if (arduinoGO == null)
        {
            arduinoGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arduinoGO.name = "Arduino_VR";
            arduinoGO.transform.localScale = new Vector3(0.06f, 0.02f, 0.09f);
            Undo.RegisterCreatedObjectUndo(arduinoGO, "Crear Arduino_VR");
            Debug.Log("[Reto4 AutoSetup] Arduino_VR creado como placeholder.");
            created++;
        }

        // ── Auto-detectar o crear el GO de la Protoboard ─────────────────
        GameObject protoboardGO = FindGOByNameContains("Protoboard")
                                ?? FindGOByNameContains("protoboard")
                                ?? UnityEngine.Object.FindAnyObjectByType<ProtoboardSimulator>()?.gameObject;

        if (protoboardGO == null)
        {
            protoboardGO = new GameObject("Protoboard_VR");
            Undo.RegisterCreatedObjectUndo(protoboardGO, "Crear Protoboard_VR");
            Debug.Log("[Reto4 AutoSetup] Protoboard_VR creado como placeholder.");
            created++;
        }

        // ── Auto-detectar o crear el Canvas del Técnico ───────────────────
        GameObject technicianCanvas = FindCanvasByName("Tecnico", "Technician", "TITA", "HUD");

        if (technicianCanvas == null)
        {
            technicianCanvas = new GameObject("Canvas_Tecnico_Reto4");
            Undo.RegisterCreatedObjectUndo(technicianCanvas, "Crear Canvas Técnico");
            var canvas = technicianCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            technicianCanvas.AddComponent<CanvasScaler>();
            technicianCanvas.AddComponent<GraphicRaycaster>();
            technicianCanvas.transform.localScale = Vector3.one * 0.001f;
            Debug.Log("[Reto4 AutoSetup] Canvas_Tecnico_Reto4 creado.");
            created++;
        }

        // ── Auto-detectar o crear la mesa del Explorador ──────────────────
        GameObject explorerTable = FindGOByNameContains("Mesa", "Table", "Workbench", "Explorador");

        if (explorerTable == null)
        {
            explorerTable = new GameObject("Mesa_Explorador");
            Undo.RegisterCreatedObjectUndo(explorerTable, "Crear Mesa Explorador");
            Debug.Log("[Reto4 AutoSetup] Mesa_Explorador creado como placeholder.");
            created++;
        }

        // ══ PASO 1 — Arduino Core + Nodos ════════════════════════════════
        Paso1_ConfigurarArduino(arduinoGO);

        // ══ PASO 2 — CircuitSimulator + Slots ════════════════════════════
        Paso2_ConfigurarProtoboard(protoboardGO);

        // ══ PASO 3 — UI del Técnico ═══════════════════════════════════════
        Paso3_CrearUI(technicianCanvas, protoboardGO);

        // ══ PASO 4 — Debug + CableBox + Botón de Validación ══════════════
        Paso4_DebugYBoton(explorerTable);

        // ── Conectar GameManager ──────────────────────────────────────────
        var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var sim = UnityEngine.Object.FindAnyObjectByType<ProtoboardSimulator>();
            if (sim != null)
            {
                var so   = new SerializedObject(gm);
                var prop = so.FindProperty("protoSim");   // campo renombrado en GameManager
                if (prop != null) { prop.objectReferenceValue = sim; so.ApplyModifiedProperties(); }
                EditorUtility.SetDirty(gm);
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string resumen = $"Auto-Setup Reto 4 completado.\n\n" +
            $"✅ Paso 1 — ArduinoCore + Nodos en '{arduinoGO.name}'\n" +
            $"✅ Paso 2 — CircuitSimulator + 50 slots en '{protoboardGO.name}'\n" +
            $"✅ Paso 3 — Panel IDE + Telemetría en '{technicianCanvas.name}'\n" +
            $"✅ Paso 4 — [DEBUG_SYSTEM] + CableBox_VR + ValidationButton_VR\n\n" +
            (created > 0 ? $"⚠ {created} GO(s) creados como placeholder — reposiciónalos en la escena.\n\n" : "") +
            "Pendiente manual:\n" +
            "  • Asignar cablePrefab en CableBoxSpawner\n" +
            "  • Asignar sfxPress/Pass/Fail en VRValidationButton\n" +
            "  • Conectar ArduinoIDEUI → ArduinoNetworkBridge en Inspector\n" +
            "  • Conectar TechnicianTelemetryUI → CircuitSimulator en Inspector";

        EditorUtility.DisplayDialog("Reto 4 — Auto-Setup Completo", resumen, "OK");
        Debug.Log("[Reto4 AutoSetup] ¡Listo! Abre Tools > TITA > Reto 4 — Configurar Arduino Sandbox pestaña 'Validar' para el checklist.");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 1
    // ═════════════════════════════════════════════════════════════════════
    static void Paso1_ConfigurarArduino(GameObject go)
    {
        Undo.RecordObject(go, "Paso1 Arduino");

        if (go.GetComponent<ArduinoCore>() == null)
            Undo.AddComponent<ArduinoCore>(go);
        if (go.GetComponent<ArduinoNetworkBridge>() == null)
            Undo.AddComponent<ArduinoNetworkBridge>(go);

        var core = go.GetComponent<ArduinoCore>();
        core.nodoP13 = GetOrCreateNodo(go, "Nodo_P13", new Vector3( 0.01f, 0,  0));
        core.nodoGND = GetOrCreateNodo(go, "Nodo_GND", new Vector3(-0.01f, 0,  0));
        core.nodoA0  = GetOrCreateNodo(go, "Nodo_A0",  new Vector3( 0,     0, -0.01f));

        EditorUtility.SetDirty(go);
        Debug.Log($"[Reto4 P1] ArduinoCore + Bridge + nodos listos en '{go.name}'.");
    }

    static ElectricalNode GetOrCreateNodo(GameObject parent, string nombre, Vector3 localPos)
    {
        var existing = parent.transform.Find(nombre);
        if (existing != null) return existing.GetComponent<ElectricalNode>();

        var nodeGO = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(nodeGO, $"Crear {nombre}");
        nodeGO.transform.SetParent(parent.transform, false);
        nodeGO.transform.localPosition = localPos;

        var node = nodeGO.AddComponent<ElectricalNode>();
        var col  = nodeGO.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.005f;
        return node;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 2
    // ═════════════════════════════════════════════════════════════════════
    static void Paso2_ConfigurarProtoboard(GameObject go)
    {
        var sim = go.GetComponent<ProtoboardSimulator>()
               ?? Undo.AddComponent<ProtoboardSimulator>(go);

        // Limpiar slots anteriores
        var oldRoot = go.transform.Find("[ProtoboardSlots]");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);

        var slotsRoot = new GameObject("[ProtoboardSlots]");
        Undo.RegisterCreatedObjectUndo(slotsRoot, "Generar ProtoboardSlots");
        slotsRoot.transform.SetParent(go.transform, false);

        const int FILAS = 10, COLS = 5;
        const float SPACING = 0.018f;
        var lista = new List<ProtoboardSlot>();

        for (int r = 0; r < FILAS; r++)
        {
            string railId = $"ROW_{(char)('A' + r)}";
            for (int c = 0; c < COLS; c++)
            {
                var slotGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                slotGO.name = $"Slot_{railId}_{c}";
                slotGO.transform.SetParent(slotsRoot.transform, false);
                slotGO.transform.localScale    = new Vector3(0.006f, 0.002f, 0.006f);
                slotGO.transform.localPosition = new Vector3(c * SPACING, 0.002f, -r * SPACING);

                var slot = slotGO.AddComponent<ProtoboardSlot>();
                slot.railId = railId;
                slot.row    = r;
                slot.col    = c;
                lista.Add(slot);
            }
        }

        // Rieles VCC y GND
        for (int c = 0; c < COLS; c++)
        {
            lista.Add(CrearSlotRiel(slotsRoot.transform, "VCC", 99, c,
                new Vector3(c * SPACING, 0.002f, -(FILAS + 1) * SPACING), Color.red));
            lista.Add(CrearSlotRiel(slotsRoot.transform, "GND", 100, c,
                new Vector3(c * SPACING, 0.002f, -(FILAS + 2) * SPACING), Color.black));
        }

        Undo.RecordObject(sim, "Asignar slots CircuitSimulator");
        sim.todosLosSlots = lista;
        EditorUtility.SetDirty(sim);
        Debug.Log($"[Reto4 P2] CircuitSimulator + {lista.Count} slots en '{go.name}'.");
    }

    static ProtoboardSlot CrearSlotRiel(Transform parent, string railId, int row, int col,
                                         Vector3 localPos, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"Slot_{railId}_{col}";
        go.transform.SetParent(parent, false);
        go.transform.localScale    = new Vector3(0.006f, 0.002f, 0.006f);
        go.transform.localPosition = localPos;
        var rend = go.GetComponent<Renderer>();
        if (rend) SetColor(rend, color);
        var slot = go.AddComponent<ProtoboardSlot>();
        slot.railId = railId;
        slot.row    = row;
        slot.col    = col;
        return slot;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 3
    // ═════════════════════════════════════════════════════════════════════
    static void Paso3_CrearUI(GameObject canvasGO, GameObject protoboardGO)
    {
        // ── Panel IDE ─────────────────────────────────────────────────────
        if (canvasGO.GetComponentInChildren<ArduinoIDEUI>(true) == null)
        {
            var panel = CrearUIPanel(canvasGO.transform, "Panel_ArduinoIDE",
                new Vector2(0f, 50f), new Vector2(380f, 260f));
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            CrearTMPLabel(panel.transform, "Lbl_TituloIDE", "// ARDUINO IDE",
                new Vector2(0, 110), new Vector2(360, 24), 14, Color.green, FontStyles.Bold);

            var ddPin   = CrearDropdown(panel.transform, "DD_Pin",   new Vector2(60,  72), new Vector2(200, 22));
            var ddMode  = CrearDropdown(panel.transform, "DD_Mode",  new Vector2(60,  40), new Vector2(200, 22));
            var ddState = CrearDropdown(panel.transform, "DD_State", new Vector2(60,   8), new Vector2(200, 22));
            var ddExtra = CrearDropdown(panel.transform, "DD_Extra", new Vector2(60, -24), new Vector2(200, 22));

            var codePreview = CrearTMPLabel(panel.transform, "Txt_CodePreview",
                "void setup() {\n  pinMode(13, OUTPUT);\n}\nvoid loop() {\n  digitalWrite(13, HIGH);\n}",
                new Vector2(0, -80), new Vector2(360, 80), 9, new Color(0.4f, 0.9f, 0.4f));

            var btnGO = CrearUIPanel(panel.transform, "Btn_Compilar",
                new Vector2(0, -122), new Vector2(200, 28));
            btnGO.AddComponent<Image>().color = new Color(0.1f, 0.5f, 0.1f);
            btnGO.AddComponent<Button>();
            CrearTMPLabel(btnGO.transform, "Txt_Btn", ">> COMPILAR Y ENVIAR",
                Vector2.zero, new Vector2(190, 24), 10, Color.white, FontStyles.Bold);

            var ide = panel.AddComponent<ArduinoIDEUI>();
            ide.dropdownPin    = ddPin;
            ide.dropdownMode   = ddMode;
            ide.dropdownState  = ddState;
            ide.dropdownExtra  = ddExtra;
            ide.btnCompilar    = btnGO.GetComponent<Button>();
            ide.txtCodePreview = codePreview;

            var bridge = UnityEngine.Object.FindAnyObjectByType<ArduinoNetworkBridge>();
            if (bridge != null) ide.bridge = bridge;

            EditorUtility.SetDirty(panel);
            Debug.Log("[Reto4 P3] Panel ArduinoIDEUI creado.");
        }

        // ── Panel Telemetría ──────────────────────────────────────────────
        if (canvasGO.GetComponentInChildren<TechnicianTelemetryUI>(true) == null)
        {
            var panel = CrearUIPanel(canvasGO.transform, "Panel_Telemetria",
                new Vector2(210f, -60f), new Vector2(200f, 240f));
            panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            CrearTMPLabel(panel.transform, "Lbl_TituloTele", "TELEMETRÍA",
                new Vector2(0, 100), new Vector2(190, 22), 12, Color.cyan, FontStyles.Bold);

            var lblV   = CrearTMPLabel(panel.transform, "Lbl_Voltaje",   "Voltaje:   -- V",    new Vector2(0, 72), new Vector2(190, 20), 10, Color.white);
            var lblI   = CrearTMPLabel(panel.transform, "Lbl_Corriente", "Corriente: -- mA",   new Vector2(0, 48), new Vector2(190, 20), 10, Color.white);
            var lblP   = CrearTMPLabel(panel.transform, "Lbl_Potencia",  "Potencia:  -- mW",   new Vector2(0, 24), new Vector2(190, 20), 10, Color.white);
            var lblAdc = CrearTMPLabel(panel.transform, "Lbl_ADC",       "Sensor A0: 0/1023",  new Vector2(0, 0),  new Vector2(190, 20), 10, Color.white);

            var alertGO  = CrearUIPanel(panel.transform, "Panel_Alerta", new Vector2(0, -48), new Vector2(190, 36));
            var alertImg = alertGO.AddComponent<Image>();
            alertImg.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            var lblAlerta = CrearTMPLabel(alertGO.transform, "Lbl_Alerta", "Circuito OK",
                Vector2.zero, new Vector2(185, 32), 10, Color.green, FontStyles.Bold);

            var tele = panel.AddComponent<TechnicianTelemetryUI>();
            tele.lblVoltaje   = lblV;
            tele.lblCorriente = lblI;
            tele.lblPotencia  = lblP;
            tele.lblAdc       = lblAdc;
            tele.lblAlerta    = lblAlerta;
            tele.panelAlerta  = alertImg;

            // tele.circuit (CircuitSimulator) se auto-detecta en runtime

            var bridge = UnityEngine.Object.FindAnyObjectByType<ArduinoNetworkBridge>();
            if (bridge != null) tele.arduinoBridge = bridge;

            EditorUtility.SetDirty(panel);
            Debug.Log("[Reto4 P3] Panel TechnicianTelemetryUI creado.");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 4
    // ═════════════════════════════════════════════════════════════════════
    static void Paso4_DebugYBoton(GameObject explorerTable)
    {
        // [DEBUG_SYSTEM]
        if (GameObject.Find("[DEBUG_SYSTEM]") == null)
        {
            var dbgGO   = new GameObject("[DEBUG_SYSTEM]");
            Undo.RegisterCreatedObjectUndo(dbgGO, "Crear DEBUG_SYSTEM");
            var skipper = dbgGO.AddComponent<DebugLevelSkipper>();
            var gm      = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                var so   = new SerializedObject(skipper);
                var prop = so.FindProperty("_gameManager");
                if (prop != null) { prop.objectReferenceValue = gm; so.ApplyModifiedProperties(); }
            }
            Debug.Log("[Reto4 P4] [DEBUG_SYSTEM] creado.");
        }

        // CableBox_VR
        if (GameObject.Find("CableBox_VR") == null)
        {
            var cableGO  = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cableGO.name = "CableBox_VR";
            Undo.RegisterCreatedObjectUndo(cableGO, "Crear CableBox_VR");
            cableGO.transform.SetParent(explorerTable.transform, false);
            cableGO.transform.localScale = new Vector3(0.1f, 0.06f, 0.08f);
            cableGO.GetComponent<BoxCollider>().isTrigger = true;
            var r = cableGO.GetComponent<Renderer>();
            if (r) SetColor(r, new Color(0.2f, 0.7f, 0.3f));
            cableGO.AddComponent<CableBoxSpawner>();
            cableGO.AddComponent<XRSimpleInteractable>();
            Debug.Log("[Reto4 P4] CableBox_VR creado.");
        }

        // ValidationButton_VR
        if (UnityEngine.Object.FindAnyObjectByType<VRValidationButton>() == null)
        {
            var root = new GameObject("ValidationButton_VR");
            Undo.RegisterCreatedObjectUndo(root, "Crear ValidationButton_VR");
            root.transform.SetParent(explorerTable.transform, false);
            root.transform.localPosition = new Vector3(0.15f, 0, 0);

            var colRoot = root.AddComponent<CapsuleCollider>();
            colRoot.isTrigger = true; colRoot.radius = 0.025f;
            colRoot.height = 0.05f;   colRoot.direction = 1;
            root.AddComponent<XRSimpleInteractable>();

            var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGO.name = "Button_Base";
            baseGO.transform.SetParent(root.transform, false);
            baseGO.transform.localScale    = new Vector3(0.05f, 0.012f, 0.05f);
            baseGO.transform.localPosition = new Vector3(0, 0.006f, 0);
            Object.DestroyImmediate(baseGO.GetComponent<Collider>());
            var bRend = baseGO.GetComponent<Renderer>();
            if (bRend) SetColor(bRend, new Color(0.2f, 0.2f, 0.2f));

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Button_Cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localScale    = new Vector3(0.042f, 0.01f, 0.042f);
            cap.transform.localPosition = new Vector3(0, 0.022f, 0);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            var capRend = cap.GetComponent<Renderer>();
            if (capRend) SetColor(capRend, new Color(0.1f, 0.4f, 0.9f));

            var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "LED_Indicator";
            led.transform.SetParent(cap.transform, false);
            led.transform.localScale    = Vector3.one * 0.15f;
            led.transform.localPosition = new Vector3(0, 1.2f, 0);
            Object.DestroyImmediate(led.GetComponent<Collider>());
            var ledRend = led.GetComponent<Renderer>();

            var btn = root.AddComponent<VRValidationButton>();
            btn.buttonCap   = cap.transform;
            btn.ledRenderer = ledRend;

            var haptics = UnityEngine.Object.FindAnyObjectByType<HapticFeedback>();
            if (haptics != null) btn.haptics = haptics;

            EditorUtility.SetDirty(root);
            Selection.activeGameObject = root;
            Debug.Log("[Reto4 P4] ValidationButton_VR creado.");
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers UI
    // ═════════════════════════════════════════════════════════════════════
    static GameObject CrearUIPanel(Transform parent, string nombre, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return go;
    }

    static TMP_Text CrearTMPLabel(Transform parent, string nombre, string texto,
        Vector2 anchoredPos, Vector2 size, int fontSize, Color color,
        FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static TMP_Dropdown CrearDropdown(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var dd = go.AddComponent<TMP_Dropdown>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lbl = labelGO.AddComponent<TextMeshProUGUI>();
        lbl.fontSize  = 9;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Left;
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin       = Vector2.zero;
        lrt.anchorMax       = Vector2.one;
        lrt.offsetMin       = new Vector2(4, 2);
        lrt.offsetMax       = new Vector2(-20, -2);
        dd.captionText = lbl;

        return dd;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers de búsqueda
    // ═════════════════════════════════════════════════════════════════════
    static GameObject FindGOByNameContains(params string[] terms)
    {
        var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var term in terms)
            foreach (var go in allGOs)
                if (go.name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return go;
        return null;
    }

    static GameObject FindCanvasByName(params string[] terms)
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var term in terms)
            foreach (var c in canvases)
                if (c.gameObject.name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return c.gameObject;
        return null;
    }

    // Crea una instancia de material nueva sin pasar por renderer.material (evita leak en Edit mode)
    static void SetColor(Renderer rend, Color color)
    {
        var mat = new Material(rend.sharedMaterial != null ? rend.sharedMaterial : Shader.Find("Universal Render Pipeline/Lit") ? new Material(Shader.Find("Universal Render Pipeline/Lit")) : new Material(Shader.Find("Standard")));
        mat.color = color;
        rend.sharedMaterial = mat;
    }
}
#endif
