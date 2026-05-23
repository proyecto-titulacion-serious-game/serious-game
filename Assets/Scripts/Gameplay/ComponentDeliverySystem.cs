using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sistema de entrega asimétrica de componentes — VERSIÓN CORRECTA.
///
/// DISEÑO CLAVE (estilo Keep Talking and Nobody Explodes):
///   - El Técnico SIEMPRE envía lo que seleccionó, sin validación previa
///   - El componente físico SIEMPRE aparece en la bandeja del Explorador
///   - El Explorador SIEMPRE puede instalarlo
///   - La validación ocurre al aplicar al circuito:
///     * Valor correcto → circuito se repara, LED cambia a verde
///     * Valor incorrecto → circuito sigue dañado o empeora
///   - Los jugadores deben colaborar para diagnosticar qué salió mal
///
/// Esto preserva el gameplay asimétrico real: el Técnico puede equivocarse,
/// el Explorador ve las consecuencias, y entre los dos deben resolverlo.
/// </summary>
public class ComponentDeliverySystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    public GameManager gameManager;

    [Tooltip("Transform de la Bandeja_Recepcion del Explorador donde aparecen los componentes.")]
    public Transform puntoDeEntrega;

    [Header("Prefabs de componentes (arrastrar desde Project)")]
    public GameObject resistorPrefab;
    public GameObject ledPrefab;
    public GameObject capacitorPrefab;
    public GameObject arduinoPinPrefab;

    [Header("Estado (solo lectura)")]
    [SerializeField] private ComponentType _pendingType = ComponentType.None;
    [SerializeField] private float         _pendingValue = 0f;
    [SerializeField] private bool          _waitingForInstall = false;
    [SerializeField] private GameObject    _spawnedComponent;

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    public static event Action<ComponentType, float> OnComponentSent;       // tipo, valor enviado
    public static event Action<bool>                 OnComponentInstalled;  // slot correcto/incorrecto
    public static event Action<bool>                 OnRepairValidated;     // circuito realmente reparado
    public static event Action<string>               OnDeliveryError;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (puntoDeEntrega == null)
        {
            var tray = GameObject.Find("Bandeja_Recepcion");
            if (tray != null)
            {
                puntoDeEntrega = tray.transform;
            }
            else
            {
                var toolbox = FindAnyObjectByType<ToolboxController>();
                if (toolbox != null) puntoDeEntrega = toolbox.GetComponentSlot();
            }

            if (puntoDeEntrega == null)
            {
                var fallback = new GameObject("PuntoDeEntrega_Fallback");
                fallback.transform.SetParent(transform);
                fallback.transform.localPosition = Vector3.up * 0.5f;
                puntoDeEntrega = fallback.transform;
                Debug.LogWarning("[Delivery] Bandeja_Recepcion no encontrada. " +
                                 "Los componentes aparecerán junto al ComponentDeliverySystem. " +
                                 "Crea un GameObject 'Bandeja_Recepcion' en la escena del Explorador.");
            }
        }
    }

    void OnEnable()  { GameManager.OnLevelLoaded += OnLevelLoaded; }
    void OnDisable() { GameManager.OnLevelLoaded -= OnLevelLoaded; }

    void OnLevelLoaded(LevelType _) => CancelPendingDelivery();

    // ─────────────────────────────────────────────
    //  API pública del Técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Técnico envía una resistencia con CUALQUIER valor.
    /// NO se valida aquí — el Explorador recibirá el componente igualmente.
    /// La validación real sucede cuando se instala en un slot.
    /// </summary>
    public void SendResistor(float resistanceValue, GameObject prefabOverride = null)
    {
        if (_waitingForInstall)
        {
            Debug.Log("[Delivery] Ya hay un componente en tránsito.");
            return;
        }

        _pendingType  = ComponentType.Resistor;
        _pendingValue = resistanceValue;
        SpawnInDeliveryTray(prefabOverride != null ? prefabOverride : resistorPrefab, resistanceValue);
    }

    public void SendLED(bool correctPolarity = true, GameObject prefabOverride = null)
    {
        if (_waitingForInstall) return;
        _pendingType  = ComponentType.LED;
        _pendingValue = correctPolarity ? 1f : -1f;
        SpawnInDeliveryTray(prefabOverride != null ? prefabOverride : ledPrefab, _pendingValue);
    }

    public void SendCapacitor(bool correctPolarity = true, GameObject prefabOverride = null)
    {
        if (_waitingForInstall) return;
        _pendingType  = ComponentType.Capacitor;
        _pendingValue = correctPolarity ? 1f : -1f;
        SpawnInDeliveryTray(prefabOverride != null ? prefabOverride : capacitorPrefab, _pendingValue);
    }

    public void SendArduinoPin(int pinNumber, GameObject prefabOverride = null)
    {
        if (_waitingForInstall) return;
        _pendingType  = ComponentType.ArduinoPin;
        _pendingValue = pinNumber;
        SpawnInDeliveryTray(prefabOverride != null ? prefabOverride : arduinoPinPrefab, pinNumber);
    }

    // ─────────────────────────────────────────────
    //  Callback desde ComponentSlot
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado por ComponentSlot cuando el Explorador instala un componente.
    ///
    /// Hay DOS niveles de validación:
    ///   1. Tipo de slot: ¿el componente va en el slot correcto?
    ///   2. Valor: ¿el valor/polaridad es el que pide el reto?
    /// </summary>
    public void OnExplorerInstalled(ComponentSlot slot)
    {
        if (!_waitingForInstall)
        {
            Debug.Log("[Delivery] No hay componente en tránsito para instalar.");
            return;
        }

        // Validación 1: ¿Slot correcto?
        bool slotCorrect = slot.acceptedType == GetSlotTypeFor(_pendingType);

        if (!slotCorrect)
        {
            OnComponentInstalled?.Invoke(false);
            OnDeliveryError?.Invoke($"Componente {_pendingType} no encaja en slot {slot.acceptedType}");
            gameManager?.RegisterWrongAttempt($"Slot incorrecto: {_pendingType} en {slot.acceptedType}");
            Debug.Log($"[Delivery] ✗ Slot incorrecto");

            Destroy(_spawnedComponent);
            ResetDeliveryState();
            return;
        }

        // Slot correcto — el componente ENTRA físicamente.
        OnComponentInstalled?.Invoke(true);

        // Validación 2: ¿Valor correcto para reparar el circuito?
        bool valueCorrect = ValidateValueForRepair();

        if (valueCorrect)
        {
            // Deshabilitar el script eléctrico del componente entregado para que
            // el rescan de RegisterRepairAction no lo cuente como segundo componente
            // del circuito (el circuito se actualiza sobre el componente original de la escena).
            if (slot.InstalledObject != null &&
                slot.InstalledObject.TryGetComponent<ElectricalComponent>(out var ec))
                ec.enabled = false;

            ApplyRepairToCircuit();
            OnRepairValidated?.Invoke(true);
            gameManager?.RegisterRepairAction();
            Debug.Log($"[Delivery] ✓ {_pendingType} ({_pendingValue}) instalado y circuito reparado.");
        }
        else
        {
            // Componente instalado pero con VALOR incorrecto.
            // El circuito NO se repara — el Explorador ve el resultado (LED sigue rojo).
            OnRepairValidated?.Invoke(false);
            OnDeliveryError?.Invoke($"Valor incorrecto instalado: {_pendingType} = {_pendingValue}");
            gameManager?.RegisterWrongAttempt($"Valor incorrecto: {_pendingType} = {_pendingValue}");
            Debug.Log($"[Delivery] ⚠ {_pendingType} instalado con valor incorrecto. Circuito no reparado.");

            // Aplicar el valor erróneo al circuito para que se vea el efecto
            ApplyIncorrectValueToCircuit();
        }

        if (_spawnedComponent != null)
            Destroy(_spawnedComponent);

        ResetDeliveryState();
    }

    // ─────────────────────────────────────────────
    //  Spawn local
    // ─────────────────────────────────────────────

    void SpawnInDeliveryTray(GameObject prefab, float value)
    {
        if (prefab == null)
        {
            Debug.LogError("[Delivery] Prefab no asignado.");
            return;
        }
        if (puntoDeEntrega == null)
        {
            Debug.LogError("[Delivery] PuntoDeEntrega no asignado.");
            return;
        }

        _spawnedComponent = Instantiate(
            prefab,
            puntoDeEntrega.position,
            puntoDeEntrega.rotation,
            puntoDeEntrega
        );

        ConfigureSpawnedComponent(_spawnedComponent, value);

        _waitingForInstall = true;
        OnComponentSent?.Invoke(_pendingType, value);

        Debug.Log($"[Delivery] {_pendingType} ({value}) enviado al Explorador.");
    }

    /// <summary>
    /// Aplica el valor/polaridad al componente físico spawneado.
    /// Así el componente visible en la bandeja REFLEJA lo que envió el Técnico.
    /// </summary>
    void ConfigureSpawnedComponent(GameObject comp, float value)
    {
        if (comp.TryGetComponent<Resistor>(out var r))
        {
            r.resistance = value;
            r.hasFault   = false;
        }

        if (comp.TryGetComponent<LED>(out var led))
            led.polarityInverted = value < 0;

        if (comp.TryGetComponent<Capacitor>(out var cap))
            cap.polarityInverted = value < 0;

        if (comp.TryGetComponent<ArduinoPin>(out var pin))
            pin.pinNumber = (int)value;
    }

    // ─────────────────────────────────────────────
    //  Validación del valor (post-instalación)
    // ─────────────────────────────────────────────

    bool ValidateValueForRepair()
    {
        if (gameManager?.circuit == null) return false;

        switch (_pendingType)
        {
            case ComponentType.Resistor:
                foreach (var c in gameManager.circuit.components)
                    if (c is Resistor r)
                        return r.IsValueCorrect(_pendingValue);
                return false;

            case ComponentType.LED:
                return _pendingValue >= 0; // polaridad correcta

            case ComponentType.Capacitor:
                return _pendingValue >= 0; // polaridad correcta

            case ComponentType.ArduinoPin:
                foreach (var c in gameManager.circuit.components)
                    if (c is ArduinoPin pin)
                        return (int)_pendingValue == pin.correctPinNumber;
                return false;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  Aplicar cambios al Circuit
    // ─────────────────────────────────────────────

    void ApplyRepairToCircuit()
    {
        if (gameManager?.circuit == null) return;

        foreach (var comp in gameManager.circuit.components)
        {
            switch (_pendingType)
            {
                case ComponentType.Resistor:
                    if (comp is Resistor res && res.hasFault)
                    {
                        res.resistance = _pendingValue;
                        res.hasFault   = false;
                    }
                    break;
                case ComponentType.LED:
                    if (comp is LED led)
                    {
                        if (led.polarityInverted) led.polarityInverted = false;
                        // Valor != ±1f → es una resistencia de reemplazo (Reto 2)
                        if (Mathf.Abs(_pendingValue) != 1f && _pendingValue > 0f)
                            led.resistance = _pendingValue;
                    }
                    break;
                case ComponentType.Capacitor:
                    if (comp is Capacitor cap && cap.polarityInverted)
                        cap.polarityInverted = false;
                    break;
                case ComponentType.ArduinoPin:
                    if (comp is ArduinoPin pin)
                    {
                        pin.pinNumber = (int)_pendingValue;
                        pin.hasFault  = false;
                    }
                    break;
            }
        }

        gameManager.circuit.MarkDirty();
    }

    /// <summary>
    /// Aplica el valor INCORRECTO al circuito para que el Explorador
    /// vea el efecto visual — LED sigue rojo, humo, sobrecarga, etc.
    /// El circuito NO se marca como reparado.
    /// </summary>
    void ApplyIncorrectValueToCircuit()
    {
        if (gameManager?.circuit == null) return;

        foreach (var comp in gameManager.circuit.components)
        {
            switch (_pendingType)
            {
                case ComponentType.Resistor:
                    if (comp is Resistor res)
                    {
                        // Valor erróneo aplicado — el circuito lo simulará
                        // y el Explorador verá la consecuencia
                        res.resistance = _pendingValue;
                        // hasFault permanece en true (sigue siendo incorrecto)
                    }
                    break;
                case ComponentType.LED:
                    if (comp is LED led)
                        led.polarityInverted = _pendingValue < 0;
                    break;
                case ComponentType.Capacitor:
                    if (comp is Capacitor cap)
                        cap.polarityInverted = _pendingValue < 0;
                    break;
                case ComponentType.ArduinoPin:
                    if (comp is ArduinoPin pin)
                        pin.pinNumber = (int)_pendingValue;
                    break;
            }
        }

        gameManager.circuit.MarkDirty();
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    ComponentSlotType GetSlotTypeFor(ComponentType type) => type switch
    {
        ComponentType.Resistor   => ComponentSlotType.Resistor,
        ComponentType.LED        => ComponentSlotType.LED,
        ComponentType.Capacitor  => ComponentSlotType.Capacitor,
        ComponentType.ArduinoPin => ComponentSlotType.ArduinoPin,
        _                         => ComponentSlotType.Resistor
    };

    void ResetDeliveryState()
    {
        _spawnedComponent  = null;
        _waitingForInstall = false;
        _pendingType       = ComponentType.None;
        _pendingValue      = 0f;
    }

    void CancelPendingDelivery()
    {
        if (_spawnedComponent != null)
            Destroy(_spawnedComponent);
        ResetDeliveryState();
    }

    // ─────────────────────────────────────────────
    //  API pública adicional
    // ─────────────────────────────────────────────

    public bool HasPendingDelivery() => _waitingForInstall;

    public void CancelDelivery()
    {
        if (!_waitingForInstall) return;
        CancelPendingDelivery();
        Debug.Log("[Delivery] Envío cancelado por el Técnico.");
    }

    /// <summary>
    /// Llamado por ExplorerComponentReceiver cuando llega un componente desde la red.
    /// Prepara el estado local sin spawnear (el objeto ya fue instanciado por el Receiver).
    /// </summary>
    public void PrepareForInstall(ComponentType tipo, float valor)
    {
        _pendingType       = tipo;
        _pendingValue      = valor;
        _waitingForInstall = true;
        _spawnedComponent  = null;   // gestionado por el Receiver
    }
}

public enum ComponentType
{
    None,
    Resistor,
    LED,
    Capacitor,
    ArduinoPin
}