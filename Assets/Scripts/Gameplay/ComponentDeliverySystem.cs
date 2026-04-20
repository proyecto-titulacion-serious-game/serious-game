using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sistema de entrega asimétrica de componentes.
///
/// FLUJO COMPLETO:
///   1. Técnico diagnostica el problema en su panel
///   2. Técnico selecciona el componente correcto desde su UI
///   3. Técnico pulsa "Enviar componente al Explorador"
///   4. El componente aparece físicamente en la mano del Explorador (XR Origin)
///   5. Explorador lo instala en el slot correcto del panel
///   6. GameManager valida → reto completado
///
/// Si el Técnico envía el componente EQUIVOCADO → error registrado,
/// el componente desaparece y el Técnico debe volver a diagnosticar.
/// </summary>
public class ComponentDeliverySystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public GameManager       gameManager;
    public PlayerInteraction explorerInteraction;   // mano del Explorador
    public Transform         explorerRightHand;     // posición donde aparece el componente

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
    public static event Action<ComponentType, float> OnComponentSent;      // tipo, valor
    public static event Action<bool>                 OnComponentInstalled; // éxito
    public static event Action                       OnDeliveryError;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        GameManager.OnLevelLoaded += _ => CancelPendingDelivery();
    }
    void OnDisable()
    {
        GameManager.OnLevelLoaded -= _ => CancelPendingDelivery();
    }

    // ─────────────────────────────────────────────
    //  API del Técnico — llamar desde UIButtonController
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Técnico envía una resistencia con el valor que calculó.
    /// Si el valor es incorrecto → error. Si es correcto → aparece en mano del Explorador.
    /// </summary>
    public void SendResistor(float resistanceValue)
    {
        if (_waitingForInstall)
        {
            Debug.Log("[Delivery] Ya hay un componente en tránsito. Espera que el Explorador lo instale.");
            return;
        }

        // Validar que el valor sea el correcto según el reto actual
        bool isCorrect = ValidateResistorValue(resistanceValue);

        if (!isCorrect)
        {
            gameManager?.RegisterWrongAttempt($"Resistencia incorrecta enviada: {resistanceValue}Ω");
            OnDeliveryError?.Invoke();
            Debug.Log($"[Delivery] ❌ Resistencia {resistanceValue}Ω incorrecta para este reto.");
            return;
        }

        _pendingType  = ComponentType.Resistor;
        _pendingValue = resistanceValue;
        SpawnInExplorerHand(resistorPrefab, resistanceValue);
    }

    /// <summary>El Técnico envía un LED (para corrección de polaridad en Reto 3).</summary>
    public void SendLED(bool correctPolarity = true)
    {
        if (_waitingForInstall) return;
        _pendingType = ComponentType.LED;
        SpawnInExplorerHand(ledPrefab, correctPolarity ? 1f : -1f);
    }

    /// <summary>El Técnico envía un Capacitor con polaridad correcta (Reto 3).</summary>
    public void SendCapacitor(bool correctPolarity = true)
    {
        if (_waitingForInstall) return;
        _pendingType = ComponentType.Capacitor;
        SpawnInExplorerHand(capacitorPrefab, correctPolarity ? 1f : -1f);
    }

    // ─────────────────────────────────────────────
    //  Instalación — llamar desde ComponentSlot
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Explorador coloca el componente en el slot correcto del panel.
    /// Llamar desde ComponentSlot.TryInsert() cuando el Explorador suelte el objeto.
    /// </summary>
    public void OnExplorerInstalled(ComponentSlot slot)
    {
        if (!_waitingForInstall || _spawnedComponent == null) return;

        bool success = slot.acceptedType == GetSlotTypeFor(_pendingType);

        if (success)
        {
            ApplyRepairToCircuit();
            OnComponentInstalled?.Invoke(true);
            gameManager?.RegisterRepairAction();
            Debug.Log($"[Delivery] ✅ {_pendingType} instalado correctamente.");
        }
        else
        {
            gameManager?.RegisterWrongAttempt($"Componente {_pendingType} en slot incorrecto");
            OnComponentInstalled?.Invoke(false);
            OnDeliveryError?.Invoke();
        }

        Destroy(_spawnedComponent);
        _spawnedComponent    = null;
        _waitingForInstall   = false;
        _pendingType         = ComponentType.None;
    }

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────

    void SpawnInExplorerHand(GameObject prefab, float value)
    {
        if (prefab == null || explorerRightHand == null)
        {
            Debug.LogError("[Delivery] Prefab o mano del Explorador no asignados.");
            return;
        }

        _spawnedComponent = Instantiate(prefab,
            explorerRightHand.position,
            explorerRightHand.rotation);

        // Configurar el valor del componente spawneado
        if (_spawnedComponent.TryGetComponent<Resistor>(out var r))
            r.resistance = value;

        if (_spawnedComponent.TryGetComponent<LED>(out var led))
            led.polarityInverted = value < 0;

        if (_spawnedComponent.TryGetComponent<Capacitor>(out var cap))
            cap.polarityInverted = value < 0;

        _waitingForInstall = true;
        OnComponentSent?.Invoke(_pendingType, value);

        Debug.Log($"[Delivery] 📦 {_pendingType} enviado a mano del Explorador (valor: {value})");
    }

    void ApplyRepairToCircuit()
    {
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
                    if (comp is LED led && led.polarityInverted)
                        led.polarityInverted = false;
                    break;
                case ComponentType.Capacitor:
                    if (comp is Capacitor cap && cap.polarityInverted)
                        cap.polarityInverted = false;
                    break;
            }
        }
        gameManager.circuit.MarkDirty();
    }

    bool ValidateResistorValue(float value)
    {
        foreach (var comp in gameManager.circuit.components)
            if (comp is Resistor r)
                return r.IsValueCorrect(value);
        return false;
    }

    ComponentSlotType GetSlotTypeFor(ComponentType type) => type switch
    {
        ComponentType.Resistor  => ComponentSlotType.Resistor,
        ComponentType.LED       => ComponentSlotType.LED,
        ComponentType.Capacitor => ComponentSlotType.Capacitor,
        _                       => ComponentSlotType.Resistor
    };

    void CancelPendingDelivery()
    {
        if (_spawnedComponent != null) Destroy(_spawnedComponent);
        _spawnedComponent  = null;
        _waitingForInstall = false;
        _pendingType       = ComponentType.None;
    }
}

public enum ComponentType { None, Resistor, LED, Capacitor, ArduinoPin }