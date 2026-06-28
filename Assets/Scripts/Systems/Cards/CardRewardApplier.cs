using System.Collections.Generic;
using UnityEngine;

public struct StatDelta
{
    public string label;
    public string valueText;
    public bool   isPositive;

    public StatDelta(string label, string valueText, bool isPositive)
    {
        this.label      = label;
        this.valueText  = valueText;
        this.isPositive = isPositive;
    }
}

public static class CardRewardApplier
{
    // computeDeltas=false → snapshot/diff atlanır ve null döner (normal kartlarda
    // feedback paneli gösterilmediği için gereksiz işten kaçınılır). Varsayılan true
    // ile geriye dönük uyumludur.
    public static List<StatDelta> Apply(CardDefinition card, GameObject player, bool computeDeltas = true)
    {
        if (card == null || player == null) return computeDeltas ? new List<StatDelta>() : null;

        Snapshot before = default;
        if (computeDeltas) before = Snapshot.Take(player);

        switch (card.cardKind)
        {
            case CardKind.WeaponUnlock:
            case CardKind.WeaponUpgrade:
                var inv = WeaponInventory.Instance;
                if (inv != null)
                    inv.UnlockOrUpgrade(card.weaponType, player);
                break;

            case CardKind.PassiveUnlock:
                RunLoadoutSystem.Instance?.UnlockPassive(card.passiveId);
                card.Apply?.Invoke(player);
                break;

            case CardKind.PassiveUpgrade:
                RunLoadoutSystem.Instance?.UpgradePassive(card.passiveId);
                card.Apply?.Invoke(player);
                break;

            case CardKind.Special:
                card.Apply?.Invoke(player);
                break;

            case CardKind.WeaponAwakening:
                WeaponInventory.Instance?.MarkWeaponAwakened(card.weaponType);
                card.Apply?.Invoke(player);
                RunLoadoutSystem.Instance?.RegisterChestReward(card.id, card.title, card.iconId, card.cardKind);
                break;

            case CardKind.ChestRelic:
            case CardKind.ChestSeal:
            case CardKind.ChestEconomy:
            case CardKind.ChestSurvival:
            case CardKind.ChestAtlas:
                card.Apply?.Invoke(player);
                RunLoadoutSystem.Instance?.RegisterChestReward(card.id, card.title, card.iconId, card.cardKind);
                break;

            default:
                // Geriye dönük uyumluluk: eski CardType sistemindeki silah kartları
                if (card.cardType == CardType.WeaponUpgrade)
                {
                    var legacyInv = WeaponInventory.Instance;
                    if (legacyInv != null)
                        legacyInv.UnlockOrUpgrade(card.weaponType, player);
                }
                else
                {
                    card.Apply?.Invoke(player);
                }
                break;
        }

        if (!computeDeltas) return null;
        return Snapshot.Diff(before, Snapshot.Take(player));
    }

    // ── Internal snapshot ─────────────────────────────────────────────────────

    struct Snapshot
    {
        int   auraDamage;
        float auraRadius;
        float auraTickInterval;
        float moveSpeed;
        float dashSpeed;
        float dashDuration;
        float dashCooldown;
        int   dashDamage;
        float dashHitRadius;
        int   maxHealth;
        int   currentHealth;
        // Silah bileşenleri
        int   kunaiDamage;
        int   kunaiCount;
        int   sphereDamage;
        int   sphereCount;

        public static Snapshot Take(GameObject p)
        {
            if (p == null) return default;
            var s = new Snapshot();
            var aura  = p.GetComponent<AutoAttackAura>();
            if (aura  != null) { s.auraDamage = aura.damage; s.auraRadius = aura.radius; s.auraTickInterval = aura.tickInterval; }
            var mv    = p.GetComponent<PlayerMovement>();
            if (mv    != null) { s.moveSpeed = mv.moveSpeed; s.dashSpeed = mv.dashSpeed; s.dashDuration = mv.dashDuration; s.dashCooldown = mv.dashCooldown; s.dashDamage = mv.dashDamage; s.dashHitRadius = mv.dashHitRadius; }
            var hp    = p.GetComponent<PlayerHealth>();
            if (hp    != null) { s.maxHealth = hp.maxHealth; s.currentHealth = hp.CurrentHealth; }
            var kunai = p.GetComponent<RuhKunai>();
            if (kunai != null) { s.kunaiDamage = kunai.damage; s.kunaiCount = kunai.projectileCount; }
            var sph   = p.GetComponent<AtlasSphere>();
            if (sph   != null) { s.sphereDamage = sph.damage; s.sphereCount = sph.sphereCount; }
            return s;
        }

        public static List<StatDelta> Diff(Snapshot a, Snapshot b)
        {
            var list = new List<StatDelta>();

            Check(list,  "Aura Hasarı",   b.auraDamage - a.auraDamage);
            CheckF(list, "Alan",           b.auraRadius - a.auraRadius, 2);
            CheckTick(list, a.auraTickInterval, b.auraTickInterval);
            CheckF(list, "Hareket Hızı",  b.moveSpeed - a.moveSpeed, 2);
            Check(list,  "Dash Hasarı",   b.dashDamage - a.dashDamage);
            CheckF(list, "Dash Bekleme",   b.dashCooldown - a.dashCooldown, 2, invertPositive: true);
            CheckF(list, "Dash Hızı",      b.dashSpeed - a.dashSpeed, 1);
            CheckF(list, "Dash Süresi",    b.dashDuration - a.dashDuration, 2);
            Check(list,  "Maks. Can",     b.maxHealth - a.maxHealth);
            Check(list,  "Kunai Hasarı",  b.kunaiDamage - a.kunaiDamage);
            Check(list,  "Kunai Sayısı",  b.kunaiCount - a.kunaiCount);
            Check(list,  "Küre Hasarı",   b.sphereDamage - a.sphereDamage);
            Check(list,  "Küre Sayısı",   b.sphereCount - a.sphereCount);

            int heal = Mathf.Max(0, (b.currentHealth - a.currentHealth)
                                  - Mathf.Max(0, b.maxHealth - a.maxHealth));
            if (heal > 0) list.Add(new StatDelta("İyileşme", $"+{heal}", true));

            return list;
        }

        static void Check(List<StatDelta> list, string label, int delta)
        {
            if (delta == 0) return;
            list.Add(new StatDelta(label, delta > 0 ? $"+{delta}" : $"{delta}", delta > 0));
        }

        static void CheckF(List<StatDelta> list, string label, float delta, int dec, bool invertPositive = false)
        {
            if (Mathf.Abs(delta) < 0.001f) return;
            bool pos = invertPositive ? delta < 0 : delta > 0;
            string v = delta > 0 ? $"+{delta.ToString($"F{dec}")}" : delta.ToString($"F{dec}");
            list.Add(new StatDelta(label, v, pos));
        }

        static void CheckTick(List<StatDelta> list, float before, float after)
        {
            if (before <= 0f || Mathf.Abs(after - before) < 0.001f) return;
            float pct    = (before - after) / before * 100f;
            bool  faster = after < before;
            list.Add(new StatDelta("Saldırı Hızı", faster ? $"+%{pct:F0}" : $"-%{Mathf.Abs(pct):F0}", faster));
        }
    }
}
