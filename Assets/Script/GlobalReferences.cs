using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance;
    public GameObject bulletImpactEffectPrefab;
    public GameObject bloodSprayEffect;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
