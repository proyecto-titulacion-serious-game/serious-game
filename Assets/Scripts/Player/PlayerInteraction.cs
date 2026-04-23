using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;   // Requiere: com.unity.xr.interaction.toolkit

/// <summary>
/// Gestiona las interacciones del Explorador VR con los componentes del circuito:
/// - Colocar puntas del multímetro en nodos
/// - Agarrar y reemplazar componentes
/// - Reconectar cables (Reto 2, 4)
/// - Corregir polaridades (Reto 3)
///
/// Cada objeto interactuable en la escena debe tener:
///   - XRGrabInteractable (o XRSimpleInteractable para "tocar")
///   - NodeInteractable / SelectableComponent según su tipo
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public GameManager     gameManager;
    public CircuitManager  circuit;
    public Multimeter      multimeter;
    public PlayerController playerController;
    public HapticFeedback  haptics;

    [Header("Interacción activa")]
    public ElectricalComponent heldComponent;    // Componente en mano
    public ElectricalNode      lastTouchedNode;  // Nodo más reciente tocado

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool _probeAPlaced = false;
    private bool _probeBPlaced = false;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        CircuitManager.OnCircuitChanged += OnCircuitUpdated;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= OnCircuitUpdated;
    }

    // ─────────────────────────────────────────────
    //  Interacciones con Multímetro
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamar desde NodeInteractable.OnSelectEntered cuando el Explorador
    /// toca un nodo con la punta roja del multímetro.
    /// </summary>
    public void PlaceRedProbe(ElectricalNode node)
    {
        if (node == null) return;

        multimeter.SetProbeA(node);
        _probeAPlaced = true;
        haptics?.PlayLight();

        Debug.Log($"[PlayerInteraction] Punta roja → {node.name}");
        circuit.MarkDirty();
    }

    /// <summary>
    /// Llamar desde NodeInteractable.OnSelectEntered cuando el Explorador
    /// toca un nodo con la punta negra del multímetro.
    /// </summary>
    public void PlaceBlackProbe(ElectricalNode node)
    {
        if (node == null) return;

        multimeter.SetProbeB(node);
        _probeBPlaced = true;
        haptics?.PlayLight();

        Debug.Log($"[PlayerInteraction] Punta negra → {node.name}");
        circuit.MarkDirty();
    }

    public void RemoveProbes()
    {
        multimeter.ResetProbes();
        _probeAPlaced = false;
        _probeBPlaced = false;
    }

    public bool BothProbesPlaced() => _probeAPlaced && _probeBPlaced;

    // ─────────────────────────────────────────────
    //  Interacciones con Componentes
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Explorador agarra un componente (llamar desde XRGrabInteractable).
    /// En Reto 1: agarra la resistencia defectuosa para reemplazarla.
    /// </summary>
    public void OnGrabComponent(SelectableComponent selectable)
    {
        if (selectable?.component == null) return;

        heldComponent = selectable.component;
        selectable.Highlight();
        haptics?.PlayMedium();
        playerController?.FreezeMovement(true);

        Debug.Log($"[PlayerInteraction] Explorador agarró: {heldComponent.name}");
    }

    /// <summary>
    /// El Explorador suelta el componente en su slot de reemplazo.
    /// Llamar cuando el objeto entra en el trigger del slot correcto.
    /// </summary>
    

    // ─────────────────────────────────────────────
    //  Reto 2 — Reconexión de cables paralelos
    // ─────────────────────────────────────────────

    /// <summary>
    /// Reconectar una rama del paralelo arrastrando el cable al punto correcto.
    /// Llamar desde el trigger de conexión.
    /// </summary>
    public void ConnectBranch(LED brokenLED, float correctResistance)
    {
        if (brokenLED == null) return;

        brokenLED.resistance = correctResistance;
        haptics?.PlayStrong();
        gameManager?.RegisterRepairAction();
        circuit.MarkDirty();

        Debug.Log($"[PlayerInteraction] Rama reconectada: {brokenLED.name}");
    }

    // ─────────────────────────────────────────────
    //  Reto 3 — Corrección de polaridades
    // ─────────────────────────────────────────────

    /// <summary>
    /// Girar un componente para corregir su polaridad.
    /// Llamar cuando el Explorador rota físicamente el componente.
    /// </summary>
    public void CorrectPolarity(LED led)
    {
        if (led == null) return;
        led.SetPolarityInverted(false);
        haptics?.PlayStrong();
        gameManager?.RegisterRepairAction();
        circuit.MarkDirty();
        Debug.Log($"[PlayerInteraction] Polaridad corregida en LED: {led.name}");
    }

    public void CorrectCapacitorPolarity(Capacitor cap)
    {
        if (cap == null) return;
        cap.SetPolarityInverted(false);
        haptics?.PlayStrong();
        gameManager?.RegisterRepairAction();
        circuit.MarkDirty();
        Debug.Log($"[PlayerInteraction] Polaridad corregida en Capacitor: {cap.name}");
    }

    // ─────────────────────────────────────────────
    //  Reto 4 — Arduino
    // ─────────────────────────────────────────────

    /// <summary>
    /// El Explorador inserta el cable en el pin indicado por el Técnico.
    /// </summary>
    public void ConnectArduinoPin(ArduinoPin pin, int selectedPinNumber)
    {
        if (pin == null) return;
        pin.RepairPin(selectedPinNumber);
        haptics?.PlayMedium();
        circuit.MarkDirty();
    }

    public void FixLooseCable(ArduinoPin pin)
    {
        if (pin == null) return;
        pin.FixLooseCable();
        haptics?.PlayMedium();
        circuit.MarkDirty();
    }

    // ─────────────────────────────────────────────
    //  Callbacks
    // ─────────────────────────────────────────────

    void OnCircuitUpdated()
    {
        // Aquí se puede disparar retroalimentación visual/auditiva
        // cuando el circuito cambia después de una interacción
    }
}

// ─────────────────────────────────────────────
//  Componente auxiliar: Slot de componente
// ─────────────────────────────────────────────

/// <summary>
/// Punto de inserción de componentes en el panel de la nave.
/// Valida que el componente insertado sea el correcto.
/// </summary>
