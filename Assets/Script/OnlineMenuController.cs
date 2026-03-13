using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Fusion;
using System;

/// <summary>
/// Controller for the complete online multiplayer menu system.
/// Handles all UI panels including main menu, create room, join room, lobby, and settings.
/// </summary>
public class OnlineMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    
    // Panels
    private VisualElement mainMenuPanel;
    private VisualElement onlineMenuPanel;
    private VisualElement createRoomPanel;
    private VisualElement joinRoomPanel;
    private VisualElement roomBrowserPanel;
    private VisualElement lobbyPanel;
    private VisualElement connectingPanel;
    private VisualElement settingsPanel;
    private VisualElement errorPopup;
    
    // Main Menu Elements
    private Button playOnlineButton;
    private Button soloModeButton;
    private Button settingsButton;
    private Button quitButton;
    
    // Online Menu Elements
    private Button createRoomButton;
    private Button joinRoomButton;
    private Button quickMatchButton;
    private Button onlineBackButton;
    
    // Create Room Elements
    private TextField createRoomNameInput;
    private SliderInt maxPlayersSlider;
    private Label maxPlayersLabel;
    private Toggle passwordToggle;
    private VisualElement passwordInputGroup;
    private TextField createRoomPasswordInput;
    private Toggle privateRoomToggle;
    private Button confirmCreateRoomButton;
    private Button createRoomBackButton;
    private Label createRoomStatusLabel;
    
    // Join Room Elements
    private TextField joinRoomIdInput;
    private TextField joinRoomPasswordInput;
    private Button confirmJoinRoomButton;
    private Button joinRoomBackButton;
    private Label joinRoomStatusLabel;
    
    // Room Browser Elements
    private TextField searchRoomInput;
    private Button refreshRoomsButton;
    private VisualElement roomListContainer;
    private Button roomBrowserBackButton;
    
    // Lobby Elements
    private Label lobbyRoomName;
    private Label lobbyRoomId;
    private VisualElement playerListContainer;
    private Label playerCountLabel;
    private Label roomStatusLabel;
    private VisualElement hostControls;
    private DropdownField difficultyDropdown;
    private DropdownField mapDropdown;
    private Button leaveLobbyButton;
    private Button readyButton;
    private Button startGameButton;
    
    // Connecting Elements
    private Label connectingStatusLabel;
    private Button cancelConnectButton;
    
    // Settings Elements
    private TextField playerNameInput;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Slider sensitivitySlider;
    private Toggle vibrationToggle;
    private Button settingsBackButton;
    private Button saveSettingsButton;
    
    // Error Popup Elements
    private Label errorTitle;
    private Label errorMessage;
    private Button errorOkButton;
    
    [Header("Background Sprites")]
    [SerializeField] private Sprite mainMenuBackgroundSprite;
    [SerializeField] private Sprite pauseButtonNormalSprite;
    
    [Header("Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    // State
    private bool isReady = false;
    private FusionNetworkManager networkManager;
    private Dictionary<PlayerRef, PlayerLobbyData> playersInLobby = new Dictionary<PlayerRef, PlayerLobbyData>();
    
    [System.Serializable]
    public class PlayerLobbyData
    {
        public string Name;
        public bool IsReady;
        public bool IsHost;
    }
    
    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        EnsureNetworkManager();
        
        // Subscribe to network events
        SubscribeToNetworkEvents();
        
        // Get all panels
        GetPanelReferences();
        
        // Get all UI elements
        GetUIReferences();
        
        // Setup callbacks
        SetupCallbacks();
        
        // Apply styles
        ApplyBackgroundAndStyles();
        
        // Apply safe area
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        
        // Load settings
        LoadSettings();
        
        // Show main menu
        ShowPanel(mainMenuPanel);
        
        // Unlock cursor
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void EnsureNetworkManager()
    {
        networkManager = FusionNetworkManager.Instance;

        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<FusionNetworkManager>();
        }

        if (networkManager == null)
        {
            Debug.LogWarning("FusionNetworkManager not found in scene. Creating one...");
            var go = new GameObject("FusionNetworkManager");
            networkManager = go.AddComponent<FusionNetworkManager>();
        }
    }
    
    void OnDisable()
    {
        UnsubscribeFromNetworkEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
    
    private void SubscribeToNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnStatusChanged += OnNetworkStatus;
            networkManager.OnError += OnNetworkError;
            networkManager.OnJoinedRoom += OnJoinedRoom;
            networkManager.OnLeftRoom += OnLeftRoom;
            networkManager.OnPlayerJoinedEvent += OnPlayerJoinedRoom;
            networkManager.OnPlayerLeftEvent += OnPlayerLeftRoom;
            networkManager.OnPlayerReadyStateChanged += OnPlayerReadyStateChanged;
            networkManager.OnGameStarting += OnGameStarting;
            networkManager.OnRoomListUpdated += OnRoomListReceived;
        }
    }
    
    private void UnsubscribeFromNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnStatusChanged -= OnNetworkStatus;
            networkManager.OnError -= OnNetworkError;
            networkManager.OnJoinedRoom -= OnJoinedRoom;
            networkManager.OnLeftRoom -= OnLeftRoom;
            networkManager.OnPlayerJoinedEvent -= OnPlayerJoinedRoom;
            networkManager.OnPlayerLeftEvent -= OnPlayerLeftRoom;
            networkManager.OnPlayerReadyStateChanged -= OnPlayerReadyStateChanged;
            networkManager.OnGameStarting -= OnGameStarting;
            networkManager.OnRoomListUpdated -= OnRoomListReceived;
        }
    }
    
    private void GetPanelReferences()
    {
        mainMenuPanel = root.Q<VisualElement>("MainMenuPanel");
        onlineMenuPanel = root.Q<VisualElement>("OnlineMenuPanel");
        createRoomPanel = root.Q<VisualElement>("CreateRoomPanel");
        joinRoomPanel = root.Q<VisualElement>("JoinRoomPanel");
        roomBrowserPanel = root.Q<VisualElement>("RoomBrowserPanel");
        lobbyPanel = root.Q<VisualElement>("LobbyPanel");
        connectingPanel = root.Q<VisualElement>("ConnectingPanel");
        settingsPanel = root.Q<VisualElement>("SettingsPanel");
        errorPopup = root.Q<VisualElement>("ErrorPopup");
    }
    
    private void GetUIReferences()
    {
        // Main Menu
        playOnlineButton = root.Q<Button>("PlayOnlineButton");
        soloModeButton = root.Q<Button>("SoloModeButton");
        settingsButton = root.Q<Button>("SettingsButton");
        quitButton = root.Q<Button>("QuitButton");
        
        // Online Menu
        createRoomButton = root.Q<Button>("CreateRoomButton");
        joinRoomButton = root.Q<Button>("JoinRoomButton");
        quickMatchButton = root.Q<Button>("QuickMatchButton");
        onlineBackButton = root.Q<Button>("OnlineBackButton");
        
        // Create Room
        createRoomNameInput = root.Q<TextField>("CreateRoomNameInput");
        maxPlayersSlider = root.Q<SliderInt>("MaxPlayersSlider");
        maxPlayersLabel = root.Q<Label>("MaxPlayersLabel");
        passwordToggle = root.Q<Toggle>("PasswordToggle");
        passwordInputGroup = root.Q<VisualElement>("PasswordInputGroup");
        createRoomPasswordInput = root.Q<TextField>("CreateRoomPasswordInput");
        privateRoomToggle = root.Q<Toggle>("PrivateRoomToggle");
        confirmCreateRoomButton = root.Q<Button>("ConfirmCreateRoomButton");
        createRoomBackButton = root.Q<Button>("CreateRoomBackButton");
        createRoomStatusLabel = root.Q<Label>("CreateRoomStatusLabel");
        
        // Join Room
        joinRoomIdInput = root.Q<TextField>("JoinRoomIdInput");
        joinRoomPasswordInput = root.Q<TextField>("JoinRoomPasswordInput");
        confirmJoinRoomButton = root.Q<Button>("ConfirmJoinRoomButton");
        joinRoomBackButton = root.Q<Button>("JoinRoomBackButton");
        joinRoomStatusLabel = root.Q<Label>("JoinRoomStatusLabel");
        
        // Room Browser
        searchRoomInput = root.Q<TextField>("SearchRoomInput");
        refreshRoomsButton = root.Q<Button>("RefreshRoomsButton");
        roomListContainer = root.Q<VisualElement>("RoomListContainer");
        roomBrowserBackButton = root.Q<Button>("RoomBrowserBackButton");
        
        // Lobby
        lobbyRoomName = root.Q<Label>("LobbyRoomName");
        lobbyRoomId = root.Q<Label>("LobbyRoomId");
        playerListContainer = root.Q<VisualElement>("PlayerListContainer");
        playerCountLabel = root.Q<Label>("PlayerCountLabel");
        roomStatusLabel = root.Q<Label>("RoomStatusLabel");
        hostControls = root.Q<VisualElement>("HostControls");
        difficultyDropdown = root.Q<DropdownField>("DifficultyDropdown");
        mapDropdown = root.Q<DropdownField>("MapDropdown");
        leaveLobbyButton = root.Q<Button>("LeaveLobbyButton");
        readyButton = root.Q<Button>("ReadyButton");
        startGameButton = root.Q<Button>("StartGameButton");
        if (startGameButton != null)
        {
            startGameButton.text = "Start Game";
        }
        
        // Connecting
        connectingStatusLabel = root.Q<Label>("ConnectingStatusLabel");
        cancelConnectButton = root.Q<Button>("CancelConnectButton");
        
        // Settings
        playerNameInput = root.Q<TextField>("PlayerNameInput");
        musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");
        sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        vibrationToggle = root.Q<Toggle>("VibrationToggle");
        settingsBackButton = root.Q<Button>("SettingsBackButton");
        saveSettingsButton = root.Q<Button>("SaveSettingsButton");
        
        // Error Popup
        errorTitle = root.Q<Label>("ErrorTitle");
        errorMessage = root.Q<Label>("ErrorMessage");
        errorOkButton = root.Q<Button>("ErrorOkButton");
    }
    
    private void SetupCallbacks()
    {
        // Main Menu
        playOnlineButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(onlineMenuPanel));
        soloModeButton?.RegisterCallback<ClickEvent>(evt => StartSoloGame());
        settingsButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(settingsPanel));
        quitButton?.RegisterCallback<ClickEvent>(evt => QuitGame());
        
        // Online Menu
        createRoomButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(createRoomPanel));
        joinRoomButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(joinRoomPanel));
        quickMatchButton?.RegisterCallback<ClickEvent>(evt => QuickMatch());
        onlineBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(mainMenuPanel));
        
        // Create Room
        maxPlayersSlider?.RegisterValueChangedCallback(evt => {
            if (maxPlayersLabel != null) maxPlayersLabel.text = evt.newValue.ToString();
        });
        passwordToggle?.RegisterValueChangedCallback(evt => {
            SetElementVisible(passwordInputGroup, evt.newValue);
        });
        confirmCreateRoomButton?.RegisterCallback<ClickEvent>(evt => CreateRoom());
        createRoomBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(onlineMenuPanel));
        
        // Join Room
        confirmJoinRoomButton?.RegisterCallback<ClickEvent>(evt => JoinRoom());
        joinRoomBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(onlineMenuPanel));
        
        // Room Browser
        refreshRoomsButton?.RegisterCallback<ClickEvent>(evt => RefreshRoomList());
        roomBrowserBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(onlineMenuPanel));
        
        // Lobby
        leaveLobbyButton?.RegisterCallback<ClickEvent>(evt => LeaveLobby());
        readyButton?.RegisterCallback<ClickEvent>(evt => ToggleReady());
        startGameButton?.RegisterCallback<ClickEvent>(evt => StartGame());
        difficultyDropdown?.RegisterValueChangedCallback(evt => UpdateGameSettings());
        mapDropdown?.RegisterValueChangedCallback(evt => UpdateGameSettings());
        
        // Connecting
        cancelConnectButton?.RegisterCallback<ClickEvent>(evt => CancelConnection());
        
        // Settings
        settingsBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(mainMenuPanel));
        saveSettingsButton?.RegisterCallback<ClickEvent>(evt => SaveSettings());
        
        // Error Popup
        errorOkButton?.RegisterCallback<ClickEvent>(evt => HideErrorPopup());
    }
    
    private void ApplyBackgroundAndStyles()
    {
        var mainMenu = root.Q<VisualElement>("MainMenu");
        if (mainMenu != null && mainMenuBackgroundSprite != null)
        {
            mainMenu.style.backgroundImage = new StyleBackground(mainMenuBackgroundSprite);
        }
        
        // Apply button sprites
        ApplyButtonSprites();
    }
    
    private void ApplyButtonSprites()
    {
        if (pauseButtonNormalSprite == null) return;
        
        var allButtons = root.Query<Button>(className: "pause-button").ToList();
        foreach (var button in allButtons)
        {
            button.style.backgroundImage = new StyleBackground(pauseButtonNormalSprite);
        }
    }
    
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        ApplySafeArea();
    }
    
    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        
        if (screenWidth <= 0 || screenHeight <= 0) return;
        
        var leftPadding = safeArea.x / screenWidth * 100;
        var rightPadding = (screenWidth - safeArea.xMax) / screenWidth * 100;
        var topPadding = (screenHeight - safeArea.yMax) / screenHeight * 100;
        var bottomPadding = safeArea.y / screenHeight * 100;
        
        root.style.paddingLeft = new StyleLength(new Length(leftPadding, LengthUnit.Percent));
        root.style.paddingRight = new StyleLength(new Length(rightPadding, LengthUnit.Percent));
        root.style.paddingTop = new StyleLength(new Length(topPadding, LengthUnit.Percent));
        root.style.paddingBottom = new StyleLength(new Length(bottomPadding, LengthUnit.Percent));
    }
    
    #region Panel Navigation
    
    private void ShowPanel(VisualElement panel)
    {
        // Hide all panels
        HideAllPanels();
        
        // Show target panel
        if (panel != null)
        {
            panel.RemoveFromClassList("hidden");
        }
    }
    
    private void HideAllPanels()
    {
        mainMenuPanel?.AddToClassList("hidden");
        onlineMenuPanel?.AddToClassList("hidden");
        createRoomPanel?.AddToClassList("hidden");
        joinRoomPanel?.AddToClassList("hidden");
        roomBrowserPanel?.AddToClassList("hidden");
        lobbyPanel?.AddToClassList("hidden");
        connectingPanel?.AddToClassList("hidden");
        settingsPanel?.AddToClassList("hidden");
    }
    
    private void SetElementVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        
        if (visible)
            element.RemoveFromClassList("hidden");
        else
            element.AddToClassList("hidden");
    }
    
    #endregion
    
    #region Network Actions
    
    private async void CreateRoom()
    {
        string roomName = createRoomNameInput?.value ?? "";
        int maxPlayers = maxPlayersSlider?.value ?? 4;
        bool hasPassword = passwordToggle?.value ?? false;
        string password = hasPassword ? (createRoomPasswordInput?.value ?? "") : "";
        bool isPrivate = privateRoomToggle?.value ?? false;
        
        if (hasPassword && string.IsNullOrEmpty(password))
        {
            ShowError("Error", "Please enter a password or disable password protection.");
            return;
        }
        
        ShowPanel(connectingPanel);
        SetStatus(createRoomStatusLabel, "Creating room...");
        
        bool success = await networkManager.CreateRoom(roomName, password, maxPlayers, isPrivate);
        
        if (!success)
        {
            ShowPanel(createRoomPanel);
        }
    }
    
    private async void JoinRoom()
    {
        string roomId = joinRoomIdInput?.value ?? "";
        string password = joinRoomPasswordInput?.value ?? "";
        
        if (string.IsNullOrEmpty(roomId))
        {
            ShowError("Error", "Please enter a Room ID or Room Name.");
            return;
        }
        
        ShowPanel(connectingPanel);
        SetStatus(joinRoomStatusLabel, "Joining room...");
        
        bool success = await networkManager.JoinRoom(roomId, password);
        
        if (!success)
        {
            ShowPanel(joinRoomPanel);
        }
    }
    
    private async void QuickMatch()
    {
        ShowPanel(connectingPanel);
        connectingStatusLabel.text = "Finding match...";
        
        bool success = await networkManager.QuickMatch();
        
        if (!success)
        {
            ShowPanel(onlineMenuPanel);
        }
    }
    
    private async void LeaveLobby()
    {
        await networkManager.Disconnect();
        playersInLobby.Clear();
        isReady = false;
        ShowPanel(onlineMenuPanel);
    }
    
    private async void CancelConnection()
    {
        await networkManager.Disconnect();
        ShowPanel(onlineMenuPanel);
    }
    
    private void RefreshRoomList()
    {
        // Photon Fusion handles room list via callbacks
        SetStatus(null, "Refreshing...");
    }
    
    private void ToggleReady()
    {
        isReady = !isReady;
        readyButton.text = isReady ? "Not Ready" : "Ready";
        
        if (isReady)
            readyButton.AddToClassList("danger-button");
        else
            readyButton.RemoveFromClassList("danger-button");

        if (networkManager != null)
        {
            networkManager.SetLocalReadyState(isReady);
        }

        if (networkManager != null && networkManager.TryGetLocalPlayer(out var localPlayer) && playersInLobby.TryGetValue(localPlayer, out var localLobbyData))
        {
            localLobbyData.IsReady = isReady;
            playersInLobby[localPlayer] = localLobbyData;
        }
        
        UpdateLobbyUI();
    }
    
    private void StartGame()
    {
        if (!networkManager.IsHost)
        {
            ShowError("Error", "Only the host can start the game!");
            return;
        }

        if (networkManager.PlayerCount < 2)
        {
            ShowError("Not Enough Players", "Need at least 2 players in the room before starting the game.");
            return;
        }

        if (!networkManager.CanStartGame())
        {
            ShowError("Players Not Ready", "All non-host players must be ready before starting the game.");
            return;
        }
        
        networkManager.StartGame();
    }
    
    private void UpdateGameSettings()
    {
        if (networkManager.IsHost)
        {
            string difficulty = difficultyDropdown?.value ?? "Normal";
            string map = mapDropdown?.value ?? "City";
            networkManager.SetGameSettings(difficulty, map);
        }
    }
    
    #endregion
    
    #region Network Event Handlers
    
    private void OnNetworkStatus(string status)
    {
        if (connectingStatusLabel != null)
            connectingStatusLabel.text = status;
        
        Debug.Log($"Network Status: {status}");
    }
    
    private void OnNetworkError(string error)
    {
        ShowError("Connection Error", error);
    }
    
    private void OnJoinedRoom()
    {
        SetMenuVisible(true);

        // Update lobby UI
        if (lobbyRoomName != null)
            lobbyRoomName.text = $"Room: {networkManager.CurrentRoomName}";

        if (lobbyRoomId != null)
            lobbyRoomId.text = $"ID: {networkManager.CurrentRoomId}";
        
        // Show/hide host controls
        SetElementVisible(hostControls, networkManager.IsHost);
        SetElementVisible(startGameButton, networkManager.IsHost);
        SetElementVisible(readyButton, !networkManager.IsHost);
        
        // Reset ready state
        isReady = false;
        if (readyButton != null)
        {
            readyButton.text = "Ready";
            readyButton.RemoveFromClassList("danger-button");
        }

        SyncPlayersFromNetwork();
        
        UpdateLobbyUI();
        ShowPanel(lobbyPanel);
    }
    
    private void OnLeftRoom()
    {
        try
        {
            playersInLobby.Clear();
            isReady = false;
            
            // Return to online menu when left room (host disconnected or normal leave)
            ShowPanel(onlineMenuPanel);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling OnLeftRoom: {e.Message}");
        }
    }
    
    private void OnPlayerJoinedRoom(PlayerRef player)
    {
        if (!playersInLobby.ContainsKey(player))
        {
            playersInLobby[player] = new PlayerLobbyData
            {
                Name = $"Player {player.PlayerId}",
                IsReady = networkManager != null && networkManager.IsPlayerReady(player),
                IsHost = player.PlayerId == 0
            };
        }
        
        UpdateLobbyUI();
    }

    private void OnPlayerReadyStateChanged(PlayerRef player, bool readyState)
    {
        if (!playersInLobby.TryGetValue(player, out var lobbyData))
        {
            lobbyData = new PlayerLobbyData
            {
                Name = $"Player {player.PlayerId}",
                IsHost = player.PlayerId == 0
            };
        }

        lobbyData.IsReady = readyState;
        playersInLobby[player] = lobbyData;

        UpdateLobbyUI();
    }
    
    private void OnPlayerLeftRoom(PlayerRef player)
    {
        playersInLobby.Remove(player);
        UpdateLobbyUI();
    }
    
    private void OnRoomListReceived(List<SessionInfo> rooms)
    {
        if (roomListContainer == null) return;
        
        roomListContainer.Clear();
        
        foreach (var room in rooms)
        {
            var roomItem = CreateRoomListItem(room);
            roomListContainer.Add(roomItem);
        }
    }
    
    #endregion
    
    #region UI Helpers
    
    private void UpdateLobbyUI()
    {
        // Update player count
        int count = networkManager.PlayerCount;
        int max = networkManager.MaxPlayers;
        
        if (playerCountLabel != null)
            playerCountLabel.text = $"Players: {count}/{max}";
        
        if (roomStatusLabel != null)
            roomStatusLabel.text = count < 2
                ? "Waiting for players..."
                : (networkManager.IsHost && !networkManager.CanStartGame() ? "Waiting for players to ready up..." : "Ready to start!");
        
        // Update player list
        UpdatePlayerList();
        
        // Enable start button if host and enough players
        if (startGameButton != null && networkManager.IsHost)
        {
            startGameButton.text = "Start Game";

            if (networkManager.CanStartGame())
            {
                startGameButton.RemoveFromClassList("button-disabled-looking");
            }
            else
            {
                startGameButton.AddToClassList("button-disabled-looking");
            }
        }
    }
    
    private void UpdatePlayerList()
    {
        if (playerListContainer == null) return;
        
        playerListContainer.Clear();
        
        foreach (var kvp in playersInLobby)
        {
            var playerItem = CreatePlayerListItem(kvp.Key, kvp.Value);
            playerListContainer.Add(playerItem);
        }
    }
    
    private VisualElement CreatePlayerListItem(PlayerRef player, PlayerLobbyData data)
    {
        var item = new VisualElement();
        item.AddToClassList("player-item");
        
        if (data.IsHost)
            item.AddToClassList("player-item-host");
        else if (data.IsReady)
            item.AddToClassList("player-item-ready");
        
        var nameLabel = new Label(data.Name);
        nameLabel.AddToClassList("player-name");
        item.Add(nameLabel);
        
        var statusLabel = new Label(data.IsHost ? "Host" : (data.IsReady ? "Ready" : "Not Ready"));
        statusLabel.AddToClassList("player-status");
        item.Add(statusLabel);
        
        return item;
    }
    
    private VisualElement CreateRoomListItem(SessionInfo session)
    {
        var item = new VisualElement();
        item.AddToClassList("room-item");
        
        var infoContainer = new VisualElement();
        infoContainer.AddToClassList("room-item-info");
        
        var nameLabel = new Label(session.Name);
        nameLabel.AddToClassList("room-item-name");
        infoContainer.Add(nameLabel);
        
        var playersLabel = new Label($"{session.PlayerCount}/{session.MaxPlayers} Players");
        playersLabel.AddToClassList("room-item-players");
        infoContainer.Add(playersLabel);
        
        item.Add(infoContainer);
        
        var joinButton = new Button(() => JoinRoomFromBrowser(session.Name));
        joinButton.text = "Join";
        joinButton.AddToClassList("menu-button");
        joinButton.AddToClassList("pause-button");
        joinButton.AddToClassList("primary-button");
        joinButton.AddToClassList("room-join-button");
        item.Add(joinButton);
        
        return item;
    }
    
    private async void JoinRoomFromBrowser(string roomId)
    {
        ShowPanel(connectingPanel);
        bool success = await networkManager.JoinRoom(roomId, "");
        
        if (!success)
        {
            ShowPanel(roomBrowserPanel);
        }
    }
    
    private void SetStatus(Label label, string message)
    {
        if (label != null)
            label.text = message;
    }
    
    private void ShowError(string title, string message)
    {
        if (errorTitle != null)
            errorTitle.text = title;
        
        if (errorMessage != null)
            errorMessage.text = message;
        
        SetElementVisible(errorPopup, true);
    }
    
    private void HideErrorPopup()
    {
        SetElementVisible(errorPopup, false);
    }
    
    #endregion
    
    #region Game Actions
    
    private void StartSoloGame()
    {
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    #endregion
    
    #region Settings
    
    private void LoadSettings()
    {
        if (playerNameInput != null)
            playerNameInput.value = networkManager?.GetPlayerName() ?? PlayerPrefs.GetString("PlayerName", "Player");
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 1f);
        
        if (vibrationToggle != null)
            vibrationToggle.value = PlayerPrefs.GetInt("Vibration", 1) == 1;
    }
    
    private void SaveSettings()
    {
        string playerName = playerNameInput?.value ?? "Player";
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";
        
        networkManager?.SetPlayerName(playerName);
        
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider?.value ?? 0.7f);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider?.value ?? 1f);
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider?.value ?? 1f);
        PlayerPrefs.SetInt("Vibration", (vibrationToggle?.value ?? true) ? 1 : 0);
        PlayerPrefs.Save();
        
        // Apply audio settings
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetMasterVolume(sfxVolumeSlider?.value ?? 1f);
            GameManager.Instance.SetMouseSensitivity(sensitivitySlider?.value ?? 1f);
        }
        
        ShowPanel(mainMenuPanel);
    }
    
    #endregion

    private void SyncPlayersFromNetwork()
    {
        if (networkManager == null)
            return;

        playersInLobby.Clear();

        foreach (var player in networkManager.ActivePlayers)
        {
            playersInLobby[player] = new PlayerLobbyData
            {
                Name = $"Player {player.PlayerId}",
                IsReady = networkManager.IsPlayerReady(player),
                IsHost = player.PlayerId == 0
            };
        }
    }

    private void OnGameStarting()
    {
        SetMenuVisible(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            SetMenuVisible(false);
            return;
        }

        if (scene.name == "MainMenu")
        {
            SetMenuVisible(true);
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (root == null)
            return;

        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
