using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea el HUD holográfico de telemetría del Explorador (boceto VR Reto 4) en la escena.
/// Genera el ExplorerTelemetryHUD con sus 3 paneles ya visibles en el Editor y lo posiciona
/// frente a la cámara del Explorador (como los paneles flotantes del boceto). El usuario
/// puede reubicarlo a mano sobre la mesa. Idempotente.
///
/// Menú: Tools → TITA → Reto 4 → HUD Holografico Explorador (VR)
/// </summary>
public static class Reto4ExploradorHUDTool
{
    [MenuItem("Tools/TITA/Reto 4/HUD Holografico Explorador (VR)")]
    public static void Create()
    {
        var existing = Object.FindAnyObjectByType<ExplorerTelemetryHUD>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog("HUD Explorador",
                "Ya existe un ExplorerTelemetryHUD en la escena (seleccionado).", "OK");
            return;
        }

        var go = new GameObject("ExplorerTelemetryHUD");
        Undo.RegisterCreatedObjectUndo(go, "Crear HUD Holografico Explorador");
        var hud = go.AddComponent<ExplorerTelemetryHUD>();
        hud.BuildUI();   // construye los 3 paneles ya visibles/persistentes en el Editor

        PositionInFront(go.transform);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[ExplorerHUD] HUD holografico creado. Ajusta su posicion sobre la mesa y " +
                  "guarda la escena (Ctrl+S). En Play muestra V/I/P/ADC, estado y red reales.");
    }

    /// <summary>Coloca el HUD ~1.8 m frente a la cámara del Explorador, mirándola.</summary>
    static void PositionInFront(Transform t)
    {
        Camera cam = FindExplorerCamera();
        if (cam != null)
        {
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            t.position = cam.transform.position + fwd * 1.8f + Vector3.up * 0.2f;
            t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);
        }
        else
        {
            t.position = new Vector3(0f, 1.5f, 1.8f);   // fallback razonable
            Debug.LogWarning("[ExplorerHUD] No se encontró cámara del Explorador; posición por defecto. Reubícalo a mano.");
        }
    }

    static Camera FindExplorerCamera()
    {
        // Preferir un GameObject llamado "ExplorerCamera"; si no, cualquier cámara de la escena.
        var byName = GameObject.Find("ExplorerCamera");
        if (byName != null)
        {
            var c = byName.GetComponent<Camera>();
            if (c != null) return c;
        }
        return Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
    }
}
