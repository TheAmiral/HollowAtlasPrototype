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

public static class FemaleSamuraiIdleMoveFinalFix
{
    const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    const string CONTROLLER_PATH = "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    const string ANIM_FOLDER     = "Assets/Art/Characters/Player/FemaleSamurai/Animations/RuntimeLooped";
    const string PREFAB_PATH     = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";

    const string IDLE_ANIM_PATH  = ANIM_FOLDER + "/FemaleSamurai_Idle_Looped.anim";
    const string MOVE_ANIM_PATH  = ANIM_FOLDER + "/FemaleSamurai_Move_Looped.anim";

    // Strict idle keywords — walk/run/move must NEVER appear here
    static readonly string[] IDLE_KEYWORDS = { "idle", "stand", "breathe", "breathing", "wait", "rest" };

    // Move keywords
    static readonly string[] MOVE_KEYWORDS = { "walk", "run", "move", "locomotion", "jog" };

    [MenuItem("Tools/Hollow Atlas/Idle Move Final Fix (Female Samurai)")]
    public static void Fix()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiIdleMoveFinalFix                          ║");
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

        // ── 2. Load all source clips from animation FBX ───────────────────────────
        string animFbxPath = FindAnimFbx(log);
        List<AnimationClip> srcClips = new List<AnimationClip>();
        if (animFbxPath != null)
        {
            srcClips = AssetDatabase.LoadAllAssetsAtPath(animFbxPath)
                .OfType<AnimationClip>()
                .Where(c => c != null
                         && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                         && c.length > 0.01f)
                .ToList();

            log.AppendLine($"[FBX]    {srcClips.Count} source clip:");
            foreach (var c in srcClips)
                log.AppendLine($"         '{c.name}'  {c.length:F3}s");
        }

        // ── 3. Determine idle source (strict — no walk/run fallback) ──────────────
        AnimationClip srcIdle = srcClips.FirstOrDefault(c =>
            IDLE_KEYWORDS.Any(k => c.name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));

        bool hasRealIdleClip = srcIdle != null;
        log.AppendLine(hasRealIdleClip
            ? $"[Idle]   Gerçek idle clip bulundu → '{srcIdle.name}' {srcIdle.length:F3}s"
            : "[Idle]   Gerçek idle clip YOK → Idle state motion=null (static pose)");

