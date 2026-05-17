using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Configuración de Red")]
    [SerializeField] private NetworkPrefabRef playerPrefab = default;

    public enum AutoConnectRole { Ninguno, Explorador, Tecnico }

    [Header("Configuración de Escena")]
    [Tooltip("Selecciona si esta escena debe conectarse automáticamente al iniciar.")]
    public AutoConnectRole rolAutomatico = AutoConnectRole.Ninguno;

    private NetworkRunner _runner;

    [Header("Modo Offline")]
    [Tooltip("Si está activo, omite Fusion y activa el entorno local directamente (útil para testing sin red).")]
    public bool modoOffline = false;

    [Tooltip("GO 'Entorno del explorador' a activar en modo offline. Si queda vacío se busca por nombre.")]
    public GameObject entornoExplorador;

    private void Start()
    {
        if (modoOffline)
        {
            ActivarEntornoLocal();
            return;
        }

        // Conexión automática para la escena del Explorador
        if (rolAutomatico == AutoConnectRole.Explorador)
        {
            Debug.Log("[Red] Conectando automáticamente como Explorador...");
            StartSimulation(GameMode.Client);
        }
        // NUEVO: Conexión automática para la escena del Técnico
        else if (rolAutomatico == AutoConnectRole.Tecnico)
        {
            Debug.Log("[Red] Creando servidor automáticamente como Técnico...");
            StartSimulation(GameMode.Host);
        }
    }

    void ActivarEntornoLocal()
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
        // GameObject.Find() omite objetos inactivos; hay que iterar la jerarquía manualmente
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

        _runner.ProvideInput = true;

        Debug.Log($"[Red] Iniciando sesión como: {mode}");

        // 1. Iniciar la conexión a la misma sala siempre
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode    = mode,
            SessionName = "LaboratorioUbicua",
            // Scene = SceneRef.None → cada jugador se queda en su propia escena.
            // Fusion sincroniza objetos de red pero NO carga/descarga escenas.
        });

        // 2. Si eres el Host (Técnico), spawnear la GameSession compartida en la red
        if (mode == GameMode.Host || mode == GameMode.Server)
        {
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

    // --- Métodos para los Botones de la UI (Solo los usará el Técnico) ---
    public void IniciarComoTecnico() => StartSimulation(GameMode.Host);

    // --- Callbacks de Fusion ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Cuando alguien se conecta, el Servidor/Host le crea su "cuerpo" en la red
        if (runner.IsServer)
        {
            Debug.Log($"[Red] Jugador {player.PlayerId} se ha unido. Generando Avatar...");
            runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);
        }
    }

    // --- Callbacks Vacíos Obligatorios ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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