using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Configura la escena IntegratedDemo añadiendo solo lo que falta:
///   • Cuerpo robot del Explorador  (ExplorerAvatar)
///   • Cuerpo robot del Técnico     (estático, idle)
///   • TechnicianWorkstation        (script + MiniHUD + DiagnosticPanel)
///   • Zona Reto 4                  (Arduino)
///   • Tags RightHand / LeftHand    en los controladores XR
///
/// Menú: Tools → TITA → Configurar IntegratedDemo
///
/// PREREQUISITO: la escena IntegratedDemo.unity debe estar abierta.
/// </summary>
public static class IntegratedDemoSetup
{
    // Paths
    private const string KYLE_PATH  = "Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab";
    private const string SCENE_NAME = "IntegratedDemo";

    // ─────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Configurar IntegratedDemo")]
    public static void Setup()
    {
        // Verificar escena activa
        if (!EnsureSceneOpen()) return;

        int steps = 0;

        steps += SetupExplorerRobot()     ? 1 : 0;
        steps += SetupTechnicianRobot()   ? 1 : 0;
        steps += SetupTechnicianHUD()     ? 1 : 0;
        steps += SetupReto4()             ? 1 : 0;
        steps += TagHandControllers()     ? 1 : 0;
        steps += WireGameManagerReto4()   ? 1 : 0;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "IntegratedDemo configurada",
            $"Se completaron {steps} de 6 pasos.\n\n" +
            "• Robot Explorador    (ExplorerAvatar)\n" +
            "• Robot Técnico       (idle)\n" +
            "• TechnicianWorkstation + HUD\n" +
            "• Zona Reto 4         (Arduino)\n" +
            "• Tags RightHand / LeftHand\n" +
            "• GameManager → Reto4Zone\n\n" +
            "Abre la escena en Play Mode para verificar.",
            "OK");

        Debug.Log($"[IntegratedDemoSetup] Setup completo — {steps}/6 pasos ejecutados.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  1. Robot del Explorador
    // ─────────────────────────────────────────────────────────────────

    static bool SetupExplorerRobot()
    {
        const string LABEL = "Robot Explorador";

        // Ya existe → skip
        if (GameObject.Find("RobotKyle_Explorer") != null)
        { Debug.Log($"[Setup] {LABEL}: ya existe."); return false; }

        // Buscar el XR Origin
        var xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin == null) xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin == null)
        { Debug.LogWarning($"[Setup] {LABEL}: no se encontró XR Origin."); return false; }

        // Buscar Main Camera dentro del XR Origin
        var mainCam = xrOrigin.GetComponentInChildren<Camera>();
        if (mainCam == null)
        { Debug.LogWarning($"[Setup] {LABEL}: no se encontró Camera en XR Origin."); return false; }

        // Buscar el CharacterController (en Player_Logica o en el XR Origin)
        var ccHost = GameObject.Find("Player_Logica") ?? xrOrigin;
        var cc     = ccHost.GetComponent<CharacterController>();
        if (cc == null) cc = ccHost.AddComponent<CharacterController>();

        // Instanciar el robot como hijo del host
        var kyle = InstantiateKyle(ccHost, "RobotKyle_Explorer",
                                    new Vector3(0f, 0f, 0f),
                                    Quaternion.identity);
        if (kyle == null) return false;

        // Añadir ExplorerAvatar si no lo tiene ya
        var avatar = ccHost.GetComponent<ExplorerAvatar>();
        if (avatar == null) avatar = ccHost.AddComponent<ExplorerAvatar>();

        avatar.xrCamera   = mainCam.transform;
        avatar.avatarRoot = kyle.transform;
        avatar.hideHeadInVR = true;

        Debug.Log($"[Setup] {LABEL}: OK  (host={ccHost.name})");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    //  2. Robot del Técnico
    // ─────────────────────────────────────────────────────────────────

