using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona los objetivos específicos de cada reto y el puntaje final.
/// Se suscribe a eventos de GameManager para actualizar el estado automáticamente.
/// </summary>
public class ObjectiveSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public GameManager      gameManager;
    public PerformanceTracker performance;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    [Header("Objetivos activos (solo lectura)")]
    [SerializeField] private List<Objective> _objectives = new List<Objective>();
    [SerializeField] private int   _totalScore    = 0;
    [SerializeField] private int   _maxScore      = 0;

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    public static event Action<Objective>     OnObjectiveCompleted;
    public static event Action<int, int>      OnScoreUpdated;        // score, maxScore
    public static event Action<SessionResult> OnSessionEnded;

    // ─────────────────────────────────────────────
    //  Flags de interacción
    // ─────────────────────────────────────────────
    // Evitan que objetivos que requieren acción del jugador se completen solos
    private bool _multimeterUsed = false;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        GameManager.OnLevelLoaded       += BuildObjectivesForLevel;
        GameManager.OnLevelCompleted    += HandleLevelCompleted;
        GameManager.OnGameCompleted     += HandleGameCompleted;
        CircuitManager.OnCircuitChanged += AutoCompleteByCircuitState;
        Multimeter.OnReadingTaken       += OnMultimeterUsed;
    }

    void OnDestroy()
    {
        GameManager.OnLevelLoaded       -= BuildObjectivesForLevel;
        GameManager.OnLevelCompleted    -= HandleLevelCompleted;
        GameManager.OnGameCompleted     -= HandleGameCompleted;
        CircuitManager.OnCircuitChanged -= AutoCompleteByCircuitState;
        Multimeter.OnReadingTaken       -= OnMultimeterUsed;
    }

    void OnMultimeterUsed() => _multimeterUsed = true;

    // ─────────────────────────────────────────────
    //  Construcción de objetivos por reto
    // ─────────────────────────────────────────────

    void BuildObjectivesForLevel(LevelType level)
    {
        _objectives.Clear();
        _totalScore      = 0;
        _maxScore        = 0;
        _multimeterUsed  = false;

        switch (level)
        {
            case LevelType.OhmLaw:
                AddObjective("Conectar el multímetro a dos nodos",            100);
                AddObjective("Leer e interpretar el voltaje medido",           150);
                AddObjective("Seleccionar la resistencia defectuosa",          150);
                AddObjective("Reemplazar la resistencia con el valor correcto", 200);
                break;

            case LevelType.Parallel:
                AddObjective("Identificar qué ramas del paralelo están activas", 150);
                AddObjective("Localizar la rama abierta con el multímetro",       200);
                AddObjective("Reconectar el cable roto en la rama correcta",      250);
                break;

            case LevelType.Mixed:
                AddObjective("Identificar el LED con polaridad invertida",         200);
                AddObjective("Identificar el capacitor con polaridad invertida",   200);
                AddObjective("Leer código de colores y corregir la resistencia",   250);
                AddObjective("Corregir las 3 fallas en orden de prioridad",        150);
                break;

            case LevelType.Arduino:
                AddObjective("Leer el pinout del Arduino en el manual técnico",     150);
                AddObjective("Identificar el pin incorrecto del sensor",            200);
                AddObjective("Calcular la resistencia necesaria para el buzzer",    200);
                AddObjective("Reconectar el cable suelto en la protoboard",         150);
                AddObjective("Verificar la señal del sensor en el monitor serial",  100);
                break;
        }

        OnScoreUpdated?.Invoke(_totalScore, _maxScore);
    }

    void AddObjective(string description, int points)
    {
        _objectives.Add(new Objective
        {
            description = description,
            maxPoints   = points,
            isCompleted = false
        });
        _maxScore += points;
    }

    // ─────────────────────────────────────────────
    //  API Pública — llamar desde otros sistemas
    // ─────────────────────────────────────────────

    /// <summary>Marca el objetivo en el índice como completado y suma puntos.</summary>
    public void CompleteObjective(int index, float timeBonus = 1f)
    {
        if (index < 0 || index >= _objectives.Count) return;

        var obj = _objectives[index];
        if (obj.isCompleted) return;

        obj.isCompleted   = true;
        obj.pointsEarned  = Mathf.RoundToInt(obj.maxPoints * Mathf.Clamp01(timeBonus));
        _totalScore      += obj.pointsEarned;
        _objectives[index] = obj;

        OnObjectiveCompleted?.Invoke(obj);
        OnScoreUpdated?.Invoke(_totalScore, _maxScore);

        Debug.Log($"[ObjectiveSystem] ✓ {obj.description} — +{obj.pointsEarned}pts");
    }

    public List<Objective>  GetObjectives()  => _objectives;
    public int              GetTotalScore()  => _totalScore;
    public int              GetMaxScore()    => _maxScore;
    public float            GetScorePercent() =>
        _maxScore > 0 ? (float)_totalScore / _maxScore : 0f;

    // ─────────────────────────────────────────────
    //  Manejo de fin de sesión
    // ─────────────────────────────────────────────

    void HandleLevelCompleted(LevelType level, bool success)
    {
        // Si el nivel se completó con éxito, dar bono de velocidad
        if (success && performance != null)
        {
            float elapsed = performance.GetTime();
            float limit   = gameManager?.currentTimeLimit ?? 600f;
            float bonus   = Mathf.Clamp01(1f - elapsed / limit);

            // Completar el último objetivo del nivel automáticamente con bono
            int lastIdx = _objectives.Count - 1;
            CompleteObjective(lastIdx, bonus);
        }
    }

    // ─────────────────────────────────────────────
    //  Auto-completar objetivos por estado del circuito
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detecta hitos observables en el circuito y marca el objetivo correspondiente.
    /// Solo completa cada objetivo una vez (CompleteObjective es idempotente).
    /// </summary>
    void AutoCompleteByCircuitState()
    {
        if (gameManager?.circuit == null) return;

        var circuit = gameManager.circuit;

        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                // Obj 0: multímetro conectado a dos nodos — solo si el jugador usó el multímetro
                if (_multimeterUsed)
                    CompleteObjectiveOnce(0);
                // Obj 1: voltaje leído — solo tras usar el multímetro y obtener lectura > 0
                if (_multimeterUsed && circuit.totalCurrent > 0f)
                    CompleteObjectiveOnce(1);
                // Obj 2: resistencia seleccionada → delegado a TechnicianActions
                // Obj 3: reparación aplicada → se completa en HandleLevelCompleted
                break;

            case LevelType.Parallel:
                // Obj 0: multímetro en uso — solo si el jugador conectó las puntas
                if (_multimeterUsed)
                    CompleteObjectiveOnce(0);
                // Obj 1: rama rota identificada — solo tras medir y haber un LED apagado
                if (_multimeterUsed && circuit.totalCurrent > 0f)
                    CompleteObjectiveOnce(1);
                // Obj 2: reparación → HandleLevelCompleted
                break;

            case LevelType.Mixed:
                // Obj 0: capacitor invertido detectado
                foreach (var c in circuit.components)
                    if (c is Capacitor cap && cap.polarityInverted)
                    { CompleteObjectiveOnce(0); break; }

                // Obj 1: LED invertido detectado
                foreach (var c in circuit.components)
                    if (c is LED led && led.polarityInverted)
                    { CompleteObjectiveOnce(1); break; }

                // Obj 2: resistencia incorrecta identificada
                foreach (var c in circuit.components)
                    if (c is Resistor r && r.hasFault)
                    { CompleteObjectiveOnce(2); break; }

                // Obj 3: todas las fallas corregidas → HandleLevelCompleted
                break;

            case LevelType.Arduino:
                // Obj 0: multímetro en escena
                if (circuit.sourceVoltage > 0f)
                    CompleteObjectiveOnce(0);

                // Obj 1: pin corregido
                bool pinOk = true;
                foreach (var c in circuit.components)
                    if (c is ArduinoPin p && p.hasFault) { pinOk = false; break; }
                if (pinOk && gameManager.HasPerformedRepair())
                    CompleteObjectiveOnce(1);

                // Obj 2: resistencia buzzer corregida
                bool resOk = true;
                foreach (var c in circuit.components)
                    if (c is Resistor rs && rs.hasFault) { resOk = false; break; }
                if (resOk && gameManager.HasPerformedRepair())
                    CompleteObjectiveOnce(2);

                // Obj 3: cable suelto reparado
                bool cableOk = true;
                foreach (var c in circuit.components)
                    if (c is ArduinoPin ap && ap.hasLooseCable) { cableOk = false; break; }
                if (cableOk && gameManager.HasPerformedRepair())
                    CompleteObjectiveOnce(3);

                // Obj 4: verificación → HandleLevelCompleted
                break;
        }
    }

    void CompleteObjectiveOnce(int index)
    {
        if (index < 0 || index >= _objectives.Count) return;
        if (_objectives[index].isCompleted) return;
        CompleteObjective(index);
    }

    void HandleGameCompleted()
    {
        var result = new SessionResult
        {
            totalScore      = _totalScore,
            maxScore        = _maxScore,
            scorePercent    = GetScorePercent(),
            totalErrors     = performance?.GetErrors() ?? 0,
            totalTimeSeconds = performance?.GetTime()  ?? 0f,
            evaluation      = performance?.GetEvaluation() ?? "—"
        };

        OnSessionEnded?.Invoke(result);
        Debug.Log($"[ObjectiveSystem] Sesión terminada: {_totalScore}/{_maxScore} pts " +
                  $"({result.scorePercent:P0}) — {result.evaluation}");
    }
}

// ─────────────────────────────────────────────
//  Estructuras de datos
// ─────────────────────────────────────────────

[Serializable]
public struct Objective
{
    public string description;
    public int    maxPoints;
    public int    pointsEarned;
    public bool   isCompleted;
}

[Serializable]
public struct SessionResult
{
    public int    totalScore;
    public int    maxScore;
    public float  scorePercent;
    public int    totalErrors;
    public float  totalTimeSeconds;
    public string evaluation;
}