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

        if (haptics          == null) haptics          = FindAnyObjectByType<HapticFeedback>();
        if (playerInteraction == null) playerInteraction = FindAnyObjectByType<PlayerInteraction>();
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
        CancelInvoke(nameof(EnableGravity));
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Desparentar de la caja para que la física sea independiente
        transform.SetParent(null);

        // XRGrabInteractable (MovementType.Kinematic) gestiona isKinematic internamente.
        // No sobreescribir aquí: si lo ponemos false, el objeto cae y XRI no puede moverlo.

        haptics?.PlayMedium();
        playerInteraction?.OnGrabComponent(selectable);

        Debug.Log($"[GrabbableComponent] Agarrado: {name}");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // XRGrabInteractable.OnSelectExited dispara este evento y luego llama Drop(),
        // que restaura isKinematic al valor previo al grab (true). Si ponemos
        // isKinematic = false aquí, Drop() lo sobreescribe en el mismo frame.
        // Diferir un frame garantiza que nuestro cambio llegue después de Drop().
        Invoke(nameof(EnableGravity), 0f);

        haptics?.PlayLight();
        playerInteraction?.OnReleaseComponent(selectable);

        Debug.Log($"[GrabbableComponent] Soltado: {name}");
    }

    void EnableGravity()
    {
        // Si el componente fue instalado en un slot, DisableGrab() deshabilita
        // el XRGrabInteractable. En ese caso no debe caer.
        if (!_grab.enabled) return;

        _rb.isKinematic = false;
        _rb.useGravity  = true;
    }

    /// <summary>Llamado por ComponentSlot tras instalación exitosa. El componente queda fijo.</summary>
    public void DisableGrab() => _grab.enabled = false;

    /// <summary>Re-habilita el grab (usado cuando el slot permite remover el componente instalado).</summary>
    public void EnableGrab()  => _grab.enabled = true;
}
