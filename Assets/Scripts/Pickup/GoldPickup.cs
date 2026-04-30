using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    public int goldAmount = 10;
    public float rotateSpeed = 120f;

    private bool collected;

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
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

        collected = true;

        wallet.AddGold(goldAmount);

        if (RunContractSystem.Instance != null)
            RunContractSystem.Instance.RegisterGoldCollected(goldAmount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGoldPickup();

        Destroy(gameObject);
    }
}