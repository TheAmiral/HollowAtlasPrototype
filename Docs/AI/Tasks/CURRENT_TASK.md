# Current Task

**Task:** Demo build final verification and packaging
**Status:** Needs full Editor/build playthrough
**Branch:** feature/vs-style-card-system

## Pending
- [ ] Unity Editor'da tam baştan sona playthrough (`StudioSplash` sahnesinden başla).
- [ ] Normal build alıp akışı test et: splash → menu → run → boss → reward → portal → game over / restart.
- [ ] Console kırmızı hatasız doğrulanacak.
- [ ] **Dev panel public build öncesi kapatılacak:** `DevTestHotkeys.cs` → `EnableDevPanelInReleaseBuild = false` (veya dosyayı + .meta sil).
- [ ] Demo build paketi hazırlanacak.

## How to verify
1. `Assets/Scenes/StudioSplash.unity` → Play → splash → MainMenu → Play → Prototype_01.
2. 10 dk pacing: faz geçişleri (`[Wave] …`), 5:00 mini-boss, 10:00 boss.
3. Boss: spawn-öncesi "… GELİYOR" → HP bar → kırmızı telegraph → ölüm → reward kartı → devam (timeScale=1).
4. Portal: reward sonrası spawn → rezonans HUD → "[E] ATLAS PORTALINI AÇ" → E ile aktivasyon ("ATLAS YOLU AÇILDI").
5. Game Over: öl → "GAME OVER" + süre → R / Yeniden Başlat / Ana Menü; restart sonrası loadout/relic/awakening/portal/boss state sıfır.
6. Console: kırmızı hata ve log spam yok.

## Definition of Done
- Build `splash → menu → run → boss → reward → portal → gameover/restart` akışı temiz çalışır.
- Console kırmızı hatasız.
- Dev panel public build için kapatılmış (`EnableDevPanelInReleaseBuild = false`).
- Demo build paketi alınmış.

## Recently Fixed (kısa geçmiş)
- **Demo loop milestone (2026-06-20, `feature/vs-style-card-system`):** HUD timer/dash bar, 10-dk run pacing + boss timing, game over Ana Menü, boss warning/HP bar/kırmızı telegraph, portal E interaction + continuation prompt, log spam fix, geçici Dev Card Panel, card flow + weapon awakening polish. 9 commit (`aa71814e` → `b5901910`), origin'e push edildi. Detay: `Docs/AI/08_CHANGELOG.md`.
- **VS Weapon/Card System (2026-06-14):** 50+ kartlık tanrı sistemi restore edildi; silah kartları (Katana Aura / Ruh Kunai / Atlas Küresi) level-up ekranına entegre; `WeaponInventory` `RuntimeInitializeOnLoadMethod` ile otomatik başlatma.
- **Relic Chest Spawn System:** kill milestone + time-gate'li sandık spawn ve reward flow.
- **Player ground alignment (2026-05-31):** başlangıçta görsel ayak-zemin hizalama (`PlayerMovement` `AlignVisualToGround`, ilk 1.25 sn `LateUpdate`); Play Mode batch doğrulaması geçti, NRE/spam yok.
