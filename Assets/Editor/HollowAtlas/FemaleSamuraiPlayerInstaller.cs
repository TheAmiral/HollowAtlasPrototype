using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FemaleSamuraiPlayerInstaller
{
    private const string SCENE_PATH      = "Assets/Scenes/Prototype_01.unity";
    private const string FBX_PATH        =
        "Assets/Art/Characters/Player/FemaleSamurai/" +
        "Meshy_AI_Moonbound_Samurai_biped/" +
        "Meshy_AI_Moonbound_Samurai_biped_Character_output.fbx";
    private const string CONTROLLER_PATH =
        "Assets/Art/Characters/Player/FemaleSamurai/Player_FemaleSamurai_Animator.controller";
    private const string PREFAB_PATH     = "Assets/Prefabs/Player/Player_FemaleSamurai.prefab";
    private const string VISUAL_NAME     = "VisualRoot_FemaleSamurai";
    private const string OLD_VISUAL_NAME = "FemaleSamurai_Visual";  // cleanup legacy name

    [MenuItem("Tools/Hollow Atlas/Install Female Samurai Player Visual")]
    public static void Install()
    {
        bool batch = Application.isBatchMode;
        var  log   = new StringBuilder();
        log.AppendLine("╔══════════════════════════════════════════════════════════╗");
        log.AppendLine("║    FemaleSamuraiPlayerInstaller — Kurulum Raporu         ║");
        log.AppendLine("╚══════════════════════════════════════════════════════════╝");

        // ── 1. Open scene ────────────────────────────────────────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (active.path != SCENE_PATH)
        {
            if (!batch)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
            log.AppendLine($"[Scene]      Açıldı         → {SCENE_PATH}");
        }
        else
        {
            log.AppendLine($"[Scene]      Zaten açık     → {SCENE_PATH}");
        }

        // ── 2. FBX importer: importAnimation = false ─────────────────────────────
        log.AppendLine($"[FBX]        Path           → {FBX_PATH}");
        var importer = AssetImporter.GetAtPath(FBX_PATH) as ModelImporter;
        if (importer != null && importer.importAnimation)
        {
            importer.importAnimation = false;
            importer.SaveAndReimport();
            log.AppendLine("[FBX]        importAnimation → false (reimport yapıldı)");
        }
        else
        {
            log.AppendLine(importer != null
                ? "[FBX]        importAnimation zaten false"
                : "[FBX]        UYARI: ModelImporter bulunamadı");
        }

        // ── 3. Load FBX asset ────────────────────────────────────────────────────
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
        if (fbxAsset == null)
        {
            Abort(log, batch, $"FBX yüklenemedi:\n{FBX_PATH}");
            return;
        }

        // ── 4. Find Player root ──────────────────────────────────────────────────
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm == null)
        {
            Abort(log, batch, "PlayerMovement sahnede bulunamadı.");
            return;
        }
        GameObject playerRoot = pm.gameObject;
        log.AppendLine($"[Player]     Root           → '{playerRoot.name}'");

        // ── 5. Remove legacy / previous visual children ──────────────────────────
        foreach (string n in new[] { OLD_VISUAL_NAME, VISUAL_NAME })
        {
            Transform t = playerRoot.transform.Find(n);
            if (t != null) { Object.DestroyImmediate(t.gameObject); log.AppendLine($"[Cleanup]    '{n}' silindi"); }
        }

        // ── 6. Instantiate FBX as visual child ───────────────────────────────────
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, playerRoot.transform);
        visual.name                    = VISUAL_NAME;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale    = Vector3.one;
        log.AppendLine($"[Visual]     Oluşturuldu    → '{VISUAL_NAME}' (Player altında)");

        // ── 7. applyRootMotion = false ───────────────────────────────────────────
        var anim = visual.GetComponent<Animator>()
                ?? visual.GetComponentInChildren<Animator>(includeInactive: true);
        if (anim != null)
        {
            anim.applyRootMotion = false;
            log.AppendLine("[Animator]   applyRootMotion → false");
        }
        else
        {
            log.AppendLine("[Animator]   UYARI: FBX kökünde Animator bulunamadı");
        }

        // ── 8. Auto-scale to fit CharacterController height ───────────────────────
        // Measures natural mesh height at scale=1, then scales VisualRoot only.
        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float meshH   = b.size.y;
                float targetH = cc.height * 0.95f;
                if (meshH > 0.01f && Mathf.Abs(meshH - targetH) / targetH > 0.05f)
                {
                    float s = targetH / meshH;
                    visual.transform.localScale = Vector3.one * s;
                    log.AppendLine($"[Scale]      Auto-fit: mesh={meshH:F3}m CC.h={cc.height:F3}m → scale={s:F4}");
                }
                else
                {
                    log.AppendLine($"[Scale]      mesh≈CC.height ({meshH:F3}m), scale=1 (ok)");
                }
            }
            else
            {
                log.AppendLine("[Scale]      Renderer yok, scale=1 bırakıldı");
            }
        }
        else
        {
            log.AppendLine("[Scale]      CharacterController yok, scale=1 bırakıldı");
        }

        // ── 9. AnimatorController ────────────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        bool controllerIsNew = controller == null;
        if (controllerIsNew)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            log.AppendLine($"[Controller] Yeni oluşturuldu → {CONTROLLER_PATH}");
        }
        else
        {
            log.AppendLine($"[Controller] Mevcut kullanılıyor → {CONTROLLER_PATH}");
        }

        EnsureParam(controller, "Hor",    AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Vert",   AnimatorControllerParameterType.Float);
        EnsureParam(controller, "State",  AnimatorControllerParameterType.Float);
        EnsureParam(controller, "IsJump", AnimatorControllerParameterType.Bool);
        log.AppendLine("[Controller] Parametreler OK  → Hor, Vert, State, IsJump");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        bool hasIdle = false;
        foreach (var cs in sm.states)
            if (cs.state.name == "Idle_Placeholder") { sm.defaultState = cs.state; hasIdle = true; break; }
        if (!hasIdle)
        {
            AnimatorState idle = sm.AddState("Idle_Placeholder");
            sm.defaultState = idle;
            log.AppendLine("[Controller] Idle_Placeholder state eklendi");
        }
        else
        {
            log.AppendLine("[Controller] Idle_Placeholder zaten var");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        if (anim != null)
        {
            anim.runtimeAnimatorController = controller;
            log.AppendLine("[Controller] Animator'a atandı");
        }

        // ── 10. CharacterAnimatorBridge ──────────────────────────────────────────
        // Must be on the same GO as Animator (uses GetComponent<Animator>).
        GameObject bridgeHost = anim != null ? anim.gameObject : visual;
        var rootBridge = playerRoot.GetComponent<CharacterAnimatorBridge>();
        if (rootBridge != null)
        {
            var nb = bridgeHost.AddComponent<CharacterAnimatorBridge>();
            nb.horParam    = rootBridge.horParam;
            nb.vertParam   = rootBridge.vertParam;
            nb.stateParam  = rootBridge.stateParam;
            nb.isJumpParam = rootBridge.isJumpParam;
            nb.dampTime    = rootBridge.dampTime;
            Object.DestroyImmediate(rootBridge);
            log.AppendLine($"[Bridge]     Player root'tan taşındı → '{bridgeHost.name}'");
        }
        else if (bridgeHost.GetComponent<CharacterAnimatorBridge>() == null)
        {
            bridgeHost.AddComponent<CharacterAnimatorBridge>();
            log.AppendLine($"[Bridge]     Yeni eklendi → '{bridgeHost.name}'");
        }
        else
        {
            log.AppendLine($"[Bridge]     Zaten var → '{bridgeHost.name}'");
        }

        // ── 11. Prefab (standalone copy — scene bağlantısını kesmez) ─────────────
        bool prefabCreated = false;
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Player");
            // Instantiate a detached copy so scene Player is never connected to the new prefab
            GameObject copy = Object.Instantiate(playerRoot);
            copy.name = playerRoot.name;
            PrefabUtility.SaveAsPrefabAsset(copy, PREFAB_PATH, out prefabCreated);
            Object.DestroyImmediate(copy);
            log.AppendLine(prefabCreated
                ? $"[Prefab]     Oluşturuldu    → {PREFAB_PATH}"
                : $"[Prefab]     UYARI: Oluşturulamadı ({PREFAB_PATH})");
        }
        catch (System.Exception ex)
        {
            log.AppendLine($"[Prefab]     HATA: {ex.Message}");
        }

        // ── 12. Save scene ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        bool saved = EditorSceneManager.SaveScene(active);
        log.AppendLine(saved ? "[Scene]      Kaydedildi" : "[Scene]      UYARI: Kaydedilemedi");

        // ── 13. Final report ─────────────────────────────────────────────────────
        log.AppendLine("════════════════════════════════════════════════════════════");
        log.AppendLine($"  FBX path     : {FBX_PATH}");
        log.AppendLine($"  Player root  : {playerRoot.name}");
        log.AppendLine($"  Visual child : {VISUAL_NAME}");
        log.AppendLine($"  Controller   : {(controllerIsNew ? "YENİ oluşturuldu" : "mevcut güncellendi")}");
        log.AppendLine($"  Bridge host  : {bridgeHost.name}");
        log.AppendLine($"  Scene saved  : {saved}");
        log.AppendLine($"  Prefab       : {(prefabCreated ? "OK" : "başarısız")}");
        log.AppendLine("════════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());

        if (!batch)
        {
            Selection.activeGameObject = visual;
            EditorUtility.DisplayDialog("Kurulum Tamamlandı",
                $"Player root  : {playerRoot.name}\n" +
                $"Visual child : {VISUAL_NAME}\n" +
                $"Controller   : {CONTROLLER_PATH}\n" +
                $"Prefab       : {(prefabCreated ? PREFAB_PATH : "oluşturulamadı")}\n" +
                $"Scene saved  : {saved}\n\n" +
                "Detaylar için Console penceresine bakın.", "Tamam");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static void Abort(StringBuilder log, bool batch, string msg)
    {
        log.AppendLine($"[ABORT]      {msg}");
        Debug.LogError(log.ToString());
        if (!batch) EditorUtility.DisplayDialog("Installer Hatası", msg, "Tamam");
    }

    static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int sep    = path.LastIndexOf('/');
        string par = path.Substring(0, sep);
        string sub = path.Substring(sep + 1);
        AssetDatabase.CreateFolder(par, sub);
    }
}
