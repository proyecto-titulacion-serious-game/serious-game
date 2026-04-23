using UnityEngine;

/// <summary>
/// Tipo de componente que acepta un slot.
/// Debe coincidir con el ComponentType enviado por el Técnico.
/// </summary>
public enum ComponentSlotType
{
    Resistor,
    LED,
    Capacitor,
    ArduinoPin
}

/// <summary>
/// Slot físico donde el Explorador instala un componente recibido del Técnico.
///
/// SETUP en Unity:
///   1. Crear Empty Object (o Cube pequeño) en Zona_Circuito del Explorador
///   2. Renombrar: Slot_Resistor, Slot_LED, Slot_Capacitor, Slot_Arduino
///   3. Agregar BoxCollider → isTrigger = TRUE
///   4. Agregar este script → configurar acceptedType
///
/// FLUJO:
///   1. Explorador lleva componente al slot con mano VR
///   2. OnTriggerEnter detecta el componente
///   3. Valida el tipo → avisa al DeliverySystem
///   4. DeliverySystem aplica la reparación al Circuit
/// </summary>
[RequireComponent(typeof(Collider))]
public class ComponentSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Tipo aceptado")]
    [Tooltip("Qué tipo de componente acepta este slot.")]
    public ComponentSlotType acceptedType = ComponentSlotType.Resistor;

    [Header("Referencias")]
    public ComponentDeliverySystem delivery;

    [Header("Feedback visual")]
    public Renderer slotRenderer;
    public Color colorNormal  = new Color(0.3f, 0.3f, 0.3f);
    public Color colorHover   = new Color(0.9f, 0.9f, 0.2f);
    public Color colorCorrect = new Color(0.2f, 0.9f, 0.3f);
    public Color colorWrong   = new Color(0.9f, 0.2f, 0.2f);

    [Header("Anclaje al instalar")]
    [Tooltip("Transform donde se posiciona visualmente el componente al ser instalado.")]
    public Transform installAnchor;

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool       _hasComponent = false;
    [SerializeField] private GameObject _installed;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID = Shader.PropertyToID("_Color");

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (slotRenderer == null) slotRenderer = GetComponent<Renderer>();
        if (delivery     == null) delivery     = FindObjectOfType<ComponentDeliverySystem>();

        // Asegurar que el collider sea trigger
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log($"[Slot {name}] Collider configurado como Trigger automáticamente.");
        }

        SetColor(colorNormal);
    }

    // ─────────────────────────────────────────────
    //  Trigger — detecta componentes
    // ─────────────────────────────────────────────

    /// <summary>Al entrar un componente en el slot: validar e instalar.</summary>
    void OnTriggerEnter(Collider other)
    {
        if (_hasComponent) return;

        ComponentType incomingType = DetectComponentType(other.gameObject);
        if (incomingType == ComponentType.None) return;

        bool typeMatches = MatchesSlotType(incomingType, acceptedType);

        if (typeMatches)
        {
            InstallComponent(other.gameObject);
            SetColor(colorCorrect);
            Debug.Log($"[Slot {name}] ✓ {incomingType} instalado correctamente.");
        }
        else
        {
            SetColor(colorWrong);
            Debug.Log($"[Slot {name}] ✗ {incomingType} no encaja aquí. Este slot es para {acceptedType}.");
        }

        // Notificar al DeliverySystem
        delivery?.OnExplorerInstalled(this);
    }

    /// <summary>Feedback de hover mientras el componente está sobre el slot.</summary>
    void OnTriggerStay(Collider other)
    {
        if (_hasComponent) return;
        if (DetectComponentType(other.gameObject) == ComponentType.None) return;
        SetColor(colorHover);
    }

    /// <summary>Restaura color al salir sin instalar.</summary>
    void OnTriggerExit(Collider other)
    {
        if (_hasComponent) return;
        SetColor(colorNormal);
    }

    // ─────────────────────────────────────────────
    //  Instalación visual
    // ─────────────────────────────────────────────

    /// <summary>Ancla el componente al slot y desactiva su física.</summary>
    void InstallComponent(GameObject comp)
    {
        _hasComponent = true;
        _installed    = comp;

        Transform anchor = installAnchor != null ? installAnchor : transform;
        comp.transform.position = anchor.position;
        comp.transform.rotation = anchor.rotation;
        comp.transform.SetParent(anchor);

        if (comp.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    /// <summary>Libera el componente (si el Explorador decide quitarlo).</summary>
    public void ReleaseComponent()
    {
        if (_installed != null && _installed.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
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
        // Prioridad: scripts del circuito sobre DeskComponent
        if (obj.GetComponent<Resistor>()   != null) return ComponentType.Resistor;
        if (obj.GetComponent<LED>()        != null) return ComponentType.LED;
        if (obj.GetComponent<Capacitor>()  != null) return ComponentType.Capacitor;
        if (obj.GetComponent<ArduinoPin>() != null) return ComponentType.ArduinoPin;

        // Fallback: DeskComponent (los componentes de la mesa del Técnico)
        if (obj.TryGetComponent<DeskComponent>(out var desk))
            return desk.componentType;

        return ComponentType.None;
    }

    bool MatchesSlotType(ComponentType compType, ComponentSlotType slotType) => (compType, slotType) switch
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
        slotRenderer.SetPropertyBlock(_mpb);
    }

    // ─────────────────────────────────────────────
    //  Debug visual en Scene
    // ─────────────────────────────────────────────
    void OnDrawGizmos()
    {
        Gizmos.color = acceptedType switch
        {
            ComponentSlotType.Resistor   => Color.yellow,
            ComponentSlotType.LED        => Color.green,
            ComponentSlotType.Capacitor  => Color.blue,
            ComponentSlotType.ArduinoPin => Color.magenta,
            _                             => Color.white
        };

        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        else if (col is SphereCollider sph)
            Gizmos.DrawWireSphere(transform.position + sph.center, sph.radius * transform.lossyScale.x);
    }
}