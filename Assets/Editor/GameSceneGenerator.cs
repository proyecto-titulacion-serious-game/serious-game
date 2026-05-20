using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Genera las 4 zonas de reto del Serious Game VR en la escena activa.
/// Crea jerarquía, componentes eléctricos, nodos, slots de reparación y labels.
/// Conecta automáticamente el GameManager con las zonas generadas.
///
/// Menú: Tools → TITA → Generar Zonas de Juego
///
/// Las zonas se crean bajo un GameObject padre "GameZones".
/// Solo Reto1_Zone arranca activa; las demás están desactivadas.
/// El GameManager las activa/desactiva automáticamente al avanzar retos.
/// </summary>
public static class GameSceneGenerator
{
    private const string PARENT_NAME = "GameZones";

    [MenuItem("Tools/TITA/Generar Zonas de Juego")]
    public static void Generate()
    {
        var existing = GameObject.Find(PARENT_NAME);
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Zonas ya existen",
                $"Ya existe '{PARENT_NAME}' en la escena.\n¿Eliminar y regenerar?",
                "Sí, regenerar", "Cancelar");
            if (!overwrite) return;
            Object.DestroyImmediate(existing);
        }

        var parent = new GameObject(PARENT_NAME);

        var z1 = CreateReto1(parent);
        var z2 = CreateReto2(parent);
        var z3 = CreateReto3(parent);
        var z4 = CreateReto4(parent);

        // Solo el primer reto arranca activo
        z1.SetActive(true);
        z2.SetActive(false);
        z3.SetActive(false);
        z4.SetActive(false);

        WireGameManager(z1, z2, z3, z4);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Zonas generadas",
            "Se crearon las 4 zonas bajo 'GameZones'.\n\n" +
            "  Reto1_Zone  — activo al inicio\n" +
            "  Reto2_Zone  — desactivado\n" +
            "  Reto3_Zone  — desactivado\n" +
            "  Reto4_Zone  — desactivado\n\n" +
            "GameManager conectado automáticamente.\n\n" +
            "PASOS SIGUIENTES:\n" +
            "1. Posiciona cada zona en el espacio físico del juego.\n" +
            "2. Asigna ComponentDeliverySystem a los ComponentSlots si aplica.\n" +
            "3. Ajusta las posiciones locales de los componentes.",
            "OK");

        Debug.Log("[GameSceneGenerator] Zonas de juego generadas exitosamente.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RETO 1 — Circuito Serie · Ley de Ohm
    //  Falla: resistencia con valor incorrecto (10 Ω en vez de 100 Ω)
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject CreateReto1(GameObject parent)
    {
        var zone = CreateZone(parent, "Reto1_Zone", new Vector3(-6f, 0f, 0f),
                              reto1: true);
        var cm = CreateCircuitManager(zone, CircuitTopology.Series);

        // Fuente de voltaje 9 V
        var vs = CreateComp<VoltageSource>(zone, "VoltageSource_R1",
            new Vector3(0f, 0.8f, -0.4f), PrimitiveType.Cylinder,
            new Color(0.9f, 0.7f, 0.1f), new Vector3(0.06f, 0.1f, 0.06f));
        vs.voltage = 9f;

        // Resistor defectuoso: arranca con 10 Ω, correcto es 100 Ω
        var res = CreateComp<Resistor>(zone, "Resistor_R1",
            new Vector3(-0.25f, 0.8f, 0.05f), PrimitiveType.Cylinder,
            new Color(0.65f, 0.35f, 0.1f), new Vector3(0.06f, 0.08f, 0.06f));
        res.faultyResistance  = 10f;
        res.correctResistance = 100f;
        res.resistance        = 10f;
        res.hasFault          = true;

        // LED indica si el circuito funciona
        var led = CreateComp<LED>(zone, "LED_R1",
            new Vector3(0.25f, 0.8f, 0.05f), PrimitiveType.Sphere,
            new Color(0.1f, 0.9f, 0.1f), Vector3.one * 0.06f);
        led.resistance = 50f;

        cm.components.Add(vs);
        cm.components.Add(res);
        cm.components.Add(led);

        // Slot donde el Explorador instala la resistencia correcta
        CreateSlot(zone, "Slot_Resistor_R1",
            new Vector3(-0.25f, 0.8f, 0.45f), ComponentSlotType.Resistor);

        CreateZoneLabel(zone, "RETO 1\nLey de Ohm\nResistencia serie",
                        new Color(1f, 0.85f, 0.2f));
        CreateDiagramPanel(zone, cm);
        return zone;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RETO 2 — Circuito Paralelo · Rama abierta
    //  Falla: LED1 con resistencia 9999 Ω (circuito abierto, no enciende)
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject CreateReto2(GameObject parent)
    {
        var zone = CreateZone(parent, "Reto2_Zone", new Vector3(-2f, 0f, 0f),
                              reto2: true);
        var cm = CreateCircuitManager(zone, CircuitTopology.Parallel);

        var vs = CreateComp<VoltageSource>(zone, "VoltageSource_R2",
            new Vector3(0f, 0.8f, -0.4f), PrimitiveType.Cylinder,
            new Color(0.9f, 0.7f, 0.1f), new Vector3(0.06f, 0.1f, 0.06f));
        vs.voltage = 9f;

        // Resistor de protección (correcto desde el inicio)
        var res = CreateComp<Resistor>(zone, "Resistor_R2",
            new Vector3(0f, 0.8f, -0.1f), PrimitiveType.Cylinder,
            new Color(0.65f, 0.35f, 0.1f), new Vector3(0.06f, 0.08f, 0.06f));
        res.correctResistance = 50f;
        res.resistance        = 50f;
        res.hasFault          = false;

        // LED1 defectuoso — rama abierta (9999 Ω)
        var led1 = CreateComp<LED>(zone, "LED1_Broken_R2",
            new Vector3(-0.25f, 0.8f, 0.15f), PrimitiveType.Sphere,
            new Color(0.85f, 0.15f, 0.15f), Vector3.one * 0.06f);
        led1.resistance = 9999f;

        // LED2 normal — sirve de referencia visual
        var led2 = CreateComp<LED>(zone, "LED2_Normal_R2",
            new Vector3(0.25f, 0.8f, 0.15f), PrimitiveType.Sphere,
            new Color(0.1f, 0.9f, 0.1f), Vector3.one * 0.06f);
        led2.resistance = 50f;

        cm.components.Add(vs);
        cm.components.Add(res);
        cm.components.Add(led1);
        cm.components.Add(led2);

        // Slot para reemplazar el LED roto
        CreateSlot(zone, "Slot_LED_R2",
            new Vector3(-0.25f, 0.8f, 0.45f), ComponentSlotType.LED);

        CreateZoneLabel(zone, "RETO 2\nCircuito Paralelo\nRama abierta",
                        new Color(0.3f, 0.9f, 1f));
        CreateDiagramPanel(zone, cm);
        return zone;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RETO 3 — Circuito Mixto · 3 fallas simultáneas
    //  Fallas: LED polaridad invertida · Capacitor polaridad invertida ·
    //          Resistor con código de colores erróneo (470 Ω → 220 Ω)
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject CreateReto3(GameObject parent)
    {
        var zone = CreateZone(parent, "Reto3_Zone", new Vector3(2f, 0f, 0f),
                              reto3: true);
        var cm = CreateCircuitManager(zone, CircuitTopology.Mixed);

        var vs = CreateComp<VoltageSource>(zone, "VoltageSource_R3",
            new Vector3(0f, 0.8f, -0.4f), PrimitiveType.Cylinder,
            new Color(0.9f, 0.7f, 0.1f), new Vector3(0.06f, 0.1f, 0.06f));
        vs.voltage = 9f;

        // Resistor con código de colores incorrecto
        var res = CreateComp<Resistor>(zone, "Resistor_R3",
            new Vector3(0f, 0.8f, -0.05f), PrimitiveType.Cylinder,
            new Color(0.65f, 0.35f, 0.1f), new Vector3(0.06f, 0.08f, 0.06f));
        res.faultyResistance  = 470f;
        res.correctResistance = 220f;
        res.resistance        = 470f;
        res.hasFault          = true;

        // LED con polaridad invertida
        var led = CreateComp<LED>(zone, "LED_R3",
            new Vector3(-0.28f, 0.8f, 0.2f), PrimitiveType.Sphere,
            new Color(0.95f, 0.45f, 0.1f), Vector3.one * 0.06f);
        led.resistance       = 50f;
        led.polarityInverted = true;

        // Capacitor con polaridad invertida
        var cap = CreateComp<Capacitor>(zone, "Capacitor_R3",
            new Vector3(0.28f, 0.8f, 0.2f), PrimitiveType.Cylinder,
            new Color(0.15f, 0.4f, 0.9f), new Vector3(0.05f, 0.1f, 0.05f));
        cap.polarityInverted = true;

        cm.components.Add(vs);
        cm.components.Add(res);
        cm.components.Add(led);
        cm.components.Add(cap);

        // Un slot por cada falla
        CreateSlot(zone, "Slot_Resistor_R3",
            new Vector3(0f, 0.8f, 0.5f), ComponentSlotType.Resistor);
        CreateSlot(zone, "Slot_LED_R3",
            new Vector3(-0.28f, 0.8f, 0.5f), ComponentSlotType.LED);
        CreateSlot(zone, "Slot_Capacitor_R3",
            new Vector3(0.28f, 0.8f, 0.5f), ComponentSlotType.Capacitor);

        CreateZoneLabel(zone, "RETO 3\nCircuito Mixto\n3 fallas simultáneas",
                        new Color(1f, 0.5f, 0.2f));
        CreateDiagramPanel(zone, cm);
        return zone;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RETO 4 — Arduino · Sensor + Buzzer
    //  Fallas: pin incorrecto · resistencia buzzer ausente · cable suelto
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject CreateReto4(GameObject parent)
    {
        var zone = CreateZone(parent, "Reto4_Zone", new Vector3(6f, 0f, 0f),
                              reto4: true);
        var cm = CreateCircuitManager(zone, CircuitTopology.Mixed);

        // Arduino usa 5 V
        var vs = CreateComp<VoltageSource>(zone, "VoltageSource_R4",
            new Vector3(0f, 0.8f, -0.4f), PrimitiveType.Cylinder,
            new Color(0.9f, 0.7f, 0.1f), new Vector3(0.06f, 0.1f, 0.06f));
        vs.voltage = 5f;

        // ArduinoPin — pin incorrecto + cable suelto
        var pin = CreateComp<ArduinoPin>(zone, "ArduinoPin_R4",
            new Vector3(-0.22f, 0.8f, 0.1f), PrimitiveType.Cube,
            new Color(0.1f, 0.5f, 0.9f), new Vector3(0.08f, 0.04f, 0.06f));
        pin.correctPinNumber = 2;
        pin.pinNumber        = 4;   // incorrecto al inicio
        pin.hasFault         = true;
        pin.hasLooseCable    = true;

        // Resistor del buzzer — falta (0 Ω), correcto 330 Ω
        var res = CreateComp<Resistor>(zone, "Resistor_Buzzer_R4",
            new Vector3(0.22f, 0.8f, 0.1f), PrimitiveType.Cylinder,
            new Color(0.65f, 0.35f, 0.1f), new Vector3(0.06f, 0.08f, 0.06f));
        res.faultyResistance  = 0f;
        res.correctResistance = 330f;
        res.resistance        = 0f;
        res.hasFault          = true;

        cm.components.Add(vs);
        cm.components.Add(pin);
        cm.components.Add(res);

        // Slot para el pin del Arduino
        CreateSlot(zone, "Slot_Arduino_R4",
            new Vector3(-0.22f, 0.8f, 0.5f), ComponentSlotType.ArduinoPin);
        // Slot para la resistencia del buzzer
        CreateSlot(zone, "Slot_Resistor_R4",
            new Vector3(0.22f, 0.8f, 0.5f), ComponentSlotType.Resistor);

        CreateZoneLabel(zone, "RETO 4\nArduino\nSensor + Buzzer",
                        new Color(0.4f, 1f, 0.5f));
        return zone;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Conectar GameManager
    // ─────────────────────────────────────────────────────────────────────────
    static void WireGameManager(GameObject z1, GameObject z2,
                                 GameObject z3, GameObject z4)
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[GameSceneGenerator] GameManager no encontrado en la escena. " +
                             "Asigna las zonas manualmente en el Inspector.");
            return;
        }

        gm.reto1Zone = z1;
        gm.reto2Zone = z2;
        gm.reto3Zone = z3;
        gm.reto4Zone = z4;
        EditorUtility.SetDirty(gm);
        Debug.Log("[GameSceneGenerator] GameManager conectado a las 4 zonas.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject CreateZone(GameObject parent, string name, Vector3 worldPos,
                                  bool reto1 = false, bool reto2 = false,
                                  bool reto3 = false, bool reto4 = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;

        // ChallengeTag marca a qué reto pertenece esta zona
        var tag              = go.AddComponent<ChallengeTag>();
        tag.reto1_OhmLaw     = reto1;
        tag.reto2_Parallel   = reto2;
        tag.reto3_Mixed      = reto3;
        tag.reto4_Arduino    = reto4;

        // Escalado por proximidad: arranca pequeño, crece cuando el Explorador se acerca
        var scaler                   = go.AddComponent<ZoneProximityScaler>();
        scaler.factorMinimo          = 0.25f;
        scaler.factorMaximo          = 1.0f;
        scaler.distanciaActivacion   = 4.0f;
        scaler.distanciaCompleta     = 1.5f;
        scaler.velocidad             = 5f;

        return go;
    }

    static CircuitManager CreateCircuitManager(GameObject zone, CircuitTopology topology)
    {
        var cmGO = new GameObject("CircuitManager");
        cmGO.transform.SetParent(zone.transform, false);
        var cm   = cmGO.AddComponent<CircuitManager>();
        cm.topology = topology;
        return cm;
    }

    static T CreateComp<T>(GameObject zone, string name, Vector3 localPos,
                            PrimitiveType meshType, Color color, Vector3 scale)
        where T : ElectricalComponent
    {
        var go = new GameObject(name);
        go.transform.SetParent(zone.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        // Renderer primero — LED y Capacitor tienen [RequireComponent(typeof(Renderer))]
        go.AddComponent<MeshFilter>().sharedMesh     = GetMesh(meshType);
        go.AddComponent<MeshRenderer>().sharedMaterial = CreateMat(name + "_Mat", color);
        go.AddComponent<BoxCollider>();

        var comp   = go.AddComponent<T>();
        comp.nodeA = CreateNode(go, "NodeA", new Vector3(-0.6f, 0f, 0f));
        comp.nodeB = CreateNode(go, "NodeB", new Vector3( 0.6f, 0f, 0f));

        return comp;
    }

    static ElectricalNode CreateNode(GameObject parent, string name, Vector3 localPos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;

        var col      = go.AddComponent<SphereCollider>();
        col.radius   = 0.4f;
        col.isTrigger = true;

        return go.AddComponent<ElectricalNode>();
    }

    static void CreateSlot(GameObject zone, string name,
                            Vector3 localPos, ComponentSlotType type)
    {
        var go = new GameObject(name);
        go.transform.SetParent(zone.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = new Vector3(0.12f, 0.04f, 0.12f);

        go.AddComponent<MeshFilter>().sharedMesh     = GetMesh(PrimitiveType.Cube);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = CreateMat(name + "_SlotMat",
                                      new Color(0.18f, 0.22f, 0.32f));

        var col        = go.AddComponent<BoxCollider>();
        col.isTrigger  = true;
        col.size       = new Vector3(1f, 2f, 1f);   // en espacio local del slot

        var slot          = go.AddComponent<ComponentSlot>();
        slot.acceptedType = type;
        slot.slotRenderer = mr;
    }

    static void CreateZoneLabel(GameObject zone, string text, Color color)
    {
        var go = new GameObject("Zone_Label");
        go.transform.SetParent(zone.transform, false);
        go.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        go.transform.localScale    = Vector3.one * 0.003f;

        var canvas        = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(210f, 90f);

        var txtGO = new GameObject("TMP_ZoneLabel");
        txtGO.transform.SetParent(go.transform, false);

        var tmp               = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = 12f;
        tmp.color             = color;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        var txtRT           = txtGO.GetComponent<RectTransform>();
        txtRT.sizeDelta     = new Vector2(205f, 88f);
        txtRT.anchoredPosition = Vector2.zero;
    }

    static void CreateDiagramPanel(GameObject zone, CircuitManager cm)
    {
        var go = new GameObject("CircuitDiagramPanel");
        go.transform.SetParent(zone.transform, false);
        go.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        go.transform.localScale    = Vector3.one * 0.003f;

        var canvas        = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 220f);

        // CircuitDiagramPanel se construye en Start(); asignamos el circuito ya.
        var panel    = go.AddComponent<CircuitDiagramPanel>();
        panel.circuit = cm;
    }

    static Material CreateMat(string matName, Color color)
    {
        string path     = $"Assets/Materials/{matName}.mat";
        var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Mesh GetMesh(PrimitiveType type)
    {
        var tmp = GameObject.CreatePrimitive(type);
        var m   = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        return m;
    }
}
