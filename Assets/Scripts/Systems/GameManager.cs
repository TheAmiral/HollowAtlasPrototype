using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
    private MainHudCanvasUI cachedHud;
    private GameOverUIController cachedGameOverUI;

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
            TogglePause();
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
        Time.timeScale = 1f;
    }

    void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public void RestartGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGUI()
    {
        EnsureStyles();

        if (IsPaused && !IsGameOver)
        {
            Color pauseOldColor = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);

            float pw = 440f;
            float ph = 120f;
            float px = Screen.width * 0.5f - pw * 0.5f;
            float py = Screen.height * 0.5f - ph * 0.5f;

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(px, py, pw, ph), whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(px + 3f, py + 22f, pw, 32f), "DURAKLATILDI", gameOverShadowStyle);
            GUI.Label(new Rect(px, py + 20f, pw, 32f), "DURAKLATILDI", gameOverTitleStyle);
            if (GUI.Button(new Rect(px + pw * 0.5f - 120f, py + 67f, 240f, 34f), "Devam Et", pauseButtonStyle))
                TogglePause();

            GUI.color = pauseOldColor;
            return;
        }

        if (IsGameOver)
            return;
    }
}
