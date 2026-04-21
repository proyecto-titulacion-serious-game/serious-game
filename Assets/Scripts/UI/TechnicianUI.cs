using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador principal de la interfaz del Técnico.
/// Actualiza todos los paneles con datos en tiempo real del circuito,
/// el multímetro del Explorador, el manual técnico y el sistema de acciones.
/// </summary>
/// <remarks>
/// Estructura del TechnicianCanvas:
/// <code>
/// TechnicianCanvas [Screen Space - Camera]
///   ├─ Panel_Datos          → datos en tiempo real del circuito
///   ├─ Panel_Instrucciones  → manual técnico, fórmulas, diagrama
///   ├─ Panel_Diagnostico    → objetivo, instrucción actual, diagnóstico
///   └─ Panel_Botones        → InputField, botones de acción, desempeño
/// </code>
/// </remarks>
public class TechnicianUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Referencias de sistemas
    // ─────────────────────────────────────────────

    /// <summary>Gestor del circuito eléctrico — fuente de todos los datos.</summary>
    [Header("Referencias de sistemas")]
    public CircuitManager circuit;

    /// <summary>Multímetro virtual del Explorador — sus lecturas se muestran en Panel_Datos.</summary>
    public Multimeter multimeter;

    /// <summary>Gestor principal del juego — controla el nivel activo.</summary>
    public GameManager gameManager;

    /// <summary>Registro de tiempo y errores de la sesión.</summary>
    public PerformanceTracker performance;

    /// <summary>Sistema de pasos e instrucciones del reto activo.</summary>
    public InstructionSystem instructionSystem;

    /// <summary>Acciones disponibles para el Técnico.</summary>
    public TechnicianActions technicianActions;

    /// <summary>Sistema de entrega de componentes al Explorador.</summary>
    public ComponentDeliverySystem delivery;

    /// <summary>Contenido del manual técnico por reto.</summary>
    public TechnicianManual manual;

    // ─────────────────────────────────────────────
    //  Panel_Datos
    // ─────────────────────────────────────────────

    /// <summary>Voltaje de la fuente. Ej: "Fuente: 9.0 V"</summary>
    [Header("Panel_Datos — TMPs")]
    public TMP_Text txtVoltajeFuente;

    /// <summary>Corriente total en mA con indicador de estado.</summary>
    public TMP_Text txtCorrienteTotal;

    /// <summary>Estado de cada componente del circuito.</summary>
    public TMP_Text txtEstadoComponentes;

    /// <summary>Lectura del multímetro: punta roja, negra y voltaje medido.</summary>
    public TMP_Text txtMultimetro;

    // ─────────────────────────────────────────────
    //  Panel_Instrucciones
    // ─────────────────────────────────────────────

    /// <summary>Título del manual. Ej: "RETO 1 — Ley de Ohm"</summary>
    [Header("Panel_Instrucciones — TMPs")]
    public TMP_Text txtManualTitulo;

    /// <summary>Concepto eléctrico del reto.</summary>
    public TMP_Text txtConcepto;

    /// <summary>Fórmulas necesarias. Ej: "V = I × R"</summary>
    public TMP_Text txtFormula;

    /// <summary>Qué debe diagnosticar y enviar el Técnico en este reto.</summary>
    public TMP_Text txtObjetivoManual;

    /// <summary>Código de colores de resistencias o pinout Arduino.</summary>
    public TMP_Text txtTablaValores;

    /// <summary>Imagen del diagrama del circuito activo.</summary>
    public Image imgDiagrama;

    /// <summary>
    /// Sprites de diagramas por reto.
    /// Índice: [0]=Reto1, [1]=Reto2, [2]=Reto3, [3]=Reto4.
    /// </summary>
    public Sprite[] diagramasPorReto;

    // ─────────────────────────────────────────────
    //  Panel_Diagnostico
    // ─────────────────────────────────────────────

    /// <summary>Objetivo resumido del reto para el Técnico.</summary>
    [Header("Panel_Diagnostico — TMPs")]
    public TMP_Text txtObjetivoReto;

    /// <summary>Instrucción del paso actual del InstructionSystem.</summary>
    public TMP_Text txtInstruccionActual;

    /// <summary>Número de paso. Ej: "Paso 2 de 4"</summary>
    public TMP_Text txtPasoActual;

    /// <summary>Componente actualmente seleccionado por el Técnico.</summary>
    public TMP_Text txtComponenteSeleccionado;

    /// <summary>Diagnóstico automático generado por DiagnosticSystem.</summary>
    public TMP_Text txtDiagnostico;

    /// <summary>Estado del envío. Ej: "📦 Resistencia en tránsito..."</summary>
    public TMP_Text txtEstadoEnvio;

    // ─────────────────────────────────────────────
    //  Panel_Botones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Campo donde el Técnico escribe el valor calculado.
    /// Content Type = Decimal Number.
    /// </summary>
    [Header("Panel_Botones — controles")]
    public TMP_InputField inputValorResistencia;

    /// <summary>
    /// Envía la resistencia calculada al Explorador.
    /// Se activa cuando CanRepairResistor() es true y hay valor en el input.
    /// </summary>
    public Button btnEnviarResistor;

    /// <summary>
    /// Autoriza la reparación del paralelo (Reto 2).
    /// Se activa cuando CanRepairParallel() es true.
    /// </summary>
    public Button btnRepararParalelo;

    /// <summary>Tiempo y errores de la sesión.</summary>
    public TMP_Text txtDesempeno;

    /// <summary>Resultado del reto. Vacío durante el juego, se llena al completar.</summary>
    public TMP_Text txtResultado;

    // ─────────────────────────────────────────────
    //  Indicadores opcionales
    // ─────────────────────────────────────────────

    /// <summary>Círculo verde/gris de conexión con el Explorador. Opcional.</summary>
    [Header("Indicadores opcionales")]
    public Image    indicadorConexion;

    /// <summary>Texto de estado de conexión. Opcional.</summary>
    public TMP_Text txtConexion;

    /// <summary>Panel activo mientras hay un componente en tránsito. SetActive=false al inicio.</summary>
    public GameObject panelEntregaPendiente;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────

    /// <summary>Motor de diagnóstico — clase pura instanciada en memoria.</summary>
    private readonly DiagnosticSystem _diagnostic = new DiagnosticSystem();

    private float _timer;
    private const float INTERVALO = 0.1f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnLevelLoaded        += OnLevelLoaded;
        GameManager.OnLevelCompleted     += OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      += OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled += OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      += OnDeliveryError;
    }

    private void OnDisable()
    {
        GameManager.OnLevelLoaded        -= OnLevelLoaded;
        GameManager.OnLevelCompleted     -= OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      -= OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled -= OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      -= OnDeliveryError;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < INTERVALO) return;
        _timer = 0f;
        RefreshAllPanels();
    }

    // ─────────────────────────────────────────────
    //  Actualización de paneles
    // ─────────────────────────────────────────────

    /// <summary>Refresca todos los paneles en orden.</summary>
    private void RefreshAllPanels()
    {
        if (gameManager == null || circuit == null) return;
        RefreshPanelDatos();
        RefreshPanelInstrucciones();
        RefreshPanelDiagnostico();
        RefreshPanelBotones();
    }

    /// <summary>
    /// Panel_Datos — actualiza voltaje, corriente,
    /// estado de componentes y lectura del multímetro.
    /// </summary>
    private void RefreshPanelDatos()
    {
        float vSource = 0f;
        foreach (var c in circuit.components)
            if (c is VoltageSource vs) { vSource = vs.voltage; break; }

        Set(txtVoltajeFuente,     $"Fuente: {vSource:F1} V");
        Set(txtCorrienteTotal,    FormatCorriente(circuit.totalCurrent));
        Set(txtEstadoComponentes, BuildEstadoComponentes());

        if (multimeter != null)
        {
            string pA  = multimeter.probeA != null ? multimeter.probeA.name : "—";
            string pB  = multimeter.probeB != null ? multimeter.probeB.name : "—";
            float  v   = multimeter.measuredVoltage;
            Set(txtMultimetro,
                $"🔴 Roja:  {pA}\n⚫ Negra: {pB}\n" +
                $"━━━━━━━━━━━━━━━━━\nVoltaje: {v:F2} V");
        }
    }

    /// <summary>
    /// Panel_Instrucciones — actualiza el manual técnico del reto activo.
    /// </summary>
    private void RefreshPanelInstrucciones()
    {
        if (manual == null) return;
        var d = manual.GetManualData(gameManager.currentLevel);
        Set(txtManualTitulo,   d.titulo);
        Set(txtConcepto,       d.concepto);
        Set(txtFormula,        d.formula);
        Set(txtObjetivoManual, d.objetivo);
        Set(txtTablaValores,   d.tablaValores);

        if (imgDiagrama != null && diagramasPorReto != null)
        {
            int i = (int)gameManager.currentLevel;
            if (i < diagramasPorReto.Length && diagramasPorReto[i] != null)
                imgDiagrama.sprite = diagramasPorReto[i];
        }
    }

    /// <summary>
    /// Panel_Diagnostico — actualiza objetivo, instrucción actual,
    /// componente seleccionado y diagnóstico automático.
    /// </summary>
    private void RefreshPanelDiagnostico()
    {
        Set(txtObjetivoReto,          BuildObjetivo());
        Set(txtInstruccionActual,     instructionSystem?.GetCurrentInstruction() ?? "—");
        Set(txtPasoActual,            BuildPaso());
        Set(txtComponenteSeleccionado,
            $"Seleccionado: {technicianActions?.GetSelectedComponentName() ?? "—"}");
        Set(txtDiagnostico,
            _diagnostic.GetDiagnosis(circuit.components, circuit.totalCurrent));
    }

    /// <summary>
    /// Panel_Botones — actualiza interactividad de botones y desempeño.
    /// </summary>
    private void RefreshPanelBotones()
    {
        bool puedeR = gameManager.currentLevel == LevelType.OhmLaw
                   && (instructionSystem?.CanRepairResistor() ?? false)
                   && !string.IsNullOrEmpty(inputValorResistencia?.text);

        bool puedeP = gameManager.currentLevel == LevelType.Parallel
                   && (instructionSystem?.CanRepairParallel() ?? false);

        SetBtn(btnEnviarResistor,  puedeR);
        SetBtn(btnRepararParalelo, puedeP);

        if (performance != null)
            Set(txtDesempeno,
                $"⏱ {performance.GetTime():F0}s  |  ❌ {performance.GetErrors()} errores");
    }

    // ─────────────────────────────────────────────
    //  OnClick — conectar desde inspector de botones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Btn_EnviarResistor → OnClick().
    /// Valida el valor del InputField y lo envía al Explorador.
    /// </summary>
    public void OnClickEnviarResistor()
    {
        if (inputValorResistencia == null) return;
        if (!float.TryParse(inputValorResistencia.text, out float v))
        { Set(txtEstadoEnvio, "⚠ Ingresa un número válido."); return; }
        delivery?.SendResistor(v);
    }

    /// <summary>Btn_RepararParalelo → OnClick(). Autoriza reparación del paralelo.</summary>
    public void OnClickRepararParalelo() => technicianActions?.FixParallelCircuit();

    /// <summary>Btn_EnviarLED → OnClick() (Reto 3).</summary>
    public void OnClickEnviarLED()       => delivery?.SendLED(true);

    /// <summary>Btn_EnviarCapacitor → OnClick() (Reto 3).</summary>
    public void OnClickEnviarCapacitor() => delivery?.SendCapacitor(true);

    // ─────────────────────────────────────────────
    //  Callbacks de eventos
    // ─────────────────────────────────────────────

    /// <summary>Limpia campos al cargar un nuevo nivel.</summary>
    private void OnLevelLoaded(LevelType level)
    {
        Set(txtEstadoEnvio, "");
        Set(txtResultado, "");
        if (inputValorResistencia != null) inputValorResistencia.text = "";
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(false);
        RefreshPanelInstrucciones();
    }

    /// <summary>Muestra resultado y evaluación al completar el nivel.</summary>
    private void OnLevelCompleted(LevelType level, bool success)
    {
        Set(txtResultado, success
            ? $"✅ Reto {(int)level + 1} completado\n{performance?.GetEvaluation() ?? ""}"
            : $"❌ Reto {(int)level + 1} fallido");
    }

    private void OnComponentSent(ComponentType tipo, float valor)
    {
        Set(txtEstadoEnvio, $"📦 {tipo} ({valor:F0}Ω) → Explorador…");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(true);
    }

    private void OnComponentInstalled(bool success)
    {
        Set(txtEstadoEnvio, success ? "✅ Instalado" : "❌ Slot incorrecto");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(false);
    }

    private void OnDeliveryError()
    {
        Set(txtEstadoEnvio, "❌ Valor incorrecto. Recalcula.");
        if (panelEntregaPendiente != null) panelEntregaPendiente.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Helpers privados
    // ─────────────────────────────────────────────

    /// <summary>Formatea la corriente con indicador visual de estado.</summary>
    private string FormatCorriente(float i)
    {
        float mA = i * 1000f;
        string s = mA > 20f ? " ⚠ SOBRECARGA" : mA < 5f ? " ⚠ MUY BAJA" : " ✓";
        return $"Corriente: {mA:F1} mA{s}";
    }

    /// <summary>Genera el texto de estado de cada componente del circuito.</summary>
    private string BuildEstadoComponentes()
    {
        var sb = new StringBuilder();
        foreach (var c in circuit.components)
        {
            if      (c is Resistor  r)   sb.AppendLine($"R: {r.resistance:F0}Ω {(r.hasFault ? "⚠ FALLA" : "✓")}");
            else if (c is LED       led) sb.AppendLine($"LED: {(led.isOn ? "🟢 ON" : "⚫ OFF")} I={led.current*1000f:F1}mA{(led.polarityInverted?" ⚠":"")}");
            else if (c is Capacitor cap) sb.AppendLine($"Cap: {(cap.polarityInverted ? "⚠ INV" : "✓")}");
            else if (c is ArduinoPin p)  sb.AppendLine($"Pin D{p.pinNumber}: {(p.hasFault ? $"⚠ (D{p.correctPinNumber})" : "✓")}{(p.hasLooseCable?" ⚠ Cable":"")}");
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "Sin componentes.";
    }

    /// <summary>Genera el texto de objetivo para el reto activo.</summary>
    private string BuildObjetivo() => gameManager.currentLevel switch
    {
        LevelType.OhmLaw   => "RETO 1\nCalcula R con V = I × R\nEnvíala al Explorador",
        LevelType.Parallel => "RETO 2\nIdentifica la rama paralela sin corriente",
        LevelType.Mixed    => "RETO 3\n3 fallas: corregir en orden prioridad",
        LevelType.Arduino  => "RETO 4\nPin + cable suelto + resistencia buzzer",
        _                  => "—"
    };

    /// <summary>Genera el texto "Paso X de Y" según el nivel.</summary>
    private string BuildPaso()
    {
        if (instructionSystem == null) return "Paso — / —";
        int total = gameManager.currentLevel switch
        {
            LevelType.OhmLaw => 4, LevelType.Parallel => 3,
            LevelType.Mixed  => 4, LevelType.Arduino  => 5, _ => 4
        };
        return $"Paso {instructionSystem.currentStep + 1} de {total}";
    }

    private void Set   (TMP_Text t, string s) { if (t != null) t.text = s; }
    private void Set   (Text     t, string s) { if (t != null) t.text = s; }
    private void SetBtn(Button   b, bool   v) { if (b != null) b.interactable = v; }
}