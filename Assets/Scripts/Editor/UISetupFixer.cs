using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Diagnostica y repara problemas comunes de UI en el proyecto TITA.
/// Cámaras reconocidas para el rol de Técnico:
///   - Walker_Camera  → cámara de primera persona (NoonA)
///   - Pc_Camera      → cámara del puesto de trabajo (Tecnico scene)
///   - Recep_Camera   → cámara de recepción (NoonA)
/// Menú: Tools → TITA → Reparar UI
/// </summary>
public static class UISetupFixer
{
    [MenuItem("Tools/TITA/Reparar UI (EventSystem + Canvas)")]
    static void FixAll()
    {
        FixEventSystem();
        FixWorldSpaceCanvases();
        Debug.Log("[UISetupFixer] Revisión completa. Ver consola para detalles.");
    }

    // ─── EventSystem ──────────────────────────────────────────────────────────

    static void FixEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            Debug.LogError("[UISetupFixer] No hay EventSystem en la escena. Agrégalo: GameObject → UI → Event System.");
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = es.GetComponent<StandaloneInputModule>();
        var inputSys   = es.GetComponent<InputSystemUIInputModule>();

        if (standalone != null && inputSys == null)
        {
            Undo.RecordObject(es.gameObject, "Fix EventSystem Input Module");
            Object.DestroyImmediate(standalone);
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            EditorUtility.SetDirty(es.gameObject);
            Debug.Log("[UISetupFixer] StandaloneInputModule reemplazado por InputSystemUIInputModule.");
        }
        else if (inputSys != null)
            Debug.Log("[UISetupFixer] EventSystem OK — usa InputSystemUIInputModule.");
        else
            Debug.Log("[UISetupFixer] EventSystem OK — módulo de input ya configurado.");
#else
        Debug.Log("[UISetupFixer] New Input System no activo — StandaloneInputModule es correcto.");
#endif
    }

    // ─── World Space Canvases ─────────────────────────────────────────────────

    static void FixWorldSpaceCanvases()
    {
        // Las únicas cámaras válidas para el rol de Técnico.
        // Walker_Camera no necesita PhysicsRaycaster (solo es para primera persona).
        Camera pcCam    = FindCameraByName("Pc_Camera");
        Camera recepCam = FindCameraByName("Recep_Camera");

        // En el editor asignamos Pc_Camera como valor de diseño por defecto.
        // En runtime WorkstationSeat.SetCanvasCamera() lo sobrescribe al sentarse.
        Camera editorDefault = pcCam ?? recepCam;

        int changes = 0;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;

            if (editorDefault != null && canvas.worldCamera == null)
            {
                Undo.RecordObject(canvas, "Assign Seat Camera to WorldSpace Canvas");
                canvas.worldCamera = editorDefault;
                EditorUtility.SetDirty(canvas);
                Debug.Log($"[UISetupFixer] {editorDefault.name} asignada al canvas WorldSpace '{canvas.gameObject.name}'.");
                changes++;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.RecordObject(canvas.gameObject, "Add GraphicRaycaster");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                EditorUtility.SetDirty(canvas.gameObject);
                Debug.Log($"[UISetupFixer] GraphicRaycaster añadido a '{canvas.gameObject.name}'.");
                changes++;
            }
        }

        if (changes == 0)
            Debug.Log("[UISetupFixer] Canvases WorldSpace OK — no hubo cambios.");

        // PhysicsRaycaster en cada cámara de puesto (necesario para objetos 3D interactivos)
        foreach (var cam in new[] { pcCam, recepCam })
        {
            if (cam == null) continue;

            if (cam.GetComponent<PhysicsRaycaster>() == null)
            {
                Undo.RecordObject(cam.gameObject, "Add PhysicsRaycaster");
                cam.gameObject.AddComponent<PhysicsRaycaster>();
                EditorUtility.SetDirty(cam.gameObject);
                Debug.Log($"[UISetupFixer] PhysicsRaycaster añadido a {cam.name}.");
            }
            else
                Debug.Log($"[UISetupFixer] {cam.name} ya tiene PhysicsRaycaster.");
        }

        if (pcCam == null)
            Debug.LogWarning("[UISetupFixer] 'Pc_Camera' no encontrada. " +
                             "Asegúrate de que la escena Tecnico esté cargada.");
        if (recepCam == null)
            Debug.LogWarning("[UISetupFixer] 'Recep_Camera' no encontrada en la escena activa.");
    }

    static Camera FindCameraByName(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<Camera>() : null;
    }
}
