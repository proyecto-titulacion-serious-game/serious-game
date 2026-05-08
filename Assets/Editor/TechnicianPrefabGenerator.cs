using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera el prefab completo de la estación de trabajo del Técnico:
/// robot RobotKyle + escritorio + bandeja + canvas UI + cámara PC.
/// Todos los campos de TechnicianWorkstation y ComponentSendingTray
/// quedan cableados automáticamente.
///
/// Uso: Tools → TITA → Generar Prefab Técnico
/// Resultado: Assets/Prefabs/Technician_Workstation.prefab
/// </summary>
public static class TechnicianPrefabGenerator
{
    private const string PREFAB_PATH   = "Assets/Prefabs/Technician_Workstation.prefab";
    private const string KYLE_PATH     = "Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab";

    [MenuItem("Tools/TITA/Generar Prefab Técnico")]
    public static void Generate()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"Ya existe:\n{PREFAB_PATH}\n\n¿Sobreescribir?",
                "Sí", "Cancelar");
            if (!overwrite) return;
        }

        // ── Raíz ─────────────────────────────────────────────────────
        var root = new GameObject("Technician_Workstation");

        // ── Robot Kyle ───────────────────────────────────────────────
        var kylePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KYLE_PATH);
        GameObject kyle;
        if (kylePrefab != null)
        {
            kyle = (GameObject)PrefabUtility.InstantiatePrefab(kylePrefab);
            kyle.transform.SetParent(root.transform, false);
            kyle.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            kyle.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            kyle.name = "RobotKyle_Technician";

            // Desactivar animaciones de locomoción (técnico está estático)
            var anim = kyle.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetFloat("MotionSpeed", 0f);
            }
        }
        else
        {
            // Placeholder si el prefab no está importado
            kyle = CreateBox(root, "RobotKyle_Technician [PLACEHOLDER]",
                new Vector3(0.4f, 1.8f, 0.3f),
                new Vector3(0f, 0.9f, 0.4f),
                CreateURPMat("Placeholder_Kyle", new Color(0.6f, 0.8f, 1f)));
            Debug.LogWarning("[TechnicianGenerator] RobotKyle.prefab no encontrado. Se usó un placeholder. " +
                             "Importa el asset SpaceRobotKyle y reasigna manualmente.");
        }

        // ── Cámara PC ────────────────────────────────────────────────
        var camGO  = new GameObject("PC_Camera");
        camGO.transform.SetParent(root.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.4f, -0.9f);
        camGO.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView  = 60f;
        cam.nearClipPlane = 0.05f;
        camGO.AddComponent<AudioListener>();

        // ── Escritorio (Desk Surface) ─────────────────────────────────
        var desk = CreateBox(root, "Desk_Surface",
            new Vector3(1.2f, 0.05f, 0.7f),
            new Vector3(0f, 0.75f, 0f),
            CreateURPMat("Mat_Desk_Generated", new Color(0.35f, 0.22f, 0.1f)));

        // ── Área de componentes ───────────────────────────────────────
        var compArea = new GameObject("ComponentsArea");
        compArea.transform.SetParent(root.transform, false);
        compArea.transform.localPosition = new Vector3(0f, 0.78f, -0.1f);

        var compMat = CreateURPMat("Mat_DeskComp", new Color(0.3f, 0.3f, 0.4f));

        // Resistor
        var compR = CreateDeskComp(compArea, "Comp_Resistor",
            new Vector3(-0.35f, 0.04f, 0f), ComponentType.Resistor,
            100f, "Resistencia 100 Ω\nUsar para limitar corriente del LED", compMat);

        // LED
        var compL = CreateDeskComp(compArea, "Comp_LED",
            new Vector3(-0.15f, 0.04f, 0f), ComponentType.LED,
            1f, "LED rojo 2V\nVerificar polaridad antes de instalar", compMat);

        // Capacitor
        var compC = CreateDeskComp(compArea, "Comp_Capacitor",
            new Vector3(0.05f, 0.04f, 0f), ComponentType.Capacitor,
            1f, "Capacitor electrolítico 100µF\nBanda negra = cátodo (−)", compMat);

        // ArduinoPin
        var compA = CreateDeskComp(compArea, "Comp_ArduinoPin",
            new Vector3(0.25f, 0.04f, 0f), ComponentType.ArduinoPin,
            2f, "Cable de pin digital Arduino\nConsultar pinout en el manual", compMat);

        // ── Bandeja de envío ──────────────────────────────────────────
        var trayGO = new GameObject("SendingTray");
        trayGO.transform.SetParent(root.transform, false);
        trayGO.transform.localPosition = new Vector3(0f, 0.78f, 0.22f);

        // Visual de la bandeja
        var trayVisual = CreateBox(trayGO, "Tray_Visual",
            new Vector3(0.28f, 0.015f, 0.18f), Vector3.zero,
            CreateURPMat("Mat_SendingTray", new Color(0.18f, 0.18f, 0.22f)));

        // Collider trigger para detección VR
        var trayCol = trayGO.AddComponent<BoxCollider>();
        trayCol.size      = new Vector3(0.28f, 0.08f, 0.18f);
        trayCol.center    = new Vector3(0f, 0.04f, 0f);
        trayCol.isTrigger = true;

        // Slot (centro de la bandeja)
        var traySlot = new GameObject("TraySlot");
        traySlot.transform.SetParent(trayGO.transform, false);
        traySlot.transform.localPosition = new Vector3(0f, 0.04f, 0f);

        // Canvas World Space de la bandeja
        var trayCanvas = BuildCanvas(trayGO, "Tray_Canvas",
            new Vector3(0f, 0.07f, 0f),
            new Vector2(240f, 220f), 0.0008f);

        // Textos y controles de la bandeja
        var txtComp   = AddTMP(trayCanvas, "TMP_ComponenteEnBandeja",
            "Bandeja vacía", new Vector2(0, 85), new Vector2(220, 24), 9f, Color.white);
        var txtDesc   = AddTMP(trayCanvas, "TMP_Descripcion",
            "Haz click en un componente", new Vector2(0, 52), new Vector2(220, 48), 7f,
            new Color(0.8f, 0.8f, 0.8f));

        var txtInLabel = AddTMP(trayCanvas, "TMP_InputLabel",
            "Valor calculado (ohm):", new Vector2(0, 16), new Vector2(220, 16), 6.5f, Color.yellow);
        txtInLabel.gameObject.SetActive(false);

        var inputField = AddInputField(trayCanvas, "InputField_Valor",
            new Vector2(0, -4), new Vector2(200, 22));
        inputField.gameObject.SetActive(false);

        var txtTogLabel = AddTMP(trayCanvas, "TMP_ToggleLabel",
            "Polaridad: CORRECTA", new Vector2(0, -28), new Vector2(220, 16), 6.5f, Color.cyan);
        txtTogLabel.gameObject.SetActive(false);

        var toggleGO = new GameObject("Toggle_Polaridad");
        toggleGO.transform.SetParent(trayCanvas.transform, false);
        var toggleRT = toggleGO.AddComponent<RectTransform>();
        toggleRT.anchoredPosition = new Vector2(-90f, -28f);
        toggleRT.sizeDelta        = new Vector2(20f, 20f);
        var toggle = toggleGO.AddComponent<Toggle>();
        toggle.isOn = true;
        toggleGO.SetActive(false);

        var btnEnviar = AddButton(trayCanvas, "BTN_Enviar",
            "ENVIAR →", new Vector2(0, -56), new Vector2(160, 28));
        btnEnviar.gameObject.SetActive(false);

        var txtFeedback = AddTMP(trayCanvas, "TMP_Feedback",
            "", new Vector2(0, -82), new Vector2(220, 20), 6f, new Color(1f, 0.6f, 0.2f));

        // Script ComponentSendingTray
        var sendingTray = trayGO.AddComponent<ComponentSendingTray>();
        sendingTray.txtComponenteEnBandeja = txtComp;
        sendingTray.txtDescripcion         = txtDesc;
        sendingTray.btnEnviar              = btnEnviar;
        sendingTray.txtFeedback            = txtFeedback;
        sendingTray.inputValor             = inputField;
        sendingTray.txtInputLabel          = txtInLabel;
        sendingTray.togglePolaridad        = toggle;
        sendingTray.txtToggleLabel         = txtTogLabel;
        sendingTray.traySlot               = traySlot.transform;

        // Asignar tray a cada DeskComponent
        SetTray(compR, sendingTray);
        SetTray(compL, sendingTray);
        SetTray(compC, sendingTray);
        SetTray(compA, sendingTray);

        // ── Panel de diagnóstico (World Space frente al Técnico) ──────
        var diagGO = new GameObject("DiagnosticPanel");
        diagGO.transform.SetParent(root.transform, false);
        diagGO.transform.localPosition = new Vector3(0f, 1.35f, 0.05f);
        diagGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var diagCanvas = BuildCanvas(diagGO, "DiagnosticPanel",
            Vector3.zero, new Vector2(340f, 160f), 0.0007f, worldParent: false);

        // Fondo del panel
        var diagBg = new GameObject("Background");
        diagBg.transform.SetParent(diagCanvas.transform, false);
        var diagBgRT  = diagBg.AddComponent<RectTransform>();
        diagBgRT.anchorMin = Vector2.zero;
        diagBgRT.anchorMax = Vector2.one;
        diagBgRT.offsetMin = Vector2.zero;
        diagBgRT.offsetMax = Vector2.zero;
        var diagImg   = diagBg.AddComponent<Image>();
        diagImg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

        var txtDiag = AddTMP(diagCanvas, "TMP_Diagnostico",
            "Diagnóstico del circuito...",
            new Vector2(0, 20), new Vector2(320, 70), 7.5f, new Color(0.3f, 1f, 0.5f));
        txtDiag.alignment = TextAlignmentOptions.TopLeft;

        var txtAccion = AddTMP(diagCanvas, "TMP_AccionSiguiente",
            "Siguiente acción...",
            new Vector2(0, -42), new Vector2(320, 36), 7f, new Color(1f, 0.9f, 0.3f));
        txtAccion.alignment = TextAlignmentOptions.TopLeft;

        // ── Mini HUD (Screen Space Overlay para modo PC) ──────────────
        var hudGO     = new GameObject("MiniHUD");
        hudGO.transform.SetParent(root.transform, false);
        var hudCanvas = hudGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 10;
        hudGO.AddComponent<CanvasScaler>();
        hudGO.AddComponent<GraphicRaycaster>();

        var hudBg = new GameObject("HUD_Background");
        hudBg.transform.SetParent(hudGO.transform, false);
        var hudBgRT  = hudBg.AddComponent<RectTransform>();
        hudBgRT.anchorMin = new Vector2(0f, 1f);
        hudBgRT.anchorMax = new Vector2(0f, 1f);
        hudBgRT.pivot     = new Vector2(0f, 1f);
        hudBgRT.anchoredPosition = new Vector2(8f, -8f);
        hudBgRT.sizeDelta = new Vector2(320f, 72f);
        var hudBgImg  = hudBg.AddComponent<Image>();
        hudBgImg.color = new Color(0.04f, 0.06f, 0.12f, 0.88f);

        var txtHudV = AddHUDText(hudGO, "TMP_HUD_Voltaje",
            "V: — V  |  I: — mA",
            new Vector2(164f, -20f), new Vector2(310f, 18f), 9f);
        var txtHudC = AddHUDText(hudGO, "TMP_HUD_Corriente",
            "Componentes: —",
            new Vector2(164f, -40f), new Vector2(310f, 18f), 8f);
        var txtHudR = AddHUDText(hudGO, "TMP_HUD_Reto",
            "RETO 1  —  Tiempo: —s",
            new Vector2(164f, -60f), new Vector2(310f, 18f), 8f);

        // ── TechnicianWorkstation + TechnicianController en raíz ──────
        var ws = root.AddComponent<TechnicianWorkstation>();
        ws.deskSurface    = desk.transform;
        ws.componentsArea = compArea.transform;
        ws.sendingTray    = trayGO.transform;
        ws.hudVoltaje     = txtHudV;
        ws.hudCorriente   = txtHudC;
        ws.hudReto        = txtHudR;
        ws.txtDiagnostico     = txtDiag;
        ws.txtAccionSiguiente = txtAccion;

        var tc = root.AddComponent<TechnicianController>();
        tc.mode       = TechnicianMode.Auto;
        tc.pcCamera   = cam;
        tc.technicianCanvas = diagGO.GetComponent<Canvas>() ?? diagGO.GetComponentInChildren<Canvas>();
        tc.canvasDistanceVR = 1.2f;

        // ── Guardar prefab ────────────────────────────────────────────
        bool saved;
        var  prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Prefab Técnico creado",
                $"Guardado en:\n{PREFAB_PATH}\n\n" +
                "Instrucciones:\n" +
                "1. Arrástralo a la escena Tecnico.unity.\n" +
                "2. Asigna GameManager y CircuitManager en TechnicianWorkstation.\n" +
                "3. (Opcional) Asigna ComponentDeliverySystem en ComponentSendingTray.\n" +
                "4. En Play: modo PC activo automáticamente.", "OK");
            Debug.Log($"[TechnicianPrefabGenerator] Prefab guardado en {PREFAB_PATH}");
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "No se pudo guardar. Verifica que exista Assets/Prefabs/.", "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Helpers de construcción
    // ─────────────────────────────────────────────────────────────────

    static GameObject CreateBox(GameObject parent, string name,
                                 Vector3 scale, Vector3 localPos, Material mat)
    {
        var go     = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetMesh(PrimitiveType.Cube);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        go.AddComponent<BoxCollider>();
        return go;
    }

    static GameObject CreateDeskComp(GameObject parent, string name,
                                      Vector3 localPos, ComponentType type,
                                      float value, string desc, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = new Vector3(0.04f, 0.04f, 0.04f);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetMesh(type == ComponentType.LED
                             ? PrimitiveType.Sphere
                             : PrimitiveType.Cylinder);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;

        var dc = go.AddComponent<DeskComponent>();
        dc.componentType        = type;
        dc.componentValue       = value;
        dc.componentDescription = desc;
        return go;
    }

    static void SetTray(GameObject compGO, ComponentSendingTray tray)
    {
        var dc = compGO.GetComponent<DeskComponent>();
        if (dc != null) dc.tray = tray;
    }

    static Canvas BuildCanvas(GameObject parent, string name,
                               Vector3 localPos, Vector2 sizeDelta,
                               float scale, bool worldParent = true)
    {
        var go = worldParent ? new GameObject(name) : parent;
        if (worldParent)
        {
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
        }
        go.transform.localScale = Vector3.one * scale;

        var canvas           = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        var rt      = go.GetComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        return canvas;
    }

    static TMP_Text AddTMP(Canvas parent, string name, string text,
                            Vector2 anchoredPos, Vector2 size,
                            float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt        = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return tmp;
    }

    static TMP_Text AddHUDText(GameObject parent, string name, string text,
                                Vector2 anchoredPos, Vector2 size, float fontSize)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin         = new Vector2(0f, 1f);
        rt.anchorMax         = new Vector2(0f, 1f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = anchoredPos;
        rt.sizeDelta         = size;
        return tmp;
    }

    static TMP_InputField AddInputField(Canvas parent, string name,
                                         Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        var field = go.AddComponent<TMP_InputField>();

        // Placeholder
        var phGO  = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var ph    = phGO.AddComponent<TextMeshProUGUI>();
        ph.text   = "Escribe el valor...";
        ph.color  = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        ph.fontSize = 6.5f;
        var phRT  = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(4, 2);
        phRT.offsetMax = new Vector2(-4, -2);

        // Text component
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.color    = Color.white;
        txt.fontSize = 6.5f;
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4, 2);
        txtRT.offsetMax = new Vector2(-4, -2);

        field.textComponent  = txt;
        field.placeholder    = ph;
        field.contentType    = TMP_InputField.ContentType.DecimalNumber;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        return field;
    }

    static Button AddButton(Canvas parent, string name, string label,
                             Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.5f, 0.2f, 0.95f);

        var btn   = go.AddComponent<Button>();
        var cb    = btn.colors;
        cb.highlightedColor = new Color(0.25f, 0.75f, 0.35f);
        cb.pressedColor     = new Color(0.1f, 0.35f, 0.15f);
        btn.colors          = cb;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 8f;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        return btn;
    }

    static Material CreateURPMat(string matName, Color color)
    {
        string path     = $"Assets/Materials/{matName}.mat";
        var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        var mat = new Material(shader);
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
