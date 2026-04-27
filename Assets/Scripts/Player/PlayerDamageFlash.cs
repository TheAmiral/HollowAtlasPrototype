using UnityEngine;

public class PlayerDamageFlash : MonoBehaviour
{
    public Color flashColor = new Color(1f, 0f, 0f, 0.35f);
    public float flashDuration = 0.2f;
    public float fadeSpeed = 4f;

    private PlayerHealth playerHealth;
    private int previousHealth;
    private float flashTimer;
    private Texture2D flashTexture;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
            previousHealth = playerHealth.CurrentHealth;

        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    void Update()
    {
        if (playerHealth == null)
            return;

        if (playerHealth.CurrentHealth < previousHealth)
        {
            flashTimer = flashDuration;
        }

        previousHealth = playerHealth.CurrentHealth;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.unscaledDeltaTime * fadeSpeed;
            if (flashTimer < 0f)
                flashTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (flashTimer <= 0f)
            return;

        Color oldColor = GUI.color;

        float alpha01 = Mathf.Clamp01(flashTimer / flashDuration);
        Color drawColor = flashColor;
        drawColor.a *= alpha01;

        GUI.color = drawColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);

        GUI.color = oldColor;
    }

    void OnDestroy()
    {
        if (flashTexture != null)
            Destroy(flashTexture);
    }
}