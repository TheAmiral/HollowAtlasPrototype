#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Hollow Atlas — Upgrade/Card ikon atayıcı.
//
// Assets/Art/UI/UpgradeIcons/icons_256_png/ altındaki 36'lık ikon setini,
// projedeki 23 ödül ikonuna (RewardIconCatalog) semantik olarak eşler ve
// Resources/Icons/Rewards/{iconId}.png dosyalarının üzerine yazar.
//
// RewardIconLibrary çalışma zamanında Resources/Icons/Rewards/{iconId} yolundan
// yüklediği için ek bir referans bağlama gerekmez — kartlar yeni ikonları otomatik kullanır.
//
// Menü: Hollow Atlas/Assign Upgrade Icons
public static class HollowAtlasAssignUpgradeIcons
{
    const string SourceDir = "Assets/Art/UI/UpgradeIcons/icons_256_png";
    const string DestDir   = "Assets/Resources/Icons/Rewards";

    // iconId -> kaynak PNG dosyası (36'lık set)
    static readonly Dictionary<string, string> Mapping = new()
    {
        // Silahlar
        { "icon_katana_aura",    "07_katana_strike.png" },
        { "icon_ruh_kunai",      "17_range_up.png"      },
        { "icon_atlas_kuresi",   "12_lotus_petal.png"   },

        // Pasifler
        { "icon_kan_yemini",     "03_bleed.png"         },
        { "icon_ruzgar_adimi",   "08_phantom_step.png"  },
        { "icon_demir_beden",    "20_shield.png"        },
        { "icon_zaman_kirigi",   "16_attack_speed.png"  },
        { "icon_buyuyen_yanki",  "15_aura_radius.png"   },
        { "icon_ruh_miknatis",   "10_spirit_thread.png" },
        { "icon_atlas_bereketi", "29_xp_gain.png"       },
        { "icon_lanetli_servet", "27_curse.png"         },

        // Chest / kalıntılar
        { "icon_firavun_tozu",   "01_dash.png"          },
        { "icon_altin_damar",    "28_gold_gain.png"     },
        { "icon_atlas_parcasi",  "32_survival.png"      },
        { "icon_muhafiz_muhru",  "13_oni_brand.png"     },
        { "icon_golge_adim",     "09_shadow_veil.png"   },
        { "icon_atlas_cekirdegi","35_explosion.png"     },
        { "icon_tas_deri",       "18_health_up.png"     },
        { "icon_yasam_runu",     "19_lifesteal.png"     },
        { "icon_atlas_pusulasi", "36_relic_luck.png"    },

        // Silah uyanışları
        { "icon_kanli_hilal",    "14_moonlit_katana.png"},
        { "icon_ruh_firtinasi",  "23_chain_lightning.png"},
        { "icon_atlas_halosu",   "11_foxfire_bloom.png" },
    };

    [MenuItem("Hollow Atlas/Assign Upgrade Icons")]
    public static void AssignIcons()
    {
        EnsureFolder(DestDir);

        int assigned = 0;
        int missingSource = 0;
        var unmatched = new List<string>();

        // RewardIconCatalog kartların tek doğruluk kaynağı — her ikonId için eşleşme ara.
        foreach (var spec in RewardIconCatalog.All)
        {
            if (!Mapping.TryGetValue(spec.IconId, out var srcName))
            {
                Debug.LogWarning($"[HollowAtlas] No upgrade icon mapping found for card: {spec.DisplayName} ({spec.IconId})");
                unmatched.Add(spec.DisplayName);
                continue;
            }

            string srcPath = $"{SourceDir}/{srcName}";
            string dstPath = $"{DestDir}/{spec.IconId}.png";

            if (!File.Exists(srcPath))
            {
                Debug.LogWarning($"[HollowAtlas] Source icon missing: {srcPath} (for {spec.IconId})");
                missingSource++;
                continue;
            }

            // Kaynak ikonun import ayarlarını da düzelt (step 5).
            ConfigureSpriteImporter(srcPath);

            // Bayt kopyası — mevcut .meta korunur, böylece GUID ve referanslar bozulmaz.
            File.Copy(srcPath, dstPath, true);
            AssetDatabase.ImportAsset(dstPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(dstPath);

            assigned++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HollowAtlas] Upgrade icons assigned: {assigned}/{RewardIconCatalog.All.Length}" +
                  (missingSource > 0 ? $"  (missing source: {missingSource})" : "") +
                  (unmatched.Count > 0 ? $"  (unmatched: {string.Join(", ", unmatched)})" : "  (all cards matched)"));
    }

    // Sprite (2D and UI), Single, Max 256, Compression None, Bilinear, Alpha is Transparency.
    static void ConfigureSpriteImporter(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        bool dirty = false;

        if (ti.textureType != TextureImporterType.Sprite)        { ti.textureType = TextureImporterType.Sprite; dirty = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single)      { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (!ti.alphaIsTransparency)                             { ti.alphaIsTransparency = true; dirty = true; }
        if (ti.filterMode != FilterMode.Bilinear)               { ti.filterMode = FilterMode.Bilinear; dirty = true; }
        if (ti.mipmapEnabled)                                    { ti.mipmapEnabled = false; dirty = true; }

        var plat = ti.GetDefaultPlatformTextureSettings();
        if (plat.maxTextureSize != 256 ||
            plat.textureCompression != TextureImporterCompression.Uncompressed)
        {
            plat.maxTextureSize     = 256;
            plat.textureCompression = TextureImporterCompression.Uncompressed; // None — kaliteyi korur
            ti.SetPlatformTextureSettings(plat);
            dirty = true;
        }

        if (dirty) ti.SaveAndReimport();
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
        string leaf   = Path.GetFileName(assetFolder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
