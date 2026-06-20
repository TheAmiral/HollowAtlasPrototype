using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class StudioSplashController : MonoBehaviour
{
    [Header("Splash (statik logo)")]
    [SerializeField] private Sprite splashSprite;
    [SerializeField] private Vector2 logoSize = new Vector2(700f, 880f);
    [SerializeField] private string nextScene = "MainMenu";

    [Header("Video (opsiyonel — atanırsa logo yerine oynatılır)")]
    [SerializeField] private VideoClip splashVideo;
    // Inspector'da boş bırakılırsa Resources'tan yüklenir: Assets/Resources/Video/Asina Splash.mp4
    [SerializeField] private string resourceVideoPath = "Video/Asina Splash";
    [SerializeField] private bool allowSkip = true;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.0f;

    private bool _loading;

    void Start()
    {
        VideoClip clip = splashVideo != null
            ? splashVideo
            : Resources.Load<VideoClip>(resourceVideoPath);

        if (clip != null)
        {
            StartCoroutine(VideoRoutine(clip));
        }
        else
        {
            CanvasGroup cg = BuildUI();
            cg.alpha = 0f;
            StartCoroutine(SplashRoutine(cg));
        }
    }

    // ── Statik logo yolu (mevcut davranış) ────────────────────────────────────

    CanvasGroup BuildUI()
    {
        GameObject canvasGO = new GameObject("StudioSplashCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = canvasGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        RectTransform root = canvasGO.GetComponent<RectTransform>();

        // Full-screen solid black background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = Color.black;

        // Centered logo
        GameObject logoGO = new GameObject("Logo");
        logoGO.transform.SetParent(root, false);
        RectTransform logoRt = logoGO.AddComponent<RectTransform>();
        logoRt.anchoredPosition = Vector2.zero;
        logoRt.sizeDelta = logoSize;
        Image logoImg = logoGO.AddComponent<Image>();
        logoImg.preserveAspect = true;

        if (splashSprite != null)
            logoImg.sprite = splashSprite;
        else
            Debug.LogWarning("[StudioSplashController] splashSprite atanmamış.");

        return cg;
    }

    IEnumerator SplashRoutine(CanvasGroup cg)
    {
        yield return Fade(cg, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(cg, 1f, 0f, fadeDuration);
        LoadNext();
    }

    // ── Video splash yolu ─────────────────────────────────────────────────────

    IEnumerator VideoRoutine(VideoClip clip)
    {
        var (cg, rawImage, fitter) = BuildVideoUI();
        cg.alpha = 0f;

        var vp = gameObject.AddComponent<VideoPlayer>();
        vp.source           = VideoSource.VideoClip;
        vp.clip             = clip;
        vp.playOnAwake      = false;
        vp.isLooping        = false;
        vp.renderMode       = VideoRenderMode.RenderTexture;
        vp.audioOutputMode  = VideoAudioOutputMode.Direct;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop       = true;

        vp.Prepare();
        float prepTimeout = 5f;
        while (!vp.isPrepared && prepTimeout > 0f)
        {
            prepTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        int w = (int)vp.width;
        int h = (int)vp.height;
        if (w <= 0) w = 1920;
        if (h <= 0) h = 1080;

        var rt = new RenderTexture(w, h, 0);
        rt.Create();
        vp.targetTexture = rt;
        rawImage.texture = rt;
        if (fitter != null) fitter.aspectRatio = (float)w / h;

        vp.Play();

        yield return Fade(cg, 0f, 1f, fadeDuration);

        // Videonun TAM uzunluğu kadar göster (ör. ~6 sn). Süre okunamazsa 6 sn varsayılır.
        double targetDuration = clip.length > 0.1 ? clip.length : 6.0;
        double elapsed = 0.0;
        while (elapsed < targetDuration)
        {
            if (allowSkip && SkipPressed()) break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Fade(cg, 1f, 0f, fadeDuration);

        if (vp != null) vp.Stop();
        if (rt != null) { rt.Release(); Destroy(rt); }

        LoadNext();
    }

    (CanvasGroup cg, RawImage raw, AspectRatioFitter fitter) BuildVideoUI()
    {
        GameObject canvasGO = new GameObject("StudioSplashCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = canvasGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        RectTransform root = canvasGO.GetComponent<RectTransform>();

        // Letterbox için siyah arka plan
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = Color.black;

        // Video yüzeyi (RenderTexture buraya çizilir)
        GameObject surf = new GameObject("VideoSurface");
        surf.transform.SetParent(root, false);
        RectTransform surfRt = surf.AddComponent<RectTransform>();
        surfRt.anchorMin = new Vector2(0.5f, 0.5f);
        surfRt.anchorMax = new Vector2(0.5f, 0.5f);
        surfRt.pivot     = new Vector2(0.5f, 0.5f);
        surfRt.anchoredPosition = Vector2.zero;
        surfRt.sizeDelta = new Vector2(1920f, 1080f);
        RawImage raw = surf.AddComponent<RawImage>();
        raw.raycastTarget = false;
        AspectRatioFitter fitter = surf.AddComponent<AspectRatioFitter>();
        fitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        return (cg, raw, fitter);
    }

    bool SkipPressed()
    {
        // Yalnızca Esc ile atla — Game view'a tıklayınca splash yanlışlıkla kesilmesin.
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    // ── Ortak yardımcılar ─────────────────────────────────────────────────────

    IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        cg.alpha = to;
    }

    void LoadNext()
    {
        if (_loading) return;
        _loading = true;
        SceneManager.LoadScene(nextScene);
    }
}
