using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

public static class FemaleSamuraiFullRepair
{
    // ── Asset paths ───────────────────────────────────────────────────────────
    const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    const string FBX_DIR         = "Assets/Art/Characters/Player/FemaleSamurai/Meshy_AI_Moonbound_Samurai_biped/";
    const string MESH_FBX        = FBX_DIR + "Meshy_AI_Moonbound_Samurai_biped_Character_output.fbx";
    const string ANIM_FBX        = FBX_DIR + "Meshy_AI_Moonbound_Samurai_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
    const string TEX_BASE        = FBX_DIR + "Meshy_AI_Moonbound_Samurai_biped_texture_0.png";
    const string TEX_NORMAL      = FBX_DIR + "Meshy_AI_Moonbound_Samurai_biped_texture_0_normal.png";
    const string TEX_METALLIC    = FBX_DIR + "Meshy_AI_Moonbound_Samurai_biped_texture_0_metallic.png";
    const string CONTROLLER_PATH = "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    const string MATERIAL_PATH   = "Assets/Art/Characters/Player/FemaleSamurai/Materials/FemaleSamurai_URP_Lit.mat";
    const string PREFAB_PATH     = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";
    const string OLD_VISUAL_NAME = "FemaleSamurai_Visual";
    const string BODY_OLD        = "Body_010";

    [MenuItem("Tools/Hollow Atlas/Full Repair Female Samurai")]
    public static void Repair()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiFullRepair                                ║");
        log.AppendLine("╚══════════════════════════════════════════════════════════╝");

        // ── 1. Open scene ────────────────────────────────────────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (active.path != SCENE_PATH)
        {
            if (!batch) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
            log.AppendLine($"[Scene]     Açıldı → {SCENE_PATH}");
        }
        else { log.AppendLine("[Scene]     Zaten açık"); }

        // ── 2. Find Player root ──────────────────────────────────────────────────
        var pm = UObject.FindFirstObjectByType<PlayerMovement>();
        if (pm == null) { Abort(log, batch, "PlayerMovement bulunamadı."); return; }
        GameObject playerRoot = pm.gameObject;
        log.AppendLine($"[Player]    Root → '{playerRoot.name}'");

        // ── 3. MESH FBX importer: importAnimation=false ──────────────────────────
        var meshImporter = AssetImporter.GetAtPath(MESH_FBX) as ModelImporter;
        if (meshImporter != null && meshImporter.importAnimation)
        {
            meshImporter.importAnimation = false;
            meshImporter.SaveAndReimport();
            log.AppendLine("[MeshFBX]   importAnimation → false (reimport)");
        }
        else { log.AppendLine("[MeshFBX]   Import ayarları ok"); }
        log.AppendLine($"[MeshFBX]   → {MESH_FBX}");

        // ── 4. ANIM FBX importer: importAnimation=true, Generic, CopyAvatar ──────
        var animImporter = AssetImporter.GetAtPath(ANIM_FBX) as ModelImporter;
        if (animImporter != null)
        {
            bool animDirty = false;
            if (!animImporter.importAnimation)
            { animImporter.importAnimation = true; animDirty = true; }

            if (animImporter.animationType != ModelImporterAnimationType.Generic)
            { animImporter.animationType = ModelImporterAnimationType.Generic; animDirty = true; }

            // Copy avatar from character FBX so bone paths align
            Avatar charAvatar = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(MESH_FBX))
                if (a is Avatar) { charAvatar = (Avatar)a; break; }

            if (charAvatar != null
                && (animImporter.avatarSetup != ModelImporterAvatarSetup.CopyFromOther
                    || animImporter.sourceAvatar != charAvatar))
            {
                animImporter.avatarSetup  = ModelImporterAvatarSetup.CopyFromOther;
                animImporter.sourceAvatar = charAvatar;
                animDirty = true;
                log.AppendLine("[AnimFBX]   avatarSetup → CopyFromOther (character FBX avatar, Humanoid)");
            }
            else if (charAvatar == null)
            {
                // Fallback: no avatar found in mesh FBX — keep CreateFromThisModel
                log.AppendLine("[AnimFBX]   UYARI: Mesh FBX avatar bulunamadı, CopyFromOther atlandı");
            }

