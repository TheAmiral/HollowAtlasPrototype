using UnityEngine;
using UnityEngine.SceneManagement;

// Atlas Kalıntı Olay Sistemi — kill milestone + süre kapısı kombinasyonu.
// Kill eşikleri: 75 / 180 / 340 / 550, sonrası büyüyen adımlarla devam eder.
// İlk sandık için GameManager.ElapsedTime >= 90 s şartı aranır.
// Sonraki sandıklar için önceki sandığın tüketildiği andan itibaren en az 120 s geçmeli.
// Aynı anda sadece 1 aktif sandık olabilir; boss kill sayacı artırmaz.
public class RelicChestSpawnSystem : MonoBehaviour
{
    public static RelicChestSpawnSystem Instance { get; private set; }

    [Header("Kill Milestones")]
    [Tooltip("Sabit kill eşikleri. Array bittikten sonra büyüyen artışla devam eder.")]
    public int[] baseKillThresholds = { 75, 180, 340, 550 };

    [Header("Time Gates")]
    [Tooltip("İlk sandık için gereken minimum oturum süresi (saniye).")]
    public float firstChestMinTime = 90f;
    [Tooltip("İkinci ve sonraki sandıklar için önceki sandıktan itibaren gereken minimum süre (saniye).")]
    public float minSecondsBetweenChests = 120f;

    [Header("Placement")]
    [Tooltip("Sandık oyuncudan en az kaç birim uzağa spawn olsun?")]
    public float minDistanceFromPlayer = 6f;
    [Tooltip("Sandık oyuncudan en fazla kaç birim uzağa spawn olsun?")]
    public float maxDistanceFromPlayer = 8f;
    [Tooltip("Geçerli spawn noktası bulmak için kaç deneme yapılsın?")]
    public int placementAttempts = 10;
    [Tooltip("Zemine raycast atarken başlangıç yüksekliği.")]
    public float groundProbeHeight = 12f;
    [Tooltip("Zemin arama raycast mesafesi.")]
    public float groundProbeDistance = 30f;
    [Tooltip("Sandığı zeminin biraz üstüne almak için Y offset.")]
    public float groundOffset = 0.05f;

    const string MainMenuSceneName     = "MainMenu";
    const string StudioSplashSceneName = "StudioSplash";

    // Beyond-array increment base; grows ×1.25 each extra chest
    const int BeyondBaseIncrement = 260;

    int        _totalNormalKillsThisRun;
    int        _chestIndex;              // how many chests have been spawned this run
    float      _lastChestConsumedTime;   // Time.time when last chest was opened (-9999 = never)
    GameObject _activeChest;
    GameObject _playerRef;

    // ── Static entry points ───────────────────────────────────────────────────

