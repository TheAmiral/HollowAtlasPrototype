# Changelog

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
