using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int xpAmount = 1;
    public bool fillsToNextLevel = false;
    public float rotateSpeed = 120f;
    public float collectRadius = 1.2f;

    private bool collected;

    private Transform         playerTransform;
    private PlayerLevelSystem playerLevelSystem;

    void Awake()
    {
        ResolvePlayer();
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        TryCollectByProximity();
    }

    // Toplama collider yüksekliğine bağımlı olmasın diye XZ düzleminde
    // player root mesafesiyle toplanır; Y farkı yok sayılır.
    void TryCollectByProximity()
    {
        if (collected)
            return;

        if (Time.timeScale <= 0f)
            return;

        ResolvePlayer();
        if (playerTransform == null || playerLevelSystem == null)
            return;

        Vector2 self   = new Vector2(transform.position.x, transform.position.z);
        Vector2 player = new Vector2(playerTransform.position.x, playerTransform.position.z);

        if (Vector2.Distance(self, player) <= collectRadius)
            Collect(playerLevelSystem);
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
