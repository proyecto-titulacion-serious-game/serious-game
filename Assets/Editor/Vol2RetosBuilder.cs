#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Construye las 4 zonas de reto en la escena.
/// Cables: cilindros primitivos coloreados (estilo protoboard clásico).
/// Circuitos ~2× más grandes que la versión anterior para mejor ergonomía VR.
/// Menú: Tools → TITA → Vol.2 Electronics → Construir Retos en Escena
/// </summary>
public static class Vol2RetosBuilder
{
    const string V2         = "Assets/Resources Vol.2 - Electronics/Prefabs/";
    const string MAT_FOLDER = "Assets/Materials/Retos";

    const float VOLTAGE      = 9f;
    const float R1_FAULTY    = 10f;
    const float R1_CORRECT   = 100f;
    const float R1_LED_RES   = 50f;
    const float R2_BROKEN    = 9999f;
    const float R2_NORMAL    = 50f;
    const float R3_FAULTY_R  = 470f;
    const float R3_CORRECT_R = 220f;

    // Player scale Y=1 → eye ~1.6 m → tabla cómoda a 0.85 m mundo.
    const float TABLE_Y = 0.85f;

    // ── circuit/models — FBX reales ───────────────────────────────────
    const string CM_BASE   = "Assets/circuit/models";
    const string CM_TEX    = "Assets/circuit/textures/masterTex.png";
    const string CM_MAT    = "Assets/Materials/Mat_Circuit.mat";
    const string FBX_RES   = CM_BASE + "/resistorVertical.fbx";
    const string FBX_LED_G = CM_BASE + "/LEDGreen.fbx";
    const string FBX_LED_R = CM_BASE + "/LEDRed.fbx";
    const string FBX_LED_Y = CM_BASE + "/LEDYellow.fbx";
    const string FBX_CAP   = CM_BASE + "/capacitorBlue.fbx";
    const string FBX_TRANS = CM_BASE + "/transistor.fbx";
    const string FBX_WIRE1 = CM_BASE + "/wire1.fbx";
    const string FBX_WIRE2 = CM_BASE + "/wire2.fbx";
    const string PFB_WIRE1 = "Assets/circuit/prefabs/wire1.prefab";
    const string PFB_WIRE2 = "Assets/circuit/prefabs/wire2.prefab";

    // Tamaño objetivo (lado más largo) en metros
    const float SZ_RES  = 0.12f;
    const float SZ_LED  = 0.09f;
    const float SZ_CAP  = 0.13f;
    const float SZ_TRAN = 0.10f;

    static readonly Color CRed    = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color CBlack  = new Color(0.08f, 0.08f, 0.08f);
    static readonly Color CYellow = new Color(0.95f, 0.90f, 0.05f);
    static readonly Color CGreen  = new Color(0.10f, 0.80f, 0.25f);
    static readonly Color CGold   = new Color(0.95f, 0.75f, 0.10f);
    static readonly Color CNode   = new Color(0.20f, 0.90f, 1.00f);
    static readonly Color CSlot   = new Color(0.95f, 0.55f, 0.10f);

    // ─────────────────────────────────────────────
    //  Menú principal
    // ─────────────────────────────────────────────
    [MenuItem("Tools/TITA/Vol.2 Electronics/Construir Retos en Escena")]
    public static void BuildAll()
    {
        Vol2MaterialFixer.FixAllSilent(out _, out _);
        EnsureMatFolder();

        var zones = GetOrCreateGameZones();
        BuildReto1(zones);
        BuildReto2(zones);
        BuildReto3(zones);
        BuildReto4(zones);

        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.reto1Zone = zones.transform.Find("Reto1_Zone")?.gameObject;
            gm.reto2Zone = zones.transform.Find("Reto2_Zone")?.gameObject;
            gm.reto3Zone = zones.transform.Find("Reto3_Zone")?.gameObject;
            gm.reto4Zone = zones.transform.Find("Reto4_Zone")?.gameObject;
            EditorUtility.SetDirty(gm);
        }

