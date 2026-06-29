using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Recolecta los datos de la sesión de juego y los expone de forma
/// thread-safe para que DashboardServer los sirva al navegador del docente.
/// También escribe un JSON en Application.persistentDataPath al finalizar.
///
/// SETUP: Añadir al mismo GO que DashboardServer (ej. NetworkManager).
/// </summary>
public class SessionDataExporter : MonoBehaviour
{
    public static SessionDataExporter Instance { get; private set; }

    private readonly object      _lock = new object();
    private SessionExportData    _data = new SessionExportData();
    private SessionHistory       _history = new SessionHistory();
    private SessionLiveData      _live = new SessionLiveData();

    // Refresco en vivo (hilo principal); el servidor HTTP solo lee el snapshot bajo lock.
    private PerformanceTracker   _tracker;
    private GameManager          _gm;
    private float                _nextLiveRefresh;

    const string HISTORY_FILE = "sessions_history.json";

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadHistory();
    }

    void OnEnable()
    {
        ObjectiveSystem.OnSessionEnded += HandleSessionEnded;
        GameManager.OnLevelLoaded      += HandleLevelLoaded;
    }

    void OnDisable()
    {
        ObjectiveSystem.OnSessionEnded -= HandleSessionEnded;
        GameManager.OnLevelLoaded      -= HandleLevelLoaded;
    }

    // ─────────────────────────────────────────────
    //  API pública (thread-safe)
    // ─────────────────────────────────────────────

    public SessionExportData GetSnapshot()
    {
        lock (_lock) { return _data; }
    }

    /// <summary>Historial (lista) de todas las sesiones finalizadas — para el dashboard.</summary>
    public SessionHistory GetHistorySnapshot()
    {
        lock (_lock) { return _history; }
    }

    /// <summary>Estado EN VIVO de la sesión en curso — para el panel docente (ambos roles).</summary>
    public SessionLiveData GetLiveSnapshot()
    {
        lock (_lock) { return _live; }
    }

    // ─────────────────────────────────────────────
    //  Refresco en vivo (hilo principal de Unity)
    // ─────────────────────────────────────────────
    void Update()
    {
        if (Time.unscaledTime < _nextLiveRefresh) return;
        _nextLiveRefresh = Time.unscaledTime + 0.5f;
        RefreshLive();
    }

    void RefreshLive()
    {
        if (_tracker == null) _tracker = FindAnyObjectByType<PerformanceTracker>(FindObjectsInactive.Include);
        if (_gm == null)      _gm      = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);

        var live = new SessionLiveData { active = _gm != null };

        if (_gm != null)
            live.currentReto = LevelName(_gm.currentLevel);

        if (_tracker != null)
        {
            live.currentTimeSeconds = _tracker.GetTime();
            live.currentErrors      = _tracker.GetErrors();
            live.currentErrorTypes  = _tracker.GetErrorBreakdown();

            var recs = _tracker.GetAllRecords();
            var dto  = new LevelRecordDto[recs.Count];
            for (int i = 0; i < recs.Count; i++) dto[i] = new LevelRecordDto(recs[i]);
            live.completedRecords = dto;
            live.retosCompletados = recs.Count;
        }

        var gs = GameSession.Instance;
        live.exploradorConectado = gs != null && gs.ExploradorListo;
        live.tecnicoConectado    = gs != null;   // el Host instancia GameSession

        lock (_lock)
        {
            live.state = _data.state;
            _live = live;
        }
    }

    public void SetAccessCode(string code)
    {
        lock (_lock) { _data.accessCode = code; }
    }

    // ─────────────────────────────────────────────
    //  Handlers
    // ─────────────────────────────────────────────

    void HandleLevelLoaded(LevelType level)
    {
        lock (_lock)
        {
            _data.currentReto = LevelName(level);
            _data.state       = "En progreso";
        }
    }

    void HandleSessionEnded(SessionResult result)
    {
        var tracker = FindAnyObjectByType<PerformanceTracker>(FindObjectsInactive.Include);
        var records = tracker != null ? tracker.GetAllRecords() : new List<LevelRecord>();

        var serialized = new LevelRecordDto[records.Count];
        for (int i = 0; i < records.Count; i++)
            serialized[i] = new LevelRecordDto(records[i]);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        lock (_lock)
        {
            _data.hasResult    = true;
            _data.state        = "Sesión finalizada";
            _data.summary      = new SessionResultDto(result);
            _data.records      = serialized;
            _data.timestamp    = stamp;

            // Añadir esta sesión al HISTORIAL (lista de todas las sesiones), con sus registros por reto.
            var lista = new List<SessionSummaryDto>(_history.sessions)
            {
                new SessionSummaryDto(result, stamp, _data.accessCode, serialized)
            };
            _history.sessions = lista.ToArray();
        }

        SaveToDisk();
        SaveHistory();
    }

    // ─────────────────────────────────────────────

    void SaveToDisk()
    {
        try
        {
            SessionExportData snapshot;
            lock (_lock) { snapshot = _data; }

            string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, "session_results.json");
            File.WriteAllText(path, json);
            Debug.Log($"[SessionDataExporter] Guardado en: {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SessionDataExporter] Error al guardar: {e.Message}");
        }
    }

    void LoadHistory()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, HISTORY_FILE);
            if (!File.Exists(path)) return;
            var loaded = JsonUtility.FromJson<SessionHistory>(File.ReadAllText(path));
            if (loaded != null && loaded.sessions != null) _history = loaded;
            Debug.Log($"[SessionDataExporter] Historial cargado: {_history.sessions.Length} sesiones.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SessionDataExporter] No se pudo cargar el historial: {e.Message}");
        }
    }

    void SaveHistory()
    {
        try
        {
            SessionHistory snapshot;
            lock (_lock) { snapshot = _history; }

            string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, HISTORY_FILE);
            File.WriteAllText(path, json);
            Debug.Log($"[SessionDataExporter] Historial guardado ({snapshot.sessions.Length} sesiones) en: {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SessionDataExporter] Error al guardar el historial: {e.Message}");
        }
    }

    public static string LevelName(LevelType level) => level switch
    {
        LevelType.OhmLaw   => "Reto 1 — Ley de Ohm",
        LevelType.Parallel => "Reto 2 — Paralelo",
        LevelType.Mixed    => "Reto 3 — Mixto",
        LevelType.Arduino  => "Reto 4 — Arduino",
        _                  => level.ToString()
    };
}

