using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float ElapsedTime { get; private set; }
    public bool IsGameOver { get; private set; }

    private Texture2D whiteTexture;
    private GUIStyle gameOverTitleStyle;
    private GUIStyle gameOverShadowStyle;
    private GUIStyle gameOverSubStyle;

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

        if (FindObjectOfType<MainHudCanvasUI>() == null)
        {
            GameObject hudObject = new GameObject("MainHudCanvas");
            hudObject.AddComponent<MainHudCanvasUI>();
        }
    }

    void Update()
    {
        if (!IsGameOver && Time.timeScale > 0f)
            ElapsedTime += Time.deltaTime;
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
        Time.timeScale = 1f;
    }

    void OnGUI()
    {
        EnsureStyles();

        if (!IsGameOver)
            return;

        Color oldColor = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);

        float panelWidth = 440f;
        float panelHeight = 150f;
        float x = Screen.width * 0.5f - panelWidth * 0.5f;
        float y = Screen.height * 0.5f - panelHeight * 0.5f;

        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(x + 3f, y + 22f, panelWidth, 32f), "GAME OVER", gameOverShadowStyle);
        GUI.Label(new Rect(x, y + 20f, panelWidth, 32f), "GAME OVER", gameOverTitleStyle);
        GUI.Label(new Rect(x, y + 68f, panelWidth, 24f), $"Süre: {ElapsedTime:0.0}s", gameOverSubStyle);
        GUI.Label(new Rect(x, y + 96f, panelWidth, 22f), "Tekrar oynamak için Play'i yeniden başlat", gameOverSubStyle);

        GUI.color = oldColor;
    }
}
