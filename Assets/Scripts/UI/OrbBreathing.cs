using UnityEngine;

public class OrbBreathing : MonoBehaviour
{
    [SerializeField] private float breathSpeed  = 1.5f;
    [SerializeField] private float breathAmount = 0.015f;

    private Vector3 baseScale;

    private void Awake() => baseScale = transform.localScale;

    private void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        transform.localScale = baseScale * s;
    }
}
