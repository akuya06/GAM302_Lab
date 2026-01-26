using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Gun Sounds")]
    public AudioClip[] gunShotClips; // Danh sách tiếng bắn súng
    public AudioClip reloadClip;     // Tiếng nạp đạn
    public AudioClip zombieChasing;
    public AudioClip zombieAttack;
    public AudioClip zombieHurt;
    public AudioClip zombieDie;
    public AudioSource zombieAudioSource;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Phát tiếng bắn súng random
    public void PlayGunShot()
    {
        if (gunShotClips.Length == 0) return;
        int idx = Random.Range(0, gunShotClips.Length);
        audioSource.PlayOneShot(gunShotClips[idx]);
    }

    // Phát tiếng nạp đạn
    public void PlayReload()
    {
        if (reloadClip == null) return;
        audioSource.PlayOneShot(reloadClip);
    }
}
