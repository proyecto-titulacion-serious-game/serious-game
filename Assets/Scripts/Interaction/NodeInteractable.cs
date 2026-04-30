using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Nodo eléctrico seleccionable por VR. El Explorador apunta con el
/// controlador y presiona el gatillo para colocar la punta del multímetro.
///
/// CAMBIO respecto a la versión anterior:
///   - Ya no llama a PlayerInteraction.PlaceRedProbe / PlaceBlackProbe
///   - Llama directamente a Multimeter.SetRedNode / SetBlackNode
///   - Mano derecha → punta roja  |  Mano izquierda → punta negra
///
/// SETUP (sin cambios respecto a lo que ya tienes):
///   1. XRSimpleInteractable en este GameObject
///   2. Collider (isTrigger = FALSE) para el ray-cast
///   3. Asignar nodeTarget (ElectricalNode con voltage y current)
///   4. Asignar multimeter desde el inspector o auto-find
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

    [Header("Punta asignada (Auto = derecha=roja, izquierda=negra)")]
    public ProbeType probeType = ProbeType.Auto;

    [Header("Referencias")]
    public Multimeter multimeter;   // ← reemplaza playerInteraction

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
    private static readonly int _colorID = Shader.PropertyToID("_BaseColor");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        _mpb          = new MaterialPropertyBlock();

        if (nodeRenderer == null)
            nodeRenderer = GetComponent<Renderer>();

        if (nodeRenderer != null)
            _originalColor = nodeRenderer.sharedMaterial.color;

        // Auto-buscar el multímetro si no se asignó en inspector
        if (multimeter == null)
            multimeter = FindObjectOfType<Multimeter>();
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

    void OnHoverEnter(HoverEnterEventArgs args) => SetColor(hoverColor);

    void OnHoverExit(HoverExitEventArgs args) => SetColor(_originalColor);

    void OnSelectEnter(SelectEnterEventArgs args)
    {
        if (multimeter == null || nodeTarget == null) return;

        SetColor(selectedColor);

        // Derecha → punta roja | Izquierda → punta negra
        bool isRightHand = args.interactorObject.transform.CompareTag("RightHand");

        ProbeType resolved = probeType == ProbeType.Auto
            ? (isRightHand ? ProbeType.Red : ProbeType.Black)
            : probeType;

        // ← CAMBIO: ahora le avisa al Multimeter, no a PlayerInteraction
        switch (resolved)
        {
            case ProbeType.Red:
                multimeter.SetRedNode(nodeTarget);
                break;
            case ProbeType.Black:
                multimeter.SetBlackNode(nodeTarget);
                break;
        }

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