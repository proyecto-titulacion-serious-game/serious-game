using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Controlador principal del juego (Modo Sandbox).
/// Gestiona los 4 retos del Serious Game VR evaluando la electrónica mediante validación física.
///
/// Retos 1-3: motor CircuitSimulator (ComponentSlot-based).
/// Reto 4:    motor ProtoboardSimulator (ProtoboardSlot-based, Arduino + Protoboard).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias principales")]
    public CircuitSimulator    circuit;           // Motor Retos 1-3
    public ProtoboardSimulator protoSim;          // Motor Reto 4 (Arduino + Protoboard)
    public Multimeter          multimeter;
    public PerformanceTracker  performance;
    public InstructionSystem   instructionSystem;

    [Header("Zonas de Reto")]
    public GameObject reto1Zone;
    public GameObject reto2Zone;
    public GameObject reto3Zone;
    public GameObject reto4Zone;
    [Tooltip("GO PC_Arduino (raíz de escena). Se activa solo durante Reto 4.")]
    public GameObject pcArduino;

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
    [SerializeField] private LevelType _currentLevel    = LevelType.OhmLaw;
    [SerializeField] private int       _currentIndex    = 0;
    [SerializeField] private bool      _levelCompleted  = false;
    [SerializeField] private bool      _repairPerformed = false;
    [SerializeField] private int       _wrongAttempts   = 0;
    [SerializeField] private float     _remainingTime   = 0f;
    [SerializeField] private bool      _timerActive     = false;

    public LevelType currentLevel     => _currentLevel;
    public bool      levelCompleted   => _levelCompleted;
    public float     currentTimeLimit => _currentIndex < timeLimits.Length ? timeLimits[_currentIndex] : 600f;
    public float     remainingTime    => _remainingTime;
    public bool      timerActive      => _timerActive;

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

    public bool HasPerformedRepair()  => _repairPerformed;
    public int  GetWrongAttempts()    => _wrongAttempts;
    public static void RaiseFaultDetected(string description) => OnFaultDetected?.Invoke(description);

    private const float RETO1_TARGET_VOLTAGE = 9f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        ValidateZones();
    }

    // Resultado de la última validación sandbox — actualizado por OnSandboxValidated
    private SandboxValidationResult _lastSandboxResult;

    void OnSandboxResult(SandboxValidationResult result) => _lastSandboxResult = result;

    void Start()
    {
        // Suscribir eventos de red (GameSession)
        GameSession.OnRetoChanged          += OnNetworkRetoChanged;
        GameSession.OnCableFixed           += OnNetworkCableFixed;
        GameSession.OnValidacionSolicitada += OnNetworkValidacionSolicitada;

        // Suscribir validador dinámico del Reto 4 sandbox
        ProtoboardSimulator.OnSandboxValidated += OnSandboxResult;

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

    // ── Callbacks de red ─────────────────────────────────────────────────

    /// <summary>El Técnico (Host) avanzó de reto — sincroniza el Explorador.</summary>
    void OnNetworkRetoChanged(int retoIndex)
    {
        // Solo actuar si el índice difiere del estado local (evitar bucle Host→RPC→Host)
        if (retoIndex != _currentIndex)
            LoadLevel(retoIndex);
    }

    /// <summary>
    /// Evento de red legacy (paradigma lineal). En el sandbox del Reto 4 ya no hay
    /// cable suelto predefinido — se deja como no-op para no romper compatibilidad de red.
    /// </summary>
    void OnNetworkCableFixed()
    {
        protoSim?.MarkDirty();
        Debug.Log("[GameManager] OnNetworkCableFixed recibido (sandbox: no-op, solo marca dirty).");
    }

    /// <summary>El Explorador solicitó validación desde el botón físico en red.</summary>
    void OnNetworkValidacionSolicitada()
    {
        bool paso = EvaluacionManualBotonFisico();
        int  cod  = paso ? 0 : _wrongAttempts;
        GameSession.Instance?.ReportarResultado(paso, cod);
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
        GameSession.OnRetoChanged              -= OnNetworkRetoChanged;
        GameSession.OnCableFixed               -= OnNetworkCableFixed;
        GameSession.OnValidacionSolicitada     -= OnNetworkValidacionSolicitada;
        ProtoboardSimulator.OnSandboxValidated -= OnSandboxResult;
    }

    // ─────────────────────────────────────────────
    //  API Pública
    // ─────────────────────────────────────────────
    public void RegisterRepairAction()
    {
        _repairPerformed = true;
        circuit?.MarkDirty();
        protoSim?.MarkDirty();   // Reto 4: sucia ProtoboardSimulator también
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
        string msg = paso ? "Circuito correcto" : "Conexion invalida o valores fuera de rango";
        return (paso, msg);
    }

    /// <summary>
    /// Evalúa el circuito desde el botón físico.
    /// Retos 1-3: usa CircuitSimulator. Reto 4: usa ProtoboardSimulator + estados de ArduinoPin/Resistor.
    /// </summary>
    public bool EvaluacionManualBotonFisico()
    {
        switch (_currentLevel)
        {
            case LevelType.OhmLaw:
            case LevelType.Parallel:
            case LevelType.Mixed:
                return EvaluarCircuitSimulator();

            case LevelType.Arduino:
                return EvaluarReto4();
        }
        return false;
    }

    // ─────────────────────────────────────────────
    //  Evaluación Retos 1-3 (CircuitSimulator)
    // ─────────────────────────────────────────────
    bool EvaluarCircuitSimulator()
    {
        if (circuit == null) circuit = FindAnyObjectByType<CircuitSimulator>();
        if (circuit == null) return false;

        circuit.ForceSimulate();

        switch (_currentLevel)
        {
            case LevelType.OhmLaw:
            {
                var slotR = circuit.todosLosSlots.Find(s => s.targetElectricalValue == 850f);
                if (slotR != null && slotR.InstalledObject != null)
                {
                    if (slotR.InstalledObject.TryGetComponent<Resistor>(out var res) && !res.hasFault)
                    {
                        // Victoria: resistor correcto instalado Y al menos un LED encendido
                        bool ledOn = false;
                        foreach (var s in circuit.todosLosSlots)
                        {
                            if (s?.InstalledObject == null) continue;
                            if (s.InstalledObject.TryGetComponent<LED>(out var l) && l.isOn)
                            { ledOn = true; break; }
                        }
                        if (ledOn) { CompleteLevel(true); return true; }
                    }
                }
                break;
            }
            case LevelType.Parallel:
                if (circuit.AreAllLEDsOn()) { CompleteLevel(true); return true; }
                break;

            case LevelType.Mixed:
            {
                bool ok  = true;
                int  cnt = 0;
                foreach (var slot in circuit.todosLosSlots)
                {
                    if (slot == null || slot.InstalledObject == null) continue;
                    cnt++;
                    if (slot.InstalledObject.TryGetComponent<Resistor>(out var r)   && r.hasFault)              ok = false;
                    if (slot.InstalledObject.TryGetComponent<LED>(out var led)       && led.state == LEDState.Overload) ok = false;
                }
                if (ok && cnt >= 2) { CompleteLevel(true); return true; }
                break;
            }
        }

        RegisterWrongAttempt("Error de circuito: conexion invalida o valores fuera de rango.");
        return false;
    }

    // ─────────────────────────────────────────────
    //  Evaluación Reto 4 (Arduino + Protoboard)
    // ─────────────────────────────────────────────
    bool EvaluarReto4()
    {
        // El validador dinámico (DFS) corre automáticamente en ProtoboardSimulator.
        // _lastSandboxResult se actualiza via OnSandboxResult cada vez que el grafo cambia.
        if (_lastSandboxResult.success)
        {
            CompleteLevel(true);
            return true;
        }

        // Forzar recálculo por si el jugador pulsó el botón antes del primer tick
        if (protoSim == null) protoSim = FindProtoSim();
        protoSim?.MarkDirty();

        string motivo = string.IsNullOrEmpty(_lastSandboxResult.message)
            ? "Circuito incompleto. Revisa que el LED, la resistencia y el pin esten conectados."
            : _lastSandboxResult.message;

        RegisterWrongAttempt("Reto 4 — " + motivo);
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

        // Retos 1-3: forzar simulación inicial. Reto 4: marcar protoboard sucia.
        if (_currentLevel != LevelType.Arduino)
        {
            circuit?.ForceSimulate();
        }
        else
        {
            protoSim?.MarkDirty();

            // En modo offline (sin Fusion) el ArduinoNetworkBridge nunca recibe Spawned().
            // Simulamos el spawn para que TechnicianTelemetryUI y ArduinoIDEUI se conecten.
            bool offline = ConnectionManager.Instance == null || ConnectionManager.Instance.modoOffline;
            if (offline)
            {
                var bridge = FindAnyObjectByType<ArduinoNetworkBridge>();
                bridge?.SimularSpawnOffline();
            }
        }

        OnZoneActivated?.Invoke(_currentIndex);
        OnLevelLoaded?.Invoke(_currentLevel);

        // Sincronizar cambio de reto a todos los clientes (solo el Host tiene StateAuthority)
        GameSession.Instance?.AvanzarReto(_currentIndex);
    }

    public void NextLevel()         => LoadLevel(_currentIndex + 1);
    public void RestartCurrentLevel() => LoadLevel(_currentIndex);
    public void GoToLevel(int index)
    {
        if (!_debugMode) return;
        LoadLevel(Mathf.Clamp(index, 0, 3));
    }

    // ─────────────────────────────────────────────
    //  Gestión de Zonas
    // ─────────────────────────────────────────────
    void ActivateComponentsForLevel(LevelType level)
    {
        if (reto1Zone != null) reto1Zone.SetActive(level == LevelType.OhmLaw);
        if (reto2Zone != null) reto2Zone.SetActive(level == LevelType.Parallel);
        if (reto3Zone != null) reto3Zone.SetActive(level == LevelType.Mixed);
        if (reto4Zone != null) reto4Zone.SetActive(level == LevelType.Arduino);
        if (pcArduino  != null) pcArduino.SetActive(level == LevelType.Arduino);

        switch (level)
        {
            case LevelType.OhmLaw:
                circuit  = reto1Zone != null ? reto1Zone.GetComponentInChildren<CircuitSimulator>(true) : null;
                protoSim = null;
                break;
            case LevelType.Parallel:
                circuit  = reto2Zone != null ? reto2Zone.GetComponentInChildren<CircuitSimulator>(true) : null;
                protoSim = null;
                break;
            case LevelType.Mixed:
                circuit  = reto3Zone != null ? reto3Zone.GetComponentInChildren<CircuitSimulator>(true) : null;
                protoSim = null;
                break;
            case LevelType.Arduino:
                circuit  = null;           // Reto 4 usa ProtoboardSimulator, no CircuitSimulator
                protoSim = FindProtoSim();
                break;
        }
    }

    /// <summary>Busca el ProtoboardSimulator primero dentro de reto4Zone, luego en toda la escena.</summary>
    ProtoboardSimulator FindProtoSim()
    {
        if (reto4Zone != null)
        {
            var s = reto4Zone.GetComponentInChildren<ProtoboardSimulator>(true);
            if (s != null) return s;
        }
        return FindAnyObjectByType<ProtoboardSimulator>();
    }

    void SetupLevel()
    {
        switch (_currentLevel)
        {
            case LevelType.OhmLaw:
                OnFaultDetected?.Invoke("Reto 1: Circuito con falla.\nArma la red usando Ley de Ohm y valida con el boton fisico.");
                break;
            case LevelType.Parallel:
                OnFaultDetected?.Invoke("Reto 2: Rama abierta.\nCompleta el circuito paralelo para energizar los LEDs.");
                break;
            case LevelType.Mixed:
                OnFaultDetected?.Invoke("Reto 3: Multiples fallas.\nRevisa polaridades y codigos de colores.");
                break;
            case LevelType.Arduino:
                OnFaultDetected?.Invoke(
                    "Reto 4: Sandbox Arduino + Protoboard.\n" +
                    "  TECNICO: Escribe el sketch en el IDE y elige cualquier pin digital (D2-D13).\n" +
                    "  EXPLORADOR: Conecta LED + resistencia desde ese pin hasta GND en la protoboard.\n" +
                    "Objetivo: Haz que un LED parpadee de forma segura. Valida con el boton fisico.");
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

        if (success)
        {
            // Retos 1-3: congelar ComponentSlots instalados
            if (circuit != null)
            {
                foreach (var slot in circuit.todosLosSlots)
                {
                    if (slot == null || slot.InstalledObject == null) continue;
                    if (slot.InstalledObject.TryGetComponent<XRGrabInteractable>(out var grab)) grab.enabled = false;
                    if (slot.InstalledObject.TryGetComponent<Collider>(out var col))            col.enabled  = false;
                }
            }

            // Reto 4: congelar todos los XRGrabInteractable dentro de reto4Zone
            if (_currentLevel == LevelType.Arduino && reto4Zone != null)
            {
                foreach (var grab in reto4Zone.GetComponentsInChildren<XRGrabInteractable>(true))
                    grab.enabled = false;
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
        Debug.Log("[GameManager] Juego completado.");
    }

    // ─────────────────────────────────────────────
    //  Utilidades
    // ─────────────────────────────────────────────
    public bool IsVoltageCorrect()
    {
        if (multimeter == null) return false;
        const float tol = 0.5f;
        return Mathf.Abs(multimeter.measuredVoltage - RETO1_TARGET_VOLTAGE) <= tol;
    }

    void ValidateZones()
    {
        if (reto1Zone == null) Debug.LogWarning("[GameManager] reto1Zone no asignado.");
        if (reto2Zone == null) Debug.LogWarning("[GameManager] reto2Zone no asignado.");
        if (reto3Zone == null) Debug.LogWarning("[GameManager] reto3Zone no asignado.");
        if (reto4Zone == null) Debug.LogWarning("[GameManager] reto4Zone no asignado.");
        if (pcArduino  == null) Debug.LogWarning("[GameManager] pcArduino no asignado — PC_Arduino no se mostrará en Reto 4.");
    }
}
