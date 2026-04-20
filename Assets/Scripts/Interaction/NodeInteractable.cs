using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Reemplaza OnMouseDown() con XRSimpleInteractable para VR.
/// Colocar en cada nodo eléctrico de la escena.
///
/// SETUP:
///   1. Agregar XRSimpleInteractable a este GameObject
///   2. Asignar el ElectricalNode en nodeTarget
///   3. Asignar playerInteraction desde el inspector o FindObjectOfType
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
[RequireComponent(typeof(Collider))]
public class NodeInteractable : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Nodo eléctrico de este punto")]
    public ElectricalNode nodeTarget;

    [Header("Tipo de punta")]
    public ProbeType probeType = ProbeType.Auto;

    [Header("Referencias")]
    public PlayerInteraction playerInteraction;

    [Header("Feedback visual")]
    public Renderer nodeRenderer;
    public Color    hoverColor    = Color.yellow;
    public Color    selectedColor = Color.cyan;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;
    private Color _originalColor;
    private MaterialPropertyBlock _mpb;
    private static readonly int _colorID = Shader.PropertyToID("_Color");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        _mpb          = new MaterialPropertyBlock();

        if (nodeRenderer != null)
        {
            nodeRenderer.GetPropertyBlock(_mpb);
            _originalColor = nodeRenderer.material.color;
        }

        if (playerInteraction == null)
            playerInteraction = FindObjectOfType<PlayerInteraction>();
    }

    void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);
        _interactable.selectEntered.AddListener(OnSelectEnter);
    }

    void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
        _interactable.selectEntered.RemoveListener(OnSelectEnter);
    }

    // ─────────────────────────────────────────────
    //  Eventos XR
    // ─────────────────────────────────────────────

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        SetColor(hoverColor);
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        SetColor(_originalColor);
    }

    void OnSelectEnter(SelectEnterEventArgs args)
    {
        if (playerInteraction == null || nodeTarget == null) return;

        SetColor(selectedColor);

        // Determinar automáticamente qué punta usar según el interactor
        bool isRightHand = args.interactorObject.transform.CompareTag("RightHand");

        ProbeType resolvedType = probeType == ProbeType.Auto
            ? (isRightHand ? ProbeType.Red : ProbeType.Black)
            : probeType;

        switch (resolvedType)
        {
            case ProbeType.Red:
                playerInteraction.PlaceRedProbe(nodeTarget);
                break;
            case ProbeType.Black:
                playerInteraction.PlaceBlackProbe(nodeTarget);
                break;
        }

        // Restaurar color después de un momento
        Invoke(nameof(ResetColor), 0.5f);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void SetColor(Color c)
    {
        if (nodeRenderer == null) return;
        nodeRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID, c);
        nodeRenderer.SetPropertyBlock(_mpb);
    }

    void ResetColor() => SetColor(_originalColor);
}

public enum ProbeType { Auto, Red, Black }