using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Quản lý việc đồng bộ restart giữa các players trong chế độ online.
/// Sử dụng Fusion RPC để thông báo và kiểm tra trạng thái sẵn sàng restart.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkRestartManager : NetworkBehaviour
{
    public static NetworkRestartManager Instance { get; private set; }

    // Events cho UI
    public event Action OnAllPlayersReady;
    public event Action<int, int> OnPlayerCountUpdate; // (readyCount, totalCount)

    // Networked dictionary để theo dõi ai đã sẵn sàng restart
    [Networked, Capacity(8)]
    private NetworkDictionary<PlayerRef, NetworkBool> PlayersReadyToRestart => default;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Instance == null)
        {
            Instance = this;
        }

        // Reset trạng thái khi spawned (chỉ State Authority)
        if (Object != null && Object.HasStateAuthority)
        {
            PlayersReadyToRestart.Clear();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Gọi khi player local bấm restart
    /// </summary>
    public void RequestRestart()
    {
        if (Runner == null || !Runner.IsRunning) return;
        RPC_RequestRestart(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRestart(PlayerRef player)
    {
        if (!PlayersReadyToRestart.ContainsKey(player))
        {
            PlayersReadyToRestart.Add(player, true);
        }
        else
        {
            PlayersReadyToRestart.Set(player, true);
        }

        int readyCount = GetReadyCount();
        int totalCount = GetTotalPlayerCount();
        Debug.Log($"Player {player} is ready to restart. Total ready: {readyCount}/{totalCount}");

        RPC_UpdateRestartStatus(readyCount, totalCount);

        if (CheckAllPlayersReady())
        {
            RPC_TriggerRestart();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateRestartStatus(int readyCount, int totalCount)
    {
        OnPlayerCountUpdate?.Invoke(readyCount, totalCount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerRestart()
    {
        Debug.Log("All players ready - triggering restart!");
        OnAllPlayersReady?.Invoke();
    }

    private bool CheckAllPlayersReady()
    {
        int totalPlayers = GetTotalPlayerCount();
        int readyPlayers = GetReadyCount();
        return totalPlayers > 0 && readyPlayers >= totalPlayers;
    }

    private int GetReadyCount()
    {
        int count = 0;
        foreach (var kvp in PlayersReadyToRestart)
        {
            if (kvp.Value)
            {
                count++;
            }
        }
        return count;
    }

    private int GetTotalPlayerCount()
    {
        if (Runner == null) return 0;

        int count = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Reset trạng thái restart
    /// </summary>
    public void ResetRestartState()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            PlayersReadyToRestart.Clear();
        }
    }

    /// <summary>
    /// Xóa player khỏi danh sách khi họ disconnect
    /// </summary>
    public void RemovePlayer(PlayerRef player)
    {
        if (Object != null && Object.HasStateAuthority && PlayersReadyToRestart.ContainsKey(player))
        {
            PlayersReadyToRestart.Remove(player);

            int readyCount = GetReadyCount();
            int totalCount = GetTotalPlayerCount();
            RPC_UpdateRestartStatus(readyCount, totalCount);

            if (CheckAllPlayersReady())
            {
                RPC_TriggerRestart();
            }
        }
    }
}
