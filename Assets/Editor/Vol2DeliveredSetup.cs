#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Crea prefabs "Delivered" con los modelos 3D del pack "Resources Vol.2 - Electronics".
///
/// PASO 1 (automático): convierte los materiales Standard → URP/Lit.
/// PASO 2: copia el prefab Vol.2 y le agrega los scripts de juego.
///
/// Menú: Tools → TITA → Vol.2 Electronics → Crear Prefabs Delivered V2
///
/// Resultado en Assets/Prefabs/Delivered/:
///   Delivered_LED_V2.prefab        (Led A.prefab)
///   Delivered_Capacitor_V2.prefab  (Capacitor.prefab)
///   Delivered_ArduinoPin_V2.prefab (Controller Board.prefab)
///
/// Resistor: no hay modelo en Vol.2. Usa "Setup Seleccionado Como Resistor"
/// con Potentiometer, Transistor, o cualquier otro prefab del pack.
///
/// DESPUÉS DE GENERAR:
///   Asignar en ComponentDeliverySystem:
///   ledPrefab / capacitorPrefab / arduinoPinPrefab
/// </summary>
public static class Vol2DeliveredSetup
{
    const string V2_PATH  = "Assets/Resources Vol.2 - Electronics/Prefabs/";
    const string MAT_PATH = "Assets/Resources Vol.2 - Electronics/Materials/";
    const string OUT_PATH = "Assets/Prefabs/Delivered/";

