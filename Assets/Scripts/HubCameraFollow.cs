using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HubCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private float smoothSpeed = 8f;

    private Camera hubCamera;
    private float preferredSize;

    private void Awake()
    {
        hubCamera = GetComponent<Camera>();
        preferredSize = hubCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        if (target == null || background == null) return;

        Bounds bounds = background.bounds;
        float sizeToFit = Mathf.Min(
            bounds.extents.y, bounds.extents.x / hubCamera.aspect);
        hubCamera.orthographicSize = Mathf.Min(preferredSize, sizeToFit);

        float halfWidth = hubCamera.orthographicSize * hubCamera.aspect;
        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float targetX = Mathf.Clamp(target.position.x, minX, maxX);

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(
            Mathf.Lerp(position.x, targetX, smoothSpeed * Time.deltaTime),
            minX, maxX);

        transform.position = position;
    }
}