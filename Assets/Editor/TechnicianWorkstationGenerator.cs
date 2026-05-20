using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera el prefab completo de la estación de trabajo del Técnico.
///
/// Menú: Tools → TITA → Generar Prefab Technician Workstation
/// Resultado: Assets/Prefabs/Technician_Workstation.prefab
///
/// Jerarquía generada:
///   TechnicianWorkstation  [TechnicianWorkstation, TechnicianController, TechnicianManual]
///   ├─ PC_Camera           [Camera, AudioListener]
///   ├─ Desk_Surface        [MeshFilter, MeshRenderer, BoxCollider]
///   ├─ Manual_Book         [MeshFilter, MeshRenderer, BoxCollider, ManualBookOpener]
///   │   └─ Manual_Canvas   [Canvas WorldSpace] — mini label sobre el libro
///   ├─ Manual_Overlay      [Canvas ScreenSpaceOverlay, TechnicianManualDisplay] — inactivo
///   ├─ ComponentsArea
///   │   ├─ Comp_R100       [MeshRenderer, BoxCollider, DeskComponent]
///   │   ├─ Comp_R220       [...]
///   │   ├─ Comp_R330       [...]
///   │   ├─ Comp_LED        [MeshRenderer, SphereCollider, DeskComponent]
///   │   └─ Comp_Cap        [MeshRenderer, BoxCollider, DeskComponent]
///   ├─ SendingTray         [BoxCollider trigger, ComponentSendingTray]
///   │   ├─ Tray_Visual     [MeshFilter, MeshRenderer]
///   │   ├─ TraySlot        (anchor de spawn)
///   │   └─ Tray_Canvas     [Canvas WorldSpace] — InputField, Toggle, Botón Enviar
///   ├─ MiniHUD             [Canvas ScreenSpaceOverlay] — telemetría del circuito
///   └─ DiagnosticPanel     [Canvas WorldSpace] — diagnóstico + acción siguiente
///
/// REFERENCIAS INTERNAS ya cableadas al generar:
///   TechnicianWorkstation: manual, deskSurface, manualBook, sendingTray,
///                          componentsArea, hudVoltaje, hudCorriente, hudReto,
///                          txtDiagnostico, txtAccionSiguiente
///   TechnicianController:  pcCamera, technicianCanvas (→ MiniHUD)
///   TechnicianManualDisplay: manual, TMPs, botones
///   ComponentSendingTray:  todos los TMPs, InputField, Toggle, btnEnviar, traySlot
///   DeskComponent.tray:    → ComponentSendingTray
///   ManualBookOpener:      bookRenderer, manualOverlay
///
/// ASIGNAR MANUALMENTE:
///   TechnicianWorkstation → gameManager, circuit, technicianActions
///   TechnicianManualDisplay → gameManager
///   ComponentSendingTray → technicianActions, delivery, gameManager
/// </summary>
public static class TechnicianWorkstationGenerator
{
    private const string PREFAB_PATH   = "Assets/Prefabs/Technician_Workstation.prefab";

    // ── circuit/models — componentes electrónicos ─────────────────────────────
    private const string CM_BASE           = "Assets/circuit/models";
    private const string CM_TEX            = "Assets/circuit/textures/masterTex.png";
    private const string CM_MAT_PATH       = "Assets/Materials/Mat_Circuit.mat";
    private const string FBX_RESISTOR      = CM_BASE + "/resistor.fbx";
    private const string FBX_RESISTOR_V    = CM_BASE + "/resistorVertical.fbx";
    private const string FBX_LED_GREEN     = CM_BASE + "/LEDGreen.fbx";
    private const string FBX_LED_RED       = CM_BASE + "/LEDRed.fbx";
    private const string FBX_LED_YELLOW    = CM_BASE + "/LEDYellow.fbx";
    private const string FBX_CAP_BLUE      = CM_BASE + "/capacitorBlue.fbx";
    private const string FBX_CAP_BLACK     = CM_BASE + "/capacitorBlack.fbx";
    private const string FBX_CAP_ORANGE    = CM_BASE + "/capacitorOrange.fbx";
    private const string FBX_TRANS         = CM_BASE + "/transistor.fbx";
    private const string DELIVERED         = "Assets/Prefabs/Delivered";

    // Lado más largo deseado en metros para los componentes sobre la mesa.
    private const float DESK_TARGET   = 0.05f;

    // ── SigunStudio Workshop Tools — decoración (toolbox) ────────────────────
    private const string WS_BASE     = "Assets/SigunStudio/Toony Workshop Tools FREE";
    private const string WS_MAT      = WS_BASE + "/Materials/Main.mat";
    private const string FBX_TOOLBOX = WS_BASE + "/Meshes/Toolboxes/toolbox.fbx";

