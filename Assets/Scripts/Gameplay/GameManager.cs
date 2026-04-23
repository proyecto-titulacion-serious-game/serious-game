using System;
using UnityEngine;

/// <summary>
/// Controlador principal del juego. Gestiona los 4 retos del Serious Game VR.
/// Usa eventos para comunicarse con UI y otros sistemas (sin acoplamiento directo).
///
/// CAMBIOS RESPECTO A LA VERSIÓN ANTERIOR:
///   - SpawnCircuitForLevel() DESACTIVADO — se usa el Circuit de la escena directamente
///   - Llamadas duplicadas a SetupLevel/ForceSimulate ELIMINADAS
///   - NUEVO: ActivateComponentsForLevel() desactiva Capacitor/Arduino en Reto 1
///   - NUEVO: AutoDetectComponents() se llama después de activar/desactivar componentes
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias principales")]
    public CircuitManager     circuit;
    public Multimeter         multimeter;
    public PerformanceTracker performance;
    public InstructionSystem  instructionSystem;

    [Header("Configuración de niveles")]
    [Tooltip("Tiempo límite en segundos para cada reto (0 = sin límite).")]
    public float[] timeLimits = { 480f, 600f, 720f, 900f };

    // ─────────────────────────────────────────────
    //  Estado (solo lectura desde inspector)
    // ─────────────────────────────────────────────
    [Header("Estado actual (solo lectura)")]
    [SerializeField] private LevelType _currentLevel   = LevelType.OhmLaw;
    [SerializeField] private int       _currentIndex   = 0;
    [SerializeField] private bool      _levelCompleted = false;
    [SerializeField] private bool      _repairPerformed = false;
    [SerializeField] private int       _wrongAttempts  = 0;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public LevelType currentLevel    => _currentLevel;
    public bool      levelCompleted  => _levelCompleted;
    public float     currentTimeLimit => timeLimits[_currentIndex];

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    public static event Action<LevelType>       OnLevelLoaded;
    public static event Action<LevelType, bool> OnLevelCompleted;
    public static event Action<string>          OnFaultDetected;
    public static event Action                  OnGameCompleted;

    // ─────────────────────────────────────────────
    //  Constantes de configuración de retos
    // ─────────────────────────────────────────────
    private const float RETO1_FAULTY_RESISTANCE  = 10f;
    private const float RETO1_CORRECT_RESISTANCE = 100f;
    private const float RETO1_LED_RESISTANCE     = 50f;
    private const float RETO1_SOURCE_VOLTAGE     = 9f;
    private const float RETO1_TARGET_VOLTAGE     = 9f;
    private const float RETO1_TOLERANCE          = 0.5f;

    private const float RETO2_BROKEN_RESISTANCE  = 9999f;
    private const float RETO2_NORMAL_RESISTANCE  = 50f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        CircuitManager.OnCircuitChanged += CheckWinCondition;
        LoadLevel(0);
    }

    void OnDestroy()
    {
        CircuitManager.OnCircuitChanged -= CheckWinCondition;
    }

    // ─────────────────────────────────────────────
    //  API Pública
    // ─────────────────────────────────────────────

    /// <summary>Registra que se realizó una reparación y resimula el circuito.</summary>
    public void RegisterRepairAction()
    {
        _repairPerformed = true;
        circuit?.MarkDirty();
        Debug.Log("[GameManager] Reparación registrada.");
    }

    /// <summary>Registra un intento incorrecto del Técnico.</summary>
    public void RegisterWrongAttempt(string reason = "")
    {
        _wrongAttempts++;
        performance?.AddError(reason);
        Debug.Log($"[GameManager] Intento incorrecto #{_wrongAttempts}: {reason}");
    }

    public bool HasPerformedRepair() => _repairPerformed;
    public int  GetWrongAttempts()   => _wrongAttempts;

    // ─────────────────────────────────────────────
    //  Carga de niveles
    // ─────────────────────────────────────────────

    void LoadLevel(int index)
    {
        if (index >= 4) { CompleteGame(); return; }

        _currentIndex    = index;
        _currentLevel    = (LevelType)index;
        _levelCompleted  = false;
        _repairPerformed = false;
        _wrongAttempts   = 0;

        // Reiniciar sistemas
        performance?.ResetTracker();
        multimeter?.ResetProbes();
        instructionSystem?.ResetInstructions();
        instructionSystem?.BuildInstructions();

        // Activar solo los componentes de este reto
        ActivateComponentsForLevel(_currentLevel);

        // Re-detectar componentes activos
        circuit?.AutoDetectComponents();

        // Configurar el reto
        SetupLevel();

        // Simular el circuito
        circuit?.ForceSimulate();

        // Notificar
        OnLevelLoaded?.Invoke(_currentLevel);
        Debug.Log($"[GameManager] Cargando: {_currentLevel}");
    }

    public void NextLevel() => LoadLevel(_currentIndex + 1);

    void SetupLevel()
    {
        switch (_currentLevel)
        {
            case LevelType.OhmLaw:   SetupReto1(); break;
            case LevelType.Parallel: SetupReto2(); break;
            case LevelType.Mixed:    SetupReto3(); break;
            case LevelType.Arduino:  SetupReto4(); break;
        }
    }

    // ─────────────────────────────────────────────
    //  Activación de componentes por reto
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa solo los componentes necesarios para el reto actual.
    /// Desactiva los demás para evitar que interfieran con la simulación.
    /// </summary>
    void ActivateComponentsForLevel(LevelType level)
    {
        if (circuit == null) return;

        // Primero obtener TODOS los componentes (incluidos los desactivados)
        var allComponents = circuit.GetComponentsInChildren<ElectricalComponent>(true);

        switch (level)
        {
            case LevelType.OhmLaw:
                // Reto 1: solo VoltageSource + Resistor + LED
                foreach (var comp in allComponents)
                {
                    if (comp is VoltageSource || comp is Resistor || comp is LED)
                        comp.gameObject.SetActive(true);
                    else
                        comp.gameObject.SetActive(false);
                }
                break;

            case LevelType.Parallel:
                // Reto 2: VoltageSource + Resistor + LEDs
                foreach (var comp in allComponents)
                {
                    if (comp is VoltageSource || comp is Resistor || comp is LED)
                        comp.gameObject.SetActive(true);
                    else
                        comp.gameObject.SetActive(false);
                }
                break;

            case LevelType.Mixed:
                // Reto 3: VoltageSource + Resistor + LED + Capacitor
                foreach (var comp in allComponents)
                {
                    if (comp is ArduinoPin)
                        comp.gameObject.SetActive(false);
                    else
                        comp.gameObject.SetActive(true);
                }
                break;

            case LevelType.Arduino:
                // Reto 4: Todos activos
                foreach (var comp in allComponents)
                    comp.gameObject.SetActive(true);
                break;
        }

        // Limpiar la lista para que AutoDetectComponents la reconstruya
        circuit.components.Clear();
    }

    // ─────────────────────────────────────────────
    //  RETO 1 — Circuito Serie & Ley de Ohm
    // ─────────────────────────────────────────────

    void SetupReto1()
    {
        if (circuit == null) return;

        circuit.topology = CircuitTopology.Series;

        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                r.faultyResistance  = RETO1_FAULTY_RESISTANCE;
                r.correctResistance = RETO1_CORRECT_RESISTANCE;
                r.ApplyFault();
            }
            if (comp is LED led)
            {
                led.resistance       = RETO1_LED_RESISTANCE;
                led.polarityInverted = false;
            }
            if (comp is VoltageSource vs)
            {
                vs.voltage = RETO1_SOURCE_VOLTAGE;
            }
        }

        OnFaultDetected?.Invoke(
            "Reto 1: La resistencia tiene valor incorrecto.\n" +
            "El Técnico debe calcular el valor correcto usando Ley de Ohm.");
    }

    void CheckReto1()
    {
        if (!_repairPerformed) return;

        // Verificar si la resistencia fue corregida
        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r && !r.hasFault)
            {
                CompleteLevel(true);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  RETO 2 — Circuito Paralelo & Divisor de Corriente
    // ─────────────────────────────────────────────

    void SetupReto2()
    {
        if (circuit == null) return;

        circuit.topology = CircuitTopology.Parallel;

        int index = 0;
        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                led.resistance       = index == 0 ? RETO2_BROKEN_RESISTANCE : RETO2_NORMAL_RESISTANCE;
                led.polarityInverted = false;
                index++;
            }
            if (comp is Resistor r)
            {
                r.Repair();
            }
        }

        OnFaultDetected?.Invoke(
            "Reto 2: Una rama del circuito paralelo está abierta.\n" +
            "El Técnico debe identificar cuál sensor no recibe corriente.");
    }

    void CheckReto2()
    {
        if (!_repairPerformed) return;

        if (circuit.AreAllLEDsOn())
            CompleteLevel(true);
    }

    // ─────────────────────────────────────────────
    //  RETO 3 — Circuito Mixto & Polaridad
    // ─────────────────────────────────────────────

    void SetupReto3()
    {
        if (circuit == null) return;

        circuit.topology = CircuitTopology.Mixed;

        int ledIndex = 0;
        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                led.polarityInverted = (ledIndex == 0);
                ledIndex++;
            }
            if (comp is Capacitor cap)
            {
                cap.SetPolarityInverted(true);
            }
            if (comp is Resistor r)
            {
                r.faultyResistance  = 470f;
                r.correctResistance = 220f;
                r.ApplyFault();
            }
        }

        SetupMixedNodeVoltages();

        OnFaultDetected?.Invoke(
            "Reto 3: 3 fallas simultáneas.\n" +
            "1) LED con polaridad invertida\n" +
            "2) Capacitor con polaridad invertida\n" +
            "3) Resistencia con código de colores erróneo");
    }

    void SetupMixedNodeVoltages()
    {
        VoltageSource source   = null;
        Resistor      resistor = null;

        foreach (var comp in circuit.components)
        {
            if (comp is VoltageSource vs) source   = vs;
            if (comp is Resistor r)       resistor = r;
        }

        if (source == null || resistor == null) return;

        float V = source.voltage;
        float I = V / (resistor.GetResistance() + 50f);
        float vAfterR = V - I * resistor.GetResistance();

        if (resistor.nodeA != null) resistor.nodeA.voltage = V;
        if (resistor.nodeB != null) resistor.nodeB.voltage = vAfterR;

        foreach (var comp in circuit.components)
        {
            if (comp is LED || comp is Capacitor)
            {
                if (comp.nodeA != null) comp.nodeA.voltage = vAfterR;
                if (comp.nodeB != null) comp.nodeB.voltage = 0f;
            }
        }
    }

    void CheckReto3()
    {
        if (!_repairPerformed) return;

        bool ledFixed = true, capFixed = true, resFixed = true;

        foreach (var comp in circuit.components)
        {
            if (comp is LED led        && led.polarityInverted) ledFixed = false;
            if (comp is Capacitor cap  && cap.polarityInverted) capFixed = false;
            if (comp is Resistor r     && r.hasFault)           resFixed = false;
        }

        if (ledFixed && capFixed && resFixed)
            CompleteLevel(true);
    }

    // ─────────────────────────────────────────────
    //  RETO 4 — Sensor-Actuador con Arduino
    // ─────────────────────────────────────────────

    void SetupReto4()
    {
        if (circuit == null) return;

        circuit.topology = CircuitTopology.Mixed;

        foreach (var comp in circuit.components)
        {
            if (comp is ArduinoPin pin)
            {
                pin.ApplyFault();
            }
            if (comp is Resistor r)
            {
                r.faultyResistance  = 0f;
                r.correctResistance = 330f;
                r.ApplyFault();
            }
        }

        OnFaultDetected?.Invoke(
            "Reto 4: Sistema sensor-temperatura no activa alarma.\n" +
            "1) Sensor en pin incorrecto del Arduino\n" +
            "2) Buzzer sin resistencia limitadora\n" +
            "3) Cable suelto en la protoboard");
    }

    void CheckReto4()
    {
        if (!_repairPerformed) return;

        bool allPinsCorrect = true;
        bool resistorFixed  = true;
        bool cableFixed     = true;

        foreach (var comp in circuit.components)
        {
            if (comp is ArduinoPin pin)
            {
                if (pin.hasFault)      allPinsCorrect = false;
                if (pin.hasLooseCable) cableFixed     = false;
            }
            if (comp is Resistor r && r.hasFault) resistorFixed = false;
        }

        if (allPinsCorrect && resistorFixed && cableFixed)
            CompleteLevel(true);
    }

    // ─────────────────────────────────────────────
    //  Verificación de condiciones de victoria
    // ─────────────────────────────────────────────

    /// <summary>Se llama automáticamente cada vez que el circuito cambia.</summary>
    void CheckWinCondition()
    {
        if (_levelCompleted) return;

        switch (_currentLevel)
        {
            case LevelType.OhmLaw:   CheckReto1(); break;
            case LevelType.Parallel: CheckReto2(); break;
            case LevelType.Mixed:    CheckReto3(); break;
            case LevelType.Arduino:  CheckReto4(); break;
        }
    }

    void CompleteLevel(bool success)
    {
        _levelCompleted = true;
        OnLevelCompleted?.Invoke(_currentLevel, success);

        if (performance != null)
            Debug.Log($"[GameManager] {_currentLevel} completado — {performance.GetEvaluation()}");

        Invoke(nameof(NextLevel), 3f);
    }

    void CompleteGame()
    {
        OnGameCompleted?.Invoke();
        Debug.Log("[GameManager] ¡Juego completado!");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    public bool IsVoltageCorrect()
    {
        if (multimeter == null) return false;
        return Mathf.Abs(multimeter.measuredVoltage - RETO1_TARGET_VOLTAGE) <= RETO1_TOLERANCE;
    }
}