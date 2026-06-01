#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


/// <summary>
/// Wizard de configuración completa para el Reto 4 — Arduino Sandbox.
/// Guía paso a paso los 4 pasos del PDF y valida cada uno.
/// Tools > TITA > Reto 4 — Configurar Arduino Sandbox
/// </summary>
public class Reto4SetupWizard : EditorWindow
{
    // ── Pestañas ──────────────────────────────────────────────────────────
    private int  _tab       = 0;
    private bool _showHelp  = false;
    private readonly string[] _tabNames = { "Paso 1", "Paso 2", "Paso 3", "Paso 4", "Validar" };

    // ── Referencias arrastrables ──────────────────────────────────────────
    private GameObject       _arduinoGO;
    private GameObject       _protoboardGO;
    private GameObject       _technicianCanvas;
    private GameObject       _explorerTable;
    private GameManager      _gameManager;

    // ── Scroll ────────────────────────────────────────────────────────────
    private Vector2 _scroll;

    // ── Colores ───────────────────────────────────────────────────────────
    private static readonly Color ColOK    = new Color(0.3f, 0.8f, 0.3f);
    private static readonly Color ColWarn  = new Color(0.9f, 0.7f, 0.1f);
    private static readonly Color ColError = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color ColInfo  = new Color(0.5f, 0.8f, 1f);

    [MenuItem("Tools/TITA/Reto 4 — Configurar Arduino Sandbox")]
    static void ShowWindow()
    {
        var w = GetWindow<Reto4SetupWizard>("Reto 4 Setup");
        w.minSize = new Vector2(420, 560);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Layout principal
    // ─────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        DrawHeader();
        _tab = GUILayout.Toolbar(_tab, _tabNames);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: DrawPaso1(); break;
            case 1: DrawPaso2(); break;
            case 2: DrawPaso3(); break;
            case 3: DrawPaso4(); break;
            case 4: DrawValidacion(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Cabecera
    // ─────────────────────────────────────────────────────────────────────
    void DrawHeader()
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("RETO 4 — Arduino Sandbox Setup", style);
        EditorGUILayout.LabelField("Configura la protoboard sandbox y el IDE del Técnico",
            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        EditorGUILayout.Space(6);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 1 — Arduino 3D (ArduinoCore + nodos)
    // ═════════════════════════════════════════════════════════════════════
    void DrawPaso1()
    {
        SectionHeader("Paso 1", "Configurar el Arduino 3D con nodos eléctricos",
            "Añade ArduinoCore + ArduinoNetworkBridge al GO del modelo 3D del Arduino " +
            "y crea tres hijos (Nodo_P13, Nodo_GND, Nodo_A0) con ElectricalNode + Trigger.");

        _arduinoGO = (GameObject)EditorGUILayout.ObjectField(
            "GO del Arduino 3D", _arduinoGO, typeof(GameObject), true);

        if (_arduinoGO == null)
        {
            HelpBox("Arrastra el GameObject del modelo 3D del Arduino desde la jerarquía.", ColInfo);
            return;
        }

        DrawStatusRow("ArduinoCore",          _arduinoGO.GetComponent<ArduinoCore>() != null);
        DrawStatusRow("ArduinoNetworkBridge", _arduinoGO.GetComponent<ArduinoNetworkBridge>() != null);
        DrawStatusRow("Nodo_P13",  FindChildNode(_arduinoGO, "Nodo_P13")  != null);
        DrawStatusRow("Nodo_GND",  FindChildNode(_arduinoGO, "Nodo_GND")  != null);
        DrawStatusRow("Nodo_A0",   FindChildNode(_arduinoGO, "Nodo_A0")   != null);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-configurar Arduino", GUILayout.Height(32)))
                ConfigurarArduinoGO();

            if (GUILayout.Button("Solo crear nodos", GUILayout.Height(32), GUILayout.Width(120)))
                CrearNodosArduino(_arduinoGO);
        }

        EditorGUILayout.Space(6);
        _showHelp = EditorGUILayout.Foldout(_showHelp, "Instrucciones manuales");
        if (_showHelp)
        {
            var style = EditorStyles.helpBox;
            EditorGUILayout.TextArea(
                "1. Selecciona el GO del Arduino 3D en la jerarquía.\n" +
                "2. Add Component → ArduinoCore.\n" +
                "3. Add Component → ArduinoNetworkBridge.\n" +
                "4. Crear hijo vacío → nombre: Nodo_P13 → Add Component: ElectricalNode.\n" +
                "   Añadir SphereCollider (isTrigger = true, radius = 0.005).\n" +
                "5. Repetir para Nodo_GND y Nodo_A0.\n" +
                "6. En ArduinoCore (Inspector): arrastrar nodoP13, nodoGND, nodoA0.", style);
        }
    }

    void ConfigurarArduinoGO()
    {
        if (_arduinoGO == null) return;
        Undo.RecordObject(_arduinoGO, "Configurar Arduino GO");

        // Scripts
        if (_arduinoGO.GetComponent<ArduinoCore>() == null)
            Undo.AddComponent<ArduinoCore>(_arduinoGO);

        if (_arduinoGO.GetComponent<ArduinoNetworkBridge>() == null)
            Undo.AddComponent<ArduinoNetworkBridge>(_arduinoGO);

        // Nodos
        var core = _arduinoGO.GetComponent<ArduinoCore>();
        core.nodoP13 = CrearNodoPin(_arduinoGO, "Nodo_P13", new Vector3( 0.01f, 0,  0));
        core.nodoGND = CrearNodoPin(_arduinoGO, "Nodo_GND", new Vector3(-0.01f, 0,  0));
        core.nodoA0  = CrearNodoPin(_arduinoGO, "Nodo_A0",  new Vector3( 0,     0, -0.01f));

        EditorUtility.SetDirty(_arduinoGO);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Paso 1 Completado",
            "ArduinoCore + ArduinoNetworkBridge añadidos.\n" +
            "Nodo_P13, Nodo_GND y Nodo_A0 creados con ElectricalNode + Trigger.\n\n" +
            "⚠ Reposiciona los nodos en el Inspector para alinearlos con los pines físicos del modelo.",
            "OK");
    }

    void CrearNodosArduino(GameObject parent)
    {
        if (parent == null) return;
        var core = parent.GetComponent<ArduinoCore>();
        var p13  = CrearNodoPin(parent, "Nodo_P13", new Vector3( 0.01f, 0,  0));
        var gnd  = CrearNodoPin(parent, "Nodo_GND", new Vector3(-0.01f, 0,  0));
        var a0   = CrearNodoPin(parent, "Nodo_A0",  new Vector3( 0,     0, -0.01f));
        if (core != null) { core.nodoP13 = p13; core.nodoGND = gnd; core.nodoA0 = a0; }
        EditorUtility.SetDirty(parent);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reto4] Nodos creados: Nodo_P13, Nodo_GND, Nodo_A0");
    }

