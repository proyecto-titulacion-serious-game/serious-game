using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Limpieza one-shot de cámaras del Técnico (correr con Unity cerrado, por batchmode):
///   Tools → TITA → Técnico → Arreglar cámara (PlayerCameraRoot)   [también vale por menú]
///
/// 1) Tecnico.unity: la ThirdPersonCamera apunta su 'target' al rig de Tecnico y queda anclada a su
///    PlayerCameraRoot; eyeHeight razonable; tag MainCamera; activa; con AudioListener.
/// 2) NoonA.unity: borra la WalkerCamera (cámara duplicada).
/// </summary>
public static class CameraFixBatch
{
    const string TEC = "Assets/Scenes/Tecnico/Tecnico.unity";
    const string NOO = "Assets/Scenes/Tecnico/NoonA.unity";

    [MenuItem("Tools/TITA/Técnico/Arreglar cámara (PlayerCameraRoot)")]
    public static void Run()
    {
        // ── TECNICO ──
        var tec = EditorSceneManager.OpenScene(TEC, OpenSceneMode.Single);
        var rig = Object.FindAnyObjectByType<TechnicianMover>(FindObjectsInactive.Include);
        int n = 0;
        foreach (var tpc in Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include))
        {
            if (rig != null) tpc.target = rig.transform;
            if (tpc.cameraRoot == null && rig != null)
                tpc.cameraRoot = FindChild(rig.transform, "PlayerCameraRoot");
            if (tpc.eyeHeight < 1f) tpc.eyeHeight = 1.65f;

            var go = tpc.gameObject;
            go.tag = "MainCamera";
            go.SetActive(true);
            var cam = go.GetComponent<Camera>(); if (cam) cam.enabled = true;
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            EditorUtility.SetDirty(tpc);
            n++;
            Debug.Log($"[CameraFixBatch] Tecnico TPC '{go.name}': target={(rig ? rig.name : "?")}, " +
                      $"cameraRoot={(tpc.cameraRoot ? tpc.cameraRoot.name : "NULL")}, eyeHeight={tpc.eyeHeight}");
        }
        EditorSceneManager.MarkSceneDirty(tec);
        EditorSceneManager.SaveScene(tec);

        // ── NOONA ──
        var noo = EditorSceneManager.OpenScene(NOO, OpenSceneMode.Single);
        int del = 0;
        foreach (var root in noo.GetRootGameObjects())
            del += DeleteByName(root.transform, "WalkerCamera");
        EditorSceneManager.MarkSceneDirty(noo);
        EditorSceneManager.SaveScene(noo);

        Debug.Log($"[CameraFixBatch] LISTO. Tecnico TPC arregladas={n}; NoonA WalkerCamera borradas={del}.");
    }

    // Entry point para batchmode (-executeMethod CameraFixBatch.Batch).
    public static void Batch()
    {
        Run();
        EditorApplication.Exit(0);
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static int DeleteByName(Transform t, string name)
    {
        var kids = new List<Transform>();
        foreach (Transform k in t) kids.Add(k);
        if (t.name == name) { Object.DestroyImmediate(t.gameObject); return 1; }
        int c = 0;
        foreach (var k in kids) c += DeleteByName(k, name);
        return c;
    }
}
