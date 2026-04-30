using UnityEngine;
using UnityEngine.UI;

public class LevelPulseRing : MonoBehaviour
{
    [SerializeField] private Image pulseImage;
    [SerializeField] private float speed = 2.5f;

    public void SetPulseImage(Image img) => pulseImage = img;

    private void Update()
    {
        if (pulseImage == null) return;
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Color c = pulseImage.color;
        c.a = Mathf.Lerp(0.3f, 0.7f, t);
        pulseImage.color       = c;
        pulseImage.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, t);
    }
}
