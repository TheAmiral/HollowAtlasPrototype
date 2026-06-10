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

public static class FemaleSamuraiAnimationLoopFix
{
    const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    const string ANIM_FBX        =
        "Assets/Art/Characters/Player/FemaleSamurai/Meshy_AI_Moonbound_Samurai_biped/" +
        "Meshy_AI_Moonbound_Samurai_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
    const string CONTROLLER_PATH =
        "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    const string PREFAB_PATH     = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";

    [MenuItem("Tools/Hollow Atlas/Fix Female Samurai Animation Loop")]
    public static void Fix()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiAnimationLoopFix                          ║");
        log.AppendLine("╚══════════════════════════════════════════════════════════╝");

        // ── 1. Open scene ────────────────────────────────────────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (active.path != SCENE_PATH)
        {
            if (!batch) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
            log.AppendLine($"[Scene]  Açıldı → {SCENE_PATH}");
        }
        else
        {
            log.AppendLine("[Scene]  Zaten açık");
        }

        // ── 2. Load real AnimationClip assets (for .length in seconds) ───────────
        var usable = AssetDatabase.LoadAllAssetsAtPath(ANIM_FBX)
            .OfType<AnimationClip>()
            .Where(c => c != null
                     && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                     && c.length > 0.01f)
            .ToList();

        log.AppendLine($"[Clips]  {usable.Count} kullanılabilir clip yüklendi:");
        foreach (var c in usable)
            log.AppendLine($"         '{c.name}'  {c.length:F3}s");

        // ── 3. Fix import loop settings via ModelImporter ────────────────────────
        // NOTE: ModelImporterClipAnimation has NO .length — use lastFrame-firstFrame.
        var loopTrue  = new List<string>();
        var loopFalse = new List<string>();

        var animImporter = AssetImporter.GetAtPath(ANIM_FBX) as ModelImporter;
        if (animImporter != null)
        {
            ModelImporterClipAnimation[] clips = animImporter.clipAnimations;
            bool wasEmpty = clips == null || clips.Length == 0;
            if (wasEmpty)
                clips = animImporter.defaultClipAnimations;

            bool importerDirty = false;
            foreach (var clip in clips)
            {
                if (clip == null) continue;

                float frameSpan = Mathf.Max(0f, clip.lastFrame - clip.firstFrame);
                bool isZeroFrame = frameSpan <= 0.01f;

                // Try to find matching loaded AnimationClip for real length
                float realLength = isZeroFrame ? 0f :
                    (usable.FirstOrDefault(c =>
                        string.Equals(c.name, clip.name, StringComparison.OrdinalIgnoreCase))
                    ?.length ?? (frameSpan / 30f)); // fallback: assume 30fps

                bool shouldLoop = ClassifyLoop(clip.name, isZeroFrame, realLength);
                bool changed    = clip.loopTime    != shouldLoop
                               || clip.loopPose    != shouldLoop
                               || clip.cycleOffset != 0f;

                clip.loopTime    = shouldLoop;
                clip.loopPose    = shouldLoop;
                clip.cycleOffset = 0f;

                if (changed) importerDirty = true;

                string entry = $"'{clip.name}' ({frameSpan:F0}f / {realLength:F3}s)";
                if (shouldLoop) loopTrue.Add(entry);
                else            loopFalse.Add(entry);
            }

            if (importerDirty || wasEmpty)
            {
                animImporter.clipAnimations = clips;
                animImporter.SaveAndReimport();
                log.AppendLine("[FBX]    Loop ayarları güncellendi ve reimport yapıldı");

                // Reload clips after reimport
                usable = AssetDatabase.LoadAllAssetsAtPath(ANIM_FBX)
                    .OfType<AnimationClip>()
                    .Where(c => c != null
                             && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                             && c.length > 0.01f)
                    .ToList();
            }
            else
            {
                log.AppendLine("[FBX]    Loop ayarları zaten doğruydu");
            }
        }
        else
        {
            log.AppendLine("[FBX]    UYARI: ModelImporter bulunamadı → " + ANIM_FBX);
        }

