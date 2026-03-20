using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Singleton to manage the NetworkRunner, QuickMatch, and scene synchronization for Fusion Shared Mode.
/// </summary>
public class FusionNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionNetworkManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("The name of the scene to load when the game starts.")]
    public string gameSceneName = "Game";

    /// <summary>
    /// Provides access to the current NetworkRunner instance.
    /// </summary>
    public NetworkRunner Runner => _runner;

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Starts a quick match in Shared Mode.
    /// </summary>
    public async void QuickMatch()
    {
        if (_runner != null)
        {
            Debug.Log("A runner is already active. Shutting it down before starting a new one.");
            await _runner.Shutdown();
        }

        // Ensure the scene manager exists
        if (_sceneManager == null)
        {
            _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        // Create and configure the runner
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this); // Add this manager as a callback handler

        Debug.Log("Starting game in Shared Mode...");
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = "QuickMatchSession", // Any name works for quick matching
            PlayerCount = 8,
            SceneManager = _sceneManager,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{gameSceneName}.unity"))
        });

        if (result.Ok)
        {
            Debug.Log("StartGame successful. Runner is now active.");
        }
        else
        {
            Debug.LogError($"Fusion StartGame failed: {result.ShutdownReason}");
            if (_runner != null)
            {
                Destroy(_runner.gameObject); // Clean up the runner GameObject
            }
        }
    }

    // --- INetworkRunnerCallbacks ---
    // We only need a few callbacks for this simple setup.
    // The spawner will handle OnPlayerJoined and OnSceneLoadDone.

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server.");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
        // Clean up runner instance
        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }
    }

    // --- Unused Callbacks ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        data.MoveDirection = new Vector3(horizontal, 0f, vertical);
        data.LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        input.Set(data);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Notify spawner to spawn local player after scene loads
        var spawner = FindFirstObjectByType<NetworkPlayerSpawner>();
        if (spawner != null)
        {
            spawner.OnSceneLoadDone(runner);
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}

