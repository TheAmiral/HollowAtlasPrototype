using System.Collections.Generic;
using UnityEngine;

public class BossSpecialAttack : MonoBehaviour
{
    [Header("Projectile Burst")]
    public GameObject projectilePrefab;
    public float attackCooldown = 5f;
    public float triggerRange = 9f;
    public float chargeDuration = 0.75f;
    public int projectileCount = 12;
    public float projectileSpeed = 7f;
    public int projectileDamage = 10;
    public float spawnHeight = 1f;

    [Header("Spawn Lock")]
    public float spawnLockDuration = 1.0f;

    [Header("Charge Visual")]
    public Color chargeColor = new Color(1f, 0.85f, 0.85f, 1f);

    [Header("Preview Ring")]
    public float previewRadius = 1.8f;
    public float previewScale = 0.26f;
    public Color previewColor = new Color(1f, 0.82f, 0.45f, 0.95f);

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyChaser enemyChaser;
    private EnemyHealth enemyHealth;

    private float cooldownTimer;
    private float spawnLockTimer;
    private bool isCharging;
    private float chargeTimer;
    private bool ownerDead;
    private bool initialized;

    private readonly List<Material> cachedMaterials = new();
    private readonly List<Color> originalColors = new();

    private readonly List<GameObject> previewMarkers = new();
    private readonly List<Material> previewMaterials = new();
    private readonly List<Vector3> previewDirections = new();

    void Awake()
    {
        enemyChaser = GetComponent<EnemyChaser>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        // Awake sırasında BossSpawnSystem henüz isBoss=true setlememiş olabilir.
        // Instantiate döndükten sonra set edildiği için kontrol Start'a taşındı.
        if (enemyHealth == null || !enemyHealth.IsBoss)
        {
            enabled = false;
            return;
        }

        CacheVisuals();
        initialized = true;

        FindPlayer();
        cooldownTimer = attackCooldown;
        spawnLockTimer = spawnLockDuration;
    }

