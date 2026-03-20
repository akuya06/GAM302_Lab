using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Quản lý màn hình Game Over và xử lý restart/main menu cho cả chế độ solo và online.
/// Online: tất cả players phải bấm restart, nếu đợi quá 15 giây thì tự về main menu.
/// Solo: restart/main menu hoạt động ngay lập tức.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text countdownText;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float onlineTimeout = 15f;

    private bool isOnlineMode = false;
    private bool hasClickedRestart = false;
    private bool isWaitingForOthers = false;
    private Coroutine timeoutCoroutine;

    // Singleton để dễ truy cập từ PlayerHealth
    public static GameOverController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Kiểm tra chế độ chơi (online hay solo)
        var networkManager = FusionNetworkManager.Instance;
        isOnlineMode = networkManager != null && networkManager.Runner != null && networkManager.Runner.IsRunning;

        // Ẩn panel khi bắt đầu
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Đăng ký sự kiện cho các nút
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // Subscribe to network events nếu đang online
        if (isOnlineMode && NetworkRestartManager.Instance != null)
        {
            NetworkRestartManager.Instance.OnAllPlayersReady += OnAllPlayersReadyToRestart;
            NetworkRestartManager.Instance.OnPlayerCountUpdate += UpdateWaitingStatus;
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        // Unsubscribe from network events
        if (NetworkRestartManager.Instance != null)
        {
            NetworkRestartManager.Instance.OnAllPlayersReady -= OnAllPlayersReadyToRestart;
            NetworkRestartManager.Instance.OnPlayerCountUpdate -= UpdateWaitingStatus;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Hiển thị màn hình Game Over
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        hasClickedRestart = false;
        isWaitingForOthers = false;

        // Cập nhật lại trạng thái online/solo
        var networkManager = FusionNetworkManager.Instance;
        isOnlineMode = networkManager != null && networkManager.Runner != null && networkManager.Runner.IsRunning;

        // Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hiển thị trạng thái phù hợp
        if (isOnlineMode)
        {
            UpdateStatusText("Bấm Restart để chờ các người chơi khác");
            if (countdownText != null) countdownText.text = "";
        }
        else
        {
            UpdateStatusText("Bấm Restart để chơi lại hoặc Main Menu để về menu chính");
            if (countdownText != null) countdownText.text = "";
        }
    }

    /// <summary>
    /// Ẩn màn hình Game Over
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
    }

    private void OnRestartClicked()
    {
        if (isOnlineMode)
        {
            HandleOnlineRestart();
        }
        else
        {
            HandleSoloRestart();
        }
    }

    private void OnMainMenuClicked()
    {
        if (isOnlineMode)
        {
            HandleOnlineMainMenu();
        }
        else
        {
            HandleSoloMainMenu();
        }
    }

    #region Solo Mode

    private void HandleSoloRestart()
    {
        Time.timeScale = 1f;
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void HandleSoloMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Online Mode

    private void HandleOnlineRestart()
    {
        if (hasClickedRestart) return;

        hasClickedRestart = true;
        isWaitingForOthers = true;

        // Thông báo cho server biết player này đã sẵn sàng restart
        if (NetworkRestartManager.Instance != null)
        {
            NetworkRestartManager.Instance.RequestRestart();
        }

        // Disable nút restart sau khi đã bấm
        if (restartButton != null)
        {
            restartButton.interactable = false;
        }

        UpdateStatusText("Đang chờ các người chơi khác...");

        // Bắt đầu đếm ngược timeout
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
        timeoutCoroutine = StartCoroutine(OnlineTimeoutCoroutine());
    }

    private async void HandleOnlineMainMenu()
    {
        // Ngắt kết nối mạng trước khi về main menu
        if (FusionNetworkManager.Instance != null && FusionNetworkManager.Instance.Runner != null)
        {
            await FusionNetworkManager.Instance.Runner.Shutdown();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator OnlineTimeoutCoroutine()
    {
        float remainingTime = onlineTimeout;

        while (remainingTime > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Tự động về Main Menu sau: {Mathf.CeilToInt(remainingTime)}s";
            }

            yield return new WaitForSecondsRealtime(1f);
            remainingTime -= 1f;
        }

        // Timeout - về main menu
        UpdateStatusText("Hết thời gian chờ. Đang về Main Menu...");

        yield return new WaitForSecondsRealtime(1f);

        HandleOnlineMainMenu();
    }

    private void OnAllPlayersReadyToRestart()
    {
        // Tất cả players đã sẵn sàng - restart game
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }

        UpdateStatusText("Tất cả người chơi đã sẵn sàng! Đang khởi động lại...");

        if (countdownText != null)
        {
            countdownText.text = "";
        }

        // Delay nhỏ trước khi restart
        StartCoroutine(DelayedRestart());
    }

    private IEnumerator DelayedRestart()
    {
        yield return new WaitForSecondsRealtime(1f);

        Time.timeScale = 1f;
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void UpdateWaitingStatus(int readyCount, int totalCount)
    {
        if (isWaitingForOthers)
        {
            UpdateStatusText($"Đang chờ: {readyCount}/{totalCount} người chơi đã sẵn sàng");
        }
    }

    #endregion

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
}
