#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Configura la escena del Explorador (Explorador.unity / MapVR.unity) con
/// todos los componentes necesarios para el juego asimétrico.
///
/// Pasos que ejecuta:
///   1. Verificar XR Origin + manos (diagnóstico)
///   2. Configurar OfflineTestSpawner
///   3. Crear/verificar CableBox_VR (Reto 4)
///   4. Crear/verificar ValidationButton_VR (Reto 4)
///   5. Configurar ExplorerComponentReceiver
///   6. Marcar escena dirty
///
/// Menú: Tools → TITA → Setup Escena Explorador
/// </summary>
public static class ExplorerSceneSetup
{
    [MenuItem("Tools/TITA/Setup Escena Explorador")]
    static void Run()
    {
        int created = 0;
        var log = new System.Text.StringBuilder();
        log.AppendLine("=== Setup Escena Explorador ===\n");

        // ── 1. XR Origin (buscar por nombre para evitar dependencias de assembly) ──
        var xrOriginGO = FindGOByName("XR Origin", "XROrigin", "XR Rig", "XRRig");
        if (xrOriginGO != null)
            log.AppendLine("✅ XR Origin: " + xrOriginGO.name);
        else
            log.AppendLine("⚠  XR Origin no encontrado. Ejecuta 'Setup Completo VR Explorador' primero.");

        // ── 2. OfflineTestSpawner ─────────────────────────────────────────
        var spawner = Object.FindAnyObjectByType<OfflineTestSpawner>();
        if (spawner == null)
        {
            var go = new GameObject("OfflineTestSpawner");
            Undo.RegisterCreatedObjectUndo(go, "Crear OfflineTestSpawner");
            spawner = go.AddComponent<OfflineTestSpawner>();
            log.AppendLine("✅ OfflineTestSpawner creado. (spawnKey=T, autoSpawn=false)");
            created++;
        }
        else
            log.AppendLine("✓  OfflineTestSpawner ya existe: " + spawner.name);

        // ── 3. Mesa del Explorador (busca o crea placeholder) ─────────────
        var explorerTable = FindGOByName("Mesa_Explorador", "ExplorerTable", "Workbench_VR", "Mesa_VR");
        if (explorerTable == null)
        {
            explorerTable = new GameObject("Mesa_Explorador");
            Undo.RegisterCreatedObjectUndo(explorerTable, "Crear Mesa Explorador");
            log.AppendLine("✅ Mesa_Explorador creada como placeholder — reposiciónala en escena.");
            created++;
        }
        else
            log.AppendLine("✓  Mesa Explorador: " + explorerTable.name);

        // ── 4. CableBox_VR ────────────────────────────────────────────────
        var cableBox = Object.FindAnyObjectByType<CableBoxSpawner>();
        if (cableBox == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CableBox_VR";
            Undo.RegisterCreatedObjectUndo(go, "Crear CableBox_VR");
            go.transform.SetParent(explorerTable.transform, false);
            go.transform.localScale    = new Vector3(0.10f, 0.06f, 0.08f);
            go.transform.localPosition = new Vector3(-0.12f, 0.03f, 0f);
            go.GetComponent<BoxCollider>().isTrigger = true;
            SetColor(go.GetComponent<Renderer>(), new Color(0.2f, 0.65f, 0.3f));
            go.AddComponent<CableBoxSpawner>();
            go.AddComponent<XRSimpleInteractable>();
            log.AppendLine("✅ CableBox_VR creada. ⚠ Asigna cablePrefab en CableBoxSpawner.");
            created++;
        }
        else
            log.AppendLine("✓  CableBoxSpawner ya existe: " + cableBox.name);

        // ── 5. ValidationButton_VR ────────────────────────────────────────
        var validBtn = Object.FindAnyObjectByType<VRValidationButton>();
        if (validBtn == null)
        {
            var root = new GameObject("ValidationButton_VR");
            Undo.RegisterCreatedObjectUndo(root, "Crear ValidationButton_VR");
            root.transform.SetParent(explorerTable.transform, false);
            root.transform.localPosition = new Vector3(0.15f, 0.03f, 0f);

            var col     = root.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.radius = 0.025f; col.height = 0.05f; col.direction = 1;
            root.AddComponent<XRSimpleInteractable>();

            // Base
            var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGO.name = "Button_Base";
            Undo.RegisterCreatedObjectUndo(baseGO, "Crear Button_Base");
            baseGO.transform.SetParent(root.transform, false);
            baseGO.transform.localScale    = new Vector3(0.05f, 0.012f, 0.05f);
            baseGO.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            Object.DestroyImmediate(baseGO.GetComponent<Collider>());
            SetColor(baseGO.GetComponent<Renderer>(), new Color(0.2f, 0.2f, 0.2f));

            // Capuchón
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Button_Cap";
            Undo.RegisterCreatedObjectUndo(cap, "Crear Button_Cap");
            cap.transform.SetParent(root.transform, false);
            cap.transform.localScale    = new Vector3(0.042f, 0.01f, 0.042f);
            cap.transform.localPosition = new Vector3(0f, 0.022f, 0f);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            SetColor(cap.GetComponent<Renderer>(), new Color(0.1f, 0.4f, 0.9f));

            // LED
            var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "LED_Indicator";
            Undo.RegisterCreatedObjectUndo(led, "Crear LED");
            led.transform.SetParent(cap.transform, false);
            led.transform.localScale    = Vector3.one * 0.15f;
            led.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Object.DestroyImmediate(led.GetComponent<Collider>());

            var btn = root.AddComponent<VRValidationButton>();
            btn.buttonCap   = cap.transform;
            btn.ledRenderer = led.GetComponent<Renderer>();

            var haptics = Object.FindAnyObjectByType<HapticFeedback>();
            if (haptics != null) btn.haptics = haptics;

            log.AppendLine("✅ ValidationButton_VR creado. ⚠ Asigna sfxPress/Pass/Fail en Inspector.");
            created++;
        }
        else
            log.AppendLine("✓  VRValidationButton ya existe: " + validBtn.name);

        // ── 6. ExplorerComponentReceiver ──────────────────────────────────
        var receiver = Object.FindAnyObjectByType<ExplorerComponentReceiver>();
        if (receiver != null)
        {
            log.AppendLine("✓  ExplorerComponentReceiver: " + receiver.name);
            if (receiver.delivery == null)
                log.AppendLine("  ⚠ delivery es null — asignar Bandeja_Recepcion en Inspector.");
        }
        else
            log.AppendLine("⚠  ExplorerComponentReceiver no encontrado (normal si escena usa Fusion).");

        // ── 7. HapticFeedback ─────────────────────────────────────────────
        var haptic = Object.FindAnyObjectByType<HapticFeedback>();
        log.AppendLine(haptic != null
            ? "✓  HapticFeedback: " + haptic.name
            : "⚠  HapticFeedback no encontrado — añádelo al rig del Explorador.");

        // ── Guardar ───────────────────────────────────────────────────────
        if (created > 0)
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine($"\nTotal GOs/referencias creados: {created}");

        EditorUtility.DisplayDialog(
            created > 0 ? $"Setup Explorador — {created} elemento(s) creado(s)" : "Setup Explorador — sin cambios",
            log.ToString(), "OK");

        Debug.Log("[ExplorerSceneSetup]\n" + log);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    static GameObject FindGOByName(params string[] names)
    {
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        foreach (var name in names)
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all)
                if (go.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return go;
        }
        return null;
    }

    static void SetColor(Renderer rend, Color color)
    {
        if (rend == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        rend.sharedMaterial = mat;
    }
}
#endif
