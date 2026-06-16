# Current Status

**Last updated:** 2026-06-14

## Active Branch
`main`

## Recently Completed
- **VS Weapon System tam entegrasyonu tamamlandı ve doğrulandı** (Play Mode'da çalışıyor)
- Orijinal 50+ kartlık tanrı sistemi (Hermes/Nyx/Atlas/Ares/Artemis/Hephaistos/Khaos) geri getirildi
- Silah kartları (Katana Aura / Ruh Kunai / Atlas Küresi) level-up ekranına entegre edildi
- `WeaponInventory` artık sahnede objeye eklemeye gerek yok — `RuntimeInitializeOnLoadMethod` ile otomatik başlatılıyor
- `CardThemeLibrary` silah görsel temalarıyla güncellendi (turuncu/cyan/altın)

## Known State
- `RuhKunai` ve `AtlasSphere` prefabları yok; bileşenler `player.AddComponent<>()` ile runtime'da ekleniyor
- Kart ekranı UI: legacy `UnityEngine.UI.Text` (TextMeshPro'ya geçiş roadmap'te)

## Pending (Roadmap)
- TextMeshPro migrasyonu
- MetaShop (`bankGold` harcama ekranı)
- SaveSystem (PlayerPrefs)
- Minimap
- WeaponInventory run sonu sıfırlama entegrasyonu (`ResetForNewRun()` → GameManager'a bağla)
