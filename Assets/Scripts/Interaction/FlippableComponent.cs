using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Permite al Explorador corregir la polaridad de un LED o Capacitor en el Reto 3.
///
/// SETUP EN UNITY (en el GameObject del LED o Capacitor en reto3Zone):
///   1. Añadir este script.
///   2. Añadir XRSimpleInteractable + Collider al mismo GameObject.
///   3. Asignar flipType (LED o Capacitor).
///   4. Asignar el componente eléctrico (ledComponent o capacitorComponent).
///   5. Asignar playerInteraction desde la escena.
///
/// FLUJO:
///   Explorador apunta con el controlador y pulsa gatillo
///   → OnFlip() detecta el tipo → llama PlayerInteraction.CorrectPolarity()
///   → polarityInverted = false → circuit.MarkDirty() → circuito resimula
///   → InstructionSystem avanza al siguiente paso
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Collider))]
public class FlippableComponent : MonoBehaviour
{
    public enum FlipType { LED, Capacitor }

    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Tipo de componente")]
    public FlipType flipType = FlipType.LED;

    [Header("Componente eléctrico a voltear")]
    public LED        ledComponent;
    public Capacitor  capacitorComponent;

    [Header("Referencias")]
    public PlayerInteraction playerInteraction;

    [Header("Feedback visual (opcional)")]
    [Tooltip("Indicador que se activa cuando el componente ya fue corregido.")]
    public GameObject fixedIndicator;
    public Color      colorPendiente = new Color(1f, 0.3f, 0.1f); // rojo-naranja
    public Color      colorCorregido = new Color(0.2f, 0.9f, 0.3f); // verde

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private XRSimpleInteractable _interactable;
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID = Shader.PropertyToID("_BaseColor");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _renderer     = GetComponent<Renderer>();
        _mpb          = new MaterialPropertyBlock();

        if (playerInteraction == null)
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();
    }

    void OnEnable()  => _interactable.selectEntered.AddListener(OnFlip);
    void OnDisable() => _interactable.selectEntered.RemoveListener(OnFlip);

    void Update()
    {
        // Actualizar color según estado actual del componente
        bool estaInvertido = IsCurrentlyInverted();
        SetRendererColor(estaInvertido ? colorPendiente : colorCorregido);

        if (fixedIndicator != null)
            fixedIndicator.SetActive(!estaInvertido);
    }

    // ─────────────────────────────────────────────
    //  Interacción VR
    // ─────────────────────────────────────────────
    void OnFlip(SelectEnterEventArgs args)
    {
        switch (flipType)
        {
            case FlipType.LED:
                if (ledComponent == null)
                {
                    Debug.LogWarning("[FlippableComponent] ledComponent no asignado.");
                    return;
                }
                if (!ledComponent.polarityInverted)
                {
                    Debug.Log("[FlippableComponent] LED ya está en polaridad correcta.");
                    return;
                }
                playerInteraction?.CorrectPolarity(ledComponent);
                Debug.Log($"[FlippableComponent] LED volteado: {gameObject.name}");
                break;

            case FlipType.Capacitor:
                if (capacitorComponent == null)
                {
                    Debug.LogWarning("[FlippableComponent] capacitorComponent no asignado.");
                    return;
                }
                if (!capacitorComponent.polarityInverted)
                {
                    Debug.Log("[FlippableComponent] Capacitor ya está en polaridad correcta.");
                    return;
                }
                playerInteraction?.CorrectCapacitorPolarity(capacitorComponent);
                Debug.Log($"[FlippableComponent] Capacitor volteado: {gameObject.name}");
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    bool IsCurrentlyInverted()
    {
        return flipType switch
        {
            FlipType.LED      => ledComponent != null && ledComponent.polarityInverted,
            FlipType.Capacitor => capacitorComponent != null && capacitorComponent.polarityInverted,
            _                  => false
        };
    }

    void SetRendererColor(Color c)
    {
        if (_renderer == null || _mpb == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID, c);
        _renderer.SetPropertyBlock(_mpb);
    }
}
