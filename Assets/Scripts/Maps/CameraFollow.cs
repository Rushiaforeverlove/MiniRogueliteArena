using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer arenaFloor;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Camera followCamera;
    private float preferredSize;

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
        preferredSize = followCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        if (target == null || arenaFloor == null) return;

        Bounds bounds = arenaFloor.bounds;
        float sizeToFit = Mathf.Min(
            bounds.extents.y, bounds.extents.x / followCamera.aspect);

        followCamera.orthographicSize = Mathf.Min(preferredSize, sizeToFit);

        float halfHeight = followCamera.orthographicSize;
        float halfWidth = halfHeight * followCamera.aspect;

        Vector3 nextPosition = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smoothSpeed * Time.deltaTime);

        nextPosition.x = Mathf.Clamp(
            nextPosition.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
        nextPosition.y = Mathf.Clamp(
            nextPosition.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);

        transform.position = nextPosition;
    }
}