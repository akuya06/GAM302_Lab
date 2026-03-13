using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

/// <summary>
/// Put this on a permanent GameObject in the Game scene.
/// It listens to player join/leave events and spawns / despawns the player prefab
/// at the next available spawn point.
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab (must have NetworkObject + NetworkPlayerController)")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Points (set in scene, leave empty to spawn at origin)")]
    [SerializeField] private Transform[] spawnPoints;

    // Maps PlayerRef → spawned NetworkObject so we can despawn cleanly
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers
        = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkRunner _runner;

    // ── register this object as a callback listener when the runner comes online ──
    private void OnEnable()
    {
        // find the runner if it already exists (e.g. when scene is loaded)
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            SpawnExistingPlayersIfNeeded(_runner);
        }
    }

    private void OnDisable()
    {
        _runner?.RemoveCallbacks(this);
    }

    // ── INetworkRunnerCallbacks ──────────────────────────────────────────────────

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        try
        {
            _runner = runner;

            // Only the host/server spawns objects
            if (!runner.IsServer) return;

            TrySpawnPlayer(runner, player);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkPlayerSpawner] Error on player joined: {e.Message}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
            Debug.Log($"[NetworkPlayerSpawner] Despawned player {player}");
        }
    }

    // Returns a spread-out spawn position based on player index
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // Default: spread players apart by 2 m on the X axis
        return new Vector3(player.PlayerId * 2f, 0f, 0f);
    }

    private void SpawnExistingPlayersIfNeeded(NetworkRunner runner)
    {
        if (runner == null || !runner.IsServer)
            return;

        foreach (var player in runner.ActivePlayers)
        {
            TrySpawnPlayer(runner, player);
        }
    }

    private void TrySpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.ContainsKey(player))
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] playerPrefab is not assigned!");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(player);
        Quaternion spawnRot = Quaternion.identity;

        var spawnedObj = runner.Spawn(
            playerPrefab,
            spawnPos,
            spawnRot,
            player
        );

        _spawnedPlayers[player] = spawnedObj;
        Debug.Log($"[NetworkPlayerSpawner] Spawned player {player} at {spawnPos}");
    }

    // ── Required stubs ───────────────────────────────────────────────────────────
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => request.Accept();
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
