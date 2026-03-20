using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using Fusion.Sockets;

/// <summary>
/// Listens to player join events and spawns a player prefab for them.
/// This should be placed on a GameObject in the scene that will be loaded.
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")]
    [SerializeField] private NetworkObject _playerPrefab;

    // Dictionary to track spawned players.
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Start()
    {
        // It's good practice to find the runner and add callbacks here.
        // This handles cases where the spawner is enabled before the runner is ready.
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
        else
        {
            Debug.LogError("NetworkRunner not found in scene. Player Spawner will not work.");
        }
    }

    /// <summary>
    /// This is the most important callback for spawning. It's called on all clients
    /// when a new player joins, including the host and the joining player.
    /// </summary>
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // In Shared Mode, each client is responsible for spawning its own player object.
        if (player == runner.LocalPlayer)
        {
            Debug.Log($"OnPlayerJoined called for local player {player}. Spawning local player object.");
            SpawnPlayer(runner, player);
        }
    }

    /// <summary>
    /// When a scene loads, the master client needs to spawn players for everyone
    /// who is already in the session.
    /// </summary>
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.LocalPlayer != PlayerRef.None)
        {
            Debug.Log($"Scene loaded. Ensuring local player object exists for {runner.LocalPlayer}.");
            SpawnPlayer(runner, runner.LocalPlayer);
        }
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer)
        {
            return;
        }

        // Nếu đã có player object trong runner thì không spawn lại.
        var existingPlayerObject = runner.GetPlayerObject(player);
        if (existingPlayerObject != null)
        {
            if (!_spawnedPlayers.ContainsKey(player))
            {
                _spawnedPlayers[player] = existingPlayerObject;
            }
            return;
        }

        // Check if the player has already been spawned.
        if (_spawnedPlayers.ContainsKey(player))
        {
            return;
        }

        // Get a random spawn position.
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-3, 3), 1, UnityEngine.Random.Range(-3, 3));

        // Spawn the local player's prefab with input authority assigned to this player.
        NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

        // Runner should automatically assign the LocalPlayerObject.

        // Keep track of the spawned player.
        _spawnedPlayers.Add(player, networkPlayerObject);
        Debug.Log($"Spawned player {player.PlayerId} at {spawnPosition}");
    }

    /// <summary>
    /// When a player leaves, the master client despawns their object.
    /// </summary>
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null && networkObject.HasStateAuthority)
            {
                runner.Despawn(networkObject);
            }

            _spawnedPlayers.Remove(player);
            Debug.Log($"Player {player.PlayerId} left. Removed tracked player object.");
        }

        if (player == runner.LocalPlayer)
        {
            runner.SetPlayerObject(player, null);
        }
    }

    // --- Unused Callbacks ---
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
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