    static bool SetupTechnicianRobot()
    {
        const string LABEL = "Robot Técnico";

        if (GameObject.Find("RobotKyle_Technician") != null)
        { Debug.Log($"[Setup] {LABEL}: ya existe."); return false; }

        // Buscar el entorno del Técnico
        var techEnv = GameObject.Find("Entorno del Técnico")
                   ?? GameObject.Find("TechnicianCamera")
                   ?? GameObject.Find("Mesa_Trabajo");
        if (techEnv == null)
        { Debug.LogWarning($"[Setup] {LABEL}: no se encontró entorno del Técnico."); return false; }

        // Posición relativa a la cámara del Técnico
        var techCam = techEnv.GetComponentInChildren<Camera>();
        Vector3 spawnPos = techCam != null
            ? techCam.transform.position + techCam.transform.forward * 1.2f
            : techEnv.transform.position + Vector3.forward * 0.5f;
        spawnPos.y = 0f;

        var kyle = InstantiateKyle(techEnv, "RobotKyle_Technician",
                                    techEnv.transform.InverseTransformPoint(spawnPos),
                                    Quaternion.Euler(0f, 180f, 0f));
        if (kyle == null) return false;

        // Poner el robot en idle (Speed = 0)
        var anim = kyle.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetFloat("Speed",       0f);
            anim.SetFloat("MotionSpeed", 0f);
        }

        Debug.Log($"[Setup] {LABEL}: OK  (parent={techEnv.name})");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    //  3. TechnicianWorkstation + MiniHUD + DiagnosticPanel
    // ─────────────────────────────────────────────────────────────────

    static bool SetupTechnicianHUD()
    {
        const string LABEL = "TechnicianWorkstation + HUD";

        // Si ya tiene el script → skip
        if (Object.FindFirstObjectByType<TechnicianWorkstation>() != null)
        { Debug.Log($"[Setup] {LABEL}: ya existe."); return false; }

        // Buscar el host del Técnico
        var host = GameObject.Find("Entorno del Técnico")
                ?? GameObject.Find("TechnicianCamera")
                ?? GameObject.Find("GameSystem");
        if (host == null)
        { Debug.LogWarning($"[Setup] {LABEL}: no se encontró host para TechnicianWorkstation."); return false; }

        // ── TechnicianWorkstation ────────────────────────────────────
        var ws = host.AddComponent<TechnicianWorkstation>();

        // Conectar referencias por búsqueda de nombre
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null) ws.gameManager = gm;

        var trayGO = GameObject.Find("SendingTray");
        if (trayGO != null) ws.sendingTray = trayGO.transform;

        var mesa = GameObject.Find("Mesa_Trabajo") ?? GameObject.Find("Desk_Surface");
        if (mesa != null) ws.deskSurface = mesa.transform;

        // ── MiniHUD (Screen Space Overlay) ───────────────────────────
        var hudGO     = new GameObject("MiniHUD");
        hudGO.transform.SetParent(host.transform, false);

        var hudCanvas = hudGO.AddComponent<Canvas>();
        hudCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 10;
        hudGO.AddComponent<CanvasScaler>();
        hudGO.AddComponent<GraphicRaycaster>();

