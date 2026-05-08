using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Va en cada prefab de componente entregable (Comp_Resistor, Comp_LED, Comp_Capacitor, ArduinoPin).
/// Gestiona el ciclo grab→soltar con física y feedback háptico.
///
/// SETUP EN CADA PREFAB (hacerlo una vez, se aplica a todas las instancias):
///   1. XRGrabInteractable  — Movement Type: Kinematic  — Track Position: ON — Track Rotation: ON
///   2. Rigidbody           — Is Kinematic: TRUE (empieza quieto en la bandeja)
///   3. Collider            — Is Trigger: FALSE (para detección física y slots)
///   4. Este script
///
/// FLUJO:
///   Componente spawneado en toolbox (kinematic, quieto)
///   → Explorador aprieta gatillo cerca del componente
///   → OnGrabbed: se desparentea de la caja, kinematic OFF, sigue la mano
///   → Explorador lo mete en el ComponentSlot del circuito
///   → ComponentSlot.OnTriggerEnter: instala, kinematic ON otra vez
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableComponent : MonoBehaviour
{
    [Header("Feedback (auto-detectado si queda vacío)")]
    public HapticFeedback haptics;

    [Header("SelectableComponent asociado (opcional — para highlight)")]
    public SelectableComponent selectable;

    [Header("PlayerInteraction (auto-detectado)")]
    public PlayerInteraction playerInteraction;

    private XRGrabInteractable _grab;
    private Rigidbody          _rb;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        if (haptics          == null) haptics          = FindFirstObjectByType<HapticFeedback>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        if (selectable        == null) selectable        = GetComponent<SelectableComponent>();
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrabbed);
        _grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Desparentar de la caja para que la física sea independiente
        transform.SetParent(null);

        // Activar física completa mientras está en la mano
        _rb.isKinematic = false;
        _rb.useGravity  = true;

        haptics?.PlayMedium();
        playerInteraction?.OnGrabComponent(selectable);

        Debug.Log($"[GrabbableComponent] Agarrado: {name}");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        haptics?.PlayLight();
        playerInteraction?.OnReleaseComponent(selectable);

        Debug.Log($"[GrabbableComponent] Soltado: {name}");
    }

    /// <summary>
    /// Llamado por ComponentSlot tras una instalación exitosa.
    /// Deshabilita el grab para que no se pueda volver a coger del slot.
    /// </summary>
    public void DisableGrab()
    {
        _grab.enabled = false;
    }
}
