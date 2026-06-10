using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ETAPA 2 del refactor: deja UN solo CharacterController, en el XR Origin (lo estándar en XRI),
/// y hace que PlayerController lo controle. Elimina el CC redundante de Explorer_Player.
///
///   Tools → TITA → Explorador → Consolidar CharacterController (XR Origin)
///
/// Requiere que PlayerController y ExplorerAvatar ya NO tengan [RequireComponent(CharacterController)]
/// (ya quitado por código) y que el proyecto haya recompilado. Reversible con Undo.
/// </summary>
public static class CharacterControllerConsolidateTool
{
    [MenuItem("Tools/TITA/Explorador/Consolidar CharacterController (XR Origin)")]
    static void Consolidate()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (pc == null)
        {
            EditorUtility.DisplayDialog("TITA — CharacterController",
                "No se encontró PlayerController (Explorer_Player). Abre Explorador.unity.", "OK");
            return;
        }

        // Localizar el XR Origin y su CharacterController.
        var xrGo = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
        if (xrGo == null)
        {
            EditorUtility.DisplayDialog("TITA — CharacterController",
                "No se encontró el 'XR Origin (XR Rig)' en la escena.", "OK");
            return;
        }
        var xrCC = xrGo.GetComponent<CharacterController>() ?? xrGo.GetComponentInChildren<CharacterController>(true);
        if (xrCC == null)
        {
            EditorUtility.DisplayDialog("TITA — CharacterController",
                "El XR Origin no tiene CharacterController. Añade uno al XR Origin (XR Rig) primero.", "OK");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (c.name.ToLower().Contains("camera")) { cam = c; break; }

        // Cablear PlayerController al CC del XR Origin.
        Undo.RecordObject(pc, "Consolidar CharacterController");
        pc.characterController = xrCC;
        pc.xrRig = xrGo.transform;
        if (cam != null) pc.headCamera = cam;
        EditorUtility.SetDirty(pc);

        // Quitar el CC redundante de Explorer_Player (el GO de PlayerController).
        string ccMsg;
        var rootCC = pc.GetComponent<CharacterController>();
        if (rootCC != null && rootCC != xrCC)
        {
            Undo.DestroyObjectImmediate(rootCC);
            // Verificar; si algo aún lo requiere (recompila pendiente), desactivarlo como fallback.
            var still = pc.GetComponent<CharacterController>();
            if (still == null)
                ccMsg = "CC redundante de Explorer_Player ELIMINADO ✓";
            else
            {
                still.enabled = false;
                EditorUtility.SetDirty(still);
                ccMsg = "No se pudo eliminar el CC (¿falta recompilar?). Lo DESACTIVÉ como medida temporal.";
            }
        }
        else
        {
            ccMsg = "Explorer_Player ya no tenía CC propio (ok).";
        }

        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Selection.activeGameObject = pc.gameObject;

        EditorUtility.DisplayDialog("TITA — CharacterController",
            "Consolidación aplicada:\n\n" +
            $"• PlayerController.characterController → {xrCC.gameObject.name}\n" +
            $"• PlayerController.xrRig → {xrGo.name}\n" +
            $"• PlayerController.headCamera → {(cam ? cam.name : "— (revisa)")}\n" +
            $"• {ccMsg}\n\n" +
            "Prueba CAMINAR en Play. Si algo falla, Undo (Ctrl+Z) o restaura el backup.\n" +
            "GUARDA la escena (Ctrl+S).", "OK");

        Debug.Log($"[CharacterControllerConsolidateTool] PlayerController→CC del XR Origin ({xrCC.gameObject.name}). {ccMsg}");
    }
}
