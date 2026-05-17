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

    // Radio de búsqueda para auto-detección de nodeTarget por proximidad
    private const float AUTO_DETECT_RADIUS = 0.5f;

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

        if (multimeter == null || !multimeter.gameObject.activeInHierarchy)
            multimeter = FindAnyObjectByType<Multimeter>();
    }

    void Start()
    {
        if (nodeTarget != null) return;

        // 1. Mismo GO o hijos
        nodeTarget = GetComponent<ElectricalNode>()
                  ?? GetComponentInChildren<ElectricalNode>(true);

        if (nodeTarget != null)
        {
            Debug.Log($"[NodeInteractable] '{name}': nodeTarget auto-detectado en '{nodeTarget.name}'.");
            return;
        }

        // 2. ElectricalNode más cercano dentro del radio
        float minDist = AUTO_DETECT_RADIUS;
        foreach (var n in FindObjectsByType<ElectricalNode>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < minDist) { minDist = d; nodeTarget = n; }
        }

        if (nodeTarget != null)
        {
            Debug.LogWarning($"[NodeInteractable] '{name}': nodeTarget auto-detectado por proximidad → '{nodeTarget.name}' ({minDist:F3} m). " +
                             "Asigna el ElectricalNode manualmente en el Inspector para mayor fiabilidad.");
        }
        else
        {
            Debug.LogError($"[NodeInteractable] '{name}': nodeTarget no asignado y no se encontró ningún ElectricalNode a menos de {AUTO_DETECT_RADIUS} m. " +
                           "El multímetro no podrá medir en este nodo. Asígnalo en el Inspector.");
        }
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
        if (multimeter == null)
        {
            Debug.LogWarning($"[NodeInteractable] '{name}': multimeter es null. Verifica que Multimeter_VR está en la escena activa.");
            return;
        }
        if (nodeTarget == null)
        {
            Debug.LogWarning($"[NodeInteractable] '{name}': nodeTarget es null. Asigna el ElectricalNode correspondiente en el Inspector.");
            return;
        }

        SetColor(selectedColor);

        ProbeType resolved = probeType == ProbeType.Auto
            ? (IsRightHand(args.interactorObject) ? ProbeType.Red : ProbeType.Black)
            : probeType;

        switch (resolved)
        {
            case ProbeType.Red:   multimeter.SetRedNode(nodeTarget);   break;
            case ProbeType.Black: multimeter.SetBlackNode(nodeTarget); break;
        }

        Invoke(nameof(ResetColor), 0.5f);
    }

    // Detecta mano derecha por tag, nombre del GameObject o posición relativa a la cámara.
    // Orden de prioridad: tag → nombre → posición lateral.
    static bool IsRightHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        Transform t = interactor.transform;

        if (t.CompareTag("RightHand")) return true;
        if (t.CompareTag("LeftHand"))  return false;

        string n = t.name.ToLowerInvariant();
        if (n.Contains("right")) return true;
        if (n.Contains("left"))  return false;

        // Último recurso: si el interactor está a la derecha de la cámara principal
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 localPos = cam.transform.InverseTransformPoint(t.position);
            return localPos.x >= 0f;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  Contacto físico — punta del multímetro entra en el nodo
    // ─────────────────────────────────────────────

    // Detectado desde el lado del nodo: la esfera trigger de la punta toca este collider.
    void OnTriggerEnter(Collider other)
    {
        if (multimeter == null || nodeTarget == null) return;

        var probe = other.GetComponent<MultimeterProbe>()
                 ?? other.GetComponentInParent<MultimeterProbe>();
        if (probe == null) return;

        ProbeType resolved = probeType == ProbeType.Auto ? probe.probeType : probeType;
        switch (resolved)
        {
            case ProbeType.Red:   multimeter.SetRedNode(nodeTarget);   break;
            case ProbeType.Black: multimeter.SetBlackNode(nodeTarget); break;
        }

        SetColor(selectedColor);
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