        EditorUtility.DisplayDialog(
            "Retos construidos",
            "4 zonas de laboratorio creadas:\n\n" +
            "  Reto 1 (0, 0.85, -3)   — Serie\n" +
            "  Reto 2 (4, 0.85, -3)   — Paralelo\n" +
            "  Reto 3 (-4, 0.85, 0)   — Mixto\n" +
            "  Reto 4 (0, 0.85, 5)    — Arduino\n\n" +
            "Mesas a 0.85 m con 4 patas de madera.\n" +
            "Discos DORADOS = puntos de medición.\n" +
            "Cubo NARANJA   = slot de reemplazo.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────
    //  RETO 1 — Serie / Ley de Ohm  (layout según Boceto.png)
    //  Tablero 44 cm × 26 cm · BAT(−14) SW(−6) R?(+1) LED(+13)
    //  Rail rojo z=−1.6 cm (VCC/señal) · Rail negro z=+1.6 cm (GND)
    //  NP 1.8 cm diámetro · Slot NARANJA · R dañado 10 Ω → correcto 100 Ω
    // ─────────────────────────────────────────────────────────────────
    static void BuildReto1(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto1_Zone", new Vector3(0f, TABLE_Y, -3f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Series;

        // Boceto: 44 cm × 26 cm
        CreatePCBBench(zone, 0.44f, 0.26f);

        float yt     = 0.012f;   // componentes sobre PCB
        float yw     = 0.007f;   // cables
        float ny     = 0.006f;   // nodos eléctricos
        float np     = 0.012f;   // discos NP

        // Posiciones X del boceto (en metros)
        float xBat   = -0.14f;
        float xSwi   = -0.06f;
        float xR     =  0.01f;
        float xLed   =  0.13f;
        // Rails Z (boceto: ±1.6 cm)
        float zFront = -0.016f;   // cable rojo — VCC / señal
        float zBack  =  0.016f;   // cable negro — GND

        // ── Batería 9 V ───────────────────────────────────────────────
        var batGO = AddRemovable(zone, "Battery 9v", "Battery_9V",
                    new Vector3(xBat, yt, 0f), new Vector3(0.025f, 0.045f, 0.018f));
        var battery = EnsureComponent<VoltageSource>(batGO);
        battery.voltage = VOLTAGE;

        // ── Switch (decorativo) ───────────────────────────────────────
        var swGO = AddFixed(zone, "Switch", "Switch_Series",
                   new Vector3(xSwi, yt - 0.002f, zFront), new Vector3(0.018f, 0.014f, 0.012f));
        var sw = swGO.AddComponent<CircuitSwitch>();
        sw.isOn = false;

        // ── Resistor FBX — dañado 10 Ω ───────────────────────────────
        var rGO = AddCircuitComp(zone, FBX_RES, 0.042f,
                  new Vector3(xR, yt, 0f), Vector3.zero, "Resistor_Faulty");
        var resistor = EnsureComponent<Resistor>(rGO);
        resistor.resistance        = R1_FAULTY;
        resistor.faultyResistance  = R1_FAULTY;
        resistor.correctResistance = R1_CORRECT;
        resistor.hasFault          = true;

        // ── LED verde FBX ─────────────────────────────────────────────
        var ledGO = AddCircuitComp(zone, FBX_LED_G, 0.035f,
                    new Vector3(xLed, yt, 0f), Vector3.zero, "LED_Output");
        var led = EnsureComponent<LED>(ledGO);
        led.resistance = R1_LED_RES;

        // ── Nodos eléctricos ──────────────────────────────────────────
        // NP_VCC: entre BAT(−14) y SW(−6) → x = −10 cm
        var nVCC     = MakeNode(zone, "Node_R1_VCC",     new Vector3(-0.10f, ny, zFront));
        var nAfterSW = MakeNode(zone, "Node_R1_AfterSW", new Vector3(-0.02f, ny, zFront));
        // NP_Mid: entre R?(+1) y LED(+13) → x = +7 cm
        var nMid     = MakeNode(zone, "Node_R1_Mid",     new Vector3(+0.07f, ny, zFront));
        // NP_GND: en el LED(+13)
        var nGND     = MakeNode(zone, "Node_R1_GND",     new Vector3(+0.13f, ny, zBack));

        battery.nodeA  = nVCC;     battery.nodeB  = nGND;
        sw.nodeA       = nVCC;     sw.nodeB       = nAfterSW;
        resistor.nodeA = nAfterSW; resistor.nodeB = nMid;
        led.nodeA      = nMid;     led.nodeB      = nGND;
        MarkDirty(batGO, swGO, rGO, ledGO);

        // ── cable ROJO (rail VCC/señal, zFront) ───────────────────────
        float sw2 = 0.010f;   // semi-ancho del switch
        float r2  = 0.022f;   // semi-ancho del resistor
        Wire(zone, new Vector3(xBat,        yw, zFront), new Vector3(xSwi - sw2, yw, zFront), CRed,    "w1_vcc_a");
        Wire(zone, new Vector3(xSwi + sw2,  yw, zFront), new Vector3(xR   - r2,  yw, zFront), CRed,    "w1_vcc_b");
        Wire(zone, new Vector3(xR   + r2,   yw, zFront), new Vector3(xLed,       yw, zFront), CYellow, "w1_sig");
        // ── cable negro (rail GND, zBack) ────────────────────────────
        Wire(zone, new Vector3(xBat, yw, zBack), new Vector3(xLed, yw, zBack), CBlack, "w1_gnd");
        // ── puentes BAT y LED entre ambos rails ───────────────────────
        Wire(zone, new Vector3(xBat, yw, zFront), new Vector3(xBat, yw, zBack), CBlack, "w1_bat_z");
        Wire(zone, new Vector3(xLed, yw, zFront), new Vector3(xLed, yw, zBack), CBlack, "w1_led_z");

        // ── NPs: 1.8 cm diámetro, triggerRadius 1.4 cm ───────────────
        MakeNodePoint(zone, nVCC, new Vector3(-0.10f, np, zFront), CGold, "NP_R1_VCC", 0.018f, 0.014f);
        MakeNodePoint(zone, nMid, new Vector3(+0.07f, np, zFront), CGold, "NP_R1_Mid", 0.018f, 0.014f);
        MakeNodePoint(zone, nGND, new Vector3(+0.13f, np, zBack),  CGold, "NP_R1_GND", 0.018f, 0.014f);

        // ── Slot NARANJA (5 cm) bajo el resistor ─────────────────────
        AddSlot(zone, new Vector3(xR, yt + 0.05f, 0f), ComponentSlotType.Resistor, "Slot_R1_Resistor", 0.05f);

        // ── Cables decorativos wire1/wire2 ────────────────────────────
        AddDecorWire(zone, PFB_WIRE2, new Vector3(xBat - 0.015f, yt + 0.015f, zFront),             new Vector3(0f,  30f, -40f), 0.18f, "DW_R1_BatPlus");
        AddDecorWire(zone, PFB_WIRE1, new Vector3(xLed + 0.020f, yt + 0.010f, zBack),              new Vector3(0f, -15f,  35f), 0.18f, "DW_R1_LedGND");
        AddDecorWire(zone, PFB_WIRE2, new Vector3((xSwi + xR) * 0.5f, yt + 0.008f, zFront - 0.014f), new Vector3(0f,  90f,  20f), 0.15f, "DW_R1_MidSig");

        Debug.Log("[RetosBuilder] ✓ Reto 1 (Serie — Boceto 44×26 cm)");
    }

    // ─────────────────────────────────────────────────────────────────
    //  RETO 2 — Paralelo
    //  Batería → bifurcación → LED_A(correcto, verde) || LED_B(defectuoso, rojo)
    // ─────────────────────────────────────────────────────────────────
    static void BuildReto2(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto2_Zone", new Vector3(4f, TABLE_Y, -3f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Parallel;

        CreatePCBBench(zone, 1.80f, 1.10f);

        float yt = 0.016f;
        float yw = 0.014f;
        float ny = 0.010f;
        float np = 0.025f;

        float xBat  = -0.80f;
        float xJunc =  0.00f;
        float xLed  =  0.60f;
        float xGND  =  0.85f;
        float zA    =  0.45f;
        float zB    = -0.45f;

        // ── Componentes ───────────────────────────────────────────────
        var batGO = AddRemovable(zone, "Battery 9v", "Battery_9V",
                    new Vector3(xBat, yt, 0f), new Vector3(0.075f, 0.150f, 0.055f));
        var battery = EnsureComponent<VoltageSource>(batGO);
        battery.voltage = VOLTAGE;

        var ledAGO = AddCircuitComp(zone, FBX_LED_G, SZ_LED,
                     new Vector3(xLed, yt, zA), Vector3.zero, "LED_RamaA");
        var ledA = EnsureComponent<LED>(ledAGO);
        ledA.resistance = R2_NORMAL;

        var ledBGO = AddCircuitComp(zone, FBX_LED_R, SZ_LED,
                     new Vector3(xLed, yt, zB), Vector3.zero, "LED_RamaB_Faulty");
        var ledB = EnsureComponent<LED>(ledBGO);
        ledB.resistance       = R2_BROKEN;
        ledB.polarityInverted = true;

        // Transistor en la bifurcación (decorativo)
        AddCircuitComp(zone, FBX_TRANS, SZ_TRAN,
                       new Vector3(xJunc, yt, 0f), Vector3.zero, "Transistor_Junction");

        // ── Nodos eléctricos ──────────────────────────────────────────
        var nVCC  = MakeNode(zone, "Node_R2_VCC",  new Vector3(xJunc - 0.15f, ny, 0f));
        var nGND  = MakeNode(zone, "Node_R2_GND",  new Vector3(xGND,          ny, 0f));
        var nMidA = MakeNode(zone, "Node_R2_MidA", new Vector3(xLed,          ny, zA));
        var nMidB = MakeNode(zone, "Node_R2_MidB", new Vector3(xLed,          ny, zB));

        battery.nodeA = nVCC;  battery.nodeB = nGND;
        ledA.nodeA    = nVCC;  ledA.nodeB    = nMidA;
        ledB.nodeA    = nVCC;  ledB.nodeB    = nMidB;
        MarkDirty(batGO, ledAGO, ledBGO);

        // ── Cables ────────────────────────────────────────────────────
        Wire(zone, new Vector3(xBat,           yw, 0f), new Vector3(xJunc - 0.030f, yw, 0f), CRed,    "w2_vcc");
        Wire(zone, new Vector3(xJunc + 0.030f, yw, 0f), new Vector3(xLed,          yw, zA),  CYellow, "w2_brA");
        Wire(zone, new Vector3(xJunc + 0.030f, yw, 0f), new Vector3(xLed,          yw, zB),  CGreen,  "w2_brB");
        Wire(zone, new Vector3(xLed,           yw, zA), new Vector3(xGND,           yw, 0f),  CBlack,  "w2_gndA");
        Wire(zone, new Vector3(xLed,           yw, zB), new Vector3(xGND,           yw, 0f),  CBlack,  "w2_gndB");
        Wire(zone, new Vector3(xGND,           yw, 0f), new Vector3(xBat,           yw, 0f),  CBlack,  "w2_gndRet");

        // ── NodePoints ────────────────────────────────────────────────
        MakeNodePoint(zone, nVCC,  new Vector3(xJunc - 0.15f, np, 0f), CGold, "NP_R2_VCC");
        MakeNodePoint(zone, nMidA, new Vector3(xLed,          np, zA), CGold, "NP_R2_MidA");
        MakeNodePoint(zone, nMidB, new Vector3(xLed,          np, zB), CGold, "NP_R2_MidB");
        MakeNodePoint(zone, nGND,  new Vector3(xGND,          np, 0f), CGold, "NP_R2_GND");

        AddSlot(zone, new Vector3(xLed, yt + 0.12f, zB), ComponentSlotType.LED, "Slot_R2_LED");

        // ── Cables decorativos ────────────────────────────────────────
        // Stub saliendo de la batería hacia la bifurcación
        AddDecorWire(zone, PFB_WIRE2, new Vector3(xBat + 0.05f, yt + 0.04f, 0.05f),  new Vector3(0f, 45f, -35f), 0.40f, "DW_R2_BatOut");
        // Cable en la bifurcación (entre los dos LEDs)
        AddDecorWire(zone, PFB_WIRE1, new Vector3(xJunc, yt + 0.05f, 0f),             new Vector3(0f, 0f,  20f),  0.42f, "DW_R2_Junction");
        // Retorno GND desde LED_A
        AddDecorWire(zone, PFB_WIRE2, new Vector3((xLed + xGND) * 0.5f, yt + 0.03f, zA * 0.6f), new Vector3(0f, -30f, 15f), 0.38f, "DW_R2_GndA");

        Debug.Log("[RetosBuilder] ✓ Reto 2 (Paralelo)");
    }

    // ─────────────────────────────────────────────────────────────────
    //  RETO 3 — Mixto (Serie → Paralelo)
    //  R_Serie → bloque paralelo (LED_Yellow + Capacitor, polaridades invertidas)
    // ─────────────────────────────────────────────────────────────────
    static void BuildReto3(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto3_Zone", new Vector3(-4f, TABLE_Y, 0f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Mixed;

        CreatePCBBench(zone, 1.40f, 0.90f);

        float yt = 0.016f;
        float yw = 0.014f;
        float ny = 0.010f;
        float np = 0.025f;

        float xBat  = -0.70f;
        float xR    = -0.20f;
        float xPar  =  0.35f;
        float xGND  =  0.60f;
        float zLed  =  0.35f;
        float zCap  = -0.35f;

        // ── Componentes ───────────────────────────────────────────────
        var batGO = AddRemovable(zone, "Battery 9v", "Battery_9V",
                    new Vector3(xBat, yt, 0f), new Vector3(0.075f, 0.150f, 0.055f));
        var battery = EnsureComponent<VoltageSource>(batGO);
        battery.voltage = VOLTAGE;

        var rGO = AddCircuitComp(zone, FBX_RES, SZ_RES,
                  new Vector3(xR, yt, 0f), Vector3.zero, "Resistor_Serie_Faulty");
        var resistor = EnsureComponent<Resistor>(rGO);
        resistor.resistance        = R3_FAULTY_R;
        resistor.faultyResistance  = R3_FAULTY_R;
        resistor.correctResistance = R3_CORRECT_R;
        resistor.hasFault          = true;

        var ledGO = AddCircuitComp(zone, FBX_LED_Y, SZ_LED,
                    new Vector3(xPar, yt, zLed), Vector3.zero, "LED_Paralelo");
        var led = EnsureComponent<LED>(ledGO);
        led.resistance       = R1_LED_RES;
        led.polarityInverted = true;

        var capGO = AddCircuitComp(zone, FBX_CAP, SZ_CAP,
                    new Vector3(xPar, yt, zCap), Vector3.zero, "Capacitor_Invertido");
        var cap = EnsureComponent<Capacitor>(capGO);
        cap.polarityInverted = true;

        // ── Nodos eléctricos ──────────────────────────────────────────
        var nVCC    = MakeNode(zone, "Node_R3_VCC",    new Vector3(xBat + 0.20f, ny,  0f));
        var nAfterR = MakeNode(zone, "Node_R3_AfterR", new Vector3(xPar - 0.08f, ny,  0f));
        var nGND    = MakeNode(zone, "Node_R3_GND",    new Vector3(xGND,         ny,  0f));

        battery.nodeA  = nVCC;    battery.nodeB  = nGND;
        resistor.nodeA = nVCC;    resistor.nodeB = nAfterR;
        led.nodeA      = nAfterR; led.nodeB      = nGND;
        cap.nodeA      = nAfterR; cap.nodeB      = nGND;
        MarkDirty(batGO, rGO, ledGO, capGO);

        // ── Cables ────────────────────────────────────────────────────
        Wire(zone, new Vector3(xBat,          yw, 0f), new Vector3(xR - 0.040f,  yw, 0f),    CRed,    "w3_vcc");
        Wire(zone, new Vector3(xR + 0.040f,   yw, 0f), new Vector3(xPar - 0.08f, yw, 0f),   CYellow, "w3_mid");
        Wire(zone, new Vector3(xPar - 0.08f,  yw, 0f), new Vector3(xPar,         yw, zLed), CYellow, "w3_led");
        Wire(zone, new Vector3(xPar - 0.08f,  yw, 0f), new Vector3(xPar,         yw, zCap), CGreen,  "w3_cap");
        Wire(zone, new Vector3(xPar,          yw, zLed), new Vector3(xGND,        yw, 0f),   CBlack,  "w3_gndL");
        Wire(zone, new Vector3(xPar,          yw, zCap), new Vector3(xGND,        yw, 0f),   CBlack,  "w3_gndC");
        Wire(zone, new Vector3(xGND,          yw, 0f),  new Vector3(xBat,         yw, 0f),   CBlack,  "w3_gndRet");

        // ── NodePoints ────────────────────────────────────────────────
        MakeNodePoint(zone, nVCC,    new Vector3(xBat + 0.20f, np, 0f),    CNode, "NP_R3_VCC");
        MakeNodePoint(zone, nAfterR, new Vector3(xPar - 0.08f, np, 0f),    CNode, "NP_R3_AfterR");
        MakeNodePoint(zone, nGND,    new Vector3(xGND,         np, 0f),    CNode, "NP_R3_GND");
        MakeNodePoint(zone, nAfterR, new Vector3(xPar,         np, zLed),  CNode, "NP_R3_LED_in");
        MakeNodePoint(zone, nAfterR, new Vector3(xPar,         np, zCap),  CNode, "NP_R3_Cap_in");

        // ── Slots ─────────────────────────────────────────────────────
        AddSlot(zone, new Vector3(xR,   yt + 0.12f, 0f),   ComponentSlotType.Resistor,  "Slot_R3_Resistor");
        AddSlot(zone, new Vector3(xPar, yt + 0.12f, zLed), ComponentSlotType.LED,       "Slot_R3_LED");
        AddSlot(zone, new Vector3(xPar, yt + 0.15f, zCap), ComponentSlotType.Capacitor, "Slot_R3_Cap");

        // ── Cables decorativos ────────────────────────────────────────
        // Stub batería → resistor
        AddDecorWire(zone, PFB_WIRE2, new Vector3((xBat + xR) * 0.5f, yt + 0.04f, -0.04f), new Vector3(0f, 10f, -30f), 0.40f, "DW_R3_BatR");
        // En la bifurcación paralelo (entre LED y cap)
        AddDecorWire(zone, PFB_WIRE1, new Vector3(xPar + 0.05f, yt + 0.05f, 0f),            new Vector3(0f, 0f,  25f),  0.38f, "DW_R3_ParJunc");
        // Retorno GND lateral
        AddDecorWire(zone, PFB_WIRE2, new Vector3(xGND + 0.06f, yt + 0.03f, -0.15f),        new Vector3(0f, -20f, 15f), 0.36f, "DW_R3_GndRet");

        Debug.Log("[RetosBuilder] ✓ Reto 3 (Mixto)");
    }

    // ─────────────────────────────────────────────────────────────────
    //  RETO 4 — Arduino / Microcontrolador
    //  Circuito SERIE: Batería → ArduinoPin(D4, hasFault+hasLooseCable)
    //                          → Resistor_Buzzer(0 Ω, faltante) → GND
    //  3 fallas:  1) Pin incorrecto D4→D2
    //             2) Resistencia buzzer faltante (0 Ω → correcto 330 Ω)
    //             3) Cable suelto en la protoboard
    // ─────────────────────────────────────────────────────────────────
    static void BuildReto4(GameObject zonesRoot)
    {
        var zone = GetOrCreateZone(zonesRoot, "Reto4_Zone", new Vector3(0f, TABLE_Y, 5f));
        zone.SetActive(false);

        var cm = EnsureComponent<CircuitManager>(zone);
        cm.topology = CircuitTopology.Series;

        CreatePCBBench(zone, 2.20f, 0.60f);

        float yt  = 0.016f;
        float yw  = 0.014f;
        float ny  = 0.010f;
        float np  = 0.025f;

        float xBat   = -0.90f;
        float xBoard =  0.10f;   // Arduino board (pin D4→D2)
        float xBuzz  =  0.60f;   // Resistor buzzer faltante (0 Ω → 330 Ω)
        float xDisp  =  0.95f;   // Display decorativo
        float zOff   = -0.040f;

        // ── Componentes eléctricos ────────────────────────────────────
        var batGO = AddRemovable(zone, "Battery 9v", "Battery_9V",
                    new Vector3(xBat, yt, 0f), new Vector3(0.075f, 0.150f, 0.055f));
        var battery = EnsureComponent<VoltageSource>(batGO);
        battery.voltage = VOLTAGE;

        // Transistor decorativo — NO componente eléctrico (solo visual)
        AddCircuitComp(zone, FBX_TRANS, SZ_TRAN,
                       new Vector3(-0.35f, yt, 0f), Vector3.zero, "Relay_Decor");

        // Arduino board: pin incorrecto D4 (correcto D2) + cable suelto.
        // AddFixed (no GrabbableComponent): la placa faulty no debe ser agarrable;
        // el Explorer instala el pin correcto enviado por el Técnico en el slot.
        var boardGO = AddFixed(zone, "Controller Board", "Arduino_WrongPin",
                      new Vector3(xBoard, yt, 0f), new Vector3(0.25f, 0.045f, 0.20f));
        var pin = EnsureComponent<ArduinoPin>(boardGO);
        pin.pinNumber        = 4;   // incorrecto
        pin.correctPinNumber = 2;
        pin.hasFault         = true;
        pin.hasLooseCable    = true;

        // Resistor del buzzer FALTANTE  (faultyResistance=0 Ω → correcto 330 Ω)
        // También fijo: se reemplaza vía Slot_R4_Resistor.
        var buzzGO = AddCircuitComp(zone, FBX_RES, SZ_RES,
                     new Vector3(xBuzz, yt, 0f), Vector3.zero, "Resistor_Buzzer_Missing");
        // Quitar interacción de agarre — el Explorer no debe agarrar el resistor faltante
        Object.DestroyImmediate(buzzGO.GetComponent<GrabbableComponent>());
        Object.DestroyImmediate(buzzGO.GetComponent<XRGrabInteractable>());
        var buzzerR = EnsureComponent<Resistor>(buzzGO);
        buzzerR.resistance        = 0f;
        buzzerR.faultyResistance  = 0f;
        buzzerR.correctResistance = 330f;
        buzzerR.hasFault          = true;

        AddVisual(zone, "Segment Display", "Display_Arduino",
                  new Vector3(xDisp, yt, 0f), new Vector3(0.125f, 0.063f, 0.075f));

        // ── Nodos eléctricos ──────────────────────────────────────────
        // Serie: BAT(nVCC→nGND) | PIN(nVCC→nAfterPin) | BUZZ(nAfterPin→nGND)
        var nVCC      = MakeNode(zone, "Node_R4_VCC",      new Vector3(-0.50f, ny, zOff));
        var nAfterPin = MakeNode(zone, "Node_R4_AfterPin", new Vector3(+0.35f, ny, zOff));
        var nGND      = MakeNode(zone, "Node_R4_GND",      new Vector3(+1.00f, ny, zOff));

        battery.nodeA  = nVCC;      battery.nodeB  = nGND;
        pin.nodeA      = nVCC;      pin.nodeB      = nAfterPin;
        buzzerR.nodeA  = nAfterPin; buzzerR.nodeB  = nGND;
        MarkDirty(batGO, boardGO, buzzGO);

        // ── Cables ────────────────────────────────────────────────────
        float zPos = zOff + 0.040f;
        Wire(zone, new Vector3(xBat,            yw, zOff), new Vector3(xBoard - 0.063f, yw, zOff), CRed,    "w4_vcc");
        Wire(zone, new Vector3(xBoard + 0.063f, yw, zOff), new Vector3(xBuzz  - 0.030f, yw, zOff), CYellow, "w4_mid");
        Wire(zone, new Vector3(xBuzz  + 0.030f, yw, zOff), new Vector3(xDisp,           yw, zOff), CYellow, "w4_out");
        Wire(zone, new Vector3(xDisp,           yw, zOff), new Vector3(xDisp,            yw, zPos), CBlack,  "w4_gndZ");
        Wire(zone, new Vector3(xDisp,           yw, zPos), new Vector3(xBat,             yw, zPos), CBlack,  "w4_gndRet");
        Wire(zone, new Vector3(xBat,            yw, zPos), new Vector3(xBat,             yw, zOff), CBlack,  "w4_batZ");

        // ── NodePoints ────────────────────────────────────────────────
        MakeNodePoint(zone, nVCC,      new Vector3(-0.50f, np, zOff), CNode, "NP_R4_VCC");
        MakeNodePoint(zone, nAfterPin, new Vector3(+0.35f, np, zOff), CNode, "NP_R4_AfterPin");
        MakeNodePoint(zone, nAfterPin, new Vector3(xBoard, np, zOff), CNode, "NP_R4_BoardIn");
        MakeNodePoint(zone, nGND,      new Vector3(+1.00f, np, zOff), CNode, "NP_R4_GND");

        // ── Slots ─────────────────────────────────────────────────────
        var slotPin = AddSlot(zone, new Vector3(xBoard, yt + 0.08f, 0f),
                              ComponentSlotType.ArduinoPin, "Slot_R4_Arduino");
        slotPin.damagedComponent = boardGO;   // se oculta al instalar el pin correcto

        var slotRes = AddSlot(zone, new Vector3(xBuzz, yt + 0.08f, 0f),
                              ComponentSlotType.Resistor, "Slot_R4_Resistor");
        slotRes.damagedComponent = buzzGO;    // se oculta al instalar la resistencia 330 Ω

        // ── Cables decorativos ────────────────────────────────────────
        AddDecorWire(zone, PFB_WIRE2, new Vector3(xBat + 0.04f, yt + 0.05f, 0.04f),   new Vector3(0f, -10f, -40f), 0.40f, "DW_R4_BatOut");
        AddDecorWire(zone, PFB_WIRE1, new Vector3(xBoard - 0.08f, yt + 0.05f, 0.06f), new Vector3(0f,  80f,  25f), 0.38f, "DW_R4_BoardIn");
        AddDecorWire(zone, PFB_WIRE2, new Vector3(xDisp + 0.06f, yt + 0.04f, zPos),   new Vector3(0f, 160f,  20f), 0.36f, "DW_R4_DispGND");

        Debug.Log("[RetosBuilder] ✓ Reto 4 (Arduino — D4→D2 + R_Buzzer 0→330 Ω + cable suelto)");
    }

    // ─────────────────────────────────────────────
    //  HELPERS DE CONSTRUCCIÓN
    // ─────────────────────────────────────────────

    static void CreateBench(GameObject zone, Vector3 tableScale, Vector3 boardScale)
    {
        AddVisual(zone, "Plywood", "Table_Plywood",
                  new Vector3(0f, -tableScale.y * 0.5f, 0f), tableScale);

        AddVisual(zone, "Bareboard", "Bareboard",
                  new Vector3(0f, boardScale.y * 0.5f, 0f), boardScale);

        // 4 patas cilíndricas de madera desde el fondo de la tabla hasta el suelo
        float legH   = TABLE_Y - tableScale.y;
        float legScY = legH * 0.5f;
        float legY   = -(tableScale.y + legH * 0.5f);
        float lx     = tableScale.x * 0.40f;
        float lz     = tableScale.z * 0.40f;
        Color wood   = new Color(0.55f, 0.35f, 0.18f);

        AddLeg(zone, new Vector3(-lx, legY, -lz), "Leg_FL", legScY, wood);
        AddLeg(zone, new Vector3(+lx, legY, -lz), "Leg_FR", legScY, wood);
        AddLeg(zone, new Vector3(-lx, legY, +lz), "Leg_BL", legScY, wood);
        AddLeg(zone, new Vector3(+lx, legY, +lz), "Leg_BR", legScY, wood);
    }

    static void AddLeg(GameObject zone, Vector3 localPos, string legName, float scaleY, Color color)
    {
        var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = legName;
        leg.transform.SetParent(zone.transform);
        leg.transform.localPosition = localPos;
        leg.transform.localScale    = new Vector3(0.06f, scaleY, 0.06f);
        Object.DestroyImmediate(leg.GetComponent<Collider>());
        SetColor(leg, color, legName + "_mat");
        Undo.RegisterCreatedObjectUndo(leg, legName);
    }

    // ── Cable: cilindro primitivo coloreado ───────────────────────────
    static void Wire(GameObject zone, Vector3 a, Vector3 b, Color color, string wireName,
                     float thickness = 0.010f)
    {
        Vector3 dir    = b - a;
        float   length = dir.magnitude;
        if (length < 0.001f) return;

        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = wireName;
        cyl.transform.SetParent(zone.transform);
        cyl.transform.localPosition = (a + b) * 0.5f;
        cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        cyl.transform.localScale    = new Vector3(thickness, length * 0.5f, thickness);
        Object.DestroyImmediate(cyl.GetComponent<Collider>());
        SetColor(cyl, color, wireName + "_mat");
        Undo.RegisterCreatedObjectUndo(cyl, wireName);
    }

    // ── Componente removible (XRGrabInteractable) ─────────────────────
    static GameObject AddRemovable(GameObject parent, string prefabName, string objName,
                                   Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        string vpath   = V2 + prefabName + ".prefab";
        var    vprefab = AssetDatabase.LoadAssetAtPath<GameObject>(vpath);
        if (vprefab != null)
        {
            var vis = Object.Instantiate(vprefab, go.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = Vector3.one;
            foreach (var col in vis.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
            var rootRend = go.GetComponent<Renderer>();
            if (rootRend != null) rootRend.enabled = false;
        }
        else
        {
            SetColor(go, ComponentColor(prefabName), objName + "_mat");
        }

        Object.DestroyImmediate(go.GetComponent<Collider>());
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = false;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        go.AddComponent<GrabbableComponent>();

        Undo.RegisterCreatedObjectUndo(go, $"Crear {objName}");
        return go;
    }

    // ── Componente fijo (XRSimpleInteractable, para switch) ───────────
    static GameObject AddFixed(GameObject parent, string prefabName, string objName,
                               Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        string vpath   = V2 + prefabName + ".prefab";
        var    vprefab = AssetDatabase.LoadAssetAtPath<GameObject>(vpath);
        if (vprefab != null)
        {
            var vis = Object.Instantiate(vprefab, go.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = Vector3.one;
            foreach (var col in vis.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
            var rootRend = go.GetComponent<Renderer>();
            if (rootRend != null) rootRend.enabled = false;
        }
        else
        {
            SetColor(go, ComponentColor(prefabName), objName + "_mat");
        }

        Object.DestroyImmediate(go.GetComponent<Collider>());
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = false;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        go.AddComponent<XRSimpleInteractable>();

        Undo.RegisterCreatedObjectUndo(go, $"Crear {objName}");
        return go;
    }

    static Color ComponentColor(string prefabName) => prefabName switch
    {
        "Battery 9v"       => new Color(0.90f, 0.85f, 0.10f),
        "Switch"           => new Color(0.80f, 0.20f, 0.10f),
        "Led A" or "Led B" => new Color(0.10f, 0.85f, 0.20f),
        "Led C"            => new Color(0.85f, 0.10f, 0.10f),
        "Led D"            => new Color(0.20f, 0.40f, 0.90f),
        "Potentiometer"    => new Color(0.55f, 0.35f, 0.15f),
        "Transistor"       => new Color(0.20f, 0.20f, 0.20f),
        "Capacitor"        => new Color(0.15f, 0.45f, 0.85f),
        "Relay"            => new Color(0.30f, 0.30f, 0.75f),
        "Controller Board" => new Color(0.10f, 0.45f, 0.20f),
        "Segment Display"  => new Color(0.15f, 0.15f, 0.15f),
        _                  => new Color(0.50f, 0.50f, 0.50f),
    };

    // ── Nodo eléctrico ────────────────────────────────────────────────
    static ElectricalNode MakeNode(GameObject zone, string nodeName, Vector3 localPos)
    {
        var go = new GameObject(nodeName);
        go.transform.SetParent(zone.transform);
        go.transform.localPosition = localPos;
        Undo.RegisterCreatedObjectUndo(go, nodeName);
        return go.AddComponent<ElectricalNode>();
    }

    // ── NodePoint: disco medible con multímetro ───────────────────────
    static void MakeNodePoint(GameObject zone, ElectricalNode node,
                              Vector3 localPos, Color color, string ptName,
                              float padSize = 0.042f, float triggerRadius = 0.030f)
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = ptName + "_Pad";
        visual.transform.SetParent(zone.transform);
        visual.transform.localPosition = localPos;
        visual.transform.localScale    = new Vector3(padSize, 0.003f, padSize);
        SetColor(visual, color, ptName + "_mat");
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        Undo.RegisterCreatedObjectUndo(visual, ptName + "_Pad");

        var det = new GameObject(ptName);
        det.transform.SetParent(zone.transform);
        det.transform.localPosition = localPos + new Vector3(0f, 0.004f, 0f);

        var col = det.AddComponent<SphereCollider>();
        col.isTrigger = false;
        col.radius    = triggerRadius;

        det.AddComponent<XRSimpleInteractable>();
        var ni = det.AddComponent<NodeInteractable>();
        ni.nodeTarget = node;

        EditorUtility.SetDirty(det);
        Undo.RegisterCreatedObjectUndo(det, ptName);
    }

    // ── Solderpad decorativo ──────────────────────────────────────────
    static void AddSolderpadDecor(GameObject zone, Vector3 localPos, string padName)
    {
        string vpath   = V2 + "Solderpad.prefab";
        var    vprefab = AssetDatabase.LoadAssetAtPath<GameObject>(vpath);
        if (vprefab == null) return;

        var pad = Object.Instantiate(vprefab, zone.transform);
        pad.name = padName;
        pad.transform.localPosition = localPos;
        pad.transform.localScale    = new Vector3(0.4f, 0.05f, 0.4f);
        foreach (var c in pad.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        Undo.RegisterCreatedObjectUndo(pad, padName);
    }

    // ── Slot de reemplazo ─────────────────────────────────────────────
    static ComponentSlot AddSlot(GameObject zone, Vector3 localPos,
                                 ComponentSlotType slotType, string slotName, float size = 0.14f)
    {
        var slotGO = new GameObject(slotName);
        slotGO.transform.SetParent(zone.transform);
        slotGO.transform.localPosition = localPos;

        var col = slotGO.AddComponent<BoxCollider>();
        col.size      = new Vector3(size, 0.04f, size);
        col.isTrigger = true;

        var slot = slotGO.AddComponent<ComponentSlot>();
        slot.acceptedType  = slotType;
        slot.installAnchor = slotGO.transform;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "SlotIndicator";
        cube.transform.SetParent(slotGO.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale    = new Vector3(size, 0.008f, size);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        SetColor(cube, CSlot, slotName + "_mat");
        slot.slotRenderer = cube.GetComponent<Renderer>();

        Undo.RegisterCreatedObjectUndo(slotGO, slotName);
        return slot;
    }

    // ─────────────────────────────────────────────
    //  HELPERS — circuit/models FBX
    // ─────────────────────────────────────────────

    static Material _circuitMat;
    static Material GetCircuitMat()
    {
        if (_circuitMat != null) return _circuitMat;
        _circuitMat = AssetDatabase.LoadAssetAtPath<Material>(CM_MAT);
        if (_circuitMat != null) return _circuitMat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        var tex    = AssetDatabase.LoadAssetAtPath<Texture2D>(CM_TEX);
        if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", Color.white); }
        else               mat.SetColor("_BaseColor", new Color(0.70f, 0.70f, 0.70f));
        // Activar emisión para que MaterialPropertyBlock pueda controlarla por instancia
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, CM_MAT);
        _circuitMat = AssetDatabase.LoadAssetAtPath<Material>(CM_MAT);
        return _circuitMat;
    }

    static float AutoScaleVal(GameObject vis, float targetSize)
    {
        float longest = 0f;
        foreach (var f in vis.GetComponentsInChildren<MeshFilter>())
            if (f.sharedMesh != null)
            {
                var sz = f.sharedMesh.bounds.size;
                longest = Mathf.Max(longest, sz.x, sz.y, sz.z);
            }
        return longest > 0.001f ? targetSize / longest : 1f;
    }

    static GameObject AddCircuitComp(GameObject zone, string fbxPath, float targetSize,
                                     Vector3 localPos, Vector3 localEuler, string objName)
    {
        var root = new GameObject(objName);
        root.transform.SetParent(zone.transform);
        root.transform.localPosition = localPos;
        root.transform.localRotation = Quaternion.Euler(localEuler);

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx != null)
        {
            var vis = Object.Instantiate(fbx, root.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            float s = AutoScaleVal(vis, targetSize);
            vis.transform.localScale = Vector3.one * s;
            var mat = GetCircuitMat();
            foreach (var rend in vis.GetComponentsInChildren<Renderer>())
                rend.sharedMaterial = mat;
            foreach (var col in vis.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
        }
        else
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(root.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale    = Vector3.one * targetSize;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            SetColor(cube, new Color(0.50f, 0.50f, 0.50f), objName + "_mat");
        }

        var bc   = root.AddComponent<BoxCollider>();
        bc.size      = Vector3.one * (targetSize * 1.2f);
        bc.isTrigger = false;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var grab = root.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        root.AddComponent<GrabbableComponent>();
        Undo.RegisterCreatedObjectUndo(root, $"Crear {objName}");
        return root;
    }

    // Coloca un cable decorativo del paquete circuit (wire1 o wire2).
    // scale ~0.4 es un buen punto de partida; ajusta en Inspector si hace falta.
    static void AddDecorWire(GameObject zone, string prefabPath,
                             Vector3 localPos, Vector3 localEuler, float scale, string objName)
    {
        var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (pfb == null)
        {
            // Fallback al FBX si el prefab no existe
            string fbxPath = prefabPath
                .Replace("Assets/circuit/prefabs/", CM_BASE + "/")
                .Replace(".prefab", ".fbx");
            pfb = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        }
        if (pfb == null) return;

        var go = Object.Instantiate(pfb, zone.transform);
        go.name = objName;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale    = Vector3.one * scale;
        foreach (var col in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);
        Undo.RegisterCreatedObjectUndo(go, objName);
    }

    // PCB-style bench: dark-green board + plywood table + 4 legs
    static void CreatePCBBench(GameObject zone, float w, float d)
    {
        var pcbGreen = new Color(0.06f, 0.28f, 0.12f);
        var wood     = new Color(0.55f, 0.35f, 0.18f);
        string zn    = zone.name;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "PCB_Board";
        board.transform.SetParent(zone.transform);
        board.transform.localPosition = new Vector3(0f, 0.004f, 0f);
        board.transform.localScale    = new Vector3(w, 0.008f, d);
        Object.DestroyImmediate(board.GetComponent<Collider>());
        SetColor(board, pcbGreen, "PCB_" + zn + "_mat");
        Undo.RegisterCreatedObjectUndo(board, "PCB_Board");

        float tw = w + 0.10f, td = d + 0.10f;
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Table_Surface";
        table.transform.SetParent(zone.transform);
        table.transform.localPosition = new Vector3(0f, -0.013f, 0f);
        table.transform.localScale    = new Vector3(tw, 0.025f, td);
        Object.DestroyImmediate(table.GetComponent<Collider>());
        SetColor(table, wood, "Table_" + zn + "_mat");
        Undo.RegisterCreatedObjectUndo(table, "Table_Surface");

        float legH  = TABLE_Y - 0.025f;
        float legScY = legH * 0.5f;
        float legY  = -(0.025f + legH * 0.5f);
        float lx    = tw * 0.40f, lz = td * 0.40f;
        AddLeg(zone, new Vector3(-lx, legY, -lz), "Leg_FL", legScY, wood);
        AddLeg(zone, new Vector3(+lx, legY, -lz), "Leg_FR", legScY, wood);
        AddLeg(zone, new Vector3(-lx, legY, +lz), "Leg_BL", legScY, wood);
        AddLeg(zone, new Vector3(+lx, legY, +lz), "Leg_BR", legScY, wood);
    }

    // ─────────────────────────────────────────────
    //  HELPERS GENÉRICOS
    // ─────────────────────────────────────────────

    static GameObject GetOrCreateGameZones()
    {
        var ex = GameObject.Find("GameZones");
        if (ex != null) return ex;
        var go = new GameObject("GameZones");
        go.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Crear GameZones");
        return go;
    }

    static GameObject GetOrCreateZone(GameObject parent, string name, Vector3 localPos)
    {
        var ex = parent.transform.Find(name);
        if (ex != null)
        {
            bool replace = EditorUtility.DisplayDialog("Zona ya existe",
                $"'{name}' ya existe. ¿Reemplazar?", "Sí", "Mantener");
            if (!replace) return ex.gameObject;
            Undo.DestroyObjectImmediate(ex.gameObject);
        }
        var zone = new GameObject(name);
        zone.transform.SetParent(parent.transform);
        zone.transform.localPosition = localPos;
        Undo.RegisterCreatedObjectUndo(zone, $"Crear {name}");
        return zone;
    }

    static GameObject AddVisual(GameObject parent, string prefabName, string objName,
                                Vector3 localPos, Vector3 localScale)
    {
        string path   = V2 + prefabName + ".prefab";
        var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject go;
        if (prefab != null)
        {
            go      = Object.Instantiate(prefab, parent.transform);
            go.name = objName;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objName + "_placeholder";
            go.transform.SetParent(parent.transform);
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        Undo.RegisterCreatedObjectUndo(go, $"Crear {objName}");
        return go;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
        => go.GetComponent<T>() ?? go.AddComponent<T>();

    static void MarkDirty(params GameObject[] gos)
    {
        foreach (var g in gos) EditorUtility.SetDirty(g);
    }

    static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Materials", "Retos");
    }

    static void SetColor(GameObject go, Color color, string matName)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;

        string path   = $"{MAT_FOLDER}/{matName}.mat";
        var    shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;

        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(mat, path);
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
#endif
