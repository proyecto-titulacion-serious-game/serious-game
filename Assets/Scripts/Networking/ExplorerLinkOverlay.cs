using UnityEngine;
using UnityEngine.XR;
using Fusion;

/// <summary>
/// Overlay del TÉCNICO (PC) que avisa el estado del enlace con el Explorador (VR).
///
/// Resuelve el "Peligro 2": en el laboratorio el Explorador se quita las gafas y la Quest
/// suspende la pantalla a los ~15 s, congelando su escena. Sin aviso, el Técnico cree que el
/// juego se colgó. Este overlay distingue tres situaciones y oscurece la pantalla en las de alarma.
///
/// Señales usadas (todas ya existentes, sin tocar gameplay):
///   • Nº de jugadores de Fusion (SessionInfo.PlayerCount): presencia autoritativa del Explorador.
///   • ConnectionManager.OnPlayerDisconnected: aviso inmediato de salida.
///   • GameSession.LastTelemetryRealtime: heartbeat ~5 Hz del sandbox del Reto 4. Si se corta de
///     golpe (y seguimos "conectados"), el visor se durmió → detección más rápida que el timeout
///     de Fusion.
///
/// 100% aditivo y sin cablear nada: se auto-crea al cargar la escena. Solo actúa en el Host
/// (Técnico) y en PC (no XR).
/// </summary>
public class ExplorerLinkOverlay : MonoBehaviour
{
    static ExplorerLinkOverlay _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        if (XRSettings.isDeviceActive) return; // overlay del Técnico (PC); el visor no lo necesita
        var go = new GameObject("[ExplorerLinkOverlay]");
        _instance = go.AddComponent<ExplorerLinkOverlay>();
        DontDestroyOnLoad(go);
    }

    enum LinkState { Oculto, EsperandoIngreso, Desconectado, Suspendido }

    // Sin telemetría por más de esto (estando conectados y en Reto 4) → posible suspensión.
    const float TelemetryStaleSecs = 4f;

    NetworkRunner _runner;
    float         _searchCd;
    bool          _explorerSeen;          // ¿el Explorador llegó a estar presente alguna vez?
    LinkState     _state = LinkState.Oculto;

    GUIStyle  _dim, _box, _title, _body;
    Texture2D _dimTex, _boxTex;

    // ─── Suscripción a la desconexión (aviso inmediato) ──────────────────
    void OnEnable()  { ConnectionManager.OnPlayerDisconnected += OnPlayerGone; }
    void OnDisable() { ConnectionManager.OnPlayerDisconnected -= OnPlayerGone; }

    void OnPlayerGone(PlayerRef _)
    {
        if (_runner != null && _runner.IsServer)
            Debug.Log("[ExplorerLinkOverlay] El Explorador se desconectó de la sala.");
    }

    // ─── Evaluación de estado ────────────────────────────────────────────
    void Update()
    {
        if (_runner == null)
        {
            _searchCd -= Time.unscaledDeltaTime;
            if (_searchCd <= 0f) { _runner = FindAnyObjectByType<NetworkRunner>(); _searchCd = 0.5f; }
        }
        _state = EvaluarEstado();
    }

    LinkState EvaluarEstado()
    {
        // Solo el Técnico (Host) vigila al Explorador. Sin runner activo (offline) → nada.
        if (_runner == null || !_runner.IsRunning || !_runner.IsServer) return LinkState.Oculto;

        int pc = PlayerCount();

        // Presencia CONFIRMADA del Explorador (Host + 1).
        if (pc >= 2)
        {
            _explorerSeen = true;

            // ¿Suspensión? Solo tiene sentido en el Reto 4 (Arduino), donde fluye la telemetría.
            // Aceptamos 3 o 4 por si RetoActual es 0-based (Arduino=3) o 1-based (Arduino=4).
            var gs = GameSession.Instance;
            bool enReto4 = gs != null && (gs.RetoActual == 3 || gs.RetoActual == 4);
            if (gs != null && enReto4 && gs.TelemHasData &&
                Time.unscaledTime - gs.LastTelemetryRealtime > TelemetryStaleSecs)
                return LinkState.Suspendido;

            return LinkState.Oculto;
        }

        // Desconocido (SessionInfo aún no lista): no alarmar ni marcar "visto".
        if (pc < 0) return LinkState.Oculto;

        // pc == 1 (solo el Host): no conectado → distinguir "aún no entró" de "se fue".
        return _explorerSeen ? LinkState.Desconectado : LinkState.EsperandoIngreso;
    }

    int PlayerCount()
    {
        try
        {
            if (_runner != null && _runner.SessionInfo != null && _runner.SessionInfo.IsValid)
                return _runner.SessionInfo.PlayerCount;
        }
        catch { /* SessionInfo aún no disponible */ }
        return -1; // desconocido
    }

    // ─── Render (PC, OnGUI) ──────────────────────────────────────────────
    void OnGUI()
    {
        if (_state == LinkState.Oculto) return;

        EnsureStyles();

        bool alarma = _state == LinkState.Desconectado || _state == LinkState.Suspendido;

        // Oscurecer la pantalla en estados de alarma para que el Técnico no crea que se colgó.
        if (alarma)
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _dim);

        string title, body;
        switch (_state)
        {
            case LinkState.EsperandoIngreso:
                title = "ESPERANDO AL EXPLORADOR";
                body  = "El visor VR aún no entra a la sala. Pídele que se ponga las gafas " +
                        "y arranque la app.";
                break;
            case LinkState.Suspendido:
                title = "VISOR EN SUSPENSIÓN";
                body  = "El Explorador dejó de responder (posible ahorro de energía de la Quest). " +
                        "Pídele que toque la pantalla del visor para despertarlo.";
                break;
            default: // Desconectado
                title = "EXPLORADOR DESCONECTADO";
                body  = "Se perdió la conexión con el visor VR. Esperando a que vuelva a la sala...";
                break;
        }

        const float w = 580f, h = 156f;
        float y = alarma ? (Screen.height - h) * 0.5f : 12f;
        var rect = new Rect((Screen.width - w) * 0.5f, y, w, h);
        GUI.Box(rect, GUIContent.none, _box);

        GUILayout.BeginArea(new Rect(rect.x + 22, rect.y + 18, w - 44, h - 36));
        GUILayout.Label(title, _title);
        GUILayout.Space(8);
        GUILayout.Label(body, _body);
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (_dimTex == null)
        {
            _dimTex = new Texture2D(1, 1);
            _dimTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            _dimTex.Apply();
        }
        if (_boxTex == null)
        {
            _boxTex = new Texture2D(1, 1);
            _boxTex.SetPixel(0, 0, new Color(0.10f, 0.03f, 0.03f, 0.96f));
            _boxTex.Apply();
        }
        if (_dim == null)
            _dim = new GUIStyle(GUI.skin.box) { normal = { background = _dimTex } };
        if (_box == null)
            _box = new GUIStyle(GUI.skin.box) { normal = { background = _boxTex } };
        if (_title == null)
            _title = new GUIStyle(GUI.skin.label)
            { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.55f, 0.2f) } };
        if (_body == null)
            _body = new GUIStyle(GUI.skin.label)
            { fontSize = 15, wordWrap = true, normal = { textColor = Color.white } };
    }
}
