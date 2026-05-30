using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Necesario para congelar piezas en VR

/// <summary>
/// Controlador principal del juego (Modo Sandbox). 
/// Gestiona los 4 retos del Serious Game VR evaluando la electrónica mediante validación física.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias principales")]
    public CircuitSimulator  circuit; // Actualizado al nuevo motor matemático
    public Multimeter        multimeter;
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
    //  Estado
    // ─────────────────────────────────────────────
    [Header("Estado actual (solo lectura)")]
    [SerializeField] private LevelType _currentLevel   = LevelType.OhmLaw;
    [SerializeField] private int       _currentIndex   = 0;
    [SerializeField] private bool      _levelCompleted = false;
    [SerializeField] private bool      _repairPerformed = false;
    [SerializeField] private int       _wrongAttempts  = 0;
    [SerializeField] private float     _remainingTime  = 0f;
    [SerializeField] private bool      _timerActive    = false;

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
    public static event Action<int>             OnZoneActivated;
    public static event Action<LevelType, bool> OnZoneTransitionStart;

    public bool HasPerformedRepair() => _repairPerformed;
    public int  GetWrongAttempts()   => _wrongAttempts;
    public static void RaiseFaultDetected(string description) => OnFaultDetected?.Invoke(description);

    // Constantes informativas
    private const float RETO1_TARGET_VOLTAGE = 9f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        ValidateZones();
    }

    void Start()
    {
        if (FindAnyObjectByType<ExplorerOnboarding>() != null)
            ExplorerOnboarding.OnOnboardingComplete += OnOnboardingDone;
        else
            LoadLevel(0);
    }

    void OnOnboardingDone()
    {
        ExplorerOnboarding.OnOnboardingComplete -= OnOnboardingDone;
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
            CompleteLevel(false); 
        }
    }

    void OnDestroy()
    {
        ExplorerOnboarding.OnOnboardingComplete -= OnOnboardingDone;
    }

    // ─────────────────────────────────────────────
    //  API Pública y Validación Física (El Botón)
    // ─────────────────────────────────────────────
    public void RegisterRepairAction()
    {
        _repairPerformed = true;
        circuit?.MarkDirty();
        Debug.Log("[GameManager] Modificación en la matriz Sandbox registrada.");
    }

    public void RegisterWrongAttempt(string reason = "")
    {
        _wrongAttempts++;
        performance?.AddError(reason);
        Debug.Log($"[GameManager] Intento incorrecto #{_wrongAttempts}: {reason}");
    }

    public (bool pass, string motivo) EvaluacionManualBotonFisicoConResultado()
    {
        bool paso = EvaluacionManualBotonFisico();
        string msg = paso ? "Circuito correcto" : "Conexión inválida o valores fuera de rango";
        return (paso, msg);
    }

    /// <summary>
    /// Invoca la evaluación desde el botón físico. Valida matemáticamente la protoboard.
    /// </summary>
    public bool EvaluacionManualBotonFisico()
    {
        if (circuit == null) circuit = FindAnyObjectByType<CircuitSimulator>();
        if (circuit == null) return false;

        circuit.ForceSimulate();

        switch (_currentLevel)
        {
            case LevelType.OhmLaw:
                ComponentSlot slotResistor1 = circuit.todosLosSlots.Find(s => s.targetElectricalValue == 850f);
                if (slotResistor1 != null && slotResistor1.InstalledObject != null)
                {
                    if (slotResistor1.InstalledObject.TryGetComponent<Resistor>(out var res))
                    {
                        if (!res.hasFault && !res.isOverloaded)
                        {
                            CompleteLevel(true);
                            return true;
                        }
                    }
                }
                break;

            case LevelType.Parallel:
                if (circuit.AreAllLEDsOn())
                {
                    CompleteLevel(true);
                    return true;
                }
                break;

            case LevelType.Mixed:
                bool componentesEstables = true;
                int contadorComponentes = 0;

                foreach (var slot in circuit.todosLosSlots)
                {
                    if (slot != null && slot.InstalledObject != null)
                    {
                        contadorComponentes++;
                        if (slot.InstalledObject.TryGetComponent<Resistor>(out var r) && r.hasFault) componentesEstables = false;
                        if (slot.InstalledObject.TryGetComponent<LED>(out var led) && led.state == LEDState.Overload) componentesEstables = false;
                    }
                }

                if (componentesEstables && contadorComponentes >= 2)
                {
                    CompleteLevel(true);
                    return true;
                }
                break;

            case LevelType.Arduino:
                LED ledMonitoreo = FindAnyObjectByType<LED>();
                ArduinoCore arduinoCore = FindAnyObjectByType<ArduinoCore>();

                if (ledMonitoreo != null && arduinoCore != null)
                {
                    if (ledMonitoreo.isOn && ledMonitoreo.state == LEDState.Correct)
                    {
                        CompleteLevel(true);
                        return true;
                    }
                }
                break;
        }

        RegisterWrongAttempt("Error de circuito: Conexión inválida o valores fuera de rango.");
        return false;
    }

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

        float limit = (index < timeLimits.Length) ? timeLimits[index] : 0f;
        _remainingTime = limit;
        _timerActive   = limit > 0f;

        performance?.ResetTracker();
        multimeter?.ResetProbes();
        instructionSystem?.ResetInstructions();
        instructionSystem?.BuildInstructions();

        ActivateComponentsForLevel(_currentLevel);
        SetupLevel();

        circuit?.ForceSimulate();

        OnZoneActivated?.Invoke(_currentIndex);
        OnLevelLoaded?.Invoke(_currentLevel);
    }

    public void NextLevel() => LoadLevel(_currentIndex + 1);
    public void RestartCurrentLevel() => LoadLevel(_currentIndex);
    public void GoToLevel(int index)
    {
        if (!_debugMode) return;
        LoadLevel(Mathf.Clamp(index, 0, 3));
    }

    // ─────────────────────────────────────────────
    //  Gestión de Zonas y Configuración
    // ─────────────────────────────────────────────
    void ActivateComponentsForLevel(LevelType level)
    {
        if (reto1Zone != null) reto1Zone.SetActive(level == LevelType.OhmLaw);
        if (reto2Zone != null) reto2Zone.SetActive(level == LevelType.Parallel);
        if (reto3Zone != null) reto3Zone.SetActive(level == LevelType.Mixed);    
        if (reto4Zone != null) reto4Zone.SetActive(level == LevelType.Arduino);

        switch (level)
        {
            case LevelType.OhmLaw:   if (reto1Zone != null) circuit = reto1Zone.GetComponentInChildren<CircuitSimulator>(true); break;
            case LevelType.Parallel: if (reto2Zone != null) circuit = reto2Zone.GetComponentInChildren<CircuitSimulator>(true); break;
            case LevelType.Mixed:    if (reto3Zone != null) circuit = reto3Zone.GetComponentInChildren<CircuitSimulator>(true); break;
            case LevelType.Arduino:  if (reto4Zone != null) circuit = reto4Zone.GetComponentInChildren<CircuitSimulator>(true); break;
        }
    }

    void SetupLevel()
    {
        switch (_currentLevel)
        {
            case LevelType.OhmLaw:
                OnFaultDetected?.Invoke("Reto 1: Circuito con falla detectada.\nArma la red usando Ley de Ohm y valida con el botón físico.");
                break;
            case LevelType.Parallel:
                OnFaultDetected?.Invoke("Reto 2: Rama abierta.\nCompleta el circuito paralelo para energizar los LEDs.");
                break;
            case LevelType.Mixed:
                OnFaultDetected?.Invoke("Reto 3: Múltiples fallas.\nRevisa polaridades y códigos de colores.");
                break;
            case LevelType.Arduino:
                OnFaultDetected?.Invoke("Reto 4: Integración Microcontrolador.\nProtege el circuito y procesa la señal intermitente.");
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  Finalización de Retos
    // ─────────────────────────────────────────────
    void CompleteLevel(bool success)
    {
        if (_levelCompleted) return;
        _levelCompleted = true;

        if (success && circuit != null)
        {
            // Congelar componentes ganadores en VR
            foreach (var slot in circuit.todosLosSlots)
            {
                if (slot != null && slot.InstalledObject != null)
                {
                    if (slot.InstalledObject.TryGetComponent<XRGrabInteractable>(out var grab)) grab.enabled = false;
                    if (slot.InstalledObject.TryGetComponent<Collider>(out var col)) col.enabled = false;
                }
            }
        }

        OnLevelCompleted?.Invoke(_currentLevel, success);
        OnZoneTransitionStart?.Invoke(_currentLevel, success);
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

    public bool IsVoltageCorrect()
    {
        if (multimeter == null) return false;
        const float voltageTolerance = 0.5f;   
        return Mathf.Abs(multimeter.measuredVoltage - RETO1_TARGET_VOLTAGE) <= voltageTolerance;
    }

    void ValidateZones()
    {
        if (reto1Zone == null) Debug.LogWarning("[GameManager] reto1Zone no asignado.");
        if (reto2Zone == null) Debug.LogWarning("[GameManager] reto2Zone no asignado.");
        if (reto3Zone == null) Debug.LogWarning("[GameManager] reto3Zone no asignado.");
        if (reto4Zone == null) Debug.LogWarning("[GameManager] reto4Zone no asignado.");
    }
}