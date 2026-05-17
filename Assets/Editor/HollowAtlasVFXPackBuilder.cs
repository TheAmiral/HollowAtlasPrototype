#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu: Tools → Hollow Atlas → VFX → Create VFX Pack 01
///       Tools → Hollow Atlas → VFX → Sanitize Existing VFX Prefabs
///
/// Scans Assets/Art/VFX/Source for textures, creates additive URP materials,
/// and saves six particle system prefabs under Assets/Art/VFX/Prefabs.
/// Safe to re-run — existing materials are updated in place, prefabs are overwritten.
/// </summary>
public static class HollowAtlasVFXPackBuilder
{
    const string PrefabPath   = "Assets/Art/VFX/Prefabs";
    const string MaterialPath = "Assets/Art/VFX/Materials";
    const string SourcePath   = "Assets/Art/VFX/Source";

    // ── Create VFX Pack 01 ────────────────────────────────────────────────────

    [MenuItem("Tools/Hollow Atlas/VFX/Create VFX Pack 01")]
    static void CreateVFXPack01()
    {
        EnsureFolders();

        var sparkTex  = FindTexture("spark_01",  "spark");
        var smokeTex  = FindTexture("smoke_01",  "smoke");
        var starTex   = FindTexture("star_01",   "star");
        var magicTex  = FindTexture("magic_01",  "magic");
        var twirlTex  = FindTexture("twirl_01",  "twirl");
        var circleTex = FindTexture("circle_05", "circle");

        var matHit       = CreateAdditiveMaterial("Mat_Hit_Spark",     sparkTex,  new Color(1f,    0.10f, 0.20f));
        var matDust      = CreateAdditiveMaterial("Mat_Death_Dust",     smokeTex,  new Color(0.40f, 0.05f, 0.50f));
        var matGold      = CreateAdditiveMaterial("Mat_Gold_Spark",     starTex,   new Color(1f,    0.88f, 0.10f));
        var matXP        = CreateAdditiveMaterial("Mat_XP_Spark",       magicTex,  new Color(0.10f, 0.90f, 1f  ));
        var matLevelUp   = CreateAdditiveMaterial("Mat_LevelUp_Burst",  twirlTex,  new Color(0.55f, 0.15f, 1f  ));
        var matTelegraph = CreateAdditiveMaterial("Mat_Boss_Telegraph", circleTex, new Color(1f,    0.08f, 0f  ));

        BuildHitSmall(matHit);
        BuildEnemyDeathDust(matDust);
        BuildGoldPickupSpark(matGold);
        BuildXPPickupSpark(matXP);
        BuildLevelUpBurst(matLevelUp);
        BuildBossTelegraphCircle(matTelegraph);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HollowAtlas VFX] VFX Pack 01 created successfully.");
    }

    // ── Sanitize Existing VFX Prefabs ────────────────────────────────────────

    [MenuItem("Tools/Hollow Atlas/VFX/Sanitize Existing VFX Prefabs")]
    static void SanitizeExistingVFXPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabPath))
        {
            Debug.LogWarning("[HollowAtlas VFX] Prefab folder not found. Run 'Create VFX Pack 01' first.");
            return;
        }

        var guids  = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabPath });
        int fixed_ = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("VFX_")) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var ps = scope.prefabContentsRoot.GetComponent<ParticleSystem>();
                if (ps != null && SanitizeVelocityModule(ps))
                {
                    fixed_++;
                    Debug.Log($"[HollowAtlas VFX] Sanitized velocity module in {name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[HollowAtlas VFX] Sanitize complete — {fixed_} prefab(s) patched.");
    }

    // ── Velocity sanitizer ────────────────────────────────────────────────────

    // Unity requires vel.x / vel.y / vel.z to share the same MinMaxCurveMode.
    // Mixing Constant on X/Z with TwoConstants on Y is enough to trigger:
    //   "Particle Velocity curves must all be in the same mode"
    // We normalize all axes to TwoConstants, preserving Y's existing range.
    // Returns true if the module was modified.
    static bool SanitizeVelocityModule(ParticleSystem ps)
    {
        var vel = ps.velocityOverLifetime;
        if (!vel.enabled) return false;

        float yMin, yMax;
        switch (vel.y.mode)
        {
            case ParticleSystemCurveMode.Constant:
                yMin = yMax = vel.y.constant;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                yMin = vel.y.constantMin;
                yMax = vel.y.constantMax;
                break;
            default:
                // Curve / TwoCurves — safest to disable rather than mangle the curves
                vel.enabled = false;
                return true;
        }

        vel.x = new ParticleSystem.MinMaxCurve(0f,   0f);
        vel.y = new ParticleSystem.MinMaxCurve(yMin, yMax);
        vel.z = new ParticleSystem.MinMaxCurve(0f,   0f);
        return true;
    }

    // ── Folder helpers ────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Art/VFX", "Materials");
        EnsureFolder("Assets/Art/VFX", "Prefabs");
        EnsureFolder("Assets/Art/VFX", "Textures");
        EnsureFolder("Assets/Scripts", "VFX");
        EnsureFolder("Assets",         "Editor");
    }

    static void EnsureFolder(string parent, string folderName)
    {
        string full = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    // ── Texture search ────────────────────────────────────────────────────────

    static Texture2D FindTexture(string preferred, string fallbackPrefix)
    {
        var guids = AssetDatabase.FindAssets($"t:Texture2D {preferred}", new[] { SourcePath });
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));

        if (!string.IsNullOrEmpty(fallbackPrefix))
        {
            guids = AssetDatabase.FindAssets($"t:Texture2D {fallbackPrefix}", new[] { SourcePath });
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        Debug.LogWarning($"[HollowAtlas VFX] Texture not found: '{preferred}' / '{fallbackPrefix}'. Material will have no texture.");
        return null;
    }

    // ── Material creation ─────────────────────────────────────────────────────

    static Material CreateAdditiveMaterial(string matName, Texture2D tex, Color tint)
    {
        string path     = $"{MaterialPath}/{matName}.mat";
        var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyTexture(existing, tex);
            existing.color = tint;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive")
                  ?? Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("[HollowAtlas VFX] No suitable particle shader found. Ensure URP is installed.");
            return null;
        }

        var mat = new Material(shader) { name = matName };
        ApplyTexture(mat, tex);
        mat.color = tint;

        if (shader.name.Contains("Universal Render Pipeline/Particles/Unlit"))
        {
            mat.SetFloat("_Surface",  1f);
            mat.SetFloat("_Blend",    2f);
            mat.SetFloat("_ZWrite",   0f);
            mat.SetFloat("_SrcBlend", 1f);
            mat.SetFloat("_DstBlend", 1f);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyTexture(Material mat, Texture2D tex)
    {
        if (tex == null) return;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
    }

    // ── Generic prefab builder ────────────────────────────────────────────────

    static void BuildPrefab(
        string           prefabName,
        Material         mat,
        System.Action<ParticleSystem, ParticleSystemRenderer> configure,
        bool             useUnscaledTime = false)
    {
        string path = $"{PrefabPath}/{prefabName}.prefab";

        var go       = new GameObject(prefabName);
        var ps       = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake     = true;
        if (useUnscaledTime) main.useUnscaledTime = true;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) renderer.material = mat;

        go.AddComponent<VFXAutoReturn>();

        configure?.Invoke(ps, renderer);

        // Safety pass: normalize velocity curve modes before serialising to disk.
        SanitizeVelocityModule(ps);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[HollowAtlas VFX] Saved {path}");
    }

    // ── VFX_Hit_Small ─────────────────────────────────────────────────────────

    static void BuildHitSmall(Material mat)
    {
        BuildPrefab("VFX_Hit_Small", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration        = 0.25f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f,  4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.10f, 0.20f), new Color(0.60f, 0f, 0.85f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.4f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 25) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.10f;
        });
    }

    // ── VFX_Enemy_Death_Dust ──────────────────────────────────────────────────

    static void BuildEnemyDeathDust(Material mat)
    {
        BuildPrefab("VFX_Enemy_Death_Dust", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration      = 0.6f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.70f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0.50f, 2f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.20f, 0.60f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.05f, 0.45f, 0.90f),
                new Color(0.20f, 0.20f, 0.25f, 0.70f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 35) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.25f;

            // All three axes explicitly set to TwoConstants so they share the same mode.
            // SanitizeVelocityModule() in BuildPrefab provides an additional safety net.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = new ParticleSystem.MinMaxCurve(0f,    0f);
            vel.y       = new ParticleSystem.MinMaxCurve(0.30f, 0.80f);
            vel.z       = new ParticleSystem.MinMaxCurve(0f,    0f);
        });
    }

    // ── VFX_Gold_Pickup_Spark ─────────────────────────────────────────────────

    static void BuildGoldPickupSpark(Material mat)
    {
        BuildPrefab("VFX_Gold_Pickup_Spark", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration      = 0.4f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.45f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f,    5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.90f, 0.10f), new Color(1f, 0.60f, 0f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 18) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;
        });
    }

    // ── VFX_XP_Pickup_Spark ───────────────────────────────────────────────────

    static void BuildXPPickupSpark(Material mat)
    {
        BuildPrefab("VFX_XP_Pickup_Spark", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration      = 0.4f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.40f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(1.5f,  3.5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.10f, 0.90f, 1f), new Color(0.50f, 0.10f, 1f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;
        });
    }

    // ── VFX_LevelUp_Burst ─────────────────────────────────────────────────────

    static void BuildLevelUpBurst(Material mat)
    {
        BuildPrefab("VFX_LevelUp_Burst", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration      = 0.8f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.90f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(3f,    7f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.60f, 0.20f, 1f), new Color(0.10f, 0.90f, 1f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 40) });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Circle;
            shape.radius          = 0.30f;
            shape.radiusThickness = 0f;
        }, useUnscaledTime: true);
    }

    // ── VFX_Boss_Telegraph_Circle ─────────────────────────────────────────────

    static void BuildBossTelegraphCircle(Material mat)
    {
        BuildPrefab("VFX_Boss_Telegraph_Circle", mat, (ps, r) =>
        {
            var main = ps.main;
            main.duration      = 1.0f;
            main.loop          = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.80f, 1.0f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(0f,    0.3f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.20f, 0.50f);
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.10f, 0f), new Color(0.80f, 0f, 0.30f));

            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 40) });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Circle;
            shape.radius          = 1.5f;
            shape.radiusThickness = 0.10f;
        });
    }
}
#endif
