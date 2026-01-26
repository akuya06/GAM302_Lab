using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement pauseMenu;
    
    private Button resumeButton;
    private Button settingsButton;
    private Button mainMenuButton;
    private Button quitButton;
    
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Tên scene main menu
    
    private bool isPaused = false;
    
    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        // Lấy pause menu container
        pauseMenu = root.Q<VisualElement>("PauseMenu");
        
        // Lấy các button
        resumeButton = root.Q<Button>("ResumeButton");
        settingsButton = root.Q<Button>("PauseSettingsButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        quitButton = root.Q<Button>("PauseQuitButton");
        
        // Đăng ký sự kiện
        resumeButton?.RegisterCallback<ClickEvent>(OnResumeClick);
        settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClick);
        mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuClick);
        quitButton?.RegisterCallback<ClickEvent>(OnQuitClick);
        
        // Ẩn menu khi bắt đầu
        if (pauseMenu != null)
        {
            pauseMenu.style.display = DisplayStyle.None;
        }
    }
    
    void OnDisable()
    {
        // Hủy đăng ký sự kiện
        resumeButton?.UnregisterCallback<ClickEvent>(OnResumeClick);
        settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClick);
        mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuClick);
        quitButton?.UnregisterCallback<ClickEvent>(OnQuitClick);
    }
    
    void Update()
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
        
        if (pauseMenu != null)
        {
            pauseMenu.style.display = DisplayStyle.Flex;
        }
        
        // Dừng game
        Time.timeScale = 0f;
        
        // Hiện cursor
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Disable player controls
        DisablePlayerControls();
    }
    
    public void Resume()
    {
        isPaused = false;
        
        if (pauseMenu != null)
        {
            pauseMenu.style.display = DisplayStyle.None;
        }
        
        // Tiếp tục game
        Time.timeScale = 1f;
        
        // Ẩn cursor
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        
        // Enable player controls
        EnablePlayerControls();
    }
    
    private void OnResumeClick(ClickEvent evt)
    {
        Resume();
    }
    
    private void OnSettingsClick(ClickEvent evt)
    {
        Debug.Log("Opening settings from pause menu...");
        // TODO: Mở settings panel
    }
    
    private void OnMainMenuClick(ClickEvent evt)
    {
        Debug.Log("Returning to main menu...");
        
        // Lưu game state nếu cần
        SaveGame();
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private void OnQuitClick(ClickEvent evt)
    {
        Debug.Log("Quitting game...");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void DisablePlayerControls()
    {
        // Tắt player movement và mouse look
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        MouseMovement mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement != null)
        {
            mouseMovement.enabled = false;
        }
        
        Weapon weapon = FindObjectOfType<Weapon>();
        if (weapon != null)
        {
            weapon.enabled = false;
        }
    }
    
    private void EnablePlayerControls()
    {
        // Bật lại player controls
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        MouseMovement mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement != null)
        {
            mouseMovement.enabled = true;
        }
        
        Weapon weapon = FindObjectOfType<Weapon>();
        if (weapon != null)
        {
            weapon.enabled = true;
        }
    }
    
    private void SaveGame()
    {
        // Lưu scene hiện tại
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedLevel", currentScene);
        PlayerPrefs.Save();
        
        Debug.Log($"Game saved at scene: {currentScene}");
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
}
