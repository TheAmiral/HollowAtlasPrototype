using UnityEngine;
using UnityEngine.UI;

public class XpBarFlowEffect : MonoBehaviour
{
    [SerializeField] private RawImage flowImage;
    [SerializeField] private float    scrollSpeed = 0.5f;

    public void SetFlowImage(RawImage img) => flowImage = img;

    private void Update()
    {
        if (flowImage == null) return;
        Rect uv = flowImage.uvRect;
        uv.x = (uv.x + scrollSpeed * Time.deltaTime) % 1f;
        flowImage.uvRect = uv;
    }
}
