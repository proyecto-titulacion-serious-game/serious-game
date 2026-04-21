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
///
/// MODO DEMO PC: activar <see cref="demoMode"/> = true en el inspector para
/// que el botón ENVIAR se active simplemente al escribir un valor,
/// sin esperar que el Explorador complete los pasos de medición.
/// </remarks>
public class TechnicianUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Modo demo
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa el modo demo para pruebas en PC sin Explorador VR.
    /// Con demoMode=true el botón ENVIAR se habilita simplemente al
    /// escribir un valor en el InputField, sin requerir que el
    /// InstructionSystem haya completado los pasos previos.
    /// Desactivar cuando se juegue con ambos roles.
    /// </summary>
    [Header("Modo de prueba")]
    public bool demoMode = true;

    // ─────────────────────────────────────────────
    //  Referencias de sistemas
    // ─────────────────────────────────────────────

    /// <summary>Gestor del circuito eléctrico — fuente de todos los datos de los paneles.</summary>
    [Header("Referencias de sistemas")]
    public CircuitManager circuit;

    /// <summary>
    /// Multímetro virtual del Explorador.
    /// Sus lecturas (punta roja, negra, voltaje medido) se muestran en Panel_Datos.
    /// Puede quedar en None si no hay Explorador activo.
    /// </summary>
    public Multimeter multimeter;

    /// <summary>Gestor principal del juego — controla el nivel activo y los eventos de reto.</summary>
    public GameManager gameManager;

    /// <summary>Registro de tiempo transcurrido y errores cometidos en la sesión.</summary>
    public PerformanceTracker performance;

    /// <summary>
    /// Sistema de pasos e instrucciones del reto activo.
    /// Determina cuándo se habilitan los botones de acción.
    /// </summary>
    public InstructionSystem instructionSystem;

    /// <summary>
    /// Acciones disponibles para el Técnico (seleccionar componente, reparar).
    /// Se consulta para mostrar el componente seleccionado en Panel_Diagnostico.
    /// </summary>
    public TechnicianActions technicianActions;

    /// <summary>
    /// Sistema de entrega de componentes al Explorador.
    /// Se llama desde los botones de acción para enviar resistencias, LEDs, etc.
    /// </summary>
    public ComponentDeliverySystem delivery;

    /// <summary>
    /// Proveedor del contenido del manual técnico por reto.
    /// Devuelve título, concepto, fórmulas, objetivo y tabla de valores.
    /// </summary>
    public TechnicianManual manual;

    // ─────────────────────────────────────────────
    //  Panel_Datos — datos en tiempo real
    // ─────────────────────────────────────────────

    /// <summary>
    /// Voltaje de la fuente de alimentación del circuito.
    /// Formato: "Fuente: 9.0 V"
    /// </summary>
    [Header("Panel_Datos — TMPs")]
    public TMP_Text txtVoltajeFuente;

    /// <summary>
    /// Corriente total del circuito en miliamperes con indicador de estado.
    /// Formato: "Corriente: 150.0 mA ⚠ SOBRECARGA" o "60.0 mA ✓"
    /// </summary>
    public TMP_Text txtCorrienteTotal;

    /// <summary>
    /// Estado de cada componente del circuito (resistencia, LED, capacitor, pin Arduino).
    /// Muestra valor actual, si hay falla y corriente que pasa por él.
    /// </summary>
    public TMP_Text txtEstadoComponentes;

    /// <summary>
    /// Lectura del multímetro del Explorador.
    /// Muestra el nombre del nodo en cada punta y el voltaje medido.
    /// Se actualiza en tiempo real mientras el Explorador mide.
    /// </summary>
    public TMP_Text txtMultimetro;

    // ─────────────────────────────────────────────
    //  Panel_Instrucciones — manual técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// Título del manual del reto activo.
    /// Ejemplo: "RETO 1 — Circuito Serie &amp; Ley de Ohm"
    /// </summary>
    [Header("Panel_Instrucciones — TMPs")]
    public TMP_Text txtManualTitulo;

    /// <summary>
    /// Explicación del concepto eléctrico del reto.
    /// Ej: "En un circuito serie la misma corriente fluye por todos los componentes."
    /// </summary>
    public TMP_Text txtConcepto;

    /// <summary>
    /// Fórmulas necesarias para resolver el reto.
    /// Ej: "V = I × R | I = V / R_total | R_t = R1 + R2 + ..."
    /// </summary>
    public TMP_Text txtFormula;

    /// <summary>
    /// Objetivo específico del reto: qué debe diagnosticar y enviar el Técnico.
    /// Incluye los pasos numerados que guían al Técnico.
    /// </summary>
    public TMP_Text txtObjetivoManual;

    /// <summary>
    /// Tabla de referencia del reto activo.
    /// Reto 1-3: código de colores de resistencias.
    /// Reto 4: pinout del Arduino.
    /// </summary>
    public TMP_Text txtTablaValores;

    /// <summary>
    /// Imagen del diagrama del circuito activo.
    /// El sprite se selecciona automáticamente desde <see cref="diagramasPorReto"/>
    /// según el índice del nivel.
    /// </summary>
    public Image imgDiagrama;

    /// <summary>
    /// Array de sprites de diagramas, uno por reto.
    /// Índice 0 = Reto1, 1 = Reto2, 2 = Reto3, 3 = Reto4.
    /// Arrastrar los sprites desde la carpeta Assets/Sprites en el inspector.
    /// Puede dejarse vacío — el campo Img_Diagrama simplemente no cambiará.
    /// </summary>
    public Sprite[] diagramasPorReto;

    // ─────────────────────────────────────────────
    //  Panel_Diagnostico — objetivo y diagnóstico
    // ─────────────────────────────────────────────

    /// <summary>
    /// Objetivo resumido del reto activo para el Técnico.
    /// Ej: "RETO 1\nCalcula R con V = I × R\nEnvíala al Explorador"
    /// </summary>
    [Header("Panel_Diagnostico — TMPs")]
    public TMP_Text txtObjetivoReto;

    /// <summary>
    /// Instrucción textual del paso actual del <see cref="InstructionSystem"/>.
    /// Ej: "Paso 1: Pide al Explorador conectar el multímetro a dos nodos."
    /// </summary>
    public TMP_Text txtInstruccionActual;

    /// <summary>
    /// Indicador de progreso del reto.
    /// Formato: "Paso 2 de 4"
    /// </summary>
    public TMP_Text txtPasoActual;

    /// <summary>
    /// Nombre del componente que el Técnico seleccionó para diagnosticar o reparar.
    /// Formato: "Seleccionado: Resistor"
    /// </summary>
    public TMP_Text txtComponenteSeleccionado;

    /// <summary>
    /// Diagnóstico automático generado por <see cref="DiagnosticSystem"/>.
    /// Analiza el estado del circuito y sugiere la acción correcta al Técnico.
    /// </summary>
    public TMP_Text txtDiagnostico;

    /// <summary>
    /// Estado del sistema de entrega de componentes.
    /// Ej: "📦 Resistencia (100Ω) → Explorador…" / "✅ Instalado" / "❌ Valor incorrecto."
    /// </summary>
    public TMP_Text txtEstadoEnvio;

    // ─────────────────────────────────────────────
    //  Panel_Botones — acciones del Técnico
    // ─────────────────────────────────────────────

    /// <summary>
    /// Campo de texto donde el Técnico escribe el valor de resistencia calculado.
    /// Configurar en Unity: Content Type = Decimal Number, Character Limit = 6.
    /// El botón ENVIAR se activa automáticamente al detectar texto aquí.
    /// </summary>
    [Header("Panel_Botones — controles")]
    public TMP_InputField inputValorResistencia;

    /// <summary>
    /// Botón para enviar la resistencia calculada al Explorador (Reto 1 y 3).
    /// En demoMode: se activa al escribir cualquier valor en el InputField.
    /// En modo normal: requiere que InstructionSystem.CanRepairResistor() sea true.
    /// OnClick → TechnicianUI.OnClickEnviarResistor()
    /// </summary>
    public Button btnEnviarResistor;

    /// <summary>
    /// Botón para autorizar la reparación del circuito paralelo (Reto 2).
    /// Se activa cuando InstructionSystem.CanRepairParallel() es true.
    /// OnClick → TechnicianUI.OnClickRepararParalelo()
    /// </summary>
    public Button btnRepararParalelo;

    /// <summary>
    /// Tiempo transcurrido y número de errores de la sesión actual.
    /// Formato: "⏱ 34s  |  ❌ 1 errores"
    /// </summary>
    public TMP_Text txtDesempeno;

    /// <summary>
    /// Resultado del reto al completarse. Vacío durante el juego.
    /// Ej: "✅ Reto 1 completado\n⭐ Excelente — Sin errores"
    /// </summary>
    public TMP_Text txtResultado;

    // ─────────────────────────────────────────────
    //  Indicadores de estado (opcionales)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Imagen circular que indica si el Explorador VR está conectado.
    /// Verde = conectado, gris = desconectado. Opcional — puede quedar en None.
    /// </summary>
    [Header("Indicadores de estado (todos opcionales)")]
    public Image indicadorConexion;

    /// <summary>
    /// Texto de estado de conexión junto al indicador.
    /// Ej: "Explorador: conectado" / "Explorador: desconectado"
    /// </summary>
    public TMP_Text txtConexion;

    /// <summary>
    /// Panel que se activa mientras hay un componente en tránsito al Explorador.
    /// Debe tener SetActive=false en el inspector al inicio.
    /// Se activa/desactiva automáticamente por los eventos de ComponentDeliverySystem.
    /// </summary>
    public GameObject panelEntregaPendiente;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────

    /// <summary>
    /// Motor de diagnóstico automático.
    /// Clase pura (sin MonoBehaviour) instanciada en memoria — no necesita
    /// estar adjunta a ningún GameObject en la jerarquía.
    /// </summary>
    private readonly DiagnosticSystem _diagnostic = new DiagnosticSystem();

    /// <summary>Acumulador de tiempo para limitar la frecuencia de actualización de la UI.</summary>
    private float _timer;

    /// <summary>
    /// Intervalo entre actualizaciones de la UI en segundos.
    /// 0.1 = 10 veces por segundo. Reducir si la UI parece lenta; aumentar para mejor rendimiento.
    /// </summary>
    private const float INTERVALO = 0.1f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    /// <summary>
    /// Suscribe los métodos de callback a los eventos estáticos de
    /// GameManager y ComponentDeliverySystem al activar el objeto.
    /// </summary>
    private void OnEnable()
    {
        GameManager.OnLevelLoaded                    += OnLevelLoaded;
        GameManager.OnLevelCompleted                 += OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      += OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled += OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      += OnDeliveryError;
    }

    /// <summary>
    /// Desuscribe todos los callbacks al desactivar el objeto
    /// para evitar referencias colgantes entre escenas.
    /// </summary>
    private void OnDisable()
    {
        GameManager.OnLevelLoaded                    -= OnLevelLoaded;
        GameManager.OnLevelCompleted                 -= OnLevelCompleted;
        ComponentDeliverySystem.OnComponentSent      -= OnComponentSent;
        ComponentDeliverySystem.OnComponentInstalled -= OnComponentInstalled;
        ComponentDeliverySystem.OnDeliveryError      -= OnDeliveryError;
    }

    /// <summary>
    /// Actualiza la UI a intervalos regulares definidos por <see cref="INTERVALO"/>.
    /// No actualiza en cada frame para no saturar el render.
    /// </summary>
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

    /// <summary>
    /// Refresca los 4 paneles del Técnico en orden.
    /// Solo ejecuta si gameManager y circuit están asignados.
    /// </summary>
    private void RefreshAllPanels()
    {
        if (gameManager == null || circuit == null) return;
        RefreshPanelDatos();
        RefreshPanelInstrucciones();
        RefreshPanelDiagnostico();
        RefreshPanelBotones();
    }

    /// <summary>
    /// Actualiza Panel_Datos con el voltaje de la fuente, la corriente total,
    /// el estado de cada componente y la lectura del multímetro del Explorador.
    /// </summary>
    private void RefreshPanelDatos()
    {
        // Voltaje de la fuente
        float vSource = 0f;
        foreach (var c in circuit.components)
            if (c is VoltageSource vs) { vSource = vs.voltage; break; }

        Set(txtVoltajeFuente,     $"Fuente: {vSource:F1} V");
        Set(txtCorrienteTotal,    FormatCorriente(circuit.totalCurrent));
        Set(txtEstadoComponentes, BuildEstadoComponentes());

        // Multímetro del Explorador
        if (multimeter != null)
        {
            string pA = multimeter.probeA != null ? multimeter.probeA.name : "—";
            string pB = multimeter.probeB != null ? multimeter.probeB.name : "—";
            float  v  = multimeter.measuredVoltage;
            Set(txtMultimetro,
                $"Roja:  {pA}\nNegra: {pB}\n" +
                $"Voltaje: {v:F2} V");
        }
        else
        {
            Set(txtMultimetro, "Sin explorador conectado");
        }
    }

    /// <summary>
    /// Actualiza Panel_Instrucciones con el manual técnico del reto activo:
    /// título, concepto, fórmulas, objetivo, tabla de valores y diagrama.
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

        // Diagrama del circuito según el reto activo
        if (imgDiagrama != null && diagramasPorReto != null)
        {
            int idx = (int)gameManager.currentLevel;
            if (idx < diagramasPorReto.Length && diagramasPorReto[idx] != null)
                imgDiagrama.sprite = diagramasPorReto[idx];
        }
    }

    /// <summary>
    /// Actualiza Panel_Diagnostico con el objetivo del reto, la instrucción
    /// del paso actual, el componente seleccionado y el diagnóstico automático.
    /// </summary>
    private void RefreshPanelDiagnostico()
    {
        Set(txtObjetivoReto,           BuildObjetivo());
        Set(txtInstruccionActual,      instructionSystem?.GetCurrentInstruction() ?? "—");
        Set(txtPasoActual,             BuildPaso());
        Set(txtComponenteSeleccionado, $"Seleccionado: {technicianActions?.GetSelectedComponentName() ?? "—"}");
        Set(txtDiagnostico,            _diagnostic.GetDiagnosis(circuit.components, circuit.totalCurrent));
    }

    /// <summary>
    /// Actualiza Panel_Botones: habilita/deshabilita los botones según
    /// el estado del reto y actualiza el contador de desempeño.
    /// </summary>
    private void RefreshPanelBotones()
    {
        bool hayValorEscrito = !string.IsNullOrEmpty(inputValorResistencia?.text);

        // En demoMode el botón se activa simplemente al escribir un valor.
        // En modo normal requiere que InstructionSystem haya completado los pasos previos.
        bool puedeEnviarR;
        if (demoMode)
        {
            puedeEnviarR = gameManager.currentLevel == LevelType.OhmLaw && hayValorEscrito;
        }
        else
        {
            puedeEnviarR = gameManager.currentLevel == LevelType.OhmLaw
                        && (instructionSystem?.CanRepairResistor() ?? false)
                        && hayValorEscrito;
        }

        bool puedeParalelo = gameManager.currentLevel == LevelType.Parallel
                          && (instructionSystem?.CanRepairParallel() ?? false);

        SetBtn(btnEnviarResistor,  puedeEnviarR);
        SetBtn(btnRepararParalelo, puedeParalelo);

        // Desempeño: tiempo + errores
        if (performance != null)
            Set(txtDesempeno, $"{performance.GetTime():F0}s  |  {performance.GetErrors()} errores");
    }

    // ─────────────────────────────────────────────
    //  Métodos públicos — OnClick de los botones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado por <c>Btn_EnviarResistor.OnClick()</c>.
    /// Parsea el valor del InputField y lo envía al sistema de entrega.
    /// Si el valor es incorrecto muestra un mensaje de error en <see cref="txtEstadoEnvio"/>.
    /// </summary>
    public void OnClickEnviarResistor()
    {
        if (inputValorResistencia == null)
        {
            Set(txtEstadoEnvio, "⚠ InputField no asignado.");
            return;
        }

        if (!float.TryParse(inputValorResistencia.text, out float valor))
        {
            Set(txtEstadoEnvio, "⚠ Ingresa un número válido.");
            return;
        }

        if (delivery != null)
        {
            delivery.SendResistor(valor);
        }
        else
        {
            // Sin sistema de entrega (demo sin Explorador): aplicar directamente
            ApplyResistorDirectly(valor);
        }
    }

    /// <summary>
    /// Llamado por <c>Btn_RepararParalelo.OnClick()</c>.
    /// Autoriza la reparación de la rama paralela rota (Reto 2).
    /// </summary>
    public void OnClickRepararParalelo() => technicianActions?.FixParallelCircuit();

    /// <summary>
    /// Llamado por <c>Btn_EnviarLED.OnClick()</c> (Reto 3).
    /// Envía un LED con polaridad correcta al Explorador.
    /// </summary>
    public void OnClickEnviarLED()       => delivery?.SendLED(true);

    /// <summary>
    /// Llamado por <c>Btn_EnviarCapacitor.OnClick()</c> (Reto 3).
    /// Envía un capacitor con polaridad correcta al Explorador.
    /// </summary>
    public void OnClickEnviarCapacitor() => delivery?.SendCapacitor(true);

    // ─────────────────────────────────────────────
    //  Demo sin Explorador — aplicación directa
    // ─────────────────────────────────────────────

    /// <summary>
    /// Aplica la resistencia directamente al circuito sin pasar por el sistema de entrega.
    /// Se usa en <see cref="demoMode"/> cuando no hay Explorador VR activo.
    /// Si el valor es correcto, el LED cambiará a verde automáticamente.
    /// </summary>
    /// <param name="valor">Valor de resistencia en ohms ingresado por el Técnico.</param>
    private void ApplyResistorDirectly(float valor)
    {
        if (circuit == null) return;

        bool valido = false;

        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                if (r.IsValueCorrect(valor))
                {
                    r.resistance = valor;
                    r.hasFault   = false;
                    valido       = true;
                }
                else
                {
                    Set(txtEstadoEnvio,
                        $"❌ {valor:F0}Ω incorrecto.\nValor correcto: {r.correctResistance:F0}Ω\n" +
                        $"Código: {r.GetColorBandString()}");
                    gameManager?.RegisterWrongAttempt($"Resistencia incorrecta: {valor}Ω");
                    return;
                }
                break;
            }
        }

        if (valido)
        {
            circuit.MarkDirty();
            gameManager?.RegisterRepairAction();
            Set(txtEstadoEnvio, $"✅ Resistencia {valor:F0}Ω aplicada — observa el LED");
        }
    }

    // ─────────────────────────────────────────────
    //  Callbacks de eventos
    // ─────────────────────────────────────────────

    /// <summary>
    /// Se llama cuando GameManager carga un nuevo nivel.
    /// Limpia el InputField, el estado de envío y el resultado anterior,
    /// y actualiza el manual técnico para el nuevo reto.
    /// </summary>
    private void OnLevelLoaded(LevelType level)
    {
        Set(txtEstadoEnvio, "");
        Set(txtResultado,   "");

        if (inputValorResistencia != null)
            inputValorResistencia.text = "";

        if (panelEntregaPendiente != null)
            panelEntregaPendiente.SetActive(false);

        RefreshPanelInstrucciones();
    }

    /// <summary>
    /// Se llama cuando el nivel se completa (con éxito o fallo).
    /// Muestra el resultado y la evaluación de desempeño en <see cref="txtResultado"/>.
    /// </summary>
    private void OnLevelCompleted(LevelType level, bool success)
    {
        string eval = performance?.GetEvaluation() ?? "";

        Set(txtResultado, success
            ? $"RETO {(int)level + 1} COMPLETADO\n{eval}"
            : $"RETO {(int)level + 1} FALLIDO");
    }

    /// <summary>
    /// Se llama cuando el Técnico envía un componente al Explorador.
    /// Muestra el nombre y valor del componente en tránsito y activa el panel de entrega.
    /// </summary>
    private void OnComponentSent(ComponentType tipo, float valor)
    {
        Set(txtEstadoEnvio, $"{tipo} ({valor:F0}) enviado al Explorador...");

        if (panelEntregaPendiente != null)
            panelEntregaPendiente.SetActive(true);
    }

    /// <summary>
    /// Se llama cuando el Explorador instala el componente en el slot del panel.
    /// Actualiza el estado de envío y desactiva el panel de entrega pendiente.
    /// </summary>
    private void OnComponentInstalled(bool success)
    {
        Set(txtEstadoEnvio, success ? "Componente instalado" : "Slot incorrecto");

        if (panelEntregaPendiente != null)
            panelEntregaPendiente.SetActive(false);
    }

    /// <summary>
    /// Se llama cuando el Técnico envía un componente con valor incorrecto.
    /// El <see cref="DiagnosticSystem"/> sugiere el valor correcto automáticamente.
    /// </summary>
    private void OnDeliveryError()
    {
        Set(txtEstadoEnvio, "Valor incorrecto. Recalcula con las formulas del manual.");

        if (panelEntregaPendiente != null)
            panelEntregaPendiente.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Helpers privados
    // ─────────────────────────────────────────────

    /// <summary>
    /// Formatea la corriente total con un indicador visual de estado.
    /// </summary>
    /// <param name="i">Corriente en Amperes.</param>
    /// <returns>Texto formateado con unidades en mA e indicador de estado.</returns>
    private string FormatCorriente(float i)
    {
        float  mA     = i * 1000f;
        string estado = mA > 20f  ? " SOBRECARGA" :
                        mA < 5f   ? " MUY BAJA"   : " OK";
        return $"Corriente: {mA:F1} mA{estado}";
    }

    /// <summary>
    /// Construye el texto de estado de cada componente del circuito.
    /// Incluye valor actual, indicador de falla y corriente.
    /// </summary>
    /// <returns>Texto multilínea con el estado de todos los componentes.</returns>
    private string BuildEstadoComponentes()
    {
        var sb = new StringBuilder();

        foreach (var c in circuit.components)
        {
            if (c is Resistor r)
                sb.AppendLine($"R: {r.resistance:F0} Ohm {(r.hasFault ? "FALLA" : "OK")}");

            else if (c is LED led)
                sb.AppendLine($"LED: {(led.isOn ? "ENCENDIDO" : "APAGADO")} " +
                              $"I={led.current * 1000f:F1}mA" +
                              $"{(led.polarityInverted ? " INV" : "")}");

            else if (c is Capacitor cap)
                sb.AppendLine($"Cap: {(cap.polarityInverted ? "INVERTIDO" : "OK")}");

            else if (c is ArduinoPin pin)
                sb.AppendLine($"Pin D{pin.pinNumber}: " +
                              $"{(pin.hasFault ? $"INCORRECTO (D{pin.correctPinNumber})" : "OK")}" +
                              $"{(pin.hasLooseCable ? " Cable suelto" : "")}");
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "Sin componentes.";
    }

    /// <summary>
    /// Genera el texto del objetivo del reto activo para mostrar en Panel_Diagnostico.
    /// </summary>
    private string BuildObjetivo() => gameManager.currentLevel switch
    {
        LevelType.OhmLaw   => "RETO 1\nCalcula R con V = I x R\nEnviala al Explorador",
        LevelType.Parallel => "RETO 2\nIdentifica la rama paralela\nsin corriente",
        LevelType.Mixed    => "RETO 3\n3 fallas: corregir en\norden de prioridad",
        LevelType.Arduino  => "RETO 4\nPin incorrecto + cable suelto\n+ resistencia buzzer",
        _                  => "—"
    };

    /// <summary>
    /// Genera el texto de progreso de pasos del reto activo.
    /// </summary>
    /// <returns>Formato: "Paso X de Y"</returns>
    private string BuildPaso()
    {
        if (instructionSystem == null) return "Paso — / —";

        int total = gameManager.currentLevel switch
        {
            LevelType.OhmLaw   => 4,
            LevelType.Parallel => 3,
            LevelType.Mixed    => 4,
            LevelType.Arduino  => 5,
            _                  => 4
        };

        return $"Paso {instructionSystem.currentStep + 1} de {total}";
    }

    /// <summary>Asigna un texto a un TMP_Text con verificación de null.</summary>
    private void Set(TMP_Text t, string s) { if (t != null) t.text = s; }

    /// <summary>Asigna un texto a un Text legacy con verificación de null.</summary>
    private void Set(Text t, string s)     { if (t != null) t.text = s; }

    /// <summary>Cambia el estado interactable de un Button con verificación de null.</summary>
    private void SetBtn(Button b, bool v)  { if (b != null) b.interactable = v; }
}