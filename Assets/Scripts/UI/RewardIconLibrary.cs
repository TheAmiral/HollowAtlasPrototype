using System.Collections.Generic;
using UnityEngine;

// Sandık / uyanış ödülleri için ikon sağlayıcı.
// Önce Resources/Icons/Rewards/{iconId}.png dener; bulamazsa
// RewardIconCatalog rengiyle runtime'da iconId'ye özgü placeholder sprite üretir.
public static class RewardIconLibrary
{
    static readonly Dictionary<string, Sprite> _cache = new();

    const int  TexSize = 64;
    const float PixelsPerUnit = 100f;

    public static Sprite GetIcon(string iconId, CardKind kind)
    {
        if (string.IsNullOrEmpty(iconId))
            iconId = "icon_unknown";

        if (_cache.TryGetValue(iconId, out var cached) && cached != null)
            return cached;

        // 1) Gerçek asset varsa onu kullan
        var loaded = Resources.Load<Sprite>("Icons/Rewards/" + iconId);
        if (loaded != null)
        {
            _cache[iconId] = loaded;
            return loaded;
        }

        // 2) Placeholder üret
        var generated = GeneratePlaceholder(iconId, kind);
        _cache[iconId] = generated;
        return generated;
    }

    // ── Placeholder üretimi ───────────────────────────────────────────────────

    static Sprite GeneratePlaceholder(string iconId, CardKind kind)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            name       = "RewardIcon_" + iconId
        };

        Color main   = GetColor(iconId);
        Color border = Color.Lerp(main, Color.white, 0.45f);
        Color bottom = main * 0.55f; bottom.a = 1f;

        // Parlak zeminlerde koyu, koyu zeminlerde açık glyph
        float lum     = 0.299f * main.r + 0.587f * main.g + 0.114f * main.b;
        Color glyphCol = lum > 0.6f
            ? new Color(0.10f, 0.08f, 0.12f, 1f)
            : new Color(0.97f, 0.96f, 1.00f, 1f);

        float c = (TexSize - 1) * 0.5f;
        const float rOuter = 30f, rInner = 26f;
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dx = x - c, dy = y - c;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);

                Color px;
                if (d > rOuter)
                {
                    px = clear;
                }
                else if (d > rInner)
                {
                    px = border;
                }
                else
                {
                    float t = y / (float)TexSize;          // alt koyu, üst açık
                    px = Color.Lerp(bottom, main, t);
                    if (InGlyph(kind, dx, dy))
                        px = glyphCol;
                }

                tex.SetPixel(x, y, px);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize),
            new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    // Merkez kökenli koordinatta (dx,dy) glyph içinde mi?
    static bool InGlyph(CardKind kind, float dx, float dy)
    {
        float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);

        switch (kind)
        {
            case CardKind.WeaponAwakening: // parıltı / yıldız
                bool v  = ax <= 2f && ay <= 12f;
                bool h  = ay <= 2f && ax <= 12f;
                bool d1 = Mathf.Abs(dx - dy) <= 2f && ax <= 8f;
                bool d2 = Mathf.Abs(dx + dy) <= 2f && ax <= 8f;
                return v || h || d1 || d2;

            case CardKind.ChestSurvival:   // artı (can)
                return (ax <= 3f && ay <= 11f) || (ay <= 3f && ax <= 11f);

            case CardKind.ChestEconomy:    // dolu daire (sikke)
                return (dx * dx + dy * dy) <= 81f; // r=9

            case CardKind.ChestRelic:      // üçgen (bıçak)
                return dy >= -9f && dy <= 9f && ax <= (9f - dy) * 0.5f;

            case CardKind.ChestSeal:       // baklava (mühür)
                return (ax + ay) <= 10f;

            case CardKind.ChestAtlas:      // halka
                float dd = Mathf.Sqrt(dx * dx + dy * dy);
                return dd >= 6f && dd <= 10f;

            default:                       // küçük daire
                return (dx * dx + dy * dy) <= 64f; // r=8
        }
    }

    // ── iconId → renk ─────────────────────────────────────────────────────────

    static Color GetColor(string iconId) => RewardIconCatalog.GetColor(iconId);
}
