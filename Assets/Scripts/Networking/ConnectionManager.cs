using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Configuración de Red")]
    [SerializeField] private NetworkPrefabRef playerPrefab = default;

    public enum AutoConnectRole { Ninguno, Explorador, Tecnico }

    [Header("Configuración de Escena")]
    [Tooltip("Selecciona si esta escena debe conectarse automáticamente al iniciar.")]
    public AutoConnectRole rolAutomatico = AutoConnectRole.Ninguno;

    private NetworkRunner _runner;

    [Header("Modo Offline / Testing")]
    [Tooltip("Si está activo, omite Fusion y activa el entorno local directamente.")]
    public bool modoOffline = false;

    [Tooltip("Segundos esperando conexión antes de fallback a offline. 0 = sin límite.")]
    [Range(0f, 30f)]
    public float connectionTimeoutSeconds = 12f;

    [Tooltip("GO 'Entorno del explorador' a activar en modo offline. Si queda vacío se busca por nombre.")]
    public GameObject entornoExplorador;

    // ─────────────────────────────────────────────
    //  Eventos estáticos
    // ─────────────────────────────────────────────

    /// <summary>Se dispara cuando un jugador remoto se desconecta.</summary>
    public static event Action<PlayerRef> OnPlayerDisconnected;

    /// <summary>Se dispara cuando la conexión falla o se agota el tiempo.</summary>
    public static event Action<string>    OnConnectionFailed;

    // ─────────────────────────────────────────────
    private Coroutine _connectionTimeout;
    private bool      _connected;

    // ─────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_runner != null && _runner.IsRunning)
            _runner.Shutdown();
    }

    private void Start()
    {
        bool esTecnico = rolAutomatico == AutoConnectRole.Tecnico;

        if (modoOffline)
        {
            if (!esTecnico)
                ActivarEntornoExplorador();
            else
                Debug.Log("[Red] Modo offline Técnico — sin entorno VR, sin Fusion.");
            return;
        }

        if (rolAutomatico == AutoConnectRole.Explorador)
        {
            Debug.Log("[Red] Conectando automáticamente como Explorador...");
            StartSimulation(GameMode.Client);
        }
        else if (esTecnico)
        {
            Debug.Log("[Red] Creando servidor automáticamente como Técnico...");
            StartSimulation(GameMode.Host);
        }
    }

    void ActivarEntornoExplorador()
    {
        if (entornoExplorador == null)
            entornoExplorador = BuscarIncluyendoInactivos("Entorno del explorador");

        if (entornoExplorador != null)
        {
            entornoExplorador.SetActive(true);
            Debug.Log("[Red] Modo offline: 'Entorno del explorador' activado localmente.");
        }
        else
        {
            Debug.LogWarning("[Red] Modo offline: no se encontró 'Entorno del explorador'. Asígnalo en el Inspector.");
        }
    }

    static GameObject BuscarIncluyendoInactivos(string nombre)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == nombre) return root;
            var child = root.transform.Find(nombre);
            if (child != null) return child.gameObject;
        }
        return null;
    }

    public async void StartSimulation(GameMode mode)
    {
        _runner = gameObject.GetComponent<NetworkRunner>();
        if (_runner == null)
            _runner = gameObject.AddComponent<NetworkRunner>();

        if (_runner.IsRunning)
        {
            Debug.LogWarning($"[Red] StartSimulation ignorado — el runner ya está corriendo ({_runner.GameMode}).");
            return;
        }

        _runner.ProvideInput = true;

        if (connectionTimeoutSeconds > 0f)
            _connectionTimeout = StartCoroutine(ConnectionTimeout());

        Debug.Log($"[Red] Iniciando sesión como: {mode}");

        StartGameResult result;
        try
        {
            result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode    = mode,
                SessionName = "LaboratorioUbicua",
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Red] StartGame lanzó excepción: {ex.Message}. Fallback a offline.");
            StopConnectionTimeout();
            FallbackModoOffline("Error al iniciar Fusion.");
            return;
        }

        if (!result.Ok)
        {
            Debug.LogWarning($"[Red] StartGame falló: {result.ShutdownReason}. Fallback a offline.");
            StopConnectionTimeout();
            FallbackModoOffline($"No se pudo conectar ({result.ShutdownReason}).");
            return;
        }

        StopConnectionTimeout();
        _connected = true;

        if (mode == GameMode.Host || mode == GameMode.Server)
        {
            // Verificar que el runner sigue activo — el timeout puede haberlo apagado
            // mientras el await StartGame() estaba pendiente.
            if (_runner == null || !_runner.IsRunning) return;

            var sessionPrefab = Resources.Load<NetworkObject>("GameSession");
            if (sessionPrefab != null)
            {
                _runner.Spawn(sessionPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log("[Red] GameSession spawneada en la red.");
            }
            else
            {
                Debug.LogWarning("[Red] Prefab 'GameSession' no encontrado en Resources/. " +
                                 "Coloca el prefab en Assets/Resources/GameSession.prefab con NetworkObject.");
            }
        }
    }

    IEnumerator ConnectionTimeout()
    {
        yield return new WaitForSecondsRealtime(connectionTimeoutSeconds);
        if (!_connected)
        {
            Debug.LogWarning($"[Red] Timeout ({connectionTimeoutSeconds}s) sin conexión. Fallback a offline.");
            FallbackModoOffline("Tiempo de espera agotado. Iniciando modo offline.");
            _runner?.Shutdown();
        }
    }

    void StopConnectionTimeout()
    {
        if (_connectionTimeout != null)
        {
            StopCoroutine(_connectionTimeout);
            _connectionTimeout = null;
        }
    }

    void FallbackModoOffline(string razon = "")
    {
        if (modoOffline) return;
        modoOffline = true;
        Debug.LogWarning($"[Red] Fallback a modo offline. {razon}");
        if (!string.IsNullOrEmpty(razon))
            OnConnectionFailed?.Invoke(razon);
        if (rolAutomatico != AutoConnectRole.Tecnico)
            ActivarEntornoExplorador();
    }

    // --- Métodos para los Botones de la UI ---
    public void IniciarComoTecnico() => StartSimulation(GameMode.Host);

    // --- Callbacks de Fusion ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        Debug.Log($"[Red] Jugador {player.PlayerId} se ha unido.");
        if (playerPrefab.IsValid)
            runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);
        else
            Debug.LogWarning("[Red] playerPrefab no asignado — se omite el spawn del avatar. " +
                             "Asígnalo en el Inspector de ConnectionManager si necesitas avatares de red.");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Red] Jugador {player.PlayerId} se ha desconectado.");
        OnPlayerDisconnected?.Invoke(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Red] Runner apagado: {shutdownReason}");
        _runner    = null;
        _connected = false;
        StopConnectionTimeout();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _connected = true;
        StopConnectionTimeout();
        Debug.Log("[Red] Conectado al servidor.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[Red] Desconectado del servidor: {reason}");
        _runner    = null;
        _connected = false;
        FallbackModoOffline($"Desconectado: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"[Red] Conexión fallida a {remoteAddress}: {reason}");
        StopConnectionTimeout();
        FallbackModoOffline($"No se pudo conectar al servidor ({reason}).");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
