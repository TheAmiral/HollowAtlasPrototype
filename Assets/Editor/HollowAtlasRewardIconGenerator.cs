using System.IO;
using UnityEditor;
using UnityEngine;

// Tools > Hollow Atlas > UI > Generate Reward Icons
// RewardIconCatalog listesinden gerçek PNG sprite assetleri üretir.
public static class HollowAtlasRewardIconGenerator
{
    const int TexSize = 128;
    const float PixelsPerUnit = 100f;
    const string OutputDir = "Assets/Resources/Icons/Rewards";

    [MenuItem("Tools/Hollow Atlas/UI/Generate Reward Icons")]
    public static void GenerateRewardIcons()
    {
        EnsureDirectory(OutputDir);

        int written = 0;
        foreach (var spec in RewardIconCatalog.All)
        {
            Texture2D tex = BuildIcon(spec);
            string path = $"{OutputDir}/{spec.IconId}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            ConfigureAsSprite(path);
            written++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Hollow Atlas] Generated {written} reward icon sprites at {OutputDir}.");
    }

    static void EnsureDirectory(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void ConfigureAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = TexSize;
        importer.SaveAndReimport();
    }

    static Texture2D BuildIcon(RewardIconSpec spec)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            name = spec.IconId,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color main = spec.MainColor;
        Color high = Color.Lerp(main, Color.white, 0.46f); high.a = 1f;
        Color low = Color.Lerp(main, Color.black, 0.48f); low.a = 1f;
        Color rim = Color.Lerp(main, Color.white, 0.68f); rim.a = 1f;
        Color innerRim = Color.Lerp(main, Color.black, 0.26f); innerRim.a = 1f;
        Color glyph = GlyphColor(main);
        Color clear = new Color(0f, 0f, 0f, 0f);

        float c = (TexSize - 1) * 0.5f;
        float outer = TexSize * 0.465f;
        float rimStart = TexSize * 0.415f;
        float inner = TexSize * 0.365f;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                Color px;
                if (d > outer)
                {
                    px = clear;
                }
                else if (d > rimStart)
                {
                    float t = Mathf.InverseLerp(outer, rimStart, d);
                    px = Color.Lerp(innerRim, rim, t);
                }
                else
                {
                    float vertical = Mathf.InverseLerp(-c, c, dy);
                    float radial = Mathf.Clamp01(d / inner);
                    px = Color.Lerp(low, high, vertical * 0.76f + 0.12f);
                    px = Color.Lerp(px, Color.black, radial * 0.18f);
                    px.a = 1f;

                    // Sol üstten hafif büyülü yansıma.
                    if (dx < -6f && dy > 8f && Mathf.Abs(dx + dy) < 15f)
                        px = Color.Lerp(px, Color.white, 0.16f);

                    if (IsGlyph(spec.Shape, dx, dy))
                        px = glyph;
                }

