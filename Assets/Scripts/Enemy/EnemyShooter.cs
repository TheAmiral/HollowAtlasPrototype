using System.Collections.Generic;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float turnSpeed = 10f;

    [Header("Distance")]
    public float desiredDistance = 6f;
    public float stopDistance = 5f;
    public float retreatDistance = 3f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootCooldown = 1.4f;
    public float projectileSpeed = 10f;
    public int projectileDamage = 8;

    [Header("Pre-Fire Charge")]
    public float chargeDuration = 0.22f;
    public Color chargeColor = Color.white;

    private Transform target;
    private PlayerHealth playerHealth;
    private float shootTimer;

    private bool isCharging;
    private float chargeTimer;

    private Renderer[] cachedRenderers;
    private readonly List<Material> cachedMaterials = new();
    private readonly List<Color> originalColors = new();

    void Awake()
    {
        CacheVisuals();
    }

    void Start()
    {
        FindPlayer();
        shootTimer = Random.Range(0.2f, shootCooldown);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (target == null || playerHealth == null)
        {
            FindPlayer();
            return;
        }

        if (playerHealth.CurrentHealth <= 0)
            return;

        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > 0.05f)
        {
            Vector3 lookDir = toPlayer.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;

            float t = 1f - Mathf.Clamp01(chargeTimer / chargeDuration);
            UpdateChargeVisual(t);

            if (chargeTimer <= 0f)
            {
                Shoot();
                RestoreOriginalColors();

                isCharging = false;
                shootTimer = shootCooldown;
            }

            return;
        }

        shootTimer -= Time.deltaTime;

        if (distance > 0.05f)
        {
            Vector3 moveDir = toPlayer.normalized;

            if (distance > desiredDistance)
            {
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
            else if (distance < retreatDistance)
            {
                transform.position -= moveDir * moveSpeed * Time.deltaTime;
            }
        }

        if (distance <= stopDistance && shootTimer <= chargeDuration)
        {
            StartCharge();
        }
    }

    void StartCharge()
    {
        if (isCharging)
            return;

        isCharging = true;
        chargeTimer = chargeDuration;
        UpdateChargeVisual(0f);
    }

    void Shoot()
    {
        if (projectilePrefab == null)
            return;

        Vector3 origin = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * 0.8f;

        Vector3 direction = (target.position + Vector3.up * 0.5f - origin).normalized;

        GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);

        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, projectileDamage, projectileSpeed);
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            projectile.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        target = player.transform;
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    void CacheVisuals()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in cachedRenderers)
        {
            Material[] mats = r.materials;
            foreach (Material m in mats)
            {
                cachedMaterials.Add(m);
                originalColors.Add(ReadColor(m));
            }
        }
    }

    void UpdateChargeVisual(float t)
    {
        for (int i = 0; i < cachedMaterials.Count; i++)
        {
            Color c = Color.Lerp(originalColors[i], chargeColor, t);
            WriteColor(cachedMaterials[i], c);
        }
    }

    void RestoreOriginalColors()
    {
        for (int i = 0; i < cachedMaterials.Count; i++)
        {
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

    void OnDisable()
    {
        RestoreOriginalColors();
    }
}