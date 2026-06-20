using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    public int   goldAmount    = 10;
    public float rotateSpeed  = 120f;
    public float collectRadius = 0.8f;   // anlık toplama mesafesi — küçük kalır
    public float attractRadius = 3.5f;   // çekim başladığı mesafe
    public float attractSpeed  = 8f;     // pikap'ın oyuncuya doğru hızı

    private bool collected;
    private bool _attracting;

    private Transform  playerTransform;
    private GoldWallet playerWallet;

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
        if (playerTransform == null || playerWallet == null) return;

        Vector2 selfXZ   = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float dist = Vector2.Distance(selfXZ, playerXZ);

        // Toplama mesafesine girdi: hemen topla
        if (dist <= collectRadius)
        {
            Collect(playerWallet);
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
        if (playerTransform != null && playerWallet != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        playerTransform = player.transform;
        playerWallet    = player.GetComponentInChildren<GoldWallet>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        GoldWallet wallet = other.GetComponent<GoldWallet>();

        if (wallet == null)
            wallet = other.GetComponentInParent<GoldWallet>();

        if (wallet == null)
            return;

        Collect(wallet);
    }

    void Collect(GoldWallet wallet)
    {
        collected = true;

        float multiplier = (RelicInventory.Instance?.GoldMultiplier ?? 1f)
                         * (RunLoadoutSystem.Instance?.GoldPickupMultiplier ?? 1f);
        int   finalGold  = Mathf.RoundToInt(goldAmount * multiplier);
        wallet.AddGold(finalGold);

        if (RunContractSystem.Instance != null)
            RunContractSystem.Instance.RegisterGoldCollected(finalGold);

        DamagePopupSystem.ShowGold(transform.position, finalGold);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGoldPickup();

        VFXSpawner.Instance?.Spawn("GoldPickupSpark", transform.position);

        Destroy(gameObject);
    }
}
