// ─────────────────────────────────────────────────────────────────────────────
//  WeaponInventory.cs
//  Assets/Scripts/Systems/WeaponInventory.cs
//
//  Oyuncunun hangi silahlara sahip olduğunu ve her silahın kaçıncı
//  level'da olduğunu tutar. LevelUpCardSystem ve CardOfferGenerator bunu okur.
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    None,
    KatanaAura,   // AutoAttackAura — başlangıçta herkesin elinde
    RuhKunai,     // Projectile silah
    AtlasSphere,  // Orbit silah
}

public class WeaponInventory : MonoBehaviour
{
    public static WeaponInventory Instance { get; private set; }

    // Sahip olunan silahlar ve seviyeleri (0 = yok, 1-8 = seviye)
    readonly Dictionary<WeaponType, int> _levels   = new();
    readonly HashSet<WeaponType>         _awakened = new();

    const int MaxWeaponLevel = 8;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("[WeaponInventory]");
        Instance = go.AddComponent<WeaponInventory>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Oyuncu her zaman KatanaAura Lv1 ile başlar
        if (!_levels.ContainsKey(WeaponType.KatanaAura))
            _levels[WeaponType.KatanaAura] = 1;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool HasWeapon(WeaponType w)         => _levels.ContainsKey(w) && _levels[w] > 0;
    public int  GetLevel(WeaponType w)          => _levels.TryGetValue(w, out int lv) ? lv : 0;
    public bool IsMaxLevel(WeaponType w)        => GetLevel(w) >= MaxWeaponLevel;
    public int  OwnedWeaponCount               => _levels.Count;
    public IEnumerable<WeaponType> OwnedWeapons() => _levels.Keys;
    public bool IsWeaponAwakened(WeaponType w)  => _awakened.Contains(w);

    /// <summary>Silahı uyanmış olarak işaretler. Idempotent — aynı silah iki kez
    /// uyanamaz, tekrar çağrı sessiz geçer (yalnız ilk seferde loglar).</summary>
    public void MarkWeaponAwakened(WeaponType w)
    {
        if (!_awakened.Add(w)) return; // zaten uyanmış → tekrar loglama/uygulama yok
        Debug.Log($"[Awakening] {AwakenedDisplayName(w)} awakened");
    }

    static string AwakenedDisplayName(WeaponType w) => w switch
    {
        WeaponType.KatanaAura  => "Katana Aura",
        WeaponType.RuhKunai    => "Ruh Kunai",
        WeaponType.AtlasSphere => "Atlas Sphere",
        _                      => w.ToString()
    };

    /// <summary>
    /// Silahı edinir (Lv1'den başlar) veya mevcut seviyeyi 1 artırır.
    /// Gerçek bileşeni Player üzerinde de aktive eder.
    /// </summary>
    public void UnlockOrUpgrade(WeaponType w, GameObject player)
    {
        if (!_levels.ContainsKey(w))
        {
            _levels[w] = 1;
            ActivateWeapon(w, player);
        }
        else if (_levels[w] < MaxWeaponLevel)
        {
            _levels[w]++;
            if (w != WeaponType.KatanaAura)
                Debug.Log($"[WeaponInventory] Upgrade {w} Lv {_levels[w]}");
            ApplyLevelStats(w, _levels[w], player);
        }
    }

    /// <summary>Run sona erince envanteri sıfırla (KatanaAura Lv1 kalır).</summary>
    public void ResetForNewRun()
    {
        _levels.Clear();
        _levels[WeaponType.KatanaAura] = 1;
        _awakened.Clear();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    void ActivateWeapon(WeaponType w, GameObject player)
    {
        if (player == null) return;

        switch (w)
        {
            case WeaponType.RuhKunai:
                if (player.GetComponent<RuhKunai>() == null)
                    player.AddComponent<RuhKunai>();
                Debug.Log("[WeaponInventory] Unlock RuhKunai Lv 1");
                break;

            case WeaponType.AtlasSphere:
                if (player.GetComponent<AtlasSphere>() == null)
                    player.AddComponent<AtlasSphere>();
                Debug.Log("[WeaponInventory] Unlock AtlasSphere Lv 1");
                break;
        }

        ApplyLevelStats(w, 1, player);
    }

    void ApplyLevelStats(WeaponType w, int level, GameObject player)
    {
        if (player == null) return;

        switch (w)
        {
            case WeaponType.KatanaAura:
                ApplyKatanaStats(level, player);
                break;
            case WeaponType.RuhKunai:
                ApplyKunaiStats(level, player);
                break;
            case WeaponType.AtlasSphere:
                ApplyAtlasStats(level, player);
                break;
        }
    }

    // ── KatanaAura level tablosu ──────────────────────────────────────────────
    // Lv1 başlangıç: damage=10, radius=2.2, tickInterval=0.5
    void ApplyKatanaStats(int level, GameObject player)
    {
        var aura = player.GetComponent<AutoAttackAura>();
        if (aura == null) return;

        // Her level-up delta değerleri (VS: flat artış)
        switch (level)
        {
            case 2: aura.damage += 4;  aura.radius += 0.2f; break;
            case 3: aura.damage += 5;  aura.tickInterval = Mathf.Max(0.35f, aura.tickInterval - 0.05f); break;
            case 4: aura.damage += 6;  aura.radius += 0.25f; break;
            case 5: aura.damage += 8;  aura.tickInterval = Mathf.Max(0.28f, aura.tickInterval - 0.05f); break;
            case 6: aura.damage += 10; aura.radius += 0.3f; break;
            case 7: aura.damage += 12; aura.tickInterval = Mathf.Max(0.22f, aura.tickInterval - 0.05f); break;
            case 8: aura.damage += 15; aura.radius += 0.4f; aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval - 0.05f); break;
        }
    }

    // ── RuhKunai level tablosu ────────────────────────────────────────────────
    // Lv1: 1 kunai, Lv4: 2 kunai, Lv7: 3 kunai
    void ApplyKunaiStats(int level, GameObject player)
    {
        var kunai = player.GetComponent<RuhKunai>();
        if (kunai == null) return;

        switch (level)
        {
            case 2: kunai.damage += 5; break;
            case 3: kunai.fireInterval = Mathf.Max(0.6f, kunai.fireInterval - 0.1f); break;
            case 4: kunai.projectileCount = 2; kunai.damage += 5; break;
            case 5: kunai.damage += 8; break;
            case 6: kunai.fireInterval = Mathf.Max(0.4f, kunai.fireInterval - 0.1f); break;
            case 7: kunai.projectileCount = 3; kunai.damage += 8; break;
            case 8: kunai.damage += 12; kunai.fireInterval = Mathf.Max(0.3f, kunai.fireInterval - 0.1f); break;
        }
    }

    // ── AtlasSphere level tablosu ─────────────────────────────────────────────
    // Lv1: 1 küre, Lv5: 2 küre
    void ApplyAtlasStats(int level, GameObject player)
    {
        var sphere = player.GetComponent<AtlasSphere>();
        if (sphere == null) return;

        switch (level)
        {
            case 2: sphere.damage += 4; break;
            case 3: sphere.orbitSpeed += 30f; break;
            case 4: sphere.damage += 6; sphere.orbitRadius += 0.2f; break;
            case 5: sphere.sphereCount = 2; sphere.damage += 4; break;
            case 6: sphere.orbitSpeed += 30f; sphere.damage += 6; break;
            case 7: sphere.damage += 8; break;
            case 8: sphere.sphereCount = 3; sphere.damage += 8; sphere.orbitSpeed += 20f; break;
        }
    }
}
