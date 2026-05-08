using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Genera los 4 prefabs físicos agarrables que el Técnico envía al Explorador.
///
/// Menú: Tools → TITA → Generar Prefabs Delivered
/// Resultado: Assets/Prefabs/Delivered/
///
/// Componentes en cada prefab:
///   MeshFilter + MeshRenderer  — visual distintivo por tipo
///   BoxCollider (no trigger)   — detección física + detección en ComponentSlot
///   Rigidbody  (kinematic)     — física activada al agarrar
///   XRGrabInteractable         — agarre con XRI
///   GrabbableComponent         — ciclo grab/soltar + haptics
///   Script eléctrico           — Resistor / LED / Capacitor / ArduinoPin
///                                (ComponentSlot.DetectComponentType lo usa para identificar el tipo)
///
/// NOTA: LED y Capacitor tienen [RequireComponent(typeof(Renderer))].
/// El MeshRenderer se añade ANTES que el script eléctrico.
///
/// DESPUÉS DE GENERAR, asignar en Inspector:
///   ComponentDeliverySystem  → resistorPrefab, ledPrefab, capacitorPrefab, arduinoPinPrefab
///   ExplorerComponentReceiver → mismos cuatro campos
/// </summary>
public static class DeliveredComponentsGenerator
{
    private const string FOLDER = "Assets/Prefabs/Delivered";

    [MenuItem("Tools/TITA/Generar Prefabs Delivered")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(FOLDER))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Delivered");

        CreateResistor();
        CreateLED();
        CreateCapacitor();
        CreateArduinoPin();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Prefabs Delivered generados",
            "Generados en Assets/Prefabs/Delivered/:\n\n" +
            "  ✓ Delivered_Resistor.prefab   — cápsula beige\n" +
            "  ✓ Delivered_LED.prefab        — esfera verde\n" +
            "  ✓ Delivered_Capacitor.prefab  — cilindro azul\n" +
            "  ✓ Delivered_ArduinoPIn.prefab — cilindro dorado\n\n" +
            "Asignar en ComponentDeliverySystem y ExplorerComponentReceiver.",
            "OK");
    }

    // ── Prefabs ───────────────────────────────────────────────────────────────

    static void CreateResistor()
    {
        const string path = FOLDER + "/Delivered_Resistor.prefab";
        if (!ConfirmOverwrite(path, "Delivered_Resistor")) return;

        var go = new GameObject("Delivered_Resistor");

        // Visual — cápsula horizontal (cuerpo de resistencia real)
        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Capsule);
        go.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_Delivered_Resistor", new Color(0.85f, 0.75f, 0.55f));
        go.transform.localScale = new Vector3(0.03f, 0.06f, 0.03f);

        // Colisión física (no trigger — ComponentSlot la detecta por contacto)
        var col       = go.AddComponent<BoxCollider>();
        col.size      = new Vector3(1f, 1.5f, 1f);
        col.isTrigger = false;

        SetupGrab(go);

        var r        = go.AddComponent<Resistor>();
        r.resistance = 100f;
        r.hasFault   = false;

        SavePrefab(go, path, "Delivered_Resistor");
    }

    static void CreateLED()
    {
        const string path = FOLDER + "/Delivered_LED.prefab";
        if (!ConfirmOverwrite(path, "Delivered_LED")) return;

        var go = new GameObject("Delivered_LED");

        // MeshRenderer ANTES que LED — RequireComponent(typeof(Renderer))
        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Sphere);
        go.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_Delivered_LED", new Color(0.15f, 0.95f, 0.25f));
        go.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

        var col       = go.AddComponent<BoxCollider>();
        col.size      = new Vector3(1.2f, 1.2f, 1.2f);
        col.isTrigger = false;

        SetupGrab(go);

        var led = go.AddComponent<LED>();
        led.polarityInverted = false;

        SavePrefab(go, path, "Delivered_LED");
    }

    static void CreateCapacitor()
    {
        const string path = FOLDER + "/Delivered_Capacitor.prefab";
        if (!ConfirmOverwrite(path, "Delivered_Capacitor")) return;

        var go = new GameObject("Delivered_Capacitor");

        // MeshRenderer ANTES que Capacitor — RequireComponent(typeof(Renderer))
        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Cylinder);
        go.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_Delivered_Capacitor", new Color(0.1f, 0.2f, 0.75f));
        go.transform.localScale = new Vector3(0.03f, 0.06f, 0.03f);

        var col       = go.AddComponent<BoxCollider>();
        col.size      = new Vector3(1.2f, 1f, 1.2f);
        col.isTrigger = false;

        SetupGrab(go);

        var cap = go.AddComponent<Capacitor>();
        cap.polarityInverted = false;

        SavePrefab(go, path, "Delivered_Capacitor");
    }

    static void CreateArduinoPin()
    {
        const string path = FOLDER + "/Delivered_ArduinoPIn.prefab";
        if (!ConfirmOverwrite(path, "Delivered_ArduinoPIn")) return;

        var go = new GameObject("Delivered_ArduinoPIn");

        go.AddComponent<MeshFilter>().sharedMesh = GetMesh(PrimitiveType.Cylinder);
        go.AddComponent<MeshRenderer>().sharedMaterial =
            CreateMat("Mat_Delivered_ArduinoPin", new Color(0.8f, 0.7f, 0.15f));
        go.transform.localScale = new Vector3(0.015f, 0.07f, 0.015f);

        // Collider más ancho que el visual para facilitar el agarre
        var col       = go.AddComponent<BoxCollider>();
        col.size      = new Vector3(2.5f, 1f, 2.5f);
        col.isTrigger = false;

        SetupGrab(go);

        var pin      = go.AddComponent<ArduinoPin>();
        pin.pinNumber = 4;

        SavePrefab(go, path, "Delivered_ArduinoPIn");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Añade Rigidbody + XRGrabInteractable + GrabbableComponent en el orden correcto.
    static void SetupGrab(GameObject go)
    {
        var rb         = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        go.AddComponent<GrabbableComponent>();
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
            Debug.Log($"[DeliveredGenerator] {label} guardado en {path}");
        }
        else
        {
            Debug.LogError($"[DeliveredGenerator] Error al guardar {label} en {path}");
        }
    }

    static Material CreateMat(string matName, Color color)
    {
        string matPath = $"Assets/Materials/{matName}.mat";
        var existing   = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        AssetDatabase.CreateAsset(mat, matPath);
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
