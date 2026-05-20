using UnityEngine;

/// <summary>
/// Sistema de pasos e instrucciones del reto activo.
/// Valida automáticamente las condiciones de cada paso y avanza
/// cuando el Explorador o el Técnico cumplen el requisito.
/// </summary>
/// <remarks>
/// Flujo por reto:
/// <list type="bullet">
///   <item>Reto 1: medir → verificar voltaje → seleccionar resistencia → reemplazar</item>
///   <item>Reto 2: medir → identificar rama rota → reconectar</item>
///   <item>Reto 3: capacitor → LED → resistencia</item>
///   <item>Reto 4: localizar pin → mover cable → instalar resistencia → reconectar cable suelto</item>
/// </list>
/// </remarks>
public class InstructionSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    /// <summary>Paso actual del reto (0-based). Solo lectura en runtime.</summary>
    [Header("Estado")]
    public int currentStep = 0;

    /// <summary>Multímetro del Explorador — se consulta para validar mediciones.</summary>
    [Header("Referencias")]
    public Multimeter multimeter;

    /// <summary>Gestor del juego — provee el nivel activo.</summary>
    public GameManager gameManager;

    /// <summary>Acciones del Técnico — se consulta para validar selecciones.</summary>
    public TechnicianActions technicianActions;

    // ─────────────────────────────────────────────
    //  Flags de progreso
    // ─────────────────────────────────────────────

    /// <summary>True cuando el Explorador realizó al menos una medición válida.</summary>
    [Header("Flags de progreso")]
    public bool hasMeasuredCorrectly = false;

    /// <summary>True cuando el Técnico seleccionó el componente correcto.</summary>
    public bool hasSelectedCorrectComponent = false;

    /// <summary>True cuando se aplicó la reparación al circuito.</summary>
    public bool hasAppliedFix = false;

    // ─────────────────────────────────────────────
    //  Instrucciones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Array de instrucciones del reto activo.
    /// Se construye en BuildInstructions() al cargar cada nivel.
    /// </summary>
    [Header("Instrucciones generadas")]
    public string[] instructions;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    /// <summary>Inicializa y construye las instrucciones al arrancar.</summary>
    private void Start()
    {
        ResetInstructions();
        BuildInstructions();
    }

    private bool _needsValidation = true;

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged += OnCircuitChanged;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= OnCircuitChanged;
    }

    private void OnCircuitChanged()
    {
        _needsValidation = true;
    }

    /// <summary>Valida el paso actual solo cuando el circuito cambia.</summary>
    private void Update()
    {
        if (gameManager == null || !_needsValidation) return;
        ValidateCurrentStep();
        _needsValidation = false;
    }

    // ─────────────────────────────────────────────
    //  Construcción de instrucciones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Genera el array de instrucciones según el nivel activo.
    /// Llamar al cargar cada nivel (GameManager.OnLevelLoaded).
    /// </summary>
    public void BuildInstructions()
    {
        if (gameManager == null) return;

        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                instructions = new[]
                {
                    "Paso 1: Pide al Explorador conectar el multímetro a dos nodos.",
                    "Paso 2: Lee el voltaje medido e identifica la anomalía.",
                    "Paso 3: Selecciona la resistencia defectuosa en el panel.",
                    "Paso 4: Calcula el valor correcto y envíalo al Explorador."
                };
                break;

            case LevelType.Parallel:
                instructions = new[]
                {
                    "Paso 1: Pide al Explorador medir voltaje en cada sensor.",
                    "Paso 2: Identifica la rama con voltaje 0V (rama rota).",
                    "Paso 3: Autoriza la reparación del circuito paralelo."
                };
                break;

            case LevelType.Mixed:
                instructions = new[]
                {
                    "Paso 1: Localiza el capacitor con humo — prioridad máxima.",
                    "Paso 2: Indica al Explorador girar el capacitor 180°.",
                    "Paso 3: Indica girar el LED invertido 180°.",
                    "Paso 4: Calcula la resistencia correcta (220Ω) y envíala."
                };
                break;

            case LevelType.Arduino:
                instructions = new[]
                {
                    "Paso 1: Pide al Explorador localizar el cable del sensor.",
                    "Paso 2: Indica mover el cable del pin D4 al pin D2.",
                    "Paso 3: Envía la resistencia de 330Ω para el buzzer.",
                    "Paso 4: Indica reconectar el cable suelto en protoboard.",
                    "Paso 5: Verifica señal en el monitor serial."
                };
                break;

            default:
                instructions = new[] { "Nivel en desarrollo." };
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  Validación automática de pasos
    // ─────────────────────────────────────────────

    /// <summary>Delega la validación al método correspondiente del reto activo.</summary>
    private void ValidateCurrentStep()
    {
        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:   ValidateOhmLaw();   break;
            case LevelType.Parallel: ValidateParallel();  break;
            case LevelType.Mixed:    ValidateMixed();     break;
            case LevelType.Arduino:  ValidateArduino();   break;
        }
    }

    // ── Reto 1 ─────────────────────────────────

    /// <summary>Valida los 4 pasos del Reto 1 — Ley de Ohm.</summary>
    private void ValidateOhmLaw()
    {
        switch (currentStep)
        {
            // Paso 0 → Explorador conecta ambas puntas
            case 0:
                if (multimeter?.probeA != null && multimeter?.probeB != null)
                    NextStep();
                break;

            // Paso 1 → Se lee un voltaje real
            case 1:
                if (multimeter != null && multimeter.measuredVoltage > 0f)
                {
                    hasMeasuredCorrectly = true;
                    NextStep();
                }
                break;

            // Paso 2 → Técnico selecciona la resistencia
            case 2:
                if (technicianActions != null && technicianActions.HasSelectedResistor())
                {
                    hasSelectedCorrectComponent = true;
                    NextStep();
                }
                break;

            // Paso 3 → Se aplica la reparación
            case 3:
                if (gameManager.HasPerformedRepair())
                    hasAppliedFix = true;
                break;
        }
    }

    // ── Reto 2 ─────────────────────────────────

    /// <summary>Valida los 3 pasos del Reto 2 — Circuito Paralelo.</summary>
    private void ValidateParallel()
    {
        switch (currentStep)
        {
            case 0:
                if (multimeter?.probeA != null && multimeter?.probeB != null)
                    NextStep();
                break;

            case 1:
                if (multimeter != null && multimeter.measuredVoltage > 0.1f)
                {
                    hasMeasuredCorrectly = true;
                    NextStep();
                }
                break;

            case 2:
                if (gameManager.HasPerformedRepair())
                    hasAppliedFix = true;
                break;
        }
    }

    // ── Reto 3 ─────────────────────────────────

    /// <summary>Valida los 4 pasos del Reto 3 — Circuito Mixto y Polaridad.</summary>
    private void ValidateMixed()
    {
        if (gameManager?.circuit == null) return;

        switch (currentStep)
        {
            // Paso 0 → Técnico detecta el capacitor humeante.
            // Auto-avanza en el primer tick: el capacitor ya está invertido al inicio.
            case 0:
                foreach (var comp in gameManager.circuit.components)
                    if (comp is Capacitor cap && cap.polarityInverted)
                    { NextStep(); return; }
                break;

            // Paso 1 → Explorador voltea el capacitor (prioridad: humo = riesgo crítico)
            case 1:
                foreach (var comp in gameManager.circuit.components)
                    if (comp is Capacitor cap && !cap.polarityInverted)
                    { NextStep(); return; }
                break;

            // Paso 2 → Explorador voltea el LED invertido
            case 2:
                bool ledCorregido = true;
                foreach (var comp in gameManager.circuit.components)
                    if (comp is LED led && led.polarityInverted)
                    { ledCorregido = false; break; }
                if (ledCorregido) NextStep();
                break;

            // Paso 3 → Técnico envió 220Ω y Explorador instaló la resistencia
            case 3:
                bool resCorregida = true;
                foreach (var comp in gameManager.circuit.components)
                    if (comp is Resistor r && r.hasFault)
                    { resCorregida = false; break; }
                if (resCorregida && gameManager.circuit.components.Count > 0)
                    hasAppliedFix = true;
                break;
        }
    }

    // ── Reto 4 ─────────────────────────────────

    /// <summary>Valida los 5 pasos del Reto 4 — Sensor-Actuador Arduino.</summary>
    private void ValidateArduino()
    {
        if (gameManager?.circuit == null) return;

        switch (currentStep)
        {
            // Paso 0 → Explorador localiza el cable (multímetro en escena)
            case 0:
                if (multimeter?.probeA != null) NextStep();
                break;

            // Paso 1 → Pin del sensor corregido
            case 1:
                foreach (var comp in gameManager.circuit.components)
                    if (comp is ArduinoPin pin && !pin.hasFault)
                    { NextStep(); return; }
                break;

            // Paso 2 → Resistencia del buzzer correcta
            case 2:
                foreach (var comp in gameManager.circuit.components)
                    if (comp is Resistor r && !r.hasFault)
                    { NextStep(); return; }
                break;

            // Paso 3 → Cable suelto reconectado
            case 3:
            {
                bool cableOk = true;
                foreach (var comp in gameManager.circuit.components)
                    if (comp is ArduinoPin p && p.hasLooseCable)
                    { cableOk = false; break; }
                if (cableOk && gameManager.HasPerformedRepair()) NextStep();
                break;
            }

            // Paso 4 → Verificación final (manual — el jugador confirma la señal)
            case 4:
                if (gameManager.HasPerformedRepair())
                    hasAppliedFix = true;
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Devuelve el texto de la instrucción del paso actual.
    /// Retorna "Procedimiento completado." si se superaron todos los pasos.
    /// </summary>
    public string GetCurrentInstruction()
    {
        if (instructions == null || instructions.Length == 0)
            return "Sin instrucciones.";

        return currentStep < instructions.Length
            ? instructions[currentStep]
            : "Procedimiento completado.";
    }

    /// <summary>
    /// Avanza al siguiente paso y lo registra en consola.
    /// </summary>
    public void NextStep()
    {
        currentStep++;
        Debug.Log($"[InstructionSystem] ➡ Paso {currentStep}");
    }

    /// <summary>
    /// Reinicia todos los flags y el contador de pasos.
    /// Llamar al cargar un nuevo nivel.
    /// </summary>
    public void ResetInstructions()
    {
        currentStep                 = 0;
        hasMeasuredCorrectly        = false;
        hasSelectedCorrectComponent = false;
        hasAppliedFix               = false;
    }

    /// <summary>
    /// True cuando el Técnico puede enviar la resistencia al Explorador.
    /// Requiere haber medido y seleccionado el componente correcto (paso 3+).
    /// </summary>
    public bool CanRepairResistor()
        => currentStep >= 3 && hasMeasuredCorrectly && hasSelectedCorrectComponent;

    /// <summary>
    /// True cuando el Técnico puede autorizar la reparación del paralelo.
    /// Requiere haber completado las mediciones (paso 2+).
    /// </summary>
    public bool CanRepairParallel()
        => currentStep >= 2 && hasMeasuredCorrectly;
}