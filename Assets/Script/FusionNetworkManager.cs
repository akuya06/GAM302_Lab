using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Manages Photon Fusion networking for multiplayer zombie game.
/// Handles room creation, joining, and session management.
/// </summary>
public class FusionNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionNetworkManager Instance { get; private set; }

    private static readonly ReliableKey LobbyReadyReliableKey = ReliableKey.FromInts(24013, 1, 0, 0);
    
    [Header("Network Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    [Header("Room Settings")]
    [SerializeField] private int defaultMaxPlayers = 4;
    
    // Current state
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;
    private bool _isCleaningUp;
    private string _playerName = "Player";
    private RoomInfo _currentRoom;
    private readonly Dictionary<PlayerRef, bool> _lobbyReadyStates = new Dictionary<PlayerRef, bool>();
    
    // Events for UI updates
    public event Action<string> OnStatusChanged;
    public event Action<string> OnError;
    public event Action<List<SessionInfo>> OnRoomListUpdated;
    public event Action OnJoinedRoom;
    public event Action OnLeftRoom;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
    public event Action<PlayerRef> OnPlayerLeftEvent;
    public event Action<PlayerRef, bool> OnPlayerReadyStateChanged;
    public event Action OnGameStarting;
    
    // Current room info
    public bool IsConnected => _runner != null && _runner.IsRunning;
    public bool IsHost => _runner != null && _runner.IsServer;
    public string CurrentRoomName => _currentRoom?.RoomName ?? "";
    public string CurrentRoomId => _currentRoom?.RoomId ?? "";
    public int PlayerCount => _runner?.ActivePlayers.Count() ?? 0;
    public int MaxPlayers => _currentRoom?.MaxPlayers ?? defaultMaxPlayers;

    public IEnumerable<PlayerRef> ActivePlayers => _runner?.ActivePlayers ?? Enumerable.Empty<PlayerRef>();
    
    [Serializable]
    public class RoomInfo
    {
        public string RoomName;
        public string RoomId;
        public string Password;
        public int MaxPlayers;
        public bool IsPrivate;
        public string Difficulty;
        public string Map;
    }
    
    void Awake()
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
        
        // Load player name
        _playerName = PlayerPrefs.GetString("PlayerName", "Player" + UnityEngine.Random.Range(1000, 9999));
    }
    
    public void SetPlayerName(string name)
    {
        _playerName = name;
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }
    
    public string GetPlayerName() => _playerName;

    private void SafeInvoke(Action callback, string callbackName)
    {
        if (callback == null)
            return;

        foreach (Delegate handler in callback.GetInvocationList())
        {
            try
            {
                ((Action)handler).Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FusionNetworkManager] Callback '{callbackName}' failed: {ex}");
            }
        }
    }

    private NetworkRunner CreateRunner()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

            // Register NetworkInputProvider if it exists in the scene
            var inputProvider = FindFirstObjectByType<NetworkInputProvider>();
            if (inputProvider != null)
                _runner.AddCallbacks(inputProvider);

        return _runner;
    }

    private INetworkSceneManager GetOrCreateSceneManager()
    {
        if (_sceneManager == null)
        {
            _sceneManager = GetComponent<NetworkSceneManagerDefault>();
            if (_sceneManager == null)
            {
                _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
        }

        return _sceneManager;
    }
    
    private string NormalizeSessionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Creates a new room with the specified settings.
    /// </summary>
    public async Task<bool> CreateRoom(string roomName, string password, int maxPlayers, bool isPrivate)
    {
        if (_runner != null)
        {
            await Disconnect();
        }
        
        OnStatusChanged?.Invoke("Creating room...");
        
        try
        {
            CreateRunner();
            
            string roomId = GenerateRoomId();
            string displayRoomName = string.IsNullOrWhiteSpace(roomName) ? "Room_" + roomId : roomName.Trim();
            string sessionName = NormalizeSessionName(displayRoomName);
            
            _currentRoom = new RoomInfo
            {
                RoomName = displayRoomName,
                RoomId = sessionName,
                Password = password,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                Difficulty = "Normal",
                Map = "City"
            };
            
            var sceneInfo = new NetworkSceneInfo();
            
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                SceneManager = GetOrCreateSceneManager(),
                SessionProperties = CreateSessionProperties(),
            });
            
            if (result.Ok)
            {
                OnStatusChanged?.Invoke("Room created successfully!");
                SafeInvoke(OnJoinedRoom, nameof(OnJoinedRoom));
                return true;
            }
            else
            {
                OnError?.Invoke($"Failed to create room: {result.ShutdownReason}");
                CleanupRunner();
                return false;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Error creating room: {e.Message}");
            CleanupRunner();
            return false;
        }
    }
    
    /// <summary>
    /// Joins an existing room by ID or name.
    /// </summary>
    public async Task<bool> JoinRoom(string roomId, string password)
    {
        string roomIdentifier = NormalizeSessionName(roomId);
        if (string.IsNullOrWhiteSpace(roomIdentifier))
        {
            OnError?.Invoke("Room ID or room name is required.");
            return false;
        }

        if (_runner != null)
        {
            await Disconnect();
        }
        
        OnStatusChanged?.Invoke("Joining room...");
        
        try
        {
            CreateRunner();
            
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = roomIdentifier,
                SceneManager = GetOrCreateSceneManager(),
            });
            
            if (result.Ok)
            {
                _currentRoom = new RoomInfo
                {
                    RoomId = roomIdentifier,
                    RoomName = roomIdentifier,
                    Password = password
                };
                
                OnStatusChanged?.Invoke("Joined room successfully!");
                SafeInvoke(OnJoinedRoom, nameof(OnJoinedRoom));
                return true;
            }
            else
            {
                OnError?.Invoke($"Failed to join room: {result.ShutdownReason}");
                CleanupRunner();
                return false;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Error joining room: {e.Message}");
            CleanupRunner();
            return false;
        }
    }
    
    /// <summary>
    /// Quick match - joins any available room or creates a new one.
    /// </summary>
    public async Task<bool> QuickMatch()
    {
        if (_runner != null)
        {
            await Disconnect();
        }
        
        OnStatusChanged?.Invoke("Finding match...");
        
        try
        {
            CreateRunner();
            
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "QuickMatch_" + UnityEngine.Random.Range(1, 100),
                PlayerCount = defaultMaxPlayers,
                SceneManager = GetOrCreateSceneManager(),
            });
            
            if (result.Ok)
            {
                _currentRoom = new RoomInfo
                {
                    RoomId = "QuickMatch",
                    RoomName = "Quick Match",
                    MaxPlayers = defaultMaxPlayers
                };
                
                OnStatusChanged?.Invoke("Match found!");
                SafeInvoke(OnJoinedRoom, nameof(OnJoinedRoom));
                return true;
            }
            else
            {
                OnError?.Invoke($"Failed to find match: {result.ShutdownReason}");
                CleanupRunner();
                return false;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Error finding match: {e.Message}");
            CleanupRunner();
            return false;
        }
    }
    
    /// <summary>
    /// Disconnects from the current session.
    /// </summary>
    public async Task Disconnect()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
            CleanupRunner();
        }
        
        _currentRoom = null;
        OnLeftRoom?.Invoke();
    }
    
    private void CleanupRunner()
    {
        if (_isCleaningUp)
            return;

        _isCleaningUp = true;

        if (_runner != null)
        {
            Destroy(_runner);
            _runner = null;
        }

        if (_sceneManager != null)
        {
            Destroy(_sceneManager);
            _sceneManager = null;
        }

        _lobbyReadyStates.Clear();

        _isCleaningUp = false;
    }
    
    /// <summary>
    /// Starts the game (host only).
    /// </summary>
    public void StartGame()
    {
        if (!IsHost)
        {
            OnError?.Invoke("Only the host can start the game!");
            return;
        }

        if (!CanStartGame())
        {
            OnError?.Invoke("All non-host players must be ready before starting.");
            return;
        }
        
        OnGameStarting?.Invoke();
        _runner.LoadScene(gameSceneName, LoadSceneMode.Single, LocalPhysicsMode.None, true);
    }

    public void SetLocalReadyState(bool isReady)
    {
        if (_runner == null || !_runner.IsRunning)
            return;

        if (IsHost)
        {
            SetReadyState(_runner.LocalPlayer, isReady);
            return;
        }

        try
        {
            _runner.SendReliableDataToServer(LobbyReadyReliableKey, new[] { (byte)(isReady ? 1 : 0) });
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FusionNetworkManager] Failed to send ready state: {ex.Message}");
        }
    }

    public bool IsPlayerReady(PlayerRef player)
    {
        if (_lobbyReadyStates.TryGetValue(player, out bool isReady))
            return isReady;

        return false;
    }

    public bool CanStartGame()
    {
        if (!IsHost || _runner == null)
            return false;

        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2)
            return false;

        foreach (var player in players)
        {
            if (player == _runner.LocalPlayer)
                continue;

            if (!IsPlayerReady(player))
                return false;
        }

        return true;
    }

    public bool TryGetLocalPlayer(out PlayerRef player)
    {
        if (_runner != null && _runner.IsRunning)
        {
            player = _runner.LocalPlayer;
            return true;
        }

        player = default;
        return false;
    }

    private void SetReadyState(PlayerRef player, bool isReady)
    {
        if (_lobbyReadyStates.TryGetValue(player, out bool previous) && previous == isReady)
            return;

        _lobbyReadyStates[player] = isReady;
        OnPlayerReadyStateChanged?.Invoke(player, isReady);
    }
    
    public void SetGameSettings(string difficulty, string map)
    {
        if (_currentRoom != null)
        {
            _currentRoom.Difficulty = difficulty;
            _currentRoom.Map = map;
        }
    }
    
    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] id = new char[6];
        for (int i = 0; i < 6; i++)
        {
            id[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return new string(id);
    }
    
    private Dictionary<string, SessionProperty> CreateSessionProperties()
    {
        return new Dictionary<string, SessionProperty>
        {
            { "RoomName", _currentRoom.RoomName },
            { "HasPassword", !string.IsNullOrEmpty(_currentRoom.Password) },
            { "IsPrivate", _currentRoom.IsPrivate },
            { "Difficulty", _currentRoom.Difficulty ?? "Normal" },
            { "Map", _currentRoom.Map ?? "City" }
        };
    }
    
    #region INetworkRunnerCallbacks
    
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} joined");

        if (runner.IsServer)
        {
            bool defaultReady = player == runner.LocalPlayer;
            SetReadyState(player, defaultReady);
        }

        OnPlayerJoinedEvent?.Invoke(player);
    }
    
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left");

        if (_lobbyReadyStates.Remove(player))
        {
            OnPlayerReadyStateChanged?.Invoke(player, false);
        }

        OnPlayerLeftEvent?.Invoke(player);

        if (NetworkRestartManager.Instance != null)
        {
            NetworkRestartManager.Instance.RemovePlayer(player);
        }
    }
    
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Input handling will be done in NetworkPlayerInput component
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        try
        {
            Debug.Log($"Network shutdown: {shutdownReason}");
            CleanupRunner();
            SafeInvoke(OnLeftRoom, nameof(OnLeftRoom));
            
            if (shutdownReason != ShutdownReason.Ok)
            {
                OnError?.Invoke($"Disconnected: {shutdownReason}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling network shutdown: {e.Message}");
        }
    }
    
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server");
        OnStatusChanged?.Invoke("Connected!");
    }
    
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
        try
        {
            // When disconnected from server, cleanup and notify UI to return to menu
            CleanupRunner();
            OnError?.Invoke($"Disconnected: {reason}");
            SafeInvoke(OnLeftRoom, nameof(OnLeftRoom));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling server disconnect: {e.Message}");
        }
    }
    
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Accept all connections for now
        // Can add password validation here
        request.Accept();
    }
    
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"Connect failed: {reason}");
        OnError?.Invoke($"Connection failed: {reason}");
    }
    
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        OnRoomListUpdated?.Invoke(sessionList);
    }
    
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        OnGameStarting?.Invoke();
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (!runner.IsServer || key != LobbyReadyReliableKey || data.Count < 1 || data.Array == null)
            return;

        bool isReady = data.Array[data.Offset] != 0;
        SetReadyState(player, isReady);
    }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    
    #endregion
}