            if (animDirty) { animImporter.SaveAndReimport(); log.AppendLine("[AnimFBX]   Reimport yapıldı"); }
            else            { log.AppendLine("[AnimFBX]   Import ayarları ok"); }
        }
        log.AppendLine($"[AnimFBX]   → {ANIM_FBX}");

        // ── 5. Find or recreate VisualRoot_FemaleSamurai ─────────────────────────
        foreach (string n in new[] { OLD_VISUAL_NAME })
        {
            var old = playerRoot.transform.Find(n);
            if (old != null) { UObject.DestroyImmediate(old.gameObject); log.AppendLine($"[Cleanup]   '{n}' silindi"); }
        }

        Transform visualTr = playerRoot.transform.Find(VISUAL_NAME);
        GameObject visual;
        if (visualTr == null)
        {
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MESH_FBX);
            if (fbxAsset == null) { Abort(log, batch, $"Mesh FBX yüklenemedi:\n{MESH_FBX}"); return; }
            visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, playerRoot.transform);
            visual.name = VISUAL_NAME;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale    = Vector3.one;
            log.AppendLine($"[Visual]    Yeni oluşturuldu → '{VISUAL_NAME}'");
        }
        else
        {
            visual = visualTr.gameObject;
            log.AppendLine($"[Visual]    Mevcut kullanılıyor → '{VISUAL_NAME}'");
        }

        // ── 6. Normal map import type ────────────────────────────────────────────
        var normImp = AssetImporter.GetAtPath(TEX_NORMAL) as TextureImporter;
        if (normImp != null && normImp.textureType != TextureImporterType.NormalMap)
        {
            normImp.textureType = TextureImporterType.NormalMap;
            normImp.SaveAndReimport();
            log.AppendLine("[Tex]       Normal map import type → NormalMap (reimport)");
        }

        // ── 7. Create / update URP Lit material ──────────────────────────────────
        EnsureFolder("Assets/Art/Characters/Player/FemaleSamurai/Materials");

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_BASE);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_NORMAL);
        var metal  = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_METALLIC);

        log.AppendLine($"[Tex]       Base color → {(albedo != null ? "OK" : "BULUNAMADI")} ({TEX_BASE})");
        log.AppendLine($"[Tex]       Normal     → {(normal != null ? "OK" : "BULUNAMADI")} ({TEX_NORMAL})");
        log.AppendLine($"[Tex]       Metallic   → {(metal  != null ? "OK" : "BULUNAMADI")} ({TEX_METALLIC})");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MATERIAL_PATH);
            log.AppendLine($"[Material]  Yeni oluşturuldu → {MATERIAL_PATH}");
        }
        else
        {
            if (mat.shader != shader) mat.shader = shader;
            log.AppendLine($"[Material]  Mevcut güncellendi → {MATERIAL_PATH}");
        }

        bool isURP = shader != null && shader.name.Contains("Universal");
        string albedoProp = isURP ? "_BaseMap" : "_MainTex";

        if (albedo != null) mat.SetTexture(albedoProp, albedo);
        mat.SetColor(isURP ? "_BaseColor" : "_Color", Color.white);

        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            if (isURP) mat.EnableKeyword("_NORMALMAP");
        }
        if (metal != null) mat.SetTexture("_MetallicGlossMap", metal);

        mat.SetFloat("_Metallic",   0f);
        mat.SetFloat("_Smoothness", 0.35f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // ── 8. Assign material to all renderers ──────────────────────────────────
        var allRenderers = visual.GetComponentsInChildren<Renderer>(true);
        int rendererCount = 0;
        foreach (var r in allRenderers)
        {
            var slots = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = mat;
            r.sharedMaterials = slots;
            rendererCount++;
        }
        log.AppendLine($"[Material]  {rendererCount} renderer'a atandı");

        // ── 9. Load animation clips from ANIM FBX ────────────────────────────────
        var usableClips = new List<AnimationClip>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ANIM_FBX))
        {
            if (asset is AnimationClip c && !c.name.StartsWith("__preview__") && c.length > 0.05f)
                usableClips.Add(c);
        }

        log.AppendLine($"[Anim]      {usableClips.Count} kullanılabilir clip:");
        foreach (var c in usableClips)
            log.AppendLine($"             '{c.name}' — {c.length:F2}s / {Mathf.RoundToInt(c.frameRate * c.length)} frame");
        if (usableClips.Count == 0)
            log.AppendLine("[Anim]      UYARI: Merged_Animations içinde kullanılabilir clip yok.");

        var idleClip = FindClip(usableClips, "idle", "stand", "rest", "tpose", "t-pose");
        var moveClip = FindClip(usableClips, "walk", "run", "move", "locomotion", "jog");
        var jumpClip = FindClip(usableClips, "jump", "fall", "air", "leap");
        var dashClip = FindClip(usableClips, "dash", "roll", "dodge", "sprint");

        if (idleClip == null && usableClips.Count > 0) idleClip = usableClips[0];
        if (moveClip == null) moveClip = idleClip;
        if (jumpClip == null) jumpClip = idleClip;
        if (dashClip == null) dashClip = idleClip;

        log.AppendLine($"[Anim]      → Idle : {(idleClip != null ? idleClip.name : "yok")}");
        log.AppendLine($"[Anim]      → Move : {(moveClip != null ? moveClip.name : "yok")}");
        log.AppendLine($"[Anim]      → Jump : {(jumpClip != null ? jumpClip.name : "yok")}");
        log.AppendLine($"[Anim]      → Dash : {(dashClip != null ? dashClip.name : "yok")}");

        // ── 10. Animator Controller ───────────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            log.AppendLine($"[Ctrl]      Yeni oluşturuldu");
        }

        EnsureParam(controller, "Hor",       AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",      AnimatorControllerParameterType.Float);
        EnsureParam(controller, "State",     AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump",    AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "IsDashing", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Remove Idle_Placeholder placeholder state if present
        RemoveState(sm, "Idle_Placeholder");

        var idleState = GetOrCreateState(sm, "Idle");
        var moveState = GetOrCreateState(sm, "Move");
        var jumpState = GetOrCreateState(sm, "Jump");
        var dashState = GetOrCreateState(sm, "Dash");

        if (idleClip != null) idleState.motion = idleClip;
        if (moveClip != null) moveState.motion = moveClip;
        if (jumpClip != null) jumpState.motion = jumpClip;
        if (dashClip != null) dashState.motion = dashClip;

        sm.defaultState = idleState;

        // Clear all transitions on managed states to prevent duplicates
        ClearTransitions(idleState);
        ClearTransitions(moveState);
        ClearTransitions(jumpState);
        ClearTransitions(dashState);
        ClearAnyStateTransitions(sm, jumpState);
        ClearAnyStateTransitions(sm, dashState);

        // Idle ↔ Move
        AddTransition(idleState, moveState, 0.15f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
        AddTransition(moveState, idleState, 0.15f).AddCondition(AnimatorConditionMode.Less,    0.1f, "State");

        // Any → Jump / Jump → Idle
        {
            var t = sm.AddAnyStateTransition(jumpState);
            t.hasExitTime = false; t.duration = 0.1f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, "IsJump");
        }
        AddTransition(jumpState, idleState, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0, "IsJump");

        // Any → Dash / Dash → Idle
        {
            var t = sm.AddAnyStateTransition(dashState);
            t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");
        }
        AddTransition(dashState, idleState, 0.1f).AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        log.AppendLine("[Ctrl]      States: Idle, Move, Jump, Dash + transitions rebuilt");

        // ── 11. Animator component ────────────────────────────────────────────────
        Animator anim = visual.GetComponent<Animator>()
                     ?? visual.GetComponentInChildren<Animator>(true);
        if (anim == null) { anim = visual.AddComponent<Animator>(); log.AppendLine("[Anim]      Animator eklendi"); }

        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        log.AppendLine($"[Anim]      Host → '{anim.gameObject.name}', controller atandı, applyRootMotion=false");

        // ── 12. CharacterAnimatorBridge ───────────────────────────────────────────
        GameObject bridgeHost = anim.gameObject;
        foreach (var b in visual.GetComponentsInChildren<CharacterAnimatorBridge>(true))
            if (b.gameObject != bridgeHost) UObject.DestroyImmediate(b);
        var rootBridgeDup = playerRoot.GetComponent<CharacterAnimatorBridge>();
        if (rootBridgeDup != null) UObject.DestroyImmediate(rootBridgeDup);
        if (bridgeHost.GetComponent<CharacterAnimatorBridge>() == null)
            bridgeHost.AddComponent<CharacterAnimatorBridge>();
        log.AppendLine($"[Bridge]    CharacterAnimatorBridge → '{bridgeHost.name}'");

        // ── 13. Scale & foot alignment ────────────────────────────────────────────
        // Set to identity first so bounds reflect model at natural size
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale    = Vector3.one;
        visual.transform.localRotation = Quaternion.identity;

        float scaleFactor = 1f;
        float footOffsetY = 0f;

        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null)
        {
            Bounds b = GetCombinedBounds(visual);
            float meshHeight = b.size.y;
            float targetH    = cc.height * 0.95f;

            if (meshHeight > 0.01f)
            {
                scaleFactor = targetH / meshHeight;
                // Align model bottom to CC feet
                float ccFeetLocalY    = cc.center.y - cc.height * 0.5f;
                float meshBottomLocal = b.min.y - playerRoot.transform.position.y;
                footOffsetY = ccFeetLocalY - meshBottomLocal * scaleFactor;
            }

            visual.transform.localScale    = Vector3.one * scaleFactor;
            visual.transform.localPosition = new Vector3(0f, footOffsetY, 0f);

            log.AppendLine($"[Scale]     meshH={b.size.y:F3}m CC.h={cc.height:F3}m → scale={scaleFactor:F4}");
            log.AppendLine($"[Align]     ccFeetLocalY={(cc.center.y - cc.height * 0.5f):F3}, footOffsetY={footOffsetY:F4}");
        }
        else
        {
            log.AppendLine("[Scale]     CharacterController yok → scale=1, pos=0");
        }

        log.AppendLine($"[Visual]    localPos={visual.transform.localPosition}");
        log.AppendLine($"[Visual]    localScale={visual.transform.localScale}");
        log.AppendLine($"[Visual]    localRot={visual.transform.localEulerAngles}");
        log.AppendLine("[Visual]    Karakter ters bakıyorsa: Tools→Hollow Atlas→Fix Visual Rotation Y180");

        // ── 14. Body_010 ─────────────────────────────────────────────────────────
        bool bodyHidden = false;
        Transform bodyTr = playerRoot.transform.Find(BODY_OLD);
        if (bodyTr != null)
        {
            foreach (var r in bodyTr.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            bodyHidden = true;
            log.AppendLine($"[Body010]   Renderer'lar disabled");
        }
        else { log.AppendLine($"[Body010]   Bulunamadı"); }

        // ── 15. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool sceneSaved = EditorSceneManager.SaveScene(active);
        log.AppendLine(sceneSaved ? "[Scene]     Kaydedildi" : "[Scene]     UYARI: Kaydedilemedi");

        // ── 16. Update prefab ─────────────────────────────────────────────────────
        bool prefabSaved = false;
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Player");
            var copy = UnityEngine.Object.Instantiate(playerRoot);
            copy.name = playerRoot.name;
            PrefabUtility.SaveAsPrefabAsset(copy, PREFAB_PATH, out prefabSaved);
            UnityEngine.Object.DestroyImmediate(copy);
        }
        catch (Exception ex) { log.AppendLine($"[Prefab]    HATA: {ex.Message}"); }
        log.AppendLine(prefabSaved ? $"[Prefab]    Güncellendi → {PREFAB_PATH}" : "[Prefab]    Atlandı");

        // ── 17. Final report ──────────────────────────────────────────────────────
        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  Mesh FBX path          : {MESH_FBX}");
        log.AppendLine($"  Anim FBX path          : {ANIM_FBX}");
        log.AppendLine($"  Clip sayısı            : {usableClips.Count}");
        log.AppendLine($"  Idle clip              : {(idleClip != null ? idleClip.name : "yok")}");
        log.AppendLine($"  Move clip              : {(moveClip != null ? moveClip.name : "yok")}");
        log.AppendLine($"  Jump clip              : {(jumpClip != null ? jumpClip.name : "yok")}");
        log.AppendLine($"  Dash clip              : {(dashClip != null ? dashClip.name : "yok")}");
        log.AppendLine($"  Material               : {MATERIAL_PATH}");
        log.AppendLine($"  Base color             : {(albedo != null ? "OK" : "yok")}");
        log.AppendLine($"  Normal map             : {(normal != null ? "OK" : "yok")}");
        log.AppendLine($"  Renderer count         : {rendererCount}");
        log.AppendLine($"  VisualRoot localPos    : {visual.transform.localPosition}");
        log.AppendLine($"  VisualRoot localRot    : {visual.transform.localEulerAngles}");
        log.AppendLine($"  VisualRoot localScale  : {visual.transform.localScale}");
        log.AppendLine($"  Body_010 hidden        : {bodyHidden}");
        log.AppendLine($"  Scene saved            : {sceneSaved}");
        log.AppendLine($"  Prefab updated         : {prefabSaved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            Selection.activeGameObject = visual;
            EditorUtility.DisplayDialog("Full Repair Tamamlandı",
                $"Renderer: {rendererCount}\nClip sayısı: {usableClips.Count}" +
                $"\nScene saved: {sceneSaved}\n\nDetaylar Console penceresinde.", "Tamam");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void Abort(StringBuilder log, bool batch, string msg)
    {
        log.AppendLine($"[ABORT]     {msg}");
        Debug.LogError(log.ToString());
        if (!batch) EditorUtility.DisplayDialog("Full Repair Hatası", msg, "Tamam");
    }

    static Bounds GetCombinedBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static AnimationClip FindClip(List<AnimationClip> clips, params string[] keywords)
    {
        foreach (var clip in clips)
        {
            string lower = clip.name.ToLower();
            foreach (var kw in keywords)
                if (lower.Contains(kw)) return clip;
        }
        return null;
    }

    static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == name) return cs.state;
        return sm.AddState(name);
    }

    static void RemoveState(AnimatorStateMachine sm, string name)
    {
        AnimatorState found = null;
        foreach (var cs in sm.states)
            if (cs.state.name == name) { found = cs.state; break; }
        if (found != null) sm.RemoveState(found);
    }

    static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = duration;
        return t;
    }

    static void ClearTransitions(AnimatorState state)
    {
        foreach (var t in state.transitions)
            state.RemoveTransition(t);
    }

    static void ClearAnyStateTransitions(AnimatorStateMachine sm, AnimatorState target)
    {
        var snapshot = sm.anyStateTransitions;
        foreach (var t in snapshot)
            if (t.destinationState == target) sm.RemoveAnyStateTransition(t);
    }

    static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters) if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int sep = path.LastIndexOf('/');
        if (sep < 0) return;
        string parent = path.Substring(0, sep);
        string child  = path.Substring(sep + 1);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
