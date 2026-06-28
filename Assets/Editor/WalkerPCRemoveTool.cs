using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Quita el mover DUPLICADO del Técnico: borra el componente WalkerPC (basado en Rigidbody) y
/// su Rigidbody del rig, y deshabilita el CapsuleCollider redundante, dejando SOLO TechnicianMover
/// (CharacterController) como único controlador de movimiento.
///
///   Tools → TITA → Técnico → Quitar WalkerPC (dejar solo TechnicianMover)
///
/// El CharacterController ya aporta la cápsula de colisión; un Rigidbody no-kinemático + un
/// CapsuleCollider extra causaban deriva ("W se va inclinando a la derecha"). No borra el SCRIPT
/// WalkerPC.cs (sigue en el proyecto), solo el componente del rig. Abre NoonA.unity antes de correr.
/// </summary>
public static class WalkerPCRemoveTool
{
    [MenuItem("Tools/TITA/Técnico/Quitar WalkerPC (dejar solo TechnicianMover)")]
    static void Remove()
    {
        var walkers = Object.FindObjectsByType<WalkerPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (walkers.Length == 0)
        {
            EditorUtility.DisplayDialog("TITA — Quitar WalkerPC",
                "No se encontró ningún WalkerPC en las escenas abiertas.\n\n" +
                "Abre NoonA.unity (donde vive el rig del Técnico) y vuelve a ejecutar.", "OK");
            return;
        }

        int removidos = 0, rbs = 0, cols = 0, sinMover = 0;
        UnityEngine.SceneManagement.Scene scene = default;

        foreach (var w in walkers)
        {
            var go = w.gameObject;
            scene = go.scene;

            // Aviso si el GO no tiene TechnicianMover (no debería quedar sin mover).
            var mover = go.GetComponent<TechnicianMover>();
            if (mover == null)
            {
                Debug.LogWarning($"[WalkerPCRemove] '{go.name}' no tiene TechnicianMover. " +
                                 "Se quita WalkerPC igual, pero revisa que el rig tenga un mover.");
                sinMover++;
            }

            // 1) WalkerPC primero (sus [RequireComponent] bloquean borrar Rigidbody/CapsuleCollider antes).
            Undo.DestroyObjectImmediate(w);
            removidos++;

            // 2) Rigidbody del rig (lo usaba WalkerPC; con CharacterController es innecesario y deriva).
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) { Undo.DestroyObjectImmediate(rb); rbs++; }

            // 3) CapsuleCollider redundante: deshabilitar (reversible) si hay CharacterController.
            var cc  = go.GetComponent<CharacterController>();
            var cap = go.GetComponent<CapsuleCollider>();
            if (cap != null && cc != null)
            {
                Undo.RecordObject(cap, "Deshabilitar CapsuleCollider redundante");
                cap.enabled = false;
                EditorUtility.SetDirty(cap);
                cols++;
            }
        }

        if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("TITA — Quitar WalkerPC",
            $"Listo. Rig del Técnico simplificado.\n\n" +
            $"• WalkerPC borrados: {removidos}\n" +
            $"• Rigidbody borrados: {rbs}\n" +
            $"• CapsuleCollider deshabilitados: {cols}\n" +
            (sinMover > 0 ? $"• ⚠ GO sin TechnicianMover: {sinMover} (revísalos)\n" : "") +
            $"\nQueda SOLO TechnicianMover (CharacterController) como mover.\n" +
            $"GUARDA la escena (Ctrl+S).", "OK");

        Debug.Log($"[WalkerPCRemove] WalkerPC={removidos}, Rigidbody={rbs}, CapsuleCollider off={cols}.");
    }
}
