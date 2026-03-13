using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Input data structure for network synchronization.
/// Used by Photon Fusion to sync player inputs.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    // Movement
    public Vector2 MoveDirection;
    public Vector2 LookDelta;
    
    // Actions
    public NetworkButtons Buttons;
    
    // Button indices
    public const int BUTTON_FIRE = 0;
    public const int BUTTON_RELOAD = 1;
    public const int BUTTON_JUMP = 2;
    public const int BUTTON_SPRINT = 3;
    public const int BUTTON_AIM = 4;
    public const int BUTTON_INTERACT = 5;
}

/// <summary>
/// Component that captures local input and provides it to the network.
/// Attach to player prefab or a persistent game object.
/// </summary>
public class NetworkInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Input Settings")]
    [SerializeField] private float lookSensitivity = 1f;
    
    private NetworkRunner _runner;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _firePressed;
    private bool _reloadPressed;
    private bool _jumpPressed;
    private bool _sprintPressed;
    private bool _aimPressed;
    private bool _interactPressed;
    
    void Update()
    {
        // Capture input every frame
        CaptureInput();
    }
    
    private void CaptureInput()
    {
        // Movement input
        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        
        // Look input (mouse or touch)
        _lookInput = new Vector2(
            Input.GetAxis("Mouse X") * lookSensitivity,
            Input.GetAxis("Mouse Y") * lookSensitivity
        );
        
        // Button inputs
        _firePressed = Input.GetButton("Fire1");
        _reloadPressed = Input.GetButtonDown("Reload") || Input.GetKeyDown(KeyCode.R);
        _jumpPressed = Input.GetButtonDown("Jump");
        _sprintPressed = Input.GetKey(KeyCode.LeftShift);
        _aimPressed = Input.GetButton("Fire2");
        _interactPressed = Input.GetKeyDown(KeyCode.E);
    }
    
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        
        data.MoveDirection = _moveInput;
        data.LookDelta = _lookInput;
        
        // Set buttons
        data.Buttons.Set(NetworkInputData.BUTTON_FIRE, _firePressed);
        data.Buttons.Set(NetworkInputData.BUTTON_RELOAD, _reloadPressed);
        data.Buttons.Set(NetworkInputData.BUTTON_JUMP, _jumpPressed);
        data.Buttons.Set(NetworkInputData.BUTTON_SPRINT, _sprintPressed);
        data.Buttons.Set(NetworkInputData.BUTTON_AIM, _aimPressed);
        data.Buttons.Set(NetworkInputData.BUTTON_INTERACT, _interactPressed);
        
        input.Set(data);
        
        // Reset one-shot inputs
        _reloadPressed = false;
        _jumpPressed = false;
        _interactPressed = false;
    }
    
    #region INetworkRunnerCallbacks (Required stubs)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}
