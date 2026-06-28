using UnityEngine;
using UnityEngine.SceneManagement;

// Atlas Kalıntı Sandığı spawn sistemi — MVP.
// Kural: Bir run'da en fazla 1 sandık spawn olur.
// Tetik: 20 normal düşman öldürüldüğünde (boss sayılmaz).
// Yerleşim: oyuncudan 6–8 birim uzağa, oyuncunun üstüne değil, okunabilir bir noktaya.
// Açıldığında tekrar spawn olmaz; yeni run (restart/scene reload) state'i sıfırlar.
// Büyük bir director/event sistemi YOK — bilinçli olarak basit ve izole tutuldu.
public class RelicChestSpawnSystem : MonoBehaviour
{
    public static RelicChestSpawnSystem Instance { get; private set; }

    [Header("Spawn Rule")]
    [Tooltip("Sandığın spawn olması için gereken normal düşman öldürme sayısı.")]
    public int killsToSpawnChest = 20;
    [Tooltip("Bir run içinde spawn edilebilecek maksimum sandık sayısı (MVP = 1).")]
    public int maxChestsPerRun = 1;

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

    int        _totalNormalKillsThisRun;
    int        _chestsSpawnedThisRun;
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
        // Sandık açıldı/tüketildi. Sayaç dolu olduğu için (maxChestsPerRun) aynı
        // run'da ikinci sandık spawn olmaz — sadece aktif referansı temizliyoruz.
        Instance._activeChest = null;
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
        _chestsSpawnedThisRun    = 0;
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

    bool CanSpawnChest()
    {
        // Run başına maksimum sandık kotası dolduysa bir daha spawn etme.
        if (_chestsSpawnedThisRun >= Mathf.Max(1, maxChestsPerRun))
            return false;

        // Halen açılmamış aktif sandık varsa yenisini spawn etme.
        if (_activeChest != null)
            return false;

        // Kill eşiği henüz dolmadıysa bekle.
        if (_totalNormalKillsThisRun < Mathf.Max(1, killsToSpawnChest))
            return false;

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
        _chestsSpawnedThisRun++;

        Debug.Log($"[RelicChestSpawnSystem] Chest spawned at {_totalNormalKillsThisRun} kills, pos {spawnPosition}.");
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
