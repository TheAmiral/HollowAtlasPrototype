using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    public int goldAmount = 10;
    public float rotateSpeed = 120f;
    public float collectRadius = 1.2f;

    private bool collected;

    private Transform  playerTransform;
    private GoldWallet playerWallet;

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
        if (playerTransform == null || playerWallet == null)
            return;

        Vector2 self   = new Vector2(transform.position.x, transform.position.z);
        Vector2 player = new Vector2(playerTransform.position.x, playerTransform.position.z);

        if (Vector2.Distance(self, player) <= collectRadius)
            Collect(playerWallet);
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

        float multiplier = RelicInventory.Instance != null ? RelicInventory.Instance.GoldMultiplier : 1f;
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