        // Fondo semitransparente
        var bg    = new GameObject("HUD_Background");
        bg.transform.SetParent(hudGO.transform, false);
        var bgRT  = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f);
        bgRT.anchorMax = new Vector2(0f, 1f);
        bgRT.pivot     = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(8f, -8f);
        bgRT.sizeDelta = new Vector2(330f, 70f);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.88f);

        ws.hudVoltaje   = AddHUDLine(hudGO, "TMP_HUD_Voltaje",
                            "V: — V  |  I: — mA", new Vector2(172f, -22f));
        ws.hudCorriente = AddHUDLine(hudGO, "TMP_HUD_Corriente",
                            "Componentes: —",      new Vector2(172f, -42f));
        ws.hudReto      = AddHUDLine(hudGO, "TMP_HUD_Reto",
                            "RETO — | Tiempo: —s", new Vector2(172f, -60f));

        // ── DiagnosticPanel (World Space frente al técnico) ───────────
        var diagGO = new GameObject("DiagnosticPanel");
        diagGO.transform.SetParent(host.transform, false);

        // Posicionar frente a la cámara del técnico
        var techCam = host.GetComponentInChildren<Camera>();
        if (techCam != null)
        {
            diagGO.transform.position = techCam.transform.position
                                      + techCam.transform.forward * 1.5f
                                      + Vector3.up * 0.1f;
            diagGO.transform.LookAt(techCam.transform);
            diagGO.transform.Rotate(0f, 180f, 0f);
        }

        var diagCanvas = diagGO.AddComponent<Canvas>();
        diagCanvas.renderMode = RenderMode.WorldSpace;
        diagGO.AddComponent<CanvasScaler>();
        diagGO.AddComponent<GraphicRaycaster>();
        diagGO.transform.localScale = Vector3.one * 0.001f;

        var diagRT       = diagGO.GetComponent<RectTransform>();
        diagRT.sizeDelta = new Vector2(360f, 160f);

        // Fondo
        var diagBg  = new GameObject("Background");
        diagBg.transform.SetParent(diagGO.transform, false);
        var diagBgRT = diagBg.AddComponent<RectTransform>();
        diagBgRT.anchorMin = Vector2.zero;
        diagBgRT.anchorMax = Vector2.one;
        diagBgRT.offsetMin = Vector2.zero;
        diagBgRT.offsetMax = Vector2.zero;
        var diagImg = diagBg.AddComponent<Image>();
        diagImg.color = new Color(0.04f, 0.06f, 0.14f, 0.92f);

        ws.txtDiagnostico     = AddWorldTMP(diagGO, "TMP_Diagnostico",
            "Esperando datos del circuito...",
            new Vector2(0f, 24f), new Vector2(340f, 72f), 8f, new Color(0.3f, 1f, 0.5f));
        ws.txtAccionSiguiente = AddWorldTMP(diagGO, "TMP_AccionSiguiente",
            "Acción siguiente...",
            new Vector2(0f, -44f), new Vector2(340f, 38f), 7.5f, new Color(1f, 0.9f, 0.3f));

        ws.txtDiagnostico.alignment     = TextAlignmentOptions.TopLeft;
        ws.txtAccionSiguiente.alignment = TextAlignmentOptions.TopLeft;

        Debug.Log($"[Setup] {LABEL}: OK  (host={host.name})");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    //  4. Zona Reto 4 (Arduino)
    // ─────────────────────────────────────────────────────────────────

    static bool SetupReto4()
    {
        const string LABEL = "Reto 4 (Arduino)";

        if (GameObject.Find("Reto 4") != null)
        { Debug.Log($"[Setup] {LABEL}: ya existe."); return false; }

        // Buscar dónde viven los demás retos
        var reto3 = GameObject.Find("Reto 3");
        Transform parent = reto3 != null ? reto3.transform.parent : null;

        // Calcular posición: junto a Reto 3, desplazado
        Vector3 basePos = reto3 != null
            ? reto3.transform.position + reto3.transform.right * 1.8f
            : new Vector3(3f, 0f, 0f);

        var reto4 = new GameObject("Reto 4");
        if (parent != null)
            reto4.transform.SetParent(parent, false);
        reto4.transform.position = basePos;

        // ── CircuitManager ───────────────────────────────────────────
        var cmGO = new GameObject("CircuitManager_R4");
        cmGO.transform.SetParent(reto4.transform, false);
        var cm   = cmGO.AddComponent<CircuitManager>();
        cm.topology = CircuitTopology.Mixed;

        // ── VoltageSource ────────────────────────────────────────────
        var vsGO = CreateCompGO(reto4, "VoltageSource_R4",
                                new Vector3(0f, 0.1f, -0.3f),
                                PrimitiveType.Cylinder,
                                new Color(0.9f, 0.7f, 0.1f));
        var vs   = vsGO.AddComponent<VoltageSource>();
        vs.voltage = 5f;

        var vsNode = CreateNode(vsGO, "Node_VS_Pos", new Vector3(0f, 0.07f, 0f));
        vs.nodeA   = vsNode;

        // ── ArduinoPin (con falla) ───────────────────────────────────
        var pinGO  = CreateCompGO(reto4, "ArduinoPin_R4",
                                   new Vector3(0f, 0.1f, 0f),
                                   PrimitiveType.Cube,
                                   new Color(0.1f, 0.6f, 0.9f));
        pinGO.transform.localScale = new Vector3(0.08f, 0.04f, 0.06f);
        var pin = pinGO.AddComponent<ArduinoPin>();

        // Hacer el pin interactuable
        pinGO.AddComponent<Rigidbody>().isKinematic = true;
        var pinInteract = pinGO.AddComponent<InteractableResistor>();   // reutilizamos el patrón

        var pinNodeA = CreateNode(pinGO, "Node_Pin_A", new Vector3(-0.06f, 0f, 0f));
        var pinNodeB = CreateNode(pinGO, "Node_Pin_B", new Vector3( 0.06f, 0f, 0f));
        pin.nodeA = pinNodeA;
        pin.nodeB = pinNodeB;

        // ── Resistor (falta la resistencia del buzzer) ───────────────
        var resGO = CreateCompGO(reto4, "Resistor_Buzzer_R4",
                                  new Vector3(0f, 0.1f, 0.25f),
                                  PrimitiveType.Cylinder,
                                  new Color(0.5f, 0.3f, 0.1f));
        var res   = resGO.AddComponent<Resistor>();
        res.faultyResistance  = 0f;
        res.correctResistance = 330f;

        var resNodeA = CreateNode(resGO, "Node_Res_A", new Vector3(-0.05f, 0f, 0f));
        var resNodeB = CreateNode(resGO, "Node_Res_B", new Vector3( 0.05f, 0f, 0f));
        res.nodeA = resNodeA;
        res.nodeB = resNodeB;

        // ── Slot de reparación ───────────────────────────────────────
        var slotGO = new GameObject("Slot_Arduino_R4");
        slotGO.transform.SetParent(reto4.transform, false);
        slotGO.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        var slotCol     = slotGO.AddComponent<BoxCollider>();
        slotCol.size    = new Vector3(0.12f, 0.08f, 0.12f);
        slotCol.isTrigger = true;
        var slot = slotGO.AddComponent<ComponentSlot>();

        // ── Registrar en CircuitManager ──────────────────────────────
        cm.components.Add(vs);
        cm.components.Add(res);

        // ── Label visual ─────────────────────────────────────────────
        var label = new GameObject("Label_R4");
        label.transform.SetParent(reto4.transform, false);
        label.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        label.transform.localScale    = Vector3.one * 0.003f;
        var labelCanvas = label.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        label.AddComponent<CanvasScaler>();
        var labelRT    = label.GetComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(200f, 40f);
        var txt = AddWorldTMP(label, "TMP_Label_R4",
            "RETO 4 — Arduino\nSensor + Buzzer",
            Vector2.zero, new Vector2(190f, 38f), 10f, new Color(0.3f, 0.8f, 1f));
        txt.alignment = TextAlignmentOptions.Center;

        // ── ChallengeTag ─────────────────────────────────────────────
        var tag = reto4.AddComponent<ChallengeTag>();

        Debug.Log($"[Setup] {LABEL}: OK");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    //  5. Tags en los controladores XR
    // ─────────────────────────────────────────────────────────────────

    static bool TagHandControllers()
    {
        const string LABEL = "Tags RightHand / LeftHand";
        bool changed = false;

        // Crear tags si no existen
        EnsureTag("RightHand");
        EnsureTag("LeftHand");

        // Buscar controladores por nombre
        string[] rightNames = { "RightHand Controller", "Right Hand Move",
                                "Right Controller",     "Right Hand" };
        string[] leftNames  = { "LeftHand Controller",  "Left Hand Move",
                                "Left Controller",      "Left Hand" };

        foreach (var n in rightNames)
        {
            var go = GameObject.Find(n);
            if (go != null && go.tag != "RightHand")
            { go.tag = "RightHand"; changed = true;
              Debug.Log($"[Setup] {LABEL}: '{n}' → RightHand"); }
        }
        foreach (var n in leftNames)
        {
            var go = GameObject.Find(n);
            if (go != null && go.tag != "LeftHand")
            { go.tag = "LeftHand"; changed = true;
              Debug.Log($"[Setup] {LABEL}: '{n}' → LeftHand"); }
        }

        if (!changed) Debug.Log($"[Setup] {LABEL}: ya configurados.");
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    //  6. Wiring GameManager → reto4Zone
    // ─────────────────────────────────────────────────────────────────

    static bool WireGameManagerReto4()
    {
        const string LABEL = "GameManager → reto4Zone";

        var gm    = Object.FindFirstObjectByType<GameManager>();
        var reto4 = GameObject.Find("Reto 4");

        if (gm == null || reto4 == null)
        { Debug.LogWarning($"[Setup] {LABEL}: GameManager o Reto4 no encontrado."); return false; }

        if (gm.reto4Zone == reto4)
        { Debug.Log($"[Setup] {LABEL}: ya estaba asignado."); return false; }

        gm.reto4Zone = reto4;
        EditorUtility.SetDirty(gm);
        Debug.Log($"[Setup] {LABEL}: OK");
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────

    static bool EnsureSceneOpen()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains(SCENE_NAME))
        {
            EditorUtility.DisplayDialog("Escena incorrecta",
                $"Abre la escena '{SCENE_NAME}.unity' antes de ejecutar el setup.\n" +
                $"(Escena activa: {scene.name})", "OK");
            return false;
        }
        return true;
    }

    static GameObject InstantiateKyle(GameObject parent, string name,
                                       Vector3 localPos, Quaternion localRot)
    {
        var kylePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KYLE_PATH);
        if (kylePrefab == null)
        {
            Debug.LogWarning($"[Setup] RobotKyle.prefab no encontrado en {KYLE_PATH}. " +
                             "Importa el asset SpaceRobotKyle.");
            return null;
        }
        var kyle = (GameObject)PrefabUtility.InstantiatePrefab(kylePrefab, parent.transform);
        kyle.name                    = name;
        kyle.transform.localPosition = localPos;
        kyle.transform.localRotation = localRot;
        return kyle;
    }

    static GameObject CreateCompGO(GameObject parent, string name,
                                    Vector3 localPos, PrimitiveType mesh, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * 0.06f;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetMesh(mesh);
        var mr = go.AddComponent<MeshRenderer>();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mr.sharedMaterial = mat;

        var col = go.AddComponent<BoxCollider>();
        return go;
    }

    static ElectricalNode CreateNode(GameObject parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.4f;
        return go.AddComponent<ElectricalNode>();
    }

    static TMP_Text AddHUDLine(GameObject parent, string name,
                                string text, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 9f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin         = new Vector2(0f, 1f);
        rt.anchorMax         = new Vector2(0f, 1f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = anchoredPos;
        rt.sizeDelta         = new Vector2(320f, 18f);
        return tmp;
    }

    static TMP_Text AddWorldTMP(GameObject parent, string name, string text,
                                 Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        var rt        = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    static void EnsureTag(string tag)
    {
        SerializedObject  tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>(
                "ProjectSettings/TagManager.asset"));
        SerializedProperty tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[Setup] Tag '{tag}' creado.");
    }

    static Mesh GetMesh(PrimitiveType type)
    {
        var tmp = GameObject.CreatePrimitive(type);
        var m   = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        return m;
    }
}
