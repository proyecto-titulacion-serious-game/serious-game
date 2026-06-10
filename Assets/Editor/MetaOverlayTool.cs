using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Desactiva (o reactiva) los OVROverlayCanvas de Meta en la escena.
///
///   Tools → TITA → Explorador → Desactivar overlays Meta (OVROverlayCanvas)
///   Tools → TITA → Explorador → Reactivar overlays Meta
///
/// OVROverlayCanvas renderiza canvas world-space como overlay del compositor del Quest y crea
/// una cámara interna por canvas. Al apagar/encender el canvas (p. ej. con el gateo de HUD por
/// zona), esa cámara se destruye y el SDK lanza MissingReferenceException en bucle.
/// Desactivar el componente detiene el error; el canvas se sigue viendo (render normal de UI).
/// </summary>
public static class MetaOverlayTool
{
    [MenuItem("Tools/TITA/Explorador/Desactivar overlays Meta (OVROverlayCanvas)")]
    static void Disable() => SetAll(false);

    [MenuItem("Tools/TITA/Explorador/Reactivar overlays Meta")]
    static void Enable() => SetAll(true);

    static void SetAll(bool on)
    {
        var list = Object.FindObjectsByType<OVROverlayCanvas>(FindObjectsInactive.Include);
        if (list == null || list.Length == 0)
        {
            EditorUtility.DisplayDialog("TITA — Overlays Meta",
                "No se encontró ningún OVROverlayCanvas en la escena.", "OK");
            return;
        }

        int n = 0;
        UnityEngine.SceneManagement.Scene scene = default;
        foreach (var o in list)
        {
            if (o == null || o.enabled == on) continue;
            Undo.RecordObject(o, on ? "Reactivar overlays Meta" : "Desactivar overlays Meta");
            o.enabled = on;
            EditorUtility.SetDirty(o);
            scene = o.gameObject.scene;
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("TITA — Overlays Meta",
            $"{n} OVROverlayCanvas {(on ? "REACTIVADO(s)" : "DESACTIVADO(s)")}.\n\n" +
            (on ? "" : "El error de cámara destruida debería desaparecer. Los canvas siguen visibles.\n") +
            "Guarda la escena (Ctrl+S).", "OK");

        Debug.Log($"[MetaOverlayTool] {n} OVROverlayCanvas → enabled={on}.");
    }
}
