using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    
    private Button newGameButton;
    private Button continueButton;
    private Button settingsButton;
    private Button quitButton;
    
    [SerializeField] private string gameSceneName = "GameScene"; // Tên scene game của bạn
    
    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        // Lấy các button từ UI
        newGameButton = root.Q<Button>("NewGameButton");
        continueButton = root.Q<Button>("ContinueButton");
        settingsButton = root.Q<Button>("SettingsButton");
        quitButton = root.Q<Button>("QuitButton");
        
        // Đăng ký các sự kiện
        newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClick);
        continueButton?.RegisterCallback<ClickEvent>(OnContinueClick);
        settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClick);
        quitButton?.RegisterCallback<ClickEvent>(OnQuitClick);
        
        // Ẩn cursor và unlock
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Kiểm tra có save game không để enable/disable nút Continue
        CheckSaveGame();
    }
    
    void OnDisable()
    {
        // Hủy đăng ký các sự kiện
        newGameButton?.UnregisterCallback<ClickEvent>(OnNewGameClick);
        continueButton?.UnregisterCallback<ClickEvent>(OnContinueClick);
        settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClick);
        quitButton?.UnregisterCallback<ClickEvent>(OnQuitClick);
    }
    
    private void OnNewGameClick(ClickEvent evt)
    {
        Debug.Log("Starting new game...");
        // Xóa save game cũ nếu có
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        
        // Load scene game
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void OnContinueClick(ClickEvent evt)
    {
        Debug.Log("Continuing game...");
        
        // Load level đã lưu
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            string savedScene = PlayerPrefs.GetString("SavedLevel", gameSceneName);
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
    private void OnSettingsClick(ClickEvent evt)
    {
        Debug.Log("Opening settings...");
        // TODO: Mở menu settings
        // Bạn có thể tạo một panel settings riêng
    }
    
    private void OnQuitClick(ClickEvent evt)
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void CheckSaveGame()
    {
        // Disable nút Continue nếu không có save game
        if (continueButton != null)
        {
            bool hasSave = PlayerPrefs.HasKey("SavedLevel");
            continueButton.SetEnabled(hasSave);
        }
    }
}
