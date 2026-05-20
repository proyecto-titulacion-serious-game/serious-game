using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Genera los prefabs físicos agarrables que el Técnico envía al Explorador.
/// Incluye variantes de color/forma para que cada DeskComponent del Técnico
/// envíe el modelo específico que eligió.
///
/// Menú: Tools → TITA → Generar Prefabs Delivered
/// Resultado: Assets/Prefabs/Delivered/
///
/// Prefabs base (usados por ComponentDeliverySystem como fallback):
///   Delivered_Resistor.prefab   — resistor.fbx
///   Delivered_LED.prefab        — LEDGreen.fbx
///   Delivered_Capacitor.prefab  — capacitorBlue.fbx
///   Delivered_ArduinoPIn.prefab — transistor.fbx
///
/// Variantes (asignar en el campo deliveredPrefab de cada DeskComponent):
///   Resistor  → Delivered_Resistor_Vertical.prefab
///   LED       → Delivered_LED_Green / _Red / _Yellow
///   Capacitor → Delivered_Capacitor_Blue / _Black / _Orange
/// </summary>
public static class DeliveredComponentsGenerator
{
    private const string FOLDER = "Assets/Prefabs/Delivered";

    // ── circuit/models ────────────────────────────────────────────────────────
    private const string CM_BASE     = "Assets/circuit/models";
    private const string CM_TEX      = "Assets/circuit/textures/masterTex.png";
    private const string CM_MAT_PATH = "Assets/Materials/Mat_Circuit.mat";

    private const string FBX_RESISTOR          = CM_BASE + "/resistor.fbx";
    private const string FBX_RESISTOR_VERTICAL = CM_BASE + "/resistorVertical.fbx";
    private const string FBX_LED_GREEN         = CM_BASE + "/LEDGreen.fbx";
    private const string FBX_LED_RED           = CM_BASE + "/LEDRed.fbx";
    private const string FBX_LED_YELLOW        = CM_BASE + "/LEDYellow.fbx";
    private const string FBX_CAP_BLUE          = CM_BASE + "/capacitorBlue.fbx";
    private const string FBX_CAP_BLACK         = CM_BASE + "/capacitorBlack.fbx";
    private const string FBX_CAP_ORANGE        = CM_BASE + "/capacitorOrange.fbx";
    private const string FBX_TRANSISTOR        = CM_BASE + "/transistor.fbx";

    private const float TARGET_SIZE = 0.15f;

    [MenuItem("Tools/TITA/Generar Prefabs Delivered")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(FOLDER))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Delivered");

        var mat = GetCircuitMat();

        // Prefabs base
        CreateResistor(mat);
        CreateLED(mat);
        CreateCapacitor(mat);
        CreateArduinoPin(mat);

