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
    public static List<StatDelta> Apply(CardDefinition card, GameObject player)
    {
        if (card == null || player == null)
            return new List<StatDelta>();

        var before = Snapshot.Take(player);
        card.Apply?.Invoke(player);
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

        public static Snapshot Take(GameObject p)
        {
            var s = new Snapshot();
            var aura = p.GetComponent<AutoAttackAura>();
            if (aura != null) { s.auraDamage = aura.damage; s.auraRadius = aura.radius; s.auraTickInterval = aura.tickInterval; }
            var mv = p.GetComponent<PlayerMovement>();
            if (mv != null) { s.moveSpeed = mv.moveSpeed; s.dashSpeed = mv.dashSpeed; s.dashDuration = mv.dashDuration; s.dashCooldown = mv.dashCooldown; s.dashDamage = mv.dashDamage; s.dashHitRadius = mv.dashHitRadius; }
            var hp = p.GetComponent<PlayerHealth>();
            if (hp != null) { s.maxHealth = hp.maxHealth; s.currentHealth = hp.CurrentHealth; }
            return s;
        }

        public static List<StatDelta> Diff(Snapshot a, Snapshot b)
        {
            var list = new List<StatDelta>();

            Check(list, "Aura Hasarı",  b.auraDamage - a.auraDamage);
            CheckF(list, "Aura Menzili", b.auraRadius - a.auraRadius, 2);
            CheckTick(list, a.auraTickInterval, b.auraTickInterval);
            CheckF(list, "Hareket Hızı", b.moveSpeed - a.moveSpeed, 2);
            Check(list, "Dash Hasarı",  b.dashDamage - a.dashDamage);
            CheckF(list, "Dash CD",      b.dashCooldown - a.dashCooldown, 2, invertPositive: true);
            CheckF(list, "Dash Hızı",    b.dashSpeed - a.dashSpeed, 1);
            CheckF(list, "Dash Süresi",  b.dashDuration - a.dashDuration, 2);
            Check(list, "Maks. Can",    b.maxHealth - a.maxHealth);
            int heal = Mathf.Max(0, (b.currentHealth - a.currentHealth) - Mathf.Max(0, b.maxHealth - a.maxHealth));
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
            float pct = (before - after) / before * 100f;
            bool faster = after < before;
            list.Add(new StatDelta("Tick Hızı", faster ? $"+%{pct:F0}" : $"-%{Mathf.Abs(pct):F0}", faster));
        }
    }
}
