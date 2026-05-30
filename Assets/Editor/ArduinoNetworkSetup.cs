#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura el sistema de red del Arduino para el Reto 4.
///
/// Cubre la integración completa Host (Técnico) ↔ Client (Explorador):
///
///   LADO EXPLORADOR (escena Explorador / MapVR):
///     • ArduinoCore      — emulador ATmega328P
///     • ArduinoNetworkBridge — sync Fusion Host↔Client
///     • Nodo_P13 / Nodo_GND / Nodo_A0 (ElectricalNode + SphereCollider)
///     • CircuitSimulator — motor eléctrico sandbox
///
///   LADO TÉCNICO (escena Tecnico / integrada):
///     • ArduinoIDEUI.bridge    → ArduinoNetworkBridge
///     • TechnicianTelemetryUI.arduinoBridge → ArduinoNetworkBridge
///     • TechnicianTelemetryUI.circuit       → CircuitSimulator
///     • GameManager.circuitSimulator        → CircuitSimulator
///
///   VALIDACIÓN:
///     • GameSession presente para RPCs de Fusion
///     • ConnectionManager presente
///     • VRValidationButton conectado a HapticFeedback
///
/// Menú: Tools → TITA → Reto 4 → Setup Arduino Network Bridge
/// </summary>
public class ArduinoNetworkSetup : EditorWindow
{
    // ── Inspector campos ──────────────────────────────────────────────────
    private GameObject  _arduinoGO;
    private GameObject  _protoboardGO;
    private int         _pinDefault    = 2;
    private int         _blinkMs       = 500;
    private bool        _modoOffline   = true;

    // ── Tabs ──────────────────────────────────────────────────────────────
    private int _tab;
    private readonly string[] _tabs = { "Explorador", "Técnico", "Validar" };
    private Vector2 _scroll;

    [MenuItem("Tools/TITA/Reto 4/Setup Arduino Network Bridge")]
    static void ShowWindow()
        => GetWindow<ArduinoNetworkSetup>("Arduino Network Setup");

    void OnEnable() => AutoDetect();

