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
    [Header("Bandeja donde aparece el componente")]
    public Transform puntoDeEntrega;

    [Header("Prefabs (mismos que ComponentDeliverySystem)")]
    public GameObject resistorPrefab;
    public GameObject ledPrefab;
    public GameObject capacitorPrefab;
    public GameObject arduinoPinPrefab;

    [Header("Sistema de delivery local (para validar instalación)")]
    public ComponentDeliverySystem delivery;

    private GameObject _componenteActual;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void OnEnable()
    {
        GameSession.OnComponenteRecibido          += HandleComponenteRecibido;
        GameSession.OnRetoChanged                 += HandleRetoChanged;
        ComponentSendingTray.OnComponentSentLocal += HandleComponenteRecibido;
    }

    void OnDisable()
    {
        GameSession.OnComponenteRecibido          -= HandleComponenteRecibido;
        GameSession.OnRetoChanged                 -= HandleRetoChanged;
        ComponentSendingTray.OnComponentSentLocal -= HandleComponenteRecibido;
    }

    // ─────────────────────────────────────────────
    //  Handlers
    // ─────────────────────────────────────────────

    void HandleComponenteRecibido(ComponentType tipo, float valor)
    {
        // Destruir componente anterior si quedó sin instalar
        if (_componenteActual != null)
            Destroy(_componenteActual);

        GameObject prefab = tipo switch
        {
            ComponentType.Resistor   => resistorPrefab,
            ComponentType.LED        => ledPrefab,
            ComponentType.Capacitor  => capacitorPrefab,
            ComponentType.ArduinoPin => arduinoPinPrefab,
            _                        => null
        };

        if (prefab == null || puntoDeEntrega == null)
        {
            Debug.LogWarning($"[Receiver] Prefab o punto de entrega no asignado para {tipo}.");
            return;
        }

        _componenteActual = Instantiate(prefab, puntoDeEntrega.position, puntoDeEntrega.rotation, puntoDeEntrega);
        ConfigurarComponente(_componenteActual, tipo, valor);

        // Mantenerlo kinematic mientras está en la bandeja — XRGrabInteractable lo activa al agarrar
        if (_componenteActual.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // Informar al DeliverySystem local para que pueda validar la instalación
        delivery?.PrepareForInstall(tipo, valor);

        Debug.Log($"[Receiver] Componente recibido: {tipo} = {valor}");
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
