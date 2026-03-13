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

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

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

        // Ẩn panel khi bắt đầu
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
    }

    private void Update()
    {
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
        bool isOnlineMode = FusionNetworkManager.Instance != null && FusionNetworkManager.Instance.IsConnected;
        
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
    }

    private void OnQuitClicked()
    {
        // Kiểm tra online mode để disconnect trước khi về main menu
        bool isOnlineMode = FusionNetworkManager.Instance != null && FusionNetworkManager.Instance.IsConnected;
        
        if (isOnlineMode)
        {
            // Ngắt kết nối mạng trước khi về main menu
            _ = FusionNetworkManager.Instance.Disconnect();
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
        // Quay lại menu pause chính
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void OnPCModeClicked()
    {
        // Mode switching removed - cả PC và Mobile input đều hoạt động cùng lúc
    }

    private void OnMobileModeClicked()
    {
        // Mode switching removed - cả PC và Mobile input đều hoạt động cùng lúc
    }

    private void DisablePlayerControls()
    {
        var playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        var mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement != null) mouseMovement.enabled = false;

        var weapon = FindObjectOfType<Weapon>();
        if (weapon != null) weapon.enabled = false;
    }

    private void EnablePlayerControls()
    {
        var playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = true;

        var mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement != null) mouseMovement.enabled = true;

        var weapon = FindObjectOfType<Weapon>();
        if (weapon != null) weapon.enabled = true;
    }
}
