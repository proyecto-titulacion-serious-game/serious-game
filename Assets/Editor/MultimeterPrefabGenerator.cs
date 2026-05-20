using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

/// <summary>
/// Genera el prefab completo del multímetro VR con toda la jerarquía,
/// materiales, componentes y referencias ya cableadas.
///
/// Uso: Tools → TITA → Generar Prefab Multímetro
///
/// Resultado: Assets/Prefabs/Multimeter_VR.prefab
/// </summary>
public static class MultimeterPrefabGenerator
{
    private const string PREFAB_PATH = "Assets/Prefabs/Multimeter_VR.prefab";

    [MenuItem("Tools/TITA/Generar Prefab Multímetro")]
    public static void Generate()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"Ya existe un prefab en:\n{PREFAB_PATH}\n\n¿Sobreescribir?",
                "Sí, sobreescribir", "Cancelar");
            if (!overwrite) return;
        }

        // ── Materiales básicos ────────────────────────────────────────
        Material bodyMat       = CreateURPLitMaterial("Multimeter_Body",    new Color(0.15f, 0.15f, 0.15f));
        Material screenMat     = CreateURPLitMaterial("Multimeter_Screen",  new Color(0.05f, 0.12f, 0.05f));
        Material indicRedMat   = CreateURPLitMaterial("Multimeter_ProbeRed",   new Color(0.85f, 0.1f,  0.1f));
        Material indicBlackMat = CreateURPLitMaterial("Multimeter_ProbeBlack", new Color(0.1f,  0.1f,  0.1f));
        Material probeRedMat   = CreateURPLitMaterial("Multimeter_TipRed",     new Color(0.9f,  0.2f,  0.2f));
        Material probeBlackMat = CreateURPLitMaterial("Multimeter_TipBlack",   new Color(0.15f, 0.15f, 0.15f));

        // ── Raíz ─────────────────────────────────────────────────────
        var root = new GameObject("Multimeter_VR");

        // Rigidbody primero — XRGrabInteractable lo requiere y lo busca en Awake
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // XRGrabInteractable
        var grab = root.AddComponent<XRGrabInteractable>();
        grab.movementType            = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition           = true;
        grab.trackRotation           = true;
        grab.throwOnDetach           = false;

        // Collider del cuerpo principal (para el grab)
        var rootCol = root.AddComponent<BoxCollider>();
        rootCol.center = Vector3.zero;
        rootCol.size   = new Vector3(0.06f, 0.12f, 0.02f);

        // ── Body (mesh principal) ─────────────────────────────────────
        var body       = CreateChild(root, "Body");
        var bodyFilter = body.AddComponent<MeshFilter>();
        bodyFilter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        var bodyRenderer = body.AddComponent<MeshRenderer>();
        bodyRenderer.sharedMaterial = bodyMat;
        body.transform.localScale = new Vector3(0.06f, 0.12f, 0.02f);

        // ── Pantalla LCD ──────────────────────────────────────────────
        var screen       = CreateChild(root, "Screen");
        var screenFilter = screen.AddComponent<MeshFilter>();
        screenFilter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        var screenRenderer = screen.AddComponent<MeshRenderer>();
        screenRenderer.sharedMaterial = screenMat;
        screen.transform.localPosition = new Vector3(0f, 0.02f, 0.011f);
        screen.transform.localScale    = new Vector3(0.048f, 0.055f, 0.001f);

        // ── Indicadores de punta (esferas pequeñas) ───────────────────
        var indicRed   = CreateIndicatorSphere(root, "Indicator_Red",   indicRedMat,
                                               new Vector3(-0.012f, 0.055f, 0f));
        var indicBlack = CreateIndicatorSphere(root, "Indicator_Black", indicBlackMat,
                                               new Vector3( 0.012f, 0.055f, 0f));

        // ── Puntas físicas (cables) ───────────────────────────────────
        CreateProbeTip(root, "Probe_Red",   probeRedMat,
                       new Vector3(-0.012f, -0.07f, 0f));
        CreateProbeTip(root, "Probe_Black", probeBlackMat,
                       new Vector3( 0.012f, -0.07f, 0f));

        // ── Canvas World Space ────────────────────────────────────────
        var canvasGO = CreateChild(root, "Screen_Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var canvasRT         = canvasGO.GetComponent<RectTransform>();
        canvasRT.localPosition = new Vector3(0f, 0.02f, 0.012f);
        canvasRT.localRotation = Quaternion.identity;
        canvasRT.localScale    = Vector3.one * 0.001f;
        canvasRT.sizeDelta     = new Vector2(46f, 52f);

        // Textos TMP
        var txtVoltage = CreateTMPLabel(canvasGO, "TMP_Voltage",
            "—.— V",     new Vector2(0f,  14f), 11f, Color.green);
        var txtCurrent = CreateTMPLabel(canvasGO, "TMP_Current",
            "—.— mA",    new Vector2(0f,  2f),  9f,  Color.green);
        var txtStatus  = CreateTMPLabel(canvasGO, "TMP_Status",
            "SIN CONTACTO", new Vector2(0f, -10f), 7f, new Color(0.6f, 1f, 0.6f));
        var txtMode    = CreateTMPLabel(canvasGO, "TMP_Mode",
            "DC VOLTAGE", new Vector2(0f, -20f), 6f, new Color(0.4f, 0.8f, 0.4f));

        // ── Script Multimeter + cableado ──────────────────────────────
        var multimeter = root.AddComponent<Multimeter>();
        multimeter.txtVoltage    = txtVoltage;
        multimeter.txtCurrent    = txtCurrent;
        multimeter.txtStatus     = txtStatus;
        multimeter.txtMode       = txtMode;
        multimeter.indicatorRed   = indicRed.GetComponent<Renderer>();
        multimeter.indicatorBlack = indicBlack.GetComponent<Renderer>();

        // ── Guardar prefab ────────────────────────────────────────────
        bool saved;
        var  prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Listo",
                $"Prefab creado en:\n{PREFAB_PATH}\n\n" +
                "Arrástralo a la escena del Explorador y posiciónalo\n" +
                "sobre la mesa de trabajo.",
                "OK");
            Debug.Log($"[MultimeterPrefabGenerator] Prefab guardado en {PREFAB_PATH}");
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "No se pudo guardar el prefab. Verifica que exista la carpeta Assets/Prefabs/.",
                "OK");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreateIndicatorSphere(GameObject parent, string name,
                                             Material mat, Vector3 localPos)
    {
        var go     = CreateChild(parent, name);
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * 0.007f;
        return go;
    }

    static void CreateProbeTip(GameObject parent, string name,
                                Material mat, Vector3 localPos)
    {
        // Cable (cilindro delgado)
        var cable     = CreateChild(parent, name + "_Cable");
        var cFilter   = cable.AddComponent<MeshFilter>();
        cFilter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
        var cRenderer = cable.AddComponent<MeshRenderer>();
        cRenderer.sharedMaterial = mat;
        cable.transform.localPosition = localPos + Vector3.up * 0.015f;
        cable.transform.localScale    = new Vector3(0.003f, 0.015f, 0.003f);

        // Punta metálica (cono/esfera)
        var tip     = CreateChild(parent, name + "_Tip");
        var tFilter = tip.AddComponent<MeshFilter>();
        tFilter.sharedMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
        var tRenderer = tip.AddComponent<MeshRenderer>();
        tRenderer.sharedMaterial = mat;
        tip.transform.localPosition = localPos;
        tip.transform.localScale    = Vector3.one * 0.005f;

        // Collider de la punta (para futuras interacciones físicas de la sonda)
        var col = tip.AddComponent<SphereCollider>();
        col.radius    = 1f;   // escala local = 0.005, radio real = 0.005 m
        col.isTrigger = true;
    }

    static TMP_Text CreateTMPLabel(GameObject canvasParent, string name,
                                    string defaultText, Vector2 anchoredPos,
                                    float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvasParent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text                = defaultText;
        tmp.fontSize            = fontSize;
        tmp.color               = color;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        var rt          = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(44f, 12f);

        return tmp;
    }

    static Material CreateURPLitMaterial(string matName, Color color)
    {
        // Buscar si ya existe en Assets/Materials
        string path = $"Assets/Materials/{matName}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[MultimeterPrefabGenerator] URP/Lit no encontrado. Usando shader por defecto.");
            urpLit = Shader.Find("Standard");
        }

        var mat = new Material(urpLit);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        var temp = GameObject.CreatePrimitive(type);
        var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        return mesh;
    }
}
