using UnityEngine;
using Fusion;

/// <summary>
/// Network player component for syncing player data in multiplayer.
/// Attach to player prefab for networked gameplay.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int Score { get; set; }
    [Networked] public int Kills { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    
    // Local references
    private bool _isLocalPlayer;
    
    public override void Spawned()
    {
        _isLocalPlayer = Object.HasStateAuthority;
        
        if (_isLocalPlayer)
        {
            // Set player name from local settings
            string localName = PlayerPrefs.GetString("PlayerName", $"Player{Random.Range(1000, 9999)}");
            PlayerName = localName;
        }
        
        // Register with game manager
        GameManager.Instance?.RegisterNetworkPlayer(this);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GameManager.Instance?.UnregisterNetworkPlayer(this);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddKill()
    {
        Kills++;
        Score += 100;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TakeDamage(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }
    
    public bool IsLocalPlayer => _isLocalPlayer;
}
