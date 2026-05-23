using UnityEngine;

/// <summary>
/// Vive en la escena Explorador.unity.
/// Escucha GameSession.OnComponenteRecibido y spawna el componente físico
/// en la bandeja del Explorador para que pueda instalarlo.
///
/// SETUP EN UNITY (escena Explorador):
///   1. Añadir este script a cualquier GameObject (ej. "ComponentReceiver").
///   2. Asignar puntoDeEntrega → Transform de la Bandeja_Recepcion del Explorador.
///   3. Asignar los prefabs de componentes (mismos que usa ComponentDeliverySystem).
///   4. Asignar delivery → ComponentDeliverySystem de la escena (para OnExplorerInstalled).
/// </summary>
public class ExplorerComponentReceiver : MonoBehaviour
{
    [Header("Punto de spawn general (fallback si no hay slot específico)")]
    public Transform puntoDeEntrega;

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

    private GameObject _componenteActual;

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
        ComponentSendingTray.OnComponentSentLocal += HandleComponenteRecibidoLocal;
    }

    void OnDisable()
    {
        GameSession.OnComponenteRecibido          -= HandleComponenteRecibido;
        GameSession.OnRetoChanged                 -= HandleRetoChanged;
        ComponentSendingTray.OnComponentSentLocal -= HandleComponenteRecibidoLocal;
    }

    // ─────────────────────────────────────────────
    //  Handlers
    // ─────────────────────────────────────────────

    // GameSession (multijugador) — sin info de variante, usa prefab base
    void HandleComponenteRecibido(ComponentType tipo, float valor)
        => SpawnComponente(tipo, valor, null);

    // OnComponentSentLocal (editor/local) — lleva el prefab de variante directamente
    void HandleComponenteRecibidoLocal(ComponentType tipo, float valor, GameObject prefabOverride)
        => SpawnComponente(tipo, valor, prefabOverride);

    void SpawnComponente(ComponentType tipo, float valor, GameObject prefabOverride)
    {
        if (_componenteActual != null)
            Destroy(_componenteActual);

        // If delivery already spawned a ghost (from the delivery path in ComponentSendingTray),
        // cancel it so we spawn at the correct Explorer location instead.
        if (delivery != null && delivery.HasPendingDelivery())
            delivery.CancelDelivery();

        // Prioridad: prefab enviado desde el Técnico → prefab base asignado en Inspector
        GameObject prefab = prefabOverride != null ? prefabOverride : tipo switch
        {
            ComponentType.Resistor   => resistorPrefab,
            ComponentType.LED        => ledPrefab,
            ComponentType.Capacitor  => capacitorPrefab,
            ComponentType.ArduinoPin => arduinoPinPrefab,
            _                        => null
        };

        Transform slot = tipo switch
        {
            ComponentType.Resistor   => slotResistor   != null ? slotResistor   : puntoDeEntrega,
            ComponentType.LED        => slotLED         != null ? slotLED         : puntoDeEntrega,
            ComponentType.Capacitor  => slotCapacitor   != null ? slotCapacitor   : puntoDeEntrega,
            ComponentType.ArduinoPin => slotArduinoPin  != null ? slotArduinoPin  : puntoDeEntrega,
            _                        => puntoDeEntrega
        };

        if (prefab == null || slot == null)
        {
            Debug.LogWarning($"[Receiver] Prefab o punto de entrega no asignado para {tipo}.");
            return;
        }

        _componenteActual = Instantiate(prefab, slot.position, slot.rotation, slot);
        ConfigurarComponente(_componenteActual, tipo, valor);

        if (_componenteActual.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        delivery?.PrepareForInstall(tipo, valor);

        Debug.Log($"[Receiver] Componente recibido: {tipo} ({(prefabOverride != null ? prefabOverride.name : "base")}) = {valor}");
    }

    void HandleRetoChanged(int reto)
    {
        if (_componenteActual != null)
        {
            Destroy(_componenteActual);
            _componenteActual = null;
        }
    }

    // ─────────────────────────────────────────────
    //  Configuración del prefab instanciado
    // ─────────────────────────────────────────────

    void ConfigurarComponente(GameObject obj, ComponentType tipo, float valor)
    {
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