    ElectricalNode CrearNodoPin(GameObject parent, string nombre, Vector3 localPos)
    {
        // Reutilizar si ya existe
        var existing = FindChildNode(parent, nombre);
        if (existing != null) return existing;

        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {nombre}");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;

        var node = go.AddComponent<ElectricalNode>();

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.005f;

        return node;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 2 — Protoboard Sandbox (CircuitSimulator + Slots)
    // ═════════════════════════════════════════════════════════════════════
    void DrawPaso2()
    {
        SectionHeader("Paso 2", "Generar la Matriz Sandbox de la Protoboard",
            "Añade CircuitSimulator al GO padre de la protoboard y genera la cuadrícula " +
            "magnética de ProtoboardSlots usando el generador de Editor.");

        _protoboardGO = (GameObject)EditorGUILayout.ObjectField(
            "GO Padre de la Protoboard", _protoboardGO, typeof(GameObject), true);

        if (_protoboardGO == null)
        {
            HelpBox("Arrastra el GameObject del modelo 3D de la protoboard.", ColInfo);
            return;
        }

        var sim = _protoboardGO.GetComponent<ProtoboardSimulator>();
        DrawStatusRow("ProtoboardSimulator", sim != null);
        DrawStatusRow("Slots generados",  sim != null && sim.todosLosSlots?.Count > 0,
                      sim != null ? $"{sim.todosLosSlots?.Count ?? 0} slots" : "0 slots");

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Añadir ProtoboardSimulator", GUILayout.Height(32)))
                AnadirCircuitSimulator();

            if (GUILayout.Button("Abrir Generador de Slots", GUILayout.Height(32)))
                GetWindow<ProtoboardSlotGenerator>("Generador Protoboard");
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Generar 10×5 (preset rápido)", GUILayout.Height(28)))
            GenerarSlotsRapido(10, 5);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "La cuadrícula magnética crea la protoboard virtual donde el Explorador\n" +
            "conecta los cables. Slots con el mismo railId comparten nodo eléctrico\n" +
            "(igual que las tiras de cobre de una protoboard real).",
            MessageType.Info);
    }