        // Variantes
        CreateResistorVariant(mat, FBX_RESISTOR_VERTICAL, "Delivered_Resistor_Vertical");
        CreateLEDVariant(mat, FBX_LED_GREEN,  "Delivered_LED_Green");
        CreateLEDVariant(mat, FBX_LED_RED,    "Delivered_LED_Red");
        CreateLEDVariant(mat, FBX_LED_YELLOW, "Delivered_LED_Yellow");
        CreateCapacitorVariant(mat, FBX_CAP_BLUE,   "Delivered_Capacitor_Blue");
        CreateCapacitorVariant(mat, FBX_CAP_BLACK,  "Delivered_Capacitor_Black");
        CreateCapacitorVariant(mat, FBX_CAP_ORANGE, "Delivered_Capacitor_Orange");

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Prefabs Delivered generados",
            "Generados en Assets/Prefabs/Delivered/:\n\n" +
            "BASE:\n" +
            "  Delivered_Resistor / LED / Capacitor / ArduinoPIn\n\n" +
            "VARIANTES:\n" +
            "  Delivered_Resistor_Vertical\n" +
            "  Delivered_LED_Green / _Red / _Yellow\n" +
            "  Delivered_Capacitor_Blue / _Black / _Orange\n\n" +
            "Asigna la variante en el campo 'deliveredPrefab' de cada DeskComponent.",
            "OK");
    }

    // ── Prefabs base ──────────────────────────────────────────────────────────

    static void CreateResistor(Material mat)
    {
        const string path = FOLDER + "/Delivered_Resistor.prefab";
        if (!ConfirmOverwrite(path, "Delivered_Resistor")) return;

        var go = new GameObject("Delivered_Resistor");
        SetupMesh(go, FBX_RESISTOR, mat, AutoScale(FBX_RESISTOR, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var r        = go.AddComponent<Resistor>();
        r.resistance = 100f;
        r.hasFault   = false;

        SavePrefab(go, path, "Delivered_Resistor");
    }

    static void CreateLED(Material mat)
    {
        const string path = FOLDER + "/Delivered_LED.prefab";
        if (!ConfirmOverwrite(path, "Delivered_LED")) return;

        var go = new GameObject("Delivered_LED");
        SetupMesh(go, FBX_LED_GREEN, mat, AutoScale(FBX_LED_GREEN, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var led              = go.AddComponent<LED>();
        led.polarityInverted = false;

        SavePrefab(go, path, "Delivered_LED");
    }

    static void CreateCapacitor(Material mat)
    {
        const string path = FOLDER + "/Delivered_Capacitor.prefab";
        if (!ConfirmOverwrite(path, "Delivered_Capacitor")) return;

        var go = new GameObject("Delivered_Capacitor");
        SetupMesh(go, FBX_CAP_BLUE, mat, AutoScale(FBX_CAP_BLUE, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var cap              = go.AddComponent<Capacitor>();
        cap.polarityInverted = false;

        SavePrefab(go, path, "Delivered_Capacitor");
    }

    static void CreateArduinoPin(Material mat)
    {
        const string path = FOLDER + "/Delivered_ArduinoPIn.prefab";
        if (!ConfirmOverwrite(path, "Delivered_ArduinoPIn")) return;

        var go = new GameObject("Delivered_ArduinoPIn");
        SetupMesh(go, FBX_TRANSISTOR, mat, AutoScale(FBX_TRANSISTOR, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var pin       = go.AddComponent<ArduinoPin>();
        pin.pinNumber = 4;

        SavePrefab(go, path, "Delivered_ArduinoPIn");
    }

    // ── Variantes ─────────────────────────────────────────────────────────────

    static void CreateResistorVariant(Material mat, string fbx, string prefabName)
    {
        string path = $"{FOLDER}/{prefabName}.prefab";
        if (!ConfirmOverwrite(path, prefabName)) return;

        var go = new GameObject(prefabName);
        SetupMesh(go, fbx, mat, AutoScale(fbx, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var r        = go.AddComponent<Resistor>();
        r.resistance = 100f;
        r.hasFault   = false;

        SavePrefab(go, path, prefabName);
    }

    static void CreateLEDVariant(Material mat, string fbx, string prefabName)
    {
        string path = $"{FOLDER}/{prefabName}.prefab";
        if (!ConfirmOverwrite(path, prefabName)) return;

        var go = new GameObject(prefabName);
        SetupMesh(go, fbx, mat, AutoScale(fbx, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var led              = go.AddComponent<LED>();
        led.polarityInverted = false;

        SavePrefab(go, path, prefabName);
    }

    static void CreateCapacitorVariant(Material mat, string fbx, string prefabName)
    {
        string path = $"{FOLDER}/{prefabName}.prefab";
        if (!ConfirmOverwrite(path, prefabName)) return;

        var go = new GameObject(prefabName);
        SetupMesh(go, fbx, mat, AutoScale(fbx, TARGET_SIZE), Vector3.zero);
        SetupGrab(go);

        var cap              = go.AddComponent<Capacitor>();
        cap.polarityInverted = false;

        SavePrefab(go, path, prefabName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Añade MeshFilter + MeshRenderer con el FBX del asset circuit/models.
    /// Fallback a primitiva si el FBX no se encuentra.
    /// El BoxCollider se ajusta automáticamente a los bounds del mesh.
    /// </summary>
    static void SetupMesh(GameObject go, string fbxPath, Material mat,
                          Vector3 scale, Vector3 rotEuler)
    {
        go.transform.localScale    = scale;
        go.transform.localRotation = Quaternion.Euler(rotEuler);

        var mesh = LoadFBXMesh(fbxPath);

        if (mesh != null && mat != null)
        {
            go.AddComponent<MeshFilter>().sharedMesh      = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            // Collider ajustado a los bounds reales del mesh
            var col    = go.AddComponent<BoxCollider>();
            col.center = mesh.bounds.center;
            col.size   = mesh.bounds.size * 1.15f;  // 15% de margen para facilitar agarre
        }
        else
        {
            // Fallback a primitiva
            Debug.LogWarning($"[DeliveredGen] FBX no encontrado: {fbxPath}. Usando primitiva.");
            go.AddComponent<MeshFilter>().sharedMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GetFallbackMat(System.IO.Path.GetFileNameWithoutExtension(fbxPath));

            var col    = go.AddComponent<BoxCollider>();
            col.size   = Vector3.one;
        }
    }

    /// <summary>Añade Rigidbody + XRGrabInteractable + GrabbableComponent.</summary>
    static void SetupGrab(GameObject go)
    {
        var rb         = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var grab          = go.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        go.AddComponent<GrabbableComponent>();
    }

    static Mesh LoadFBXMesh(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is Mesh m) return m;
        return null;
    }

    /// <summary>
    /// Calcula una escala uniforme para que el lado más largo del mesh mida targetSize metros.
    /// Evita depender de constantes fijas que no coinciden con el modelado real del FBX.
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
    /// Devuelve un material URP compatible con masterTex.png del asset circuit/models.
    /// El masterTex.mat original usa un shader no-URP → lo remplazamos con URP/Lit.
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
        Debug.Log($"[DeliveredGen] Material URP creado: {CM_MAT_PATH}");
        return mat;
    }

    static Material GetFallbackMat(string name)
    {
        string path   = $"Assets/Materials/Mat_FB_{name}.mat";
        var existing  = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        var tmp = GameObject.CreatePrimitive(type);
        var m   = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);
        return m;
    }

    static bool ConfirmOverwrite(string path, string name)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return true;
        return EditorUtility.DisplayDialog(
            "Prefab ya existe",
            $"Ya existe:\n{path}\n\n¿Sobreescribir {name}?",
            "Sí, sobreescribir", "Cancelar");
    }

    static void SavePrefab(GameObject root, string path, string label)
    {
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out saved);
        Object.DestroyImmediate(root);
        if (saved)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[DeliveredGen] {label} guardado en {path}");
        }
        else
        {
            Debug.LogError($"[DeliveredGen] Error al guardar {label} en {path}");
        }
    }
}
