using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private bool invertYAxis = false;
    
    private PauseMenuController pauseMenuController;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Tìm pause menu controller trong scene
        pauseMenuController = FindObjectOfType<PauseMenuController>();
        
        // Lock cursor khi bắt đầu game
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            LockCursor();
        }
    }
    
    void Update()
    {
        // Refresh pause menu controller reference nếu cần
        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>();
        }
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    // Settings Management
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();
    }
    
    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }
    
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    public void SetInvertYAxis(bool invert)
    {
        invertYAxis = invert;
        PlayerPrefs.SetInt("InvertY", invert ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public bool GetInvertYAxis()
    {
        return invertYAxis;
    }
    
    private void LoadSettings()
    {
        // Load saved settings
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        invertYAxis = PlayerPrefs.GetInt("InvertY", 0) == 1;
        
        // Apply volume
        AudioListener.volume = masterVolume;
    }
    
    // Game State Management
    public void LoadLevel(string levelName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelName);
    }
    
    public void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    public bool IsGamePaused()
    {
        return pauseMenuController != null && pauseMenuController.IsPaused();
    }
}
