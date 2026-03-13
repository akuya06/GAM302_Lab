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
        _isLocalPlayer = Object.HasInputAuthority;
        
        if (_isLocalPlayer)
        {
            // Set player name from local settings
            string localName = PlayerPrefs.GetString("PlayerName", $"Player{Random.Range(1000, 9999)}");
            RPC_SetPlayerName(localName);
            
            // Enable local player components
            EnableLocalComponents();
        }
        else
        {
            // Disable local-only components for remote players
            DisableLocalComponents();
        }
        
        // Register with game manager
        GameManager.Instance?.RegisterNetworkPlayer(this);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GameManager.Instance?.UnregisterNetworkPlayer(this);
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        PlayerName = name;
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
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
    
    private void EnableLocalComponents()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;
        
        // Enable camera for local player
        var cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
            cam.gameObject.SetActive(true);
        
        // Enable audio listener
        var listener = GetComponentInChildren<AudioListener>(true);
        if (listener != null)
            listener.enabled = true;
    }
    
    private void DisableLocalComponents()
    {
        // Disable camera for remote players
        var cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
            cam.gameObject.SetActive(false);
        
        // Disable audio listener for remote players
        var listener = GetComponentInChildren<AudioListener>(true);
        if (listener != null)
            listener.enabled = false;
    }
    
    public bool IsLocalPlayer => _isLocalPlayer;
}