    // ─────────────────────────────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        EditorGUILayout.LabelField("Arduino Network Bridge — Reto 4", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Configura la comunicación Host↔Client del Arduino virtual.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        _tab = GUILayout.Toolbar(_tab, _tabs);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: DrawTabExplorador(); break;
            case 1: DrawTabTecnico();    break;
            case 2: DrawTabValidar();    break;
        }
        EditorGUILayout.EndScrollView();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 0 — EXPLORADOR (ArduinoCore + Bridge + Nodos)
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabExplorador()
    {
        SectionHeader("Paso 1 — Lado Explorador",
            "Configura el GameObject del Arduino con ArduinoCore, ArduinoNetworkBridge " +
            "y los nodos eléctricos (P13, GND, A0).");

        _arduinoGO = (GameObject)EditorGUILayout.ObjectField(
            "GO del Arduino 3D", _arduinoGO, typeof(GameObject), true);

        _protoboardGO = (GameObject)EditorGUILayout.ObjectField(
            "GO Protoboard (CircuitSimulator)", _protoboardGO, typeof(GameObject), true);

        _pinDefault = EditorGUILayout.IntField("Pin por defecto (correcto)", _pinDefault);
        _blinkMs    = EditorGUILayout.IntField("BlinkMs por defecto", _blinkMs);
        _modoOffline = EditorGUILayout.Toggle("Modo offline (sin Fusion)", _modoOffline);

        if (_arduinoGO == null)
        {
            EditorGUILayout.HelpBox("Arrastra el GO del modelo 3D del Arduino.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.Space(4);
            // Estado
            DrawStatus("ArduinoCore",           _arduinoGO.GetComponent<ArduinoCore>()          != null);
            DrawStatus("ArduinoNetworkBridge",   _arduinoGO.GetComponent<ArduinoNetworkBridge>() != null);
            DrawStatus("Nodo_P13",               _arduinoGO.transform.Find("Nodo_P13")           != null);
            DrawStatus("Nodo_GND",               _arduinoGO.transform.Find("Nodo_GND")           != null);
            DrawStatus("Nodo_A0",                _arduinoGO.transform.Find("Nodo_A0")            != null);

            if (_protoboardGO != null)
                DrawStatus("ProtoboardSimulator", _protoboardGO.GetComponent<ProtoboardSimulator>() != null);
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
            "  para alinearlos con los pines físicos del modelo 3D.\n" +
            "• Si usas Fusion, desactiva 'Modo offline' y verifica el AppID.",
            MessageType.Info);
    }

    void SetupArduinoGO()
    {
        Undo.RecordObject(_arduinoGO, "Setup Arduino GO");

        // ArduinoCore
        var core = _arduinoGO.GetComponent<ArduinoCore>()
                ?? Undo.AddComponent<ArduinoCore>(_arduinoGO);

        // ArduinoNetworkBridge
        var bridge = _arduinoGO.GetComponent<ArduinoNetworkBridge>()
                  ?? Undo.AddComponent<ArduinoNetworkBridge>(_arduinoGO);

        // Conectar _arduino via SerializedObject (campo privado serializado)
        var so = new SerializedObject(bridge);
        var propArduino = so.FindProperty("_arduino");
        if (propArduino != null && propArduino.objectReferenceValue == null)
        {
            propArduino.objectReferenceValue = core;
            so.ApplyModifiedProperties();
        }

        // Nodos
        core.nodoP13 = GetOrCreateNodo(_arduinoGO, "Nodo_P13", new Vector3( 0.01f, 0f,  0f));
        core.nodoGND = GetOrCreateNodo(_arduinoGO, "Nodo_GND", new Vector3(-0.01f, 0f,  0f));
        core.nodoA0  = GetOrCreateNodo(_arduinoGO, "Nodo_A0",  new Vector3( 0f,    0f, -0.01f));

        // Valores por defecto
        core.activePinNumber = _pinDefault;
        core.blinkEnabled    = true;

        EditorUtility.SetDirty(_arduinoGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        // Offline mode en ConnectionManager
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
            "⚠  Reposiciona los nodos en el Inspector para que coincidan con los pines del modelo.",
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
    //  TAB 1 — TÉCNICO (conexión bridge → UI)
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabTecnico()
    {
        SectionHeader("Paso 2 — Lado Técnico",
            "Conecta el ArduinoNetworkBridge al IDE y al panel de telemetría del Técnico. " +
            "Abre ambas escenas (Tecnico + Explorador) o usa IntegratedDemo.");

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
        {
            DrawStatus("  IDE.bridge asignado",
                ide.bridge != null, ide.bridge != null ? ide.bridge.name : "null");
        }
        if (tele != null)
        {
            DrawStatus("  Tele.arduinoBridge asignado",
                tele.arduinoBridge != null,
                tele.arduinoBridge != null ? tele.arduinoBridge.name : "null");
            DrawStatus("  Tele.circuit asignado",
                tele.circuit != null,
                tele.circuit != null ? tele.circuit.name : "null");
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Conectar todas las referencias (Técnico)", GUILayout.Height(34)))
            ConnectTecnicoSide(bridge, ide, tele, gm);

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Si el bridge no aparece es porque está en la escena del Explorador.\n" +
            "Abre IntegratedDemo.unity y ejecuta este paso para conectar ambas escenas.",
            MessageType.Info);
    }

    void ConnectTecnicoSide(ArduinoNetworkBridge bridge, ArduinoIDEUI ide,
                             TechnicianTelemetryUI tele, GameManager gm)
    {
        int done = 0;
        var log = new System.Text.StringBuilder();

        if (ide != null && bridge != null && ide.bridge == null)
        {
            Undo.RecordObject(ide, "Asignar bridge");
            ide.bridge = bridge;
            EditorUtility.SetDirty(ide);
            log.AppendLine("✅ ArduinoIDEUI.bridge → " + bridge.name);
            done++;
        }

        if (tele != null && bridge != null && tele.arduinoBridge == null)
        {
            Undo.RecordObject(tele, "Asignar arduinoBridge");
            tele.arduinoBridge = bridge;
            EditorUtility.SetDirty(tele);
            log.AppendLine("✅ TechnicianTelemetryUI.arduinoBridge → " + bridge.name);
            done++;
        }

        // TechnicianTelemetryUI.circuit auto-detecta vía FindAnyObjectByType en runtime

        if (done == 0)
            log.AppendLine("Sin cambios — todas las referencias ya estaban asignadas.");

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            done > 0 ? $"Técnico conectado ({done} ref.)" : "Sin cambios",
            log.ToString(), "OK");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB 2 — VALIDACIÓN
    // ═════════════════════════════════════════════════════════════════════
    void DrawTabValidar()
    {
        SectionHeader("Validación — Arduino Network Bridge", null);

        var checks = BuildChecklist();
        int ok = 0;
        foreach (var c in checks) if (c.ok) ok++;

        // Barra de progreso
        var rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(rect, (float)ok / checks.Count,
            ok == checks.Count ? "¡Red Arduino lista!" : $"{ok}/{checks.Count}");
        EditorGUILayout.Space(6);

        foreach (var c in checks)
        {
            var col = c.ok ? new Color(0.3f, 0.9f, 0.3f)
                    : c.warn ? new Color(0.9f, 0.7f, 0.1f)
                    : new Color(0.9f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = col } };
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"{(c.ok ? "✅" : c.warn ? "⚠" : "❌")} {c.name}", style, GUILayout.Width(270));
                if (!string.IsNullOrEmpty(c.detail))
                    EditorGUILayout.LabelField(c.detail, EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Actualizar", GUILayout.Height(26))) Repaint();
    }

    List<CheckItem> BuildChecklist()
    {
        var list = new List<CheckItem>();

        var core   = FindAnyObjectByType<ArduinoCore>();
        var bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
        var sim    = FindAnyObjectByType<ProtoboardSimulator>();
        var ide    = FindAnyObjectByType<ArduinoIDEUI>();
        var tele   = FindAnyObjectByType<TechnicianTelemetryUI>();
        var gm     = FindAnyObjectByType<GameManager>();
        var gs     = FindAnyObjectByType<GameSession>();
        var cm     = FindAnyObjectByType<ConnectionManager>();
        var vbtn   = FindAnyObjectByType<VRValidationButton>();
        var haptic = FindAnyObjectByType<HapticFeedback>();

        list.Add(C("ArduinoCore en escena",           core   != null, false, core?.name));
        list.Add(C("ArduinoNetworkBridge en escena",  bridge != null, false, bridge?.name));

        if (bridge != null)
        {
            var so = new SerializedObject(bridge);
            var propA = so.FindProperty("_arduino");
            list.Add(C("Bridge._arduino asignado",
                propA?.objectReferenceValue != null, false,
                propA?.objectReferenceValue?.name));
        }

        if (core != null)
        {
            list.Add(C("nodoP13 asignado", core.nodoP13 != null, false));
            list.Add(C("nodoGND asignado", core.nodoGND != null, false));
            list.Add(C("nodoA0  asignado", core.nodoA0  != null, false));
        }

        list.Add(C("ProtoboardSimulator en escena", sim != null, false, sim?.name));
        list.Add(C("ArduinoIDEUI en escena",     ide  != null, true,  ide?.name));
        list.Add(C("TechnicianTelemetryUI",      tele != null, true,  tele?.name));

        if (ide  != null) list.Add(C("IDE.bridge asignado",
            ide.bridge  != null, true, ide.bridge?.name));
        if (tele != null)
        {
            list.Add(C("Tele.arduinoBridge asignado",
                tele.arduinoBridge != null, true, tele.arduinoBridge?.name));
            list.Add(C("Tele.circuit asignado",
                tele.circuit != null, true, tele.circuit?.name));
        }
        // Nota: tele.circuit se auto-detecta en runtime si no está asignado en el Inspector

        if (gm != null)
        {
            var so = new SerializedObject(gm);
            var prop = so.FindProperty("circuitSimulator");
            list.Add(C("GameManager.circuitSimulator",
                prop?.objectReferenceValue != null, true,
                prop?.objectReferenceValue?.name));
        }

        list.Add(C("GameSession en escena",      gs     != null, true,  gs?.name));
        list.Add(C("ConnectionManager",          cm     != null, false, cm?.name));
        list.Add(C("VRValidationButton",         vbtn   != null, true,  vbtn?.name));

        if (vbtn != null)
            list.Add(C("Botón.haptics asignado",
                vbtn.haptics != null, true, vbtn.haptics?.name));

        return list;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    void AutoDetect()
    {
        if (_arduinoGO == null)
        {
            var core = FindAnyObjectByType<ArduinoCore>();
            if (core != null) _arduinoGO = core.gameObject;
            else
            {
                var go = GameObject.Find("Arduino_VR") ?? GameObject.Find("Arduino");
                if (go != null) _arduinoGO = go;
            }
        }
        if (_protoboardGO == null)
        {
            var sim = FindAnyObjectByType<CircuitSimulator>();
            if (sim != null) _protoboardGO = sim.gameObject;
            else
            {
                var go = GameObject.Find("Protoboard_VR") ?? GameObject.Find("Protoboard");
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
            EditorGUILayout.LabelField($"{(ok ? "✅" : "⚠ ")} {label}", style, GUILayout.Width(260));
            if (!string.IsNullOrEmpty(detail))
                EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
        }
    }

    static CheckItem C(string name, bool ok, bool warn, string detail = "")
        => new CheckItem { name = name, ok = ok, warn = !ok && warn, detail = detail ?? "" };

    struct CheckItem { public string name, detail; public bool ok, warn; }
}
#endif