                tex.SetPixel(x, y, px);
            }
        }

        tex.Apply();
        return tex;
    }

    static Color GlyphColor(Color main)
    {
        float lum = 0.299f * main.r + 0.587f * main.g + 0.114f * main.b;
        return lum > 0.60f
            ? new Color(0.10f, 0.07f, 0.12f, 1f)
            : new Color(0.98f, 0.96f, 1.00f, 1f);
    }

    static bool IsGlyph(RewardIconShape shape, float dx, float dy)
    {
        float ax = Mathf.Abs(dx);
        float ay = Mathf.Abs(dy);

        switch (shape)
        {
            case RewardIconShape.Katana:
                return Line(dx, dy, -19f, -21f, 19f, 17f, 3.0f) ||
                       Line(dx, dy, -17f, -16f, -5f, -28f, 3.0f) ||
                       Line(dx, dy, -23f, -10f, -10f, -23f, 2.3f);

            case RewardIconShape.Kunai:
                return TriangleUp(dx, dy + 1f, 0f, 22f, 13f) ||
                       (ax <= 3f && dy >= -24f && dy <= 0f) ||
                       Ring(dx, dy + 25f, 6f, 9f);

            case RewardIconShape.Sphere:
                return Ring(dx, dy, 14f, 18f) || Ring(dx + 13f, dy - 9f, 3f, 6f) || Line(dx, dy, -25f, -8f, 25f, 8f, 1.8f);

            case RewardIconShape.Heart:
                return Circle(dx + 9f, dy - 5f, 9f) || Circle(dx - 9f, dy - 5f, 9f) || TriangleDown(dx, dy + 3f, 0f, 25f, 18f);

            case RewardIconShape.Boots:
                return RoundedBox(dx + 10f, dy - 2f, 8f, 19f, 4f) || RoundedBox(dx - 10f, dy + 2f, 8f, 19f, 4f) ||
                       RoundedBox(dx + 13f, dy + 18f, 15f, 5f, 3f) || RoundedBox(dx - 7f, dy + 22f, 15f, 5f, 3f);

            case RewardIconShape.Shield:
                return (ay <= 22f && ax <= 17f - Mathf.Max(0f, dy) * 0.26f && dy >= -19f) || TriangleDown(dx, dy + 3f, 0f, 22f, 16f);

            case RewardIconShape.Hourglass:
                return Line(dx, dy, -15f, 20f, 15f, 20f, 2.8f) || Line(dx, dy, -15f, -20f, 15f, -20f, 2.8f) ||
                       Line(dx, dy, -12f, 17f, 12f, -17f, 3.0f) || Line(dx, dy, 12f, 17f, -12f, -17f, 3.0f) ||
                       Circle(dx, dy + 9f, 3f) || Circle(dx, dy - 9f, 3f);

            case RewardIconShape.Echo:
                return Arc(dx + 13f, dy, 10f, 2.5f, -55f, 55f) || Arc(dx + 13f, dy, 18f, 2.5f, -55f, 55f) || Arc(dx + 13f, dy, 26f, 2.5f, -55f, 55f);

            case RewardIconShape.Magnet:
                return (Ring(dx, dy + 1f, 17f, 22f) && dy < 11f) ||
                       RoundedBox(dx + 16f, dy + 15f, 7f, 12f, 3f) || RoundedBox(dx - 16f, dy + 15f, 7f, 12f, 3f);

            case RewardIconShape.Xp:
                return (ax <= 3f && ay <= 23f) || (ay <= 3f && ax <= 23f) ||
                       Line(dx, dy, -16f, -16f, 16f, 16f, 2.7f) || Line(dx, dy, -16f, 16f, 16f, -16f, 2.7f);

            case RewardIconShape.Coin:
                return Circle(dx, dy, 18f) || Line(dx, dy, 0f, -12f, 0f, 12f, 2.5f);

            case RewardIconShape.Dust:
                return Circle(dx + 12f, dy - 8f, 6f) || Circle(dx - 6f, dy, 5f) || Circle(dx - 16f, dy + 12f, 4f) || Circle(dx + 8f, dy + 17f, 3.5f);

            case RewardIconShape.AtlasShard:
                return Diamond(dx, dy, 14f, 27f) || Line(dx, dy, 0f, -20f, 0f, 20f, 2f);

            case RewardIconShape.Seal:
                return Diamond(dx, dy, 20f, 20f) || Ring(dx, dy, 8f, 12f);

            case RewardIconShape.Footstep:
                return RoundedBox(dx + 9f, dy + 3f, 8f, 17f, 5f) || RoundedBox(dx - 11f, dy - 5f, 8f, 17f, 5f) ||
                       Circle(dx + 5f, dy - 16f, 3.3f) || Circle(dx + 13f, dy - 15f, 3.0f) || Circle(dx - 15f, dy - 24f, 3.1f);

            case RewardIconShape.Core:
                return Ring(dx, dy, 5f, 9f) || Ring(dx, dy, 17f, 21f) || Line(dx, dy, -24f, 0f, 24f, 0f, 2f);

            case RewardIconShape.Stone:
                return Diamond(dx, dy, 19f, 24f) || Line(dx, dy, -10f, 4f, 12f, 16f, 2.2f) || Line(dx, dy, -11f, -11f, 9f, 4f, 2.2f);

            case RewardIconShape.Rune:
                return Line(dx, dy, 0f, -24f, 0f, 24f, 3.0f) || Line(dx, dy, -14f, -6f, 0f, 5f, 3.0f) || Line(dx, dy, 0f, 5f, 14f, -6f, 3.0f) ||
                       Line(dx, dy, -13f, 15f, 13f, 15f, 2.7f);

            case RewardIconShape.Compass:
                return TriangleUp(dx, dy + 2f, 0f, 26f, 10f) || TriangleDown(dx, dy - 2f, 0f, 26f, 10f) || Ring(dx, dy, 22f, 24f);

            case RewardIconShape.Crescent:
                return Circle(dx, dy, 21f) && !Circle(dx + 10f, dy + 1f, 21f);

            case RewardIconShape.Storm:
                return PolygonLightning(dx, dy);

            case RewardIconShape.Halo:
                return Ring(dx, dy, 13f, 18f) || (ax <= 2.5f && ay <= 28f) || (ay <= 2.5f && ax <= 28f);

            default:
                return Circle(dx, dy, 11f);
        }
    }

    static bool Line(float px, float py, float x1, float y1, float x2, float y2, float thickness)
    {
        float vx = x2 - x1;
        float vy = y2 - y1;
        float wx = px - x1;
        float wy = py - y1;
        float lenSq = vx * vx + vy * vy;
        if (lenSq <= 0.0001f) return false;
        float t = Mathf.Clamp01((wx * vx + wy * vy) / lenSq);
        float cx = x1 + t * vx;
        float cy = y1 + t * vy;
        float dx = px - cx;
        float dy = py - cy;
        return dx * dx + dy * dy <= thickness * thickness;
    }

    static bool Circle(float dx, float dy, float radius) => dx * dx + dy * dy <= radius * radius;

    static bool Ring(float dx, float dy, float inner, float outer)
    {
        float d2 = dx * dx + dy * dy;
        return d2 >= inner * inner && d2 <= outer * outer;
    }

    static bool Diamond(float dx, float dy, float halfW, float halfH)
    {
        return Mathf.Abs(dx) / halfW + Mathf.Abs(dy) / halfH <= 1f;
    }

    static bool RoundedBox(float dx, float dy, float halfW, float halfH, float radius)
    {
        float qx = Mathf.Abs(dx) - halfW + radius;
        float qy = Mathf.Abs(dy) - halfH + radius;
        float outsideX = Mathf.Max(qx, 0f);
        float outsideY = Mathf.Max(qy, 0f);
        return outsideX * outsideX + outsideY * outsideY <= radius * radius && qx <= radius && qy <= radius;
    }

    static bool TriangleUp(float dx, float dy, float cx, float height, float halfBase)
    {
        float localY = dy + height * 0.5f;
        if (localY < 0f || localY > height) return false;
        float allowed = Mathf.Lerp(halfBase, 0f, localY / height);
        return Mathf.Abs(dx - cx) <= allowed;
    }

    static bool TriangleDown(float dx, float dy, float cx, float height, float halfBase)
    {
        float localY = dy + height * 0.5f;
        if (localY < 0f || localY > height) return false;
        float allowed = Mathf.Lerp(0f, halfBase, localY / height);
        return Mathf.Abs(dx - cx) <= allowed;
    }

    static bool Arc(float dx, float dy, float radius, float thickness, float minAngle, float maxAngle)
    {
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (Mathf.Abs(d - radius) > thickness) return false;
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        return angle >= minAngle && angle <= maxAngle;
    }

    static bool PolygonLightning(float dx, float dy)
    {
        return Line(dx, dy, 3f, -27f, -9f, -3f, 4f) ||
               Line(dx, dy, -9f, -3f, 4f, -3f, 4f) ||
               Line(dx, dy, 4f, -3f, -6f, 27f, 4f);
    }
}
