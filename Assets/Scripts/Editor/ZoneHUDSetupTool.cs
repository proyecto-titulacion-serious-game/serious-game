using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea una zona-trigger (BoxCollider + ZoneHUDTrigger) lista para dimensionar sobre la
/// habitación de un reto. Pre-asigna el HUD holográfico del Explorador como objetivo si existe.
/// Duplica la zona (Ctrl+D) y reubícala en cada habitación; en cada una eliges qué targets mostrar.
///
/// Menú: Tools → TITA → Reto 4 → Crear Zona HUD (trigger)
/// </summary>
public static class ZoneHUDSetupTool
{
    [MenuItem("Tools/TITA/Reto 4/Crear Zona HUD (trigger)")]
    public static void Create()
    {
        var go = new GameObject("ZoneHUD_Trigger");
        Undo.RegisterCreatedObjectUndo(go, "Crear Zona HUD");

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3.5f, 3f, 3.5f);   // tamaño de habitación; ajústalo a la zona real

        var zone = go.AddComponent<ZoneHUDTrigger>();

        // Pre-asignar el HUD holográfico del Explorador como objetivo (puedes añadir Clipboard_VR, etc.)
        var hud = Object.FindAnyObjectByType<ExplorerTelemetryHUD>(FindObjectsInactive.Include);
        if (hud != null) zone.targets = new[] { hud.gameObject };

        PositionInFront(go.transform);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[ZoneHUD] Zona creada. Dimensiona el BoxCollider sobre la habitación del reto, " +
                  "arrastra a 'targets' el HUD/Clipboard_VR que quieras, y guarda (Ctrl+S). " +
                  "Duplica (Ctrl+D) para las demás habitaciones." +
                  (hud != null ? " HUD del Explorador pre-asignado." : " (No se encontró ExplorerTelemetryHUD; asigna targets a mano.)"));
    }

    static void PositionInFront(Transform t)
    {
        var cam = GameObject.Find("ExplorerCamera");
        Camera c = cam != null ? cam.GetComponent<Camera>() : Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
        if (c != null)
        {
            Vector3 fwd = c.transform.forward; fwd.y = 0f; fwd.Normalize();
            t.position = c.transform.position + fwd * 1.5f;
        }
        else
        {
            t.position = new Vector3(0f, 1.5f, 0f);
        }
    }
}