    // ─────────────────────────────────────────────
    //  Menu items
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Vol.2 Electronics/Crear Prefabs Delivered V2")]
    public static void CreateAll()
    {
        EnsureFolder();
        Vol2MaterialFixer.FixAllSilent(out _, out _);

        CreateFromVol2("Led A",           "Delivered_LED_V2",
                       ComponentType.LED,        new Vector3(0.04f, 0.04f, 0.04f));
        CreateFromVol2("Capacitor",       "Delivered_Capacitor_V2",
                       ComponentType.Capacitor,  new Vector3(0.03f, 0.08f, 0.03f));
        CreateFromVol2("Controller Board","Delivered_ArduinoPin_V2",
                       ComponentType.ArduinoPin, new Vector3(0.08f, 0.02f, 0.06f));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Vol.2 Prefabs Delivered creados",
            "Materiales convertidos a URP y prefabs guardados en Assets/Prefabs/Delivered/:\n\n" +
            "  ✓ Delivered_LED_V2.prefab\n" +
            "  ✓ Delivered_Capacitor_V2.prefab\n" +
            "  ✓ Delivered_ArduinoPin_V2.prefab\n\n" +
            "NOTA: No hay modelo de Resistor en Vol.2.\n" +
            "Selecciona Potentiometer o Transistor en el Project View y usa:\n" +
            "'Setup Seleccionado Como Resistor'\n\n" +
            "SIGUIENTE PASO:\n" +
            "Asignar en ComponentDeliverySystem →\n" +
            "  ledPrefab / capacitorPrefab / arduinoPinPrefab",
            "OK");
    }

    [MenuItem("Tools/TITA/Vol.2 Electronics/Solo Convertir Materiales a URP")]
    public static void ConvertMaterialsOnly()
    {
        Vol2MaterialFixer.FixAll();
    }

    [MenuItem("Tools/TITA/Vol.2 Electronics/Setup Seleccionado Como Resistor")]
    static void SetupAsResistor()
        => SetupSelected(ComponentType.Resistor, "Delivered_Resistor_V2", new Vector3(0.035f, 0.04f, 0.035f));

    [MenuItem("Tools/TITA/Vol.2 Electronics/Setup Seleccionado Como LED")]
    static void SetupAsLED()
        => SetupSelected(ComponentType.LED, "Delivered_LED_V2_Custom", new Vector3(0.04f, 0.04f, 0.04f));

    [MenuItem("Tools/TITA/Vol.2 Electronics/Setup Seleccionado Como Capacitor")]
    static void SetupAsCapacitor()
        => SetupSelected(ComponentType.Capacitor, "Delivered_Capacitor_V2_Custom", new Vector3(0.03f, 0.08f, 0.03f));

    [MenuItem("Tools/TITA/Vol.2 Electronics/Setup Seleccionado Como ArduinoPin")]
    static void SetupAsArduinoPin()
        => SetupSelected(ComponentType.ArduinoPin, "Delivered_ArduinoPin_V2_Custom", new Vector3(0.08f, 0.02f, 0.06f));

    // ─────────────────────────────────────────────
    //  Conversión de materiales Standard → URP
    // ─────────────────────────────────────────────

    /// <summary>
    /// Convierte todos los materiales de la carpeta Vol.2 de Standard a URP/Lit.
    /// Idem a lo que hace URPMaterialConverter, pero focalizado en la carpeta.
    /// </summary>
    public static void ConvertVol2MaterialsToURP()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[Vol2Setup] No se encontró URP/Lit shader. ¿Está instalado Universal RP?");
            return;
        }

        Shader standard     = Shader.Find("Standard");
        Shader standardSpec = Shader.Find("Standard (Specular setup)");

        string[] guids = AssetDatabase.FindAssets("t:Material",
            new[] { "Assets/Resources Vol.2 - Electronics/Materials" });

        int converted = 0;
        foreach (string guid in guids)
        {
            string   path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader != standard && mat.shader != standardSpec) continue;

            ConvertToURP(mat, urpLit);
            EditorUtility.SetDirty(mat);
            converted++;
        }

        if (converted > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vol2Setup] {converted} material(es) convertidos a URP/Lit.");
        }
        else
        {
            Debug.Log("[Vol2Setup] Materiales ya están en URP o no se encontraron materiales Standard.");
        }
    }

    static void ConvertToURP(Material mat, Shader urpLit)
    {
        Color   baseColor  = GetColor(mat, "_Color",   Color.white);
        Texture mainTex    = GetTex(mat, "_MainTex");
        Texture bumpMap    = GetTex(mat, "_BumpMap");
        float   bumpScale  = GetFloat(mat, "_BumpScale", 1f);
        Texture metallic   = GetTex(mat, "_MetallicGlossMap");
        float   metalVal   = GetFloat(mat, "_Metallic", 0f);
        float   smooth     = GetFloat(mat, "_Glossiness", 0.5f);
        Texture occlusion  = GetTex(mat, "_OcclusionMap");
        Color   emission   = GetColor(mat, "_EmissionColor", Color.black);
        bool    hasEmit    = mat.IsKeywordEnabled("_EMISSION");

        mat.shader = urpLit;

        mat.SetColor("_BaseColor", baseColor);
        if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);

        mat.SetFloat("_WorkflowMode", 1); // Metallic
        mat.SetFloat("_Metallic",     metalVal);
        mat.SetFloat("_Smoothness",   smooth);
        if (metallic != null) mat.SetTexture("_MetallicGlossMap", metallic);

        if (bumpMap != null)
        {
            mat.SetTexture("_BumpMap",   bumpMap);
            mat.SetFloat("_BumpScale",   bumpScale);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (occlusion != null) mat.SetTexture("_OcclusionMap", occlusion);

        if (hasEmit)
        {
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }

        // Opaque surface defaults
        mat.SetFloat("_Surface",   0);
        mat.SetFloat("_AlphaClip", 0);
        mat.SetInt("_ZWrite",      1);
        mat.renderQueue = (int)RenderQueue.Geometry;
    }

    // ─────────────────────────────────────────────
    //  Core — creación de prefabs
    // ─────────────────────────────────────────────

    static void SetupSelected(ComponentType type, string outputName, Vector3 scale)
    {
        var sel = Selection.activeObject as GameObject;
        if (sel == null)
        {
            EditorUtility.DisplayDialog("Vol.2 Setup",
                "Selecciona un prefab (.prefab) en el Project View primero.", "OK");
            return;
        }
        string assetPath = AssetDatabase.GetAssetPath(sel);
        if (!assetPath.EndsWith(".prefab"))
        {
            EditorUtility.DisplayDialog("Vol.2 Setup",
                "El objeto seleccionado no es un archivo .prefab.", "OK");
            return;
        }
        EnsureFolder();
        Vol2MaterialFixer.FixAllSilent(out _, out _);
        BuildDeliveredPrefab(assetPath, outputName, type, scale);
        AssetDatabase.Refresh();
    }

    static void CreateFromVol2(string sourceName, string outputName,
                               ComponentType type, Vector3 scale)
    {
        string srcPath = V2_PATH + sourceName + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(srcPath) == null)
        {
            Debug.LogWarning($"[Vol2Setup] No se encontró: {srcPath}  — omitido.");
            return;
        }
        BuildDeliveredPrefab(srcPath, outputName, type, scale);
    }

    /// <summary>
    /// Copia el prefab Vol.2 íntegro (AssetDatabase.CopyAsset preserva TODOS los
    /// materiales y sus texturas), luego abre la copia en modo aislado y añade
    /// los scripts de juego sin afectar el prefab original.
    /// </summary>
    static void BuildDeliveredPrefab(string srcPath, string outputName,
                                     ComponentType type, Vector3 scale)
    {
        string outputPath = OUT_PATH + outputName + ".prefab";

        if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"¿Sobreescribir {outputName}.prefab?",
                "Sí, sobreescribir", "Cancelar");
            if (!overwrite) return;
            AssetDatabase.DeleteAsset(outputPath);
        }

        // Copia exacta del prefab origen — preserva materiales, texturas, meshes
        if (!AssetDatabase.CopyAsset(srcPath, outputPath))
        {
            Debug.LogError($"[Vol2Setup] No se pudo copiar {srcPath} a {outputPath}");
            return;
        }
        AssetDatabase.SaveAssets();

        // Abrir la copia en modo aislado para modificarla sin alterar el original
        var go = PrefabUtility.LoadPrefabContents(outputPath);
        if (go == null)
        {
            Debug.LogError($"[Vol2Setup] LoadPrefabContents falló: {outputPath}");
            return;
        }

        go.name = outputName;
        go.transform.localScale = scale;

        // MeshCollider debe ser convex cuando hay Rigidbody
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>())
        {
            mc.convex    = true;
            mc.isTrigger = false;
        }
        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>().isTrigger = false;

        // Rigidbody
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // XRGrabInteractable
        var grab = go.GetComponent<XRGrabInteractable>() ?? go.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        // GrabbableComponent
        if (go.GetComponent<GrabbableComponent>() == null)
            go.AddComponent<GrabbableComponent>();

        // Script eléctrico — necesario para ComponentSlot.DetectComponentType
        AddElectricalScript(go, type);

        // Guardar y descargar
        PrefabUtility.SaveAsPrefabAsset(go, outputPath);
        PrefabUtility.UnloadPrefabContents(go);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (prefab != null)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Vol2Setup] ✓ {outputName}.prefab guardado → {outputPath}");
        }
        else
            Debug.LogError($"[Vol2Setup] ✗ Error guardando {outputName}.prefab");
    }

    // ─────────────────────────────────────────────
    //  Script eléctrico por tipo
    // ─────────────────────────────────────────────

    static void AddElectricalScript(GameObject go, ComponentType type)
    {
        switch (type)
        {
            case ComponentType.Resistor:
                var r = go.AddComponent<Resistor>();
                r.resistance = 100f;
                r.hasFault   = false;
                break;

            case ComponentType.LED:
                // LED requiere Renderer — los prefabs Vol.2 ya lo tienen en root
                var led = go.AddComponent<LED>();
                led.polarityInverted = false;
                break;

            case ComponentType.Capacitor:
                // Capacitor requiere Renderer — ídem
                var cap = go.AddComponent<Capacitor>();
                cap.polarityInverted = false;
                break;

            case ComponentType.ArduinoPin:
                var pin = go.AddComponent<ArduinoPin>();
                pin.pinNumber = 4;
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(OUT_PATH.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Delivered");
    }

    static Color   GetColor(Material m, string p, Color   d) => m.HasProperty(p) ? m.GetColor(p)   : d;
    static float   GetFloat(Material m, string p, float   d) => m.HasProperty(p) ? m.GetFloat(p)   : d;
    static Texture GetTex  (Material m, string p)            => m.HasProperty(p) ? m.GetTexture(p) : null;
}
#endif
