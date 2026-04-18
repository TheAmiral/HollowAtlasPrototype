using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject shooterEnemyPrefab;
    public Transform player;

    public float spawnInterval = 0.75f;
    public float minSpawnDistance = 10f;
    public float maxSpawnDistance = 14f;
    public int maxAliveEnemies = 25;

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float fastSpawnChance = 0.25f;
    [Range(0f, 1f)] public float tankSpawnChance = 0.15f;
    [Range(0f, 1f)] public float shooterSpawnChance = 0.20f;

    [Header("XP Rewards")]
    public int basicEnemyXP = 1;
    public int fastEnemyXP = 1;
    public int shooterEnemyXP = 1;
    public int tankEnemyXP = 3;

    public float difficultyRamp = 0.02f;

    private float timer;

    void Update()
    {
        if (enemyPrefab == null || player == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        float timeBonus = 0f;

        if (GameManager.Instance != null)
            timeBonus = GameManager.Instance.ElapsedTime * difficultyRamp;

        float currentSpawnInterval = Mathf.Clamp(spawnInterval - timeBonus, 0.2f, spawnInterval);
        timer = currentSpawnInterval;

        int currentMaxAlive = maxAliveEnemies + Mathf.FloorToInt(timeBonus * 10f);

        int aliveCount = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
        if (aliveCount >= currentMaxAlive)
            return;

        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        Vector2 point = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);

        if (point == Vector2.zero)
            point = Vector2.up * minSpawnDistance;

        Vector3 spawnPos = player.position + new Vector3(point.x, 0.5f, point.y);

        GameObject prefabToSpawn = ChooseEnemyPrefab();
        GameObject spawnedEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        ApplyXpRewardByType(spawnedEnemy, prefabToSpawn);
    }

    GameObject ChooseEnemyPrefab()
    {
        float roll = Random.value;

        float shooterThreshold = shooterSpawnChance;
        float fastThreshold = shooterThreshold + fastSpawnChance;
        float tankThreshold = fastThreshold + tankSpawnChance;

        if (shooterEnemyPrefab != null && roll < shooterThreshold)
            return shooterEnemyPrefab;

        if (fastEnemyPrefab != null && roll < fastThreshold)
            return fastEnemyPrefab;

        if (tankEnemyPrefab != null && roll < tankThreshold)
            return tankEnemyPrefab;

        return enemyPrefab;
    }

    void ApplyXpRewardByType(GameObject spawnedEnemy, GameObject sourcePrefab)
    {
        if (spawnedEnemy == null)
            return;

        EnemyHealth enemyHealth = spawnedEnemy.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
            return;

        if (sourcePrefab == tankEnemyPrefab)
            enemyHealth.xpDropValue = Mathf.Max(1, tankEnemyXP);
        else if (sourcePrefab == fastEnemyPrefab)
            enemyHealth.xpDropValue = Mathf.Max(1, fastEnemyXP);
        else if (sourcePrefab == shooterEnemyPrefab)
            enemyHealth.xpDropValue = Mathf.Max(1, shooterEnemyXP);
        else
            enemyHealth.xpDropValue = Mathf.Max(1, basicEnemyXP);
    }
}
