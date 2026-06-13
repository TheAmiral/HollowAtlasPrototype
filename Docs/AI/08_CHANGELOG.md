# Changelog

## 2026-06-14

### VS-Style Weapon System Added
- **WeaponInventory.cs** — yeni singleton; silah slotlarını ve seviye takibini yönetir
- **RuhKunai.cs** — ilk VS tarzı silah; fırlatma/dönüş mekaniği
- **AtlasSphere.cs** — ikinci VS tarzı silah; orbital yörünge mekaniği
- **CardType** enum'una `WeaponUpgrade / Passive / Chaos` tipleri eklendi
- **WeaponType** enum'una `KatanaAura / RuhKunai / AtlasSphere` girişleri eklendi
- `CardDefinition.cs` güncellendi: `weaponType` ve `weaponLevel` alanları eklendi
- Duplicate enum tanımları (`WeaponType`, `CardType`) `CardEnums.cs`'ten kaldırıldı
- Derleme: **0 hata**
- Branch: `main` — commit `be2b0026`