    void Update()
    {
        if (!initialized || ownerDead)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            CancelChargeAndCleanup();
            return;
        }

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            HandleOwnerDeath();
            return;
        }

        if (player == null || playerHealth == null)
        {
            FindPlayer();
            return;
        }

        if (playerHealth.IsDead || playerHealth.CurrentHealth <= 0)
        {
            CancelChargeAndCleanup();
            return;
        }

        if (spawnLockTimer > 0f)
        {
            spawnLockTimer -= Time.deltaTime;
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;

            float t = 1f - Mathf.Clamp01(chargeTimer / chargeDuration);
            UpdateChargeVisual(t);
            UpdatePreviewRingVisual(t);

            if (chargeTimer <= 0f)
            {
                if (ownerDead || enemyHealth == null || enemyHealth.IsDead)
                {
                    HandleOwnerDeath();
                    return;
                }

                FireRadialBurst();
                EndCharge();
                cooldownTimer = attackCooldown;
            }

            return;
        }

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f && distance <= triggerRange)
        {
            StartCharge();
        }
    }

    public void ScaleForWave(int waveScale)
    {
        projectileDamage += waveScale * 2;
        attackCooldown = Mathf.Max(3.0f, attackCooldown - waveScale * 0.35f);
    }

    public void HandleOwnerDeath()
    {
        if (ownerDead)
            return;

        ownerDead = true;
        CancelChargeAndCleanup();
        enabled = false;
    }

    void StartCharge()
    {
        if (isCharging || ownerDead)
            return;

        isCharging = true;
        chargeTimer = chargeDuration;

        if (enemyChaser != null)
            enemyChaser.enabled = false;

        CreatePreviewRing();
        UpdateChargeVisual(0f);
        UpdatePreviewRingVisual(0f);
    }

    void EndCharge()
    {
        isCharging = false;
        chargeTimer = 0f;

        if (enemyChaser != null && !ownerDead && (enemyHealth == null || !enemyHealth.IsDead))
            enemyChaser.enabled = true;

        RestoreOriginalColors();
        ClearPreviewRing();
    }

    void CancelChargeAndCleanup()
    {
        isCharging = false;
        chargeTimer = 0f;

        RestoreOriginalColors();
        ClearPreviewRing();

        if (enemyChaser != null && !ownerDead && (enemyHealth == null || !enemyHealth.IsDead))
            enemyChaser.enabled = true;
    }

    void FireRadialBurst()
    {
        if (projectilePrefab == null || ownerDead || (enemyHealth != null && enemyHealth.IsDead))
            return;

        int count = Mathf.Max(4, projectileCount);
        Vector3 origin = transform.position + Vector3.up * spawnHeight;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);

            EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
                projectileScript.Initialize(direction, projectileDamage, projectileSpeed);

            if (direction.sqrMagnitude > 0.001f)
                projectile.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void CreatePreviewRing()
    {
        ClearPreviewRing();
        previewDirections.Clear();

        int count = Mathf.Max(4, projectileCount);
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
            previewDirections.Add(direction);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "BossPreviewMarker";

            Collider c = marker.GetComponent<Collider>();
            if (c != null)
                Destroy(c);

            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = direction * previewRadius + Vector3.up * spawnHeight;
            marker.transform.localScale = Vector3.one * previewScale;

            Renderer r = marker.GetComponent<Renderer>();
            if (r != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                Material mat = new Material(shader);
                SetMaterialColor(mat, previewColor);
                r.material = mat;
                previewMaterials.Add(mat);
            }

            previewMarkers.Add(marker);
        }
    }

    void UpdatePreviewRingVisual(float t)
    {
        if (previewMarkers.Count == 0 || previewDirections.Count != previewMarkers.Count)
            return;

        float radius = Mathf.Lerp(previewRadius * 0.85f, previewRadius * 1.05f, t);
        float scale = Mathf.Lerp(previewScale * 0.9f, previewScale * 1.12f, t);
        Color pulseColor = Color.Lerp(previewColor, Color.white, t * 0.7f);

        for (int i = 0; i < previewMarkers.Count; i++)
        {
            GameObject marker = previewMarkers[i];
            if (marker == null)
                continue;

            Vector3 direction = previewDirections[i];
            marker.transform.localPosition = direction * radius + Vector3.up * spawnHeight;
            marker.transform.localScale = Vector3.one * scale;

            if (i < previewMaterials.Count && previewMaterials[i] != null)
                SetMaterialColor(previewMaterials[i], pulseColor);
        }
    }

    void ClearPreviewRing()
    {
        for (int i = 0; i < previewMarkers.Count; i++)
        {
            if (previewMarkers[i] != null)
                Destroy(previewMarkers[i]);
        }

        previewMarkers.Clear();
        previewDirections.Clear();

        for (int i = 0; i < previewMaterials.Count; i++)
        {
            if (previewMaterials[i] != null)
                Destroy(previewMaterials[i]);
        }

        previewMaterials.Clear();
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerHealth = playerObject.GetComponent<PlayerHealth>();
    }

    void CacheVisuals()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                cachedMaterials.Add(mats[j]);
                originalColors.Add(ReadColor(mats[j]));
            }
        }
    }

    void UpdateChargeVisual(float t)
    {
        for (int i = 0; i < cachedMaterials.Count; i++)
        {
            if (cachedMaterials[i] == null)
                continue;

            Color c = Color.Lerp(originalColors[i], chargeColor, t);
            WriteColor(cachedMaterials[i], c);
        }
    }

    void RestoreOriginalColors()
    {
        for (int i = 0; i < cachedMaterials.Count; i++)
        {
            if (cachedMaterials[i] != null)
                WriteColor(cachedMaterials[i], originalColors[i]);
        }
    }

    Color ReadColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        if (mat.HasProperty("_Color"))
            return mat.GetColor("_Color");

        return Color.white;
    }

    void WriteColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }

    void SetMaterialColor(Material mat, Color color)
    {
        WriteColor(mat, color);
    }

    void OnDisable()
    {
        CancelChargeAndCleanup();
    }

    void OnDestroy()
    {
        ClearPreviewRing();
    }
}