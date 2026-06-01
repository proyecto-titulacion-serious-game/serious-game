using System.Collections;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Bandeja de envío sobre la mesa del Técnico.
/// Actúa como Mediador para la selección de DeskComponents.
/// </summary>
public class ComponentSendingTray : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public TechnicianActions       technicianActions;
    public ComponentDeliverySystem delivery;
    public GameManager             gameManager;

    [Header("UI de la bandeja (World Space Canvas)")]
    public TMP_Text       txtComponenteEnBandeja;
    public TMP_Text       txtDescripcion;
    public TMP_InputField inputValor;
    public TMP_Text       txtInputLabel;
    public Toggle         togglePolaridad;
    public TMP_Text       txtToggleLabel;
    public Button         btnEnviar;
    public TMP_Text       txtFeedback;   // mensaje de estado tras enviar (OK / Error)
    public Transform      traySlot;      // punto donde aparece el componente en la bandeja

    // ─────────────────────────────────────────────
    //  Estado Interno
    // ─────────────────────────────────────────────
    private DeskComponent _currentSelectedDeskComponent;
    private Coroutine     _feedbackCoroutine;

    void Awake()
    {
        if (btnEnviar != null)
            btnEnviar.onClick.AddListener(EnviarComponente);

        if (inputValor == null)
            Debug.LogWarning("[Bandeja] 'inputValor' no asignado en Inspector — " +
                             "el campo de ohms no se ocultará para LEDs/Capacitores.", this);

        ActualizarUI();
    }

    // ─────────────────────────────────────────────
    //  Lógica de Selección (Patrón Mediador)
    // ─────────────────────────────────────────────
    
    public void SetSelectedComponent(DeskComponent newComponent)
    {
        // 1. Apagar el anterior usando el método correcto (SetSelectionState)
        if (_currentSelectedDeskComponent != null)
        {
            _currentSelectedDeskComponent.SetSelectionState(false);
        }

        // 2. Encender el nuevo
        _currentSelectedDeskComponent = newComponent;
        
        if (_currentSelectedDeskComponent != null)
        {
            _currentSelectedDeskComponent.SetSelectionState(true);
        }

        // 3. Actualizar UI
        ActualizarUI();
    }

    // ─────────────────────────────────────────────
    //  Actualización de Interfaz
    // ─────────────────────────────────────────────

    void ActualizarUI()
    {
        if (_currentSelectedDeskComponent != null)
        {
            SetTexto(txtComponenteEnBandeja, _currentSelectedDeskComponent.name.Replace("Comp_", ""));
            SetTexto(txtDescripcion, _currentSelectedDeskComponent.componentDescription);

            // BUG FIX: inputValor solo para tipos que requieren valor numérico
            bool necesitaValor = _currentSelectedDeskComponent.componentType == ComponentType.Resistor
                              || _currentSelectedDeskComponent.componentType == ComponentType.ArduinoPin;
            if (inputValor  != null) { inputValor.gameObject.SetActive(necesitaValor);  inputValor.text = ""; }
            if (txtInputLabel != null)  txtInputLabel.gameObject.SetActive(necesitaValor);

            // Toggle de polaridad solo para LED y Capacitor
            bool necesitaToggle = _currentSelectedDeskComponent.componentType == ComponentType.LED
                               || _currentSelectedDeskComponent.componentType == ComponentType.Capacitor;
            if (togglePolaridad != null) { togglePolaridad.gameObject.SetActive(necesitaToggle); togglePolaridad.isOn = true; }
            if (txtToggleLabel  != null) { txtToggleLabel.gameObject.SetActive(necesitaToggle);  txtToggleLabel.text = "Polaridad: CORRECTA"; }

            if (btnEnviar    != null)  btnEnviar.gameObject.SetActive(true);
            if (txtFeedback  != null)  txtFeedback.text = "";   // limpiar feedback anterior
        }
        else
        {
            SetTexto(txtComponenteEnBandeja, "Bandeja vacía");
            SetTexto(txtDescripcion, "Haz click en un componente de la mesa");

            if (inputValor       != null) inputValor.gameObject.SetActive(false);
            if (txtInputLabel    != null) txtInputLabel.gameObject.SetActive(false);
            if (togglePolaridad  != null) togglePolaridad.gameObject.SetActive(false);
            if (txtToggleLabel   != null) txtToggleLabel.gameObject.SetActive(false);
            if (btnEnviar        != null) btnEnviar.gameObject.SetActive(false);
        }
    }

    void SetTexto(TMP_Text t, string s) { if (t != null) t.text = s; }

    // ─────────────────────────────────────────────
    //  Envío al Explorador
    // ─────────────────────────────────────────────

    void EnviarComponente()
    {
        if (_currentSelectedDeskComponent == null) return;

        ComponentType tipo       = _currentSelectedDeskComponent.componentType;
        float         valorFinal = 0f;

        if (tipo == ComponentType.Resistor || tipo == ComponentType.ArduinoPin)
        {
            if (inputValor != null && !string.IsNullOrEmpty(inputValor.text))
                float.TryParse(inputValor.text, NumberStyles.Float,
                               CultureInfo.InvariantCulture, out valorFinal);
        }
        else if (tipo == ComponentType.LED || tipo == ComponentType.Capacitor)
        {
            valorFinal = (togglePolaridad != null && togglePolaridad.isOn) ? 1f : -1f;
        }

        if (GameSession.Instance != null)
        {
            // Path de red: el RPC llega al Explorador y su ExplorerComponentReceiver
            // spawna el componente y llama delivery.PrepareForInstall allá.
            GameSession.Instance.RPC_EnviarComponente((int)tipo, valorFinal);
            Debug.Log($"[Bandeja] {tipo} ({valorFinal}) enviado por red.");
        }
        else
        {
            // Path offline/sin Fusion: dispara evento local.
            // ExplorerComponentReceiver lo escucha y spawna + llama PrepareForInstall.
            RaiseOnComponentSentLocal(tipo, valorFinal);
            // Fallback si no hay ExplorerComponentReceiver en la escena.
            delivery?.PrepareForInstall(tipo, valorFinal);
            Debug.LogWarning("[Bandeja] GameSession.Instance es null — entrega local. " +
                             "Verificar que ConnectionManager.modoOffline = false y que " +
                             "no haya un CM duplicado con rolAutomatico = Ninguno en la escena.");
        }

        // Limpiar la bandeja usando el mediador
        SetSelectedComponent(null);

        // Mostrar feedback (se limpia tras 3 s)
        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
        SetTexto(txtFeedback, "✓ Componente enviado");
        _feedbackCoroutine = StartCoroutine(LimpiarFeedback(3f));
    }

    IEnumerator LimpiarFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetTexto(txtFeedback, "");
        _feedbackCoroutine = null;
    }
    // ─────────────────────────────────────────────
    //  Eventos de comunicación (Fix para CS0117)
    // ─────────────────────────────────────────────
    public static event System.Action<ComponentType, float> OnComponentSentLocal;

    public static void RaiseOnComponentSentLocal(ComponentType tipo, float valor)
    {
        OnComponentSentLocal?.Invoke(tipo, valor);
    }
}