using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Configura en un solo click el modo VR estático para el Técnico.
///
/// Menu: Tools → TITA → Setup VR Técnico (Meta Quest)
///
/// Qué hace:
///   1. Encuentra o crea XRInteractionManager
///   2. Localiza el XROrigin en la escena (o pide al usuario que lo cree)
///   3. Elimina providers de locomoción (Técnico no se mueve)
///   4. Asegura XRRayInteractor en la mano derecha para apuntar al canvas
///   5. Añade TrackedDeviceGraphicRaycaster en todos los canvases WorldSpace
///   6. Cablea referencias en TechnicianController
///
/// REQUISITO PREVIO:
///   Si no hay XROrigin en escena, ve a:
///   GameObject → XR → XR Origin (VR) — o arrastra el prefab desde:
///   Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/
/// </summary>
public static class TechnicianVRSetupTool
{
    static readonly string[] StarterAssetPrefabPaths =
    {
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab",
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin.prefab",
        "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab",
    };

    [MenuItem("Tools/TITA/Setup VR Técnico (Meta Quest)")]
    static void Run()
    {
        var log     = new StringBuilder();
        bool changed = false;

        // ── 1. TechnicianController ──────────────────────────────────────────
        var tc = Object.FindAnyObjectByType<TechnicianController>(FindObjectsInactive.Include);
        if (tc == null)
        {
            EditorUtility.DisplayDialog("Setup VR Técnico",
                "No se encontró TechnicianController en la escena.\n\n" +
                "Abre la escena del Técnico primero.", "OK");
            return;
        }
        log.AppendLine($"[TechnicianController en '{tc.gameObject.name}']");

        // ── 2. XRInteractionManager ──────────────────────────────────────────
        var xrManager = Object.FindAnyObjectByType<XRInteractionManager>(FindObjectsInactive.Include);
        if (xrManager == null)
        {
            var go = new GameObject("XR Interaction Manager");
            Undo.RegisterCreatedObjectUndo(go, "Setup VR Técnico");
            Undo.AddComponent<XRInteractionManager>(go);
            log.AppendLine("  ✓ XRInteractionManager creado");
            changed = true;
        }
        else log.AppendLine($"  — XRInteractionManager: '{xrManager.name}'");

        // ── 3. XROrigin ──────────────────────────────────────────────────────
        GameObject xrOriginGO = tc.xrOriginTechnician;

        if (xrOriginGO == null)
        {
            var existing = Object.FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (existing != null)
            {
                xrOriginGO = existing.gameObject;
                log.AppendLine($"  — XROrigin encontrado en escena: '{xrOriginGO.name}'");
            }
            else
            {
                xrOriginGO = TryInstantiateStarterPrefab(log);
                if (xrOriginGO == null)
                {
                    EditorUtility.DisplayDialog("Setup VR Técnico",
                        "No se encontró un XR Origin en la escena.\n\n" +
                        "Crea uno primero:\n" +
                        "  GameObject → XR → XR Origin (VR)\n\n" +
                        "O arrastra el prefab desde:\n" +
                        "  Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/\n\n" +
                        "Luego vuelve a correr este tool.", "OK");
                    return;
                }
                xrOriginGO.name = "XROrigin_Tecnico";
                log.AppendLine($"  ✓ XROrigin instanciado desde Starter Assets");
                changed = true;
            }
        }
        else log.AppendLine($"  — xrOriginTechnician ya asignado: '{xrOriginGO.name}'");

        if (xrOriginGO.GetComponent<XROrigin>() == null)
        {
            ShowResult(log, changed, tc,
                "El GO no tiene componente XROrigin. Usa un prefab válido de XRI.");
            return;
        }

        // ── 4. Eliminar locomoción (Técnico es estático) ─────────────────────
        int locomotionRemoved = 0;
        foreach (var c in xrOriginGO.GetComponentsInChildren<ContinuousMoveProvider>(true))
            { Undo.DestroyObjectImmediate(c); locomotionRemoved++; changed = true; }
        foreach (var c in xrOriginGO.GetComponentsInChildren<ContinuousTurnProvider>(true))
            { Undo.DestroyObjectImmediate(c); locomotionRemoved++; changed = true; }
        foreach (var c in xrOriginGO.GetComponentsInChildren<SnapTurnProvider>(true))
            { Undo.DestroyObjectImmediate(c); locomotionRemoved++; changed = true; }

        log.AppendLine(locomotionRemoved > 0
            ? $"  ✓ {locomotionRemoved} provider(s) de locomoción eliminados"
            : "  — Sin locomoción (correcto para Técnico estático)");

        // ── 5. Mano derecha con XRRayInteractor ──────────────────────────────
        GameObject rightHandGO = tc.rightHandVR;

        if (rightHandGO == null)
            rightHandGO = FindChildContaining(xrOriginGO.transform,
                "righthand", "right hand", "right controller", "rightcontroller");

        if (rightHandGO == null)
        {
            Transform parent = xrOriginGO.transform.Find("Camera Offset") ?? xrOriginGO.transform;
            rightHandGO = new GameObject("RightHand Controller (Tecnico)");
            Undo.RegisterCreatedObjectUndo(rightHandGO, "Setup VR Técnico");
            Undo.SetTransformParent(rightHandGO.transform, parent, "Setup VR Técnico");
            log.AppendLine("  ✓ RightHand Controller creado");
            log.AppendLine("    ⚠ Asigna Input Actions en ActionBasedController (posición/rotación del controlador).");
            changed = true;
        }
        else log.AppendLine($"  — Mano derecha: '{rightHandGO.name}'");

        var ray = rightHandGO.GetComponent<XRRayInteractor>();
        if (ray == null)
        {
            Undo.AddComponent<XRRayInteractor>(rightHandGO);
            // Undo.AddComponent puede devolver null cuando Unity añade
            // dependencias en cadena (LineRenderer, etc.); GetComponent es fiable.
            ray = rightHandGO.GetComponent<XRRayInteractor>();
            if (ray != null)
            {
                log.AppendLine("  ✓ XRRayInteractor añadido");
                changed = true;
            }
            else
            {
                log.AppendLine("  ✗ No se pudo añadir XRRayInteractor — añádelo manualmente a la mano derecha.");
            }
        }
        else log.AppendLine("  — XRRayInteractor ya existe");

        if (ray != null)
            ray.enableUIInteraction = true;

        // ── 6. TrackedDeviceGraphicRaycaster en canvases WorldSpace ──────────
        int raycasterAdded = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;
            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() != null) continue;
            Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
            raycasterAdded++;
            changed = true;
        }
        log.AppendLine(raycasterAdded > 0
            ? $"  ✓ TrackedDeviceGraphicRaycaster añadido a {raycasterAdded} canvas(es)"
            : "  — TrackedDeviceGraphicRaycaster ya configurado");

        // ── 7. Cablear TechnicianController ──────────────────────────────────
        Undo.RecordObject(tc, "Setup VR Técnico");
        bool tcDirty = false;

        if (tc.xrOriginTechnician != xrOriginGO)
            { tc.xrOriginTechnician = xrOriginGO; tcDirty = true; }
        if (tc.rightHandVR != rightHandGO)
            { tc.rightHandVR = rightHandGO; tcDirty = true; }
        if (tc.mode != TechnicianMode.Auto)
            { tc.mode = TechnicianMode.Auto; tcDirty = true; }
        if (tc.forcePCMode)
            { tc.forcePCMode = false; tcDirty = true; }

        if (tcDirty)
        {
            EditorUtility.SetDirty(tc);
            log.AppendLine("  ✓ TechnicianController: xrOriginTechnician, rightHandVR, mode=Auto");
            changed = true;
        }
        else log.AppendLine("  — TechnicianController ya estaba configurado");

        ShowResult(log, changed, tc);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static void ShowResult(StringBuilder log, bool changed, TechnicianController tc,
        string error = null)
    {
        log.AppendLine();
        if (error != null)
        {
            log.AppendLine($"✗ ERROR: {error}");
        }
        else
        {
            log.AppendLine(changed
                ? "✓ Setup completado — guarda la escena (Ctrl+S)."
                : "— Sin cambios necesarios.");
            log.AppendLine();
            log.AppendLine("FLUJO DE PRUEBA:");
            log.AppendLine("1. Conecta el Quest por Link o AirLink.");
            log.AppendLine("2. En ConnectionManager → rolAutomatico = Tecnico.");
            log.AppendLine("3. En TechnicianController → desactivarXRSiEsPC = true (ya está).");
            log.AppendLine("4. Dale Play — detecta el HMD y activa modo VR automáticamente.");
            log.AppendLine("5. El canvas aparece frente al visor; usa el trigger derecho para los botones.");
        }

        if (changed && tc != null)
            EditorSceneManager.MarkSceneDirty(tc.gameObject.scene);

        EditorUtility.DisplayDialog("Setup VR Técnico", log.ToString(), "OK");
    }

    static GameObject TryInstantiateStarterPrefab(StringBuilder log)
    {
        foreach (var path in StarterAssetPrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Setup VR Técnico");
            log.AppendLine($"  ✓ Prefab cargado: {path}");
            return go;
        }
        return null;
    }

    static GameObject FindChildContaining(Transform root, params string[] keywords)
    {
        foreach (Transform child in root)
        {
            string nameLower = child.name.ToLowerInvariant();
            foreach (var kw in keywords)
                if (nameLower.Contains(kw)) return child.gameObject;

            var found = FindChildContaining(child, keywords);
            if (found != null) return found;
        }
        return null;
    }
}
