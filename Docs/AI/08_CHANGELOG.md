# Changelog

## 2026-06-20

### Demo Loop Milestone — Prototype_01 oynanabilir demo (10 dk run → boss → reward → portal)

Branch `feature/vs-style-card-system`; 9 commit (`aa71814e` → `b5901910`), origin'e push edildi. End-to-end statik QA pass temiz; derleme 0 hata (Editor/build final playthrough bekliyor).

- **MainHudCanvasUI.cs** — HUD timer dakika.saniye formatı (`{minutes}.{seconds:00}`, örn. `1.30`); dash bar `Image.fillAmount` yerine RectTransform anchor ile **sağdan sola** dolum (XP barın tersi), `dashBarFillRect`.
- **EnemySpawner.cs** — zaman bazlı 5 fazlı pacing (`SpawnPhase[]`, Inspector'dan ayarlanabilir): 0-1 basic · 1-3 +fast · 3-5 +tank/shooter · 5-7 yoğun · 7-10 max. `maxAlive` tavanı (sonsuz spawn/FPS riski yok); eski difficultyRamp `usePhaseSystem=false` fallback olarak korundu. `[Wave]/[Spawner]` logları flag arkasında.
- **BossSpawnSystem.cs** + **Prototype_01.unity** — boss zamanlaması 5:00 (mini) / 10:00 (ölçeklenmiş) → `firstBossTime=300`, `waveInterval=300` (kod + sahne); spawn-öncesi "… GELİYOR" merkez uyarısı (`preSpawnWarned`); `[Boss]` logları `debugLogging` arkasında.
- **BossSpecialAttack.cs** — şarj sırasında büyüyen + yoğunlaşan **yarı saydam kırmızı yer telegraph diski** (`CreateTelegraphDisc/Update/Clear`); çarpışmasız (mermileri engellemez), kendi materyalini yönetir; Inspector: `telegraphRadius / telegraphColor / telegraphYOffset`. (Boss HP bar `BossHealthCanvasUI` + spawn geri sayımı `BossStatusUIController` zaten vardı, doğrulandı.)
- **PortalSpawnSystem.cs** — otomatik proximity → **E etkileşimi** (`Keyboard.current.eKey`); portal üstünde "[E] ATLAS PORTALINI AÇ" prompt; aktivasyonda portal tükenir + "ATLAS YOLU AÇILDI / PORTAL UYANDI" + `[Portal] Spawned/Activated`. `CanInteract` guard'ı: `timeScale<=0` + `SelectionPending` + `RewardPending` + relic seçimi. Rezonans HUD (`AtlasResonanceHudController`) korundu.
- **GameOverUIController.cs** — Game Over ekranına Ana Menü butonu (Restart + Ana Menü yan yana; `MakeButton` parametreli; Ana Menü → `GameManager.ReturnToMainMenu`, tam state reset).
- **PlayerHealth.cs** — her vuruştaki `Player damaged…` logu `debugLogs` (varsayılan **kapalı**) arkasına alındı → demo console temiz.
- **DevTestHotkeys.cs** (yeni) — geçici **manuel Dev Card Panel** (mouse, F tuşu değil); silah/pasif/relic/uyanış gerçek kod yoluyla uygulanır (`WeaponInventory.UnlockOrUpgrade`, `CardRewardApplier.Apply`); kart ekranı açıkken apply bloklu. ⚠️ `EnableDevPanelInReleaseBuild = true` — **public build öncesi `false` yapılmalı**; dosya başı uyarı blokları korunuyor.
- **CardDatabase.cs / LevelUpCardSystem.cs / RelicChest.cs / AtlasSphere.cs / RuhKunai.cs** — `effectPreview` metinleri gerçek effect değerleriyle hizalandı; kart seçim ekranı açıkken relic chest prompt/E ve dev panel apply çakışması giderildi (`IsSelectionOpen` guard'ları).
- **WeaponInventory.cs** — awakening state `_awakened` (HashSet) idempotent; `ResetForNewRun` hem `_levels` hem `_awakened`'i temizliyor → restart/new run'da uyanış sızmıyor.
- **QA / build:** timeScale tüm `=0` setter'larında restore'lu; reset tam (loadout/relic/awakening/portal/boss/ElapsedTime); build flow `StudioSplash → MainMenu → Prototype_01` doğrulandı.

## 2026-06-14 (3)

### VS Weapon System — Tam Entegrasyon (Çalışır Hale Getirildi)

**Sorun:** Önceki oturumda 50+ kartlık orijinal tanrı sistemi (Hermes/Nyx/Atlas/Ares/Artemis/Hephaistos/Khaos) silinerek yerine 15 kart konulmuş, hepsi `CardClass.Atlas` olarak işaretlenmiş. Sonuç: kart ekranında her kart "ATLAS" gösteriyordu, sistem bozulmuş görünüyordu.

**Yapılan düzeltmeler:**

- **CardDefinition.cs** — Orijinal yapı restore edildi. `CardType` enum eklendi (`WeaponUpgrade / Passive / Chaos`). `weaponType`, `weaponLevel`, `IsWeaponCard/IsPassiveCard/IsChaosCard` eklendi. `IsRandomLike` orijinal haliyle korundu (`cardClass == CardClass.Khaos || Random tag`). `CardVisualCategory`'ye `WeaponKatana / WeaponKunai / WeaponSphere` eklendi.

- **CardDatabase.cs** — Orijinal 50+ kart tam geri getirildi. `Build()` ve `Card()` helper'ı değiştirilmedi. `GetWeaponCards(WeaponInventory inv)` metodu eklendi; her silah için `WeaponUnlockCard()` / `WeaponLevelCard()` üretiyor. Lv1–Lv8 açıklamaları tanımlandı.

- **CardOfferGenerator.cs** — Orijinal rarity/sınıf-çeşitlilik sistemi korundu. Slot 1'e `WeaponInventory.Instance` varsa silah kartı enjekte ediliyor (önce upgrade, sonra yeni silah); yoksa 3 kart tamamen eskisi gibi çalışıyor.

- **CardRewardApplier.cs** — Silah kartları `WeaponInventory.UnlockOrUpgrade()` ile uygulanıyor; pasif/Khaos kartlar `Apply` action ile. Her ikisi için de `Snapshot.Diff` alınıyor. Kunai/küre stat takibi eklendi.

- **CardThemeLibrary.cs** — `GetVisualTheme()` ve `GetVisualCornerLabel()` switch'lerine `WeaponKatana` (turuncu), `WeaponKunai` (cyan), `WeaponSphere` (altın/mor) eklendi; köşe etiketi `"SİLAH"`.

- **WeaponInventory.cs** — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` eklendi; Inspector'a elle bileşen eklemeye gerek kalmadı, oyun başlarken otomatik oluşuyor.

- Derleme: **0 hata** — Play Mode'da test edildi, tüm sistem çalışıyor.

## 2026-06-14 (2)

### Weapon Cards — Kart Ekranına Entegrasyon
- **CardDefinition.cs** — `weaponType` alanı ve `IsWeaponCard` özelliği eklendi; `CreateCopy()` güncellendi
- **CardDatabase.cs** — `WeaponCard()` builder eklendi; `ruh_kunai` ve `atlas_sferi` kartları pool'a eklendi
- **CardOfferGenerator.cs** — `PickWeaponCard()` ve `BuildWeaponOffer()` eklendi; her seviye atlamada 1 garantili silah kartı; max level silahlar filtreleniyor; stat kartları silah kartlarını dışlıyor
- `effectPreview` dinamik: sahip olunmayan silahta "Yeni Silah — Kilidi Aç", mevcutta "Lv X → Lv X+1"

## 2026-06-14

### VS-Style Weapon System Added
- **WeaponInventory.cs** — yeni singleton; silah slotlarını ve seviye takibini yönetir
- **RuhKunai.cs** — ilk VS tarzı silah; fırlatma/dönüş mekaniği
- **AtlasSphere.cs** — ikinci VS tarzı silah; orbital yörünge mekaniği
- **WeaponType** enum'una `KatanaAura / RuhKunai / AtlasSphere` girişleri eklendi
- `CardDefinition.cs` güncellendi: `weaponType` ve `weaponLevel` alanları eklendi
- Duplicate enum tanımları `CardEnums.cs`'ten kaldırıldı
- Derleme: **0 hata**
- Branch: `main` — commit `be2b0026`
