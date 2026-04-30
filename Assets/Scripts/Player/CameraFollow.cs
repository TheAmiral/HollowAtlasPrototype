using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-8f, 12f, -8f);
    public float smoothTime = 0.08f;

    private Vector3 _velocity;

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.position + offset,
            ref _velocity,
            smoothTime);
        transform.rotation = Quaternion.Euler(45f, 45f, 0f);
    }
}