#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Diseña el Reto 4 completo: Arduino + Protoboard (grilla ya generada).
/// Menu: Tools > TITA > Reto 4 Arduino Protoboard
/// </summary>
public class Reto4ArduinoProtoboardTool : EditorWindow
{
    // ── Referencias ───────────────────────────────────────────────────────
    GameObject _reto4Zone;
    GameObject _protoboardGO;
    GameObject _arduinoGO;

    // ── Circuito ──────────────────────────────────────────────────────────
    float _ttlVoltage = 5f;
    int   _wrongPin   = 4;
    int   _correctPin = 2;
    float _wrongRes   = 0f;
    float _correctRes = 330f;

    // ── Rails (fila de la grilla) ──────────────────────────────────────────
    int _rowVCC = 0;
    int _rowGND = 1;
    int _rowDIG = 2;
    int _rowMID = 3;
    int _rowADC = 4;

    // ── NodePoints ─────────────────────────────────────────────────────────
    float _npPadSize = 0.018f;
    float _npRadius  = 0.014f;

    // ── UI ─────────────────────────────────────────────────────────────────
    Vector2 _scroll;
    Vector2 _logScroll;
    string  _log = "Listo.";

    // ── Colores ────────────────────────────────────────────────────────────
    static readonly Color CGold = new Color(0.95f, 0.75f, 0.10f);
    static readonly Color CSlot = new Color(0.95f, 0.55f, 0.10f);
    static readonly Color CArdu = new Color(0.10f, 0.45f, 0.20f);
    static readonly Color CRed  = new Color(0.90f, 0.15f, 0.15f);

    const string MAT_FOLDER = "Assets/Materials/Retos";
    const string FBX_RES    = "Assets/circuit/models/resistorVertical.fbx";

    // ─────────────────────────────────────────────────────────────────────
    //  Menu — solo ASCII, sin guiones especiales ni simbolos
    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Reto 4 Arduino Protoboard")]
    static void OpenWindow() => GetWindow<Reto4ArduinoProtoboardTool>("Reto4 Arduino+Proto");

    // ─────────────────────────────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("RETO 4 - Arduino + Protoboard", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Referencias
        EditorGUILayout.LabelField("Referencias de escena", EditorStyles.boldLabel);
        _reto4Zone    = (GameObject)EditorGUILayout.ObjectField("Reto4_Zone", _reto4Zone,    typeof(GameObject), true);
        _protoboardGO = (GameObject)EditorGUILayout.ObjectField("Protoboard", _protoboardGO, typeof(GameObject), true);
        _arduinoGO    = (GameObject)EditorGUILayout.ObjectField("Arduino GO", _arduinoGO,    typeof(GameObject), true);

        if (GUILayout.Button("Auto-detectar en escena"))
            AutoDetect();

        EditorGUILayout.Space(6);

        // Circuito
        EditorGUILayout.LabelField("Circuito", EditorStyles.boldLabel);
        _ttlVoltage = EditorGUILayout.FloatField("Voltaje TTL Arduino (V)",        _ttlVoltage);
        _wrongPin   = EditorGUILayout.IntField("Pin defectuoso (D?)",               _wrongPin);
        _correctPin = EditorGUILayout.IntField("Pin correcto   (D?)",               _correctPin);
        _wrongRes   = EditorGUILayout.FloatField("Resistor buzzer defectuoso (ohm)", _wrongRes);
        _correctRes = EditorGUILayout.FloatField("Resistor buzzer correcto   (ohm)", _correctRes);

        EditorGUILayout.Space(6);

        // Rails
        EditorGUILayout.LabelField("Asignacion de rails (fila de grilla)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Cada fila queda en un rail electrico.\nAjusta si tu protoboard tiene otro layout.",
            MessageType.Info);
        _rowVCC = EditorGUILayout.IntField("Fila VCC  (+5V poder)", _rowVCC);
        _rowGND = EditorGUILayout.IntField("Fila GND  (0V tierra)", _rowGND);
        _rowDIG = EditorGUILayout.IntField("Fila DIG  (salida D13)", _rowDIG);
        _rowMID = EditorGUILayout.IntField("Fila MID  (nodo medio)", _rowMID);
        _rowADC = EditorGUILayout.IntField("Fila ADC  (entrada A0)", _rowADC);

