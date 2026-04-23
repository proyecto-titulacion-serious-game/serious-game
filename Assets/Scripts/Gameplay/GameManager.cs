using System;
using UnityEngine;

/// <summary>
/// Controlador principal del juego. Gestiona los 4 retos del Serious Game VR.
/// Usa eventos para comunicarse con UI y otros sistemas (sin acoplamiento directo).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias principales")]
    public CircuitManager  circuit;
    public Multimeter      multimeter;
    public PerformanceTracker performance;
    public InstructionSystem  instructionSystem;

    [Header("Configuración de niveles")]
    [Tooltip("Tiempo límite en segundos para cada reto (0 = sin límite).")]
    public float[] timeLimits = { 480f, 600f, 720f, 900f };  // 8, 10, 12, 15 min

    [Header("Sistema de Niveles (Prefabs)")]
    [Tooltip("Arrastra aquí los 4 prefabs de tus circuitos (Reto 1, 2, 3 y 4).")]
    public GameObject[] circuitPrefabs;
    
    [Tooltip("El objeto vacío en tu Panel Vertical donde aparecerán los circuitos.")]
    public Transform circuitSpawnPoint;

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
    public static event Action<LevelType>      OnLevelLoaded;
    public static event Action<LevelType, bool> OnLevelCompleted;   // nivel, éxito
    public static event Action<string>         OnFaultDetected;
    public static event Action                 OnGameCompleted;

    // ─────────────────────────────────────────────
    //  Constantes de configuración de retos
    // ─────────────────────────────────────────────
    private const float RETO1_FAULTY_RESISTANCE  = 10f;
    private const float RETO1_CORRECT_RESISTANCE = 100f;
    private const float RETO1_LED_RESISTANCE     = 50f;
    private const float RETO1_SOURCE_VOLTAGE     = 9f;
    private const float RETO1_TARGET_VOLTAGE     = 9f;
    private const float RETO1_TOLERANCE          = 0.5f;

    private const float RETO2_BROKEN_RESISTANCE  = 9999f; // Rama abierta
    private const float RETO2_NORMAL_RESISTANCE  = 50f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        // Suscribirse al evento de circuito para verificar condiciones de éxito
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

    public void RegisterRepairAction()
    {
        _repairPerformed = true;
        circuit?.MarkDirty();   // Resimular tras la reparación
        Debug.Log("[GameManager] Reparación registrada.");
    }

    public void RegisterWrongAttempt(string reason = "")
    {
        _wrongAttempts++;
        performance?.AddError(reason);
        Debug.Log($"[GameManager] Intento incorrecto #{_wrongAttempts}: {reason}");
    }

    public bool HasPerformedRepair()   => _repairPerformed;
    public int  GetWrongAttempts()     => _wrongAttempts;

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

        performance?.ResetTracker();
        multimeter?.ResetProbes();
        instructionSystem?.ResetInstructions();
        instructionSystem?.BuildInstructions();

        SpawnCircuitForLevel(index); // <-- Llama al cambio de prefab antes de configurarlo
        SetupLevel();
        circuit?.ForceSimulate();

        SetupLevel();
        circuit?.ForceSimulate();

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

    void SpawnCircuitForLevel(int index)
    {
        // 1. Destruimos el circuito del nivel anterior (si existe)
        if (circuit != null)
        {
            Destroy(circuit.gameObject);
        }

        // 2. Revisamos si tenemos un prefab para este nivel
        if (index < circuitPrefabs.Length && circuitPrefabs[index] != null)
        {
            // 3. Instanciamos el "cartucho" en la posición y rotación exactas del spawn point
            GameObject newCircuitObj = Instantiate(circuitPrefabs[index], circuitSpawnPoint.position, circuitSpawnPoint.rotation);
            
            // 4. Lo hacemos "hijo" del spawn point para que todo quede ordenado en el Panel
            newCircuitObj.transform.SetParent(circuitSpawnPoint);

            // 5. Conectamos el nuevo CircuitManager al GameManager
            circuit = newCircuitObj.GetComponent<CircuitManager>();
        }
        else
        {
            Debug.LogWarning($"[GameManager] Falta asignar el Prefab para el nivel {index} en el Inspector.");
        }
    }

    // ─────────────────────────────────────────────
    //  RETO 1 — Circuito Serie & Ley de Ohm
    // ─────────────────────────────────────────────

    void SetupReto1()
    {
        circuit.topology = CircuitTopology.Series;

        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                r.faultyResistance  = RETO1_FAULTY_RESISTANCE;
                r.correctResistance = RETO1_CORRECT_RESISTANCE;
                r.ApplyFault();    // Resistencia inicial INCORRECTA
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

        OnFaultDetected?.Invoke("Reto 1: La resistencia tiene valor incorrecto.\n" +
                                 "El Técnico debe calcular el valor correcto usando Ley de Ohm.");
    }

    void CheckReto1()
    {
        if (!_repairPerformed) return;
        if (multimeter?.probeA == null || multimeter?.probeB == null) return;

        float measured = multimeter.measuredVoltage;

        if (Mathf.Abs(measured - RETO1_TARGET_VOLTAGE) <= RETO1_TOLERANCE)
            CompleteLevel(true);
    }

    // ─────────────────────────────────────────────
    //  RETO 2 — Circuito Paralelo & Divisor de Corriente
    // ─────────────────────────────────────────────

    void SetupReto2()
    {
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
                r.Repair();  // Resistencias correctas en Reto 2
            }
        }

        OnFaultDetected?.Invoke("Reto 2: Una rama del circuito paralelo está abierta.\n" +
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
        circuit.topology = CircuitTopology.Mixed;

        int ledIndex = 0;
        foreach (var comp in circuit.components)
        {
            // Falla 1: LED con polaridad invertida
            if (comp is LED led)
            {
                led.polarityInverted = (ledIndex == 0);   // Solo el primer LED
                ledIndex++;
            }

            // Falla 2: Capacitor con polaridad invertida
            if (comp is Capacitor cap)
            {
                cap.SetPolarityInverted(true);
            }

            // Falla 3: Resistencia con valor incorrecto (código de colores erróneo)
            if (comp is Resistor r)
            {
                r.faultyResistance  = 470f;   // Color de bandas equivocado
                r.correctResistance = 220f;   // El que pide el circuito
                r.ApplyFault();
            }
        }

        // En mixto, el GameManager asigna voltajes a los nodos manualmente
        SetupMixedNodeVoltages();

        OnFaultDetected?.Invoke("Reto 3: 3 fallas simultáneas.\n" +
                                 "1) LED con polaridad invertida\n" +
                                 "2) Capacitor con polaridad invertida\n" +
                                 "3) Resistencia con código de colores erróneo");
    }

    void SetupMixedNodeVoltages()
    {
        // Topología Reto 3: Fuente → Resistencia en serie → dos ramas paralelas (LED, Capacitor)
        VoltageSource source = null;
        Resistor      resistor = null;

        foreach (var comp in circuit.components)
        {
            if (comp is VoltageSource vs) source = vs;
            if (comp is Resistor r)       resistor = r;
        }

        if (source == null || resistor == null) return;

        float V = source.voltage;
        float I = V / (resistor.GetResistance() + 50f);   // 50Ω estimado de ramas
        float vAfterR = V - I * resistor.GetResistance();

        if (resistor.nodeA != null) resistor.nodeA.voltage = V;
        if (resistor.nodeB != null) resistor.nodeB.voltage = vAfterR;

        // Las ramas paralelas comparten el mismo nodo después de la resistencia
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
        circuit.topology = CircuitTopology.Mixed;

        // Fallas del Reto 4
        foreach (var comp in circuit.components)
        {
            if (comp is ArduinoPin pin)
            {
                pin.ApplyFault();     // Pin incorrecto
            }
            if (comp is Resistor r)
            {
                r.faultyResistance  = 0f;    // Sin resistencia limitadora al buzzer
                r.correctResistance = 330f;  // 330Ω para proteger el buzzer
                r.ApplyFault();
            }
        }

        // El cable suelto en la protoboard lo maneja ArduinoPin
        OnFaultDetected?.Invoke("Reto 4: Sistema sensor-temperatura no activa alarma.\n" +
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
                if (pin.hasFault)           allPinsCorrect = false;
                if (pin.hasLooseCable)      cableFixed     = false;
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