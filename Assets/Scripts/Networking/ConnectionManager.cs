using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Configuración de Red")]
    [SerializeField] private NetworkPrefabRef playerPrefab;

    public enum AutoConnectRole { Ninguno, Explorador, Tecnico }

    [Header("Configuración de Escena")]
    [Tooltip("Selecciona si esta escena debe conectarse automáticamente al iniciar.")]
    public AutoConnectRole rolAutomatico = AutoConnectRole.Ninguno;

    private NetworkRunner _runner;

    private void Start()
    {
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
            GameMode = mode,
            SessionName = "LaboratorioUbicua",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // 2. Si eres el Técnico (Host), inicializamos el sistema de entrega
        if (mode == GameMode.Host || mode == GameMode.Server)
        {
            var deliverySystem = FindObjectOfType<ComponentDeliverySystemBackup>();
            if (deliverySystem != null)
            {
                deliverySystem.InicializarManual(_runner);
                Debug.Log("[Red] Sistema de entrega conectado al NetworkRunner.");
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