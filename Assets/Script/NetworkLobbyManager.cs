using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

/// <summary>
/// Networked lobby data manager.
/// Handles syncing player ready states and lobby information.
/// Attach to a NetworkObject that exists in the lobby scene.
/// </summary>
public class NetworkLobbyManager : NetworkBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }
    
    // Max 8 players
    [Networked, Capacity(8)] 
    public NetworkArray<PlayerLobbyInfo> Players => default;
    
    [Networked]
    public int PlayerCount { get; set; }
    
    [Networked]
    public NetworkString<_32> RoomName { get; set; }
    
    [Networked]
    public NetworkString<_16> Difficulty { get; set; }
    
    [Networked]
    public NetworkString<_16> Map { get; set; }
    
    [Networked]
    public NetworkBool GameStarting { get; set; }
    
    // Events
    public event Action OnLobbyUpdated;
    public event Action OnGameStart;
    
    public override void Spawned()
    {
        Instance = this;
        
        if (Object.HasStateAuthority)
        {
            Difficulty = "Normal";
            Map = "City";
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }
    
    public override void FixedUpdateNetwork()
    {
        if (GameStarting && Object.HasStateAuthority)
        {
            // Start game after a short delay
            StartGame();
        }
    }
    
    public override void Render()
    {
        // Notify UI of changes
        OnLobbyUpdated?.Invoke();
    }
    
    #region Host Methods
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateRoomSettings(NetworkString<_16> difficulty, NetworkString<_16> map)
    {
        if (!Object.HasStateAuthority) return;
        
        Difficulty = difficulty;
        Map = map;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        GameStarting = true;
        OnGameStart?.Invoke();
    }
    
    private void StartGame()
    {
        if (!Object.HasStateAuthority) return;
        
        // Get the game scene based on map selection
        string sceneName = GetSceneForMap(Map.ToString());

        if (Runner != null && Runner.IsRunning)
        {
            Runner.LoadScene(sceneName, LoadSceneMode.Single, LocalPhysicsMode.None, true);
        }
    }
    
    private string GetSceneForMap(string map)
    {
        // Map selection to scene names
        return map switch
        {
            "Forest" => "ForestScene",
            "Hospital" => "HospitalScene",
            "Factory" => "FactoryScene",
            _ => "GameScene" // Default city map
        };
    }
    
    #endregion
    
    #region Player Methods
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_JoinLobby(PlayerRef player, NetworkString<_32> playerName)
    {
        if (!Object.HasStateAuthority) return;
        
        // Find empty slot
        for (int i = 0; i < Players.Length; i++)
        {
            if (!Players[i].IsActive)
            {
                var info = new PlayerLobbyInfo
                {
                    PlayerId = player,
                    PlayerName = playerName,
                    IsReady = false,
                    IsHost = player == Runner.LocalPlayer,
                    IsActive = true
                };
                Players.Set(i, info);
                PlayerCount++;
                break;
            }
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_LeaveLobby(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i].PlayerId == player)
            {
                Players.Set(i, default);
                PlayerCount--;
                break;
            }
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(PlayerRef player, NetworkBool isReady)
    {
        if (!Object.HasStateAuthority) return;
        
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i].PlayerId == player && Players[i].IsActive)
            {
                var info = Players[i];
                info.IsReady = isReady;
                Players.Set(i, info);
                break;
            }
        }
    }
    
    #endregion
    
    #region Query Methods
    
    public List<PlayerLobbyInfo> GetAllPlayers()
    {
        var list = new List<PlayerLobbyInfo>();
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i].IsActive)
                list.Add(Players[i]);
        }
        return list;
    }
    
    public bool AreAllPlayersReady()
    {
        int readyCount = 0;
        int totalCount = 0;
        
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i].IsActive)
            {
                totalCount++;
                if (Players[i].IsReady || Players[i].IsHost)
                    readyCount++;
            }
        }
        
        return totalCount >= 2 && readyCount == totalCount;
    }
    
    public PlayerLobbyInfo? GetPlayerInfo(PlayerRef player)
    {
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i].PlayerId == player && Players[i].IsActive)
                return Players[i];
        }
        return null;
    }
    
    #endregion
}

/// <summary>
/// Network struct for storing player lobby information.
/// </summary>
[System.Serializable]
public struct PlayerLobbyInfo : INetworkStruct
{
    public PlayerRef PlayerId;
    public NetworkString<_32> PlayerName;
    public NetworkBool IsReady;
    public NetworkBool IsHost;
    public NetworkBool IsActive;
}
