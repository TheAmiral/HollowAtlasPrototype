using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StudioSplashController : MonoBehaviour
{
    [Header("Splash")]
    [SerializeField] private Sprite splashSprite;
    [SerializeField] private Vector2 logoSize = new Vector2(700f, 880f);
    [SerializeField] private string nextScene = "MainMenu";

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.0f;

    void Start()
    {
        CanvasGroup cg = BuildUI();
        cg.alpha = 0f;
        StartCoroutine(SplashRoutine(cg));
    }

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
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(holdDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(1f - t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(nextScene);
    }
}
