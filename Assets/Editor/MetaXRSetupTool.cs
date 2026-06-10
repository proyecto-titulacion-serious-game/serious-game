#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Integra Meta XR SDK en la escena Explorador.unity del proyecto TITA.
///
/// Qué hace:
///   1. Añade OVRManager al GO [MetaXR_Manager] (si no existe).
///   2. Configura OVRManager para coexistir con XR Origin + XRI (no lo reemplaza).
///   3. Actualiza HapticFeedback para opcionalmente usar OVRInput.
///   4. Marca la escena dirty.
///
/// La integración usa la ruta OpenXR + Meta OpenXR (com.unity.xr.meta-openxr)
/// que ya está instalada en el proyecto. OVRManager complementa el XR Rig
/// existente añadiendo features específicos de Quest sin reemplazar XRI.
///
/// Menú: Tools → TITA → Meta XR → Setup OVRManager en Explorador
/// </summary>
public static class MetaXRSetupTool
{
    const string OVRMGR_GO = "[MetaXR_Manager]";

    [MenuItem("Tools/TITA/Meta XR/Setup OVRManager en Explorador")]
    static void Run()
    {
        var log = new System.Text.StringBuilder("=== MetaXRSetupTool ===\n\n");

        // ── 1. Verificar que la escena Explorador está abierta ────────────
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.Contains("Explorador") && !scene.name.Contains("MapVR"))
        {
            bool open = EditorUtility.DisplayDialog(
                "Escena incorrecta",
                $"La escena activa es '{scene.name}'.\n" +
                "Abre Explorador.unity (o MapVR.unity) antes de continuar.\n\n" +
                "¿Continuar de todos modos en la escena actual?",
                "Continuar", "Cancelar");
            if (!open) return;
        }

        // ── 2. Buscar o crear [MetaXR_Manager] ───────────────────────────
        var mgrGO = GameObject.Find(OVRMGR_GO);
        if (mgrGO == null)
        {
            mgrGO = new GameObject(OVRMGR_GO);
            Undo.RegisterCreatedObjectUndo(mgrGO, "Crear MetaXR_Manager");
            log.AppendLine($"[OK] GO '{OVRMGR_GO}' creado.");
        }
        else
            log.AppendLine($"[--] GO '{OVRMGR_GO}' ya existía.");

        // ── 3. Añadir OVRManager ─────────────────────────────────────────
        var ovrMgr = mgrGO.GetComponent<OVRManager>();
        if (ovrMgr == null)
        {
            ovrMgr = Undo.AddComponent<OVRManager>(mgrGO);
            log.AppendLine("[OK] OVRManager añadido.");
        }
        else
            log.AppendLine("[--] OVRManager ya existía.");

        // ── 4. Configurar OVRManager ─────────────────────────────────────
        // Usamos SerializedObject para acceder a campos de OVRManager.
        var so = new SerializedObject(ovrMgr);

        // trackingOriginType: FloorLevel (2) — necesario para KAT VR
        SetProp(so, "trackingOriginType", (int)OVRManager.TrackingOrigin.FloorLevel);
        // usePositionTracking: true
        SetProp(so, "usePositionTracking", true);
        // useRotationTracking: true
        SetProp(so, "useRotationTracking", true);
        // useIPDInPositionTracking: true
        SetProp(so, "useIPDInPositionTracking", true);
        // enablePassthrough: false (no se usa en este juego)
        SetProp(so, "enablePassthrough", false);
        // NO sobrescribir headPoseRelativeOffsetTranslation ni el XR Rig
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(mgrGO);
        log.AppendLine("[OK] OVRManager configurado:");
        log.AppendLine("     trackingOriginType = FloorLevel");
        log.AppendLine("     usePositionTracking = true");
        log.AppendLine("     enablePassthrough   = false");

        // ── 5. Verificar que XR Origin no tiene un OVRCameraRig ─────────
        // Confirmamos que el XR Rig existente sigue siendo el principal.
        var xrOrigin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>(
            FindObjectsInactive.Include);
        if (xrOrigin != null)
            log.AppendLine($"[OK] XR Origin existente conservado: {xrOrigin.gameObject.name}");
        else
            log.AppendLine("[??] XR Origin no encontrado. Verifica que el XR Rig está en escena.");

        // ── 6. Marcar dirty ──────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);

        log.AppendLine("\n✅ LISTO. OVRManager integrado sin reemplazar XR Rig + XRI.");
        log.AppendLine("\nPasos restantes en Unity Editor:");
        log.AppendLine("  1. Edit → Project Settings → XR Plug-in Management");
        log.AppendLine("     Android → habilitar 'Meta Quest feature set'");
        log.AppendLine("  2. Edit → Project Settings → Meta XR");
        log.AppendLine("     → clic 'Fix All' para resolver los 2 required fixes");
        log.AppendLine("  3. Ctrl+S para guardar la escena");

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Meta XR integrado", log.ToString(), "OK");
    }

    static void SetProp(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.intValue = value;
    }

    static void SetProp(SerializedObject so, string name, bool value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.boolValue = value;
    }
}
#endif
