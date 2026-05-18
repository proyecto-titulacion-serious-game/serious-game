#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Construye las 4 zonas de reto en la escena abierta usando modelos del
/// pack "Resources Vol.2 - Electronics".
///
/// Menú: Tools → TITA → Vol.2 Electronics → Construir Retos en Escena
///
/// Estructura generada en la escena:
///   GameZones
///   ├─ Reto1_Zone   (Serie - Ley de Ohm)
///   ├─ Reto2_Zone   (Paralelo)
///   ├─ Reto3_Zone   (Mixto Serie-Paralelo)
///   └─ Reto4_Zone   (Arduino / Microcontrolador)
///
/// Cada zona contiene:
///   CircuitManager     → simula el circuito
///   VoltageSource_Obj  → batería 9V (Battery 9v.prefab)
///   Componentes eléctricos con visual Vol.2 + script eléctrico
///   ComponentSlot      → punto de instalación del componente reparado
///   Bareboard          → tablero de fondo
///
/// DESPUÉS DE CORRER:
///   1. Asignar las zonas en GameManager → reto1Zone … reto4Zone
///   2. Ajustar posiciones en la escena VR
///   3. Ejecutar Play — los nodos se auto-crean en CircuitManager.EnsureAllNodesExist()
/// </summary>
public static class Vol2RetosBuilder
{
    const string V2 = "Assets/Resources Vol.2 - Electronics/Prefabs/";

    // Parámetros de circuito según GameManager
    const float R1_FAULTY    = 10f;
    const float R1_CORRECT   = 100f;
    const float R1_LED_RES   = 50f;
    const float R1_VOLTAGE   = 9f;

    const float R2_BROKEN    = 9999f;
    const float R2_NORMAL    = 50f;

    const float R3_FAULTY_R  = 470f;
    const float R3_CORRECT_R = 220f;

    // ─────────────────────────────────────────────
    //  Menu entry
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Vol.2 Electronics/Construir Retos en Escena")]
    public static void BuildAll()
    {
        // Convertir materiales Vol.2 a URP primero
        Vol2DeliveredSetup.ConvertVol2MaterialsToURP();

        var zones = GetOrCreateGameZones();

        BuildReto1(zones);
        BuildReto2(zones);
        BuildReto3(zones);
        BuildReto4(zones);

        // Conectar al GameManager si existe
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.reto1Zone = zones.transform.Find("Reto1_Zone")?.gameObject;
            gm.reto2Zone = zones.transform.Find("Reto2_Zone")?.gameObject;
            gm.reto3Zone = zones.transform.Find("Reto3_Zone")?.gameObject;
            gm.reto4Zone = zones.transform.Find("Reto4_Zone")?.gameObject;
            EditorUtility.SetDirty(gm);
            Debug.Log("[RetosBuilder] GameManager.reto1Zone…reto4Zone asignados.");
        }
        else
        {
            Debug.LogWarning("[RetosBuilder] GameManager no encontrado en la escena. " +
                             "Asigna manualmente reto1Zone…reto4Zone en el Inspector.");
        }

