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

public static class FemaleSamuraiIdleAuditAndAssign
{
    const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    const string CONTROLLER_PATH = "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    const string ANIM_FOLDER     = "Assets/Art/Characters/Player/FemaleSamurai/Animations/RuntimeLooped";
    const string PREFAB_PATH     = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";

    const string IDLE_ANIM_PATH  = ANIM_FOLDER + "/FemaleSamurai_Idle_Looped.anim";
    const string MOVE_ANIM_PATH  = ANIM_FOLDER + "/FemaleSamurai_Move_Looped.anim";

    const int SAMPLE_COUNT = 20;

    // Keywords that identify a clip as definitely locomotion (never use as idle)
    static readonly string[] LOCO_KEYWORDS = { "run", "walk", "move", "locomotion", "jog" };
    // Keywords that identify a clip as definitely a one-shot action (never idle)
    static readonly string[] ACTION_KEYWORDS = { "attack", "hit", "death", "die", "dash", "roll", "dodge", "cast", "shoot" };
    // Keywords for explicit move selection
    static readonly string[] MOVE_KEYWORDS = { "run", "walk", "move", "locomotion", "jog" };

    struct ClipScore
    {
        public AnimationClip clip;
        public float positionVariation;   // root/hips world displacement proxy
        public float rotationVariation;   // total pose change
        public int   bindingCount;
        public bool  isLocoByName;
        public bool  isActionByName;
    }

    [MenuItem("Tools/Hollow Atlas/Idle Audit And Assign (Female Samurai)")]
    public static void Fix()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiIdleAuditAndAssign                        ║");
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
        else { log.AppendLine("[Scene]  Zaten açık"); }

        // ── 2. Find FBX ──────────────────────────────────────────────────────────
        string fbxPath = FindAnimFbx(log);
        if (fbxPath == null) { Abort(log, batch, "Animations FBX bulunamadı."); return; }

