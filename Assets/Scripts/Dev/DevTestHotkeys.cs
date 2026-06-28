// ═════════════════════════════════════════════════════════════════════════════
//  WARNING: Temporary debug panel. EDITOR-ONLY — yalnız Unity Editor'da görünür;
//           normal build ve demo/development build'lerde tamamen kapalıdır.
//           (Build'de açmak gerekirse EnableDevPanelInReleaseBuild = true.)
// ═════════════════════════════════════════════════════════════════════════════
//
//  DevTestHotkeys.cs   (GEÇİCİ / TEMP DEV TOOL — manuel kart seçici)
//  Assets/Scripts/Dev/DevTestHotkeys.cs
//
//  Ekranda mouse ile tıklanabilir bir DEV CARD PANEL. Tüm test edilebilir kartlar
//  (silah / pasif / relic / uyanış) gruplar halinde listelenir; [Uygula] butonu
//  gerçek upgrade mantığını çalıştırır (gameplay bypass YOK). Ayrıca gold/level/
//  timeScale/heal/dump için Utility butonları vardır.
//
//  Sahneye ELLE eklemeye gerek yok: RuntimeInitializeOnLoadMethod ile Play Mode
//  başlar başlamaz kendini otomatik kurar. Klavye/F-tuşu YOK; panel tamamen mouse
//  ile kullanılır. Uygulamalar mevcut sistemin kod yolunu kullanır:
//    • Silah  → WeaponInventory.UnlockOrUpgrade
//    • Pasif  → CardRewardApplier.Apply (CardDatabase.GetPassiveCards)
//    • Relic / Uyanış → CardRewardApplier.Apply (GetChestCards / GetWeaponAwakenings)
//  Build HUD (MainHudCanvasUI) state değişince signature-hash ile otomatik yenilenir.
//
//  TEMP: Enabled in normal builds for gameplay testing. Remove before release.
//  Eskiden tüm dosya #if UNITY_EDITOR || DEVELOPMENT_BUILD ile sarılıydı; bu yüzden
//  normal (non-development) build'de sınıf tamamen derlemeden çıkıyordu. Artık
//  aktivasyon EnableDevPanelInReleaseBuild const'una bağlı, preprocessor ile SİLİNMEZ.
//
//  Reflection KULLANMAZ, private alana DOKUNMAZ — yalnız mevcut public API.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DevTestHotkeys : MonoBehaviour
{
    // Dev panel artık YALNIZ Unity Editor'da görünür. Normal build ve demo /
    // development build'lerde tamamen kapalıdır. Build'de göstermek gerekirse
    // bu const true yapılabilir.
    private const bool EnableDevPanelInReleaseBuild = false;

    // Yalnız Unity Editor'da açık; tüm build türlerinde (development dahil) kapalı.
    static bool DevPanelEnabled =>
#if UNITY_EDITOR
        true;
#else
        EnableDevPanelInReleaseBuild;
#endif

    const int GoldGrant = 500;

    static readonly WeaponType[] WeaponList =
        { WeaponType.KatanaAura, WeaponType.RuhKunai, WeaponType.AtlasSphere };

    static readonly string[] PassiveIds =
    {
        "kan_yemini", "ruzgar_adimi", "demir_beden", "zaman_kirigi",
        "buyuyen_yanki", "ruh_miknatis", "atlas_bereketi", "lanetli_servet"
    };

    // Dev panel görünürlüğü için uyanış meta-tablosu. Koşullar CardDatabase
    // .GetWeaponAwakenings ile AYNIDIR — burası yalnız DISPLAY içindir; gerçek
    // uygulama her zaman GetWeaponAwakenings'ten gelen kartı + CardRewardApplier'ı
    // kullanır (koşul/etki tek kaynak CardDatabase'de kalır).
    struct AwakeningInfo
    {
        public WeaponType weapon;
        public string     name;        // uyanış adı (HUD ile aynı)
        public string[]   anyPassive;  // bunlardan biri yeterli
    }

    static readonly AwakeningInfo[] Awakenings =
    {
        new AwakeningInfo { weapon = WeaponType.KatanaAura,  name = "Kanlı Hilal",   anyPassive = new[] { "kan_yemini",   "buyuyen_yanki" } },
        new AwakeningInfo { weapon = WeaponType.RuhKunai,    name = "Ruh Fırtınası", anyPassive = new[] { "zaman_kirigi", "ruh_miknatis"  } },
        new AwakeningInfo { weapon = WeaponType.AtlasSphere, name = "Atlas Halosu",  anyPassive = new[] { "buyuyen_yanki","demir_beden"   } },
    };

    GameObject _player;

    // ── Panel state ─────────────────────────────────────────────────────────────
    bool _panelOpen = true;
    Vector2 _scroll;
    float _lastBuild = -999f;
    bool _forceRebuild = true;
    List<DevGroup> _groups;

    // ── GUI styles ──────────────────────────────────────────────────────────────
    bool _stylesReady;
    GUIStyle _areaStyle, _titleStyle, _warnStyle, _groupStyle;
    GUIStyle _cardNameStyle, _cardDetailStyle, _cardButtonStyle, _utilButtonStyle, _toggleStyle;
    Texture2D _bgTex;

    sealed class DevRow
    {
        public string name;
        public string detail;       // "Lv 4 → 5   |   +8 Hasar"
        public bool   actionable;   // false → buton disable
        public string buttonText;   // "Uygula" / "MAX" / "DOLU" / "—"
        public Action apply;
        public bool   utility;      // tam genişlik buton
    }

    sealed class DevGroup
    {
        public string title;
        public readonly List<DevRow> rows = new();
    }

    // ── Bootstrap: Play Mode başlayınca kendini otomatik kurar ──────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!DevPanelEnabled) return;

        // Sahneye elle de eklenmiş olabilir → çift instance oluşturma.
        // (Unity 6 uyumlu, FindObjectOfType obsolete → FindFirstObjectByType.)
        if (FindFirstObjectByType<DevTestHotkeys>() != null) return;

        var go = new GameObject("[DEV] Card Panel");
        DontDestroyOnLoad(go);
        go.AddComponent<DevTestHotkeys>();
        Debug.Log("[DevPanel] Bootstrap created");
    }

    void Awake()
    {
        Debug.Log("[DevPanel] Awake");
        Debug.Log("[DevPanel] HOLLOW ATLAS DEV CARD PANEL - TEMP BUILD ENABLED");
    }

    void Update()
    {
        if (!DevPanelEnabled || !_panelOpen) return;

        // Kart listesini fazla GC üretmeden tazele: anlık apply'dan sonra zorla,
        // aksi halde 0.2 sn'de bir (normal gameplay level-up'larını da yakalar).
        if (_forceRebuild || Time.unscaledTime - _lastBuild > 0.2f)
        {
            _forceRebuild = false;
            _lastBuild = Time.unscaledTime;
            RebuildRows();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Kart listesini (gruplar halinde) kur
    // ─────────────────────────────────────────────────────────────────────────
    void RebuildRows()
    {
        _groups ??= new List<DevGroup>();
        _groups.Clear();

        var inv = WeaponInventory.Instance;
        var rls = RunLoadoutSystem.Instance;

        BuildWeaponGroup(inv);
        BuildPassiveGroup(rls);
        BuildRelicGroup(rls);
        BuildAwakeningGroup(inv, rls);
        BuildUtilityGroup();
    }

    void BuildWeaponGroup(WeaponInventory inv)
    {
        var g = new DevGroup { title = "SİLAHLAR" };
        var cards = inv != null ? CardDatabase.GetWeaponCards(inv) : null;

        foreach (var wt in WeaponList)
        {
            string name = CardDatabase.GetWeaponName(wt);
            var row = new DevRow { name = name, buttonText = "—" };

            if (inv == null)
            {
                row.detail = "WeaponInventory not found";
            }
            else
            {
                int cur   = inv.GetLevel(wt);
                bool maxed = inv.IsMaxLevel(wt);
                var card  = cards?.Find(c => c.weaponType == wt);
                string preview = card != null ? card.effectPreview : "—";

                if (maxed)
                {
                    row.detail = $"Lv {cur} / Max   |   {preview}";
                    row.buttonText = "MAX";
                }
                else
                {
                    int next = card != null ? card.weaponLevel : cur + 1;
                    row.detail = (cur == 0 ? "Aç → Lv 1" : $"Lv {cur} → {next}") + $"   |   {preview}";
                    row.buttonText = "Uygula";
                    row.actionable = true;
                    var captured = wt;
                    row.apply = () => ApplyWeapon(captured);
                }
            }
            g.rows.Add(row);
        }
        _groups.Add(g);
    }

    void BuildPassiveGroup(RunLoadoutSystem rls)
    {
        var g = new DevGroup { title = "PASİFLER" };
        var cards = rls != null ? CardDatabase.GetPassiveCards(rls) : null;

        foreach (var id in PassiveIds)
        {
            string name = CardDatabase.GetPassiveName(id);
            var row = new DevRow { name = name, buttonText = "—" };

            if (rls == null)
            {
                row.detail = "RunLoadoutSystem not found";
            }
            else
            {
                int cur   = rls.GetPassiveLevel(id);
                bool maxed = rls.IsPassiveMaxLevel(id);
                var card  = cards?.Find(c => c.passiveId == id);
                string preview = card != null ? card.effectPreview : "—";

                if (maxed)
                {
                    row.detail = $"Lv {cur} / Max   |   {preview}";
                    row.buttonText = "MAX";
                }
                else if (card != null)
                {
                    int next = card.passiveLevel;
                    row.detail = (cur == 0 ? "Aç → Lv 1" : $"Lv {cur} → {next}") + $"   |   {preview}";
                    row.buttonText = "Uygula";
                    row.actionable = true;
                    var capturedId = id;
                    var capturedCard = card;
                    row.apply = () => ApplyPassive(capturedId, capturedCard);
                }
                else
                {
                    // Sahip değil + pasif slotu dolu (6/6) → eklenemiyor.
                    row.detail = $"Lv {cur}   |   Pasif slotu dolu (6/6)";
                    row.buttonText = "DOLU";
                }
            }
            g.rows.Add(row);
        }
        _groups.Add(g);
    }

    void BuildRelicGroup(RunLoadoutSystem rls)
    {
        var g = new DevGroup { title = "RELİC / KALINTILAR" };
        foreach (var card in CardDatabase.GetChestCards())
        {
            bool owned = rls != null && rls.HasChestReward(card.id);
            var captured = card;
            g.rows.Add(new DevRow
            {
                name       = card.title + (owned ? "  ✓" : ""),
                detail     = $"Kalıntı   |   {card.effectPreview}",
                actionable = true,
                buttonText = "Uygula",
                apply      = () => ApplyOneShot(captured),
            });
        }
        _groups.Add(g);
    }

    void BuildAwakeningGroup(WeaponInventory inv, RunLoadoutSystem rls)
    {
        var g = new DevGroup { title = "UYANIŞLAR / ÖZEL" };

        // Sadece koşulu sağlanan (Lv8 + pasif + henüz uyanmamış) uyanışlar [Uygula]
        // alır; uyanmış olanlar [UYANDI], koşulu eksik olanlar nedeniyle [KİLİTLİ]
        // disabled görünür. Apply gerçek kartı (GetWeaponAwakenings) kullanır.
        var eligible = (inv != null && rls != null)
            ? CardDatabase.GetWeaponAwakenings(inv, rls)
            : null;

        foreach (var info in Awakenings)
        {
            var row = new DevRow { name = info.name, buttonText = "—" };

            if (inv == null || rls == null)
            {
                row.detail = "WeaponInventory / RunLoadoutSystem not found";
            }
            else if (inv.IsWeaponAwakened(info.weapon))
            {
                row.detail = $"{CardDatabase.GetWeaponName(info.weapon)} → uyandı";
                row.buttonText = "UYANDI"; // disabled
            }
            else
            {
                var card = eligible?.Find(c => c.weaponType == info.weapon);
                if (card != null)
                {
                    row.detail = $"{CardDatabase.GetWeaponName(info.weapon)} Lv8 hazır   |   {card.effectPreview}";
                    row.buttonText = "Uygula";
                    row.actionable = true;
                    var captured = card;
                    row.apply = () => ApplyOneShot(captured);
                }
                else
                {
                    string reason = !inv.IsMaxLevel(info.weapon)
                        ? $"Silah Lv8 değil (Lv {inv.GetLevel(info.weapon)})"
                        : "Gerekli pasif yok: " +
                          string.Join(" / ", System.Array.ConvertAll(info.anyPassive, CardDatabase.GetPassiveName));
                    row.detail = $"Kilitli — {reason}";
                    row.buttonText = "KİLİTLİ"; // disabled
                }
            }

            g.rows.Add(row);
        }

        _groups.Add(g);
    }

    void BuildUtilityGroup()
    {
        var g = new DevGroup { title = "UTILITY / TEST" };
        g.rows.Add(Util("+500 Gold",        GiveGold));
        g.rows.Add(Util("Level Up Card Aç", GiveLevelUp));
        g.rows.Add(Util("TimeScale = 1",    ForceResume));
        g.rows.Add(Util("Full Heal",        FullHeal));
        g.rows.Add(Util("Dump Build",       DumpStatus));
        _groups.Add(g);

        static DevRow Util(string label, Action a) =>
            new DevRow { name = label, utility = true, actionable = true, buttonText = label, apply = a };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Kart uygulama (gerçek kod yolu) — kart seçim ekranı açıkken engellenir
    // ─────────────────────────────────────────────────────────────────────────
    bool CanApplyCard()
    {
        if (LevelUpCardSystem.IsSelectionOpen)
        {
            Debug.Log("[DevPanel] Card selection screen is open; close it before manual apply.");
            return false;
        }
        if (ResolvePlayer() == null)
        {
            Debug.LogWarning("[DevPanel] Player not found");
            return false;
        }
        return true;
    }

    void ApplyWeapon(WeaponType wt)
    {
        if (!CanApplyCard()) return;

        var inv = WeaponInventory.Instance;
        if (inv == null) { Debug.LogWarning("[DevPanel] WeaponInventory not found"); return; }

        string name = CardDatabase.GetWeaponName(wt);
        if (inv.IsMaxLevel(wt)) { Debug.Log($"[DevPanel] {name} already max level"); return; }

        int before = inv.GetLevel(wt);
        inv.UnlockOrUpgrade(wt, ResolvePlayer());   // gerçek silah upgrade yolu
        int after = inv.GetLevel(wt);
        Debug.Log($"[DevPanel] Applied card: {name} Lv {before} -> {after}");
        _forceRebuild = true;
    }

    void ApplyPassive(string id, CardDefinition card)
    {
        if (!CanApplyCard()) return;

        var rls = RunLoadoutSystem.Instance;
        if (rls == null) { Debug.LogWarning("[DevPanel] RunLoadoutSystem not found"); return; }

        string name = CardDatabase.GetPassiveName(id);
        if (rls.IsPassiveMaxLevel(id)) { Debug.Log($"[DevPanel] {name} already max level"); return; }
        if (card == null) { Debug.Log($"[DevPanel] {name} card unavailable (passive slots full?)"); return; }

        int before = rls.GetPassiveLevel(id);
        CardRewardApplier.Apply(card, ResolvePlayer()); // UnlockPassive/UpgradePassive + gerçek effect
        int after = rls.GetPassiveLevel(id);
        Debug.Log($"[DevPanel] Applied card: {name} Lv {before} -> {after}");
        _forceRebuild = true;
    }

    void ApplyOneShot(CardDefinition card)
    {
        if (card == null) return;
        if (!CanApplyCard()) return;

        CardRewardApplier.Apply(card, ResolvePlayer()); // relic/uyanış: gerçek Apply + kayıt
        Debug.Log($"[DevPanel] Applied card: {card.title}");
        _forceRebuild = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utility (kart seçim ekranından bağımsız çalışır; Level Up hariç)
    // ─────────────────────────────────────────────────────────────────────────
    void GiveGold()
    {
        var player = ResolvePlayer();
        if (player == null) { Debug.LogWarning("[DevPanel] Player not found"); return; }

        var wallet = player.GetComponentInChildren<GoldWallet>();
        if (wallet == null) { Debug.LogWarning("[DevPanel] GoldWallet not found"); return; }

        wallet.AddGold(GoldGrant);
        Debug.Log($"[DevPanel] +{GoldGrant} gold | RunGold: {wallet.RunGold}");
    }

    void GiveLevelUp()
    {
        if (LevelUpCardSystem.IsSelectionOpen)
        {
            Debug.Log("[DevPanel] Card selection screen is open; close it before manual apply.");
            return;
        }

        var player = ResolvePlayer();
        if (player == null) { Debug.LogWarning("[DevPanel] Player not found"); return; }

        var ls = player.GetComponentInChildren<PlayerLevelSystem>();
        if (ls == null) { Debug.LogWarning("[DevPanel] PlayerLevelSystem not found"); return; }

        int before = ls.Level;
        ls.FillXPToNextLevel();
        Debug.Log($"[DevPanel] Level Up triggered: {before} -> {ls.Level} (card screen should open)");
    }

    void ForceResume()
    {
        Time.timeScale = 1f;
        Debug.Log("[DevPanel] Time.timeScale = 1");
    }

    void FullHeal()
    {
        var player = ResolvePlayer();
        if (player == null) { Debug.LogWarning("[DevPanel] Player not found"); return; }

        var hp = player.GetComponentInChildren<PlayerHealth>();
        if (hp == null) { Debug.LogWarning("[DevPanel] PlayerHealth not found"); return; }

        hp.Heal(hp.MaxHealth); // Heal() maxHealth'a clamp eder → tam dolar
        Debug.Log($"[DevPanel] HP full: {hp.CurrentHealth}/{hp.MaxHealth}");
    }

    void DumpStatus()
    {
        var player = ResolvePlayer();
        var ls     = player != null ? player.GetComponentInChildren<PlayerLevelSystem>() : null;
        var wallet = player != null ? player.GetComponentInChildren<GoldWallet>() : null;
        var inv    = WeaponInventory.Instance;
        var rls    = RunLoadoutSystem.Instance;

        if (player == null) Debug.LogWarning("[DevPanel] Player not found");
        if (ls == null)     Debug.LogWarning("[DevPanel] PlayerLevelSystem not found");
        if (rls == null)    Debug.LogWarning("[DevPanel] RunLoadoutSystem not found");
        if (inv == null)    Debug.LogWarning("[DevPanel] WeaponInventory not found");

        var sb = new StringBuilder("[DevPanel] Status | ");
        sb.Append($"Player:{(player != null ? "OK" : "NULL")} ");
        sb.Append($"PlayerLevelSystem:{(ls != null ? $"OK(Lv{ls.Level})" : "NULL")} ");
        sb.Append($"RunLoadoutSystem:{(rls != null ? "OK" : "NULL")} ");
        sb.Append($"WeaponInventory:{(inv != null ? "OK" : "NULL")}");

        if (inv != null)
        {
            sb.Append(" | Weapons[");
            bool first = true;
            foreach (var w in inv.OwnedWeapons())
            {
                if (!first) sb.Append(", ");
                sb.Append($"{CardDatabase.GetWeaponName(w)} Lv{inv.GetLevel(w)}{(inv.IsWeaponAwakened(w) ? "*" : "")}");
                first = false;
            }
            sb.Append("]");
        }

        if (rls != null)
        {
            sb.Append(" Passives[");
            var po = rls.OrderedPassives;
            for (int i = 0; i < po.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"{CardDatabase.GetPassiveName(po[i])} Lv{rls.GetPassiveLevel(po[i])}");
            }
            sb.Append($"] Relics:{rls.ChestRewards.Count}");
            sb.Append($" | Gold x{rls.GoldPickupMultiplier:F2} Pickup x{rls.PickupRadiusMultiplier:F2} Pressure x{rls.EnemySpawnPressure:F2}");
        }

        if (ls != null)     sb.Append($" | XP x{ls.XpMultiplier:F2}");
        if (wallet != null) sb.Append($" | RunGold:{wallet.RunGold}");

        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ON-SCREEN DEV CARD PANEL (build'de okunaklı, mouse ile kullanılır)
    // ─────────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        if (!DevPanelEnabled) return;

        EnsureStyles();
        if (_groups == null) RebuildRows();

        const float pad = 12f;
        const float w   = 500f;   // 470-520 px aralığında geniş panel
        float x = Screen.width - w - pad;
        float areaH = _panelOpen ? (Screen.height - 2f * pad) : 64f;

        GUILayout.BeginArea(new Rect(x, pad, w, areaH), _areaStyle);

        // Büyük AÇ/KAPAT butonu — panel kapalıyken bile görünür.
        if (GUILayout.Button(_panelOpen ? "▼  DEV CARD PANEL — KAPAT" : "▶  DEV CARD PANEL — AÇ",
                _toggleStyle, GUILayout.Height(44)))
        {
            _panelOpen = !_panelOpen;
            _forceRebuild = true;
        }

        if (_panelOpen)
        {
            GUILayout.Label("HOLLOW ATLAS DEV CARD PANEL", _titleStyle);
            GUILayout.Label("TEMP BUILD ENABLED", _warnStyle);
            GUILayout.Space(6f);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            foreach (var grp in _groups)
            {
                GUILayout.Space(6f);
                GUILayout.Label(grp.title, _groupStyle);
                foreach (var row in grp.rows)
                    DrawRow(row);
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    void DrawRow(DevRow row)
    {
        if (row.utility)
        {
            if (GUILayout.Button(row.name, _utilButtonStyle, GUILayout.Height(42)))
                row.apply?.Invoke();
            GUILayout.Space(4f);
            return;
        }

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Label(row.name, _cardNameStyle);
        GUILayout.Label(row.detail, _cardDetailStyle);
        GUILayout.EndVertical();

        GUI.enabled = row.actionable;
        if (GUILayout.Button(row.buttonText, _cardButtonStyle, GUILayout.Width(92), GUILayout.Height(42)))
            row.apply?.Invoke();
        GUI.enabled = true;

        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _areaStyle = new GUIStyle(GUI.skin.box)
        {
            border  = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(10, 10, 10, 10),
        };
        _areaStyle.normal.background = BgTex();

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 23,                  // başlık 22-24
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap  = false,               // tek satırda kesilmesin
        };
        _titleStyle.normal.textColor = new Color(1f, 0.84f, 0.30f);

        _warnStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        _warnStyle.normal.textColor = new Color(1f, 0.50f, 0.40f);

        _groupStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 19,                  // grup başlığı 18-20
            fontStyle = FontStyle.Bold,
            margin    = new RectOffset(0, 0, 6, 2),
        };
        _groupStyle.normal.textColor = new Color(0.60f, 0.90f, 1f);

        _cardNameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,                  // kart satırı 15-16
            fontStyle = FontStyle.Bold,
            wordWrap  = true,
        };
        _cardNameStyle.normal.textColor = Color.white;

        _cardDetailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true,                 // uzun preview'da satır yükselir
        };
        _cardDetailStyle.normal.textColor = new Color(0.82f, 0.82f, 0.86f);

        _cardButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 16,                  // buton 16-18
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _utilButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _toggleStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
    }

    Texture2D BgTex()
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _bgTex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.94f));
            _bgTex.Apply();
        }
        return _bgTex;
    }

    GameObject ResolvePlayer()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        return _player;
    }
}