// ─────────────────────────────────────────────────────────
//  DTOs serializables por JsonUtility
// ─────────────────────────────────────────────────────────

[Serializable]
public class SessionExportData
{
    public bool              hasResult   = false;
    public string            currentReto = "Sin iniciar";
    public string            state       = "En espera";
    public string            timestamp   = "";
    public string            accessCode  = "----";
    public SessionResultDto  summary     = new SessionResultDto();
    public LevelRecordDto[]  records     = Array.Empty<LevelRecordDto>();
}

[Serializable]
public class SessionResultDto
{
    public int    totalScore;
    public int    maxScore;
    public float  scorePercent;
    public int    totalErrors;
    public float  totalTimeSeconds;
    public string evaluation = "";

    public SessionResultDto() { }
    public SessionResultDto(SessionResult r)
    {
        totalScore       = r.totalScore;
        maxScore         = r.maxScore;
        scorePercent     = r.scorePercent;
        totalErrors      = r.totalErrors;
        totalTimeSeconds = r.totalTimeSeconds;
        evaluation       = r.evaluation;
    }
}

[Serializable]
public class LevelRecordDto
{
    public string         levelName  = "";
    public float          timeSeconds;
    public int            errors;
    public bool           success;
    public string         evaluation = "";
    public ErrorTagCount[] errorTypes = Array.Empty<ErrorTagCount>();

    public LevelRecordDto() { }
    public LevelRecordDto(LevelRecord r)
    {
        levelName   = SessionDataExporter.LevelName(r.level);
        timeSeconds = r.timeSeconds;
        errors      = r.errors;
        success     = r.success;
        evaluation  = r.evaluation;
        errorTypes  = r.errorTypes ?? Array.Empty<ErrorTagCount>();
    }
}

// ─────────────────────────────────────────────────────────
//  Datos EN VIVO de la sesión en curso (para el panel docente)
// ─────────────────────────────────────────────────────────

[Serializable]
public class SessionLiveData
{
    public bool             active;                 // hay una partida en escena
    public string           state               = "En espera";
    public string           currentReto         = "Sin iniciar";
    public float            currentTimeSeconds;     // tiempo en el reto en curso
    public int              currentErrors;          // errores en el reto en curso
    public ErrorTagCount[]  currentErrorTypes   = Array.Empty<ErrorTagCount>();
    public int              retosCompletados;
    public LevelRecordDto[] completedRecords    = Array.Empty<LevelRecordDto>();
    public bool             exploradorConectado;
    public bool             tecnicoConectado;
}

// ─────────────────────────────────────────────────────────
//  Historial de sesiones (lista para el dashboard)
// ─────────────────────────────────────────────────────────

[Serializable]
public class SessionHistory
{
    public SessionSummaryDto[] sessions = Array.Empty<SessionSummaryDto>();
}

[Serializable]
public class SessionSummaryDto
{
    public string timestamp  = "";
    public string accessCode = "----";
    public string evaluation = "";
    public int    totalScore;
    public int    maxScore;
    public float  scorePercent;
    public int    totalErrors;
    public float  totalTimeSeconds;
    public LevelRecordDto[] records = Array.Empty<LevelRecordDto>();   // registros por reto de esta sesión

    public SessionSummaryDto() { }
    public SessionSummaryDto(SessionResult r, string stamp, string code, LevelRecordDto[] recs = null)
    {
        timestamp        = stamp;
        accessCode       = code;
        evaluation       = r.evaluation;
        totalScore       = r.totalScore;
        maxScore         = r.maxScore;
        scorePercent     = r.scorePercent;
        totalErrors      = r.totalErrors;
        totalTimeSeconds = r.totalTimeSeconds;
        records          = recs ?? Array.Empty<LevelRecordDto>();
    }
}
