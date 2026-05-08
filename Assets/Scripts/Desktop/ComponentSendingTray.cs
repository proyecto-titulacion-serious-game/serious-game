using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

/// <summary>
/// Bandeja de envío sobre la mesa del Técnico.
///
/// FLUJO EDUCATIVO (Reto 1 ejemplo):
///   1. Técnico hace click en "Comp_Resistor" genérico sobre la mesa
///   2. El componente va a la bandeja visualmente
///   3. Aparece un InputField: "Escribe el valor en ohmios..."
///   4. Técnico consulta el manual, calcula R = 100Ω, y escribe "100"
///   5. Click ENVIAR → el componente se envía con R=100Ω al Explorador
///   6. Si escribió un valor incorrecto → el circuito no se repara → aprendizaje por error
///
/// PC: click en DeskComponent → llega a bandeja → escribe valor → ENVIAR
/// VR: agarra componente → suelta en bandeja → escribe valor → ENVIAR
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
    public TMP_Text   txtComponenteEnBandeja;
    public TMP_Text   txtDescripcion;
    public Button     btnEnviar;
    public TMP_Text   txtFeedback;

    [Header("InputField para valor calculado por el Técnico")]
    [Tooltip("El Técnico escribe aquí el valor que calculó del manual. Solo aparece para Resistor y ArduinoPin.")]
    public TMP_InputField inputValor;

    [Header("Label del InputField")]
    [Tooltip("Texto que indica qué debe escribir el Técnico. Cambia según el tipo de componente.")]
    public TMP_Text txtInputLabel;

    [Header("Toggle de polaridad (LED y Capacitor)")]
    [Tooltip("ON = polaridad correcta / OFF = polaridad invertida. " +
             "Permite al Técnico enviar intencionalmente la polaridad incorrecta.")]
    public Toggle   togglePolaridad;
    public TMP_Text txtToggleLabel;

    [Header("Posición visual donde aparece el componente seleccionado")]
    public Transform traySlot;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    private DeskComponent _pending;
    private Vector3       _posicionOriginal;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (btnEnviar != null)
            btnEnviar.onClick.AddListener(Enviar);

        if (technicianActions == null)
            technicianActions = FindFirstObjectByType<TechnicianActions>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (togglePolaridad != null)
            togglePolaridad.onValueChanged.AddListener(OnPolaridadToggleChanged);

        UpdateUI();
    }

    // ─────────────────────────────────────────────
    //  API pública — llamado por DeskComponent
    // ─────────────────────────────────────────────

    /// <summary>
    /// Coloca un componente en la bandeja (llamado por DeskComponent en PC).
    /// Si es Resistor o ArduinoPin, muestra el InputField para escribir el valor.
    /// </summary>
    public void PlaceComponent(DeskComponent comp)
    {
        if (comp == null) return;

        // Click en la misma pieza → cancelar y devolver
        if (_pending == comp)
        {
            ReturnComponent();
            return;
        }

        // Si había otra pieza → devolverla primero
        if (_pending != null && _pending != comp)
        {
            ReturnComponent();
        }

        // Guardar la nueva pieza y recordar posición original
        _pending = comp;
        _posicionOriginal = comp.transform.position;

        // Mover visualmente a la bandeja
        if (traySlot != null)
            comp.transform.position = traySlot.position;

        // Limpiar el InputField al poner un componente nuevo
        if (inputValor != null)
            inputValor.text = "";

        UpdateUI();
        Set(txtFeedback, "");
    }

    /// <summary>Devuelve el componente a su posición original en la mesa.</summary>
    public void ReturnComponent()
    {
        if (_pending != null)
        {
            _pending.transform.position = _posicionOriginal;
            _pending.Deselect();
            _pending = null;

            // Limpiar InputField
            if (inputValor != null) inputValor.text = "";

            UpdateUI();
        }
    }

    // ─────────────────────────────────────────────
    //  Envío
    // ─────────────────────────────────────────────

    /// <summary>
    /// Envía el componente al Explorador.
    /// Si es Resistor → usa el valor del InputField (el que calculó el Técnico).
    /// Si es LED/Capacitor → envía con polaridad correcta.
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
                exito = EnviarResistorConValorEscrito();
                break;

            case ComponentType.LED:
            {
                bool correcta = togglePolaridad == null || togglePolaridad.isOn;
                float valorLED = correcta ? 1f : -1f;
                if (delivery != null) { delivery.SendLED(correcta); exito = true; }
                else if (correcta)    { exito = FixLEDPolarity(); }
                else                  { exito = true; }   // envío con polarity incorrecta en modo demo
                if (exito) GameSession.Instance?.EnviarComponente(ComponentType.LED, valorLED);
                break;
            }

            case ComponentType.Capacitor:
            {
                bool correcta = togglePolaridad == null || togglePolaridad.isOn;
                float valorCap = correcta ? 1f : -1f;
                if (delivery != null) { delivery.SendCapacitor(correcta); exito = true; }
                else if (correcta)    { exito = FixCapacitorPolarity(); }
                else                  { exito = true; }
                if (exito) GameSession.Instance?.EnviarComponente(ComponentType.Capacitor, valorCap);
                break;
            }

            case ComponentType.ArduinoPin:
                exito = EnviarArduinoPinConValorEscrito();
                break;

            default:
                Set(txtFeedback, "Tipo de componente no soportado.");
                return;
        }

        if (exito)
        {
            Set(txtFeedback, $"Enviado: {_pending.componentType}");
            _pending.Deselect();
            _pending = null;
            if (inputValor != null) inputValor.text = "";
            UpdateUI();
        }
    }

    // ─────────────────────────────────────────────
    //  Envío de Resistor — con valor del InputField
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lee el valor que el Técnico escribió en el InputField y envía
    /// la resistencia con ESE valor (correcto o incorrecto).
    /// El Técnico debe calcular el valor usando las fórmulas del manual.
    /// </summary>
    bool EnviarResistorConValorEscrito()
    {
        // Verificar que escribió algo
        if (inputValor == null || string.IsNullOrEmpty(inputValor.text))
        {
            Set(txtFeedback, "Escribe el valor de resistencia en ohmios.");
            return false;
        }

        // Parsear el valor
        if (!float.TryParse(inputValor.text, out float valorEscrito))
        {
            Set(txtFeedback, "Valor invalido. Escribe un numero.");
            return false;
        }

        // Validaciones básicas
        if (valorEscrito <= 0f)
        {
            Set(txtFeedback, "El valor debe ser mayor a 0.");
            return false;
        }

        if (valorEscrito > 10000f)
        {
            Set(txtFeedback, "Valor demasiado alto. Revisa tus calculos.");
            return false;
        }

        // Enviar con el valor escrito (sin validar si es correcto o no)
        if (delivery != null)
        {
            delivery.SendResistor(valorEscrito);
            GameSession.Instance?.EnviarComponente(ComponentType.Resistor, valorEscrito);
            Set(txtFeedback, $"Resistencia de {valorEscrito:F0} ohm enviada.");
            return true;
        }
        else if (technicianActions != null)
        {
            // Modo demo sin Explorador — aplicar directo al circuito
            bool resultado = technicianActions.ApplyResistorValue(valorEscrito);
            if (resultado)
                Set(txtFeedback, $"Resistencia de {valorEscrito:F0} ohm aplicada.");
            else
                Set(txtFeedback, $"Resistencia de {valorEscrito:F0} ohm aplicada. Revisa el LED.");
            return true; // Siempre retorna true para que el componente se consuma
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  Envío de Arduino Pin — con valor del InputField
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lee el número de pin que el Técnico escribió en el InputField.
    /// El Técnico debe consultar el manual para saber qué pin es el correcto.
    /// </summary>
    bool EnviarArduinoPinConValorEscrito()
    {
        if (inputValor == null || string.IsNullOrEmpty(inputValor.text))
        {
            Set(txtFeedback, "Escribe el numero de pin (ej: 2).");
            return false;
        }

        if (!int.TryParse(inputValor.text, out int pinEscrito))
        {
            Set(txtFeedback, "Escribe un numero entero de pin.");
            return false;
        }

        if (delivery != null)
        {
            delivery.SendArduinoPin(pinEscrito);
            GameSession.Instance?.EnviarComponente(ComponentType.ArduinoPin, pinEscrito);
            Set(txtFeedback, $"Pin D{pinEscrito} enviado.");
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  VR — soltar componente sobre la bandeja
    // ─────────────────────────────────────────────

    void OnPolaridadToggleChanged(bool isOn)
    {
        if (txtToggleLabel != null)
            txtToggleLabel.text = isOn ? "Polaridad: CORRECTA" : "Polaridad: INVERTIDA";
    }

    void OnTriggerEnter(Collider other)
    {
        var comp = other.GetComponent<DeskComponent>();
        if (comp == null) return;

        PlaceComponent(comp);
        // En VR NO se envía automáticamente — el Técnico VR también
        // debe escribir el valor y pulsar ENVIAR
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
            // Nombre del componente en la bandeja
            Set(txtComponenteEnBandeja, $"{_pending.componentType}");

            // Descripción con telemetría del circuito
            if (gameManager != null && gameManager.circuit != null)
            {
                if (gameManager.circuit.isShortCircuited)
                {
                    string alerta = $"{_pending.componentDescription}\n\n" +
                                    "<color=red><b>PELIGRO: CORTOCIRCUITO</b></color>\n" +
                                    "La corriente es demasiado alta.";
                    Set(txtDescripcion, alerta);
                }
                else
                {
                    string textoDinamico = $"{_pending.componentDescription}\n\n" +
                                           $"Corriente: {gameManager.circuit.totalCurrent:F2} A\n" +
                                           $"Potencia: {gameManager.circuit.totalPower:F2} W";
                    Set(txtDescripcion, textoDinamico);
                }
            }
            else
            {
                Set(txtDescripcion, _pending.componentDescription);
            }

            // InputField — solo para Resistor y ArduinoPin
            bool necesitaInput = _pending.componentType == ComponentType.Resistor
                              || _pending.componentType == ComponentType.ArduinoPin;

            if (inputValor != null)
            {
                inputValor.gameObject.SetActive(necesitaInput);
                if (necesitaInput)
                {
                    var placeholder = inputValor.placeholder as TMP_Text;
                    if (placeholder != null)
                        placeholder.text = _pending.componentType == ComponentType.Resistor
                            ? "Escribe ohmios..."
                            : "Numero de pin...";
                }
            }

            if (txtInputLabel != null)
            {
                txtInputLabel.gameObject.SetActive(necesitaInput);
                txtInputLabel.text = _pending.componentType == ComponentType.Resistor
                    ? "Valor calculado (ohm):"
                    : "Numero de pin:";
            }

            // Toggle de polaridad — solo para LED y Capacitor
            bool necesitaToggle = _pending.componentType == ComponentType.LED
                               || _pending.componentType == ComponentType.Capacitor;

            if (togglePolaridad != null)
            {
                togglePolaridad.gameObject.SetActive(necesitaToggle);
                if (necesitaToggle)
                    togglePolaridad.isOn = true;   // resetear a correcta al poner componente nuevo
            }

            if (txtToggleLabel != null)
            {
                txtToggleLabel.gameObject.SetActive(necesitaToggle);
                txtToggleLabel.text = "Polaridad: CORRECTA";
            }

            if (btnEnviar != null) btnEnviar.gameObject.SetActive(true);
        }
        else
        {
            // Bandeja vacía
            Set(txtComponenteEnBandeja, "Bandeja vacia");
            Set(txtDescripcion, "Haz click en un componente de la mesa");
            if (inputValor      != null) inputValor.gameObject.SetActive(false);
            if (txtInputLabel   != null) txtInputLabel.gameObject.SetActive(false);
            if (togglePolaridad != null) togglePolaridad.gameObject.SetActive(false);
            if (txtToggleLabel  != null) txtToggleLabel.gameObject.SetActive(false);
            if (btnEnviar       != null) btnEnviar.gameObject.SetActive(false);
        }
    }

    void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}