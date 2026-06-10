using UnityEngine;
using UnityEngine.SceneManagement;

// Run başına kill istatistiklerini tutar.
// Boss kill sayılmaz. Sahne yüklenince veya ResetForNewRun çağrılınca sıfırlanır.
public class RunStatsSystem : MonoBehaviour
{
    public static RunStatsSystem Instance { get; private set; }

    public int KillsThisRun { get; private set; }

    // ── Static entry points ───────────────────────────────────────────────────

    public static RunStatsSystem EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<RunStatsSystem>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        var go = new GameObject("RunStatsSystem");
        return go.AddComponent<RunStatsSystem>();
    }

    public static void RegisterEnemyKill(bool isBoss)
    {
        EnsureInstance()?.HandleKill(isBoss);
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

    // ── Kill tracking ─────────────────────────────────────────────────────────

    void HandleKill(bool isBoss)
    {
        if (isBoss) return;
        KillsThisRun++;
    }

    public void ResetForNewRun()
    {
        KillsThisRun = 0;
    }
}
