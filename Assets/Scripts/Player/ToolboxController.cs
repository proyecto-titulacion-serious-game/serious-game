using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Caja de herramientas que el Explorador VR puede cargar con la mano.
/// Los componentes enviados por el Técnico aparecen dentro de la caja.
///
/// SETUP EN UNITY (escena Explorador):
///   1. Crear GameObject "Toolbox" con mesh visible (caja, maletín, etc.).
///   2. Agregar XRGrabInteractable (Movement Type: Kinematic).
///   3. Agregar Rigidbody (Is Kinematic: TRUE — la caja no cae por física).
///   4. Agregar Collider no-trigger para el grab (BoxCollider ajustado a la caja).
///   5. Agregar este script.
///   6. Crear un Transform hijo llamado "ComponentSlot" en el interior visual de la caja.
///      → Asignarlo al campo componentSlot de este script.
///      → Asignarlo también a ExplorerComponentReceiver.puntoDeEntrega.
///   7. (Opcional) Crear un Transform vacío en la mesa y asignarlo a restAnchor.
///
/// FLUJO:
///   Técnico envía componente → aparece en ComponentSlot (dentro de la caja, quieto).
///   Explorador agarra la caja con una mano → la lleva al circuito.
///   Explorador agarra el componente de dentro con la otra mano → lo extrae de la caja.
///   Explorador deposita el componente en el slot del circuito → instalación validada.
///   Explorador suelta la caja cerca de la mesa → hace snap al anchor.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ToolboxController : MonoBehaviour
{
    [Header("Slot interior de la caja")]
    [Tooltip("Transform hijo en el interior de la caja. " +
             "Asignarlo también en ExplorerComponentReceiver.puntoDeEntrega.")]
    public Transform componentSlot;

    [Header("Anclaje en mesa (snap al soltar cerca)")]
    [Tooltip("Transform vacío sobre la mesa donde descansa la caja.")]
    public Transform restAnchor;
    [Range(0.05f, 1f)]
    [Tooltip("Distancia máxima desde restAnchor para hacer snap automático.")]
    public float snapDistance = 0.5f;

    [Header("Feedback")]
    public HapticFeedback haptics;

    public bool IsHeld { get; private set; }

    private XRGrabInteractable _grab;
    private Rigidbody          _rb;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        if (haptics == null)
            haptics = FindAnyObjectByType<HapticFeedback>();

        // La caja siempre es kinematic: no cae ni es empujada por física
        _rb.isKinematic = true;
        _rb.useGravity  = false;
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnPickedUp);
        _grab.selectExited.AddListener(OnPutDown);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnPickedUp);
        _grab.selectExited.RemoveListener(OnPutDown);
    }

    void OnPickedUp(SelectEnterEventArgs args)
    {
        IsHeld = true;
        haptics?.PlayMedium();
        Debug.Log("[Toolbox] Recogida.");
    }

    void OnPutDown(SelectExitEventArgs args)
    {
        IsHeld = false;
        haptics?.PlayLight();

        if (restAnchor != null &&
            Vector3.Distance(transform.position, restAnchor.position) <= snapDistance)
            SnapToAnchor();

        Debug.Log("[Toolbox] Depositada.");
    }

    void SnapToAnchor()
    {
        transform.SetPositionAndRotation(restAnchor.position, restAnchor.rotation);
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        Debug.Log("[Toolbox] Snap al anchor.");
    }

    /// <summary>Posición donde deben aparecer los componentes recibidos del Técnico.</summary>
    public Transform GetComponentSlot() => componentSlot != null ? componentSlot : transform;
}
