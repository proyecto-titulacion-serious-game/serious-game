using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controlador principal del juego. Gestiona los 4 retos del Serious Game VR.
/// Usa eventos para comunicarse con UI y otros sistemas (sin acoplamiento directo).
///
/// Niveles:
///  1) Ohm's Law: Circuito serie con resistencia defectuosa.
///  2) Parallel: Circuito paralelo con una rama abierta.
///  3) Mixed: Circuito mixto con LED y capacitor con polaridad invertida + resistencia errónea.
///  4) Arduino: Sensor conectado al pin incorrecto + buzzer sin resistencia + cable suelto.
/// 
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

    [Header("Zonas de Reto")]
    public GameObject reto1Zone;
    public GameObject reto2Zone;
    public GameObject reto3Zone;
    public GameObject reto4Zone;

    [Header("Transición entre retos")]
    [Tooltip("Segundos de pausa entre reto completado y carga del siguiente.")]
    public float zoneTransitionDelay = 3f;

    [Header("Debug")]
    [Tooltip("Permite usar GoToLevel() en builds de prueba.")]
    [SerializeField] private bool _debugMode = false;

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
    [SerializeField] private float     _remainingTime  = 0f;
    [SerializeField] private bool      _timerActive    = false;

    // ─────────────────────────────────────────────
    //  Propiedades públicas
    // ─────────────────────────────────────────────
    public LevelType currentLevel    => _currentLevel;
    public bool      levelCompleted  => _levelCompleted;
    public float     currentTimeLimit => _currentIndex < timeLimits.Length ? timeLimits[_currentIndex] : 600f;
    public float     remainingTime   => _remainingTime;
    public bool      timerActive     => _timerActive;

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    public static event Action<LevelType>       OnLevelLoaded;
    public static event Action<LevelType, bool> OnLevelCompleted;
    public static event Action<string>          OnFaultDetected;
    public static event Action                  OnGameCompleted;
    public static event Action<float>           OnTimerTick;
    public static event Action<LevelType>       OnTimerExpired;
    /// <summary>Zona activada: índice 0-3. Útil para UI y efectos de transición.</summary>
    public static event Action<int>             OnZoneActivated;
    /// <summary>Disparado justo antes de cargar el siguiente reto.</summary>
    public static event Action<LevelType, bool> OnZoneTransitionStart;

    /// <summary>
    /// Permite que sistemas externos (e.g. FaultManager) disparen OnFaultDetected.
    /// Los eventos estáticos solo pueden invocarse desde dentro de su clase.
    /// </summary>
    public static void RaiseFaultDetected(string description) =>
        OnFaultDetected?.Invoke(description);

    // ─────────────────────────────────────────────
    //  Constantes de configuración de retos
    // ─────────────────────────────────────────────
    private const float RETO1_FAULTY_RESISTANCE  = 10f;    // 10Ω → 150mA → overload (rojo)
    private const float RETO1_CORRECT_RESISTANCE = 850f;   // 850Ω → 10mA → correcto (verde); consistente con DiagnosticSystem
    private const float RETO1_LED_RESISTANCE     = 50f;
    private const float RETO1_SOURCE_VOLTAGE     = 9f;
    private const float RETO1_TARGET_VOLTAGE     = 9f;
    // Tolerancia: usa Resistor.tolerancePercent (5%) — ver CheckReto1()

    private const float RETO2_BROKEN_RESISTANCE  = 9999f;
    private const float RETO2_NORMAL_RESISTANCE  = 50f;

    private const float RETO3_FAULTY_RESISTANCE  = 470f;
    private const float RETO3_CORRECT_RESISTANCE = 220f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        ValidateZones();
    }

    void Start()
    {
        CircuitManager.OnCircuitChanged += CheckWinCondition;
        LoadLevel(0);
    }

    void Update()
    {
        if (!_timerActive || _levelCompleted) return;

        _remainingTime -= Time.deltaTime;
        OnTimerTick?.Invoke(_remainingTime);

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _timerActive   = false;
            OnTimerExpired?.Invoke(_currentLevel);
            CompleteLevel(false);  // false = fracasó por tiempo
        }
    }

    void OnDestroy()
    {
        CircuitManager.OnCircuitChanged -= CheckWinCondition;
        // No nulamos los eventos estáticos — otros objetos siguen suscritos
        // y necesitan seguir recibiendo eventos si GameManager se recrea.
    }

    // ─────────────────────────────────────────────
    //  API Pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Registra que se realizó una reparación.
    /// Limpia la lista de componentes para que el rescan incluya el reemplazo instalado
    /// y excluya el componente defectuoso que el Explorador sacó de la zona.
    /// </summary>
    public void RegisterRepairAction()
    {
        _repairPerformed = true;
        if (circuit != null)
        {
            circuit.components.Clear();   // descarta caché que aún apunta al componente viejo
            circuit.AutoDetectComponents(); // rescana: encuentra el reemplazo (hijo de la zona)
            circuit.MarkDirty();            // provoca resimulación → dispara OnCircuitChanged → CheckReto
        }
        Debug.Log("[GameManager] Reparación registrada — componentes reescaneados.");
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

        // Iniciar temporizador del reto
        float limit = (index < timeLimits.Length) ? timeLimits[index] : 0f;
        _remainingTime = limit;
        _timerActive   = limit > 0f;

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
        OnZoneActivated?.Invoke(_currentIndex);
        OnLevelLoaded?.Invoke(_currentLevel);
        Debug.Log($"[GameManager] Cargando: {_currentLevel}");
    }

    public void NextLevel() => LoadLevel(_currentIndex + 1);

    /// <summary>Reinicia el reto actual desde cero (conserva el índice).</summary>
    public void RestartCurrentLevel() => LoadLevel(_currentIndex);

    /// <summary>Salta directamente a un reto. Solo funciona con _debugMode activo.</summary>
    public void GoToLevel(int index)
    {
        if (!_debugMode)
        {
            Debug.LogWarning("[GameManager] GoToLevel requiere _debugMode = true en el Inspector.");
            return;
        }
        LoadLevel(Mathf.Clamp(index, 0, 3));
    }

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
        // Activar/desactivar ZONAS físicas
       if (reto1Zone != null) reto1Zone.SetActive(level == LevelType.OhmLaw);
        if (reto2Zone != null) reto2Zone.SetActive(level == LevelType.Parallel);
        if (reto3Zone != null) reto3Zone.SetActive(level == LevelType.Mixed);    
        if (reto4Zone != null) reto4Zone.SetActive(level == LevelType.Arduino);

        // Actualizar la referencia circuit al CircuitManager de la zona activa.
        // Se usa includeInactive:true porque la zona puede estar inactiva en jerarquía
        // cuando esta función corre (padre GameZones desactivado al inicio).
        switch (level)
        {
            case LevelType.OhmLaw:
                if (reto1Zone != null)
                    circuit = reto1Zone.GetComponentInChildren<CircuitManager>(true);
                break;
            case LevelType.Parallel:
                if (reto2Zone != null)
                    circuit = reto2Zone.GetComponentInChildren<CircuitManager>(true);
                break;
            case LevelType.Mixed:
                if (reto3Zone != null)
                    circuit = reto3Zone.GetComponentInChildren<CircuitManager>(true);
                break;
            case LevelType.Arduino:
                if (reto4Zone != null)
                    circuit = reto4Zone.GetComponentInChildren<CircuitManager>(true);
                break;
        }

        if (circuit == null)
        {
            Debug.LogError($"[GameManager] No se encontró CircuitManager en la zona activa ({level}). " +
                           "Asegúrate de que cada zona tenga un CircuitManager hijo.");
            return;
        }

        Debug.Log($"[GameManager] circuit reasignado → '{circuit.name}' (path={GetTransformPath(circuit.transform)}) en {level}");

        // Activar/desactivar COMPONENTES del circuito
        var allComponents = circuit.GetComponentsInChildren<ElectricalComponent>(true);

        switch (level)
        {
            case LevelType.OhmLaw:
                foreach (var comp in allComponents)
                {
                    if (comp is VoltageSource || comp is Resistor || comp is LED)
                        comp.gameObject.SetActive(true);
                    else
                        comp.gameObject.SetActive(false);
                }
                break;

            case LevelType.Parallel:
                foreach (var comp in allComponents)
                {
                    if (comp is VoltageSource || comp is Resistor || comp is LED)
                        comp.gameObject.SetActive(true);
                    else
                        comp.gameObject.SetActive(false);
                }
                break;

            case LevelType.Mixed:
                foreach (var comp in allComponents)
                {
                    if (comp is ArduinoPin)
                        comp.gameObject.SetActive(false);
                    else
                        comp.gameObject.SetActive(true);
                }
                break;

            case LevelType.Arduino:
                foreach (var comp in allComponents)
                    comp.gameObject.SetActive(true);
                break;
        }

        circuit.components.Clear();
        
        Debug.Log($"[GameManager] Zona activa: {level}, Circuit: {circuit.gameObject.name}");
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

        if (circuit.FindCircuitComponent<Resistor>() == null)
            Debug.LogWarning("[GameManager] SetupReto1: no hay Resistor en circuit.components — la falla no se aplicó. Revisa la escena.");

        OnFaultDetected?.Invoke(
            "Reto 1: La resistencia tiene valor incorrecto.\n" +
            "El Técnico debe calcular el valor correcto usando Ley de Ohm.");
    }

    void CheckReto1()
    {
        if (!_repairPerformed || circuit == null) return;

        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                // Usar la misma tolerancia que Resistor.IsValueCorrect (5% del valor correcto)
                float margin = RETO1_CORRECT_RESISTANCE * (r.tolerancePercent / 100f);
                bool  valorCorrecto = Mathf.Abs(r.resistance - RETO1_CORRECT_RESISTANCE) <= margin;

                if (!r.hasFault && valorCorrecto)
                {
                    CompleteLevel(true);
                    return;
                }
                else if (!r.hasFault && !valorCorrecto)
                {
                    RegisterWrongAttempt($"Resistencia incorrecta: {r.resistance:F0}Ω (correcto: {RETO1_CORRECT_RESISTANCE}Ω)");
                    OnFaultDetected?.Invoke(
                        $"Valor incorrecto: {r.resistance:F0} Ω\n" +
                        $"Recalcula usando V = I × R\n" +
                        $"R = (Vfuente - VLED) / I = ?");
                }
                else if (r.hasFault)
                {
                    // El Explorador instaló la resistencia pero con valor incorrecto.
                    // ComponentDeliverySystem ya registró el intento — solo damos feedback visual.
                    OnFaultDetected?.Invoke(
                        $"Valor incorrecto instalado: {r.resistance:F0} Ω\n" +
                        $"Correcto: {RETO1_CORRECT_RESISTANCE:F0} Ω\n" +
                        $"Recalcula y vuelve a enviar.");
                }
                return; // Solo hay un resistor en Reto 1
            }
        }

        Debug.LogWarning("[GameManager] CheckReto1: no hay Resistor en circuit.components — condición de victoria inaccesible. Revisa la escena.");
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
        if (!_repairPerformed || circuit == null) return;

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
                cap.polarityInverted = true;
            }
            if (comp is Resistor r)
            {
                r.faultyResistance  = RETO3_FAULTY_RESISTANCE;
                r.correctResistance = RETO3_CORRECT_RESISTANCE;
                r.ApplyFault();
            }
        }

        OnFaultDetected?.Invoke(
            "Reto 3: 3 fallas simultáneas.\n" +
            "1) LED con polaridad invertida\n" +
            "2) Capacitor con polaridad invertida\n" +
            "3) Resistencia con código de colores erróneo");
    }

    void CheckReto3()
    {
        if (!_repairPerformed || circuit == null) return;

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
                pin.hasLooseCable = true;   // ← NUEVO: activar explícitamente
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
        if (!_repairPerformed || circuit == null) return;

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
        if (_levelCompleted) return;
        _levelCompleted = true;
        OnLevelCompleted?.Invoke(_currentLevel, success);
        OnZoneTransitionStart?.Invoke(_currentLevel, success);

        if (performance != null)
            Debug.Log($"[GameManager] {_currentLevel} completado — {performance.GetEvaluation()}");

        StartCoroutine(TransitionToNextLevel());
    }

    IEnumerator TransitionToNextLevel()
    {
        yield return new WaitForSeconds(zoneTransitionDelay);
        NextLevel();
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
        const float voltageTolerance = 0.5f;   // ±0.5 V aceptable en la medición
        return Mathf.Abs(multimeter.measuredVoltage - RETO1_TARGET_VOLTAGE) <= voltageTolerance;
    }

    void ValidateZones()
    {
        if (reto1Zone == null) Debug.LogWarning("[GameManager] reto1Zone no asignado en el Inspector.");
        if (reto2Zone == null) Debug.LogWarning("[GameManager] reto2Zone no asignado en el Inspector.");
        if (reto3Zone == null) Debug.LogWarning("[GameManager] reto3Zone no asignado en el Inspector.");
        if (reto4Zone == null) Debug.LogWarning("[GameManager] reto4Zone no asignado en el Inspector.");
    }

    static string GetTransformPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}