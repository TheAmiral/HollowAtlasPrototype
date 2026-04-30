using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int xpAmount = 1;
    public bool fillsToNextLevel = false;
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

        PlayerLevelSystem levelSystem = other.GetComponent<PlayerLevelSystem>();

        if (levelSystem == null)
            levelSystem = other.GetComponentInParent<PlayerLevelSystem>();

        if (levelSystem == null)
            return;

        collected = true;

        if (fillsToNextLevel)
            levelSystem.FillXPToNextLevel();
        else
            levelSystem.AddXP(xpAmount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayXpPickup();

        Destroy(gameObject);
    }
}