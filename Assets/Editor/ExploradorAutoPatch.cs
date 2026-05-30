#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Se ejecuta automáticamente cada vez que Unity recarga los scripts.
/// Detecta si Explorador.unity está activa y reconstruye el CableBox_VR
/// con la jerarquía industrial (Button, Gate, LED, etc.) si todavía es
/// el cubo plano anterior (sin hijos).
///
/// Es idempotente: si el CableBox ya tiene hijos, no hace nada.
[InitializeOnLoad]
static class ExploradorAutoPatch
{
    static ExploradorAutoPatch()
    {
        EditorApplication.delayCall += TryPatch;
    }

    static void TryPatch()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.path.Contains("Explorador")) return;

        var existing = Object.FindAnyObjectByType<CableBoxSpawner>();
        if (existing == null) return;

        // Si ya tiene hijos = prop nuevo → nada que hacer
        if (existing.transform.childCount > 0) return;

        Debug.Log("[ExploradorAutoPatch] CableBox plano detectado → reconstruyendo prop industrial...");
        Rebuild(existing, scene);
    }

    static void Rebuild(CableBoxSpawner old, Scene scene)
    {
        // 1. Asegurar que Cable_Jumper.prefab existe
        var cablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cable_Jumper.prefab");
        if (cablePrefab == null)
            cablePrefab = CableBoxSetupTool.BuildCablePrefab();

        // 2. Guardar posición/padre antes de destruir
        Transform parent   = old.transform.parent;
        Vector3   worldPos = old.transform.position;
        Quaternion worldRot = Quaternion.identity;   // reset rotación del cubo plano

        Undo.DestroyObjectImmediate(old.gameObject);

        // 3. Crear el prop industrial completo
        var newBox = CableBoxSetupTool.BuildBoxGO(cablePrefab);

        // 4. Re-parentar y posicionar
        if (parent != null)
            newBox.transform.SetParent(parent, false);

        newBox.transform.position   = worldPos;
        newBox.transform.rotation   = worldRot;
        newBox.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(newBox, "Rebuild CableBox_VR");

        // 5. Guardar escena
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[ExploradorAutoPatch] CableBox_VR reconstruido y escena guardada. ✅");
    }
}
#endif
