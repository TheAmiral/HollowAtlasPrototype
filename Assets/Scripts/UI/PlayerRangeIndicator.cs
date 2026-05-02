using UnityEngine;

public class PlayerRangeIndicator : MonoBehaviour
{
    private AutoAttackAura aura;
    private LineRenderer ring;

    private float trackedRadius;
    private float pulseTimer;

    private const int   SEGMENTS      = 64;
    private const float RING_Y        = 0.06f;
    private const float PULSE_DURATION = 0.40f;
    private const float BASE_WIDTH    = 0.045f;
    private const float PULSE_WIDTH   = 0.10f;

    void Start()
    {
        aura = GetComponent<AutoAttackAura>();
        BuildRing();

        if (aura != null)
        {
            trackedRadius = aura.radius;
            UpdatePositions(trackedRadius);
        }
    }

    void Update()
    {
        bool hide =
            aura == null ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver) ||
            (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending) ||
            (BossRewardSystem.Instance  != null && BossRewardSystem.Instance.RewardPending);

        if (ring != null)
            ring.enabled = !hide;

        if (hide || ring == null)
            return;

        // Detect radius change → trigger pulse
        if (Mathf.Abs(aura.radius - trackedRadius) > 0.01f)
        {
            trackedRadius = aura.radius;
            pulseTimer    = PULSE_DURATION;
            UpdatePositions(trackedRadius);
        }

        if (pulseTimer > 0f)
            pulseTimer -= Time.deltaTime;

        float pulseFactor = pulseTimer > 0f ? pulseTimer / PULSE_DURATION : 0f;

        // Slight radius expansion on pulse
        float displayRadius = trackedRadius * (1f + pulseFactor * 0.07f);
        if (pulseFactor > 0f)
            UpdatePositions(displayRadius);

        // Breathing alpha
        float breathAlpha = 0.18f + 0.10f * Mathf.Sin(Time.time * 2f);
        float alpha = Mathf.Clamp01(breathAlpha + pulseFactor * 0.45f);

        // Width pulse
        float width = BASE_WIDTH + pulseFactor * PULSE_WIDTH;
        ring.widthMultiplier = width;

        Color ringColor = Color.Lerp(
            new Color(0.65f, 0.30f, 0.90f, alpha),
            new Color(0.90f, 0.70f, 1.00f, alpha),
            pulseFactor
        );

        ring.startColor = ringColor;
        ring.endColor   = ringColor;
    }

    void BuildRing()
    {
        ring = gameObject.AddComponent<LineRenderer>();
        ring.loop                = true;
        ring.positionCount       = SEGMENTS;
        ring.useWorldSpace       = false;
        ring.widthMultiplier     = BASE_WIDTH;
        ring.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows      = false;
        ring.allowOcclusionWhenDynamic = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.renderQueue = 4000;
            ring.material   = mat;
        }

        Color initialColor = new Color(0.65f, 0.30f, 0.90f, 0.20f);
        ring.startColor = initialColor;
        ring.endColor   = initialColor;
    }

    void UpdatePositions(float radius)
    {
        if (ring == null) return;

        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = 2f * Mathf.PI * i / SEGMENTS;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                RING_Y,
                Mathf.Sin(angle) * radius
            ));
        }
    }
}
