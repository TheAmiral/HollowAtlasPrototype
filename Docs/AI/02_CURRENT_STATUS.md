# Current Status

**Last updated:** 2026-06-20

## Active Branch
`feature/vs-style-card-system` (origin'e push edildi)

## Demo Loop Milestone — DONE
Prototype_01 artık baştan sona oynanabilir demo loop:
**10 dk run → boss → reward → portal → game over / restart.**
End-to-end statik QA pass temiz; demo build alınabilir durumda (Editor/build final playthrough bekliyor — bkz. `Tasks/CURRENT_TASK.md`).

## Recently Completed
- **Dev Card Panel** eklendi (`DevTestHotkeys.cs`); normal build'de geçici olarak açık. F tuşu değil, **mouse ile manuel kart seçici**. Kartlar / pasifler / relicler / uyanışlar panelden manuel uygulanabiliyor (gerçek kod yolu, gameplay bypass yok).
- **Kart seçim ekranı çakışmaları giderildi:** kart ekranı açıkken relic chest prompt + E interaction ve dev panel apply bloklanıyor (`SelectionPending` / `IsSelectionOpen` guard'ları).
- **CardDatabase `effectPreview`** metinleri gerçek gameplay effect değerleriyle hizalandı.
- **Weapon Awakening** doğrulandı + polish edildi; uyanmış silah state'i `WeaponInventory._awakened` içinde tutuluyor, idempotent (silah iki kez uyanmaz), restart/new run sonrası temizleniyor.
- **Relic Chest reward flow** polish edildi.
- **Boss fight polish:** spawn-öncesi "… GELİYOR" merkez uyarısı, üst-orta boss HP bar, şarj sırasında yarı saydam **kırmızı yer telegraph'ı** (hasar uyarıdan sonra geliyor).
- **Boss reward → Portal:** reward sonrası portal spawn + rezonans HUD + "[E] ATLAS PORTALINI AÇ" prompt + E ile aktivasyon ("ATLAS YOLU AÇILDI"). Kart ekranı / timeScale guard'lı.
- **HUD timer** dakika.saniye formatına geçti (örn. `1.30`).
- **Dash bar** XP barın tersi yönde — **sağdan sola** dolacak şekilde düzeltildi.
- **10 dakikalık run pacing** (EnemySpawner faz sistemi, Inspector'dan ayarlanabilir):
  - 0-1 dk basic
  - 1-3 dk +fast
  - 3-5 dk +tank/shooter
  - 5-7 dk yoğun faz + mini-boss (5:00)
  - 7-10 dk max baskı
  - 10:00 ikinci (ölçeklenmiş) boss
- **Game Over** ekranına Ana Menü butonu eklendi (Restart + Ana Menü).
- **Log spam temizliği:** Player damage logu + weapon debug logları `debugLogs` flag'i arkasına alındı.

## Known State
- **Build flow:** `StudioSplash → MainMenu → Prototype_01` (üçü de Build Settings'te, doğru sırayla).
- ⚠️ **GEÇİCİ:** `DevTestHotkeys.cs` → `EnableDevPanelInReleaseBuild = true`. Test build için bilerek açık. **Public demo / Steam build öncesi `false` yapılmalı VEYA `DevTestHotkeys.cs` (+ .meta) silinmeli.** Dosya başındaki uyarı yorumları korunuyor.
- `RuhKunai` / `AtlasSphere` prefabsız; bileşenler runtime'da `player.AddComponent<>()` ile ekleniyor.
- Kart ekranı UI hâlâ legacy `UnityEngine.UI.Text` (TextMeshPro'ya geçiş roadmap'te).

## Pending (Roadmap)
- **Demo build final verification + paketleme** (aktif görev — `Tasks/CURRENT_TASK.md`)
- TextMeshPro migrasyonu
- MetaShop (`bankGold` harcama ekranı)
- SaveSystem (PlayerPrefs)
- Minimap
