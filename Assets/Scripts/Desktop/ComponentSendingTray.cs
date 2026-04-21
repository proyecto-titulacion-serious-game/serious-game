using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Bandeja de envío sobre la mesa del Técnico.
/// PC: el Técnico hace click en un DeskComponent → llega a la bandeja → click ENVIAR.
/// VR: el Técnico suelta físicamente el componente sobre la bandeja → envío automático.
/// </summary>
public class ComponentSendingTray : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    public TechnicianActions    technicianActions;
    public ComponentDeliverySystem delivery;
    public GameManager          gameManager;

    [Header("UI de la bandeja (World Space Canvas)")]
    public TMP_Text   txtComponenteEnBandeja;
    public TMP_Text   txtDescripcion;
    public Button     btnEnviar;
    public TMP_Text   txtFeedback;

    [Header("Posición visual donde aparece el componente seleccionado")]
    public Transform  traySlot;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    private DeskComponent _pending;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (btnEnviar != null)
            btnEnviar.onClick.AddListener(Enviar);

        if (technicianActions == null)
            technicianActions = FindObjectOfType<TechnicianActions>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        UpdateUI();
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Coloca un componente en la bandeja (llamado por DeskComponent en PC).
    /// </summary>
    public void PlaceComponent(DeskComponent comp)
    {
        if (comp == null) return;

        _pending = comp;

        // Mover el objeto 3D al slot visual de la bandeja
        if (traySlot != null)
            comp.transform.position = traySlot.position;

        UpdateUI();
        Set(txtFeedback, "");
        Debug.Log($"[Tray] En bandeja: {comp.componentType} {comp.componentValue}");
    }

    // ─────────────────────────────────────────────
    //  Envío
    // ─────────────────────────────────────────────

    /// <summary>
    /// Envía el componente de la bandeja al Explorador (o aplica directo en demo).
    /// </summary>
    public void Enviar()
    {
        if (_pending == null)
        {
            Set(txtFeedback, "Coloca un componente primero.");
            return;
        }

        bool exito = false;

        switch (_pending.componentType)
        {
            case ComponentType.Resistor:
                // Intentar con DeliverySystem primero, si no existe aplicar directo
                if (delivery != null)
                {
                    delivery.SendResistor(_pending.componentValue);
                    exito = true;
                }
                else if (technicianActions != null)
                {
                    exito = technicianActions.ApplyResistorValue(_pending.componentValue);
                }
                break;

            case ComponentType.LED:
                if (delivery != null) { delivery.SendLED(true); exito = true; }
                else { exito = FixLEDPolarity(); }
                break;

            case ComponentType.Capacitor:
                if (delivery != null) { delivery.SendCapacitor(true); exito = true; }
                else { exito = FixCapacitorPolarity(); }
                break;

            default:
                Set(txtFeedback, "Tipo de componente no soportado.");
                return;
        }

        if (exito)
        {
            Set(txtFeedback, $"Enviado: {_pending.componentType} {_pending.componentValue:F0}");
            _pending.Deselect();
            _pending = null;
            UpdateUI();
        }
        else
        {
            Set(txtFeedback, $"Valor incorrecto. Revisa el manual.");
        }
    }

    // ─────────────────────────────────────────────
    //  VR — soltar componente sobre la bandeja
    // ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        var comp = other.GetComponent<DeskComponent>();
        if (comp == null) return;

        PlaceComponent(comp);
        Invoke(nameof(Enviar), 0.5f);   // pequeño delay para feedback visual
    }

    // ─────────────────────────────────────────────
    //  Aplicaciones directas (sin Explorador)
    // ─────────────────────────────────────────────

    bool FixLEDPolarity()
    {
        if (gameManager?.circuit == null) return false;
        foreach (var c in gameManager.circuit.components)
        {
            if (c is LED led && led.polarityInverted)
            {
                led.polarityInverted = false;
                gameManager.circuit.MarkDirty();
                gameManager.RegisterRepairAction();
                return true;
            }
        }
        return false;
    }

    bool FixCapacitorPolarity()
    {
        if (gameManager?.circuit == null) return false;
        foreach (var c in gameManager.circuit.components)
        {
            if (c is Capacitor cap && cap.polarityInverted)
            {
                cap.polarityInverted = false;
                gameManager.circuit.MarkDirty();
                gameManager.RegisterRepairAction();
                return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────

    void UpdateUI()
    {
        if (_pending != null)
        {
            Set(txtComponenteEnBandeja, $"{_pending.componentType}  {_pending.componentValue:F0}");
            Set(txtDescripcion, _pending.componentDescription);
            if (btnEnviar != null) btnEnviar.interactable = true;
        }
        else
        {
            Set(txtComponenteEnBandeja, "Bandeja vacia");
            Set(txtDescripcion, "Haz click en un componente de la mesa");
            if (btnEnviar != null) btnEnviar.interactable = false;
        }
    }

    void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}