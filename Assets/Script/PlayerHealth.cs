using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject gameOverScreen; // Gán panel Game Over trong Inspector (ẩn sẵn)
    [SerializeField] private bool stopTimeOnDeath = true;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;          // Slider hiển thị máu
    [SerializeField] private Image hurtEffectImage;        // Image overlay hiệu ứng hurt (UI full screen)
    [SerializeField] private float hurtFadeDuration = 0.5f;
    [SerializeField] private float hurtMaxAlpha = 0.7f;
    [SerializeField] public Text restartCountdownText; // Text hiển thị đếm ngược khi restart

    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }

        // Khởi tạo slider máu
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Đảm bảo hiệu ứng hurt ban đầu tắt
        if (hurtEffectImage != null)
        {
            var c = hurtEffectImage.color;
            c.a = 0f;
            hurtEffectImage.color = c;
        }

        if (restartCountdownText != null)
        {
            restartCountdownText.text = "";
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth -= amount;

        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHealthUI();
        PlayHurtEffect();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Tắt điều khiển player
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var mouseLook = GetComponent<MouseMovement>();
        if (mouseLook != null) mouseLook.enabled = false;

        // Vũ khí thường nằm trên object khác nên dùng FindObjectOfType
        var weapon = FindObjectOfType<Weapon>();
        if (weapon != null) weapon.enabled = false;

        // Hiện màn hình Game Over
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }

        // Mở khóa chuột
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockCursor();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Dừng thời gian nếu muốn (chỉ cho solo mode)
        bool isOnlineMode = FusionNetworkManager.Instance != null && FusionNetworkManager.Instance.IsConnected;
        
        if (stopTimeOnDeath && !isOnlineMode)
        {
            Time.timeScale = 0f;
        }

        // Sử dụng GameOverController nếu có (hỗ trợ cả online và solo)
        if (GameOverController.Instance != null)
        {
            GameOverController.Instance.ShowGameOver();
        }
        else
        {
            // Fallback: bắt đầu đếm ngược restart nếu có text (chỉ cho solo mode)
            if (restartCountdownText != null && !isOnlineMode)
            {
                StartCoroutine(RestartCountdownCoroutine());
            }
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void PlayHurtEffect()
    {
        if (hurtEffectImage == null) return;

        // Reset alpha lên max mỗi lần bị trúng đạn
        var c = hurtEffectImage.color;
        c.a = hurtMaxAlpha;
        hurtEffectImage.color = c;

        // Bắt đầu fade dần về 0
        CancelInvoke(nameof(FadeHurtEffectStep));
        InvokeRepeating(nameof(FadeHurtEffectStep), 0.02f, 0.02f);
    }

    private void FadeHurtEffectStep()
    {
        if (hurtEffectImage == null)
        {
            CancelInvoke(nameof(FadeHurtEffectStep));
            return;
        }

        var c = hurtEffectImage.color;
        float deltaAlpha = (hurtMaxAlpha / hurtFadeDuration) * 0.02f;
        c.a -= deltaAlpha;

        if (c.a <= 0f)
        {
            c.a = 0f;
            hurtEffectImage.color = c;
            CancelInvoke(nameof(FadeHurtEffectStep));
        }
        else
        {
            hurtEffectImage.color = c;
        }
    }

    private System.Collections.IEnumerator RestartCountdownCoroutine()
    {
        // Chỉ dành cho solo mode khi không có GameOverController
        float countdown = 5f;
        while (countdown > 0f)
        {
            if (restartCountdownText != null)
            {
                restartCountdownText.text = $"Chơi lại sau {Mathf.CeilToInt(countdown)}";
            }

            yield return new WaitForSecondsRealtime(1f);
            countdown -= 1f;
        }

        if (restartCountdownText != null)
        {
            restartCountdownText.text = "Đang chơi lại...";
        }

        // trả time scale về 1 rồi reload scene hiện tại
        Time.timeScale = 1f;
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Reset máu về đầy (sử dụng khi restart game)
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        // Bật lại điều khiển player
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;

        var mouseLook = GetComponent<MouseMovement>();
        if (mouseLook != null) mouseLook.enabled = true;

        var weapon = FindObjectOfType<Weapon>();
        if (weapon != null) weapon.enabled = true;
    }
}