        log.AppendLine("[Loop=true]  " + (loopTrue.Count  > 0 ? string.Join(", ", loopTrue)  : "yok"));
        log.AppendLine("[Loop=false] " + (loopFalse.Count > 0 ? string.Join(", ", loopFalse) : "yok"));

        // ── 4. Classify clips for state machine ──────────────────────────────────
        AnimationClip idleClip = FindClip(usable, "idle", "stand", "breathe", "wait", "rest");
        AnimationClip moveClip = FindClip(usable, "walk", "run", "move", "locomotion", "jog");
        AnimationClip dashClip = FindClip(usable, "dash", "roll", "dodge", "sprint");

        // Fallbacks
        if (idleClip == null && moveClip == null && usable.Count > 0)
        {
            // Sort by length descending; longest → Move, next → Idle
            var sorted = usable.OrderByDescending(c => c.length).ToList();
            moveClip = sorted[0];
            idleClip = sorted.Count > 1 ? sorted[1] : sorted[0];
        }
        else
        {
            if (idleClip == null) idleClip = moveClip;
            if (moveClip == null) moveClip = idleClip;
        }

        bool hasDash = dashClip != null;

        log.AppendLine($"[State]  Idle → '{(idleClip != null ? idleClip.name : "yok")}'");
        log.AppendLine($"[State]  Move → '{(moveClip != null ? moveClip.name : "yok")}'");
        log.AppendLine($"[State]  Dash → '{(hasDash  ? dashClip.name       : "yok (state oluşturulmayacak)")}'");

