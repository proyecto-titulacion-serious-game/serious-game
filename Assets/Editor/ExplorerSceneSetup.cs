using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura la escena Explorador para funcionar únicamente con VR (Meta Quest 3) o KAT VR.
/// Elimina cualquier componente de control por teclado/mouse y ajusta el PlayerController.
///
/// Menu: Tools → TITA → Configurar Escena Explorador para VR
/// </summary>
public static class ExplorerSceneSetup
{
    [MenuItem("Tools/TITA/Configurar Escena Explorador para VR")]
    static void Configure()
    {
        var scene   = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var log     = new System.Text.StringBuilder();
        bool changed = false;

        // ── 1. Eliminar MouseGrabSimulator ────────────────────────────────────
        var mouseGrabs = Object.FindObjectsByType<MouseGrabSimulator>(FindObjectsSortMode.None);
        foreach (var mg in mouseGrabs)
        {
            log.AppendLine($"  ✓ MouseGrabSimulator eliminado de '{mg.gameObject.name}'");
            Undo.DestroyObjectImmediate(mg);
            changed = true;
        }
        if (mouseGrabs.Length == 0)
            log.AppendLine("  — MouseGrabSimulator: no encontrado en la escena.");

        // ── 2. Configurar PlayerController ────────────────────────────────────
        var controllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (controllers.Length == 0)
        {
            log.AppendLine("  ✗ PlayerController: no encontrado en la escena.");
        }
        else
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Modo de locomoción",
                "¿Qué modo de movimiento usará el Explorador?",
                "Meta Quest 3 (joystick)", "KAT VR (caminadora)", "Cancelar");

            if (choice == 2) return; // Cancelar

            bool useKat = choice == 1;

            foreach (var pc in controllers)
            {
                Undo.RecordObject(pc, "Configurar PlayerController VR");
                pc.useKatVR = useKat;
                EditorUtility.SetDirty(pc);
                changed = true;
                log.AppendLine($"  ✓ PlayerController en '{pc.gameObject.name}' → " +
                               (useKat ? "KAT VR" : "Meta Quest 3 (joystick)"));
            }
        }

        // ── 3. Desactivar cámaras sin TrackedPoseDriver que puedan interferir ─
        int disabledCams = 0;
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            bool isMainVR = cam.CompareTag("MainCamera");
            bool hasTPD   = cam.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>() != null;

            // Si hay una cámara secundaria activa (no VR) que podría renderizar de más
            if (!isMainVR && !hasTPD && cam.enabled && cam.gameObject.activeInHierarchy)
            {
                Undo.RecordObject(cam, "Desactivar cámara no-VR");
                cam.enabled = false;
                EditorUtility.SetDirty(cam);
                changed = true;
                disabledCams++;
                log.AppendLine($"  ✓ Cámara desactivada (no VR): '{cam.gameObject.name}'");
            }
        }
        if (disabledCams == 0)
            log.AppendLine("  — Cámaras: no se encontraron cámaras no-VR activas.");

        // ── 4. Verificar que las manos tienen HandModelController ──────────────
        string[] handNames = { "LeftHand Controller", "LeftHand_Controller",
                               "RightHand Controller", "RightHand_Controller" };
        foreach (var handName in handNames)
        {
            var handGO = GameObject.Find(handName);
            if (handGO == null) continue;

            if (handGO.GetComponent<HandModelController>() == null)
                log.AppendLine($"  ✗ '{handName}' no tiene HandModelController — ejecuta: " +
                               "Tools → TITA → Setup VR Hand Controllers");
            else
                log.AppendLine($"  ✓ '{handName}' tiene HandModelController.");

            if (handGO.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>() == null)
                log.AppendLine($"  ✗ '{handName}' no tiene TrackedPoseDriver — ejecuta: " +
                               "Tools → TITA → Setup VR Hand Controllers");
        }

        // ── Resultado ─────────────────────────────────────────────────────────
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[ExplorerSceneSetup] Configuración aplicada:\n" + log);
            EditorUtility.DisplayDialog(
                "Escena Explorador configurada para VR",
                "Cambios aplicados:\n\n" + log + "\nGuarda la escena con Ctrl+S.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Sin cambios",
                "Revisión completada:\n\n" + log,
                "OK");
        }
    }
}
