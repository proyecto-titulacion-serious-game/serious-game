using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Consolida el sistema de manos VR en runtime.
///
/// Problema que resuelve: la escena tiene dos sistemas paralelos:
///   A) Scene-level LeftHand/RightHand Controller → XRDirectInteractor + TrackedPoseDriver (interacción, sin visual)
///   B) Explorer_Player prefab hands → HandModelController + TrackedPoseDriver (visual, sin interacción)
///
/// Esta clase une ambos: añade HandModelController a los hands con XRDirectInteractor
/// y deshabilita los TrackedPoseDrivers duplicados del prefab para evitar conflictos.
///
/// Añadir a cualquier GO persistente en la escena Explorador (ej. GameManager o XR Origin).
/// </summary>
public class RuntimeHandConsolidator : MonoBehaviour
{
    void Awake()
    {
        ConsolidateHand(FindHandGO("Left"),  HandModelController.HandSide.Left);
        ConsolidateHand(FindHandGO("Right"), HandModelController.HandSide.Right);
        DisableDuplicateTrackers();
    }

    // Prueba varios nombres estándar de XRI 3.x según la versión del paquete o del prefab.
    static GameObject FindHandGO(string side)
    {
        string[] candidates = {
            $"{side}Hand_Controller",   // prefab TITA (guión bajo)
            $"{side}Hand Controller",
            $"{side}Hand",
            $"{side} Controller",
            $"XRController ({side})",
        };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        Debug.LogWarning($"[HandConsolidator] No se encontró controlador de mano '{side}'. " +
                         $"Candidatos: {string.Join(", ", candidates)}");
        return null;
    }

    void ConsolidateHand(GameObject go, HandModelController.HandSide side)
    {
        if (go == null) return;
        string handName = go.name;

        bool hasInteractor = go.GetComponent<XRDirectInteractor>() != null
                          || go.GetComponent<NearFarInteractor>() != null;
        if (!hasInteractor)
        {
            Debug.LogWarning($"[HandConsolidator] '{handName}' no tiene XRDirectInteractor ni NearFarInteractor — saltando.");
            return;
        }

        // Destruir placeholders heredados del prefab
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = go.transform.GetChild(i);
            if (child.name.StartsWith("HandPlaceholder"))
            {
                Destroy(child.gameObject);
                Debug.Log($"[HandConsolidator] Placeholder '{child.name}' eliminado de '{handName}'.");
            }
        }

        // Buscar visual Quest como hijo directo
        string questName = side == HandModelController.HandSide.Left
            ? "LeftHandQuestVisual" : "RightHandQuestVisual";
        Transform questVisual = go.transform.Find(questName);

        if (questVisual != null)
        {
            // Visual Quest disponible — desactivar HandModelController y su geometría procedural
            HandModelController hmc = go.GetComponent<HandModelController>();
            if (hmc != null)
            {
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = go.transform.GetChild(i);
                    if (child.name.StartsWith("Hand_")) Destroy(child.gameObject);
                }
                hmc.enabled = false;
            }
            questVisual.gameObject.SetActive(true);
            Debug.Log($"[HandConsolidator] QuestVisual '{questName}' activado en '{handName}'.");
        }
        else
        {
            // Si ya hay un mesh de controlador físico como hijo, no añadir geometría procedural.
            bool hasControllerVisual = go.GetComponentInChildren<MeshRenderer>() != null
                                    || go.GetComponentInChildren<SkinnedMeshRenderer>() != null;
            if (hasControllerVisual)
            {
                Debug.Log($"[HandConsolidator] '{handName}' ya tiene visual de controlador — omitiendo HandModelController.");
                return;
            }

            // Fallback: mano procedural con HandModelController
            HandModelController hmc = go.GetComponent<HandModelController>();
            if (hmc == null)
            {
                hmc      = go.AddComponent<HandModelController>();
                hmc.hand = side;
                Debug.Log($"[HandConsolidator] HandModelController añadido a '{handName}' (lado: {side}).");
            }
            else
            {
                hmc.hand    = side;
                hmc.enabled = true;
            }
        }
    }

    void DisableDuplicateTrackers()
    {
        // Busca todos los HandModelController en escena.
        // Los que NO tienen XRDirectInteractor en el mismo GO son los duplicados del prefab.
        HandModelController[] allHMCs = FindObjectsByType<HandModelController>(FindObjectsInactive.Include);
        foreach (var hmc in allHMCs)
        {
            bool hasInteractor = hmc.GetComponent<XRDirectInteractor>() != null
                              || hmc.GetComponent<NearFarInteractor>() != null;
            if (hasInteractor) continue; // es el correcto, no tocar

            // Deshabilitar TrackedPoseDriver para que no compita con el correcto
            TrackedPoseDriver tpd = hmc.GetComponent<TrackedPoseDriver>();
            if (tpd != null)
            {
                tpd.enabled = false;
                Debug.Log($"[HandConsolidator] TrackedPoseDriver duplicado deshabilitado en '{hmc.gameObject.name}'.");
            }

            // Deshabilitar este HandModelController duplicado para que no construya geometría extra
            hmc.enabled = false;
            Debug.Log($"[HandConsolidator] HandModelController duplicado deshabilitado en '{hmc.gameObject.name}'.");
        }
    }
}