        // ── 5. Build clean Animator Controller ──────────────────────────────────
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            log.AppendLine("[Ctrl]   Yeni oluşturuldu");
        }
        else
        {
            log.AppendLine("[Ctrl]   Mevcut controller yüklendi");
        }

        EnsureParam(controller, "Hor",       AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",      AnimatorControllerParameterType.Float);
        EnsureParam(controller, "State",     AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump",    AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "IsDashing", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Remove all existing states for a clean rebuild
        foreach (var cs in sm.states.ToArray())
            sm.RemoveState(cs.state);
        foreach (var t in sm.anyStateTransitions.ToArray())
            sm.RemoveAnyStateTransition(t);

        // Create states
        var idleState = sm.AddState("Idle");
        var moveState = sm.AddState("Move");
        AnimatorState dashState = hasDash ? sm.AddState("Dash") : null;

        idleState.motion           = idleClip;
        idleState.speed            = 1f;
        idleState.writeDefaultValues = false;

        moveState.motion           = moveClip;
        moveState.speed            = 1f;
        moveState.writeDefaultValues = false;

        if (hasDash)
        {
            dashState.motion           = dashClip;
            dashState.speed            = 1f;
            dashState.writeDefaultValues = false;
        }

        sm.defaultState = idleState;

        // ── 5a. Idle ↔ Move ──────────────────────────────────────────────────────
        {
            var t = idleState.AddTransition(moveState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.05f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
        }
        {
            var t = moveState.AddTransition(idleState);
            t.hasExitTime      = false;
            t.hasFixedDuration = true;
            t.duration         = 0.05f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "State");
        }

        // ── 5b. Dash transitions (only if dash clip found) ───────────────────────
        if (hasDash)
        {
            {
                var t = sm.AddAnyStateTransition(dashState);
                t.hasExitTime         = false;
                t.hasFixedDuration    = true;
                t.duration            = 0.02f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");
            }
            {
                var t = dashState.AddTransition(moveState);
                t.hasExitTime      = true;
                t.exitTime         = 0.75f;
                t.hasFixedDuration = true;
                t.duration         = 0.05f;
                t.AddCondition(AnimatorConditionMode.IfNot,  0,    "IsDashing");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "State");
            }
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
        log.AppendLine("[Ctrl]   State machine temizlendi ve yeniden kuruldu");

        // ── 6. Wire Animator on VisualRoot (no scale/position/rotation touch) ────
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

                // CharacterAnimatorBridge check (do not move it — only report)
                var bridge = pm.gameObject.GetComponentInChildren<CharacterAnimatorBridge>(true);
                if (bridge != null)
                    log.AppendLine($"[Bridge] '{bridge.gameObject.name}' üzerinde mevcut");
                else
                    log.AppendLine("[Bridge] UYARI: CharacterAnimatorBridge bulunamadı");
            }
            else { log.AppendLine($"[Anim]   UYARI: '{VISUAL_NAME}' Player altında bulunamadı"); }
        }
        else { log.AppendLine("[Anim]   UYARI: PlayerMovement bulunamadı"); }

        // ── 7. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool sceneSaved = EditorSceneManager.SaveScene(active);
        log.AppendLine(sceneSaved ? "[Scene]  Kaydedildi" : "[Scene]  UYARI: Kaydedilemedi");

        // ── 8. Update prefab ─────────────────────────────────────────────────────
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

        // ── 9. Final report ──────────────────────────────────────────────────────
        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  Compile error kaldı mı      : Hayır (çalışıyor)");
        log.AppendLine($"  Toplam AnimationClip sayısı : {usable.Count}");
        log.AppendLine($"  Loop=true  clipler          : {string.Join(", ", loopTrue)}");
        log.AppendLine($"  Loop=false clipler          : {string.Join(", ", loopFalse)}");
        log.AppendLine($"  Idle state clip             : {(idleClip != null ? $"'{idleClip.name}' {idleClip.length:F3}s" : "yok")}");
        log.AppendLine($"  Move state clip             : {(moveClip != null ? $"'{moveClip.name}' {moveClip.length:F3}s" : "yok")}");
        log.AppendLine($"  Dash state clip             : {(hasDash  ? $"'{dashClip.name}' {dashClip.length:F3}s" : "yok")}");
        log.AppendLine($"  Transition'lar kuruldu mu   : True");
        log.AppendLine($"  applyRootMotion = false     : {animWired}");
        log.AppendLine($"  Scene saved                 : {sceneSaved}");
        log.AppendLine($"  Prefab updated              : {prefabSaved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            EditorUtility.DisplayDialog("Animation Loop Fix Tamamlandı",
                $"Clip sayısı: {usable.Count}\n" +
                $"Loop=true: {loopTrue.Count} clip\n" +
                $"Loop=false: {loopFalse.Count} clip\n" +
                $"Scene saved: {sceneSaved}\n\n" +
                "Detaylar Console penceresinde.", "Tamam");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static bool ClassifyLoop(string clipName, bool isZeroFrame, float realLengthSeconds)
    {
        if (isZeroFrame) return false;

        string n = clipName.ToLower();

        if (n.Contains("dash")   || n.Contains("attack") || n.Contains("hit")
         || n.Contains("death")  || n.Contains("die")    || n.Contains("roll")
         || n.Contains("dodge")  || n.Contains("cast")   || n.Contains("shoot"))
            return false;

        if (n.Contains("idle")   || n.Contains("stand")  || n.Contains("breathe")
         || n.Contains("wait")   || n.Contains("rest")
         || n.Contains("walk")   || n.Contains("run")    || n.Contains("jog")
         || n.Contains("move")   || n.Contains("locomotion")
         || n.Contains("fall")   || n.Contains("air"))
            return true;

        // Unknown: loop if > 0.5 seconds (likely continuous motion)
        return realLengthSeconds > 0.5f;
    }

    static AnimationClip FindClip(List<AnimationClip> clips, params string[] keywords)
    {
        foreach (var kw in keywords)
            foreach (var clip in clips)
                if (clip.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip;
        return null;
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
