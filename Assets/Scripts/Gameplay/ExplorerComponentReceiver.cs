using UnityEngine;
using System.Collections.Generic; // Necesario para usar List<>
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRGrabInteractable (soltar de la bandeja)

/// <summary>
/// Vive en la escena Explorador.unity.
/// Escucha GameSession.OnComponenteRecibido y spawna el componente físico
/// en la bandeja del Explorador para que pueda instalarlo.
/// Modificado para ACUMULAR componentes con físicas reales en la mesa.
/// </summary>
public class ExplorerComponentReceiver : MonoBehaviour
{
    [Header("Punto de spawn general (fallback si no hay slot específico)")]
    public Transform puntoDeEntrega;
    
    [Tooltip("Margen de dispersión para que los componentes no se fusionen al aparecer.")]
    public float radioDispersion = 0.08f;

    [Header("Bandeja híbrida")]
    [Tooltip("Si está activo, los componentes se 'pegan' a la bandeja (puntoDeEntrega) y viajan con " +
             "ella; al agarrarlos con la mano se sueltan para instalarlos. REQUIERE que puntoDeEntrega " +
             "tenga escala UNIFORME (el root del ComponentReceiver, NO el Tray_Visual achatado).")]
    public bool modoBandejaHibrida = true;

    [Header("Slots por tipo — arrastra los empties de la escena (opcional)")]
    [Tooltip("Si se asigna, el Resistor aparece aquí en lugar del puntoDeEntrega general.")]
    public Transform slotResistor;
    [Tooltip("Si se asigna, el LED aparece aquí.")]
    public Transform slotLED;
    [Tooltip("Si se asigna, el Capacitor aparece aquí.")]
    public Transform slotCapacitor;
    [Tooltip("Si se asigna, el ArduinoPin aparece aquí.")]
    public Transform slotArduinoPin;

    [Header("Prefabs base (fallback cuando no hay variante específica)")]
    public GameObject resistorPrefab;
    public GameObject ledPrefab;
    public GameObject capacitorPrefab;
    public GameObject arduinoPinPrefab;

    [Header("Variantes LED (opcionales — asigna los Delivered_LED_X)")]
    public GameObject ledGreenPrefab;
    public GameObject ledRedPrefab;
    public GameObject ledYellowPrefab;

    [Header("Variantes Capacitor (opcionales — asigna los Delivered_Capacitor_X)")]
    public GameObject capacitorBluePrefab;
    public GameObject capacitorBlackPrefab;
    public GameObject capacitorOrangePrefab;

    [Header("Variante Resistor (opcional — asigna Delivered_Resistor_Vertical)")]
    public GameObject resistorVerticalPrefab;

    [Header("Sistema de delivery local (para validar instalación)")]
    public ComponentDeliverySystem delivery;

    // LISTA para acumular componentes en lugar de una sola variable
    private List<GameObject> _componentesRecibidos = new List<GameObject>();
    // Último componente recibido POR TIPO → para REEMPLAZAR en vez de apilar (Retos 1-3 = 1 pieza/tipo).
    private readonly Dictionary<ComponentType, GameObject> _ultimoPorTipo = new Dictionary<ComponentType, GameObject>();

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        // Auto-asignar ComponentDeliverySystem y copiar sus prefabs si faltan
        if (delivery == null)
            delivery = FindAnyObjectByType<ComponentDeliverySystem>();

        if (delivery != null)
        {
            if (resistorPrefab   == null) resistorPrefab   = delivery.resistorPrefab;
            if (ledPrefab        == null) ledPrefab        = delivery.ledPrefab;
            if (capacitorPrefab  == null) capacitorPrefab  = delivery.capacitorPrefab;
            if (arduinoPinPrefab == null) arduinoPinPrefab = delivery.arduinoPinPrefab;
        }
        else
        {
            Debug.LogWarning("[ExplorerComponentReceiver] ComponentDeliverySystem no encontrado. " +
                             "Los componentes recibidos no podrán instalarse en el circuito.", this);
        }

