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
    private const string PREFAB_PATH = "Assets/Prefabs/Technician_Workstation.prefab";

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
        var book = CreateCube(root, "Manual_Book",
            new Vector3(-0.45f, 0.02f, -0.1f), new Vector3(0.4f, 0.02f, 0.3f),
            CreateMat("Mat_ManualBook", new Color(0.12f, 0.22f, 0.52f)));
        ws.manualBook = book.transform;

        var bookOpener           = book.AddComponent<ManualBookOpener>();
        bookOpener.bookRenderer  = book.GetComponent<Renderer>();

        // Mini label WorldSpace sobre el libro
        var mcGO = CreateWorldCanvas(book, "Manual_Canvas",
            new Vector3(0f, 0.13f, 0f), new Vector2(400f, 280f), 0.0008f);
        AddPanelBG(mcGO, new Color(0.04f, 0.06f, 0.14f, 0.92f));
        var mcLabel = CreateTMP(mcGO, "TMP_ManualLabel",
            "MANUAL TECNICO\n[Click para abrir]",
            Vector2.zero, new Vector2(380f, 270f), 14f, new Color(0.4f, 0.8f, 1f));
        mcLabel.alignment = TextAlignmentOptions.Center;

        // Manual Overlay — pantalla completa, se abre con click en el libro
        var overlayGO       = BuildManualOverlay(root, manual, out var manualDisplay);
        overlayGO.SetActive(false);
        bookOpener.manualOverlay = overlayGO;

        // ── ComponentsArea ────────────────────────────────────────────────────
        var compArea = new GameObject("ComponentsArea");
        compArea.transform.SetParent(root.transform, false);
        compArea.transform.localPosition = new Vector3(0.1f, 0.025f, 0.05f);
        ws.componentsArea = compArea.transform;

        AddDeskComp(compArea, "Comp_R100", new Vector3(-0.2f, 0, 0),
            ComponentType.Resistor, 100f, "100 Ω — Reto 1 (Ley de Ohm)",
            CreateMat("Mat_CompR100", new Color(0.85f, 0.75f, 0.55f)));
        AddDeskComp(compArea, "Comp_R220", new Vector3(-0.1f, 0, 0),
            ComponentType.Resistor, 220f, "220 Ω — Reto 3 (Mixto)",
            CreateMat("Mat_CompR220", new Color(0.80f, 0.70f, 0.50f)));
        AddDeskComp(compArea, "Comp_R330", new Vector3(0f, 0, 0),
            ComponentType.Resistor, 330f, "330 Ω — Reto 4 (Buzzer)",
            CreateMat("Mat_CompR330", new Color(0.75f, 0.65f, 0.45f)));
        AddDeskCompSphere(compArea, "Comp_LED", new Vector3(0.1f, 0, 0),
            ComponentType.LED, 1f, "LED — Verificar polaridad (Reto 2 / 3)",
            CreateMat("Mat_CompLED", new Color(0.15f, 0.95f, 0.25f)));
        AddDeskComp(compArea, "Comp_Cap", new Vector3(0.2f, 0, 0),
            ComponentType.Capacitor, 1f, "Capacitor — Verificar polaridad (Reto 3)",
            CreateMat("Mat_CompCap", new Color(0.1f, 0.2f, 0.75f)));

        // ── Sending Tray ──────────────────────────────────────────────────────
        var trayGO           = BuildSendingTray(root, out var traySending);
        ws.sendingTray       = trayGO.transform;

        // Conectar todos los DeskComponents al tray
        foreach (var dc in compArea.GetComponentsInChildren<DeskComponent>())
            dc.tray = traySending;

        // ── MiniHUD (telemetría en vivo) ──────────────────────────────────────
        BuildMiniHUD(root, ws, out var miniCanvas);

        // ── DiagnosticPanel ───────────────────────────────────────────────────
        BuildDiagnosticPanel(root, ws);

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
                "  ✓ DeskComponent.tray → ComponentSendingTray\n" +
                "  ✓ ManualBookOpener (renderer + overlay)\n" +
                "  ✓ TechnicianWorkstation (TMPs del MiniHUD y DiagnosticPanel)\n\n" +
                "ASIGNAR MANUALMENTE:\n" +
                "  TechnicianWorkstation → gameManager, circuit, technicianActions\n" +
                "  TechnicianManualDisplay → gameManager\n" +
                "  ComponentSendingTray → technicianActions, delivery, gameManager\n\n" +
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
                                         out TechnicianManualDisplay display)
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
        txtIzq.enableWordWrapping = true;

        var txtDer = CreateTMP(go, "TMP_PaginaDerecha", "",
            new Vector2(330f, -10f), new Vector2(580f, 820f), 10f, Color.white);
        txtDer.alignment          = TextAlignmentOptions.TopLeft;
        txtDer.enableWordWrapping = true;

        var txtNumPag = CreateTMP(go, "TMP_NumeroPagina", "Pag 1 / 3",
            new Vector2(0, -460f), new Vector2(200f, 30f), 11f, new Color(0.7f, 0.7f, 0.7f));
        txtNumPag.alignment = TextAlignmentOptions.Center;

        var btnPrev = CreateButton(go, "Button_PaginaAnterior",
            new Vector2(-180f, -460f), new Vector2(160f, 40f),
            "◄ Anterior", new Color(0.1f, 0.2f, 0.4f), out _);

        var btnNext = CreateButton(go, "Button_PaginaSiguiente",
            new Vector2(180f, -460f), new Vector2(160f, 40f),
            "Siguiente ►", new Color(0.1f, 0.2f, 0.4f), out _);

        CreateButton(go, "Button_Cerrar",
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
        txtDesc.enableWordWrapping = true;

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
        txtC.enableWordWrapping = true;

        var txtR = CreateTMP(panel, "TMP_HudReto",
            "RETO 1 — Tiempo: 480 s",
            new Vector2(-10f, -76f), new Vector2(370f, 18f), 9f,
            new Color(1f, 0.85f, 0.4f));
        txtR.alignment = TextAlignmentOptions.Right;

        ws.hudVoltaje   = txtV;
        ws.hudCorriente = txtC;
        ws.hudReto      = txtR;
    }

    static void BuildDiagnosticPanel(GameObject root, TechnicianWorkstation ws)
    {
        var go = new GameObject("DiagnosticPanel");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(-0.42f, 0.5f, -0.38f);
        go.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);

        var canvas        = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        var rt            = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(240f, 320f);
        go.transform.localScale = Vector3.one * 0.001f;

        AddPanelBG(go, new Color(0.04f, 0.06f, 0.14f, 0.93f));

        var hdr1 = CreateTMP(go, "TMP_HeaderDiag", "DIAGNÓSTICO",
            new Vector2(0, 145f), new Vector2(220f, 20f), 9f, new Color(0.4f, 0.8f, 1f));
        hdr1.alignment = TextAlignmentOptions.Center;

        var txtDiag = CreateTMP(go, "TMP_Diagnostico", "—",
            new Vector2(0, 55f), new Vector2(220f, 150f), 7f, Color.white);
        txtDiag.alignment          = TextAlignmentOptions.TopLeft;
        txtDiag.enableWordWrapping = true;

        var hdr2 = CreateTMP(go, "TMP_HeaderAccion", "SIGUIENTE ACCIÓN",
            new Vector2(0, -50f), new Vector2(220f, 18f), 8.5f, new Color(1f, 0.85f, 0.3f));
        hdr2.alignment = TextAlignmentOptions.Center;

        var txtAccion = CreateTMP(go, "TMP_AccionSiguiente", "—",
            new Vector2(0, -120f), new Vector2(220f, 110f), 7f, new Color(0.9f, 0.95f, 0.7f));
        txtAccion.alignment          = TextAlignmentOptions.TopLeft;
        txtAccion.enableWordWrapping = true;

        ws.txtDiagnostico     = txtDiag;
        ws.txtAccionSiguiente = txtAccion;
    }

    // ── DeskComponent helpers ─────────────────────────────────────────────────

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
        tmp.enableWordWrapping = false;
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