    [MenuItem("Tools/TITA/Generar Prefab Technician Workstation")]
    public static void Generate()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            bool ow = EditorUtility.DisplayDialog("Prefab ya existe",
                $"Ya existe:\n{PREFAB_PATH}\n\n¿Sobreescribir?",
                "Sí, sobreescribir", "Cancelar");
            if (!ow) return;
        }

        var root   = new GameObject("TechnicianWorkstation");
        var ws     = root.AddComponent<TechnicianWorkstation>();
        var tc     = root.AddComponent<TechnicianController>();
        var manual = root.AddComponent<TechnicianManual>();

        // ── PC Camera ─────────────────────────────────────────────────────────
        var camGO = new GameObject("PC_Camera");
        camGO.transform.SetParent(root.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.2f, 1.0f);
        camGO.transform.localRotation = Quaternion.Euler(15f, 180f, 0f);
        var cam             = camGO.AddComponent<Camera>();
        cam.fieldOfView     = 60f;
        cam.nearClipPlane   = 0.05f;
        camGO.AddComponent<AudioListener>();

        // ── Desk Surface ──────────────────────────────────────────────────────
        var desk = CreateCube(root, "Desk_Surface",
            new Vector3(0f, -0.025f, 0f), new Vector3(1.5f, 0.05f, 0.8f),
            CreateMat("Mat_DeskSurface", new Color(0.38f, 0.22f, 0.12f)));
        ws.deskSurface = desk.transform;

        // ── Manual Book ───────────────────────────────────────────────────────
        // Manual — pergamino enrollado sobre la mesa
        var scrollGO = BuildManualScroll(root, out var scrollComp);
        ws.manualBook = scrollGO.transform;

        // Manual Overlay — pantalla completa, se abre al desenrollar
        var overlayGO = BuildManualOverlay(root, manual, out var manualDisplay, out var closeBtn);
        overlayGO.SetActive(false);
        scrollComp.manualOverlay = overlayGO;
        if (closeBtn != null)
            closeBtn.onClick.AddListener(scrollComp.CloseManual);

        // ── ComponentsArea — modelos de circuit/models ────────────────────────
        var compArea = new GameObject("ComponentsArea");
        compArea.transform.SetParent(root.transform, false);
        compArea.transform.localPosition = new Vector3(0.05f, 0.04f, 0.0f);
        ws.componentsArea = compArea.transform;

        var circuitMat = GetCircuitMat();

        // Cargar variantes de Delivered para asignar como deliveredPrefab
        var dR_H  = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_Resistor.prefab");
        var dR_V  = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_Resistor_Vertical.prefab");
        var dLG   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_LED_Green.prefab");
        var dLR   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_LED_Red.prefab");
        var dLY   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_LED_Yellow.prefab");
        var dCB   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_Capacitor_Blue.prefab");
        var dCK   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_Capacitor_Black.prefab");
        var dCO   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_Capacitor_Orange.prefab");
        var dAP   = AssetDatabase.LoadAssetAtPath<GameObject>(DELIVERED + "/Delivered_ArduinoPIn.prefab");

        // ── Fila 1 — Resistores (z = -0.08) ─────────────────────────────────
        AddDeskCompWS(compArea, "Comp_R100", new Vector3(-0.28f, 0f, -0.08f),
            AutoScale(FBX_RESISTOR, DESK_TARGET), Vector3.zero,
            ComponentType.Resistor, 100f, "100 Ω — Reto 1 (Ley de Ohm)",
            FBX_RESISTOR, circuitMat, dR_H);

        AddDeskCompWS(compArea, "Comp_R220", new Vector3(-0.14f, 0f, -0.08f),
            AutoScale(FBX_RESISTOR, DESK_TARGET), Vector3.zero,
            ComponentType.Resistor, 220f, "220 Ω — Reto 3 (Mixto)",
            FBX_RESISTOR, circuitMat, dR_H);

        AddDeskCompWS(compArea, "Comp_R330", new Vector3(0.00f, 0f, -0.08f),
            AutoScale(FBX_RESISTOR, DESK_TARGET), Vector3.zero,
            ComponentType.Resistor, 330f, "330 Ω — Reto 4 (Buzzer)",
            FBX_RESISTOR, circuitMat, dR_H);

        AddDeskCompWS(compArea, "Comp_R_Vertical", new Vector3(0.14f, 0f, -0.08f),
            AutoScale(FBX_RESISTOR_V, DESK_TARGET), Vector3.zero,
            ComponentType.Resistor, 100f, "100 Ω Vertical",
            FBX_RESISTOR_V, circuitMat, dR_V);

        // ── Fila 2 — LEDs (z = +0.06) ────────────────────────────────────────
        AddDeskCompWS(compArea, "Comp_LED_Green", new Vector3(-0.28f, 0f, 0.06f),
            AutoScale(FBX_LED_GREEN, DESK_TARGET), Vector3.zero,
            ComponentType.LED, 1f, "LED Verde — polaridad correcta",
            FBX_LED_GREEN, circuitMat, dLG);

        AddDeskCompWS(compArea, "Comp_LED_Red", new Vector3(-0.15f, 0f, 0.06f),
            AutoScale(FBX_LED_RED, DESK_TARGET), Vector3.zero,
            ComponentType.LED, 1f, "LED Rojo — polaridad correcta",
            FBX_LED_RED, circuitMat, dLR);

        AddDeskCompWS(compArea, "Comp_LED_Yellow", new Vector3(-0.02f, 0f, 0.06f),
            AutoScale(FBX_LED_YELLOW, DESK_TARGET), Vector3.zero,
            ComponentType.LED, 1f, "LED Amarillo — polaridad correcta",
            FBX_LED_YELLOW, circuitMat, dLY);

        // ── Fila 2 — Capacitores (z = +0.06) ─────────────────────────────────
        AddDeskCompWS(compArea, "Comp_Cap_Blue", new Vector3(0.11f, 0f, 0.06f),
            AutoScale(FBX_CAP_BLUE, DESK_TARGET), Vector3.zero,
            ComponentType.Capacitor, 1f, "Capacitor Azul — polaridad correcta",
            FBX_CAP_BLUE, circuitMat, dCB);

        AddDeskCompWS(compArea, "Comp_Cap_Black", new Vector3(0.24f, 0f, 0.06f),
            AutoScale(FBX_CAP_BLACK, DESK_TARGET), Vector3.zero,
            ComponentType.Capacitor, 1f, "Capacitor Negro — polaridad correcta",
            FBX_CAP_BLACK, circuitMat, dCK);

        AddDeskCompWS(compArea, "Comp_Cap_Orange", new Vector3(0.37f, 0f, 0.06f),
            AutoScale(FBX_CAP_ORANGE, DESK_TARGET), Vector3.zero,
            ComponentType.Capacitor, 1f, "Capacitor Naranja — polaridad correcta",
            FBX_CAP_ORANGE, circuitMat, dCO);

        // ── Arduino Pin ───────────────────────────────────────────────────────
        AddDeskCompWS(compArea, "Comp_ArduinoPin", new Vector3(0.50f, 0f, 0.06f),
            AutoScale(FBX_TRANS, DESK_TARGET), Vector3.zero,
            ComponentType.ArduinoPin, 2f, "Pin Arduino — Reto 4 (numero de pin D2)",
            FBX_TRANS, circuitMat, dAP);

        // Decoración: caja de herramientas al costado de la mesa (SigunStudio)
        var wsMat = AssetDatabase.LoadAssetAtPath<Material>(WS_MAT);
        AddToolboxDecoration(root, wsMat);

        // ── ComponentDeliverySystem ───────────────────────────────────────────
        var delivery = root.AddComponent<ComponentDeliverySystem>();

        delivery.resistorPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Delivered/Delivered_Resistor.prefab");
        delivery.ledPrefab        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Delivered/Delivered_LED.prefab");
        delivery.capacitorPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Delivered/Delivered_Capacitor.prefab");
        delivery.arduinoPinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Delivered/Delivered_ArduinoPIn.prefab");

        if (delivery.resistorPrefab == null || delivery.ledPrefab == null ||
            delivery.capacitorPrefab == null || delivery.arduinoPinPrefab == null)
            Debug.LogWarning("[WorkstationGen] Uno o más Delivered prefabs no encontrados. " +
                             "Ejecuta primero 'Generar Prefabs Delivered'.");

        // ── Sending Tray ──────────────────────────────────────────────────────
        var trayGO           = BuildSendingTray(root, out var traySending);
        ws.sendingTray       = trayGO.transform;
        traySending.delivery = delivery;

        // Conectar todos los DeskComponents al tray
        foreach (var dc in compArea.GetComponentsInChildren<DeskComponent>())
            dc.tray = traySending;

        // ── MiniHUD (telemetría en vivo) ──────────────────────────────────────
        BuildMiniHUD(root, ws, out var miniCanvas);

        // ── DiagnosticPanel (Clipboard) ───────────────────────────────────────
        BuildDiagnosticPanel(root, ws, cam);

        // ── Cableado final ────────────────────────────────────────────────────
        ws.manual              = manual;
        tc.pcCamera            = cam;
        tc.technicianCanvas    = miniCanvas;
        manualDisplay.manual   = manual;

        // ── Guardar prefab ────────────────────────────────────────────────────
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Technician_Workstation creado",
                $"Guardado en:\n{PREFAB_PATH}\n\n" +
                "REFERENCIAS INTERNAS cableadas:\n" +
                "  ✓ TechnicianManual → mismo GO\n" +
                "  ✓ TechnicianController.pcCamera → PC_Camera\n" +
                "  ✓ TechnicianManualDisplay (manual, TMPs, botones)\n" +
                "  ✓ ComponentSendingTray (TMPs, InputField, Toggle, Btn, TraySlot)\n" +
                "  ✓ ComponentSendingTray.delivery → ComponentDeliverySystem\n" +
                "  ✓ DeskComponent.tray → ComponentSendingTray\n" +
                "  ✓ ManualBookOpener (renderer + overlay)\n" +
                "  ✓ TechnicianWorkstation (TMPs del MiniHUD y DiagnosticPanel)\n\n" +
                "COMPONENTES EN LA MESA:\n" +
                "  R100 (100Ω Reto1), R220 (220Ω Reto3), R330 (330Ω Reto4)\n" +
                "  LED, Capacitor, ArduinoPin\n\n" +
                "ASIGNAR MANUALMENTE:\n" +
                "  GameManager → TechnicianWorkstation, ComponentSendingTray\n" +
                "  ComponentDeliverySystem → resistorPrefab, ledPrefab, capacitorPrefab, arduinoPinPrefab\n" +
                "    (usar prefabs de Assets/Prefabs/Delivered/ — correr 'Generar Prefabs Delivered' primero)\n\n" +
                "OPCIONAL para modo VR estático:\n" +
                "  TechnicianController → xrOriginTechnician, rightHandVR\n" +
                "  Añadir XRSimpleInteractable a cada Comp_* para interacción VR",
                "OK");
            Debug.Log($"[WorkstationGenerator] Guardado en {PREFAB_PATH}");
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "No se pudo guardar el prefab.\nVerifica que exista Assets/Prefabs/.", "OK");
        }
    }

    // ── Sub-builders ─────────────────────────────────────────────────────────

    static GameObject BuildManualOverlay(GameObject root, TechnicianManual manual,
                                         out TechnicianManualDisplay display,
                                         out Button closeButton)
    {
        var go     = new GameObject("Manual_Overlay");
        go.transform.SetParent(root.transform, false);

        var canvas              = go.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder     = 20;
        var scaler              = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        go.AddComponent<GraphicRaycaster>();

        // Fondo oscuro
        AddPanelBG(go, new Color(0.04f, 0.06f, 0.14f, 0.97f));

        var txtTitulo = CreateTMP(go, "TMP_Titulo", "MANUAL TECNICO",
            new Vector2(0, 460f), new Vector2(900f, 50f), 26f, new Color(0.4f, 0.8f, 1f));
        txtTitulo.alignment = TextAlignmentOptions.Center;

        var txtIzq = CreateTMP(go, "TMP_PaginaIzquierda", "",
            new Vector2(-330f, -10f), new Vector2(580f, 820f), 10f, Color.white);
        txtIzq.alignment          = TextAlignmentOptions.TopLeft;
        txtIzq.textWrappingMode = TextWrappingModes.Normal;

        var txtDer = CreateTMP(go, "TMP_PaginaDerecha", "",
            new Vector2(330f, -10f), new Vector2(580f, 820f), 10f, Color.white);
        txtDer.alignment          = TextAlignmentOptions.TopLeft;
        txtDer.textWrappingMode = TextWrappingModes.Normal;

        var txtNumPag = CreateTMP(go, "TMP_NumeroPagina", "Pag 1 / 3",
            new Vector2(0, -460f), new Vector2(200f, 30f), 11f, new Color(0.7f, 0.7f, 0.7f));
        txtNumPag.alignment = TextAlignmentOptions.Center;

        var btnPrev = CreateButton(go, "Button_PaginaAnterior",
            new Vector2(-180f, -460f), new Vector2(160f, 40f),
            "◄ Anterior", new Color(0.1f, 0.2f, 0.4f), out _);

        var btnNext = CreateButton(go, "Button_PaginaSiguiente",
            new Vector2(180f, -460f), new Vector2(160f, 40f),
            "Siguiente ►", new Color(0.1f, 0.2f, 0.4f), out _);

        closeButton = CreateButton(go, "Button_Cerrar",
            new Vector2(870f, 480f), new Vector2(100f, 36f),
            "✕ Cerrar", new Color(0.4f, 0.1f, 0.1f), out _);

        display                     = go.AddComponent<TechnicianManualDisplay>();
        display.txtTitulo           = txtTitulo;
        display.txtPaginaIzquierda  = txtIzq;
        display.txtPaginaDerecha    = txtDer;
        display.txtNumeroPagina     = txtNumPag;
        display.btnPaginaAnterior   = btnPrev;
        display.btnPaginaSiguiente  = btnNext;

        return go;
    }

    /// <summary>
    /// Construye el pergamino enrollado que reemplaza al libro del manual.
    /// Estructura:
    ///   Manual_Scroll  [ManualScroll, BoxCollider]
    ///   ├─ Scroll_Roll    — cilindro horizontal (pergamino cerrado)
    ///   ├─ Scroll_CapL/R  — tapas decorativas en los extremos
    ///   └─ Scroll_Paper   — cubo plano que crece hacia arriba al abrir
    /// </summary>
    static GameObject BuildManualScroll(GameObject root, out ManualScroll scrollComp)
    {
        var parchmentMat = CreateMat("Mat_ScrollParchment", new Color(0.95f, 0.88f, 0.70f));
        var rollMat      = CreateMat("Mat_ScrollRoll",      new Color(0.85f, 0.76f, 0.58f));
        var capMat       = CreateMat("Mat_ScrollCap",       new Color(0.52f, 0.34f, 0.16f));

        // ── Raíz ─────────────────────────────────────────────────────────────
        var go = new GameObject("Manual_Scroll");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(-0.45f, 0.005f, -0.05f);
        go.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

        var col    = go.AddComponent<BoxCollider>();
        col.size   = new Vector3(0.26f, 0.08f, 0.09f);
        col.center = new Vector3(0f, 0.025f, 0f);

        // ── Cilindro del rollo (lying along X) ───────────────────────────────
        // Unity Cylinder: 2 m alto, 1 m diámetro a escala 1.
        // Con (0.04, 0.11, 0.04) + Euler(0,0,90): diámetro 8 cm, longitud 22 cm.
        var roll = new GameObject("Scroll_Roll");
        roll.transform.SetParent(go.transform, false);
        roll.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        roll.transform.localScale    = new Vector3(0.04f, 0.11f, 0.04f);
        roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        roll.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cylinder);
        roll.AddComponent<MeshRenderer>().sharedMaterial = rollMat;

        // ── Tapas decorativas ─────────────────────────────────────────────────
        foreach (var side in new[] { -1f, 1f })
        {
            var cap = new GameObject(side < 0 ? "Scroll_CapL" : "Scroll_CapR");
            cap.transform.SetParent(go.transform, false);
            cap.transform.localPosition = new Vector3(side * 0.115f, 0.025f, 0f);
            cap.transform.localScale    = new Vector3(0.022f, 0.006f, 0.022f);
            cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            cap.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cylinder);
            cap.AddComponent<MeshRenderer>().sharedMaterial = capMat;
        }

        // ── Papel (empieza plano, crece hacia arriba) ─────────────────────────
        // Se posiciona ligeramente detrás del rollo (-Z) para el efecto visual.
        var paper = new GameObject("Scroll_Paper");
        paper.transform.SetParent(go.transform, false);
        paper.transform.localPosition = new Vector3(0f, 0f, -0.046f);
        paper.transform.localScale    = new Vector3(0.21f, 0.001f, 0.002f);
        paper.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cube);
        paper.AddComponent<MeshRenderer>().sharedMaterial = parchmentMat;

        // ── Label "Click para abrir" (siempre visible) ────────────────────────
        var lblGO = CreateWorldCanvas(go, "Scroll_Label",
            new Vector3(0f, 0.072f, 0f), new Vector2(220f, 14f), 0.0007f);
        var lbl = CreateTMP(lblGO, "TMP_ScrollHint",
            "[ MANUAL  —  Click para abrir ]",
            Vector2.zero, new Vector2(215f, 12f), 7.5f, new Color(0.40f, 0.24f, 0.06f));
        lbl.alignment = TextAlignmentOptions.Center;

        // ── Componente ManualScroll ───────────────────────────────────────────
        scrollComp              = go.AddComponent<ManualScroll>();
        scrollComp.scrollRoll   = roll;
        scrollComp.scrollPaper  = paper;
        scrollComp.paperHeight  = 0.36f;
        scrollComp.animDuration = 0.50f;

        return go;
    }

    static GameObject BuildSendingTray(GameObject root, out ComponentSendingTray sending)
    {
        var go = new GameObject("SendingTray");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(0.3f, 0.01f, 0.22f);

        var col       = go.AddComponent<BoxCollider>();
        col.size      = new Vector3(0.3f, 0.15f, 0.22f);
        col.center    = new Vector3(0f, 0.07f, 0f);
        col.isTrigger = true;

        // Visual plano de la bandeja
        var vis = new GameObject("Tray_Visual");
        vis.transform.SetParent(go.transform, false);
        vis.transform.localScale = new Vector3(0.3f, 0.01f, 0.22f);
        vis.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Cube);
        vis.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_SendingTray", new Color(0.18f, 0.28f, 0.48f));

        // Anchor de spawn
        var slot = new GameObject("TraySlot");
        slot.transform.SetParent(go.transform, false);
        slot.transform.localPosition = new Vector3(0f, 0.06f, 0f);

        // Canvas WorldSpace inclinado hacia el técnico
        var cGO = CreateWorldCanvas(go, "Tray_Canvas",
            new Vector3(0f, 0.28f, 0f), new Vector2(280f, 340f), 0.001f);
        cGO.transform.localRotation = Quaternion.Euler(-65f, 0f, 0f);
        AddPanelBG(cGO, new Color(0.04f, 0.07f, 0.16f, 0.93f));

        var txtComp = CreateTMP(cGO, "TMP_ComponenteEnBandeja",
            "Bandeja vacía", new Vector2(0, 138f), new Vector2(260f, 24f), 10.5f,
            new Color(0.4f, 0.85f, 1f));

        var txtDesc = CreateTMP(cGO, "TMP_Descripcion",
            "Haz click en un componente de la mesa",
            new Vector2(0, 82f), new Vector2(260f, 70f), 8f, new Color(0.85f, 0.85f, 0.85f));
        txtDesc.textWrappingMode = TextWrappingModes.Normal;

        var txtInputLabel = CreateTMP(cGO, "TMP_InputLabel",
            "Valor calculado (ohm):", new Vector2(0, 35f), new Vector2(260f, 18f),
            8f, new Color(0.7f, 0.85f, 1f));
        txtInputLabel.gameObject.SetActive(false);

        var inputField = CreateInputField(cGO, "InputField_Valor",
            new Vector2(0, 6f), new Vector2(242f, 30f), "Escribe ohmios...");
        inputField.gameObject.SetActive(false);

        var toggle = CreateToggle(cGO, "Toggle_Polaridad",
            new Vector2(0, -30f), new Vector2(242f, 28f),
            "Polaridad: CORRECTA", out var txtToggle);
        toggle.gameObject.SetActive(false);
        txtToggle.gameObject.SetActive(false);

        var btnEnviar = CreateButton(cGO, "Button_Enviar",
            new Vector2(0, -78f), new Vector2(242f, 38f),
            "ENVIAR COMPONENTE", new Color(0.1f, 0.38f, 0.14f), out _);
        btnEnviar.gameObject.SetActive(false);

        var txtFeedback = CreateTMP(cGO, "TMP_Feedback", "",
            new Vector2(0, -130f), new Vector2(260f, 22f), 8f, new Color(1f, 0.8f, 0.3f));
        txtFeedback.alignment = TextAlignmentOptions.Center;

        sending                       = go.AddComponent<ComponentSendingTray>();
        sending.txtComponenteEnBandeja = txtComp;
        sending.txtDescripcion        = txtDesc;
        sending.txtInputLabel         = txtInputLabel;
        sending.inputValor            = inputField;
        sending.togglePolaridad       = toggle;
        sending.txtToggleLabel        = txtToggle;
        sending.btnEnviar             = btnEnviar;
        sending.txtFeedback           = txtFeedback;
        sending.traySlot              = slot.transform;

        return go;
    }

    static void BuildMiniHUD(GameObject root, TechnicianWorkstation ws, out Canvas miniCanvas)
    {
        var go              = new GameObject("MiniHUD");
        go.transform.SetParent(root.transform, false);

        miniCanvas              = go.AddComponent<Canvas>();
        miniCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        miniCanvas.sortingOrder = 5;
        var scaler              = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode      = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        go.AddComponent<GraphicRaycaster>();

        // Panel telemetría — esquina superior derecha
        var panel = new GameObject("Panel_Telemetria");
        panel.transform.SetParent(go.transform, false);
        var rt          = panel.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(1f, 1f);
        rt.anchorMax    = new Vector2(1f, 1f);
        rt.pivot        = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta    = new Vector2(390f, 90f);
        panel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.14f, 0.88f);

        var txtV = CreateTMP(panel, "TMP_HudVoltaje",
            "V: —  |  I: — mA  —",
            new Vector2(-10f, -18f), new Vector2(370f, 20f), 10f,
            new Color(0.4f, 1f, 0.4f));
        txtV.alignment = TextAlignmentOptions.Right;

        var txtC = CreateTMP(panel, "TMP_HudCorriente", "—",
            new Vector2(-10f, -46f), new Vector2(370f, 28f), 8.5f,
            new Color(0.85f, 0.9f, 1f));
        txtC.alignment          = TextAlignmentOptions.Right;
        txtC.textWrappingMode = TextWrappingModes.Normal;

        var txtR = CreateTMP(panel, "TMP_HudReto",
            "RETO 1 — Tiempo: 480 s",
            new Vector2(-10f, -76f), new Vector2(370f, 18f), 9f,
            new Color(1f, 0.85f, 0.4f));
        txtR.alignment = TextAlignmentOptions.Right;

        ws.hudVoltaje   = txtV;
        ws.hudCorriente = txtC;
        ws.hudReto      = txtR;
    }

    /// <summary>
    /// Construye el clipboard físico con tablero, clip metálico, papel y canvas de diagnóstico.
    /// Click → anima hacia la cámara.  Click / Escape → regresa a la mesa.
    /// </summary>
    static void BuildDiagnosticPanel(GameObject root, TechnicianWorkstation ws, Camera pcCamera)
    {
        // ── Raíz del clipboard ────────────────────────────────────────────────
        var clip = new GameObject("Clipboard");
        clip.transform.SetParent(root.transform, false);
        // Sobre la mesa, ligeramente apoyado hacia el técnico
        clip.transform.localPosition = new Vector3(-0.46f, 0.03f, 0.08f);
        clip.transform.localRotation = Quaternion.Euler(-80f, 12f, -4f);

        // Collider principal para hacer click en el clipboard completo
        var col    = clip.AddComponent<BoxCollider>();
        col.size   = new Vector3(0.22f, 0.34f, 0.02f);
        col.center = Vector3.zero;

        var zoom             = clip.AddComponent<ClipboardZoom>();
        zoom.pcCamera        = pcCamera;
        zoom.zoomedDistance  = 0.45f;
        zoom.zoomedScaleMult = 2f;
        zoom.animDuration    = 0.35f;

        // ── Tablero (board) ───────────────────────────────────────────────────
        var board = new GameObject("Clipboard_Board");
        board.transform.SetParent(clip.transform, false);
        board.transform.localScale = new Vector3(0.22f, 0.34f, 0.008f);
        board.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cube);
        board.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_ClipboardBoard", new Color(0.32f, 0.20f, 0.09f));

        // ── Clip metálico (parte superior) ────────────────────────────────────
        var metalClip = new GameObject("Clipboard_Clip");
        metalClip.transform.SetParent(clip.transform, false);
        metalClip.transform.localPosition = new Vector3(0f, 0.158f, 0.006f);
        metalClip.transform.localScale    = new Vector3(0.09f, 0.028f, 0.014f);
        metalClip.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cube);
        metalClip.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_ClipMetal", new Color(0.62f, 0.63f, 0.68f));

        // ── Papel (hoja en blanco) ────────────────────────────────────────────
        var paper = new GameObject("Clipboard_Paper");
        paper.transform.SetParent(clip.transform, false);
        paper.transform.localPosition = new Vector3(0f, -0.012f, 0.005f);
        paper.transform.localScale    = new Vector3(0.19f, 0.28f, 0.002f);
        paper.AddComponent<MeshFilter>().sharedMesh      = GetMesh(PrimitiveType.Cube);
        paper.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_ClipboardPaper", new Color(0.97f, 0.96f, 0.92f));

        // ── Canvas con el diagnóstico (flotando sobre el papel) ───────────────
        var canvasGO = new GameObject("Clipboard_Canvas");
        canvasGO.transform.SetParent(clip.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, -0.012f, 0.008f);
        canvasGO.transform.localScale    = Vector3.one * 0.001f;

        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(185f, 275f);

        var hdr1 = CreateTMP(canvasGO, "TMP_HeaderDiag", "DIAGNÓSTICO",
            new Vector2(0f, 124f), new Vector2(178f, 18f), 9.5f, new Color(0.2f, 0.35f, 0.75f));
        hdr1.alignment = TextAlignmentOptions.Center;

        // Línea divisora (guión largo)
        var sep1 = CreateTMP(canvasGO, "TMP_Sep1", "─────────────────",
            new Vector2(0f, 108f), new Vector2(178f, 12f), 6f, new Color(0.6f, 0.6f, 0.6f));
        sep1.alignment = TextAlignmentOptions.Center;

        var txtDiag = CreateTMP(canvasGO, "TMP_Diagnostico", "—",
            new Vector2(0f, 30f), new Vector2(175f, 130f), 7f, new Color(0.08f, 0.08f, 0.08f));
        txtDiag.alignment          = TextAlignmentOptions.TopLeft;
        txtDiag.textWrappingMode = TextWrappingModes.Normal;

        var hdr2 = CreateTMP(canvasGO, "TMP_HeaderAccion", "SIGUIENTE ACCIÓN",
            new Vector2(0f, -52f), new Vector2(178f, 16f), 8.5f, new Color(0.55f, 0.25f, 0f));
        hdr2.alignment = TextAlignmentOptions.Center;

        var sep2 = CreateTMP(canvasGO, "TMP_Sep2", "─────────────────",
            new Vector2(0f, -66f), new Vector2(178f, 12f), 6f, new Color(0.6f, 0.6f, 0.6f));
        sep2.alignment = TextAlignmentOptions.Center;

        var txtAccion = CreateTMP(canvasGO, "TMP_AccionSiguiente", "—",
            new Vector2(0f, -118f), new Vector2(175f, 82f), 7f, new Color(0.12f, 0.12f, 0.12f));
        txtAccion.alignment          = TextAlignmentOptions.TopLeft;
        txtAccion.textWrappingMode = TextWrappingModes.Normal;

        // Nota al pie — hint de interacción
        var hint = CreateTMP(canvasGO, "TMP_HintZoom", "[ Click para leer · Esc para cerrar ]",
            new Vector2(0f, -130f), new Vector2(178f, 12f), 5f, new Color(0.55f, 0.55f, 0.55f));
        hint.alignment = TextAlignmentOptions.Center;

        ws.txtDiagnostico     = txtDiag;
        ws.txtAccionSiguiente = txtAccion;
    }

    // ── DeskComponent Workshop helpers ───────────────────────────────────────

    /// <summary>
    /// Crea un DeskComponent usando un mesh del SigunStudio Workshop asset.
    /// Si el FBX no carga (cambia la ruta) usa una primitiva como fallback.
    /// Escala: X/Z = grosor, Y = longitud. Rota el GO para orientar el modelo.
    /// </summary>
    static void AddDeskCompWS(GameObject parent, string name, Vector3 localPos,
                               Vector3 scale, Vector3 rotEuler,
                               ComponentType type, float value, string desc,
                               string fbxPath, Material wsMat,
                               GameObject deliveredPrefab = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.transform.localRotation = Quaternion.Euler(rotEuler);

        var mesh = LoadFBXMesh(fbxPath);

        if (mesh != null && wsMat != null)
        {
            go.AddComponent<MeshFilter>().sharedMesh       = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = wsMat;
        }
        else
        {
            var prim = type == ComponentType.LED ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh(prim);
            go.AddComponent<MeshRenderer>().sharedMaterial =
                CreateMat("Mat_FB_" + name, new Color(0.6f, 0.6f, 0.6f));
            Debug.LogWarning($"[WorkstationGen] FBX no encontrado en {fbxPath}. Usando primitiva para {name}.");
        }

        go.AddComponent<BoxCollider>();

        var dc                  = go.AddComponent<DeskComponent>();
        dc.componentType        = type;
        dc.componentValue       = value;
        dc.componentDescription = desc;
        dc.deliveredPrefab      = deliveredPrefab;
        dc.colorNormal          = Color.white;
        dc.colorHover           = new Color(1f, 0.85f, 0.2f);
        dc.colorSelected        = new Color(0.3f, 1f, 0.45f);
    }

    /// <summary>Añade la caja de herramientas como decoración visual en la esquina de la mesa.</summary>
    static void AddToolboxDecoration(GameObject root, Material wsMat)
    {
        var mesh = LoadFBXMesh(FBX_TOOLBOX);
        if (mesh == null || wsMat == null) return;

        var go = new GameObject("Toolbox_Deco");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(-0.55f, 0.025f, 0.0f);
        go.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
        go.AddComponent<MeshFilter>().sharedMesh      = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = wsMat;
        go.AddComponent<BoxCollider>();
    }

    /// <summary>Carga el primer Mesh encontrado dentro de un archivo FBX.</summary>
    static Mesh LoadFBXMesh(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is Mesh m) return m;
        return null;
    }

    /// <summary>
    /// Escala uniforme para que el lado más largo del mesh mida targetSize metros.
    /// </summary>
    static Vector3 AutoScale(string fbxPath, float targetSize)
    {
        var mesh = LoadFBXMesh(fbxPath);
        if (mesh == null) return Vector3.one * targetSize;
        float longest = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
        if (longest < 0.0001f) return Vector3.one * targetSize;
        return Vector3.one * (targetSize / longest);
    }

    /// <summary>
    /// Material URP compatible con masterTex.png del asset circuit/models.
    /// Reutiliza el .mat si ya existe; si no, lo crea en Assets/Materials/.
    /// </summary>
    static Material GetCircuitMat()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(CM_MAT_PATH);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        var tex    = AssetDatabase.LoadAssetAtPath<Texture2D>(CM_TEX);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, CM_MAT_PATH);
        return mat;
    }

    // ── DeskComponent helpers (primitivas — kept como fallback) ───────────────

    static void AddDeskComp(GameObject parent, string name, Vector3 localPos,
                             ComponentType type, float value, string desc, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = new Vector3(0.03f, 0.045f, 0.03f);

        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Cylinder);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<BoxCollider>();

        var dc                     = go.AddComponent<DeskComponent>();
        dc.componentType           = type;
        dc.componentValue          = value;
        dc.componentDescription    = desc;
    }

    static void AddDeskCompSphere(GameObject parent, string name, Vector3 localPos,
                                   ComponentType type, float value, string desc, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = new Vector3(0.035f, 0.035f, 0.035f);

        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Sphere);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<SphereCollider>();

        var dc                  = go.AddComponent<DeskComponent>();
        dc.componentType        = type;
        dc.componentValue       = value;
        dc.componentDescription = desc;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    static GameObject CreateWorldCanvas(GameObject parent, string name,
                                         Vector3 localPos, Vector2 sizeDelta, float scale)
    {
        var go        = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * scale;

        var canvas        = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.GetComponent<RectTransform>().sizeDelta = sizeDelta;
        return go;
    }

    static void AddPanelBG(GameObject parent, Color color)
    {
        var bg = new GameObject("Panel_BG");
        bg.transform.SetParent(parent.transform, false);
        var rt        = bg.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = color;
        bg.transform.SetAsFirstSibling();
    }

    static TMP_Text CreateTMP(GameObject parent, string name, string text,
                               Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp               = go.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = fontSize;
        tmp.color             = color;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        var rt              = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    static Button CreateButton(GameObject parent, string name,
                                Vector2 pos, Vector2 size, string label,
                                Color bgColor, out TMP_Text outLabel)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var img             = go.AddComponent<Image>();
        img.color           = bgColor;
        var btn             = go.AddComponent<Button>();
        btn.targetGraphic   = img;
        var cb              = btn.colors;
        cb.highlightedColor = bgColor * 1.35f;
        cb.pressedColor     = bgColor * 0.65f;
        cb.disabledColor    = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors          = cb;
        outLabel            = CreateTMP(go, "TMP_Label", label, Vector2.zero,
                                  size - new Vector2(8f, 0f), 9f, Color.white);
        return btn;
    }

    static Toggle CreateToggle(GameObject parent, string name,
                                Vector2 pos, Vector2 size,
                                string label, out TMP_Text outLabel)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var toggle          = go.AddComponent<Toggle>();

        float boxSize = size.y - 4f;

        // Checkbox background
        var bgGO        = new GameObject("Background");
        bgGO.transform.SetParent(go.transform, false);
        var bgRT        = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin  = new Vector2(0f, 0.5f);
        bgRT.anchorMax  = new Vector2(0f, 0.5f);
        bgRT.pivot      = new Vector2(0f, 0.5f);
        bgRT.anchoredPosition = new Vector2(2f, 0f);
        bgRT.sizeDelta  = new Vector2(boxSize, boxSize);
        var bgImg       = bgGO.AddComponent<Image>();
        bgImg.color     = new Color(0.12f, 0.18f, 0.32f);

        // Checkmark
        var ckGO        = new GameObject("Checkmark");
        ckGO.transform.SetParent(bgGO.transform, false);
        var ckRT        = ckGO.AddComponent<RectTransform>();
        ckRT.anchorMin  = Vector2.zero;
        ckRT.anchorMax  = Vector2.one;
        ckRT.offsetMin  = new Vector2(3f, 3f);
        ckRT.offsetMax  = new Vector2(-3f, -3f);
        var ckImg       = ckGO.AddComponent<Image>();
        ckImg.color     = new Color(0.2f, 0.9f, 0.3f);

        // Label
        var lblGO       = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lblRT       = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(boxSize + 6f, 0f);
        lblRT.offsetMax = Vector2.zero;
        var lbl         = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text        = label;
        lbl.fontSize    = 8.5f;
        lbl.color       = Color.white;
        lbl.alignment   = TextAlignmentOptions.Left;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = ckImg;
        toggle.isOn          = true;

        outLabel = lbl;
        return toggle;
    }

    static TMP_InputField CreateInputField(GameObject parent, string name,
                                            Vector2 pos, Vector2 size, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.2f, 0.95f);
        var field = go.AddComponent<TMP_InputField>();

        // Viewport con máscara de recorte
        var areaGO        = new GameObject("Text_Area");
        areaGO.transform.SetParent(go.transform, false);
        var areaRT        = areaGO.AddComponent<RectTransform>();
        areaRT.anchorMin  = Vector2.zero;
        areaRT.anchorMax  = Vector2.one;
        areaRT.offsetMin  = new Vector2(5f, 2f);
        areaRT.offsetMax  = new Vector2(-5f, -2f);
        areaGO.AddComponent<RectMask2D>();

        // Placeholder
        var phGO        = new GameObject("Placeholder");
        phGO.transform.SetParent(areaGO.transform, false);
        var phRT        = phGO.AddComponent<RectTransform>();
        phRT.anchorMin  = Vector2.zero;
        phRT.anchorMax  = Vector2.one;
        phRT.offsetMin  = phRT.offsetMax = Vector2.zero;
        var phTmp       = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text      = placeholder;
        phTmp.fontSize  = 8f;
        phTmp.color     = new Color(0.5f, 0.5f, 0.6f);
        phTmp.fontStyle = FontStyles.Italic;

        // Input text
        var txtGO        = new GameObject("Text");
        txtGO.transform.SetParent(areaGO.transform, false);
        var txtRT        = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin  = Vector2.zero;
        txtRT.anchorMax  = Vector2.one;
        txtRT.offsetMin  = txtRT.offsetMax = Vector2.zero;
        var inputTmp     = txtGO.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize = 9f;
        inputTmp.color   = Color.white;

        field.textViewport  = areaRT;
        field.textComponent = inputTmp;
        field.placeholder   = phTmp;

        return field;
    }

    // ── Mesh / Material helpers ───────────────────────────────────────────────

    static GameObject CreateCube(GameObject parent, string name,
                                  Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Cube);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<BoxCollider>();
        return go;
    }

    static Material CreateMat(string matName, Color color)
    {
        string path    = $"Assets/Materials/{matName}.mat";
        var existing   = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing  != null) return existing;
        var shader     = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat        = new Material(shader);
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
