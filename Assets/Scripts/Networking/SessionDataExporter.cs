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

            // Añadir esta sesión al HISTORIAL (lista de todas las sesiones).
            var lista = new List<SessionSummaryDto>(_history.sessions)
            {
                new SessionSummaryDto(result, stamp, _data.accessCode)
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
    public string levelName  = "";
    public float  timeSeconds;
    public int    errors;
    public bool   success;
    public string evaluation = "";

    public LevelRecordDto() { }
    public LevelRecordDto(LevelRecord r)
    {
        levelName   = SessionDataExporter.LevelName(r.level);
        timeSeconds = r.timeSeconds;
        errors      = r.errors;
        success     = r.success;
        evaluation  = r.evaluation;
    }
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

    public SessionSummaryDto() { }
    public SessionSummaryDto(SessionResult r, string stamp, string code)
    {
        timestamp        = stamp;
        accessCode       = code;
        evaluation       = r.evaluation;
        totalScore       = r.totalScore;
        maxScore         = r.maxScore;
        scorePercent     = r.scorePercent;
        totalErrors      = r.totalErrors;
        totalTimeSeconds = r.totalTimeSeconds;
    }
}
