using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel completo del Técnico — disponible en PC (Screen Space) o VR estático (World Space).
///
/// Muestra TODO lo que el Técnico necesita para guiar al Explorador:
///   • Datos del circuito en tiempo real (voltaje, corriente, estado de componentes)
///   • Lectura del multímetro del Explorador
///   • Manual técnico + diagrama del reto actual
///   • Diagnóstico automático
///   • Herramientas de acción (seleccionar componente, calcular valor, enviar al Explorador)
///   • Progreso del reto + desempeño
///
/// El Técnico NO puede ver el entorno 3D — toda su información viene de este panel.
/// </summary>
public class TechnicianUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Referencias de sistemas
    // ─────────────────────────────────────────────
    [Header("Sistemas")]
    public CircuitManager          circuit;
    public Multimeter              multimeter;
    public GameManager             gameManager;
    public PerformanceTracker      performance;
    public InstructionSystem       instructionSystem;
    public TechnicianActions       technicianActions;
    public ComponentDeliverySystem delivery;
    public TechnicianManual        manual;

    // ─────────────────────────────────────────────
    //  Panel izquierdo — Manual técnico
    // ─────────────────────────────────────────────
    [Header("Panel Izquierdo — Manual")]
    public TMP_Text txtManualTitulo;
    public TMP_Text txtConcepto;
    public TMP_Text txtFormula;
    public TMP_Text txtObjetivoManual;
    public TMP_Text txtTablaValores;      // Código de colores / pinout Arduino
    public Image    imgDiagrama;          // Sprite del diagrama del circuito
    public Sprite[] diagramasPorReto;     // Arrastrar sprites: [0]=R1, [1]=R2, [2]=R3, [3]=R4

    // ─────────────────────────────────────────────
    //  Panel central — Datos en tiempo real
    // ─────────────────────────────────────────────
    [Header("Panel Central — Datos en tiempo real")]
    public TMP_Text txtVoltajeFuente;
    public TMP_Text txtCorrienteTotal;
    public TMP_Text txtEstadoComponentes;
    public TMP_Text txtMultimetro;        // Lectura de ambas puntas + voltaje medido
    public TMP_Text txtDiagnostico;       // Análisis automático del DiagnosticSystem

    // ─────────────────────────────────────────────
    //  Panel derecho — Acciones del Técnico
    // ─────────────────────────────────────────────
    [Header("Panel Derecho — Acciones")]
    public TMP_Text    txtObjetivoReto;
    public TMP_Text    txtInstruccionActual;
    public TMP_Text    txtPasoActual;
    public TMP_Text    txtComponenteSeleccionado;
    public TMP_Text    txtEstadoEnvio;        // "Componente en tránsito…" / "Instalado"
    public TMP_Text    txtDesempeno;           // Tiempo + errores
    public TMP_Text    txtResultado;

    [Header("Campo de valor (para calcular resistencia)")]
    public TMP_InputField inputValorResistencia;  // Técnico escribe el valor calculado

    [Header("Botones de acción")]
    public Button btnEnviarResistor;      // Envía la resistencia calculada al Explorador
    public Button btnEnviarLED;           // Envía LED correcto (Reto 3)
    public Button btnEnviarCapacitor;     // Envía Capacitor correcto (Reto 3)
    public Button btnRepararParalelo;     // Autoriza reparación de rama (Reto 2)
    public Button btnRepararArduino;      // Autoriza reparación Arduino (Reto 4)

    [Header("Indicadores de estado")]
    public Image  indicadorConexion;      // Verde = explorador conectado
    public TMP_Text txtConexion;
    public GameObject panelEntregaPendiente;  // Se activa cuando hay componente en tránsito

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private DiagnosticSystem _diagnostic = new DiagnosticSystem();
    private float _updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.1f;   // 10 veces/segundo — no cada frame

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        GameManager.OnLevelLoaded        += OnLevelLoaded;
        GameManager.OnLevelCompleted     += OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      += OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled += OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      += OnDeliveryError;
    }

    void OnDisable()
    {
        GameManager.OnLevelLoaded        -= OnLevelLoaded;
        GameManager.OnLevelCompleted     -= OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      -= OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled -= OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      -= OnDeliveryError;
    }

    void Update()
    {
        _updateTimer += Time.deltaTime;
        if (_updateTimer < UPDATE_INTERVAL) return;
        _updateTimer = 0f;

        RefreshPanel();
    }

    // ─────────────────────────────────────────────
    //  Actualización del panel
    // ─────────────────────────────────────────────

    void RefreshPanel()
    {
        if (gameManager == null || circuit == null) return;

        UpdateManual();
        UpdateDatosCircuito();
        UpdateMultimetro();
        UpdateDiagnostico();
        UpdateAcciones();
        UpdateProgreso();
    }

    // ── Panel izquierdo ───────────────────────────

    void UpdateManual()
    {
        if (manual == null) return;

        var data = manual.GetManualData(gameManager.currentLevel);

        SetText(txtManualTitulo,    data.titulo);
        SetText(txtConcepto,        data.concepto);
        SetText(txtFormula,         data.formula);
        SetText(txtObjetivoManual,  data.objetivo);
        SetText(txtTablaValores,    data.tablaValores);

        if (imgDiagrama != null && diagramasPorReto != null)
        {
            int idx = (int)gameManager.currentLevel;
            if (idx < diagramasPorReto.Length && diagramasPorReto[idx] != null)
                imgDiagrama.sprite = diagramasPorReto[idx];
        }
    }

    // ── Panel central ─────────────────────────────

    void UpdateDatosCircuito()
    {
        // Voltaje de la fuente
        float vSource = 0f;
        foreach (var c in circuit.components)
            if (c is VoltageSource vs) { vSource = vs.voltage; break; }

        SetText(txtVoltajeFuente, $"Fuente: {vSource:F1} V");
        SetText(txtCorrienteTotal, $"Corriente total: {circuit.totalCurrent * 1000f:F1} mA");

        // Estado de componentes
        var sb = new StringBuilder();
        foreach (var c in circuit.components)
        {
            if (c is Resistor r)
                sb.AppendLine($"R: {r.resistance:F0} Ω  {(r.hasFault ? "⚠ FALLA" : "✓ OK")}");
            else if (c is LED led)
                sb.AppendLine($"LED: {(led.isOn ? "ENCENDIDO ✓" : "APAGADO ✗")}  " +
                              $"{(led.polarityInverted ? "⚠ Polaridad inv." : "")}");
            else if (c is Capacitor cap)
                sb.AppendLine($"Cap: {(cap.polarityInverted ? "⚠ Polaridad inv." : "✓ OK")}");
            else if (c is ArduinoPin pin)
                sb.AppendLine($"Pin {pin.pinNumber}: {(pin.hasFault ? "⚠ PIN INCORRECTO" : "✓ OK")}  " +
                              $"{(pin.hasLooseCable ? "⚠ Cable suelto" : "")}");
        }
        SetText(txtEstadoComponentes, sb.ToString());
    }

    void UpdateMultimetro()
    {
        if (multimeter == null) return;

        string probeA = multimeter.probeA != null ? multimeter.probeA.name : "—";
        string probeB = multimeter.probeB != null ? multimeter.probeB.name : "—";
        float  volt   = multimeter.measuredVoltage;

        SetText(txtMultimetro,
            $"🔴 Punta roja:  {probeA}\n" +
            $"⚫ Punta negra: {probeB}\n" +
            $"━━━━━━━━━━━━━━━━\n" +
            $"Voltaje medido: {volt:F2} V");
    }

    void UpdateDiagnostico()
    {
        if (circuit == null) return;
        string diag = _diagnostic.GetDiagnosis(circuit.components, circuit.totalCurrent);
        SetText(txtDiagnostico, diag);
    }

    // ── Panel derecho ─────────────────────────────

    void UpdateAcciones()
    {
        if (instructionSystem == null) return;

        // Objetivo del reto
        SetText(txtObjetivoReto, GetObjetivoTexto());

        // Instrucción y paso actual
        SetText(txtInstruccionActual, instructionSystem.GetCurrentInstruction());
        SetText(txtPasoActual, $"Paso {instructionSystem.currentStep + 1}");

        // Componente seleccionado por el Técnico
        string seleccionado = technicianActions?.GetSelectedComponentName() ?? "—";
        SetText(txtComponenteSeleccionado, $"Seleccionado: {seleccionado}");

        // Habilitar/deshabilitar botones según el reto y el paso
        UpdateBotones();
    }

    void UpdateBotones()
    {
        bool puedEnviarR  = gameManager.currentLevel == LevelType.OhmLaw   &&
                            instructionSystem.CanRepairResistor()           &&
                            !string.IsNullOrEmpty(inputValorResistencia?.text);

        bool puedeParalelo = gameManager.currentLevel == LevelType.Parallel &&
                             instructionSystem.CanRepairParallel();

        bool puedeLED      = gameManager.currentLevel == LevelType.Mixed;
        bool puedeCapacitor= gameManager.currentLevel == LevelType.Mixed;
        bool puedeArduino  = gameManager.currentLevel == LevelType.Arduino;

        SetInteractable(btnEnviarResistor, puedEnviarR);
        SetInteractable(btnRepararParalelo, puedeParalelo);
        SetInteractable(btnEnviarLED,       puedeLED);
        SetInteractable(btnEnviarCapacitor, puedeCapacitor);
        SetInteractable(btnRepararArduino,  puedeArduino);
    }

    void UpdateProgreso()
    {
        if (performance == null) return;
        SetText(txtDesempeno,
            $"⏱ {performance.GetTime():F0}s  |  " +
            $"❌ {performance.GetErrors()} errores");
    }

    // ─────────────────────────────────────────────
    //  Botones — llamados desde OnClick en Unity
    // ─────────────────────────────────────────────

    /// <summary>Técnico pulsa "Enviar Resistencia" después de escribir el valor calculado.</summary>
    public void OnClickEnviarResistor()
    {
        if (inputValorResistencia == null) return;
        if (!float.TryParse(inputValorResistencia.text, out float valor))
        {
            SetText(txtEstadoEnvio, "⚠ Ingresa un valor numérico válido.");
            return;
        }
        delivery?.SendResistor(valor);
    }

    public void OnClickEnviarLED()       => delivery?.SendLED(true);
    public void OnClickEnviarCapacitor() => delivery?.SendCapacitor(true);

    public void OnClickRepararParalelo()
    {
        technicianActions?.FixParallelCircuit();
    }

    public void OnClickRepararArduino()
    {
        // Para Reto 4: el Técnico autoriza la corrección de pin
        // El número de pin correcto viene del manual
        delivery?.SendResistor(330f); // Envía resistencia de 330Ω para el buzzer
    }

    // ─────────────────────────────────────────────
    //  Callbacks de eventos
    // ─────────────────────────────────────────────

    void OnLevelLoaded(LevelType level)
    {
        SetText(txtEstadoEnvio, "");
        SetText(txtResultado,   "");
        if (panelEntregaPendiente != null)
            panelEntregaPendiente.SetActive(false);
        if (inputValorResistencia != null)
            inputValorResistencia.text = "";
    }

    void OnLevelCompleted(LevelType level, bool success)
    {
        string eval = performance?.GetEvaluation() ?? "";
        SetText(txtResultado, success
            ? $"✅ Reto {(int)level + 1} completado\n{eval}"
            : $"❌ Reto {(int)level + 1} fallido\n{eval}");
    }

    void OnComponentSent(ComponentType type, float value)
    {
        SetText(txtEstadoEnvio, $"📦 {type} ({value:F0}) en camino al Explorador…");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(true);
    }

    void OnComponentInstalled(bool success)
    {
        SetText(txtEstadoEnvio, success ? "✅ Componente instalado" : "❌ Slot incorrecto");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(false);
    }

    void OnDeliveryError()
    {
        SetText(txtEstadoEnvio, "❌ Componente incorrecto. Recalcula.");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    string GetObjetivoTexto()
    {
        return gameManager.currentLevel switch
        {
            LevelType.OhmLaw  => "RETO 1\nCalcula R correcta con V=I×R\nEnvíala al Explorador",
            LevelType.Parallel=> "RETO 2\nIdentifica la rama del paralelo\nque no recibe corriente",
            LevelType.Mixed   => "RETO 3\n3 fallas: LED invertido,\ncapacitor invertido, R incorrecta",
            LevelType.Arduino => "RETO 4\nPin sensor incorrecto + cable suelto\n+ resistencia buzzer faltante",
            _                 => "—"
        };
    }

    void SetText(TMP_Text t, string s)       { if (t != null) t.text = s; }
    void SetText(Text t, string s)           { if (t != null) t.text = s; }
    void SetInteractable(Button b, bool v)   { if (b != null) b.interactable = v; }
}