        // Auto-asignar punto de entrega desde el Toolbox si no está asignado
        if (puntoDeEntrega == null)
        {
            var toolbox = FindAnyObjectByType<ToolboxController>();
            if (toolbox != null) puntoDeEntrega = toolbox.GetComponentSlot();
        }
    }

    void OnEnable()
    {
        GameSession.OnComponenteRecibido          += HandleComponenteRecibido;
        GameSession.OnRetoChanged                 += HandleRetoChanged;
        GameSession.OnCableFixed                  += HandleCableFixed;
        ComponentSendingTray.OnComponentSentLocal += HandleComponenteRecibidoLocal;
    }

    void OnDisable()
    {
        GameSession.OnComponenteRecibido          -= HandleComponenteRecibido;
        GameSession.OnRetoChanged                 -= HandleRetoChanged;
        GameSession.OnCableFixed                  -= HandleCableFixed;
        ComponentSendingTray.OnComponentSentLocal -= HandleComponenteRecibidoLocal;
    }

    // ─────────────────────────────────────────────
    //  Handlers
    // ─────────────────────────────────────────────

    // GameSession (multijugador) — sin info de variante, usa prefab base
    void HandleComponenteRecibido(ComponentType tipo, float valor)
        => SpawnComponente(tipo, valor, null);

    // OnComponentSentLocal (editor/offline) — misma firma que el evento Action<ComponentType, float>
    void HandleComponenteRecibidoLocal(ComponentType tipo, float valor)
        => SpawnComponente(tipo, valor, null);

    void SpawnComponente(ComponentType tipo, float valor, GameObject prefabOverride)
    {
        // Reto 4: el Arduino YA NO se entrega como componente físico. Es un objeto fijo en la
        // escena y se programa por código (Técnico → ArduinoNetworkBridge → ArduinoCore), sus
        // pines se conectan con cables (CableBox + ProtoboardConnector). Ignoramos cualquier
        // ArduinoPin que llegue por el canal de entrega (legacy del paradigma lineal de retos 1-3).
        if (tipo == ComponentType.ArduinoPin)
        {
            Debug.LogWarning("[Receiver] ArduinoPin ignorado: el Arduino no se entrega como " +
                             "componente; se programa por el bridge y se conecta con cables.", this);
            return;
        }

        // ¡ELIMINADO EL DESTROY AQUÍ PARA PERMITIR ACUMULACIÓN!

        // If delivery already spawned a ghost (from the delivery path in ComponentSendingTray),
        // cancel it so we spawn at the correct Explorer location instead.
        if (delivery != null && delivery.HasPendingDelivery())
            delivery.CancelDelivery();

        // Prioridad: prefab enviado desde el Técnico → variante específica → prefab base.
        GameObject prefab = prefabOverride != null ? prefabOverride : SeleccionarPrefab(tipo, valor);

        Transform slot = tipo switch
        {
            ComponentType.Resistor   => slotResistor   != null ? slotResistor   : puntoDeEntrega,
            ComponentType.LED        => slotLED        != null ? slotLED        : puntoDeEntrega,
            ComponentType.Capacitor  => slotCapacitor  != null ? slotCapacitor  : puntoDeEntrega,
            ComponentType.ArduinoPin => slotArduinoPin != null ? slotArduinoPin : puntoDeEntrega,
            _                        => puntoDeEntrega
        };

        // Si no hay slot ni puntoDeEntrega asignados, resolver uno seguro (protoboard/cámara)
        // en vez de no spawnear o caer fuera del mapa.
        if (slot == null)
            slot = puntoDeEntrega = ComponentDeliverySystem.ResolverPuntoEntregaSeguro(transform);

        if (prefab == null || slot == null)
        {
            Debug.LogWarning($"[Receiver] Prefab o punto de entrega no asignado para {tipo}.");
            return;
        }

        // REEMPLAZAR el componente anterior del mismo tipo: en los Retos 1-3 solo hay 1 pieza por
        // tipo, así que reenviar no debe apilar objetos en la mesa. (Tipos distintos coexisten.)
        if (_ultimoPorTipo.TryGetValue(tipo, out var previo) && previo != null)
        {
            _componentesRecibidos.Remove(previo);
            Destroy(previo);
        }

        // Crear un ligero desfase aleatorio para que no colisionen violentamente
        Vector3 offsetAleatorio = new Vector3(
            Random.Range(-radioDispersion, radioDispersion),
            0.05f, // Aparece un poquito arriba de la mesa para caer con gravedad
            Random.Range(-radioDispersion, radioDispersion)
        );

        Vector3 posicionSpawn = slot.position + offsetAleatorio;

        GameObject nuevoComponente = Instantiate(prefab, posicionSpawn, slot.rotation);

        // Agregar a nuestra lista de control + registrar como el actual de su tipo.
        _componentesRecibidos.Add(nuevoComponente);
        _ultimoPorTipo[tipo] = nuevoComponente;

        ConfigurarComponente(nuevoComponente, tipo, valor);

        bool tieneRb = nuevoComponente.TryGetComponent<Rigidbody>(out var rb);

        if (modoBandejaHibrida)
        {
            // ── BANDEJA HÍBRIDA ───────────────────────────────────────────────
            // El componente se "sostiene" en la bandeja: se emparenta al punto de entrega y queda
            // kinematic → VIAJA con la caja cuando el Explorador la mueve. Al agarrarlo con la mano
            // (XRGrabInteractable) se suelta (un-parent) y pasa a física para poder instalarlo.
            AdvertirSiEscalaNoUniforme(slot);
            nuevoComponente.transform.SetParent(slot, worldPositionStays: true);
            if (tieneRb) { rb.isKinematic = true; rb.useGravity = false; }

            var grab = nuevoComponente.GetComponentInChildren<XRGrabInteractable>(true);
            if (grab != null)
            {
                grab.retainTransformParent = false;   // que XRI no lo re-pegue a la bandeja al soltar
                grab.selectEntered.AddListener(_ =>
                {
                    // Solo des-emparentar de la bandeja. NO tocar isKinematic/useGravity aquí:
                    // el XRGrabInteractable es MovementType.Kinematic y gestiona el kinematic durante
                    // el agarre. Forzar no-kinemático + gravedad hacía que la pieza CAYERA y temblara
                    // ("epilepsia") al moverla. La gravedad post-soltar la pone GrabbableComponent.
                    nuevoComponente.transform.SetParent(null, worldPositionStays: true);
                });
            }
        }
        else if (tieneRb)
        {
            // Modo clásico: cae por gravedad y descansa por física sobre la bandeja.
            rb.isKinematic = false;
            rb.useGravity  = true;
        }

        // Collision continua + interpolación: no atravesar la bandeja fina ni temblar.
        if (tieneRb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;
        }

        delivery?.PrepareForInstall(tipo, valor);

        Debug.Log($"[Receiver] Componente recibido y acumulado: {tipo} ({(prefabOverride != null ? prefabOverride.name : "base")}) = {valor}");
    }

    void HandleRetoChanged(int reto)
    {
        // Limpiamos toda la mesa destruyendo todos los componentes acumulados
        foreach (var comp in _componentesRecibidos)
        {
            if (comp != null)
            {
                Destroy(comp);
            }
        }
        
        // Vaciamos la lista para el nuevo reto
        _componentesRecibidos.Clear();
        _ultimoPorTipo.Clear();
        Debug.Log("[Receiver] Mesa limpiada para el nuevo reto.");
    }

    /// <summary>
    /// Elige el prefab a instanciar: usa la VARIANTE específica si está asignada, si no el base.
    /// Nota: el evento de entrega (ComponentType, valor) no transporta color, así que se usa una
    /// variante por defecto coherente (LED verde = pieza sana entregada; capacitor azul). Las demás
    /// variantes (rojo/amarillo, negro/naranja, resistor vertical) quedan listas para cuando el
    /// Técnico envíe el color en el RPC (ampliar GameSession.OnComponenteRecibido con un parámetro).
    /// </summary>
    GameObject SeleccionarPrefab(ComponentType tipo, float valor)
    {
        switch (tipo)
        {
            case ComponentType.Resistor:
                return resistorPrefab;
            case ComponentType.LED:
                return ledGreenPrefab      != null ? ledGreenPrefab      : ledPrefab;
            case ComponentType.Capacitor:
                return capacitorBluePrefab != null ? capacitorBluePrefab : capacitorPrefab;
            case ComponentType.ArduinoPin:
                return arduinoPinPrefab;
            default:
                return null;
        }
    }

    /// <summary>Avisa si el punto de entrega tiene escala no uniforme (deformaría a los hijos).</summary>
    static void AdvertirSiEscalaNoUniforme(Transform t)
    {
        Vector3 s = t.lossyScale;
        if (Mathf.Abs(s.x - s.y) > 0.01f || Mathf.Abs(s.x - s.z) > 0.01f)
            Debug.LogWarning($"[Receiver] El punto de entrega '{t.name}' tiene escala NO uniforme {s} → " +
                             "los componentes emparentados se deformarán. Usa el ROOT del ComponentReceiver " +
                             "(escala 1,1,1), no el Tray_Visual achatado.", t);
    }

    // ─────────────────────────────────────────────
    //  Configuración del prefab instanciado
    // ─────────────────────────────────────────────

    // Reto 4: el Técnico reparó el cable — propagar al circuito del Explorador
    void HandleCableFixed()
    {
        var circuit = FindAnyObjectByType<CircuitManager>();
        if (circuit == null) return;

        foreach (var comp in circuit.components)
        {
            if (comp is ArduinoPin pin && pin.hasLooseCable)
            {
                pin.FixLooseCable();
                circuit.MarkDirty();
                Debug.Log("[Receiver] Cable suelto reparado remotamente (Reto 4).");
                return;
            }
        }
    }

    void ConfigurarComponente(GameObject obj, ComponentType tipo, float valor)
    {
        // Reto 4: garantizar ProtoboardConnector en el componente recibido por red,
        // si no el CircuitSimulator nunca lo engancha a los nodos de la protoboard.
        ProtoboardConnector.EnsureOn(obj);

        switch (tipo)
        {
            case ComponentType.Resistor:
                if (obj.TryGetComponent<Resistor>(out var r))
                {
                    r.resistance = valor;
                    r.hasFault   = false;
                }
                break;
            case ComponentType.LED:
                if (obj.TryGetComponent<LED>(out var led))
                    led.polarityInverted = valor < 0;
                break;
            case ComponentType.Capacitor:
                if (obj.TryGetComponent<Capacitor>(out var cap))
                    cap.polarityInverted = valor < 0;
                break;
            case ComponentType.ArduinoPin:
                if (obj.TryGetComponent<ArduinoPin>(out var pin))
                    pin.pinNumber = (int)valor;
                break;
        }
    }
}