    void AnadirCircuitSimulator()
    {
        if (_protoboardGO == null) return;
        if (_protoboardGO.GetComponent<ProtoboardSimulator>() != null)
        {
            EditorUtility.DisplayDialog("Info", "ProtoboardSimulator ya existe en este GO.", "OK");
            return;
        }
        Undo.AddComponent<ProtoboardSimulator>(_protoboardGO);
        EditorUtility.SetDirty(_protoboardGO);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reto4] ProtoboardSimulator añadido a " + _protoboardGO.name);
    }

    void GenerarSlotsRapido(int filas, int cols)
    {
        if (_protoboardGO == null) return;

        var sim = _protoboardGO.GetComponent<ProtoboardSimulator>();
        if (sim == null) sim = Undo.AddComponent<ProtoboardSimulator>(_protoboardGO);

        // Limpiar anteriores
        var oldRoot = _protoboardGO.transform.Find("[ProtoboardSlots]");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);

        var root = new GameObject("[ProtoboardSlots]");
        Undo.RegisterCreatedObjectUndo(root, "Generar slots");
        root.transform.SetParent(_protoboardGO.transform, false);

        float spacing = 0.018f;
        var lista = new List<ProtoboardSlot>();

        for (int r = 0; r < filas; r++)
        {
            string railId = $"ROW_{(char)('A' + r)}";
            for (int c = 0; c < cols; c++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"Slot_{railId}_{c}";
                go.transform.SetParent(root.transform, false);
                go.transform.localScale    = new Vector3(0.006f, 0.002f, 0.006f);
                go.transform.localPosition = new Vector3(c * spacing, 0.002f, -r * spacing);

                var col = go.GetComponent<Collider>();
                if (col != null) { col.isTrigger = false; }

                var slot = go.AddComponent<ProtoboardSlot>();
                slot.railId = railId;
                slot.row    = r;
                slot.col    = c;
                lista.Add(slot);
            }
        }

        // Rieles VCC / GND
        for (int c = 0; c < cols; c++)
        {
            lista.Add(CrearSlotRiel(root.transform, "VCC", 99, c,
                new Vector3(c * spacing, 0.002f, -(filas + 1) * spacing), Color.red));
            lista.Add(CrearSlotRiel(root.transform, "GND", 100, c,
                new Vector3(c * spacing, 0.002f, -(filas + 2) * spacing), Color.black));
        }

        Undo.RecordObject(sim, "Asignar slots");
        sim.todosLosSlots = lista;
        EditorUtility.SetDirty(sim);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Paso 2 Completado",
            $"Cuadrícula generada: {filas} filas × {cols} columnas ({lista.Count} slots).\n" +
            "Rieles VCC (rojo) y GND (negro) añadidos.\n\n" +
            "Ajusta la posición del root '[ProtoboardSlots]' para alinearla con el modelo 3D.",
            "OK");
    }

    ProtoboardSlot CrearSlotRiel(Transform parent, string railId, int row, int col,
                                  Vector3 localPos, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"Slot_{railId}_{col}";
        go.transform.SetParent(parent, false);
        go.transform.localScale    = new Vector3(0.006f, 0.002f, 0.006f);
        go.transform.localPosition = localPos;

        var rend = go.GetComponent<Renderer>();
        if (rend) rend.material.color = color;

        var slot = go.AddComponent<ProtoboardSlot>();
        slot.railId = railId;
        slot.row    = row;
        slot.col    = col;
        return slot;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 3 — UI del Técnico (ArduinoIDEUI + TechnicianTelemetryUI)
    // ═════════════════════════════════════════════════════════════════════
    void DrawPaso3()
    {
        SectionHeader("Paso 3", "Diseñar la UI del Técnico en PC",
            "Crea el panel IDE de Arduino (dropdowns) y el panel de telemetría " +
            "(voltaje, corriente, potencia, alertas) en el Canvas del Técnico.");

        _technicianCanvas = (GameObject)EditorGUILayout.ObjectField(
            "Canvas del Técnico", _technicianCanvas, typeof(GameObject), true);

        if (_technicianCanvas == null)
        {
            HelpBox("Arrastra el Canvas WorldSpace del Técnico.", ColInfo);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Crear Canvas Técnico nuevo", GUILayout.Height(28)))
                CrearCanvasTecnico();
            return;
        }

        var ide  = _technicianCanvas.GetComponentInChildren<ArduinoIDEUI>(true);
        var tele = _technicianCanvas.GetComponentInChildren<TechnicianTelemetryUI>(true);
        DrawStatusRow("Panel ArduinoIDEUI",          ide  != null);
        DrawStatusRow("Panel TechnicianTelemetryUI", tele != null);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Crear Panel IDE Arduino", GUILayout.Height(32)))
                CrearPanelIDE(_technicianCanvas);

            if (GUILayout.Button("Crear Panel Telemetría", GUILayout.Height(32)))
                CrearPanelTelemetria(_technicianCanvas);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Crear ambos paneles", GUILayout.Height(32)))
        {
            CrearPanelIDE(_technicianCanvas);
            CrearPanelTelemetria(_technicianCanvas);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Los paneles se crean como hijos del Canvas. Posiciónalos y ajusta los\n" +
            "Dropdowns manualmente en el Inspector según el layout de tu UI.\n" +
            "Los TMP_Text se auto-detectan por nombre al entrar en Play Mode.",
            MessageType.Info);
    }

    void CrearCanvasTecnico()
    {
        var canvasGO = new GameObject("Canvas_Tecnico_Reto4");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Crear Canvas Técnico");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.transform.localScale = Vector3.one * 0.001f;
        _technicianCanvas = canvasGO;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    void CrearPanelIDE(GameObject canvas)
    {
        if (canvas == null) return;
        if (canvas.GetComponentInChildren<ArduinoIDEUI>(true) != null)
        {
            EditorUtility.DisplayDialog("Info", "ArduinoIDEUI ya existe en el Canvas.", "OK");
            return;
        }

        // Panel raíz
        var panelGO = CrearUIPanel(canvas.transform, "Panel_ArduinoIDE",
            new Vector2(0f, 50f), new Vector2(380f, 260f));

        // Fondo negro
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Título
        CrearTMPLabel(panelGO.transform, "Lbl_TituloIDE", "// ARDUINO IDE",
            new Vector2(0, 110), new Vector2(360, 24), 14, Color.green, FontStyles.Bold);

        // Dropdowns
        CrearTMPLabel(panelGO.transform, "Lbl_Pin",   "Pin:",   new Vector2(-120, 72), new Vector2(60, 20),  10, Color.white);
        CrearTMPLabel(panelGO.transform, "Lbl_Modo",  "Modo:",  new Vector2(-120, 40), new Vector2(60, 20),  10, Color.white);
        CrearTMPLabel(panelGO.transform, "Lbl_State", "Estado:", new Vector2(-120, 8), new Vector2(60, 20),  10, Color.white);
        CrearTMPLabel(panelGO.transform, "Lbl_Extra", "Bucle:", new Vector2(-120,-24), new Vector2(60, 20),  10, Color.white);

        var ddPin   = CrearDropdown(panelGO.transform, "DD_Pin",   new Vector2(60,  72), new Vector2(200, 22));
        var ddMode  = CrearDropdown(panelGO.transform, "DD_Mode",  new Vector2(60,  40), new Vector2(200, 22));
        var ddState = CrearDropdown(panelGO.transform, "DD_State", new Vector2(60,   8), new Vector2(200, 22));
        var ddExtra = CrearDropdown(panelGO.transform, "DD_Extra", new Vector2(60, -24), new Vector2(200, 22));

        // TextArea código
        CrearTMPLabel(panelGO.transform, "Txt_CodePreview",
            "void setup() {\n  pinMode(13, OUTPUT);\n}\nvoid loop() {\n  digitalWrite(13, HIGH);\n}",
            new Vector2(0, -80), new Vector2(360, 80), 9, new Color(0.4f, 0.9f, 0.4f));

        // Botón
        var btnGO = CrearUIPanel(panelGO.transform, "Btn_Compilar",
            new Vector2(0, -122), new Vector2(200, 28));
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.5f, 0.1f);
        btnGO.AddComponent<Button>();
        CrearTMPLabel(btnGO.transform, "Txt_Btn", ">> COMPILAR Y ENVIAR",
            Vector2.zero, new Vector2(190, 24), 10, Color.white, FontStyles.Bold);

        // Script ArduinoIDEUI
        var ide = panelGO.AddComponent<ArduinoIDEUI>();
        ide.dropdownPin   = ddPin;
        ide.dropdownMode  = ddMode;
        ide.dropdownState = ddState;
        ide.dropdownExtra = ddExtra;
        ide.btnCompilar   = btnGO.GetComponent<Button>();
        ide.txtCodePreview = panelGO.transform.Find("Txt_CodePreview")?.GetComponent<TMP_Text>();

        EditorUtility.SetDirty(panelGO);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reto4] Panel ArduinoIDEUI creado en " + canvas.name);
    }

    void CrearPanelTelemetria(GameObject canvas)
    {
        if (canvas == null) return;
        if (canvas.GetComponentInChildren<TechnicianTelemetryUI>(true) != null)
        {
            EditorUtility.DisplayDialog("Info", "TechnicianTelemetryUI ya existe en el Canvas.", "OK");
            return;
        }

        var panelGO = CrearUIPanel(canvas.transform, "Panel_Telemetria",
            new Vector2(210f, -60f), new Vector2(200f, 240f));

        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        CrearTMPLabel(panelGO.transform, "Lbl_TituloTele", "TELEMETRÍA",
            new Vector2(0, 100), new Vector2(190, 22), 12, Color.cyan, FontStyles.Bold);

        var lblV   = CrearTMPLabel(panelGO.transform, "Lbl_Voltaje",   "Voltaje:   -- V",
            new Vector2(0, 72), new Vector2(190, 20), 10, Color.white);
        var lblI   = CrearTMPLabel(panelGO.transform, "Lbl_Corriente", "Corriente: -- mA",
            new Vector2(0, 48), new Vector2(190, 20), 10, Color.white);
        var lblP   = CrearTMPLabel(panelGO.transform, "Lbl_Potencia",  "Potencia:  -- mW",
            new Vector2(0, 24), new Vector2(190, 20), 10, Color.white);
        var lblAdc = CrearTMPLabel(panelGO.transform, "Lbl_ADC",       "Sensor A0: 0/1023",
            new Vector2(0, 0), new Vector2(190, 20), 10, Color.white);

        // Panel de alerta
        var alertGO = CrearUIPanel(panelGO.transform, "Panel_Alerta",
            new Vector2(0, -48), new Vector2(190, 36));
        var alertImg = alertGO.AddComponent<Image>();
        alertImg.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
        var lblAlerta = CrearTMPLabel(alertGO.transform, "Lbl_Alerta", "Circuito OK",
            Vector2.zero, new Vector2(185, 32), 10, Color.green, FontStyles.Bold);

        // Script
        var tele = panelGO.AddComponent<TechnicianTelemetryUI>();
        tele.lblVoltaje   = lblV;
        tele.lblCorriente = lblI;
        tele.lblPotencia  = lblP;
        tele.lblAdc       = lblAdc;
        tele.lblAlerta    = lblAlerta;
        tele.panelAlerta  = alertImg;

        EditorUtility.SetDirty(panelGO);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reto4] Panel TechnicianTelemetryUI creado en " + canvas.name);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PASO 4 — Herramientas de Debug y Prueba Asimétrica
    // ═════════════════════════════════════════════════════════════════════
    void DrawPaso4()
    {
        SectionHeader("Paso 4", "Enlace y Pruebas Asimétricas (F4 Skip)",
            "Crea el sistema de debug, crea la CableBox del Explorador y activa el " +
            "modo de prueba del GameManager para saltar al Reto 4 con F4.");

        _gameManager = (GameManager)EditorGUILayout.ObjectField(
            "GameManager", _gameManager, typeof(GameManager), true);
        _explorerTable = (GameObject)EditorGUILayout.ObjectField(
            "Mesa del Explorador (VR)", _explorerTable, typeof(GameObject), true);

        if (_gameManager == null)
            _gameManager = FindAnyObjectByType<GameManager>();

        EditorGUILayout.Space(4);

        var debugSys    = GameObject.Find("[DEBUG_SYSTEM]");
        var cableBox    = GameObject.Find("CableBox_VR");
        var btnVR       = FindAnyObjectByType<VRValidationButton>();
        var debugMode   = GetDebugMode();

        DrawStatusRow("[DEBUG_SYSTEM]",              debugSys  != null);
        DrawStatusRow("CableBox_VR",                 cableBox  != null);
        DrawStatusRow("Botón VRValidationButton",    btnVR     != null);
        DrawStatusRow("GameManager Debug Mode",      debugMode);
        DrawStatusRow("DebugLevelSkipper",
            debugSys?.GetComponent<DebugLevelSkipper>() != null);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Crear [DEBUG_SYSTEM]", GUILayout.Height(32)))
                CrearDebugSystem();
            if (GUILayout.Button("Crear CableBox VR", GUILayout.Height(32)))
                CrearCableBox();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Crear Botón de Validación VR", GUILayout.Height(32)))
            CrearBotonValidacion();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Activar Debug Mode", GUILayout.Height(28)))
                SetDebugMode(true);
            if (GUILayout.Button("Desactivar Debug Mode", GUILayout.Height(28)))
                SetDebugMode(false);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "PRUEBA DE FUEGO:\n" +
            "1. Build PC → Técnico  |  Editor Play → Explorador (Quest)\n" +
            "2. Unirse a la sala de red\n" +
            "3. Presionar F4 → salta directo al Reto 4\n" +
            "4. Técnico programa el Arduino desde ArduinoIDEUI\n" +
            "5. Explorador cablea la protoboard y presiona el botón físico",
            MessageType.Info);
    }

    void CrearDebugSystem()
    {
        var existing = GameObject.Find("[DEBUG_SYSTEM]");
        if (existing != null)
        {
            bool ok = EditorUtility.DisplayDialog("[DEBUG_SYSTEM]",
                "Ya existe. ¿Reemplazarlo?", "Sí", "No");
            if (!ok) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var go = new GameObject("[DEBUG_SYSTEM]");
        Undo.RegisterCreatedObjectUndo(go, "Crear DEBUG_SYSTEM");

        var skipper = go.AddComponent<DebugLevelSkipper>();

        if (_gameManager != null)
        {
            // Asignar via serialización
            var so   = new SerializedObject(skipper);
            var prop = so.FindProperty("_gameManager");
            if (prop != null) { prop.objectReferenceValue = _gameManager; so.ApplyModifiedProperties(); }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reto4] [DEBUG_SYSTEM] creado con DebugLevelSkipper.");
    }

    void CrearCableBox()
    {
        if (GameObject.Find("CableBox_VR") != null)
        {
            EditorUtility.DisplayDialog("Info", "CableBox_VR ya existe en la escena.", "OK");
            return;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "CableBox_VR";
        Undo.RegisterCreatedObjectUndo(go, "Crear CableBox VR");

        go.transform.localScale = new Vector3(0.1f, 0.06f, 0.08f);

        if (_explorerTable != null)
            go.transform.SetParent(_explorerTable.transform, false);

        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;

        var rend = go.GetComponent<Renderer>();
        if (rend) rend.material.color = new Color(0.2f, 0.7f, 0.3f);

        go.AddComponent<CableBoxSpawner>();
        go.AddComponent<XRSimpleInteractable>();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("CableBox VR creada",
            "CableBox_VR creada con CableBoxSpawner y XRSimpleInteractable.\n\n" +
            "⚠ Asigna el prefab del cable en CableBoxSpawner → cablePrefab.\n" +
            "  El prefab debe tener: XRGrabInteractable + Rigidbody + Collider.",
            "OK");
    }

    void CrearBotonValidacion()
    {
        if (Object.FindAnyObjectByType<VRValidationButton>() != null)
        {
            EditorUtility.DisplayDialog("Info", "Ya existe un VRValidationButton en la escena.", "OK");
            return;
        }

        // ── GO raíz del botón ────────────────────────────────────────────
        var root = new GameObject("ValidationButton_VR");
        Undo.RegisterCreatedObjectUndo(root, "Crear Botón Validación");

        if (_explorerTable != null)
            root.transform.SetParent(_explorerTable.transform, false);

        // CapsuleCollider trigger (detección de mano)
        var colRoot = root.AddComponent<CapsuleCollider>();
        colRoot.isTrigger = true;
        colRoot.radius    = 0.025f;
        colRoot.height    = 0.05f;
        colRoot.direction = 1; // Y

        // XRSimpleInteractable
        root.AddComponent<XRSimpleInteractable>();

        // ── Base del botón ───────────────────────────────────────────────
        var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseGO.name = "Button_Base";
        Undo.RegisterCreatedObjectUndo(baseGO, "Crear Button_Base");
        baseGO.transform.SetParent(root.transform, false);
        baseGO.transform.localScale    = new Vector3(0.05f, 0.012f, 0.05f);
        baseGO.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        DestroyImmediate(baseGO.GetComponent<Collider>());
        var baseRend = baseGO.GetComponent<Renderer>();
        if (baseRend) baseRend.material.color = new Color(0.2f, 0.2f, 0.2f);

        // ── Capuchón animado ─────────────────────────────────────────────
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Button_Cap";
        Undo.RegisterCreatedObjectUndo(cap, "Crear Button_Cap");
        cap.transform.SetParent(root.transform, false);
        cap.transform.localScale    = new Vector3(0.042f, 0.01f, 0.042f);
        cap.transform.localPosition = new Vector3(0f, 0.022f, 0f);
        DestroyImmediate(cap.GetComponent<Collider>());
        var capRend = cap.GetComponent<Renderer>();
        if (capRend) capRend.material.color = new Color(0.1f, 0.4f, 0.9f);

        // ── LED indicador ────────────────────────────────────────────────
        var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        led.name = "LED_Indicator";
        Undo.RegisterCreatedObjectUndo(led, "Crear LED");
        led.transform.SetParent(cap.transform, false);
        led.transform.localScale    = Vector3.one * 0.15f;
        led.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        DestroyImmediate(led.GetComponent<Collider>());
        var ledRend = led.GetComponent<Renderer>();

        // ── Script principal ─────────────────────────────────────────────
        var btn = root.AddComponent<VRValidationButton>();
        btn.buttonCap   = cap.transform;
        btn.ledRenderer = ledRend;

        // Vincular HapticFeedback si hay uno en escena
        var haptics = FindAnyObjectByType<HapticFeedback>();
        if (haptics != null) btn.haptics = haptics;

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog("Botón de Validación VR creado",
            "ValidationButton_VR creado con:\n" +
            "  • Button_Base  (cilindro gris — cuerpo fijo)\n" +
            "  • Button_Cap   (cilindro azul — se anima al presionar)\n" +
            "  • LED_Indicator (esfera — cambia de color con el resultado)\n" +
            "  • VRValidationButton + XRSimpleInteractable\n\n" +
            "⚠ Posiciona el GO sobre la mesa del Explorador, cerca de la protoboard.\n" +
            "  Asigna sfxPress/sfxPass/sfxFail en Inspector si tienes AudioClips.",
            "OK");
    }

    bool GetDebugMode()
    {
        if (_gameManager == null) return false;
        var so   = new SerializedObject(_gameManager);
        var prop = so.FindProperty("_debugMode");
        return prop != null && prop.boolValue;
    }

    void SetDebugMode(bool value)
    {
        if (_gameManager == null) { Debug.LogWarning("[Reto4] GameManager no encontrado."); return; }
        var so   = new SerializedObject(_gameManager);
        var prop = so.FindProperty("_debugMode");
        if (prop != null) { prop.boolValue = value; so.ApplyModifiedProperties(); }
        EditorUtility.SetDirty(_gameManager);
        Debug.Log($"[Reto4] GameManager Debug Mode → {value}");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  VALIDACIÓN — Checklist completo
    // ═════════════════════════════════════════════════════════════════════
    void DrawValidacion()
    {
        SectionHeader("Validación", "Checklist completo del Reto 4", null);

        var checks = BuildChecklist();
        int ok = checks.Count(c => c.status == CheckStatus.OK);
        int total = checks.Count;

        // Barra de progreso
        EditorGUILayout.LabelField($"Progreso: {ok}/{total} items configurados");
        var rect = GUILayoutUtility.GetRect(18, 16, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(rect, (float)ok / total,
            ok == total ? "¡Listo para pruebas!" : $"{ok}/{total}");

        EditorGUILayout.Space(8);

        foreach (var c in checks)
        {
            Color color = c.status switch
            {
                CheckStatus.OK      => ColOK,
                CheckStatus.Warning => ColWarn,
                _                   => ColError
            };
            string icon = c.status switch
            {
                CheckStatus.OK      => "✅",
                CheckStatus.Warning => "⚠",
                _                   => "❌"
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                var labelStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
                EditorGUILayout.LabelField($"{icon} {c.name}", labelStyle, GUILayout.Width(260));
                if (!string.IsNullOrEmpty(c.detail))
                    EditorGUILayout.LabelField(c.detail,
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
            }
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Actualizar validación", GUILayout.Height(28)))
            Repaint();

        if (ok == total)
        {
            EditorGUILayout.HelpBox(
                "¡Configuración completa! El Reto 4 está listo.\n" +
                "Recuerda asignar el prefab del cable en CableBox_VR → CableBoxSpawner.",
                MessageType.None);
        }
    }

    List<CheckItem> BuildChecklist()
    {
        var list = new List<CheckItem>();

        // Paso 1 — Arduino
        var arduinoAny = FindAnyObjectByType<ArduinoCore>();
        list.Add(new CheckItem("ArduinoCore en escena",
            arduinoAny != null ? CheckStatus.OK : CheckStatus.Error,
            arduinoAny?.gameObject.name ?? "—"));

        var bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
        list.Add(new CheckItem("ArduinoNetworkBridge en escena",
            bridge != null ? CheckStatus.OK : CheckStatus.Error,
            bridge?.gameObject.name ?? "—"));

        if (arduinoAny != null)
        {
            list.Add(new CheckItem("nodoP13 asignado",
                arduinoAny.nodoP13 != null ? CheckStatus.OK : CheckStatus.Error));
            list.Add(new CheckItem("nodoGND asignado",
                arduinoAny.nodoGND != null ? CheckStatus.OK : CheckStatus.Error));
            list.Add(new CheckItem("nodoA0 asignado",
                arduinoAny.nodoA0  != null ? CheckStatus.OK : CheckStatus.Error));
        }

        // Paso 2 — ProtoboardSimulator
        var sim = FindAnyObjectByType<ProtoboardSimulator>();
        list.Add(new CheckItem("ProtoboardSimulator en escena",
            sim != null ? CheckStatus.OK : CheckStatus.Error,
            sim?.gameObject.name ?? "—"));

        if (sim != null)
        {
            int slotCount = sim.todosLosSlots?.Count ?? 0;
            list.Add(new CheckItem("Slots generados",
                slotCount > 0 ? CheckStatus.OK : CheckStatus.Error,
                $"{slotCount} slots"));
        }

        // Paso 3 — UI
        var ide  = FindAnyObjectByType<ArduinoIDEUI>();
        var tele = FindAnyObjectByType<TechnicianTelemetryUI>();
        list.Add(new CheckItem("ArduinoIDEUI en escena",
            ide  != null ? CheckStatus.OK : CheckStatus.Warning));
        list.Add(new CheckItem("TechnicianTelemetryUI en escena",
            tele != null ? CheckStatus.OK : CheckStatus.Warning));

        if (ide  != null) list.Add(new CheckItem("IDE → bridge asignado",
            ide.bridge != null ? CheckStatus.OK : CheckStatus.Warning));
        if (tele != null) list.Add(new CheckItem("TechnicianTelemetryUI en escena",
            CheckStatus.OK, tele.gameObject.name));

        // Paso 4 — Debug
        var debugSys = GameObject.Find("[DEBUG_SYSTEM]");
        list.Add(new CheckItem("[DEBUG_SYSTEM] en escena",
            debugSys != null ? CheckStatus.OK : CheckStatus.Warning));

        var cableBox = FindAnyObjectByType<CableBoxSpawner>();
        list.Add(new CheckItem("CableBoxSpawner en escena",
            cableBox != null ? CheckStatus.OK : CheckStatus.Warning));

        if (cableBox != null) list.Add(new CheckItem("CableBoxSpawner → cablePrefab asignado",
            cableBox.cablePrefab != null ? CheckStatus.OK : CheckStatus.Warning,
            cableBox.cablePrefab == null ? "Asignar en Inspector" : "OK"));

        var btnVR = FindAnyObjectByType<VRValidationButton>();
        list.Add(new CheckItem("VRValidationButton en escena",
            btnVR != null ? CheckStatus.OK : CheckStatus.Error,
            btnVR?.gameObject.name ?? "—"));

        if (btnVR != null)
        {
            list.Add(new CheckItem("Botón → buttonCap asignado",
                btnVR.buttonCap  != null ? CheckStatus.OK : CheckStatus.Warning));
            list.Add(new CheckItem("Botón → ledRenderer asignado",
                btnVR.ledRenderer != null ? CheckStatus.OK : CheckStatus.Warning));
        }

        // GameManager
        var gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var so   = new SerializedObject(gm);
            var prop = so.FindProperty("protoSim");   // campo renombrado en GameManager
            list.Add(new CheckItem("GameManager → protoSim (ProtoboardSimulator Reto 4)",
                prop?.objectReferenceValue != null ? CheckStatus.OK : CheckStatus.Warning));
        }

        return list;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helpers de UI de la herramienta
    // ═════════════════════════════════════════════════════════════════════

    void SectionHeader(string step, string title, string desc)
    {
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        EditorGUILayout.LabelField($"[{step}] {title}", titleStyle);
        if (!string.IsNullOrEmpty(desc))
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);
    }

    void DrawStatusRow(string label, bool ok, string extra = "")
    {
        var color = ok ? ColOK : ColError;
        var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"{(ok ? "✅" : "❌")} {label}", style, GUILayout.Width(240));
            if (!string.IsNullOrEmpty(extra))
                EditorGUILayout.LabelField(extra, EditorStyles.miniLabel);
        }
    }

    static void HelpBox(string msg, Color color)
    {
        var style = new GUIStyle(EditorStyles.helpBox)
            { normal = { textColor = color }, wordWrap = true };
        EditorGUILayout.LabelField(msg, style);
    }

    ElectricalNode FindChildNode(GameObject go, string nombre)
    {
        var t = go.transform.Find(nombre);
        return t?.GetComponent<ElectricalNode>();
    }

    // ── Helpers para crear UI ────────────────────────────────────────────

    GameObject CrearUIPanel(Transform parent, string nombre, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {nombre}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return go;
    }

    TMP_Text CrearTMPLabel(Transform parent, string nombre, string texto,
                            Vector2 anchoredPos, Vector2 size,
                            int fontSize, Color color,
                            FontStyles fontStyle = FontStyles.Normal)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {nombre}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = texto;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.fontStyle  = fontStyle;
        tmp.alignment  = TextAlignmentOptions.Left;
        return tmp;
    }

    TMP_Dropdown CrearDropdown(Transform parent, string nombre,
                                Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {nombre}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
        return go.AddComponent<TMP_Dropdown>();
    }

    // ── Checklist helpers ────────────────────────────────────────────────
    enum CheckStatus { OK, Warning, Error }
    struct CheckItem
    {
        public string      name;
        public CheckStatus status;
        public string      detail;
        public CheckItem(string n, CheckStatus s, string d = "")
            { name = n; status = s; detail = d; }
    }
}
#endif
