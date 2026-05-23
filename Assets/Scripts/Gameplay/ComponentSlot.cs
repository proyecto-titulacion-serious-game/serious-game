using UnityEngine;

public enum ComponentSlotType { Resistor, LED, Capacitor, ArduinoPin }

/// <summary>
/// Slot físico donde el Explorador instala un componente recibido del Técnico.
/// CORRECCIONES respecto a la versión anterior:
///   1. delivery.OnExplorerInstalled() solo se llama cuando el tipo COINCIDE.
///   2. Se llama gameManager.RegisterRepairAction() al instalar correctamente,
///      lo que dispara CheckReto1() / CheckReto2() automáticamente.
///   3. Si el tipo NO coincide, se llama RegisterWrongAttempt() para el log.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ComponentSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Tipo aceptado")]
    public ComponentSlotType acceptedType = ComponentSlotType.Resistor;

    [Header("Referencias")]
    public ComponentDeliverySystem delivery;
    public GameManager             gameManager;   // ← NUEVO: para RegisterRepairAction

    [Header("Feedback visual")]
    public Renderer slotRenderer;
    public Color colorNormal  = new Color(0.3f, 0.3f, 0.3f);
    public Color colorHover   = new Color(0.9f, 0.9f, 0.2f);
    public Color colorCorrect = new Color(0.2f, 0.9f, 0.3f);
    public Color colorWrong   = new Color(0.9f, 0.2f, 0.2f);

    [Header("Anclaje al instalar")]
    public Transform installAnchor;

    [Header("Componente dañado a reemplazar (opcional)")]
    [Tooltip("Arrastra aquí el GameObject del componente defectuoso visible en la escena. " +
             "Se ocultará automáticamente cuando el Explorador instale el reemplazo.")]
    public GameObject damagedComponent;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool       _hasComponent = false;
    [SerializeField] private GameObject _installed;

    private GrabbableComponent _hoveringGC;

    /// <summary>El GameObject actualmente instalado en este slot (null si vacío).</summary>
    public GameObject InstalledObject => _installed;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private static readonly int _colorID       = Shader.PropertyToID("_BaseColor"); // URP
    private static readonly int _colorLegacyID = Shader.PropertyToID("_Color");     // Built-in fallback

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (slotRenderer == null) slotRenderer = GetComponent<Renderer>();
        if (delivery     == null) delivery     = FindAnyObjectByType<ComponentDeliverySystem>();
        if (gameManager  == null) gameManager  = FindAnyObjectByType<GameManager>();

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger) col.isTrigger = true;

        SetColor(colorNormal);
    }

    // ─────────────────────────────────────────────
    //  Trigger
    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_hasComponent) return;

        var gc = other.GetComponentInParent<GrabbableComponent>();
        if (gc == null) return;

        // Sólo aceptar componentes mientras hay una entrega pendiente.
        // Evita que componentes de escena (ej. Resistor_Faulty) se auto-instalen al inicio.
        if (delivery != null && !delivery.HasPendingDelivery()) return;

        ComponentType incoming = DetectComponentType(gc.gameObject);
        bool matches = MatchesSlotType(incoming, acceptedType);

        if (matches)
        {
            _hoveringGC = gc;
            SetColor(colorHover);
            gc.Released += OnComponentReleasedInSlot;

            // Si el usuario soltó el componente justo antes de entrar al trigger
            if (!gc.IsGrabbed)
                OnComponentReleasedInSlot(gc);
        }
        else
        {
            SetColor(colorWrong);
            gameManager?.RegisterWrongAttempt(
                $"Componente incorrecto: {incoming} en slot de {acceptedType}");
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 1.2f);
            Debug.Log($"[Slot {name}] Tipo incorrecto: {incoming} (esperaba {acceptedType})");
        }
    }

    void OnTriggerStay(Collider other) { }

    void OnTriggerExit(Collider other)
    {
        if (_hasComponent) return;
        var gc = other.GetComponentInParent<GrabbableComponent>();
        if (gc != null && gc == _hoveringGC)
        {
            gc.Released -= OnComponentReleasedInSlot;
            _hoveringGC = null;
            SetColor(colorNormal);
        }
    }

    void OnComponentReleasedInSlot(GrabbableComponent gc)
    {
        if (gc != null) gc.Released -= OnComponentReleasedInSlot;
        _hoveringGC = null;
        if (_hasComponent) return;

        InstallComponent(gc.gameObject);
        SetColor(colorCorrect);
        delivery?.OnExplorerInstalled(this);
        Debug.Log($"[Slot {name}] Instalado al soltar: {gc.name}");
    }

    // ─────────────────────────────────────────────
    //  Instalación visual
    // ─────────────────────────────────────────────
    void InstallComponent(GameObject comp)
    {
        _hasComponent = true;
        _installed    = comp;

        Transform anchor = installAnchor != null ? installAnchor : transform;
        comp.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        comp.transform.SetParent(anchor);

        if (comp.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        comp.GetComponent<GrabbableComponent>()?.DisableGrab();

        // Ocultar el componente dañado original que este slot reemplaza
        if (damagedComponent != null)
            damagedComponent.SetActive(false);
    }

    public void ReleaseComponent()
    {
        if (_installed != null)
        {
            if (_installed.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity  = true;
            }
            _installed.GetComponent<GrabbableComponent>()?.EnableGrab();
        }
        _installed    = null;
        _hasComponent = false;
        SetColor(colorNormal);
    }

    // ─────────────────────────────────────────────
    //  Detección de tipo
    // ─────────────────────────────────────────────
    ComponentType DetectComponentType(GameObject obj)
    {
        if (obj.GetComponent<Resistor>()   != null) return ComponentType.Resistor;
        if (obj.GetComponent<LED>()        != null) return ComponentType.LED;
        if (obj.GetComponent<Capacitor>()  != null) return ComponentType.Capacitor;
        if (obj.GetComponent<ArduinoPin>() != null) return ComponentType.ArduinoPin;

        if (obj.TryGetComponent<DeskComponent>(out var desk))
            return desk.componentType;

        return ComponentType.None;
    }

    bool MatchesSlotType(ComponentType c, ComponentSlotType s) => (c, s) switch
    {
        (ComponentType.Resistor,   ComponentSlotType.Resistor)   => true,
        (ComponentType.LED,        ComponentSlotType.LED)        => true,
        (ComponentType.Capacitor,  ComponentSlotType.Capacitor)  => true,
        (ComponentType.ArduinoPin, ComponentSlotType.ArduinoPin) => true,
        _                                                         => false
    };

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    void SetColor(Color c)
    {
        if (slotRenderer == null) return;
        slotRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID, c);
        _mpb.SetColor(_colorLegacyID, c);
        slotRenderer.SetPropertyBlock(_mpb);
    }

    void ResetColor()
    {
        if (_hasComponent) return;  // no pisar colorCorrect si algo fue instalado
        SetColor(colorNormal);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = acceptedType switch
        {
            ComponentSlotType.Resistor   => Color.yellow,
            ComponentSlotType.LED        => Color.green,
            ComponentSlotType.Capacitor  => Color.blue,
            ComponentSlotType.ArduinoPin => Color.magenta,
            _                            => Color.white
        };
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
    }
}