# HollowAtlasPrototype

> **Hollow Atlas** — Roguelite Auto-Attack Survivor | Unity 6 + URP 17

## Proje Hakkında

Hades 2 görselliği + Vampire Survivors oynanışından ilham alan roguelite.  
Oyuncu otomatik saldırı ile dalgaları temizler, level-up kartları seçer, boss öldürür.

---

## Kurulum

1. Unity **6000.3.10f1** ile aç
2. `Assets/Scenes/Prototype_01.unity` sahnesini yükle
3. Play'e bas

---

## Kontroller

| Tuş | Aksiyon |
|-----|---------|
| WASD / Ok tuşları | Hareket |
| Space | Dash (düşmana hasar verir) |
| 1 / 2 / 3 | Boss ödülü / Level-up kart seçimi |

---

## Script Klasör Yapısı

```
Assets/Scripts/
├── Player/      PlayerMovement, PlayerHealth, PlayerLevelSystem, PlayerDamageFlash, CharacterAnimatorBridge, CameraFollow
├── Combat/      AutoAttackAura
├── Enemy/       EnemyChaser, EnemyHealth, EnemyProjectile, EnemyShooter, EnemySpawner, EnemyVisuals
├── Boss/        BossSpawnSystem, BossSpecialAttack, BossRewardSystem, BossHealthUI
├── Pickup/      ExperiencePickup, GoldPickup, HealthPickup, PickupMagnet
├── UI/          MainHudCanvasUI
└── Systems/     GameManager, GoldWallet, RunContractSystem
```

---

## Düzeltilen Bug'lar

| # | Script | Sorun | Durum |
|---|--------|-------|-------|
| 1 | HealthPickup.cs | CircleCollider2D (2D) → SphereCollider (3D) fix | ✅ |
| 2 | PlayerLevelSystem.cs | XP sabit 100 → scaling formula (`level*80+20`) | ✅ |
| 3 | GoldWallet.cs | B/R debug kısayolları `#if UNITY_EDITOR` bloğuna taşındı | ✅ |
| 4 | EnemySpawner.cs | Her frame FindObjectsByType → EnemyHealth.AliveCount statik sayaç | ✅ |
| 5 | PickupMagnet.cs | Her frame FindGameObjectWithTag → Awake'te cache | ✅ |

---

## Bilinen Eksikler (Roadmap)

- [ ] LevelUpCardSystem — kart seçim ekranı (şimdi direkt stat boost)
- [ ] WeaponSystem — silah çeşitliliği (şimdi sadece aura)
- [ ] MetaShop — BankGold harcanabilir hale gelmeli
- [ ] SaveSystem — PlayerPrefs ile kalıcı kayıt
- [ ] TextMeshPro migrasyonu (şimdi legacy UI.Text)
- [ ] Gerçek minimap (şimdi placeholder)
