using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private const string MainMenuSceneName = "MainMenu";
    private const string BrightnessKey = "HollowAtlas.Brightness";
    private const string FullscreenKey = "HollowAtlas.Fullscreen";
    private const string ShowRangeKey = "HollowAtlas.ShowRangeIndicator";
    private const float BrightnessMin = 0.80f;
    private const float BrightnessMax = 1.30f;

    public float ElapsedTime { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    private Texture2D whiteTexture;
    private Texture2D pauseBtnNormalTex;
    private Texture2D pauseBtnHoverTex;
    private GUIStyle gameOverTitleStyle;
    private GUIStyle gameOverShadowStyle;
    private GUIStyle gameOverSubStyle;
    private GUIStyle pauseButtonStyle;
    private GUIStyle pauseLabelStyle;
    private GUIStyle pauseToggleStyle;
    private MainHudCanvasUI cachedHud;
    private GameOverUIController cachedGameOverUI;
    private bool pauseSettingsOpen;
    private float pauseBrightness = 1f;
    private bool pauseFullscreen;
    private bool pauseShowRangeIndicator = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        EnsureAudioManager();
        EnsureMainHudCanvas();
        EnsureGameOverUI();
    }

    void EnsureAudioManager()
    {
        if (AudioManager.Instance != null)
            return;

        new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    void Update()
    {
        if (!IsGameOver && Time.timeScale > 0f)
            ElapsedTime += Time.deltaTime;

        if (!IsGameOver)
            EnsureMainHudCanvas();

        EnsureGameOverUI();

        if (IsGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartGame();

        if (!IsGameOver
            && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame
            && (LevelUpCardSystem.Instance == null || !LevelUpCardSystem.Instance.SelectionPending)
            && (BossRewardSystem.Instance == null || !BossRewardSystem.Instance.RewardPending))
        {
            if (IsPaused && pauseSettingsOpen)
                pauseSettingsOpen = false;
            else
                TogglePause();
        }
    }

    void EnsureMainHudCanvas()
    {
        if (cachedHud != null && cachedHud.gameObject.activeSelf && cachedHud.enabled)
            return;

        MainHudCanvasUI hud = FindExistingHud();

        if (hud == null)
        {
            GameObject hudObject = new GameObject("MainHudCanvas");
            hud = hudObject.AddComponent<MainHudCanvasUI>();
        }

        if (!hud.gameObject.activeSelf)
            hud.gameObject.SetActive(true);

        if (!hud.enabled)
            hud.enabled = true;

        cachedHud = hud;
    }

    MainHudCanvasUI FindExistingHud()
    {
        MainHudCanvasUI[] allHudObjects = Resources.FindObjectsOfTypeAll<MainHudCanvasUI>();

        for (int i = 0; i < allHudObjects.Length; i++)
        {
            MainHudCanvasUI hud = allHudObjects[i];
            if (hud == null)
                continue;

            if (!hud.gameObject.scene.IsValid())
                continue;

            return hud;
        }

        return null;
    }

    void EnsureGameOverUI()
    {
        if (cachedGameOverUI != null && cachedGameOverUI.gameObject.activeSelf && cachedGameOverUI.enabled)
            return;

        cachedGameOverUI = FindFirstObjectByType<GameOverUIController>();

        if (cachedGameOverUI != null)
            return;

        GameObject uiObject = new GameObject("GameOverCanvas");
        cachedGameOverUI = uiObject.AddComponent<GameOverUIController>();
    }

    void EnsureStyles()
    {
        if (gameOverTitleStyle != null)
            return;

        gameOverTitleStyle = new GUIStyle(GUI.skin.label);
        gameOverTitleStyle.alignment = TextAnchor.MiddleCenter;
        gameOverTitleStyle.fontSize = 30;
        gameOverTitleStyle.fontStyle = FontStyle.Bold;
        gameOverTitleStyle.normal.textColor = Color.white;

        gameOverShadowStyle = new GUIStyle(gameOverTitleStyle);
        gameOverShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.92f);

        gameOverSubStyle = new GUIStyle(GUI.skin.label);
        gameOverSubStyle.alignment = TextAnchor.MiddleCenter;
        gameOverSubStyle.fontSize = 18;
        gameOverSubStyle.fontStyle = FontStyle.Bold;
        gameOverSubStyle.normal.textColor = Color.white;

        pauseBtnNormalTex = new Texture2D(1, 1);
        pauseBtnNormalTex.SetPixel(0, 0, new Color(0.28f, 0.12f, 0.55f, 0.92f));
        pauseBtnNormalTex.Apply();

        pauseBtnHoverTex = new Texture2D(1, 1);
        pauseBtnHoverTex.SetPixel(0, 0, new Color(0.45f, 0.22f, 0.75f, 0.95f));
        pauseBtnHoverTex.Apply();

        pauseButtonStyle = new GUIStyle();
        pauseButtonStyle.alignment = TextAnchor.MiddleCenter;
        pauseButtonStyle.fontSize = 18;
        pauseButtonStyle.fontStyle = FontStyle.Bold;
        pauseButtonStyle.normal.textColor = Color.white;
        pauseButtonStyle.hover.textColor = new Color(1f, 0.88f, 1f, 1f);
        pauseButtonStyle.active.textColor = new Color(0.75f, 0.55f, 1f, 1f);
        pauseButtonStyle.normal.background = pauseBtnNormalTex;
        pauseButtonStyle.hover.background = pauseBtnHoverTex;
        pauseButtonStyle.active.background = pauseBtnNormalTex;

        pauseLabelStyle = new GUIStyle(GUI.skin.label);
        pauseLabelStyle.fontSize = 16;
        pauseLabelStyle.fontStyle = FontStyle.Bold;
        pauseLabelStyle.normal.textColor = Color.white;

        pauseToggleStyle = new GUIStyle(GUI.skin.toggle);
        pauseToggleStyle.fontSize = 16;
        pauseToggleStyle.fontStyle = FontStyle.Bold;
        pauseToggleStyle.normal.textColor = Color.white;
        pauseToggleStyle.hover.textColor = new Color(1f, 0.88f, 1f, 1f);
        pauseToggleStyle.active.textColor = new Color(0.75f, 0.55f, 1f, 1f);
    }

    public void SetGameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("Game Over");
    }

    public void ResetRun()
    {
        ElapsedTime = 0f;
        IsGameOver = false;
        IsPaused = false;
        pauseSettingsOpen = false;
        Time.timeScale = 1f;
    }

    void TogglePause()
    {
        IsPaused = !IsPaused;

        if (!IsPaused)
            pauseSettingsOpen = false;

        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public void RestartGame()
    {
        IsPaused = false;
        pauseSettingsOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        IsPaused = false;
        IsGameOver = false;
        pauseSettingsOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    void OnGUI()
    {
        EnsureStyles();

        if (IsPaused && !IsGameOver)
        {
            if (pauseSettingsOpen)
                DrawPauseSettingsPanel();
            else
                DrawPauseMainPanel();

            return;
        }

        if (IsGameOver)
            return;
    }

    void DrawPauseMainPanel()
    {
        Color oldColor = GUI.color;
        DrawPauseBackdrop();

        float pw = 440f;
        float ph = 235f;
        float px = Screen.width * 0.5f - pw * 0.5f;
        float py = Screen.height * 0.5f - ph * 0.5f;

        DrawPausePanel(px, py, pw, ph, "DURAKLATILDI");

        if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 78f, 240f, 34f), "DEVAM ET", pauseButtonStyle))
            TogglePause();

        if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 122f, 240f, 34f), "AYARLAR", pauseButtonStyle))
            OpenPauseSettings();

        if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 166f, 240f, 34f), "ANA MEN\u00DCYE D\u00D6N", pauseButtonStyle))
            ReturnToMainMenu();

        GUI.color = oldColor;
    }

    void DrawPauseSettingsPanel()
    {
        Color oldColor = GUI.color;
        DrawPauseBackdrop();

        float pw = 520f;
        float ph = 340f;
        float px = Screen.width * 0.5f - pw * 0.5f;
        float py = Screen.height * 0.5f - ph * 0.5f;

        DrawPausePanel(px, py, pw, ph, "AYARLAR");

        GUI.color = Color.white;
        GUI.Label(new Rect(px + 70f, py + 82f, 150f, 24f), "Parlakl\u0131k", pauseLabelStyle);
        pauseBrightness = GUI.HorizontalSlider(new Rect(px + 210f, py + 88f, 180f, 20f), pauseBrightness, BrightnessMin, BrightnessMax);
        GUI.Label(new Rect(px + 405f, py + 82f, 60f, 24f), Mathf.RoundToInt((pauseBrightness - BrightnessMin) / (BrightnessMax - BrightnessMin) * 100f).ToString(), pauseLabelStyle);

        pauseFullscreen = GUI.Toggle(new Rect(px + 70f, py + 130f, 280f, 28f), pauseFullscreen, "Tam Ekran", pauseToggleStyle);
        pauseShowRangeIndicator = GUI.Toggle(new Rect(px + 70f, py + 172f, 330f, 28f), pauseShowRangeIndicator, "Menzil Halkas\u0131n\u0131 G\u00F6ster", pauseToggleStyle);

        if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 226f, 240f, 34f), "UYGULA", pauseButtonStyle))
            SavePauseSettings();

        if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 270f, 240f, 34f), "GER\u0130", pauseButtonStyle))
            pauseSettingsOpen = false;

        GUI.color = oldColor;
    }

    void DrawPauseBackdrop()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.50f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
    }

    void DrawPausePanel(float px, float py, float pw, float ph, string title)
    {
        GUI.color = new Color(0.020f, 0.014f, 0.045f, 0.92f);
        GUI.DrawTexture(new Rect(px, py, pw, ph), whiteTexture);

        GUI.color = new Color(1f, 0.78f, 0.36f, 0.72f);
        GUI.DrawTexture(new Rect(px + 34f, py + 60f, pw - 68f, 2f), whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(px + 3f, py + 22f, pw, 32f), title, gameOverShadowStyle);
        GUI.Label(new Rect(px, py + 20f, pw, 32f), title, gameOverTitleStyle);
    }

    void OpenPauseSettings()
    {
        pauseBrightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), BrightnessMin, BrightnessMax);
        pauseFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        pauseShowRangeIndicator = PlayerPrefs.GetInt(ShowRangeKey, 1) == 1;
        pauseSettingsOpen = true;
    }

    void SavePauseSettings()
    {
        PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp(pauseBrightness, BrightnessMin, BrightnessMax));
        PlayerPrefs.SetInt(FullscreenKey, pauseFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ShowRangeKey, pauseShowRangeIndicator ? 1 : 0);
        PlayerPrefs.Save();

        Screen.fullScreen = pauseFullscreen;
        RefreshRangeIndicators();
    }

    void RefreshRangeIndicators()
    {
        PlayerRangeIndicator[] indicators = FindObjectsByType<PlayerRangeIndicator>(FindObjectsSortMode.None);

        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] != null)
                indicators[i].RefreshVisibilityPreference();
        }
    }
}
