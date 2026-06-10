using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

public static class FemaleSamuraiAnimCopyLoopFix
{
    const string SCENE_PATH       = "Assets/Scenes/Prototype_01.unity";
    const string CONTROLLER_PATH  = "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    const string ANIM_OUT_FOLDER  = "Assets/Art/Characters/Player/FemaleSamurai/Animations/RuntimeLooped";
    const string PREFAB_PATH      = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    const string VISUAL_NAME      = "VisualRoot_FemaleSamurai";

    const string IDLE_ANIM_PATH   = ANIM_OUT_FOLDER + "/FemaleSamurai_Idle_Looped.anim";
    const string MOVE_ANIM_PATH   = ANIM_OUT_FOLDER + "/FemaleSamurai_Move_Looped.anim";
    const string DASH_ANIM_PATH   = ANIM_OUT_FOLDER + "/FemaleSamurai_Dash_Once.anim";

    [MenuItem("Tools/Hollow Atlas/Anim Copy Loop Fix (Female Samurai)")]
    public static void Fix()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiAnimCopyLoopFix                           ║");
        log.AppendLine("╚══════════════════════════════════════════════════════════╝");

        // ── 1. Open scene ────────────────────────────────────────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (active.path != SCENE_PATH)
        {
            if (!batch) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
            log.AppendLine("[Scene]  Açıldı → " + SCENE_PATH);
        }
        else
        {
            log.AppendLine("[Scene]  Zaten açık");
        }

        // ── 2. Find animation FBX ────────────────────────────────────────────────
        string animFbxPath = FindAnimFbx(log);
        if (animFbxPath == null)
        {
            Abort(log, batch, "Animations FBX bulunamadı.");
            return;
        }

        // ── 3. Load real AnimationClip assets ────────────────────────────────────
        var allClips = AssetDatabase.LoadAllAssetsAtPath(animFbxPath)
            .OfType<AnimationClip>()
            .Where(c => c != null
                     && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                     && c.length > 0.01f)
            .ToList();

        log.AppendLine($"[Clips]  {allClips.Count} kullanılabilir source clip:");
        foreach (var c in allClips)
            log.AppendLine($"         '{c.name}'  {c.length:F3}s");

        if (allClips.Count == 0)
        {
            Abort(log, batch, "FBX içinde kullanılabilir AnimationClip bulunamadı.");
            return;
        }

        // ── 4. Select source clips ───────────────────────────────────────────────
        AnimationClip srcIdle = SelectIdle(allClips);
        AnimationClip srcMove = SelectMove(allClips);
        AnimationClip srcDash = SelectDash(allClips);
        bool hasDash = srcDash != null;

        log.AppendLine($"[Seçim]  Idle source  → '{(srcIdle != null ? srcIdle.name + " " + srcIdle.length.ToString("F3") + "s" : "yok")}'");
        log.AppendLine($"[Seçim]  Move source  → '{(srcMove != null ? srcMove.name + " " + srcMove.length.ToString("F3") + "s" : "yok")}'");
        log.AppendLine($"[Seçim]  Dash source  → '{(hasDash ? srcDash.name + " " + srcDash.length.ToString("F3") + "s" : "yok — Dash state oluşturulmayacak")}'");

        if (srcIdle == null && srcMove == null)
        {
            Abort(log, batch, "Idle ve Move için uygun clip bulunamadı.");
            return;
        }
        if (srcIdle == null) srcIdle = srcMove;
        if (srcMove == null) srcMove = srcIdle;

        // ── 5. Create output folder ──────────────────────────────────────────────
        EnsureFolder("Assets/Art/Characters/Player/FemaleSamurai/Animations");
        EnsureFolder(ANIM_OUT_FOLDER);

