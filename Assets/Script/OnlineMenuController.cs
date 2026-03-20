using UnityEngine;
using UnityEngine.UIElements;
using Fusion;
using System;

/// <summary>
/// Simplified controller for the main menu. Handles starting a Quick Match.
/// </summary>
public class OnlineMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    
    // Panels
    private VisualElement mainMenuPanel;
    private VisualElement onlineMenuPanel;
    private VisualElement connectingPanel;
    private VisualElement settingsPanel;

    // Main Menu Buttons
    private Button playOnlineButton;
    private Button soloModeButton;
    private Button settingsButton;
    private Button quitButton;

    // Online Menu Buttons
    private Button quickMatchButton;
    private Button createRoomButton;
    private Button joinRoomButton;
    private Button onlineBackButton;

    // Connecting Elements
    private Label connectingStatusLabel;
    private Button cancelConnectButton;

    // Settings Buttons
    private Button settingsBackButton;
    private Button saveSettingsButton;

    [Header("UI Styling")]
    public Sprite fullscreenBackgroundSprite;
    public Sprite buttonSprite;

    private VisualElement backgroundElement;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        AddFullscreenBackground();

        // Panels
        mainMenuPanel = root.Q<VisualElement>("MainMenuPanel");
        onlineMenuPanel = root.Q<VisualElement>("OnlineMenuPanel");
        connectingPanel = root.Q<VisualElement>("ConnectingPanel");
        settingsPanel = root.Q<VisualElement>("SettingsPanel");

        // Main Menu Buttons
        playOnlineButton = root.Q<Button>("PlayOnlineButton");
        soloModeButton = root.Q<Button>("SoloModeButton");
        settingsButton = root.Q<Button>("SettingsButton");
        quitButton = root.Q<Button>("QuitButton");

        // Online Menu Buttons
        quickMatchButton = root.Q<Button>("QuickMatchButton");
        createRoomButton = root.Q<Button>("CreateRoomButton");
        joinRoomButton = root.Q<Button>("JoinRoomButton");
        onlineBackButton = root.Q<Button>("OnlineBackButton");

        // Connecting
        connectingStatusLabel = root.Q<Label>("ConnectingStatusLabel");
        cancelConnectButton = root.Q<Button>("CancelConnectButton");

        // Settings
        settingsBackButton = root.Q<Button>("SettingsBackButton");
        saveSettingsButton = root.Q<Button>("SaveSettingsButton");

        // Main Menu callbacks
        playOnlineButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(onlineMenuPanel));
        soloModeButton?.RegisterCallback<ClickEvent>(evt => Debug.Log("Solo Mode Clicked!")); // Thay bằng logic solo nếu có
        settingsButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(settingsPanel));
        quitButton?.RegisterCallback<ClickEvent>(evt => QuitGame());

        // Online Menu callbacks
        quickMatchButton?.RegisterCallback<ClickEvent>(evt => StartQuickMatch());
        createRoomButton?.RegisterCallback<ClickEvent>(evt => Debug.Log("Create Room Clicked!")); // Thay bằng logic tạo phòng nếu có
        joinRoomButton?.RegisterCallback<ClickEvent>(evt => Debug.Log("Join Room Clicked!")); // Thay bằng logic join phòng nếu có
        onlineBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(mainMenuPanel));

        // Connecting callbacks
        cancelConnectButton?.RegisterCallback<ClickEvent>(evt => CancelConnection());

        // Settings callbacks
        settingsBackButton?.RegisterCallback<ClickEvent>(evt => ShowPanel(mainMenuPanel));
        saveSettingsButton?.RegisterCallback<ClickEvent>(evt => Debug.Log("Save Settings Clicked!")); // Thay bằng logic lưu settings nếu có

        // Show main menu
        ShowPanel(mainMenuPanel);

        // Unlock cursor
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        ApplyButtonSprites();
    }

    private void AddFullscreenBackground()
    {
        if (fullscreenBackgroundSprite == null || root == null)
            return;

        if (backgroundElement != null && backgroundElement.parent != null)
        {
            backgroundElement.parent.Remove(backgroundElement);
        }

        backgroundElement = new VisualElement();
        backgroundElement.style.position = Position.Absolute;
        backgroundElement.style.left = 0;
        backgroundElement.style.top = 0;
        backgroundElement.style.width = Length.Percent(100);
        backgroundElement.style.height = Length.Percent(100);
        backgroundElement.style.backgroundImage = new StyleBackground(fullscreenBackgroundSprite);

        root.Insert(0, backgroundElement);
    }

    private void ApplyButtonSprites()
    {
        if (buttonSprite != null)
        {
            var buttons = root.Query<Button>().ToList();
            foreach (var btn in buttons)
            {
                btn.style.backgroundImage = new StyleBackground(buttonSprite);
            }
        }
    }

    private void ShowPanel(VisualElement panel)
    {
        // Ẩn tất cả panel
        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
        if (onlineMenuPanel != null) onlineMenuPanel.style.display = DisplayStyle.None;
        if (connectingPanel != null) connectingPanel.style.display = DisplayStyle.None;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;

        // Hiện panel mong muốn
        if (panel != null)
        {
            panel.style.display = DisplayStyle.Flex;
        }
    }

    private void StartQuickMatch()
    {
        ShowPanel(connectingPanel);
        if (connectingStatusLabel != null)
        {
            connectingStatusLabel.text = "Connecting...";
        }

        var networkManager = FindFirstObjectByType<FusionNetworkManager>();
        if (networkManager != null)
        {
            networkManager.QuickMatch();
        }
        else
        {
            Debug.LogError("FusionNetworkManager not found in scene!");
            if (connectingStatusLabel != null)
            {
                connectingStatusLabel.text = "Error: Network Manager not found!";
            }
        }
    }

    private async void CancelConnection()
    {
        ShowPanel(mainMenuPanel);
        var networkManager = FindFirstObjectByType<FusionNetworkManager>();
        if (networkManager != null && networkManager.Runner != null)
        {
            await networkManager.Runner.Shutdown();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
