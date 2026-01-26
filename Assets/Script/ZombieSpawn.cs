using UnityEngine;

public class ZombieSpawn : MonoBehaviour
{
    public GameObject zombiePrefab;
    public Transform baseCenter; // Gán vị trí trung tâm căn cứ trong Inspector
    public float spawnRadius = 30f; // Bán kính spawn quanh căn cứ
    public int zombieCount = 10;

    void Start()
    {
        SpawnZombies();
    }

    void SpawnZombies()
    {
        for (int i = 0; i < zombieCount; i++)
        {
            Vector3 spawnPos = GetRandomPositionAroundBase();
            Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        }
    }

    Vector3 GetRandomPositionAroundBase()
    {
        // Lấy vị trí ngẫu nhiên trên mặt phẳng xung quanh căn cứ
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 pos = baseCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Nếu cần đặt zombie trên mặt đất, có thể raycast xuống để lấy đúng độ cao mặt đất
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 50, Vector3.down, out hit, 100f))
        {
            pos.y = hit.point.y;
        }
        return pos;
    }

    void OnDrawGizmos()
    {
        if (baseCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(baseCenter.position, spawnRadius);
        }
    }
}
