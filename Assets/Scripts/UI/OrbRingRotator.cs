using UnityEngine;

public class OrbRingRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = -12f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
