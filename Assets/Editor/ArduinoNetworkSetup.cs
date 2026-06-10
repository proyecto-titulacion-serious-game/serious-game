#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura el sistema de red del Arduino para el Reto 4.
///
/// Cubre la integracion completa Host (Tecnico) <-> Client (Explorador):
///
///   LADO EXPLORADOR (escena Explorador / MapVR):
///     • ArduinoCore          — emulador ATmega328P
///     • ArduinoNetworkBridge — sync Fusion Host<->Client
///     • Nodo_P13 / Nodo_GND / Nodo_A0 (ElectricalNode + SphereCollider)
///     • ProtoboardSimulator  — motor electrico sandbox (Reto 4)
///
///   LADO TECNICO (escena Tecnico / integrada):
///     • ArduinoIDEUI.bridge            → ArduinoNetworkBridge
///     • TechnicianTelemetryUI.arduinoBridge → ArduinoNetworkBridge
///     • TechnicianTelemetryUI.circuit       → CircuitSimulator (Retos 1-3)
///     • GameManager.protoSim           → ProtoboardSimulator (Reto 4)
///
///   Motor dual:
///     Retos 1-3 → GameManager.circuit   (CircuitSimulator)
///     Reto  4   → GameManager.protoSim  (ProtoboardSimulator)
///
/// Menu: Tools → TITA → Reto 4 → Setup Arduino Network Bridge
/// </summary>
public class ArduinoNetworkSetup : EditorWindow
{
    // ── Campos ────────────────────────────────────────────────────────────
    private GameObject _arduinoGO;
    private GameObject _protoboardGO;
    private int        _pinDefault  = 2;
    private int        _blinkMs     = 500;
    private bool       _modoOffline = true;

    // ── Tabs ──────────────────────────────────────────────────────────────
    private int _tab;
    private readonly string[] _tabs = { "Explorador", "Tecnico", "Validar", "Multi-Escena" };
    private Vector2 _scroll;

    // ── Estado validador ──────────────────────────────────────────────────
    private enum ValidadorContexto { Explorador, Tecnico, Completo }
    private ValidadorContexto _validCtx = ValidadorContexto.Completo;

    [MenuItem("Tools/TITA/Reto 4/Setup Arduino Network Bridge")]
    static void ShowWindow() => GetWindow<ArduinoNetworkSetup>("Arduino Network Setup");

    void OnEnable() => AutoDetect();

