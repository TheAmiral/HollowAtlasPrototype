using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Zaman bazlı spawn fazı. 10 dakikalık run pacing'i Inspector'dan ayarlamak için.
    [System.Serializable]
    public class SpawnPhase
    {
        public string name = "Phase";
        [Tooltip("Bu faz, ElapsedTime bu değere (saniye) ulaşana kadar geçerlidir.")]
        public float endTime = 60f;
        [Tooltip("Spawn aralığı (saniye). Düşük = daha yoğun.")]
        public float spawnInterval = 1.2f;
        [Tooltip("Aynı anda canlı düşman üst sınırı.")]
        public int maxAlive = 16;
        [Range(0f, 1f)] public float fastChance = 0f;
        [Range(0f, 1f)] public float tankChance = 0f;
        [Range(0f, 1f)] public float shooterChance = 0f;
    }

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject shooterEnemyPrefab;
    public Transform player;

    [Header("Placement")]
    public float minSpawnDistance = 10f;
    public float maxSpawnDistance = 14f;

    [Header("Phase Pacing (10 dk yay)")]
    [Tooltip("Açıkken faz tablosu kullanılır; kapalıyken eski difficultyRamp formülü çalışır.")]
    public bool usePhaseSystem = true;
    [Tooltip("Aktif boss varken spawn aralığını bu kat yavaşlatır (1 = etkisiz).")]
    [Range(1f, 3f)] public float bossActiveSpawnMultiplier = 1.5f;
    [Tooltip("Spawn aralığı için mutlak alt sınır (saniye). Sonsuz spawn'ı engeller.")]
    public float minSpawnIntervalFloor = 0.18f;
    public SpawnPhase[] phases =
    {
        new SpawnPhase { name = "P1 Basic",         endTime =  60f, spawnInterval = 1.20f, maxAlive = 14, fastChance = 0.00f, tankChance = 0.00f, shooterChance = 0.00f },
        new SpawnPhase { name = "P2 +Fast",         endTime = 180f, spawnInterval = 1.00f, maxAlive = 20, fastChance = 0.30f, tankChance = 0.00f, shooterChance = 0.00f },
        new SpawnPhase { name = "P3 +Tank/Shooter", endTime = 300f, spawnInterval = 0.80f, maxAlive = 28, fastChance = 0.30f, tankChance = 0.18f, shooterChance = 0.15f },
        new SpawnPhase { name = "P4 Dense",         endTime = 420f, spawnInterval = 0.60f, maxAlive = 36, fastChance = 0.32f, tankChance = 0.20f, shooterChance = 0.20f },
        new SpawnPhase { name = "P5 Max",           endTime = 600f, spawnInterval = 0.45f, maxAlive = 46, fastChance = 0.32f, tankChance = 0.22f, shooterChance = 0.22f },
    };

    [Header("Legacy Fallback (usePhaseSystem = false)")]
    public float spawnInterval = 0.75f;
    public int maxAliveEnemies = 25;
    [Range(0f, 1f)] public float fastSpawnChance = 0.25f;
    [Range(0f, 1f)] public float tankSpawnChance = 0.15f;
    [Range(0f, 1f)] public float shooterSpawnChance = 0.20f;
    public float difficultyRamp = 0.02f;

    [Header("XP Rewards")]
    public int basicEnemyXP = 1;
    public int fastEnemyXP = 2;
    public int shooterEnemyXP = 3;
    public int tankEnemyXP = 4;

    [Header("Debug Logging")]
    [Tooltip("Faz geçişlerini loglar: [Wave] P2 +Fast started")]
    public bool debugLogging = true;
    [Tooltip("Her 5 sn'de bir pressure/interval/active loglar (opt-in, spam'i önler).")]
    public bool verboseSpawnLog = false;

    private float timer;
    private int _currentPhaseIndex = -1;
    private float _lastVerboseLog = -999f;

    void Update()
    {
        if (enemyPrefab == null || player == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        if (usePhaseSystem && phases != null && phases.Length > 0)
            TickPhaseSystem();
        else
            TickLegacy();
    }

    // ── Phase-based pacing ────────────────────────────────────────────────────

    void TickPhaseSystem()
    {
        float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;
        SpawnPhase phase = ResolvePhase(elapsed, out int phaseIndex);

        // Tüm faz girdileri geçersizse güvenli şekilde legacy yola düş.
        if (phase == null)
        {
            TickLegacy();
            return;
        }

        if (phaseIndex != _currentPhaseIndex)
        {
            _currentPhaseIndex = phaseIndex;
            if (debugLogging)
                Debug.Log($"[Wave] {phase.name} started");
        }

        float pressure = RunLoadoutSystem.Instance?.EnemySpawnPressure ?? 1f;
        if (pressure < 0.01f)
            pressure = 1f;

        float interval = phase.spawnInterval / pressure;

        if (IsBossActive())
            interval *= Mathf.Max(1f, bossActiveSpawnMultiplier);

        interval = Mathf.Max(minSpawnIntervalFloor, interval);
        timer = interval;

        int aliveCount = EnemyHealth.AliveCount;

        if (verboseSpawnLog && Time.unscaledTime - _lastVerboseLog >= 5f)
        {
            _lastVerboseLog = Time.unscaledTime;
            Debug.Log($"[Spawner] Pressure: {pressure:0.00} Interval: {interval:0.00} Active: {aliveCount}");
        }

        if (aliveCount >= phase.maxAlive)
            return;

        SpawnEnemy(phase);
    }

    SpawnPhase ResolvePhase(float elapsed, out int index)
    {
        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i] == null)
                continue;

            if (elapsed < phases[i].endTime)
            {
                index = i;
                return phases[i];
            }
        }

        // Son fazın bitiş süresinden sonra son fazda kal (10 dk sonrası max pacing).
        index = phases.Length - 1;
        return phases[index];
    }

    bool IsBossActive()
    {
        BossSpawnSystem boss = BossSpawnSystem.Instance;
        return boss != null && boss.BossSpawned && !boss.BossDefeated;
    }

    // ── Legacy pacing (usePhaseSystem = false) ────────────────────────────────

    void TickLegacy()
    {
        float timeBonus = 0f;
        if (GameManager.Instance != null)
            timeBonus = GameManager.Instance.ElapsedTime * difficultyRamp;

        float pressure = RunLoadoutSystem.Instance?.EnemySpawnPressure ?? 1f;
        if (pressure < 0.01f)
            pressure = 1f;

        float currentSpawnInterval = Mathf.Clamp((spawnInterval - timeBonus) / pressure, 0.15f, spawnInterval);
        timer = currentSpawnInterval;

        int currentMaxAlive = maxAliveEnemies + Mathf.FloorToInt(timeBonus * 10f);

        if (EnemyHealth.AliveCount >= currentMaxAlive)
            return;

        SpawnEnemy(null);
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    void SpawnEnemy(SpawnPhase phase)
    {
        Vector2 point = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);

        if (point == Vector2.zero)
            point = Vector2.up * minSpawnDistance;

        float candidateX = player.position.x + point.x;
        float candidateZ = player.position.z + point.y;
        float groundY = GroundYAtPoint(candidateX, candidateZ, player.position.y);

        GameObject prefabToSpawn = phase != null ? ChooseEnemyPrefabForPhase(phase) : ChooseEnemyPrefab();
        GameObject spawnedEnemy = Instantiate(prefabToSpawn, new Vector3(candidateX, groundY, candidateZ), Quaternion.identity);

        Renderer r = spawnedEnemy.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            float delta = groundY - r.bounds.min.y;
            if (Mathf.Abs(delta) > 0.005f)
                spawnedEnemy.transform.position += Vector3.up * delta;
        }

        ApplyXpRewardByType(spawnedEnemy, prefabToSpawn);
    }

    float GroundYAtPoint(float x, float z, float referenceY)
    {
        Vector3 origin = new Vector3(x, referenceY + 5f, z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        return referenceY;
    }

    // Faz tabanlı tip seçimi: yalnız o fazda izinli tipler (null prefab → basic'e düşer).
    GameObject ChooseEnemyPrefabForPhase(SpawnPhase phase)
    {
        float roll = Random.value;

        float shooter = shooterEnemyPrefab != null ? Mathf.Clamp01(phase.shooterChance) : 0f;
        float fast    = fastEnemyPrefab    != null ? Mathf.Clamp01(phase.fastChance)    : 0f;
        float tank    = tankEnemyPrefab    != null ? Mathf.Clamp01(phase.tankChance)    : 0f;

        float total = shooter + fast + tank;
        float scale = total > 1f ? 1f / total : 1f;

        float shooterThreshold = shooter * scale;
        float fastThreshold    = shooterThreshold + fast * scale;
        float tankThreshold    = fastThreshold    + tank * scale;

        if (shooterEnemyPrefab != null && roll < shooterThreshold)
            return shooterEnemyPrefab;

        if (fastEnemyPrefab != null && roll < fastThreshold)
            return fastEnemyPrefab;

        if (tankEnemyPrefab != null && roll < tankThreshold)
            return tankEnemyPrefab;

        return enemyPrefab;
    }

    // Legacy tip seçimi (usePhaseSystem = false yolunda kullanılır).
    GameObject ChooseEnemyPrefab()
    {
        float roll = Random.value;

        float total = shooterSpawnChance + fastSpawnChance + tankSpawnChance;
        float scale = total > 1f ? 1f / total : 1f;

        float shooterThreshold = shooterSpawnChance * scale;
        float fastThreshold    = shooterThreshold + fastSpawnChance * scale;
        float tankThreshold    = fastThreshold    + tankSpawnChance * scale;

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
