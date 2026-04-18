using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    public int goldAmount = 10;
    public float rotateSpeed = 120f;

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        GoldWallet wallet = other.GetComponent<GoldWallet>();
        if (wallet == null) return;

        wallet.AddGold(goldAmount);
        Destroy(gameObject);
    }
}