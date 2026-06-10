using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Consolida las manos VR del Explorador en runtime, poniendo la mano procedural EXACTAMENTE
/// en el GameObject del controlador que el usuario ve y usa: el que tiene el interactor
/// Near-Far (o Poke). Esos son los controladores stock del XR Origin (con TrackedPoseDriver),
/// así que la mano queda pegada a la posición real del mando.
///
/// La escena tenía varios HandModelController en GOs paralelos (con XRDirectInteractor) que NO
/// coincidían con los Near-Far/Poke → la mano salía en otro sitio. Aquí:
///   1) Se elige UN GO por lado: el que tiene NearFarInteractor (preferido) o XRPokeInteractor.
///   2) Se asegura el HandModelController en ESE GO (la mano se construye ahí, sobre el mando).
///   3) Se desactiva cualquier otro HandModelController (duplicados) para no ver manos dobles.
///
/// Añadir a un GO persistente de la escena (ej. XR Origin o GameManager).
/// </summary>
public class RuntimeHandConsolidator : MonoBehaviour
{
    void Awake()
    {
        // ── 1) Elegir el GO de cada mano por su interactor Near-Far / Poke ──
        GameObject left  = FindControllerGO(HandModelController.HandSide.Left)  ?? FindHandGOByName("Left");
        GameObject right = FindControllerGO(HandModelController.HandSide.Right) ?? FindHandGOByName("Right");

        if (left == null && right == null)
            Debug.LogWarning("[HandConsolidator] No se encontró ningún controlador (Near-Far/Poke/nombre).");

        // ¿Se usan las manos del PROPIO avatar (RobotKyle) en vez de las procedurales?
        var rig = FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
        bool manosAvatar = rig != null && rig.usarManosDelAvatar;

        if (manosAvatar)
        {
            // Manos del robot: NO construir procedurales; apagar todas; el VRRig mueve las del avatar.
            DisableHandsExcept(new List<GameObject>());
            Debug.Log("[HandConsolidator] Modo MANOS DEL AVATAR → manos procedurales desactivadas; " +
                      "el VRRig mueve los brazos/dedos del robot hacia los mandos.");
        }
        else
        {
            // ── Manos PROCEDURALES sobre el Near-Far/Poke ──
            var elegidos = new List<GameObject>();
            if (left  != null) { EnsureHand(left,  HandModelController.HandSide.Left);  elegidos.Add(left);  }
            if (right != null) { EnsureHand(right, HandModelController.HandSide.Right); elegidos.Add(right); }
            DisableHandsExcept(elegidos);
        }

        // ── Alinear los targets de IK del VRRig a ESTOS controladores (Near-Far/Poke) ──
        //    Así el brazo del robot apunta EXACTAMENTE a donde está el mando.
        AlinearTargetsVRRig(left, right);
    }

    void AlinearTargetsVRRig(GameObject left, GameObject right)
    {
        var rig = FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
        if (rig == null) return;
        if (left  != null) rig.leftHandTarget  = left.transform;
        if (right != null) rig.rightHandTarget = right.transform;
        Debug.Log("[HandConsolidator] Targets de IK del VRRig alineados a los controladores Near-Far/Poke " +
                  $"(L='{(left ? left.name : "—")}', R='{(right ? right.name : "—")}').");
    }

    /// <summary>
    /// GO del controlador de un lado: prefiere el que tiene NearFarInteractor; si no, el Poke.
    /// Estos son los mandos stock (con TrackedPoseDriver) → la mano queda sobre el Poke/Near-Far.
    /// </summary>
    GameObject FindControllerGO(HandModelController.HandSide side)
    {
        // Preferir Near-Far
        foreach (var nf in FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include))
            if (DetectSide(nf.transform) == side) return nf.gameObject;

        // Si no hay Near-Far, usar Poke
        foreach (var pk in FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include))
            if (DetectSide(pk.transform) == side) return pk.gameObject;

        return null;
    }

    /// <summary>Detecta el lado subiendo por la jerarquía buscando "left"/"right" en los nombres.</summary>
    static HandModelController.HandSide? DetectSide(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
        {
            string n = cur.name.ToLowerInvariant();
            if (n.Contains("left")  || n.Contains("izq")) return HandModelController.HandSide.Left;
            if (n.Contains("right") || n.Contains("der")) return HandModelController.HandSide.Right;
        }
        return null;
    }

    /// <summary>Asegura un HandModelController activo del lado correcto en el GO indicado.</summary>
    void EnsureHand(GameObject go, HandModelController.HandSide side)
    {
        // Limpiar placeholders heredados del prefab.
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            if (go.transform.GetChild(i).name.StartsWith("HandPlaceholder"))
                Destroy(go.transform.GetChild(i).gameObject);

        var hmc = go.GetComponent<HandModelController>();
        if (hmc == null)
        {
            hmc = go.AddComponent<HandModelController>();
            hmc.hand = side;
            Debug.Log($"[HandConsolidator] Mano procedural AÑADIDA en '{NombreRuta(go.transform)}' " +
                      $"(lado {side}) → sobre el Near-Far/Poke.");
        }
        else
        {
            hmc.hand    = side;
            hmc.enabled = true;
            // Reactivar geometría por si un pase previo la había ocultado.
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                if (go.transform.GetChild(i).name.StartsWith("Hand_"))
                    go.transform.GetChild(i).gameObject.SetActive(true);
            Debug.Log($"[HandConsolidator] Mano procedural HABILITADA en '{NombreRuta(go.transform)}' (lado {side}).");
        }
    }

    /// <summary>Desactiva todas las manos procedurales que NO sean las elegidas (duplicados).</summary>
    void DisableHandsExcept(List<GameObject> elegidos)
    {
        foreach (var hmc in FindObjectsByType<HandModelController>(FindObjectsInactive.Include))
        {
            if (elegidos.Contains(hmc.gameObject)) continue;

            // Ocultar la geometría ya construida y apagar el componente.
            for (int i = hmc.transform.childCount - 1; i >= 0; i--)
                if (hmc.transform.GetChild(i).name.StartsWith("Hand_"))
                    hmc.transform.GetChild(i).gameObject.SetActive(false);

            hmc.enabled = false;
            Debug.Log($"[HandConsolidator] Mano duplicada DESACTIVADA en '{NombreRuta(hmc.transform)}'.");
        }
    }

    // Prueba nombres estándar de XRI 3.x (fallback si no hay Near-Far/Poke).
    static GameObject FindHandGOByName(string side)
    {
        string[] candidates = {
            $"{side} Controller", $"{side}Hand Controller", $"{side}Hand_Controller",
            $"{side}Hand", $"XRController ({side})",
        };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        return null;
    }

    static string NombreRuta(Transform t)
    {
        string r = t.name;
        for (var p = t.parent; p != null; p = p.parent) r = p.name + "/" + r;
        return r;
    }
}