        EditorGUILayout.Space(6);

        // NodePoints
        EditorGUILayout.LabelField("NodePoints de medicion", EditorStyles.boldLabel);
        _npPadSize = EditorGUILayout.FloatField("Diametro pad (m)",  _npPadSize);
        _npRadius  = EditorGUILayout.FloatField("Radio trigger (m)", _npRadius);

        EditorGUILayout.Space(8);

        // Boton principal
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("CONSTRUIR RETO 4 COMPLETO", GUILayout.Height(36)))
            BuildAll();
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Pasos individuales:", EditorStyles.centeredGreyMiniLabel);
        if (GUILayout.Button("1. Crear / vincular ArduinoCore"))                   Step1_Arduino();
        if (GUILayout.Button("2. Asignar railIds a ProtoboardSlots"))               Step2_Rails();
        if (GUILayout.Button("3. Crear componentes defectuosos en protoboard"))     Step3_FaultyComps();
        if (GUILayout.Button("4. Crear slots de reemplazo (ComponentSlot)"))        Step4_ReplacementSlots();
        if (GUILayout.Button("5. Crear NodePoints de medicion"))                    Step5_NodePoints();

        EditorGUILayout.Space(6);

        // Log
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Log:", EditorStyles.miniLabel);
        if (GUILayout.Button("Limpiar", GUILayout.Width(60)))
            _log = "";
        EditorGUILayout.EndHorizontal();

        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(110));
        EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Auto-detect
    // ─────────────────────────────────────────────────────────────────────
    void AutoDetect()
    {
        var zone = GameObject.Find("Reto4_Zone");
        if (zone != null) { _reto4Zone = zone; Log("Reto4_Zone encontrado."); }
        else Log("Reto4_Zone no encontrado en escena.");

        var sim = Object.FindAnyObjectByType<ProtoboardSimulator>();
        if (sim != null) { _protoboardGO = sim.gameObject; Log("Protoboard: " + sim.gameObject.name); }
        else
        {
            string[] names = { "Bareboard", "Protoboard", "Slots_Matriz" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) { _protoboardGO = go; Log("Protoboard: " + go.name); break; }
            }
            if (_protoboardGO == null) Log("Protoboard no encontrado. Arrastralo manualmente.");
        }

        var core = Object.FindAnyObjectByType<ArduinoCore>();
        if (core != null) { _arduinoGO = core.gameObject; Log("Arduino GO: " + core.gameObject.name); }
        else
        {
            string[] names = { "Arduino_Board", "Arduino_WrongPin", "Arduino", "Controller Board" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) { _arduinoGO = go; Log("Arduino GO: " + go.name); break; }
            }
            if (_arduinoGO == null) Log("Arduino GO no encontrado. El Paso 1 lo creara.");
        }

        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Build All
    // ─────────────────────────────────────────────────────────────────────
    void BuildAll()
    {
        if (!CheckProtoboard()) return;
        EnsureMatFolder();
        Step1_Arduino();
        Step2_Rails();
        Step3_FaultyComps();
        Step4_ReplacementSlots();
        Step5_NodePoints();
        AssetDatabase.SaveAssets();
        Log("=== Reto 4 construido. ===");
        EditorUtility.DisplayDialog("Reto 4 listo",
            "Configuracion completada.\n\n" +
            "Fallas activas:\n" +
            "  Pin D" + _wrongPin + " incorrecto -> correcto D" + _correctPin + "\n" +
            "  Resistor: " + _wrongRes + " ohm -> correcto " + _correctRes + " ohm\n" +
            "  hasLooseCable = true\n\n" +
            "Proximo paso: verificar railIds en el Inspector.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PASO 1 - ArduinoCore
    // ─────────────────────────────────────────────────────────────────────
    void Step1_Arduino()
    {
        EnsureMatFolder();

        if (_arduinoGO == null)
        {
            var parent = _reto4Zone != null ? _reto4Zone.transform : null;
            _arduinoGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _arduinoGO.name = "Arduino_Board";
            if (parent != null) _arduinoGO.transform.SetParent(parent);
            _arduinoGO.transform.localPosition = new Vector3(-0.25f, 0.018f, 0f);
            _arduinoGO.transform.localScale    = new Vector3(0.18f, 0.014f, 0.07f);
            Object.DestroyImmediate(_arduinoGO.GetComponent<Collider>());
            var bc = _arduinoGO.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            var rb = _arduinoGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
            PaintGO(_arduinoGO, CArdu, "Arduino_Board_mat");
            Undo.RegisterCreatedObjectUndo(_arduinoGO, "Crear Arduino_Board");
            Log("Arduino_Board creado.");
        }

        var acore = _arduinoGO.GetComponent<ArduinoCore>() ?? _arduinoGO.AddComponent<ArduinoCore>();
        acore.activePinNumber  = _wrongPin;
        acore.activePinMode    = PinMode.OUTPUT;
        acore.activePinState   = PinState.HIGH;
        acore.outputVoltageTTL = _ttlVoltage;

        acore.nodoP13 = GetOrMakeChildNode(_arduinoGO.transform, "Nodo_P13", new Vector3( 0.07f, 0f,  0.02f));
        acore.nodoGND = GetOrMakeChildNode(_arduinoGO.transform, "Nodo_GND", new Vector3( 0.07f, 0f, -0.02f));
        acore.nodoA0  = GetOrMakeChildNode(_arduinoGO.transform, "Nodo_A0",  new Vector3(-0.07f, 0f,  0.02f));
        acore.nodoGND.voltage = 0f;
        acore.nodoP13.voltage = _ttlVoltage;

        EditorUtility.SetDirty(_arduinoGO);
        Log("ArduinoCore: pin activo D" + _wrongPin + ", TTL=" + _ttlVoltage + "V.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PASO 2 - ProtoboardSlots + ProtoboardSimulator
    // ─────────────────────────────────────────────────────────────────────
    void Step2_Rails()
    {
        if (!CheckProtoboard()) return;

        var compSlots = _protoboardGO.GetComponentsInChildren<ComponentSlot>(true).ToList();
        if (compSlots.Count == 0)
        {
            Log("ERROR: No hay ComponentSlots. Genera la grilla primero.");
            return;
        }

        // Agregar ProtoboardSlot a cada ComponentSlot GO
        var protoSlots = new List<ProtoboardSlot>();
        foreach (var cs in compSlots)
        {
            var ps  = cs.gameObject.GetComponent<ProtoboardSlot>()
                   ?? cs.gameObject.AddComponent<ProtoboardSlot>();
            ps.col    = ParseSlotCol(cs.gameObject.name, cs.transform.localPosition);
            ps.row    = ParseSlotRow(cs.gameObject.name, cs.transform.localPosition);
            ps.railId = RowToRailId(ps.row);
            protoSlots.Add(ps);
            EditorUtility.SetDirty(cs.gameObject);
        }

        // ProtoboardSimulator
        var psim = _protoboardGO.GetComponent<ProtoboardSimulator>()
                ?? _protoboardGO.AddComponent<ProtoboardSimulator>();
        psim.todosLosSlots = protoSlots;
        EditorUtility.SetDirty(psim.gameObject);

        // ElectricalNode en el primer slot de cada rail
        var firstOfRail = new Dictionary<string, ProtoboardSlot>();
        foreach (var ps in protoSlots)
            if (!firstOfRail.ContainsKey(ps.railId)) firstOfRail[ps.railId] = ps;

        foreach (var kv in firstOfRail)
        {
            var en = kv.Value.gameObject.GetComponent<ElectricalNode>()
                  ?? kv.Value.gameObject.AddComponent<ElectricalNode>();
            en.voltage = 0f;
            kv.Value.assignedNode = en;
            EditorUtility.SetDirty(kv.Value.gameObject);
        }

        // Reenlazar ArduinoCore a los nodos de rails
        if (_arduinoGO != null)
        {
            var acore = _arduinoGO.GetComponent<ArduinoCore>();
            if (acore != null)
            {
                ElectricalNode enDIG = FirstRailNode(firstOfRail, "DIG");
                ElectricalNode enGND = FirstRailNode(firstOfRail, "GND");
                ElectricalNode enADC = FirstRailNode(firstOfRail, "ADC");
                if (enDIG != null) acore.nodoP13 = enDIG;
                if (enGND != null) acore.nodoGND = enGND;
                if (enADC != null) acore.nodoA0  = enADC;
                EditorUtility.SetDirty(_arduinoGO);
                Log("ArduinoCore reenlazado: nodoP13->DIG, nodoGND->GND, nodoA0->ADC.");
            }
        }

        string railList = string.Join(", ", firstOfRail.Keys.OrderBy(k => k));
        Log(protoSlots.Count + " ProtoboardSlots configurados | Rails: " + railList);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PASO 3 - Componentes defectuosos
    // ─────────────────────────────────────────────────────────────────────
    void Step3_FaultyComps()
    {
        if (!CheckProtoboard()) return;
        if (!CheckSim(out var psim)) return;

        var nDIG = FindRailNode(psim, "DIG");
        var nMID = FindRailNode(psim, "MID");
        var nGND = FindRailNode(psim, "GND");

        if (nDIG == null || nMID == null || nGND == null)
        {
            Log("ERROR: Faltan nodos DIG/MID/GND. Ejecuta el Paso 2 primero.");
            return;
        }

        // Arduino_WrongPin
        var pinGO = GetOrMakePrimChild(_protoboardGO, "Arduino_WrongPin", PrimitiveType.Cube);
        pinGO.transform.localPosition = RailLocalCenter(psim, "DIG") + new Vector3(0f, 0.020f, 0f);
        pinGO.transform.localScale    = new Vector3(0.025f, 0.022f, 0.018f);
        PaintGO(pinGO, CArdu, "R4_ArduPin_mat");
        SetupRigidbody(pinGO);
        EnsureBoxCollider(pinGO, false);

        var apin = pinGO.GetComponent<ArduinoPin>() ?? pinGO.AddComponent<ArduinoPin>();
        apin.pinNumber        = _wrongPin;
        apin.correctPinNumber = _correctPin;
        apin.hasFault         = true;
        apin.hasLooseCable    = true;
        apin.nodeA            = nDIG;
        apin.nodeB            = nMID;
        EditorUtility.SetDirty(pinGO);
        Log("Arduino_WrongPin: D" + _wrongPin + " hasFault=true, hasLooseCable=true.");

        // Resistor_Buzzer_Missing
        var buzzGO = GetOrMakePrimChild(_protoboardGO, "Resistor_Buzzer_Missing", PrimitiveType.Cube);
        buzzGO.transform.localPosition = RailLocalCenter(psim, "MID") + new Vector3(0.045f, 0.030f, 0f);
        buzzGO.transform.localScale    = new Vector3(0.012f, 0.040f, 0.012f);
        SetupRigidbody(buzzGO);
        EnsureBoxCollider(buzzGO, false);

        if (!ApplyFbxVisual(buzzGO, FBX_RES, 0.10f))
            PaintGO(buzzGO, CRed, "R4_BuzzRes_mat");

        var res = buzzGO.GetComponent<Resistor>() ?? buzzGO.AddComponent<Resistor>();
        res.resistance        = _wrongRes;
        res.faultyResistance  = _wrongRes;
        res.correctResistance = _correctRes;
        res.hasFault          = true;
        res.nodeA             = nMID;
        res.nodeB             = nGND;
        EditorUtility.SetDirty(buzzGO);
        Log("Resistor_Buzzer_Missing: " + _wrongRes + " ohm -> correcto " + _correctRes + " ohm.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PASO 4 - Slots de reemplazo
    // ─────────────────────────────────────────────────────────────────────
    void Step4_ReplacementSlots()
    {
        if (!CheckProtoboard()) return;
        if (!CheckSim(out var psim)) return;

        var container = _reto4Zone != null ? _reto4Zone : _protoboardGO;

        // Slot ArduinoPin
        Vector3 pinWorldPos = _protoboardGO.transform.TransformPoint(
            RailLocalCenter(psim, "DIG") + new Vector3(0f, 0.10f, 0f));
        var slotPin = MakeSlot(container, pinWorldPos, ComponentSlotType.ArduinoPin, "Slot_R4_Arduino");
        var wPin = _protoboardGO.transform.Find("Arduino_WrongPin");
        if (wPin != null) slotPin.damagedComponent = wPin.gameObject;

        // Slot Resistor
        Vector3 resWorldPos = _protoboardGO.transform.TransformPoint(
            RailLocalCenter(psim, "MID") + new Vector3(0.045f, 0.10f, 0f));
        var slotRes = MakeSlot(container, resWorldPos, ComponentSlotType.Resistor, "Slot_R4_Resistor");
        var wRes = _protoboardGO.transform.Find("Resistor_Buzzer_Missing");
        if (wRes != null) slotRes.damagedComponent = wRes.gameObject;

        Log("Slots de reemplazo creados: Slot_R4_Arduino + Slot_R4_Resistor.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PASO 5 - NodePoints
    // ─────────────────────────────────────────────────────────────────────
    void Step5_NodePoints()
    {
        if (!CheckProtoboard()) return;
        if (!CheckSim(out var psim)) return;

        var container = _reto4Zone != null ? _reto4Zone : _protoboardGO;
        string[] rails = { "VCC", "GND", "DIG", "MID", "ADC" };

        foreach (var railId in rails)
        {
            var node = FindRailNode(psim, railId);
            if (node == null) { Log("Rail '" + railId + "' sin nodo. Omitido."); continue; }

            Vector3 localPos  = RailLocalCenter(psim, railId) + new Vector3(0f, 0.006f, 0f);
            Vector3 worldPos  = _protoboardGO.transform.TransformPoint(localPos);
            MakeNodePoint(container, node, worldPos, CGold, "NP_R4_" + railId, _npPadSize, _npRadius);
        }

        Log("NodePoints creados: VCC, GND, DIG, MID, ADC.");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════

    bool CheckProtoboard()
    {
        if (_protoboardGO != null) return true;
        Log("ERROR: Arrastra la Protoboard al campo 'Protoboard' y pulsa Auto-detectar.");
        return false;
    }

    bool CheckSim(out ProtoboardSimulator psim)
    {
        psim = _protoboardGO != null ? _protoboardGO.GetComponent<ProtoboardSimulator>() : null;
        if (psim != null) return true;
        Log("ERROR: ProtoboardSimulator no encontrado. Ejecuta el Paso 2 primero.");
        return false;
    }

    // Rail helpers
    string RowToRailId(int row)
    {
        if (row == _rowVCC) return "VCC";
        if (row == _rowGND) return "GND";
        if (row == _rowDIG) return "DIG";
        if (row == _rowMID) return "MID";
        if (row == _rowADC) return "ADC";
        return "F" + row;
    }

    ElectricalNode FindRailNode(ProtoboardSimulator psim, string railId)
    {
        if (psim == null) return null;
        foreach (var ps in psim.todosLosSlots)
            if (ps != null && ps.railId == railId)
            {
                var en = ps.gameObject.GetComponent<ElectricalNode>();
                if (en != null) return en;
            }
        return null;
    }

    static ElectricalNode FirstRailNode(Dictionary<string, ProtoboardSlot> dict, string key)
    {
        if (!dict.TryGetValue(key, out var ps) || ps == null) return null;
        return ps.gameObject.GetComponent<ElectricalNode>();
    }

    Vector3 RailLocalCenter(ProtoboardSimulator psim, string railId)
    {
        if (psim == null) return Vector3.zero;
        var slots = psim.todosLosSlots.Where(s => s != null && s.railId == railId).ToList();
        if (slots.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var s in slots) sum += s.transform.localPosition;
        return sum / slots.Count;
    }

    // Parsing nombre "Slot_{col}_{row}"
    static int ParseSlotCol(string goName, Vector3 localPos)
    {
        var parts = goName.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int c)) return c;
        return Mathf.RoundToInt(localPos.x / 0.018f);
    }

    static int ParseSlotRow(string goName, Vector3 localPos)
    {
        var parts = goName.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[2], out int r)) return r;
        return Mathf.RoundToInt(localPos.z / 0.018f);
    }

    // Object construction helpers
    static ElectricalNode GetOrMakeChildNode(Transform parent, string nodeName, Vector3 offset)
    {
        var ex = parent.Find(nodeName);
        if (ex != null)
            return ex.GetComponent<ElectricalNode>() ?? ex.gameObject.AddComponent<ElectricalNode>();
        var go = new GameObject(nodeName);
        go.transform.SetParent(parent);
        go.transform.localPosition = offset;
        Undo.RegisterCreatedObjectUndo(go, nodeName);
        return go.AddComponent<ElectricalNode>();
    }

    static GameObject GetOrMakePrimChild(GameObject parent, string childName, PrimitiveType prim)
    {
        var ex = parent.transform.Find(childName);
        if (ex != null) return ex.gameObject;
        var go = GameObject.CreatePrimitive(prim);
        go.name = childName;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        Undo.RegisterCreatedObjectUndo(go, childName);
        return go;
    }

    static void SetupRigidbody(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    static void EnsureBoxCollider(GameObject go, bool isTrigger)
    {
        var bc = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        bc.isTrigger = isTrigger;
    }

    static bool ApplyFbxVisual(GameObject root, string fbxPath, float targetSize)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null) return false;
        var old = root.transform.Find("Visual");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var vis = Object.Instantiate(fbx, root.transform);
        vis.name = "Visual";
        vis.transform.localPosition = Vector3.zero;
        float longest = 0f;
        foreach (var mf in vis.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null)
            {
                var sz = mf.sharedMesh.bounds.size;
                longest = Mathf.Max(longest, sz.x, sz.y, sz.z);
            }
        float s = longest > 0.001f ? targetSize / longest : 1f;
        vis.transform.localScale = Vector3.one * s;
        foreach (var c in vis.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        return true;
    }

    ComponentSlot MakeSlot(GameObject parent, Vector3 worldPos,
                           ComponentSlotType slotType, string slotName, float size = 0.065f)
    {
        var ex = parent.transform.Find(slotName);
        if (ex != null) Undo.DestroyObjectImmediate(ex.gameObject);

        var go = new GameObject(slotName);
        go.transform.SetParent(parent.transform);
        go.transform.position = worldPos;

        var bc = go.AddComponent<BoxCollider>();
        bc.size      = new Vector3(size, 0.04f, size);
        bc.isTrigger = true;

        var slot = go.AddComponent<ComponentSlot>();
        slot.acceptedType  = slotType;
        slot.installAnchor = go.transform;

        var ind = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ind.name = "SlotIndicator";
        ind.transform.SetParent(go.transform);
        ind.transform.localPosition = Vector3.zero;
        ind.transform.localScale    = new Vector3(size, 0.008f, size);
        Object.DestroyImmediate(ind.GetComponent<Collider>());
        PaintGO(ind, CSlot, slotName + "_ind_mat");
        slot.slotRenderer = ind.GetComponent<Renderer>();

        Undo.RegisterCreatedObjectUndo(go, slotName);
        return slot;
    }

    void MakeNodePoint(GameObject parent, ElectricalNode node, Vector3 worldPos,
                       Color color, string ptName, float padSize, float radius)
    {
        var exPad = parent.transform.Find(ptName + "_Pad");
        var exDet = parent.transform.Find(ptName);
        if (exPad != null) Undo.DestroyObjectImmediate(exPad.gameObject);
        if (exDet != null) Undo.DestroyObjectImmediate(exDet.gameObject);

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = ptName + "_Pad";
        pad.transform.SetParent(parent.transform);
        pad.transform.position   = worldPos;
        pad.transform.localScale = new Vector3(padSize, 0.003f, padSize);
        Object.DestroyImmediate(pad.GetComponent<Collider>());
        PaintGO(pad, color, ptName + "_mat");
        Undo.RegisterCreatedObjectUndo(pad, ptName + "_Pad");

        var det = new GameObject(ptName);
        det.transform.SetParent(parent.transform);
        det.transform.position = worldPos + new Vector3(0f, 0.005f, 0f);
        var sc = det.AddComponent<SphereCollider>();
        sc.isTrigger = false;
        sc.radius    = radius;
        det.AddComponent<XRSimpleInteractable>();
        var ni = det.AddComponent<NodeInteractable>();
        ni.nodeTarget = node;
        EditorUtility.SetDirty(det);
        Undo.RegisterCreatedObjectUndo(det, ptName);
    }

    // Material helpers
    static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Materials", "Retos");
    }

    static void PaintGO(GameObject go, Color color, string matName)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        string path = MAT_FOLDER + "/" + matName + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    void Log(string msg)
    {
        _log = "[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n" + _log;
        Debug.Log("[Reto4Tool] " + msg);
        Repaint();
    }
}
#endif
