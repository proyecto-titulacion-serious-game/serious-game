#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea Walker_PC en la escena NoonA y configura la jerarquía correcta:
///   Walker_PC (TechnicianMover + CharacterController)
///     └─ TechnicianRobot  (se hace hijo de Walker_PC)
///   WalkerCamera           (se desparentea al raíz de la escena)
///
/// Menú: Tools → TITA → Configurar Walker_PC (Técnico)
/// </summary>
public static class WalkerPCSetupTool
{
    [MenuItem("Tools/TITA/Configurar Walker_PC (Técnico)")]
    static void Setup()
    {
        var robot = GameObject.Find("TechnicianRobot");
        if (robot == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'TechnicianRobot' en la escena activa.\n" +
                "Abre la escena NoonA y vuelve a ejecutar esta herramienta.", "OK");
            return;
        }

        // ── 1. Crear Walker_PC ────────────────────────────────────────────────
        var existing = GameObject.Find("Walker_PC");
        GameObject walkerGO;

        if (existing != null)
        {
            walkerGO = existing;
            Debug.Log("[WalkerPCSetup] Walker_PC ya existe — reutilizando.");
        }
        else
        {
            walkerGO = new GameObject("Walker_PC");
            Undo.RegisterCreatedObjectUndo(walkerGO, "Create Walker_PC");
        }

        // Posicionar al mismo lugar que el robot
        Vector3 robotPos = robot.transform.position;
        walkerGO.transform.position = robotPos;
        walkerGO.transform.rotation = robot.transform.rotation;

        // ── 2. TechnicianMover (agrega CharacterController automáticamente) ───
        if (walkerGO.GetComponent<TechnicianMover>() == null)
            Undo.AddComponent<TechnicianMover>(walkerGO);

        // CharacterController se añade por [RequireComponent]; configurar su cápsula
        var cc = walkerGO.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
        }

        // ── 3. TechnicianRobot → hijo de Walker_PC ───────────────────────────
        if (robot.transform.parent != walkerGO.transform)
        {
            Undo.SetTransformParent(robot.transform, walkerGO.transform, "Parent Robot to Walker_PC");
            robot.transform.localPosition = Vector3.zero;
            robot.transform.localRotation = Quaternion.identity;
        }

        // ── 4. WalkerCamera → desparentar al raíz de la escena ───────────────
        var walkerCamGO = GameObject.Find("WalkerCamera");
        if (walkerCamGO != null && walkerCamGO.transform.parent != null)
        {
            Undo.SetTransformParent(walkerCamGO.transform, null, "Unparent WalkerCamera");
            Debug.Log("[WalkerPCSetup] WalkerCamera desparentada al raíz de la escena.");
        }

        // ── 5. Asignar target en ThirdPersonCamera ────────────────────────────
        if (walkerCamGO != null)
        {
            var tpc = walkerCamGO.GetComponent<ThirdPersonCamera>();
            if (tpc != null)
            {
                Undo.RecordObject(tpc, "Assign Walker_PC target");
                tpc.target = walkerGO.transform;
                EditorUtility.SetDirty(tpc);
                Debug.Log("[WalkerPCSetup] ThirdPersonCamera.target → Walker_PC.");
            }
        }

        // ── 6. Marcar escena dirty ────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Walker_PC configurado",
            "✓ Walker_PC creado con TechnicianMover + CharacterController\n" +
            "✓ TechnicianRobot → hijo de Walker_PC\n" +
            "✓ WalkerCamera desparentada al raíz\n" +
            "✓ ThirdPersonCamera.target asignado\n\n" +
            "PRÓXIMOS PASOS:\n" +
            "1. Posiciona Walker_PC dentro de la habitación Japan Office\n" +
            "2. Guarda la escena (Ctrl+S)",
            "OK");
    }
}
#endif
