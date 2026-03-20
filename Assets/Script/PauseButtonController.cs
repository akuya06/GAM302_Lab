using UnityEngine;
using UnityEngine.UI;

public class PauseButtonController : MonoBehaviour
{
    [Header("Pause Menu Canvas")]
    public GameObject pauseMenuCanvas;

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnPauseButtonClicked);
    }

    void OnPauseButtonClicked()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);
        
    }
}
