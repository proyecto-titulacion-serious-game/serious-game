using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Configura el seguimiento, visual e interacción de manos VR en la escena Explorador.
/// Menu: Tools → TITA → Setup VR Hand Controllers
///
/// Que hace:
///   1. Busca los GameObjects "LeftHand Controller" y "RightHand Controller"
///   2. Añade TrackedPoseDriver con bindings de OpenXR
///   3. Añade HandModelController para mostrar la geometría de la mano
///   4. Añade XRDirectInteractor + SphereCollider trigger para interacción
/// </summary>
public class VRHandSetupTool : Editor
{
    [MenuItem("Tools/TITA/Setup VR Hand Controllers")]
    static void SetupHandControllers()
    {
        bool changed = false;
        // Soporta tanto "LeftHand Controller" (espacio, XRI estándar)
        // como "LeftHand_Controller" (guión bajo, prefab TITA)
        changed |= SetupHand(FindHandGO("Left"),
            "<XRController>{LeftHand}/devicePosition",
            "<XRController>{LeftHand}/deviceRotation",
            HandModelController.HandSide.Left);
        changed |= SetupHand(FindHandGO("Right"),
            "<XRController>{RightHand}/devicePosition",
            "<XRController>{RightHand}/deviceRotation",
            HandModelController.HandSide.Right);

        if (changed)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[VRHandSetupTool] Manos configuradas. Guarda la escena (Ctrl+S).");
        }
        else
        {
            Debug.Log("[VRHandSetupTool] Sin cambios (los controladores ya estaban configurados).");
        }
    }

    // Busca el GO de la mano por las variantes de nombre más comunes.
    static GameObject FindHandGO(string side)
    {
        string[] candidates = {
            $"{side}Hand Controller",    // XRI estándar
            $"{side}Hand_Controller",    // prefab TITA (guión bajo)
            $"{side}Hand",
            $"{side} Controller",
            $"XRController ({side})",
        };
        foreach (var n in candidates)
        {
            var go = GameObject.Find(n);
            if (go != null) return go;
        }
        Debug.LogWarning($"[VRHandSetupTool] Mano '{side}' no encontrada. " +
                         $"Candidatos probados: {string.Join(", ", candidates)}");
        return null;
    }

    static bool SetupHand(GameObject go, string posPath, string rotPath, HandModelController.HandSide side)
    {
        if (go == null) return false;
        string goName = go.name;

        bool changed = false;

        // ── TrackedPoseDriver ──────────────────────────────────────────────
        TrackedPoseDriver tpd = go.GetComponent<TrackedPoseDriver>();
        if (tpd == null)
        {
            tpd = Undo.AddComponent<TrackedPoseDriver>(go);
            ConfigureTrackedPoseDriver(tpd, posPath, rotPath);
            changed = true;
            Debug.Log($"[VRHandSetupTool] TrackedPoseDriver añadido a '{goName}'.");
        }

        // ── HandModelController ────────────────────────────────────────────
        HandModelController hmc = go.GetComponent<HandModelController>();
        if (hmc == null)
        {
            hmc      = Undo.AddComponent<HandModelController>(go);
            hmc.hand = side;
            changed  = true;
            Debug.Log($"[VRHandSetupTool] HandModelController añadido a '{goName}' (lado: {side}).");
        }

        // ── XRDirectInteractor + SphereCollider trigger ────────────────────
        XRDirectInteractor interactor = go.GetComponent<XRDirectInteractor>();
        if (interactor == null)
        {
            // SphereCollider trigger requerido por XRDirectInteractor para detección
            SphereCollider sc = go.GetComponent<SphereCollider>();
            if (sc == null)
            {
                sc = Undo.AddComponent<SphereCollider>(go);
                sc.isTrigger = true;
                sc.radius    = 0.05f;
                Debug.Log($"[VRHandSetupTool] SphereCollider trigger añadido a '{goName}' (r=0.05).");
            }

            interactor = Undo.AddComponent<XRDirectInteractor>(go);
            changed = true;
            Debug.Log($"[VRHandSetupTool] XRDirectInteractor añadido a '{goName}'.");
        }

        // Eliminar cápsulas placeholder antiguas si existen
        foreach (Transform child in go.transform)
        {
            if (child.name == "HandPlaceholder_Replace")
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                changed = true;
                Debug.Log($"[VRHandSetupTool] Placeholder eliminado de '{goName}'.");
            }
        }

        return changed;
    }

    static void ConfigureTrackedPoseDriver(TrackedPoseDriver tpd, string posPath, string rotPath)
    {
        // Position input
        var posAction = new InputAction("Position", InputActionType.Value, posPath, expectedControlType: "Vector3");
        if (posPath.Contains("LeftHand"))
            posAction.AddBinding("<XRHMD>/leftEyePosition");
        else
            posAction.AddBinding("<XRHMD>/rightEyePosition");

        // Rotation input
        var rotAction = new InputAction("Rotation", InputActionType.Value, rotPath, expectedControlType: "Quaternion");
        if (rotPath.Contains("LeftHand"))
            rotAction.AddBinding("<XRHMD>/leftEyeRotation");
        else
            rotAction.AddBinding("<XRHMD>/rightEyeRotation");

        tpd.positionInput = new InputActionProperty(posAction);
        tpd.rotationInput = new InputActionProperty(rotAction);
        tpd.trackingType  = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType    = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
    }

    [MenuItem("Tools/TITA/Remove Hand Placeholders")]
    static void RemovePlaceholders()
    {
        var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        int count = 0;
        foreach (var go in all)
        {
            if (go.name == "HandPlaceholder_Replace")
            {
                Undo.DestroyObjectImmediate(go);
                count++;
            }
        }
        if (count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[VRHandSetupTool] {count} placeholder(s) eliminado(s).");
        }
    }
}