        // ── 6. Copy clips as standalone .anim assets ─────────────────────────────
        AnimationClip idleAnim = CopyClip(srcIdle, IDLE_ANIM_PATH, looped: true,  log);
        AnimationClip moveAnim = CopyClip(srcMove, MOVE_ANIM_PATH, looped: true,  log);
        AnimationClip dashAnim = hasDash ? CopyClip(srcDash, DASH_ANIM_PATH, looped: false, log) : null;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Re-load from disk to get proper asset references
        idleAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(IDLE_ANIM_PATH);
        moveAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(MOVE_ANIM_PATH);
        if (hasDash) dashAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(DASH_ANIM_PATH);

        log.AppendLine($"[Anim]   Idle .anim loopTime  : {VerifyLoop(IDLE_ANIM_PATH)}");
        log.AppendLine($"[Anim]   Move .anim loopTime  : {VerifyLoop(MOVE_ANIM_PATH)}");
        if (hasDash)
            log.AppendLine($"[Anim]   Dash .anim loopTime  : {VerifyLoop(DASH_ANIM_PATH)}");

        // ── 7. Build Animator Controller ─────────────────────────────────────────
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            log.AppendLine("[Ctrl]   Yeni oluşturuldu");
        }
        else
        {
            log.AppendLine("[Ctrl]   Mevcut controller güncelleniyor");
        }

        EnsureParam(controller, "Hor",       AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",      AnimatorControllerParameterType.Float);
        EnsureParam(controller, "State",     AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump",    AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "IsDashing", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Wipe existing state machine
        foreach (var cs in sm.states.ToArray())           sm.RemoveState(cs.state);
        foreach (var t  in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        // States
        var idleState = sm.AddState("Idle");
        var moveState = sm.AddState("Move");
        AnimatorState dashState = hasDash ? sm.AddState("Dash") : null;

        idleState.motion           = idleAnim;
        idleState.speed            = 1f;
        idleState.writeDefaultValues = false;

        moveState.motion           = moveAnim;
        moveState.speed            = 1f;
        moveState.writeDefaultValues = false;

        if (hasDash && dashAnim != null)
        {
            dashState.motion           = dashAnim;
            dashState.speed            = 1f;
            dashState.writeDefaultValues = false;
        }

        sm.defaultState = idleState;

        // Idle → Move
        {
            var t = idleState.AddTransition(moveState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.05f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
        }
        // Move → Idle
        {
            var t = moveState.AddTransition(idleState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.05f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "State");
        }

        if (hasDash)
        {
            // Any → Dash
            {
                var t = sm.AddAnyStateTransition(dashState);
                t.hasExitTime         = false;
                t.hasFixedDuration    = true;
                t.duration            = 0.02f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");
            }
            // Dash → Move
            {
                var t = dashState.AddTransition(moveState);
                t.hasExitTime      = true;
                t.exitTime         = 0.75f;
                t.hasFixedDuration = true;
                t.duration         = 0.05f;
                t.AddCondition(AnimatorConditionMode.IfNot,  0,    "IsDashing");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
            }
            // Dash → Idle
            {
                var t = dashState.AddTransition(idleState);
                t.hasExitTime      = true;
                t.exitTime         = 0.75f;
                t.hasFixedDuration = true;
                t.duration         = 0.05f;
                t.AddCondition(AnimatorConditionMode.IfNot, 0,    "IsDashing");
                t.AddCondition(AnimatorConditionMode.Less,  0.1f, "State");
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        int stateCount = sm.states.Length;
        log.AppendLine($"[Ctrl]   {stateCount} state, transitionlar kuruldu");

        // ── 8. Wire Animator on VisualRoot (NO scale/pos/rot touch) ─────────────
        bool animWired = false;
        var pm = UObject.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            Transform visualTr = pm.transform.Find(VISUAL_NAME);
            if (visualTr != null)
            {
                Animator anim = visualTr.GetComponent<Animator>()
                             ?? visualTr.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.runtimeAnimatorController = controller;
                    anim.applyRootMotion           = false;
                    animWired = true;
                    log.AppendLine($"[Anim]   '{anim.gameObject.name}' → controller atandı, applyRootMotion=false");
                }
                else
                {
                    log.AppendLine("[Anim]   UYARI: Animator bulunamadı");
                }

                var bridge = visualTr.GetComponentInChildren<CharacterAnimatorBridge>(true)
                          ?? pm.GetComponentInChildren<CharacterAnimatorBridge>(true);
                log.AppendLine(bridge != null
                    ? $"[Bridge] '{bridge.gameObject.name}' üzerinde mevcut"
                    : "[Bridge] UYARI: CharacterAnimatorBridge bulunamadı");
            }
            else
            {
                log.AppendLine($"[Anim]   UYARI: '{VISUAL_NAME}' Player altında bulunamadı");
            }
        }
        else
        {
            log.AppendLine("[Anim]   UYARI: PlayerMovement bulunamadı");
        }

        // ── 9. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool sceneSaved = EditorSceneManager.SaveScene(active);
        log.AppendLine(sceneSaved ? "[Scene]  Kaydedildi" : "[Scene]  UYARI: Kaydedilemedi");

        // ── 10. Update prefab ────────────────────────────────────────────────────
        bool prefabSaved = false;
        if (pm != null)
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                EnsureFolder("Assets/Prefabs/Player");
                var copy = UObject.Instantiate(pm.gameObject);
                copy.name = pm.gameObject.name;
                PrefabUtility.SaveAsPrefabAsset(copy, PREFAB_PATH, out prefabSaved);
                UObject.DestroyImmediate(copy);
            }
            catch (Exception ex) { log.AppendLine($"[Prefab] HATA: {ex.Message}"); }
        }
        log.AppendLine(prefabSaved ? $"[Prefab] Güncellendi → {PREFAB_PATH}" : "[Prefab] Atlandı");

        // ── 11. Final report ─────────────────────────────────────────────────────
        bool idleLoop = VerifyLoopBool(IDLE_ANIM_PATH);
        bool moveLoop = VerifyLoopBool(MOVE_ANIM_PATH);
        bool dashOnce = hasDash && !VerifyLoopBool(DASH_ANIM_PATH);

        bool idleMoveTransition = sm.states.Any(s => s.state.name == "Idle") &&
                                  sm.states.Any(s => s.state.name == "Move");

        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  FBX path                    : {animFbxPath}");
        log.AppendLine($"  Source clip sayısı          : {allClips.Count}");
        log.AppendLine($"  Idle source clip            : {(srcIdle != null ? srcIdle.name + " " + srcIdle.length.ToString("F3") + "s" : "yok")}");
        log.AppendLine($"  Move source clip            : {(srcMove != null ? srcMove.name + " " + srcMove.length.ToString("F3") + "s" : "yok")}");
        log.AppendLine($"  Dash source clip            : {(hasDash ? srcDash.name + " " + srcDash.length.ToString("F3") + "s" : "yok")}");
        log.AppendLine($"  Idle .anim path             : {IDLE_ANIM_PATH}");
        log.AppendLine($"  Move .anim path             : {MOVE_ANIM_PATH}");
        log.AppendLine(hasDash ? $"  Dash .anim path             : {DASH_ANIM_PATH}" : "  Dash .anim                 : oluşturulmadı");
        log.AppendLine($"  Idle .anim loopTime=true    : {idleLoop}");
        log.AppendLine($"  Move .anim loopTime=true    : {moveLoop}");
        if (hasDash) log.AppendLine($"  Dash .anim loopTime=false   : {dashOnce}");
        log.AppendLine($"  Controller state sayısı     : {stateCount}");
        log.AppendLine($"  Idle→Move / Move→Idle trans : {idleMoveTransition}");
        log.AppendLine($"  applyRootMotion = false     : {animWired}");
        log.AppendLine($"  Scene saved                 : {sceneSaved}");
        log.AppendLine($"  Prefab updated              : {prefabSaved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            EditorUtility.DisplayDialog("Anim Copy Loop Fix Tamamlandı",
                $"Source clip: {allClips.Count}\n" +
                $"Idle loop: {idleLoop} | Move loop: {moveLoop}\n" +
                $"Scene saved: {sceneSaved}\n\n" +
                "Detaylar Console penceresinde.", "Tamam");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string FindAnimFbx(StringBuilder log)
    {
        // Try known exact path first
        const string knownPath =
            "Assets/Art/Characters/Player/FemaleSamurai/Meshy_AI_Moonbound_Samurai_biped/" +
            "Meshy_AI_Moonbound_Samurai_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
        if (AssetDatabase.AssetPathToGUID(knownPath) != "")
        {
            log.AppendLine($"[FBX]    Bulundu (bilinen yol) → {knownPath}");
            return knownPath;
        }

        // Search by name fragment
        string[] guids = AssetDatabase.FindAssets("Merged_Animations t:Model",
            new[] { "Assets/Art/Characters/Player/FemaleSamurai" });
        if (guids.Length > 0)
        {
            string found = AssetDatabase.GUIDToAssetPath(guids[0]);
            log.AppendLine($"[FBX]    Bulundu (arama) → {found}");
            return found;
        }

        log.AppendLine("[FBX]    HATA: Animations FBX bulunamadı");
        return null;
    }

    static AnimationClip SelectIdle(List<AnimationClip> clips)
    {
        var hit = clips.FirstOrDefault(c =>
            Contains(c.name, "idle", "stand", "breathe", "wait", "rest"));
        if (hit != null) return hit;
        // Shortest clip above 0.3s
        return clips.Where(c => c.length > 0.3f).OrderBy(c => c.length).FirstOrDefault();
    }

    static AnimationClip SelectMove(List<AnimationClip> clips)
    {
        var hit = clips.FirstOrDefault(c =>
            Contains(c.name, "walk", "run", "move", "locomotion", "jog"));
        if (hit != null) return hit;
        // Longest clip
        return clips.OrderByDescending(c => c.length).FirstOrDefault();
    }

    static AnimationClip SelectDash(List<AnimationClip> clips) =>
        clips.FirstOrDefault(c => Contains(c.name, "dash", "roll", "dodge", "sprint"));

    static bool Contains(string s, params string[] keywords) =>
        keywords.Any(k => s.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

    static AnimationClip CopyClip(AnimationClip source, string destPath, bool looped, StringBuilder log)
    {
        var newClip = new AnimationClip();
        EditorUtility.CopySerialized(source, newClip);
        newClip.name     = System.IO.Path.GetFileNameWithoutExtension(destPath);
        newClip.wrapMode = looped ? WrapMode.Loop : WrapMode.Once;

        var settings = AnimationUtility.GetAnimationClipSettings(newClip);
        settings.loopTime             = looped;
        settings.loopBlend            = looped;
        settings.loopBlendOrientation = looped;
        settings.loopBlendPositionY   = looped;
        settings.loopBlendPositionXZ  = looped;
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // Overwrite or create
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(newClip, existing);
            // Re-apply settings after overwrite
            AnimationUtility.SetAnimationClipSettings(existing, settings);
            EditorUtility.SetDirty(existing);
            log.AppendLine($"[Copy]   Güncellendi → {destPath}  loop={looped}");
        }
        else
        {
            AssetDatabase.CreateAsset(newClip, destPath);
            log.AppendLine($"[Copy]   Oluşturuldu → {destPath}  loop={looped}");
        }
        return newClip;
    }

    static string VerifyLoop(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) return "DOSYA YOK";
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        return s.loopTime.ToString();
    }

    static bool VerifyLoopBool(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) return false;
        return AnimationUtility.GetAnimationClipSettings(clip).loopTime;
    }

    static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        if (c.parameters.Any(p => p.name == name)) return;
        c.AddParameter(name, type);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int sep = path.LastIndexOf('/');
        if (sep < 0) return;
        EnsureFolder(path.Substring(0, sep));
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(path.Substring(0, sep), path.Substring(sep + 1));
    }

    static void Abort(StringBuilder log, bool batch, string msg)
    {
        log.AppendLine($"[ABORT]  {msg}");
        Debug.LogError(log.ToString());
        if (!batch) EditorUtility.DisplayDialog("Fix Hatası", msg, "Tamam");
    }
}
