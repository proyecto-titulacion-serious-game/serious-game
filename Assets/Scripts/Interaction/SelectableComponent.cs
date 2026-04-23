using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Versión VR de SelectableComponent.
/// Reemplaza OnMouseDown() con XRSimpleInteractable para funcionar con los
/// controladores del Meta Quest.
///
/// SETUP:
///   1. Agregar XRSimpleInteractable (o XRGrabInteractable si el componente se puede agarrar)
///   2. Asignar el ElectricalComponent en component
///   3. Asignar TechnicianActions desde el inspector
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
[RequireComponent(typeof(Collider))]
public class SelectableComponent : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Componente eléctrico")]
    public ElectricalComponent component;

    [Header("Referencias")]
    public TechnicianActions technicianActions;
    public PlayerInteraction  playerInteraction;   // Para Explorador VR

    [Header("Feedback visual")]
    public Color hoverColor    = new Color(1f, 1f, 0f, 0.5f);
    public Color selectedColor = Color.yellow;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;    private Renderer             _renderer;
    private Color                _originalColor;
    private MaterialPropertyBlock _mpb;
    private static readonly int  _colorID = Shader.PropertyToID("_Color");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();        _renderer     = GetComponent<Renderer>();
        _mpb          = new MaterialPropertyBlock();

        if (_renderer != null)
            _originalColor = _renderer.material.color;

        if (technicianActions == null)
            technicianActions = FindObjectOfType<TechnicianActions>();
        if (playerInteraction == null)
            playerInteraction = FindObjectOfType<PlayerInteraction>();
    }

    void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);
        _interactable.selectEntered.AddListener(OnSelect);
    }

    void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
        _interactable.selectEntered.RemoveListener(OnSelect);
    }

    // ─────────────────────────────────────────────
    //  Eventos XR
    // ─────────────────────────────────────────────

    void OnHoverEnter(HoverEnterEventArgs args) => SetColor(hoverColor);
    void OnHoverExit (HoverExitEventArgs  args) => ResetHighlight();

    void OnSelect(SelectEnterEventArgs args)
    {
        if (component == null) return;

        // El Técnico selecciona (diagnostica)
        technicianActions?.SelectComponent(component, this);

        // El Explorador puede agarrar el componente
        playerInteraction?.OnGrabComponent(this);

        SetColor(selectedColor);
        Debug.Log($"[SelectableComponent] Seleccionado: {component.name}");
    }

    // ─────────────────────────────────────────────
    //  Highlight API (llamado desde TechnicianActions)
    // ─────────────────────────────────────────────

    public void Highlight()    => SetColor(selectedColor);
    public void ResetHighlight() => SetColor(_originalColor);

    // ─────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────

    void SetColor(Color c)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID, c);
        _renderer.SetPropertyBlock(_mpb);
    }
}