        EditorUtility.DisplayDialog(
            "Retos construidos",
            "Las 4 zonas de reto fueron creadas en 'GameZones'.\n\n" +
            "  Reto 1 — Serie (Ley de Ohm): Battery + Potentiometer + Led A\n" +
            "  Reto 2 — Paralelo: Battery + Led B + Led C (polaridad invertida)\n" +
            "  Reto 3 — Mixto: Battery + Transistor + Led D + Capacitor\n" +
            "  Reto 4 — Arduino: Battery + Relay + Controller Board\n\n" +
            (gm != null
                ? "✓ GameManager ya conectado.\n"
                : "⚠ Asigna manualmente las zonas en GameManager.\n") +
            "\nAjusta las posiciones en la escena VR según sea necesario.",
            "OK");
    }

    // ─────────────────────────────────────────────
    //  RETO 1 — Serie / Ley de Ohm
    //  Falla: Potentiometer (Resistor) con valor incorrecto
    //  Reparación: entregar resistencia correcta (100 Ω)
    // ─────────────────────────────────────────────
    static void BuildReto1(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto1_Zone", new Vector3(0f, 0f, 2f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Series;

        // ── Bareboard (fondo visual) ──────────────────
        AddVisual(zone, "Bareboard", "Bareboard", new Vector3(0f, 0f, 0f),
                  new Vector3(0.18f, 0.004f, 0.22f));

        // ── Batería (VoltageSource) ───────────────────
        var battery = AddCircuitComponent<VoltageSource>(zone, "Battery 9v", "Battery_9V",
                      new Vector3(-0.07f, 0.03f, 0f),
                      new Vector3(0.04f, 0.07f, 0.03f));
        battery.voltage = R1_VOLTAGE;

        // ── Resistencia defectuosa (Potentiometer) ────
        var resistorObj = AddCircuitComponentObj(zone, "Potentiometer", "Resistor_Faulty",
                          new Vector3(0f, 0.025f, 0f),
                          new Vector3(0.035f, 0.04f, 0.035f));
        var resistor = EnsureComponent<Resistor>(resistorObj);
        resistor.resistance       = R1_FAULTY;
        resistor.faultyResistance = R1_FAULTY;
        resistor.correctResistance= R1_CORRECT;
        resistor.hasFault         = true;

        // Slot para instalar la resistencia correcta
        AddComponentSlot(resistorObj, ComponentSlotType.Resistor, new Vector3(0f, 0.05f, 0f));
        AddNodeInteractable(resistorObj);

        // ── LED ───────────────────────────────────────
        var ledObj = AddCircuitComponentObj(zone, "Led A", "LED_Output",
                     new Vector3(0.07f, 0.025f, 0f),
                     new Vector3(0.025f, 0.025f, 0.025f));
        var led = EnsureComponent<LED>(ledObj);
        led.resistance       = R1_LED_RES;
        led.polarityInverted = false;
        AddNodeInteractable(ledObj);

        Debug.Log("[RetosBuilder] ✓ Reto 1 (Serie) construido.");
    }

    // ─────────────────────────────────────────────
    //  RETO 2 — Paralelo
    //  Falla: Led C con polaridad invertida
    //  Reparación: entregar LED con polaridad correcta
    // ─────────────────────────────────────────────
    static void BuildReto2(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto2_Zone", new Vector3(0f, 0f, 2f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Parallel;

        AddVisual(zone, "Bareboard", "Bareboard", new Vector3(0f, 0f, 0f),
                  new Vector3(0.20f, 0.004f, 0.22f));

        // Batería
        var battery = AddCircuitComponent<VoltageSource>(zone, "Battery 9v", "Battery_9V",
                      new Vector3(-0.08f, 0.03f, 0f),
                      new Vector3(0.04f, 0.07f, 0.03f));
        battery.voltage = R1_VOLTAGE;

        // LED B — rama correcta
        var ledBObj = AddCircuitComponentObj(zone, "Led B", "LED_RamaA",
                      new Vector3(0.02f, 0.025f, 0.04f),
                      new Vector3(0.025f, 0.025f, 0.025f));
        var ledB = EnsureComponent<LED>(ledBObj);
        ledB.resistance       = R2_NORMAL;
        ledB.polarityInverted = false;
        AddNodeInteractable(ledBObj);

        // LED C — rama con polaridad invertida (falla)
        var ledCObj = AddCircuitComponentObj(zone, "Led C", "LED_RamaB_Faulty",
                      new Vector3(0.02f, 0.025f, -0.04f),
                      new Vector3(0.025f, 0.025f, 0.025f));
        var ledC = EnsureComponent<LED>(ledCObj);
        ledC.resistance       = R2_BROKEN;
        ledC.polarityInverted = true;

        AddComponentSlot(ledCObj, ComponentSlotType.LED, new Vector3(0f, 0.05f, 0f));
        AddNodeInteractable(ledCObj);

        Debug.Log("[RetosBuilder] ✓ Reto 2 (Paralelo) construido.");
    }

    // ─────────────────────────────────────────────
    //  RETO 3 — Mixto
    //  Fallas: resistencia incorrecta (Transistor) + Capacitor polaridad invertida
    //  Reparación: resistencia correcta Y capacitor correcto
    // ─────────────────────────────────────────────
    static void BuildReto3(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto3_Zone", new Vector3(0f, 0f, 2f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Mixed;

        AddVisual(zone, "Bareboard", "Bareboard", new Vector3(0f, 0f, 0f),
                  new Vector3(0.22f, 0.004f, 0.22f));

        // Batería
        var battery = AddCircuitComponent<VoltageSource>(zone, "Battery 9v", "Battery_9V",
                      new Vector3(-0.09f, 0.03f, 0f),
                      new Vector3(0.04f, 0.07f, 0.03f));
        battery.voltage = R1_VOLTAGE;

        // Transistor como Resistor (serie, valor incorrecto)
        var rObj = AddCircuitComponentObj(zone, "Transistor", "Resistor_Series_Faulty",
                   new Vector3(-0.02f, 0.025f, 0f),
                   new Vector3(0.025f, 0.04f, 0.025f));
        var r = EnsureComponent<Resistor>(rObj);
        r.resistance        = R3_FAULTY_R;
        r.faultyResistance  = R3_FAULTY_R;
        r.correctResistance = R3_CORRECT_R;
        r.hasFault          = true;
        AddComponentSlot(rObj, ComponentSlotType.Resistor, new Vector3(0f, 0.055f, 0f));
        AddNodeInteractable(rObj);

        // LED (paralelo, correcto)
        var ledObj = AddCircuitComponentObj(zone, "Led D", "LED_Paralelo",
                     new Vector3(0.06f, 0.025f, 0.04f),
                     new Vector3(0.025f, 0.025f, 0.025f));
        var led = EnsureComponent<LED>(ledObj);
        led.resistance       = R1_LED_RES;
        led.polarityInverted = false;
        AddNodeInteractable(ledObj);

        // Capacitor con polaridad invertida (falla)
        var capObj = AddCircuitComponentObj(zone, "Capacitor", "Capacitor_Invertido",
                     new Vector3(0.06f, 0.025f, -0.04f),
                     new Vector3(0.025f, 0.055f, 0.025f));
        var cap = EnsureComponent<Capacitor>(capObj);
        cap.polarityInverted = true;
        AddComponentSlot(capObj, ComponentSlotType.Capacitor, new Vector3(0f, 0.065f, 0f));
        AddNodeInteractable(capObj);

        Debug.Log("[RetosBuilder] ✓ Reto 3 (Mixto) construido.");
    }

    // ─────────────────────────────────────────────
    //  RETO 4 — Arduino / Microcontrolador
    //  Falla: Controller Board con pin incorrecto
    //  Reparación: entregar pin correcto
    // ─────────────────────────────────────────────
    static void BuildReto4(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto4_Zone", new Vector3(0f, 0f, 2f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Mixed;

        AddVisual(zone, "Bareboard", "Bareboard", new Vector3(0f, 0f, 0f),
                  new Vector3(0.22f, 0.004f, 0.25f));

        // Batería
        var battery = AddCircuitComponent<VoltageSource>(zone, "Battery 9v", "Battery_9V",
                      new Vector3(-0.09f, 0.03f, 0f),
                      new Vector3(0.04f, 0.07f, 0.03f));
        battery.voltage = R1_VOLTAGE;

        // Relay como Resistor (serie, correcto para no provocar cortocircuito)
        var rObj = AddCircuitComponentObj(zone, "Relay", "Resistor_Serie",
                   new Vector3(-0.02f, 0.025f, 0f),
                   new Vector3(0.035f, 0.04f, 0.025f));
        var r = EnsureComponent<Resistor>(rObj);
        r.resistance        = 100f;
        r.faultyResistance  = 100f;
        r.correctResistance = 100f;
        r.hasFault          = false;
        AddNodeInteractable(rObj);

        // Controller Board con pin incorrecto
        var boardObj = AddCircuitComponentObj(zone, "Controller Board", "Arduino_WrongPin",
                       new Vector3(0.07f, 0.02f, 0f),
                       new Vector3(0.08f, 0.015f, 0.07f));
        var pin = EnsureComponent<ArduinoPin>(boardObj);
        pin.pinNumber        = 4;   // pin incorrecto
        pin.correctPinNumber = 2;
        pin.hasFault         = true;

        // Segment Display como indicador visual del Arduino
        AddVisual(zone, "Segment Display", "Display",
                  new Vector3(0.07f, 0.04f, 0.05f),
                  new Vector3(0.04f, 0.02f, 0.025f));

        AddComponentSlot(boardObj, ComponentSlotType.ArduinoPin, new Vector3(0f, 0.035f, 0f));
        AddNodeInteractable(boardObj);

        Debug.Log("[RetosBuilder] ✓ Reto 4 (Arduino) construido.");
    }

    // ─────────────────────────────────────────────
    //  Helpers de construcción
    // ─────────────────────────────────────────────

    static GameObject GetOrCreateGameZones()
    {
        var existing = GameObject.Find("GameZones");
        if (existing != null) return existing;

        var go = new GameObject("GameZones");
        go.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Crear GameZones");
        return go;
    }

    static GameObject GetOrCreateZone(GameObject parent, string name, Vector3 localPos)
    {
        var existing = parent.transform.Find(name);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Zona ya existe",
                $"'{name}' ya existe. ¿Reemplazar?",
                "Sí, reemplazar", "Mantener");
            if (!replace) return existing.gameObject;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var zone = new GameObject(name);
        zone.transform.SetParent(parent.transform);
        zone.transform.localPosition = localPos;
        Undo.RegisterCreatedObjectUndo(zone, $"Crear {name}");
        return zone;
    }

    /// <summary>Instancia un prefab Vol.2 como hijo, solo visual (sin scripts de juego).</summary>
    static GameObject AddVisual(GameObject parent, string prefabName, string objName,
                                Vector3 localPos, Vector3 localScale)
    {
        string path = V2 + prefabName + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = objName;
        }
        else
        {
            // Fallback: cubo primitivo si no existe el prefab
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objName + "_placeholder";
            go.transform.SetParent(parent.transform);
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        Undo.RegisterCreatedObjectUndo(go, $"Crear visual {objName}");
        return go;
    }

    /// <summary>
    /// Instancia el prefab Vol.2 y le añade el script eléctrico T.
    /// Devuelve el componente eléctrico.
    /// </summary>
    static T AddCircuitComponent<T>(GameObject parent, string prefabName, string objName,
                                    Vector3 localPos, Vector3 localScale)
        where T : ElectricalComponent
    {
        var go = AddVisual(parent, prefabName, objName, localPos, localScale);
        var comp = EnsureComponent<T>(go);

        // Asegurar collider para NodeInteractable y ComponentSlot
        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>();

        return comp;
    }

    /// <summary>Igual que AddCircuitComponent pero devuelve el GameObject en lugar del componente.</summary>
    static GameObject AddCircuitComponentObj(GameObject parent, string prefabName, string objName,
                                             Vector3 localPos, Vector3 localScale)
    {
        var go = AddVisual(parent, prefabName, objName, localPos, localScale);
        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>();
        return go;
    }

    /// <summary>Crea un ComponentSlot hijo del componente eléctrico.</summary>
    static void AddComponentSlot(GameObject parent, ComponentSlotType slotType, Vector3 localPos)
    {
        var slotGo = new GameObject($"Slot_{slotType}");
        slotGo.transform.SetParent(parent.transform);
        slotGo.transform.localPosition = localPos;
        slotGo.transform.localScale    = Vector3.one;

        var col = slotGo.AddComponent<BoxCollider>();
        col.size      = new Vector3(0.06f, 0.04f, 0.06f);
        col.isTrigger = true;

        var slot = slotGo.AddComponent<ComponentSlot>();
        slot.acceptedType  = slotType;
        slot.installAnchor = slotGo.transform;

        // Renderer para feedback de color
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(slotGo.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale    = new Vector3(0.06f, 0.005f, 0.06f);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        slot.slotRenderer = cube.GetComponent<Renderer>();

        Undo.RegisterCreatedObjectUndo(slotGo, $"Crear ComponentSlot {slotType}");
    }

    /// <summary>Añade un NodeInteractable al componente para que el Explorador pueda medirlo.</summary>
    static void AddNodeInteractable(GameObject go)
    {
        // NodeInteractable necesita XRSimpleInteractable y Collider
        if (go.GetComponent<XRSimpleInteractable>() == null)
            go.AddComponent<XRSimpleInteractable>();

        if (go.GetComponent<NodeInteractable>() == null)
            go.AddComponent<NodeInteractable>();
        // nodeTarget se asigna automáticamente en NodeInteractable.Start()
        // desde los hijos creados por CircuitManager.EnsureAllNodesExist()
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
        => go.GetComponent<T>() ?? go.AddComponent<T>();
}
#endif
