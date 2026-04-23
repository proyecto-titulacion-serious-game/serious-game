using System;
using System.Collections;
using UnityEngine;
using Fusion; // 1. AÑADIDO: Necesario para los comandos de red

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
public class ComponentDeliverySystemBackup : NetworkBehaviour // 2. CAMBIADO: De MonoBehaviour a NetworkBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    
    private NetworkRunner _manualRunner;
    
    [Header("Referencias")]
    public GameManager   gameManager;
    
    
    // 3. CAMBIADO: Reemplazamos la mano del explorador por la bandeja fija
    [Tooltip("El objeto vacío sobre la mesa donde aparecerán los componentes")]
    public Transform     puntoDeEntrega; 

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

    public void SendResistor(float resistanceValue)
    {
        if (_waitingForInstall)
        {
            Debug.Log("[Delivery] Ya hay un componente en tránsito.");
            return;
        }

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
        SpawnInDeliveryTray(resistorPrefab, resistanceValue);
    }

    public void SendLED(bool correctPolarity = true)
    {
        if (_waitingForInstall) return;
        _pendingType = ComponentType.LED;
        SpawnInDeliveryTray(ledPrefab, correctPolarity ? 1f : -1f);
    }

    public void SendCapacitor(bool correctPolarity = true)
    {
        if (_waitingForInstall) return;
        _pendingType = ComponentType.Capacitor;
        SpawnInDeliveryTray(capacitorPrefab, correctPolarity ? 1f : -1f);
    }

    // ─────────────────────────────────────────────
    //  Instalación — llamar desde ComponentSlot
    // ─────────────────────────────────────────────

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

        // 4. CAMBIADO: Usamos Despawn en red en lugar de Destroy local
        if (Runner != null && _spawnedComponent.TryGetComponent<NetworkObject>(out var netObj))
        {
            Runner.Despawn(netObj);
        }
        else
        {
            Destroy(_spawnedComponent); // Respaldo por si se prueba sin red
        }
        
        _spawnedComponent    = null;
        _waitingForInstall   = false;
        _pendingType         = ComponentType.None;
    }

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────

    public void InicializarManual(NetworkRunner runnerActivo)
    {
        _manualRunner = runnerActivo;
        Debug.Log("[Delivery] Runner sincronizado manualmente.");
    }

    void SpawnInDeliveryTray(GameObject prefab, float value)
    {
        // 1. Verificación de seguridad: ¿Tenemos el motor de red listo? 🏎️
        if (_manualRunner == null)
        {
            Debug.LogWarning("[Delivery] El sistema aún no está vinculado al NetworkRunner. Reintenta en un segundo.");
            return;
        }

        // 2. Verificación de referencias: ¿Tenemos qué enviar y dónde ponerlo? 📦
        if (prefab == null || puntoDeEntrega == null)
        {
            Debug.LogError("[Delivery] Prefab o punto de entrega no asignados en el Inspector.");
            return;
        }

        // 3. Log de diagnóstico (opcional para tu tranquilidad) 🔍
        Debug.Log($"[Check] ManualRunner: {_manualRunner != null}, Generando: {prefab.name}");

        // 4. Instanciación en red usando nuestro runner manual 🌐
        NetworkObject objRed = _manualRunner.Spawn(
            prefab,
            puntoDeEntrega.position,
            puntoDeEntrega.rotation,
            _manualRunner.LocalPlayer
        );

        _spawnedComponent = objRed.gameObject;

        // 5. Configuración de la lógica del componente (Resistencia, LED o Capacitor) ⚡
        if (_spawnedComponent.TryGetComponent<Resistor>(out var r))
            r.resistance = value;

        if (_spawnedComponent.TryGetComponent<LED>(out var led))
            led.polarityInverted = value < 0;

        if (_spawnedComponent.TryGetComponent<Capacitor>(out var cap))
            cap.polarityInverted = value < 0;

        // 6. Estado del sistema
        _waitingForInstall = true;
        OnComponentSent?.Invoke(_pendingType, value);

        Debug.Log($"[Delivery] 📦 {_pendingType} enviado con éxito (Valor/Polaridad: {value})");
    }


    void ApplyRepairToCircuit()
    {
        // NUEVO: Verificamos que el circuito exista antes de repararlo
        if (gameManager == null || gameManager.circuit == null) return;

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
        // NUEVO: Verificamos que el circuito exista antes de leerlo
        if (gameManager == null || gameManager.circuit == null) return false;

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
        if (_spawnedComponent != null)
        {
            if (Runner != null && _spawnedComponent.TryGetComponent<NetworkObject>(out var netObj))
            {
                Runner.Despawn(netObj);
            }
            else
            {
                Destroy(_spawnedComponent);
            }
        }
        _spawnedComponent  = null;
        _waitingForInstall = false;
        _pendingType       = ComponentType.None;
    }
}

