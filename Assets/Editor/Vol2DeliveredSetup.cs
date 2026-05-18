#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Crea prefabs "Delivered" con los modelos 3D del pack "Resources Vol.2 - Electronics".
///
/// Menú: Tools → TITA → Vol.2 Electronics → Crear Prefabs Delivered V2
///
/// Genera en Assets/Prefabs/Delivered/:
///   Delivered_LED_V2.prefab        (Led A.prefab)
///   Delivered_Capacitor_V2.prefab  (Capacitor.prefab)
///   Delivered_ArduinoPin_V2.prefab (Controller Board.prefab)
///
/// Para el Resistor no hay modelo en Vol.2 — usa "Setup Seleccionado" con
/// cualquier prefab del pack o deja el Delivered_Resistor.prefab existente.
///
/// DESPUÉS DE GENERAR:
///   Asignar los nuevos prefabs en ComponentDeliverySystem →
///   ledPrefab / capacitorPrefab / arduinoPinPrefab
///   (y en ExplorerComponentReceiver si usas modo red).
/// </summary>
public static class Vol2DeliveredSetup
{
    const string V2_PATH  = "Assets/Resources Vol.2 - Electronics/Prefabs/";
    const string OUT_PATH = "Assets/Prefabs/Delivered/";

    // ─────────────────────────────────────────────
    //  Menu items
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Vol.2 Electronics/Crear Prefabs Delivered V2")]
    public static void CreateAll()
    {
        EnsureFolder();

        // Led A → LED component, escala ~4 cm
        CreateFromVol2("Led A",           "Delivered_LED_V2",
                       ComponentType.LED,        new Vector3(0.04f, 0.04f, 0.04f));
        // Capacitor → Capacitor component, escala ~3×8 cm
        CreateFromVol2("Capacitor",       "Delivered_Capacitor_V2",
                       ComponentType.Capacitor,  new Vector3(0.03f, 0.08f, 0.03f));
        // Controller Board → ArduinoPin component, escala ~8×2×6 cm
        CreateFromVol2("Controller Board","Delivered_ArduinoPin_V2",
                       ComponentType.ArduinoPin, new Vector3(0.08f, 0.02f, 0.06f));

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Vol.2 Prefabs Delivered creados",
            "Generados en Assets/Prefabs/Delivered/:\n\n" +
            "  ✓ Delivered_LED_V2.prefab\n" +
            "  ✓ Delivered_Capacitor_V2.prefab\n" +
            "  ✓ Delivered_ArduinoPin_V2.prefab\n\n" +
            "NOTA: No hay modelo de Resistor en Vol.2.\n" +
            "Usa 'Setup Seleccionado Como Resistor' o mantén Delivered_Resistor.prefab.\n\n" +
            "SIGUIENTE PASO:\n" +
            "Asignar en ComponentDeliverySystem →\n" +
            "  ledPrefab / capacitorPrefab / arduinoPinPrefab",
            "OK");
    }

    [MenuItem("Tools/TITA/Vol.2 Electronics/Setup Seleccionado Como Resistor")]
    static void SetupAsResistor()
        => SetupSelected(ComponentType.Resistor, "Delivered_Resistor_V2", new Vector3(0.035f, 0.012f, 0.012f));

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
    //  Core
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
        BuildDeliveredPrefab(sel, outputName, type, scale);
        AssetDatabase.Refresh();
    }

    static void CreateFromVol2(string sourceName, string outputName,
                               ComponentType type, Vector3 scale)
    {
        string srcPath = V2_PATH + sourceName + ".prefab";
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (src == null)
        {
            Debug.LogWarning($"[Vol2Setup] No se encontró: {srcPath}  — omitido.");
            return;
        }
        BuildDeliveredPrefab(src, outputName, type, scale);
    }

    /// <summary>
    /// Crea un nuevo prefab agarrable a partir del modelo fuente.
    /// Copia únicamente la malla y el material; no hereda prefab link.
    /// </summary>
    static void BuildDeliveredPrefab(GameObject source, string outputName,
                                     ComponentType type, Vector3 scale)
    {
        string outputPath = OUT_PATH + outputName + ".prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"¿Sobreescribir {outputName}.prefab?",
                "Sí, sobreescribir", "Cancelar");
            if (!overwrite) return;
        }

        // ── Crear raíz vacía ───────────────────────────────────────────
        var go   = new GameObject(outputName);
        go.transform.localScale = scale;

        // ── Copiar malla y material del source ─────────────────────────
        var srcMf = source.GetComponent<MeshFilter>();
        var srcMr = source.GetComponent<MeshRenderer>();

        if (srcMf != null && srcMr != null)
        {
            go.AddComponent<MeshFilter>().sharedMesh = srcMf.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials   = srcMr.sharedMaterials;
            mr.shadowCastingMode = srcMr.shadowCastingMode;
            mr.receiveShadows    = srcMr.receiveShadows;
        }
        else
        {
            // El source puede tener la malla en un hijo — copiar la jerarquía completa
            // como hijo estático para preservar el aspecto visual.
            var visual = Object.Instantiate(source, go.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale    = Vector3.one;
            // Eliminar scripts del hijo visual para no duplicar lógica
            foreach (var mb in visual.GetComponentsInChildren<MonoBehaviour>())
                Object.DestroyImmediate(mb);
        }

        // ── Collider ───────────────────────────────────────────────────
        // Los prefabs Vol.2 ya traen MeshCollider — ponemos convex para Rigidbody.
        var srcMc = source.GetComponent<MeshCollider>();
        if (srcMc != null)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = srcMc.sharedMesh ?? (srcMf != null ? srcMf.sharedMesh : null);
            mc.convex     = true;   // obligatorio al tener Rigidbody
            mc.isTrigger  = false;
        }
        else
        {
            // Fallback: BoxCollider si no hay MeshCollider
            go.AddComponent<BoxCollider>().isTrigger = false;
        }

        // ── Rigidbody ──────────────────────────────────────────────────
        var rb         = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // ── XRGrabInteractable ─────────────────────────────────────────
        var grab = go.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        // ── GrabbableComponent ─────────────────────────────────────────
        go.AddComponent<GrabbableComponent>();

        // ── Script eléctrico (necesario para ComponentSlot.DetectComponentType) ──
        AddElectricalScript(go, type);

        // ── Guardar prefab ─────────────────────────────────────────────
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, outputPath, out saved);
        Object.DestroyImmediate(go);

        if (saved)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Vol2Setup] ✓ {outputName}.prefab guardado → {outputPath}");
        }
        else
            Debug.LogError($"[Vol2Setup] ✗ Error guardando {outputName}.prefab");
    }

    // ─────────────────────────────────────────────
    //  Electrical script per component type
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
                // LED.cs tiene [RequireComponent(typeof(Renderer))].
                // El MeshRenderer ya fue añadido arriba — AddComponent es seguro.
                var led = go.AddComponent<LED>();
                led.polarityInverted = false;
                break;

            case ComponentType.Capacitor:
                // Igual que LED — Renderer ya presente.
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
}
#endif
