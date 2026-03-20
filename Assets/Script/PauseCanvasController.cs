using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseCanvasController : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Panel chính của Pause (Resume/Restart/Settings/Quit)")]
    public GameObject pausePanel;

    [Tooltip("Panel Settings, nơi chứa nút chọn PC/Mobile, Back, v.v.")]
    public GameObject settingsPanel;

    [Header("Pause Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings Buttons")]
    public Button backFromSettingsButton;
    public Button pcModeButton;
    public Button mobileModeButton;

    [Header("Control Mode")]
    [Tooltip("Toggle mode trong settings: Off = PC, On = Mobile")]
    public Toggle controlModeToggle;
    [Tooltip("Danh sách UI chỉ dùng cho Mobile (joystick, bắn, nạp đạn, nhảy, ...)")]
    public GameObject[] mobileOnlyControls;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isInitializingToggle = false;
    private bool waitingInitialModeSelection = false;
    private ControlMode currentControlMode = ControlMode.PC;

    private PlayerMovement cachedPlayerMovement;
    private MouseMovement cachedMouseMovement;
    private Weapon cachedWeapon;

    public bool IsPaused()
    {
        return isPaused;
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện cho các nút nếu được gán
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        if (backFromSettingsButton != null) backFromSettingsButton.onClick.AddListener(OnBackFromSettingsClicked);
        if (pcModeButton != null) pcModeButton.onClick.AddListener(OnPCModeClicked);
        if (mobileModeButton != null) mobileModeButton.onClick.AddListener(OnMobileModeClicked);
        if (controlModeToggle != null) controlModeToggle.onValueChanged.AddListener(OnControlModeToggleChanged);

        // Ẩn panel khi bắt đầu
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        bool hasSavedMode = ControlModeSettings.HasSavedMode();
        currentControlMode = ControlModeSettings.LoadOrDefault(ControlMode.PC);
        ApplyControlMode(currentControlMode, false);

        if (!hasSavedMode)
        {
            EnterInitialModeSelection();
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

        if (backFromSettingsButton != null) backFromSettingsButton.onClick.RemoveListener(OnBackFromSettingsClicked);
        if (pcModeButton != null) pcModeButton.onClick.RemoveListener(OnPCModeClicked);
        if (mobileModeButton != null) mobileModeButton.onClick.RemoveListener(OnMobileModeClicked);
        if (controlModeToggle != null) controlModeToggle.onValueChanged.RemoveListener(OnControlModeToggleChanged);

        ControlModeSettings.Save(currentControlMode);
    }

    private void Update()
    {
        if (waitingInitialModeSelection)
            return;

        // Nhấn ESC để pause/resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        isPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisablePlayerControls();
    }

    public void Resume()
    {
        isPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EnablePlayerControls();
    }

    private void OnResumeClicked()
    {
        Resume();
    }

    private void OnRestartClicked()
    {
        var networkManager = FusionNetworkManager.Instance;
        bool isOnlineMode = networkManager != null && networkManager.Runner != null && networkManager.Runner.IsRunning;
        
        if (isOnlineMode)
        {
            // Online mode: không cho restart từ pause menu, chỉ cho phép khi game over
            Debug.Log("Restart không khả dụng trong chế độ online từ pause menu");
            return;
        }
        
        // Solo mode: Restart ngay lập tức
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    private void OnSettingsClicked()
    {
        // Mở panel Settings (để chọn PC/Mobile)
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (backFromSettingsButton != null)
            backFromSettingsButton.gameObject.SetActive(true);
    }

    private async void OnQuitClicked()
    {
        // Kiểm tra online mode để disconnect trước khi về main menu
        var networkManager = FusionNetworkManager.Instance;
        bool isOnlineMode = networkManager != null && networkManager.Runner != null && networkManager.Runner.IsRunning;
        
        if (isOnlineMode)
        {
            // Ngắt kết nối mạng trước khi về main menu
            await networkManager.Runner.Shutdown();
        }
        
        // Có thể về main menu hoặc thoát hẳn game
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnBackFromSettingsClicked()
    {
        if (waitingInitialModeSelection)
            return;

        // Quay lại menu pause chính
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void OnPCModeClicked()
    {
        ApplyControlMode(ControlMode.PC, true);
        CompleteInitialSelectionIfNeeded();
    }

    private void OnMobileModeClicked()
    {
        ApplyControlMode(ControlMode.Mobile, true);
        CompleteInitialSelectionIfNeeded();
    }

    private void OnControlModeToggleChanged(bool isMobile)
    {
        if (isInitializingToggle)
            return;

        ApplyControlMode(isMobile ? ControlMode.Mobile : ControlMode.PC, true);
        CompleteInitialSelectionIfNeeded();
    }

    private void EnterInitialModeSelection()
    {
        waitingInitialModeSelection = true;
        isPaused = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisablePlayerControls();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (backFromSettingsButton != null)
            backFromSettingsButton.gameObject.SetActive(false);
    }

    private void CompleteInitialSelectionIfNeeded()
    {
        if (!waitingInitialModeSelection)
            return;

        waitingInitialModeSelection = false;

        if (backFromSettingsButton != null)
            backFromSettingsButton.gameObject.SetActive(true);

        Resume();
    }

    private void ApplyControlMode(ControlMode mode, bool saveToFile)
    {
        currentControlMode = mode;

        bool isMobile = mode == ControlMode.Mobile;
        ApplyMobileControlsVisibility(isMobile);

        if (controlModeToggle != null)
        {
            isInitializingToggle = true;
            controlModeToggle.isOn = isMobile;
            isInitializingToggle = false;
        }

        if (saveToFile)
        {
            ControlModeSettings.Save(mode);
        }
    }

    private void ApplyMobileControlsVisibility(bool isMobile)
    {
        if (mobileOnlyControls == null)
            return;

        for (int i = 0; i < mobileOnlyControls.Length; i++)
        {
            if (mobileOnlyControls[i] != null)
            {
                mobileOnlyControls[i].SetActive(isMobile);
            }
        }
    }

    private void DisablePlayerControls()
    {
        CachePlayerControlReferences();

        if (cachedPlayerMovement != null) cachedPlayerMovement.enabled = false;
        if (cachedMouseMovement != null) cachedMouseMovement.enabled = false;
        if (cachedWeapon != null) cachedWeapon.enabled = false;
    }

    private void EnablePlayerControls()
    {
        CachePlayerControlReferences();

        if (cachedPlayerMovement != null) cachedPlayerMovement.enabled = true;
        if (cachedMouseMovement != null) cachedMouseMovement.enabled = true;
        if (cachedWeapon != null) cachedWeapon.enabled = true;
    }

    private void CachePlayerControlReferences()
    {
        if (cachedPlayerMovement == null)
        {
            cachedPlayerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (cachedMouseMovement == null)
        {
            cachedMouseMovement = FindFirstObjectByType<MouseMovement>();
        }

        if (cachedWeapon == null)
        {
            cachedWeapon = FindFirstObjectByType<Weapon>();
        }
    }
}
