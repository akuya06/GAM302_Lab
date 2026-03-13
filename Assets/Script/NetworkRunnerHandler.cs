using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Quản lý NetworkRunner cho chế độ chơi zombies online.
/// Setup các callback và initialize các network components cần thiết.
/// </summary>
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    private void OnEnable()
    {
        // Tìm runner nếu đã tồn tại (e.g. khi scene được load)
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            InitializeNetworkComponents();
        }
    }

    private void OnDisable()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    /// <summary>
    /// Initialize các network components cần thiết cho game
    /// </summary>
    private void InitializeNetworkComponents()
    {
        // Find and setup NetworkRestartManager nếu tồn tại trong scene
        var restartManager = FindFirstObjectByType<NetworkRestartManager>();
        if (restartManager != null && restartManager.TryGetComponent<NetworkObject>(out var networkObj))
        {
            Debug.Log("[NetworkRunnerHandler] NetworkRestartManager initialized");
        }

        // Setup các component khác cần thiết cho zombie game
        Debug.Log("[NetworkRunnerHandler] Network components initialized");
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // ───────────────── INetworkRunnerCallbacks Implementation ─────────────────────
    // ════════════════════════════════════════════════════════════════════════════════

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
        Debug.Log($"[NetworkRunnerHandler] Player {player} joined");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkRunnerHandler] Player {player} left");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Input handling được xử lý bởi NetworkInputProvider
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Xử lý input bị mất - có thể thêm default input nếu cần
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkRunnerHandler] Network shutdown: {shutdownReason}");
        _runner = null;

        // Reset các component khi network bị tắt
        var restartManager = FindFirstObjectByType<NetworkRestartManager>();
        if (restartManager != null)
        {
            restartManager.ResetRestartState();
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkRunnerHandler] Disconnected from server: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Chấp nhận tất cả kết nối (có thể thêm password validation ở đây nếu cần)
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"[NetworkRunnerHandler] Connect failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // Xử lý custom simulation messages nếu cần
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Update session list cho UI nếu cần
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // Handle custom auth response
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[NetworkRunnerHandler] Host migration occurred");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] Scene load starting");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] Scene load completed");
        InitializeNetworkComponents();
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Xử lý khi object rời khỏi area of interest
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Xử lý khi object vào area of interest
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // Xử lý reliable data từ các players nếu cần
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // Track progress của reliable data nếu cần
    }
}
