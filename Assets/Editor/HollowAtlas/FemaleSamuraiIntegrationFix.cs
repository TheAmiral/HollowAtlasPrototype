using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fixes the post-install state of the Female Samurai visual:
///   1. Finds the Animator anywhere inside VisualRoot_FemaleSamurai.
///   2. Ensures Animator is on the correct node; adds one if missing entirely.
///   3. Moves CharacterAnimatorBridge to the same GameObject as the Animator.
///   4. Assigns Player_FemaleSamurai_Animator.controller and disables root-motion.
///   5. Disables all Renderer components on Body_010 (old placeholder).
///   6. Saves the scene.
/// </summary>
public static class FemaleSamuraiIntegrationFix
{
    private const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    private const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";
    private const string BODY_OLD_NAME   = "Body_010";
    private const string CONTROLLER_PATH =
        "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";

    [MenuItem("Tools/Hollow Atlas/Fix Female Samurai Integration")]
    public static void Fix()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiIntegrationFix — Rapor                   ║");
        log.AppendLine("╚══════════════════════════════════════════════════════════╝");

        // ── 1. Open scene if needed ──────────────────────────────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (active.path != SCENE_PATH)
        {
            if (!batch) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
            log.AppendLine($"[Scene]   Açıldı → {SCENE_PATH}");
        }
        else
        {
            log.AppendLine($"[Scene]   Zaten açık → {SCENE_PATH}");
        }

        // ── 2. Find Player root ──────────────────────────────────────────────────
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm == null) { Abort(log, batch, "PlayerMovement sahnede bulunamadı."); return; }
        GameObject playerRoot = pm.gameObject;
        log.AppendLine($"[Player]  Root → '{playerRoot.name}'");

        // ── 3. Load AnimatorController asset ────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            Abort(log, batch,
                $"AnimatorController bulunamadı:\n{CONTROLLER_PATH}\n" +
                "Önce 'Install Female Samurai Player Visual' komutunu çalıştırın.");
            return;
        }
        log.AppendLine($"[Ctrl]    Controller yüklendi → {CONTROLLER_PATH}");

        // ── 4. Fix VisualRoot_FemaleSamurai ──────────────────────────────────────
        Transform visualTransform = playerRoot.transform.Find(VISUAL_NAME);
        if (visualTransform == null)
        {
            Abort(log, batch,
                $"'{VISUAL_NAME}' Player altında bulunamadı.\n" +
                "Önce 'Install Female Samurai Player Visual' komutunu çalıştırın.");
            return;
        }
        GameObject visualRoot = visualTransform.gameObject;
        log.AppendLine($"[Visual]  Bulundu → '{VISUAL_NAME}'");

        // ── 4a. Find Animator anywhere in visual hierarchy ───────────────────────
        // Check root first, then children.
        Animator anim = visualRoot.GetComponent<Animator>();
        string animSource = "root (mevcut)";

        if (anim == null)
        {
            anim = visualRoot.GetComponentInChildren<Animator>(includeInactive: true);
            if (anim != null)
                animSource = $"child '{anim.gameObject.name}' (mevcut)";
        }

        if (anim == null)
        {
            // No Animator anywhere — add one to the visual root itself.
            anim = visualRoot.AddComponent<Animator>();
            animSource = "root (YENİ eklendi)";
            log.AppendLine("[Animator] FBX içinde Animator bulunamadı → VisualRoot'a eklendi");
        }
        else
        {
            log.AppendLine($"[Animator] Bulundu → {animSource}");
        }

        // ── 4b. Configure Animator ───────────────────────────────────────────────
        anim.applyRootMotion = false;

        if (anim.runtimeAnimatorController == null
            || anim.runtimeAnimatorController != controller)
        {
            anim.runtimeAnimatorController = controller;
            log.AppendLine("[Animator] Controller atandı");
        }
        else
        {
            log.AppendLine("[Animator] Controller zaten atanmış, değiştirilmedi");
        }

        log.AppendLine($"[Animator] applyRootMotion = false, host = '{anim.gameObject.name}'");

        // ── 4c. Move CharacterAnimatorBridge to Animator's GameObject ────────────
        GameObject bridgeHost = anim.gameObject;
        string bridgeReport;

        // Remove any existing bridge(s) from wrong locations (visual root or its children).
        var existingBridges = visualRoot.GetComponentsInChildren<CharacterAnimatorBridge>(true);
        CharacterAnimatorBridge correctBridge = null;

        foreach (var b in existingBridges)
        {
            if (b.gameObject == bridgeHost)
            {
                correctBridge = b; // already in the right place
            }
            else
            {
                Object.DestroyImmediate(b);
                log.AppendLine($"[Bridge]  Yanlış konumdan silindi → '{b.gameObject.name}'");
            }
        }

        // Also remove bridge from Player root if it ended up there.
        var rootBridge = playerRoot.GetComponent<CharacterAnimatorBridge>();
        if (rootBridge != null)
        {
            Object.DestroyImmediate(rootBridge);
            log.AppendLine("[Bridge]  Player root'tan silindi");
        }

        if (correctBridge == null)
        {
            correctBridge = bridgeHost.AddComponent<CharacterAnimatorBridge>();
            bridgeReport = $"'{bridgeHost.name}'e yeni eklendi";
        }
        else
        {
            bridgeReport = $"'{bridgeHost.name}' üzerinde zaten doğru konumda";
        }

        log.AppendLine($"[Bridge]  {bridgeReport}");

        // ── 5. Disable Body_010 renderers (keep GameObject, just hide visuals) ───
        bool bodyHidden = false;
        Transform bodyTransform = playerRoot.transform.Find(BODY_OLD_NAME);
        if (bodyTransform != null)
        {
            var renderers = bodyTransform.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var r in renderers)
                r.enabled = false;

            bodyHidden = renderers.Length > 0;
            log.AppendLine(bodyHidden
                ? $"[Body010] {renderers.Length} Renderer disabled → '{BODY_OLD_NAME}' gizlendi"
                : $"[Body010] '{BODY_OLD_NAME}' bulundu ama Renderer yok");
        }
        else
        {
            log.AppendLine($"[Body010] '{BODY_OLD_NAME}' Player altında bulunamadı (sorun değil)");
        }

        // ── 6. Save scene ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool saved = EditorSceneManager.SaveScene(active);
        log.AppendLine(saved ? "[Scene]   Kaydedildi" : "[Scene]   UYARI: Kaydedilemedi");

        // ── 7. Final report ──────────────────────────────────────────────────────
        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  Animator hangi objede    : {anim.gameObject.name} ({animSource})");
        log.AppendLine($"  CharacterAnimatorBridge  : {bridgeReport}");
        log.AppendLine($"  Body_010 gizlendi mi     : {(bodyHidden ? "Evet" : "Hayır / bulunamadı")}");
        log.AppendLine($"  Scene saved              : {saved}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            Selection.activeGameObject = visualRoot;
            EditorUtility.DisplayDialog("Fix Tamamlandı",
                $"Animator objesi   : {anim.gameObject.name}\n" +
                $"Bridge objesi     : {bridgeHost.name}\n" +
                $"Body_010 gizlendi : {(bodyHidden ? "Evet" : "Hayır")}\n" +
                $"Scene saved       : {saved}\n\n" +
                "Detaylar Console penceresinde.",
                "Tamam");
        }
    }

    static void Abort(StringBuilder log, bool batch, string msg)
    {
        log.AppendLine($"[ABORT]   {msg}");
        Debug.LogError(log.ToString());
        if (!batch) EditorUtility.DisplayDialog("Fix Hatası", msg, "Tamam");
    }
}
