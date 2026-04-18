using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int xpAmount = 1;
    public float rotateSpeed = 120f;

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerLevelSystem levelSystem = other.GetComponent<PlayerLevelSystem>();
        if (levelSystem == null) return;

        levelSystem.AddXP(xpAmount);
        Destroy(gameObject);
    }
}