    // ─────────────────────────────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        EditorGUILayout.LabelField("Arduino Network Bridge — Reto 4", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Configura la comunicacion Host<->Client del Arduino virtual.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        _tab = GUILayout.Toolbar(_tab, _tabs);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: DrawTabExplorador();  break;
            case 1: DrawTabTecnico();     break;
            case 2: DrawTabValidar();     break;
            case 3: DrawTabMultiEscena(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 0 — EXPLORADOR
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabExplorador()
    {
        SectionHeader("Paso 1 — Lado Explorador",
            "Configura el GO del Arduino con ArduinoCore, ArduinoNetworkBridge " +
            "y los nodos electricos (P13, GND, A0). " +
            "Añade ProtoboardSimulator a la protoboard.");

        _arduinoGO = (GameObject)EditorGUILayout.ObjectField(
            "GO del Arduino 3D", _arduinoGO, typeof(GameObject), true);

        _protoboardGO = (GameObject)EditorGUILayout.ObjectField(
            "GO Protoboard (ProtoboardSimulator)", _protoboardGO, typeof(GameObject), true);

        _pinDefault  = EditorGUILayout.IntField("Pin por defecto (correcto)", _pinDefault);
        _blinkMs     = EditorGUILayout.IntField("BlinkMs por defecto", _blinkMs);
        _modoOffline = EditorGUILayout.Toggle("Modo offline (sin Fusion)", _modoOffline);

        if (_arduinoGO == null)
        {
            EditorGUILayout.HelpBox("Arrastra el GO del modelo 3D del Arduino.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.Space(4);
            DrawStatus("ArduinoCore",          _arduinoGO.GetComponent<ArduinoCore>()          != null);
            DrawStatus("ArduinoNetworkBridge", _arduinoGO.GetComponent<ArduinoNetworkBridge>() != null);
            DrawStatus("Nodo_P13",             _arduinoGO.transform.Find("Nodo_P13")           != null);
            DrawStatus("Nodo_GND",             _arduinoGO.transform.Find("Nodo_GND")           != null);
            DrawStatus("Nodo_A0",              _arduinoGO.transform.Find("Nodo_A0")            != null);

            if (_protoboardGO != null)
                DrawStatus("ProtoboardSimulator",
                    _protoboardGO.GetComponent<ProtoboardSimulator>() != null);
        }

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(_arduinoGO == null))
        {
            if (GUILayout.Button("Configurar Arduino GO", GUILayout.Height(34)))
                SetupArduinoGO();
        }

        if (_protoboardGO != null)
        {
            if (GUILayout.Button("Añadir ProtoboardSimulator a Protoboard", GUILayout.Height(28)))
                SetupProtoboard();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Tras ejecutar:\n" +
            "• Reposiciona Nodo_P13 / Nodo_GND / Nodo_A0 en el Inspector\n" +
            "  para alinearlos con los pines fisicos del modelo 3D.\n" +
            "• Si usas Fusion, desactiva 'Modo offline' y verifica el AppID.",
            MessageType.Info);
    }

    void SetupArduinoGO()
    {
        Undo.RecordObject(_arduinoGO, "Setup Arduino GO");

        var core = _arduinoGO.GetComponent<ArduinoCore>()
                ?? Undo.AddComponent<ArduinoCore>(_arduinoGO);

        var bridge = _arduinoGO.GetComponent<ArduinoNetworkBridge>()
                  ?? Undo.AddComponent<ArduinoNetworkBridge>(_arduinoGO);

        // Asignar _arduinoCore via SerializedObject (ahora tiene [SerializeField]).
        // Si core está en el mismo GO, Awake() también lo resuelve en runtime.
        var so       = new SerializedObject(bridge);
        var propArdu = so.FindProperty("_arduinoCore") ?? so.FindProperty("_arduino");
        if (propArdu != null && propArdu.objectReferenceValue == null)
        {
            propArdu.objectReferenceValue = core;
            so.ApplyModifiedProperties();
            Debug.Log("[ArduinoNetworkSetup] Bridge._arduinoCore asignado vía SerializedObject.");
        }
        else if (propArdu == null)
        {
            Debug.Log("[ArduinoNetworkSetup] _arduinoCore no serializable — se resolverá " +
                      "en runtime vía GetComponent<ArduinoCore>() en Awake().");
        }

        // Nodos electricos
        core.nodoP13 = GetOrCreateNodo(_arduinoGO, "Nodo_P13", new Vector3( 0.01f, 0f,  0f));
        core.nodoGND = GetOrCreateNodo(_arduinoGO, "Nodo_GND", new Vector3(-0.01f, 0f,  0f));
        core.nodoA0  = GetOrCreateNodo(_arduinoGO, "Nodo_A0",  new Vector3( 0f,    0f, -0.01f));

        core.activePinNumber = _pinDefault;
        core.blinkEnabled    = true;

        EditorUtility.SetDirty(_arduinoGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        if (_modoOffline)
        {
            var cm = FindAnyObjectByType<ConnectionManager>();
            if (cm != null)
            {
                var cso = new SerializedObject(cm);
                var propOffline = cso.FindProperty("modoOffline");
                if (propOffline != null) { propOffline.boolValue = true; cso.ApplyModifiedProperties(); }
                EditorUtility.SetDirty(cm);
            }
        }

        Debug.Log($"[ArduinoNetworkSetup] Arduino GO configurado: {_arduinoGO.name}");
        EditorUtility.DisplayDialog("Arduino GO configurado",
            $"ArduinoCore + ArduinoNetworkBridge listos en '{_arduinoGO.name}'.\n\n" +
            "Nodos creados:\n  Nodo_P13 · Nodo_GND · Nodo_A0\n\n" +
            "Reposiciona los nodos en el Inspector para que coincidan con los pines del modelo.",
            "OK");
    }

    void SetupProtoboard()
    {
        if (_protoboardGO.GetComponent<ProtoboardSimulator>() == null)
        {
            Undo.AddComponent<ProtoboardSimulator>(_protoboardGO);
            EditorUtility.SetDirty(_protoboardGO);
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[ArduinoNetworkSetup] ProtoboardSimulator añadido a " + _protoboardGO.name);
        }
        else
            EditorUtility.DisplayDialog("Info", "ProtoboardSimulator ya existe.", "OK");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 1 — TECNICO
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabTecnico()
    {
        SectionHeader("Paso 2 — Lado Tecnico",
            "Conecta el ArduinoNetworkBridge al IDE y al panel de telemetria. " +
            "Tambien enlaza GameManager.protoSim al ProtoboardSimulator del Reto 4.");

        var bridge  = FindAnyObjectByType<ArduinoNetworkBridge>();
        var ide     = FindAnyObjectByType<ArduinoIDEUI>();
        var tele    = FindAnyObjectByType<TechnicianTelemetryUI>();
        var sim     = FindAnyObjectByType<ProtoboardSimulator>();
        var gm      = FindAnyObjectByType<GameManager>();

        DrawStatus("ArduinoNetworkBridge en escena",  bridge != null,
            bridge != null ? bridge.gameObject.name : "—");
        DrawStatus("ArduinoIDEUI en escena",           ide    != null,
            ide    != null ? ide.gameObject.name    : "—");
        DrawStatus("TechnicianTelemetryUI en escena",  tele   != null,
            tele   != null ? tele.gameObject.name   : "—");
        DrawStatus("ProtoboardSimulator en escena",    sim    != null,
            sim    != null ? sim.gameObject.name    : "—");
        DrawStatus("GameManager en escena",            gm     != null,
            gm     != null ? gm.gameObject.name     : "—");

        if (ide != null)
            DrawStatus("  IDE.bridge asignado",
                ide.bridge != null, ide.bridge != null ? ide.bridge.name : "null");

        if (tele != null)
        {
            DrawStatus("  Tele.arduinoBridge asignado",
                tele.arduinoBridge != null,
                tele.arduinoBridge != null ? tele.arduinoBridge.name : "null");
            DrawStatus("  Tele.circuit asignado (Retos 1-3)",
                tele.circuit != null,
                tele.circuit != null ? tele.circuit.name : "null");
        }

        if (gm != null)
        {
            DrawStatus("  GameManager.circuit (Retos 1-3)",
                gm.circuit  != null, gm.circuit  != null ? gm.circuit.name  : "null (auto-update)");
            DrawStatus("  GameManager.protoSim (Reto 4)",
                gm.protoSim != null, gm.protoSim != null ? gm.protoSim.name : "null (auto-detect)");
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Conectar todas las referencias (Tecnico)", GUILayout.Height(34)))
            ConnectTecnicoSide(bridge, ide, tele, gm, sim);

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Si el bridge no aparece es porque esta en la escena del Explorador.\n" +
            "Abre IntegratedDemo.unity y ejecuta este paso para conectar ambas escenas.\n\n" +
            "GameManager.circuit se actualiza automaticamente al cambiar de reto (Retos 1-3).\n" +
            "GameManager.protoSim se auto-detecta en runtime si queda vacio.",
            MessageType.Info);
    }

    void ConnectTecnicoSide(ArduinoNetworkBridge bridge, ArduinoIDEUI ide,
                             TechnicianTelemetryUI tele, GameManager gm,
                             ProtoboardSimulator sim)
    {
        int done = 0;
        var log  = new System.Text.StringBuilder();

        // IDE → Bridge
        if (ide != null && bridge != null && ide.bridge == null)
        {
            Undo.RecordObject(ide, "Asignar bridge");
            ide.bridge = bridge;
            EditorUtility.SetDirty(ide);
            log.AppendLine("ArduinoIDEUI.bridge → " + bridge.name);
            done++;
        }

        // TelemetryUI → Bridge
        if (tele != null && bridge != null && tele.arduinoBridge == null)
        {
            Undo.RecordObject(tele, "Asignar arduinoBridge");
            tele.arduinoBridge = bridge;
            EditorUtility.SetDirty(tele);
            log.AppendLine("TechnicianTelemetryUI.arduinoBridge → " + bridge.name);
            done++;
        }

        // GameManager.protoSim → ProtoboardSimulator (Reto 4)
        if (gm != null && sim != null && gm.protoSim == null)
        {
            Undo.RecordObject(gm, "Asignar protoSim");
            gm.protoSim = sim;
            EditorUtility.SetDirty(gm);
            log.AppendLine("GameManager.protoSim → " + sim.name);
            done++;
        }

        // GameManager.circuit → CircuitSimulator (Retos 1-3, si hay uno en escena)
        if (gm != null && gm.circuit == null)
        {
            var csim = FindAnyObjectByType<CircuitSimulator>();
            if (csim != null)
            {
                Undo.RecordObject(gm, "Asignar circuit");
                gm.circuit = csim;
                EditorUtility.SetDirty(gm);
                log.AppendLine("GameManager.circuit → " + csim.name + " (Retos 1-3)");
                done++;
            }
        }

        if (done == 0)
            log.AppendLine("Sin cambios — todas las referencias ya estaban asignadas.");

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            done > 0 ? $"Tecnico conectado ({done} ref.)" : "Sin cambios",
            log.ToString(), "OK");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 2 — VALIDACION (con contexto de escena)
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabValidar()
    {
        SectionHeader("Validacion — Arduino Network Bridge", null);

        // ── Selector de contexto ─────────────────────────────────────────
        EditorGUILayout.HelpBox(
            "Selecciona qué escena tienes abierta. Cada escena tiene componentes distintos:\n" +
            "• Explorador: ArduinoCore, ArduinoNetworkBridge, ProtoboardSimulator\n" +
            "• Técnico: ArduinoIDEUI, TechnicianTelemetryUI, GameManager\n" +
            "• Completo: ambas escenas cargadas de forma aditiva (ver Tab Multi-Escena)",
            MessageType.Info);

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Contexto:", GUILayout.Width(70));
            _validCtx = (ValidadorContexto)GUILayout.Toolbar((int)_validCtx,
                new[] { "Explorador", "Tecnico", "Completo" });
        }
        EditorGUILayout.Space(6);

        // ── Checklist filtrada ────────────────────────────────────────────
        var checks = BuildChecklist(_validCtx);
        int ok = 0;
        foreach (var c in checks) if (c.ok) ok++;

        if (checks.Count > 0)
        {
            var rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, (float)ok / checks.Count,
                ok == checks.Count ? "Lista!" : $"{ok}/{checks.Count}");
            EditorGUILayout.Space(6);
        }

        foreach (var c in checks)
        {
            var col = c.ok    ? new Color(0.3f, 0.9f, 0.3f)
                    : c.warn  ? new Color(0.9f, 0.7f, 0.1f)
                              : new Color(0.9f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = col } };
            using (new EditorGUILayout.HorizontalScope())
            {
                string icon = c.ok ? "OK" : c.warn ? "??" : "X ";
                EditorGUILayout.LabelField($"[{icon}] {c.name}", style, GUILayout.Width(280));
                if (!string.IsNullOrEmpty(c.detail))
                    EditorGUILayout.LabelField(c.detail, EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Actualizar", GUILayout.Height(26))) Repaint();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 3 — MULTI-ESCENA (Opción B)
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabMultiEscena()
    {
        SectionHeader("Multi-Escena — Opcion B", null);

        EditorGUILayout.HelpBox(
            "Esta opción carga Explorador.unity de forma aditiva junto a la escena activa.\n" +
            "Con ambas escenas visibles, la herramienta puede conectar referencias cross-scene\n" +
            "y guardarlas en disco. Al terminar, cierra Explorador.unity.",
            MessageType.Info);

        EditorGUILayout.Space(6);

        bool exploLoaded = IsExploradoLoaded();
        string sceneStatus = exploLoaded
            ? "Explorador.unity — CARGADA (aditiva)"
            : "Explorador.unity — no cargada";
        var statusCol = exploLoaded ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.7f, 0.1f);
        var statusStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = statusCol } };
        EditorGUILayout.LabelField(sceneStatus, statusStyle);
        EditorGUILayout.Space(4);

        if (!exploLoaded)
        {
            if (GUILayout.Button("1. Cargar Explorador.unity de forma aditiva", GUILayout.Height(32)))
                CargarExploradoAditivo();
        }
        else
        {
            EditorGUILayout.LabelField("Ambas escenas cargadas. Ejecuta la conexion:", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("2. Conectar referencias cross-scene", GUILayout.Height(32)))
                ConectarCrossScene();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("3. Guardar ambas escenas", GUILayout.Height(28)))
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[ArduinoNetworkSetup] Ambas escenas guardadas.");
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Tras guardar, puedes cerrar Explorador.unity desde el Hierarchy " +
                "(clic derecho → Remove Scene) o usar el botón siguiente.",
                MessageType.None);

            if (GUILayout.Button("4. Cerrar Explorador.unity (opcional)", GUILayout.Height(28)))
                CerrarExploradoAditivo();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "NOTA: Los NetworkBehaviour no pueden tener referencias directas cross-scene\n" +
            "en tiempo de ejecución porque Fusion spawna objetos dinámicamente.\n" +
            "Esta opción es útil para pre-configurar referencias en MonoBehaviours\n" +
            "estáticos (TechnicianTelemetryUI, ArduinoIDEUI) que viven en la escena\n" +
            "del Técnico y necesitan conocer el GO del ArduinoNetworkBridge.\n\n" +
            "Gracias a la Opción A (OnBridgeReady), esas referencias se actualizan\n" +
            "automáticamente en runtime aunque queden vacías aquí.",
            MessageType.Info);
    }

    // ─── Helpers Multi-Escena ─────────────────────────────────────────────

    static bool IsExploradoLoaded()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.path.Contains("Explorador") && s.isLoaded) return true;
        }
        return false;
    }

    static void CargarExploradoAditivo()
    {
        const string PATH = "Assets/Scenes/Explorador.unity";
        if (!System.IO.File.Exists(PATH))
        {
            EditorUtility.DisplayDialog("Error",
                $"No se encontró {PATH}. Verifica la ruta.", "OK");
            return;
        }
        EditorSceneManager.OpenScene(PATH, OpenSceneMode.Additive);
        Debug.Log("[ArduinoNetworkSetup] Explorador.unity cargada de forma aditiva.");
    }

    static void CerrarExploradoAditivo()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.path.Contains("Explorador"))
            {
                EditorSceneManager.CloseScene(s, true);
                Debug.Log("[ArduinoNetworkSetup] Explorador.unity cerrada.");
                return;
            }
        }
    }

    static void ConectarCrossScene()
    {
        var log = new System.Text.StringBuilder();

        var bridge = Object.FindAnyObjectByType<ArduinoNetworkBridge>(FindObjectsInactive.Include);
        var ide    = Object.FindAnyObjectByType<ArduinoIDEUI>(FindObjectsInactive.Include);
        var tele   = Object.FindAnyObjectByType<TechnicianTelemetryUI>(FindObjectsInactive.Include);

        if (bridge == null)
        {
            log.AppendLine("ArduinoNetworkBridge no encontrado. " +
                           "Asegúrate de que Explorador.unity está cargada.");
        }
        // Unity NO soporta guardar referencias cross-scene en disco.
        // Solo verificamos qué está disponible y lo reportamos.
        // La Opción A (OnBridgeReady) conecta todo en runtime automáticamente.
        if (bridge != null)
        {
            log.AppendLine($"ArduinoNetworkBridge detectado: {bridge.name} ({bridge.gameObject.scene.name})");
            if (ide  != null) log.AppendLine($"ArduinoIDEUI detectado: {ide.gameObject.name} ({ide.gameObject.scene.name})");
            if (tele != null) log.AppendLine($"TechnicianTelemetryUI detectado: {tele.gameObject.name} ({tele.gameObject.scene.name})");

            log.AppendLine("\n⚠ Unity NO guarda referencias cross-scene en disco.");
            log.AppendLine("✅ Esto NO es un problema: Opción A (OnBridgeReady) conecta");
            log.AppendLine("   ArduinoIDEUI y TechnicianTelemetryUI automáticamente en runtime");
            log.AppendLine("   cuando Fusion spawnea el ArduinoNetworkBridge.");
        }
        else
        {
            log.AppendLine("ArduinoNetworkBridge no encontrado en ninguna escena cargada.");
            log.AppendLine("Verifica que Explorador.unity esté cargada aditivamente (Paso 1).");
        }

        EditorUtility.DisplayDialog(
            "Estado Multi-Escena",
            log.ToString(), "OK");
    }

    List<CheckItem> BuildChecklist(ValidadorContexto ctx = ValidadorContexto.Completo)
    {
        var list = new List<CheckItem>();

        bool showExplo  = ctx == ValidadorContexto.Explorador || ctx == ValidadorContexto.Completo;
        bool showTecni  = ctx == ValidadorContexto.Tecnico    || ctx == ValidadorContexto.Completo;

        var core   = FindAnyObjectByType<ArduinoCore>();
        var bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
        var sim    = FindAnyObjectByType<ProtoboardSimulator>();
        var ide    = FindAnyObjectByType<ArduinoIDEUI>();
        var tele   = FindAnyObjectByType<TechnicianTelemetryUI>();
        var gm     = FindAnyObjectByType<GameManager>();
        var gs     = FindAnyObjectByType<GameSession>();
        var cm     = FindAnyObjectByType<ConnectionManager>();
        var vbtn   = FindAnyObjectByType<VRValidationButton>();

        // ── Lado Explorador (VR) ─────────────────────────────────────────
        if (showExplo)
        {
            list.Add(C("ArduinoCore en escena",          core   != null, false, core?.name));
            list.Add(C("ArduinoNetworkBridge en escena", bridge != null, false, bridge?.name));

            if (bridge != null)
            {
                // _arduinoCore ahora tiene [SerializeField] → FindProperty lo encuentra.
                // Fallback: verificar GetComponent por si ambos están en el mismo GO.
                var so    = new SerializedObject(bridge);
                var propA = so.FindProperty("_arduinoCore") ?? so.FindProperty("_arduino");
                bool coreOk = propA?.objectReferenceValue != null
                           || bridge.GetComponent<ArduinoCore>() != null;
                string coreDetail = propA?.objectReferenceValue?.name
                                 ?? (bridge.GetComponent<ArduinoCore>() != null
                                        ? $"{bridge.GetComponent<ArduinoCore>().gameObject.name} (mismo GO)"
                                        : null);
                list.Add(C("Bridge._arduinoCore asignado", coreOk, false, coreDetail));
            }

            if (core != null)
            {
                list.Add(C("nodoP13 asignado", core.nodoP13 != null, false));
                list.Add(C("nodoGND asignado", core.nodoGND != null, false));
                list.Add(C("nodoA0  asignado", core.nodoA0  != null, false));
            }

            list.Add(C("ProtoboardSimulator (Reto 4)", sim  != null, false, sim?.name));
            list.Add(C("VRValidationButton",           vbtn != null, true,  vbtn?.name));
            if (vbtn != null)
                list.Add(C("  Boton.haptics asignado",
                    vbtn.haptics != null, true,
                    vbtn.haptics != null ? vbtn.haptics.name : null));
        }

        // ── Lado Técnico (PC) ────────────────────────────────────────────
        if (showTecni)
        {
            list.Add(C("ArduinoIDEUI en escena",   ide  != null, true, ide?.name));
            list.Add(C("TechnicianTelemetryUI",    tele != null, true, tele?.name));
            list.Add(C("GameManager en escena",    gm   != null, false,
                gm != null ? gm.gameObject.name : null));
            list.Add(C("GameSession en escena",    gs   != null, true,  gs?.name));
            list.Add(C("ConnectionManager",        cm   != null, false, cm?.name));

            if (ide != null)
            {
                bool bridgeOk = ide.bridge != null;
                list.Add(C("  IDE.bridge asignado (o auto-detectado)",
                    bridgeOk, true,
                    bridgeOk ? ide.bridge.name : "se conecta por OnBridgeReady en runtime"));
            }

            if (tele != null)
            {
                bool bridgeOk = tele.arduinoBridge != null;
                list.Add(C("  Tele.arduinoBridge asignado (o auto-detectado)",
                    bridgeOk, true,
                    bridgeOk ? tele.arduinoBridge.name : "se conecta por OnBridgeReady en runtime"));
            }

            if (gm != null)
            {
                list.Add(C("GameManager.reto4Zone asignado",
                    gm.reto4Zone != null, false,
                    gm.reto4Zone != null ? gm.reto4Zone.name : null));
                list.Add(C("GameManager.protoSim (Reto 4)",
                    gm.protoSim != null, true,
                    gm.protoSim != null ? gm.protoSim.name : "auto-detecta en runtime"));
            }
        }

        return list;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    void AutoDetect()
    {
        // Arduino GO: buscar por ArduinoCore primero, luego por nombre
        if (_arduinoGO == null)
        {
            var core = FindAnyObjectByType<ArduinoCore>();
            if (core != null) _arduinoGO = core.gameObject;
            else
            {
                var go = GameObject.Find("Arduino_VR") ?? GameObject.Find("Arduino_Board")
                      ?? GameObject.Find("Arduino");
                if (go != null) _arduinoGO = go;
            }
        }

        // Protoboard GO: buscar ProtoboardSimulator primero (motor Reto 4), luego por nombre
        if (_protoboardGO == null)
        {
            var psim = FindAnyObjectByType<ProtoboardSimulator>();
            if (psim != null) _protoboardGO = psim.gameObject;
            else
            {
                var go = GameObject.Find("Protoboard_VR") ?? GameObject.Find("Protoboard")
                      ?? GameObject.Find("Bareboard");
                if (go != null) _protoboardGO = go;
            }
        }
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

    static void SectionHeader(string title, string desc)
    {
        EditorGUILayout.LabelField(title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
        if (!string.IsNullOrEmpty(desc))
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);
    }

    static void DrawStatus(string label, bool ok, string detail = "")
    {
        var col   = ok ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.4f, 0.2f);
        var style = new GUIStyle(EditorStyles.label) { normal = { textColor = col } };
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"[{(ok ? "OK" : " ?")}] {label}", style, GUILayout.Width(265));
            if (!string.IsNullOrEmpty(detail))
                EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
        }
    }

    static CheckItem C(string name, bool ok, bool warn, string detail = "")
        => new CheckItem { name = name, ok = ok, warn = !ok && warn, detail = detail ?? "" };

    struct CheckItem { public string name, detail; public bool ok, warn; }
}
#endif
