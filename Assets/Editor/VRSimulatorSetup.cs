using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// Habilita el XR Device Simulator para probar VR directamente en el Editor de Unity
/// sin necesitar un headset físico. La cámara y los controladores se simulan con el mouse.
///
/// Menu: Tools → TITA → Simulador VR Editor → Habilitar / Deshabilitar
///
/// CONTROLES del simulador (dentro del Play Mode):
///   Boton derecho del mouse (drag)  → Rotar la cabeza (HMD)
///   W A S D                         → Moverse por el mundo
///   Q / E                           → Bajar / Subir
///   Shift (mantener) + mouse        → Mover/rotar el controlador DERECHO
///   Ctrl  (mantener) + mouse        → Mover/rotar el controlador IZQUIERDO
///   G (con Shift/Ctrl)              → Grip del controlador activo
///   T (con Shift/Ctrl)              → Trigger del controlador activo
///   Tab                             → Alternar entre HMD y controladores
/// </summary>
public static class VRSimulatorSetup
{
    private const string SETTINGS_PATH = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    private const string PREFAB_DIR    = "Assets/XRI";
    private const string PREFAB_PATH   = "Assets/XRI/XR Device Simulator.prefab";

    // ─────────────────────────────────────────────
    //  Habilitar
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Simulador VR Editor/Habilitar")]
    static void Enable()
    {
        var simulatorPrefab = FindOrCreateSimulatorPrefab();
        if (simulatorPrefab == null) return;

        var settings = LoadSettings();
        if (settings == null) return;

        var so = new SerializedObject(settings);
        so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
        so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue    = true;
        so.FindProperty("m_SimulatorPrefab").objectReferenceValue              = simulatorPrefab;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Simulador VR habilitado",
            "Al presionar ▶ Play en el Editor podrás simular VR sin headset.\n\n" +
            "CONTROLES:\n" +
            "  Drag (botón derecho)  → Rotar cabeza\n" +
            "  W A S D              → Moverse\n" +
            "  Q / E                → Bajar / Subir\n" +
            "  Shift + mouse        → Controlador derecho\n" +
            "  Ctrl  + mouse        → Controlador izquierdo\n" +
            "  G                    → Grip\n" +
            "  T                    → Trigger\n" +
            "  Tab                  → Cambiar foco (HMD / controlador)\n\n" +
            "Tip: Deshabilítalo antes de hacer el build para el Quest.",
            "OK");

        Debug.Log("[VRSimulatorSetup] Simulador VR habilitado para el Editor.");
    }

    // ─────────────────────────────────────────────
    //  Deshabilitar
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Simulador VR Editor/Deshabilitar")]
    static void Disable()
    {
        var settings = LoadSettings();
        if (settings == null) return;

        var so = new SerializedObject(settings);
        so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = false;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log("[VRSimulatorSetup] Simulador VR deshabilitado. El build para Quest no se ve afectado.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static GameObject FindOrCreateSimulatorPrefab()
    {
        // Prefab ya creado en el proyecto
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (existing != null)
        {
            Debug.Log($"[VRSimulatorSetup] Usando prefab existente: {PREFAB_PATH}");
            return existing;
        }

        // Buscar en los Starter Assets del XRI (si fueron importados)
        var guids = AssetDatabase.FindAssets("XR Device Simulator t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Samples~") && !path.Contains("PackageCache"))
            {
                Debug.Log($"[VRSimulatorSetup] Usando prefab encontrado: {path}");
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        // Crear prefab mínimo con XRDeviceSimulator
        if (!AssetDatabase.IsValidFolder(PREFAB_DIR))
            AssetDatabase.CreateFolder("Assets", "XRI");

        var go = new GameObject("XR Device Simulator");
        go.AddComponent<XRDeviceSimulator>();

        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PREFAB_PATH, out saved);
        Object.DestroyImmediate(go);

        if (!saved || prefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"No se pudo crear el prefab del simulador en:\n{PREFAB_PATH}", "OK");
            return null;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[VRSimulatorSetup] Prefab creado: {PREFAB_PATH}");
        return prefab;
    }

    // Carga XRDeviceSimulatorSettings como ScriptableObject genérico para evitar
    // el error CS0122 (la clase es internal dentro del paquete XRI).
    static ScriptableObject LoadSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SETTINGS_PATH);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró XRDeviceSimulatorSettings en:\n" +
                SETTINGS_PATH + "\n\n" +
                "Verifica que el archivo exista en el proyecto.", "OK");
        }
        return settings;
    }
}
