using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Configura el XR Rig del Explorador para Meta Quest:
///   • Ajusta SphereCollider de manos a radio 0.015 (precisión de pinch)
///   • Corrige handedness del XRDirectInteractor (Left=1, Right=2)
///   • Añade HandPinchGrab a LeftHand y RightHand
///   • Instancia ExplorerHUD.prefab si no existe en la escena
///   • Añade ExplorerHUDFollower al ExplorerHUD (canvas flotante)
///   • Auto-asigna headCamera del XR Origin al follower
///
/// USO: Tools → TITA → Explorador → Setup XR Rig (Manos + HUD + Pinch)
/// Requiere Explorador.unity activa en el Editor.
/// </summary>
public static class ExplorerXRSetup
{
    const string EXPLORER_HUD_PREFAB = "Assets/Prefabs/ExplorerHUD.prefab";

    [MenuItem("Tools/TITA/Explorador/Setup XR Rig (Manos + HUD + Pinch)")]
    static void Setup()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.ToLower().Contains("explorador"))
        {
            bool go = EditorUtility.DisplayDialog(
                "Escena incorrecta",
                $"La escena activa es '{scene.name}'.\nAbre Explorador.unity primero.\n\n¿Continuar de todas formas?",
                "Continuar", "Cancelar");
            if (!go) return;
        }

        int fixes = 0;
        fixes += SetupHands();
        fixes += SetupHUD();
        fixes += SetupGrabbableLayerHint();

        EditorSceneManager.MarkSceneDirty(scene);

        string msg = fixes > 0
            ? $"{fixes} cambios aplicados. Guarda la escena (Ctrl+S).\nRevisa la Consola para el detalle."
            : "El XR Rig ya estaba configurado correctamente.";
        EditorUtility.DisplayDialog("Explorer XR Setup", msg, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MANOS — radio, handedness, HandPinchGrab
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupHands()
    {
        // Identificar manos por HandModelController (solo está en manos del XR Rig)
        var handModels = Object.FindObjectsByType<HandModelController>(
            FindObjectsInactive.Include);

        if (handModels.Length == 0)
        {
            Debug.LogWarning("[XR Setup] No se encontró HandModelController en la escena. " +
                             "Asegúrate de que el XR Rig está en Explorador.unity.");
            return 0;
        }

        int count = 0;
        foreach (var hmc in handModels)
        {
            bool isLeft = hmc.hand == HandModelController.HandSide.Left;
            var go      = hmc.gameObject;
            string side = isLeft ? "Left" : "Right";

            Undo.RecordObject(go, $"Setup {side}Hand XR");
            count += FixSphereCollider(go, side);
            count += FixDirectInteractorHandedness(go, isLeft, side);
            count += AddHandPinchGrab(go, isLeft, side);
        }

        if (count == 0)
            Debug.Log("[XR Setup] Manos: sin cambios (ya configuradas).");

        return count;
    }

    static int FixSphereCollider(GameObject go, string side)
    {
        var col = go.GetComponent<SphereCollider>();
        if (col == null)
        {
            Debug.LogWarning($"[XR Setup] {side}Hand no tiene SphereCollider. Añádelo manualmente.");
            return 0;
        }

        bool changed = false;
        Undo.RecordObject(col, $"Fix {side}Hand SphereCollider");

        if (!Mathf.Approximately(col.radius, 0.015f))
        {
            col.radius  = 0.015f;
            changed = true;
        }

        // Resetear center si está muy desplazado (valores > 0.1 en cualquier eje → bug previo)
        if (col.center.magnitude > 0.1f)
        {
            col.center = Vector3.zero;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(col);
            Debug.Log($"[XR Setup] {side}Hand SphereCollider → radius=0.015, center=(0,0,0)");
        }

        return changed ? 1 : 0;
    }

    static int FixDirectInteractorHandedness(GameObject go, bool isLeft, string side)
    {
        var xrdi = go.GetComponent<XRDirectInteractor>();
        if (xrdi == null) return 0;

        // Handedness enum en XRI: None=0, Left=1, Right=2
        // m_Handedness es un campo serializado privado — accedemos via SerializedObject
        var so = new SerializedObject(xrdi);
        so.Update();
        var handProp = so.FindProperty("m_Handedness");
        if (handProp == null) return 0;

        int correctValue = isLeft ? 1 : 2;
        if (handProp.intValue == correctValue) return 0;

        handProp.intValue = correctValue;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(xrdi);
        Debug.Log($"[XR Setup] {side}Hand XRDirectInteractor.handedness → {(isLeft ? "Left" : "Right")}");
        return 1;
    }

    static int AddHandPinchGrab(GameObject go, bool isLeft, string side)
    {
        if (go.GetComponent<HandPinchGrab>() != null)
        {
            Debug.Log($"[XR Setup] {side}Hand HandPinchGrab ya existe.");
            return 0;
        }

        Undo.RecordObject(go, $"Add HandPinchGrab to {side}Hand");
        var hpg = Undo.AddComponent<HandPinchGrab>(go);
        hpg.handedness = isLeft ? Handedness.Left : Handedness.Right;

        EditorUtility.SetDirty(go);
        Debug.Log($"[XR Setup] {side}Hand HandPinchGrab añadido (handedness={hpg.handedness}).");
        return 1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EXPLORER HUD — instanciar prefab + añadir follower
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupHUD()
    {
        int count = 0;

        // Buscar ExplorerHUD en escena (Canvas con ese nombre)
        var existingHUD = FindCanvasNamed("ExplorerHUD");

        if (existingHUD == null)
        {
            // Instanciar desde prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EXPLORER_HUD_PREFAB);
            if (prefab == null)
            {
                Debug.LogWarning($"[XR Setup] ExplorerHUD.prefab no encontrado en {EXPLORER_HUD_PREFAB}.");
                return 0;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Instanciar ExplorerHUD");
            instance.name = "ExplorerHUD";
            existingHUD   = instance;
            count++;
            Debug.Log("[XR Setup] ExplorerHUD.prefab instanciado en escena.");
        }

        // Añadir ExplorerHUDFollower si falta
        var follower = existingHUD.GetComponent<ExplorerHUDFollower>();
        if (follower == null)
        {
            follower = Undo.AddComponent<ExplorerHUDFollower>(existingHUD);
            count++;
            Debug.Log("[XR Setup] ExplorerHUDFollower añadido a ExplorerHUD.");
        }

        // Auto-asignar headCamera
        if (follower.headCamera == null)
        {
            var cam = FindExplorerCamera();
            if (cam != null)
            {
                Undo.RecordObject(follower, "Asignar headCamera a ExplorerHUDFollower");
                follower.headCamera = cam;
                EditorUtility.SetDirty(follower);
                count++;
                Debug.Log($"[XR Setup] ExplorerHUDFollower.headCamera → {cam.name}");
            }
            else
            {
                Debug.LogWarning("[XR Setup] No se encontró la cámara del XR Rig. " +
                                 "Asigna 'headCamera' manualmente en ExplorerHUDFollower.");
            }
        }

        // Posicionar el HUD frente al origen de la escena si está en (0,0,0)
        if (existingHUD.transform.position == Vector3.zero)
        {
            existingHUD.transform.position = new Vector3(0f, 1.5f, 1.2f);
            Debug.Log("[XR Setup] ExplorerHUD posicionado provisionalmente en (0, 1.5, 1.2). " +
                      "El follower lo moverá al inicio de la sesión.");
        }

        // Verificar que el Canvas es WorldSpace
        var canvas = existingHUD.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            Undo.RecordObject(canvas, "Set ExplorerHUD Canvas WorldSpace");
            canvas.renderMode = RenderMode.WorldSpace;
            EditorUtility.SetDirty(canvas);
            count++;
            Debug.Log("[XR Setup] ExplorerHUD Canvas → WorldSpace.");
        }

        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CAPA "Grabbable" — guía al usuario (no modificable en runtime por API)
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupGrabbableLayerHint()
    {
        // XRI 3.x interaction layers se configuran en Project Settings.
        // Verificar si la capa Default (0) es suficiente para el proyecto.
        // Los XRDirectInteractors actuales tienen m_Bits=4294967295 (todas las capas).
        Debug.Log("[XR Setup] CAPA GRABBABLE: los XRDirectInteractor ya tienen " +
                  "InteractionLayers=ALL (4294967295). Para crear la capa dedicada 'Grabbable':\n" +
                  "  Edit → Project Settings → XR Interaction Toolkit → Interaction Layers\n" +
                  "  → Añadir 'Grabbable' en índice 1.\n" +
                  "  Luego en cada XRDirectInteractor e XRGrabInteractable: " +
                  "Interaction Layer Mask = Grabbable (en lugar de Everything).");
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject FindCanvasNamed(string name)
    {
        var canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include);
        foreach (var c in canvases)
            if (c.gameObject.name == name) return c.gameObject;
        return null;
    }

    static Camera FindExplorerCamera()
    {
        var cameras = Object.FindObjectsByType<Camera>(
            FindObjectsInactive.Include);

        // Prioridad 1: se llama ExplorerCamera y está bajo un GO llamado "Camera Offset"
        foreach (var cam in cameras)
        {
            string n = cam.gameObject.name.ToLowerInvariant();
            if (n.Contains("explorer") || n.Contains("centereye"))
                return cam;
        }

        // Prioridad 2: cámara con tag MainCamera cuyo padre contiene "Camera Offset"
        foreach (var cam in cameras)
        {
            if (!cam.CompareTag("MainCamera")) continue;
            var p = cam.transform.parent;
            if (p != null && p.name.ToLowerInvariant().Contains("camera offset"))
                return cam;
        }

        // Fallback: Camera.main
        return Camera.main;
    }
}