    public static RelicChestSpawnSystem EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<RelicChestSpawnSystem>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        var go = new GameObject("RelicChestSpawnSystem");
        return go.AddComponent<RelicChestSpawnSystem>();
    }

    public static void RegisterEnemyKill(Vector3 deathPosition, bool isBoss)
    {
        EnsureInstance()?.HandleEnemyKill(deathPosition, isBoss);
    }

    public static void NotifyChestConsumed(RelicChest chest)
    {
        if (Instance == null || chest == null) return;
        if (Instance._activeChest != chest.gameObject) return;
        Instance._activeChest           = null;
        Instance._lastChestConsumedTime = Time.time;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetForNewRun();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetForNewRun();
    }

    public void ResetForNewRun()
    {
        _totalNormalKillsThisRun = 0;
        _chestIndex              = 0;
        _lastChestConsumedTime   = -9999f;
        _activeChest             = null;
        _playerRef               = null;
    }

    // ── Kill tracking ─────────────────────────────────────────────────────────

    void HandleEnemyKill(Vector3 deathPosition, bool isBoss)
    {
        if (isBoss)
            return;

        if (!IsGameplaySceneActive())
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        _totalNormalKillsThisRun++;

        if (!CanSpawnChest())
            return;

        SpawnChestNearPlayer(deathPosition);
    }

    bool IsGameplaySceneActive()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return !string.IsNullOrEmpty(sceneName)
               && sceneName != MainMenuSceneName
               && sceneName != StudioSplashSceneName;
    }

    // ── Threshold & time gate ─────────────────────────────────────────────────

    // Returns the kill count required to spawn the next chest (index = _chestIndex).
    int GetNextKillThreshold()
    {
        if (_chestIndex < baseKillThresholds.Length)
            return baseKillThresholds[_chestIndex];

        // Beyond the base array: start from last entry and grow each step by ×1.25
        int beyondCount = _chestIndex - baseKillThresholds.Length + 1;
        int total       = baseKillThresholds[baseKillThresholds.Length - 1];
        int increment   = BeyondBaseIncrement;
        for (int i = 0; i < beyondCount; i++)
        {
            total    += increment;
            increment = Mathf.RoundToInt(increment * 1.25f);
        }
        return total;
    }

    bool CanSpawnChest()
    {
        if (_activeChest != null)
            return false;

        if (_totalNormalKillsThisRun < GetNextKillThreshold())
            return false;

        if (_chestIndex == 0)
        {
            // First chest: minimum game-session time must have elapsed
            if (GameManager.Instance == null)
                return false;
            if (GameManager.Instance.ElapsedTime < firstChestMinTime)
                return false;
        }
        else
        {
            // Subsequent chests: cooldown starts when the previous chest was consumed
            if (_lastChestConsumedTime < 0f)
                return false;  // previous chest was never opened
            if (Time.time - _lastChestConsumedTime < minSecondsBetweenChests)
                return false;
        }

        return true;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    void SpawnChestNearPlayer(Vector3 fallbackPosition)
    {
        if (!TryFindSpawnPosition(fallbackPosition, out Vector3 spawnPosition))
            spawnPosition = fallbackPosition + Vector3.right * Mathf.Max(1f, minDistanceFromPlayer);

        var chestObject = new GameObject("RelicChest_AutoSpawn");
        chestObject.transform.position = spawnPosition;

        var chest = chestObject.AddComponent<RelicChest>();
        chest.interactRadius          = 2.5f;
        chest.createPlaceholderVisual = true;
        chest.visualScale             = 1f;

        _activeChest = chestObject;
        _chestIndex++;

        Debug.Log($"[RelicChestSpawnSystem] Chest #{_chestIndex} spawned at {_totalNormalKillsThisRun} kills, {spawnPosition}. Next threshold: {GetNextKillThreshold()}.");
    }

    bool TryFindSpawnPosition(Vector3 fallbackPosition, out Vector3 spawnPosition)
    {
        var player = ResolvePlayer();
        Vector3 center = player != null ? player.transform.position : fallbackPosition;

        float minDistance = Mathf.Max(1f, minDistanceFromPlayer);
        float maxDistance = Mathf.Max(minDistance, maxDistanceFromPlayer);
        int   attempts    = Mathf.Max(1, placementAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float baseAngle = (360f / attempts) * i;
            float jitter    = Random.Range(-18f, 18f);
            float angle     = (baseAngle + jitter) * Mathf.Deg2Rad;
            float distance  = Random.Range(minDistance, maxDistance);

            Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (TryProjectToGround(candidate, out spawnPosition))
                return true;
        }

        spawnPosition = center + Vector3.right * minDistance + Vector3.up * groundOffset;
        return true;
    }

    bool TryProjectToGround(Vector3 candidate, out Vector3 groundedPosition)
    {
        Vector3 origin   = candidate + Vector3.up * Mathf.Max(1f, groundProbeHeight);
        float   distance = Mathf.Max(1f, groundProbeDistance);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            groundedPosition = hit.point + Vector3.up * groundOffset;
            return true;
        }

        groundedPosition = candidate + Vector3.up * groundOffset;
        return true;
    }

    GameObject ResolvePlayer()
    {
        if (_playerRef != null)
            return _playerRef;

        var movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
            _playerRef = movement.gameObject;

        return _playerRef;
    }
}
