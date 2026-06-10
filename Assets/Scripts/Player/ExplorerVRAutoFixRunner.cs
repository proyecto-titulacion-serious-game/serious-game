using System.Collections;
using UnityEngine;

/// <summary>
/// Helper de arranque diferido para <see cref="ExplorerVRAutoFix"/>.
///
/// Conecta los brazos del avatar RobotKyle a los mandos VR usando RobotHandIK,
/// para que las manos del robot sigan a los controladores (no solo animación).
/// Luego oculta las manos procedurales (HandModelController) para evitar
/// "manos dobles".
///
/// Se ejecuta unos frames después de cargar la escena (las manos procedurales y
/// el avatar se construyen en sus propios Start), valida que el avatar sea
/// Humanoid y, si no lo es, deja intactas las manos procedurales como respaldo.
/// El GameObject se autodestruye al terminar.
/// </summary>
public class ExplorerVRAutoFixRunner : MonoBehaviour
{
    IEnumerator Start()
    {
        // Esperar a que HandModelController.Start construya las manos procedurales
        // y a que RPMExplorerAvatar resuelva el avatar base (RobotKyle).
        yield return null;
        yield return null;

        ConnectRobotKyleHands();
        Destroy(gameObject);
    }

    void ConnectRobotKyleHands()
    {
        // Si VRRig controla el avatar, las manos visibles son las PROCEDURALES (siguen el mando
        // exacto por TrackedPoseDriver). No añadimos RobotHandIK ni ocultamos las procedurales.
        if (FindAnyObjectByType<VRRig>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[ExplorerVRAutoFix] VRRig presente → manos procedurales visibles (sin RobotHandIK).");
            return;
        }

        var ea = FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        if (ea == null) return;

        Transform root = ea.avatarRoot;
        if (root == null)
        {
            Debug.LogWarning("[ExplorerVRAutoFix] RobotKyle no resuelto (avatarRoot nulo); " +
                             "se mantienen las manos procedurales.");
            return;
        }

        var anim = root.GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            Debug.LogWarning("[ExplorerVRAutoFix] El avatar no es Humanoid; no se conectan brazos IK. " +
                             "Se mantienen las manos procedurales.");
            return;
        }

        // Localizar los controladores por sus HandModelController (izquierda/derecha).
        Transform leftCtrl = null, rightCtrl = null;
        var hands = FindObjectsByType<HandModelController>(FindObjectsInactive.Include);

        foreach (var h in hands)
        {
            if (h.hand == HandModelController.HandSide.Left) leftCtrl = h.transform;
            else                                             rightCtrl = h.transform;
        }

        if (leftCtrl == null && rightCtrl == null)
        {
            Debug.LogWarning("[ExplorerVRAutoFix] No se encontraron manos (HandModelController); IK no conectado.");
            return;
        }

        // Añadir y cablear RobotHandIK en el mismo GO que ExplorerAvatar.
        var ik = ea.GetComponent<RobotHandIK>();
        if (ik == null) ik = ea.gameObject.AddComponent<RobotHandIK>();
        ik.avatarRoot      = root;
        ik.leftController  = leftCtrl;
        ik.rightController = rightCtrl;
        ik.enabled         = true;

        // Ocultar las manos procedurales (solo el visual; el GO conserva el
        // XRDirectInteractor para poder agarrar) → evita "manos dobles".
        foreach (var h in hands)
        {
            var pivot = h.transform.Find("Hand_Pivot");
            if (pivot != null) pivot.gameObject.SetActive(false);
        }

        Debug.Log("[ExplorerVRAutoFix] Brazos de RobotKyle conectados a los mandos (RobotHandIK); " +
                  "manos procedurales ocultadas.");
    }
}
