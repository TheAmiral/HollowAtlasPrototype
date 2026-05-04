using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVisuals : MonoBehaviour
{
    public float hitFlashDuration = 0.08f;
    public Color hitFlashColor = Color.white;
    public float deathDuration = 0.25f;

    [Header("Audio Override")]
    [SerializeField] AudioClip hitSfxOverride;
    [SerializeField] AudioClip deathSfxOverride;

    private Renderer[] renderers;
    private readonly List<Material> materials = new();
    private readonly List<Color> originalColors = new();

    private Coroutine flashRoutine;
    private bool isDying;
    private Vector3 originalScale;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalScale = transform.localScale;

        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials; // instance materyal oluşturur
            foreach (Material m in mats)
            {
                materials.Add(m);
                originalColors.Add(ReadColor(m));
            }
        }
    }

    public void PlayHitFlash()
    {
        if (isDying || materials.Count == 0)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    public void PlayDeathAndDestroy()
    {
        if (isDying)
            return;

        isDying = true;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        if (hitSfxOverride != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(hitSfxOverride, 0.75f);

        SetAllColors(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreColors();
        flashRoutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        if (deathSfxOverride != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(deathSfxOverride);

        SetAllColors(hitFlashColor);

        // Brief scale-up pop before shrinking
        const float bounceDur = 0.06f;
        float bounceTimer = 0f;
        Vector3 bouncePeak = originalScale * 1.12f;
        while (bounceTimer < bounceDur)
        {
            bounceTimer += Time.deltaTime;
            float t = bounceTimer / bounceDur;
            transform.localScale = Vector3.Lerp(originalScale, bouncePeak, t);
            yield return null;
        }

        float timer = 0f;
        while (timer < deathDuration)
        {
            timer += Time.deltaTime;
            float t = timer / deathDuration;
            transform.localScale = Vector3.Lerp(bouncePeak, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAllColors(Color color)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            WriteColor(materials[i], color);
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            WriteColor(materials[i], originalColors[i]);
        }
    }

    private Color ReadColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        if (mat.HasProperty("_Color"))
            return mat.GetColor("_Color");

        return Color.white;
    }

    private void WriteColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }
}