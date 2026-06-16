using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int   xpAmount        = 1;
    public bool  fillsToNextLevel = false;
    public float rotateSpeed     = 120f;
    public float collectRadius   = 0.8f;   // anlık toplama mesafesi — küçük kalır
    public float attractRadius   = 3.5f;   // çekim başladığı mesafe
    public float attractSpeed    = 8f;     // pikap'ın oyuncuya doğru hızı

    private bool collected;
    private bool _attracting;

    private Transform         playerTransform;
    private PlayerLevelSystem playerLevelSystem;

    void Awake()
    {
        ResolvePlayer();
    }

    void Update()
    {
        // Çekilirken dönme animasyonunu durdur
        if (!_attracting)
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        TryCollectByProximity();
    }

    void TryCollectByProximity()
    {
        if (collected) return;
        if (Time.timeScale <= 0f) return;

        ResolvePlayer();
        if (playerTransform == null || playerLevelSystem == null) return;

        Vector2 selfXZ   = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float dist = Vector2.Distance(selfXZ, playerXZ);

        // Toplama mesafesine girdi: hemen topla
        if (dist <= collectRadius)
        {
            Collect(playerLevelSystem);
            return;
        }

        // Çekim alanına girdi: oyuncuya doğru süzül
        float multiplier = RunLoadoutSystem.Instance?.PickupRadiusMultiplier ?? 1f;
        if (dist <= attractRadius * multiplier)
        {
            Vector3 target = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, attractSpeed * Time.deltaTime);
            _attracting = true;
        }
        else
        {
            _attracting = false;
        }
    }

    void ResolvePlayer()
    {
        if (playerTransform != null && playerLevelSystem != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        playerTransform   = player.transform;
        playerLevelSystem = player.GetComponentInChildren<PlayerLevelSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerLevelSystem levelSystem = other.GetComponent<PlayerLevelSystem>();

        if (levelSystem == null)
            levelSystem = other.GetComponentInParent<PlayerLevelSystem>();

        if (levelSystem == null)
            return;

        Collect(levelSystem);
    }

    void Collect(PlayerLevelSystem levelSystem)
    {
        collected = true;

        if (fillsToNextLevel)
            levelSystem.FillXPToNextLevel();
        else
            levelSystem.AddXP(xpAmount);

        if (xpAmount > 0)
            DamagePopupSystem.ShowXP(transform.position, xpAmount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayXpPickup();

        VFXSpawner.Instance?.Spawn("XPPickupSpark", transform.position);

        Destroy(gameObject);
    }
}