        // ── 4. Determine move source ──────────────────────────────────────────────
        AnimationClip srcMove = srcClips.FirstOrDefault(c =>
            MOVE_KEYWORDS.Any(k => c.name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        if (srcMove == null)
            srcMove = srcClips.OrderByDescending(c => c.length).FirstOrDefault();

        log.AppendLine(srcMove != null
            ? $"[Move]   Move clip → '{srcMove.name}' {srcMove.length:F3}s"
            : "[Move]   UYARI: Move clip bulunamadı");

        // ── 5. Ensure output folder ───────────────────────────────────────────────
        EnsureFolder("Assets/Art/Characters/Player/FemaleSamurai/Animations");
        EnsureFolder(ANIM_FOLDER);

        // ── 6. Write idle .anim (only if real idle clip found) ────────────────────
        if (hasRealIdleClip)
            WriteLoopedAnim(srcIdle, IDLE_ANIM_PATH, looped: true, log);
        else
        {
            // Delete stale idle .anim that may have been a walk clip copy
            if (AssetDatabase.AssetPathToGUID(IDLE_ANIM_PATH) != "")
            {
                AssetDatabase.DeleteAsset(IDLE_ANIM_PATH);
                log.AppendLine("[Idle]   Eski hatalı Idle .anim silindi");
            }
        }

        // ── 7. Write move .anim ───────────────────────────────────────────────────
        if (srcMove != null)
            WriteLoopedAnim(srcMove, MOVE_ANIM_PATH, looped: true, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AnimationClip idleAnim = hasRealIdleClip
            ? AssetDatabase.LoadAssetAtPath<AnimationClip>(IDLE_ANIM_PATH)
            : null;
        AnimationClip moveAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(MOVE_ANIM_PATH);

        log.AppendLine($"[Anim]   Idle .anim loopTime : {(idleAnim != null ? AnimationUtility.GetAnimationClipSettings(idleAnim).loopTime.ToString() : "N/A (null motion)")}");
        log.AppendLine($"[Anim]   Move .anim loopTime : {(moveAnim != null ? AnimationUtility.GetAnimationClipSettings(moveAnim).loopTime.ToString() : "DOSYA YOK")}");

        // ── 8. Rebuild Animator Controller ───────────────────────────────────────
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

        // Ensure parameters
        EnsureParam(controller, "State",     AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Hor",       AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",      AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump",    AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "IsDashing", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Wipe all states and any-state transitions for clean rebuild
        foreach (var cs in sm.states.ToArray())             sm.RemoveState(cs.state);
        foreach (var t  in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        // Create states
        var idleState = sm.AddState("Idle");
        var moveState = sm.AddState("Move");

        // Idle: null motion if no real idle clip (character holds bind pose)
        idleState.motion             = idleAnim; // null is valid — animator stays in bind pose
        idleState.speed              = 1f;
        idleState.writeDefaultValues = false;

        // Move: looped move clip
        moveState.motion             = moveAnim;
        moveState.speed              = 1f;
        moveState.writeDefaultValues = false;

        // Default state is always Idle
        sm.defaultState = idleState;

        // Idle → Move  (State > 0.1)
        {
            var t = idleState.AddTransition(moveState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.03f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
        }

        // Move → Idle  (State < 0.1)
        {
            var t = moveState.AddTransition(idleState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.03f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "State");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        log.AppendLine($"[Ctrl]   Default state : {sm.defaultState?.name}");
        log.AppendLine($"[Ctrl]   State sayısı  : {sm.states.Length}  (Idle + Move)");
        log.AppendLine("[Ctrl]   AnyState transitions : temizlendi");

        // Verify transitions
        var idleSt = sm.states.FirstOrDefault(s => s.state.name == "Idle").state;
        bool idleToMove = idleSt?.transitions.Any(t =>
            t.destinationState?.name == "Move") ?? false;
        var moveSt = sm.states.FirstOrDefault(s => s.state.name == "Move").state;
        bool moveToIdle = moveSt?.transitions.Any(t =>
            t.destinationState?.name == "Idle") ?? false;

        log.AppendLine($"[Ctrl]   Idle→Move transition : {idleToMove}");
        log.AppendLine($"[Ctrl]   Move→Idle transition : {moveToIdle}");

        // ── 9. Wire Animator (no scale/pos/rot touch) ─────────────────────────────
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
                else { log.AppendLine("[Anim]   UYARI: Animator bulunamadı"); }
            }
            else { log.AppendLine($"[Anim]   UYARI: '{VISUAL_NAME}' Player altında bulunamadı"); }
        }
        else { log.AppendLine("[Anim]   UYARI: PlayerMovement bulunamadı"); }

        // ── 10. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool sceneSaved = EditorSceneManager.SaveScene(active);
        log.AppendLine(sceneSaved ? "[Scene]  Kaydedildi" : "[Scene]  UYARI: Kaydedilemedi");

        // ── 11. Update prefab ─────────────────────────────────────────────────────
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

        // ── 12. Final report ──────────────────────────────────────────────────────
        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  Source clip sayısı          : {srcClips.Count}");
        log.AppendLine($"  Gerçek idle clip bulundu mu : {hasRealIdleClip}");
        log.AppendLine($"  Idle state motion           : {(idleAnim != null ? idleAnim.name : "null (static/bind pose)")}");
        log.AppendLine($"  Move state motion           : {(moveAnim != null ? moveAnim.name : "YOK")}");
        log.AppendLine($"  Default state = Idle        : {sm.defaultState?.name == "Idle"}");
        log.AppendLine($"  Idle→Move (State>0.1)       : {idleToMove}");
        log.AppendLine($"  Move→Idle (State<0.1)       : {moveToIdle}");
        log.AppendLine($"  AnyState→Move temizlendi    : True");
        log.AppendLine($"  applyRootMotion = false     : {animWired}");
        log.AppendLine($"  Scene saved                 : {sceneSaved}");
        log.AppendLine($"  Prefab updated              : {prefabSaved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            EditorUtility.DisplayDialog("Idle/Move Final Fix Tamamlandı",
                $"Idle clip: {(hasRealIdleClip ? srcIdle.name : "YOK — static pose")}\n" +
                $"Move clip: {(srcMove != null ? srcMove.name : "YOK")}\n" +
                $"Scene saved: {sceneSaved}\n\n" +
                "Detaylar Console penceresinde.", "Tamam");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string FindAnimFbx(StringBuilder log)
    {
        const string knownPath =
            "Assets/Art/Characters/Player/FemaleSamurai/Meshy_AI_Moonbound_Samurai_biped/" +
            "Meshy_AI_Moonbound_Samurai_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
        if (AssetDatabase.AssetPathToGUID(knownPath) != "")
        {
            log.AppendLine("[FBX]    " + knownPath);
            return knownPath;
        }
        string[] guids = AssetDatabase.FindAssets("Merged_Animations t:Model",
            new[] { "Assets/Art/Characters/Player/FemaleSamurai" });
        if (guids.Length > 0)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[0]);
            log.AppendLine("[FBX]    " + p);
            return p;
        }
        log.AppendLine("[FBX]    HATA: Animations FBX bulunamadı");
        return null;
    }

    static void WriteLoopedAnim(AnimationClip source, string destPath, bool looped, StringBuilder log)
    {
        var newClip = new AnimationClip();
        EditorUtility.CopySerialized(source, newClip);
        newClip.name     = System.IO.Path.GetFileNameWithoutExtension(destPath);
        newClip.wrapMode = looped ? WrapMode.Loop : WrapMode.Once;

        var s = AnimationUtility.GetAnimationClipSettings(newClip);
        s.loopTime             = looped;
        s.loopBlend            = looped;
        s.loopBlendOrientation = looped;
        s.loopBlendPositionY   = looped;
        s.loopBlendPositionXZ  = looped;
        AnimationUtility.SetAnimationClipSettings(newClip, s);

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(newClip, existing);
            AnimationUtility.SetAnimationClipSettings(existing, s);
            EditorUtility.SetDirty(existing);
            log.AppendLine($"[Write]  Güncellendi → {destPath}  (looped={looped})");
        }
        else
        {
            AssetDatabase.CreateAsset(newClip, destPath);
            log.AppendLine($"[Write]  Oluşturuldu → {destPath}  (looped={looped})");
        }
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
}