        // ── 3. Load all source clips ──────────────────────────────────────────────
        var srcClips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => c != null
                     && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                     && c.length > 0.01f)
            .ToList();

        if (srcClips.Count == 0) { Abort(log, batch, "Kullanılabilir AnimationClip bulunamadı."); return; }

        log.AppendLine($"[Clips]  {srcClips.Count} source clip:");
        foreach (var c in srcClips)
            log.AppendLine($"         '{c.name}'  {c.length:F3}s");

        // ── 4. Analyse every clip ─────────────────────────────────────────────────
        var scores = new List<ClipScore>();
        foreach (var clip in srcClips)
        {
            var score = AnalyseClip(clip);
            scores.Add(score);
        }

        // ── 5. Print full audit table ─────────────────────────────────────────────
        log.AppendLine("");
        log.AppendLine("── Clip Motion Audit ─────────────────────────────────────────");
        log.AppendLine($"{"Clip",-30} {"Dur",6} {"Bind",5} {"PosVar",8} {"RotVar",8} {"Loco",5} {"Act",5}");
        foreach (var s in scores)
            log.AppendLine(
                $"{s.clip.name,-30} {s.clip.length,6:F3} {s.bindingCount,5} " +
                $"{s.positionVariation,8:F4} {s.rotationVariation,8:F4} " +
                $"{s.isLocoByName,5} {s.isActionByName,5}");
        log.AppendLine("──────────────────────────────────────────────────────────────");

        // ── 6. Select move clip ───────────────────────────────────────────────────
        // Prefer explicit name match; fallback = highest positionVariation
        ClipScore? moveScore = scores.FirstOrDefault(s =>
            MOVE_KEYWORDS.Any(k => s.clip.name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        if (moveScore == null)
            moveScore = scores.OrderByDescending(s => s.positionVariation).First();

        AnimationClip srcMove = moveScore.Value.clip;
        log.AppendLine($"[Move]   Seçildi → '{srcMove.name}'  posVar={moveScore.Value.positionVariation:F4}");

        // ── 7. Select idle clip ───────────────────────────────────────────────────
        // Candidates: not move, not loco-by-name, not action-by-name, length >= 0.5s
        var idleCandidates = scores
            .Where(s => s.clip != srcMove
                     && !s.isLocoByName
                     && !s.isActionByName
                     && s.clip.length >= 0.5f)
            .OrderBy(s => s.positionVariation)   // least displacement = most idle-like
            .ToList();

        log.AppendLine($"\n[Idle]   {idleCandidates.Count} aday (posVar artan sıra):");
        foreach (var c in idleCandidates)
            log.AppendLine($"         '{c.clip.name}'  posVar={c.positionVariation:F4}  rotVar={c.rotationVariation:F4}");

        // If even after filtering nothing survives, try without length constraint
        if (idleCandidates.Count == 0)
        {
            idleCandidates = scores
                .Where(s => s.clip != srcMove && !s.isLocoByName && !s.isActionByName)
                .OrderBy(s => s.positionVariation)
                .ToList();
            log.AppendLine("[Idle]   length>=0.5 filtresiz yeniden denendi");
        }

        AnimationClip srcIdle = idleCandidates.Count > 0 ? idleCandidates[0].clip : null;

        // Extra guard: if best candidate still has very high posVar (close to move), warn
        if (srcIdle != null && idleCandidates.Count > 0)
        {
            float idlePosVar = idleCandidates[0].positionVariation;
            float movePosVar = moveScore.Value.positionVariation;
            if (movePosVar > 0.001f && idlePosVar / movePosVar > 0.6f)
                log.AppendLine($"[Idle]   UYARI: Idle adayı ({idlePosVar:F4}) Move'a ({movePosVar:F4}) yakın — elle kontrol önerilir");
        }

        log.AppendLine(srcIdle != null
            ? $"[Idle]   Seçildi → '{srcIdle.name}'  posVar={idleCandidates[0].positionVariation:F4}"
            : "[Idle]   Uygun aday bulunamadı → motion=null");

        // ── 8. Write .anim assets ─────────────────────────────────────────────────
        EnsureFolder("Assets/Art/Characters/Player/FemaleSamurai/Animations");
        EnsureFolder(ANIM_FOLDER);

        if (srcIdle != null)
            WriteAnim(srcIdle, IDLE_ANIM_PATH, looped: true, log);
        else
        {
            // Delete stale idle anim so Idle state gets null motion
            if (AssetDatabase.AssetPathToGUID(IDLE_ANIM_PATH) != "")
            {
                AssetDatabase.DeleteAsset(IDLE_ANIM_PATH);
                log.AppendLine("[Write]  Eski Idle .anim silindi (aday yok)");
            }
        }

        WriteAnim(srcMove, MOVE_ANIM_PATH, looped: true, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AnimationClip idleAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(IDLE_ANIM_PATH);
        AnimationClip moveAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(MOVE_ANIM_PATH);

        log.AppendLine($"[Check]  Idle .anim loopTime : {(idleAnim != null ? AnimationUtility.GetAnimationClipSettings(idleAnim).loopTime.ToString() : "N/A (null)")}");
        log.AppendLine($"[Check]  Move .anim loopTime : {(moveAnim != null ? AnimationUtility.GetAnimationClipSettings(moveAnim).loopTime.ToString() : "DOSYA YOK")}");

        // ── 9. Rebuild Animator Controller ───────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH)
                      ?? AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        EnsureParam(controller, "State",     AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Hor",       AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",      AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump",    AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "IsDashing", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        foreach (var cs in sm.states.ToArray())              sm.RemoveState(cs.state);
        foreach (var t  in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        var idleState = sm.AddState("Idle");
        var moveState = sm.AddState("Move");

        idleState.motion             = idleAnim;  // null = static bind pose if no idle found
        idleState.speed              = 1f;
        idleState.writeDefaultValues = false;

        moveState.motion             = moveAnim;
        moveState.speed              = 1f;
        moveState.writeDefaultValues = false;

        sm.defaultState = idleState;

        // Idle → Move
        {
            var t = idleState.AddTransition(moveState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.03f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
        }
        // Move → Idle
        {
            var t = moveState.AddTransition(idleState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.03f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "State");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        log.AppendLine($"[Ctrl]   State machine kuruldu — default: {sm.defaultState?.name}  states: {sm.states.Length}");

        // ── 10. Wire Animator ─────────────────────────────────────────────────────
        bool animWired = false;
        var pm = UObject.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            Transform vt = pm.transform.Find(VISUAL_NAME);
            if (vt != null)
            {
                Animator anim = vt.GetComponent<Animator>() ?? vt.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.runtimeAnimatorController = controller;
                    anim.applyRootMotion           = false;
                    animWired = true;
                    log.AppendLine($"[Anim]   '{anim.gameObject.name}' → controller atandı, applyRootMotion=false");
                }
                else log.AppendLine("[Anim]   UYARI: Animator bulunamadı");
            }
            else log.AppendLine($"[Anim]   UYARI: '{VISUAL_NAME}' bulunamadı");
        }
        else log.AppendLine("[Anim]   UYARI: PlayerMovement bulunamadı");

        // ── 11. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool sceneSaved = EditorSceneManager.SaveScene(active);
        log.AppendLine(sceneSaved ? "[Scene]  Kaydedildi" : "[Scene]  UYARI: Kaydedilemedi");

        // ── 12. Update prefab ─────────────────────────────────────────────────────
        bool prefabSaved = false;
        if (pm != null)
        {
            try
            {
                EnsureFolder("Assets/Prefabs"); EnsureFolder("Assets/Prefabs/Player");
                var copy = UObject.Instantiate(pm.gameObject);
                copy.name = pm.gameObject.name;
                PrefabUtility.SaveAsPrefabAsset(copy, PREFAB_PATH, out prefabSaved);
                UObject.DestroyImmediate(copy);
            }
            catch (Exception ex) { log.AppendLine($"[Prefab] HATA: {ex.Message}"); }
        }
        log.AppendLine(prefabSaved ? $"[Prefab] Güncellendi → {PREFAB_PATH}" : "[Prefab] Atlandı");

        // ── 13. Final report ──────────────────────────────────────────────────────
        bool idleToMoveExists = (sm.states.FirstOrDefault(s => s.state.name == "Idle").state
            ?.transitions.Any(t => t.destinationState?.name == "Move")) ?? false;
        bool moveToIdleExists = (sm.states.FirstOrDefault(s => s.state.name == "Move").state
            ?.transitions.Any(t => t.destinationState?.name == "Idle")) ?? false;

        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  Source clip sayısı          : {srcClips.Count}");
        log.AppendLine($"  Seçilen idle source         : {(srcIdle != null ? srcIdle.name + " " + srcIdle.length.ToString("F3") + "s" : "YOK")}");
        log.AppendLine($"  Seçilen move source         : {srcMove.name}  {srcMove.length:F3}s");
        log.AppendLine($"  Idle .anim path             : {IDLE_ANIM_PATH}");
        log.AppendLine($"  Move .anim path             : {MOVE_ANIM_PATH}");
        log.AppendLine($"  Idle .anim loopTime=true    : {(idleAnim != null ? AnimationUtility.GetAnimationClipSettings(idleAnim).loopTime.ToString() : "N/A")}");
        log.AppendLine($"  Move .anim loopTime=true    : {(moveAnim != null ? AnimationUtility.GetAnimationClipSettings(moveAnim).loopTime.ToString() : "DOSYA YOK")}");
        log.AppendLine($"  Controller default state    : {sm.defaultState?.name}");
        log.AppendLine($"  Idle state motion           : {(idleAnim != null ? idleAnim.name : "null (static pose)")}");
        log.AppendLine($"  Move state motion           : {(moveAnim != null ? moveAnim.name : "YOK")}");
        log.AppendLine($"  Idle→Move transition        : {idleToMoveExists}");
        log.AppendLine($"  Move→Idle transition        : {moveToIdleExists}");
        log.AppendLine($"  AnyState transitions        : temizlendi");
        log.AppendLine($"  applyRootMotion = false     : {animWired}");
        log.AppendLine($"  Scene saved                 : {sceneSaved}");
        log.AppendLine($"  Prefab updated              : {prefabSaved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            EditorUtility.DisplayDialog("Idle Audit & Assign Tamamlandı",
                $"Idle source: {(srcIdle != null ? srcIdle.name : "YOK — static pose")}\n" +
                $"Move source: {srcMove.name}\n" +
                $"Scene saved: {sceneSaved}\n\n" +
                "Detaylar Console penceresinde.", "Tamam");
        }
    }

    // ── Motion analysis ───────────────────────────────────────────────────────

    static ClipScore AnalyseClip(AnimationClip clip)
    {
        var score = new ClipScore { clip = clip };
        string nameL = clip.name.ToLower();
        score.isLocoByName   = LOCO_KEYWORDS.Any(k   => nameL.Contains(k));
        score.isActionByName = ACTION_KEYWORDS.Any(k  => nameL.Contains(k));

        var bindings = AnimationUtility.GetCurveBindings(clip);
        score.bindingCount = bindings.Length;

        float posVar = 0f;
        float rotVar = 0f;

        foreach (var b in bindings)
        {
            bool isRoot = IsRootPath(b.path);
            bool isPos  = b.propertyName.IndexOf("localPosition", StringComparison.OrdinalIgnoreCase) >= 0
                       || b.propertyName.IndexOf("m_LocalPosition", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isRot  = b.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) >= 0
                       || b.propertyName.IndexOf("m_LocalRotation", StringComparison.OrdinalIgnoreCase) >= 0
                       || b.propertyName.IndexOf("eulerAngles",    StringComparison.OrdinalIgnoreCase) >= 0;

            var curve = AnimationUtility.GetEditorCurve(clip, b);
            if (curve == null || curve.keys.Length == 0) continue;

            float variation = SampleVariation(curve, clip.length);

            if (isPos)
            {
                // Root/hips position variation counts more heavily
                float weight = isRoot ? 3f : 1f;
                posVar += variation * weight;
            }
            if (isRot)
            {
                rotVar += variation;
            }
        }

        score.positionVariation = posVar;
        score.rotationVariation = rotVar;
        return score;
    }

    static bool IsRootPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return true; // root transform
        string pl = path.ToLower();
        return pl.Contains("hips") || pl.Contains("root") || pl.Contains("pelvis")
            || pl.Contains("armature") || pl == "";
    }

    static float SampleVariation(AnimationCurve curve, float duration)
    {
        if (duration <= 0f) return 0f;
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i <= SAMPLE_COUNT; i++)
        {
            float t = (i / (float)SAMPLE_COUNT) * duration;
            float v = curve.Evaluate(t);
            if (v < min) min = v;
            if (v > max) max = v;
        }
        return max - min;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string FindAnimFbx(StringBuilder log)
    {
        const string known =
            "Assets/Art/Characters/Player/FemaleSamurai/Meshy_AI_Moonbound_Samurai_biped/" +
            "Meshy_AI_Moonbound_Samurai_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
        if (AssetDatabase.AssetPathToGUID(known) != "")
        { log.AppendLine("[FBX]    " + known); return known; }

        string[] guids = AssetDatabase.FindAssets("Merged_Animations t:Model",
            new[] { "Assets/Art/Characters/Player/FemaleSamurai" });
        if (guids.Length > 0)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[0]);
            log.AppendLine("[FBX]    " + p); return p;
        }
        log.AppendLine("[FBX]    HATA: FBX bulunamadı"); return null;
    }

    static void WriteAnim(AnimationClip source, string destPath, bool looped, StringBuilder log)
    {
        var newClip = new AnimationClip();
        EditorUtility.CopySerialized(source, newClip);
        newClip.name     = System.IO.Path.GetFileNameWithoutExtension(destPath);
        newClip.wrapMode = looped ? WrapMode.Loop : WrapMode.Once;

        var s = AnimationUtility.GetAnimationClipSettings(newClip);
        s.loopTime = s.loopBlend = s.loopBlendOrientation =
        s.loopBlendPositionY = s.loopBlendPositionXZ = looped;
        AnimationUtility.SetAnimationClipSettings(newClip, s);

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(newClip, existing);
            AnimationUtility.SetAnimationClipSettings(existing, s);
            EditorUtility.SetDirty(existing);
            log.AppendLine($"[Write]  Güncellendi → {destPath}  (loop={looped})");
        }
        else
        {
            AssetDatabase.CreateAsset(newClip, destPath);
            log.AppendLine($"[Write]  Oluşturuldu → {destPath}  (loop={looped})");
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

    static void Abort(StringBuilder log, bool batch, string msg)
    {
        log.AppendLine("[ABORT]  " + msg);
        Debug.LogError(log.ToString());
        if (!batch) EditorUtility.DisplayDialog("Fix Hatası", msg, "Tamam");
    }
}
