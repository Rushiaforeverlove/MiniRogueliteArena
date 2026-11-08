using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HubCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private float smoothSpeed = 8f;

    private Camera hubCamera;
    private float preferredSize;
    private float preferredY;
    private Transform previewTarget;
    private float previewSize = 1.8f;
    private AbyssBar.Hub.HubSceneBounds sceneBounds;
    private Vector2 selectionOverview;
    private bool hasSelectionOverview;
    public bool IsPreviewing => previewTarget != null;

    private void Awake()
    {
        hubCamera = GetComponent<Camera>();
        preferredSize = hubCamera.orthographicSize;
        preferredY = transform.position.y;
        sceneBounds=FindAnyObjectByType<AbyssBar.Hub.HubSceneBounds>();
    }

    public void FocusCharacter(Transform character, float size = 1.8f)
    { previewTarget = character; previewSize = Mathf.Max(.5f, size); }
    public void EndCharacterFocus() { previewTarget = null; }

    public void ShowSeatOverview(Transform[] seats)
    {
        float left=float.PositiveInfinity,right=float.NegativeInfinity;
        foreach(var seat in seats)
            if(seat!=null&&seat.gameObject.activeInHierarchy){left=Mathf.Min(left,seat.position.x);right=Mathf.Max(right,seat.position.x);}
        if(float.IsInfinity(left))return;
        selectionOverview=new Vector2((left+right)*.5f,preferredY);hasSelectionOverview=true;
        transform.position=new Vector3(selectionOverview.x,selectionOverview.y,transform.position.z);
        hubCamera.orthographicSize=preferredSize;
    }

    public void SnapToPlayer()
    {
        previewTarget=null;
        if(target==null)return;
        transform.position=new Vector3(target.position.x,preferredY,transform.position.z);
        hubCamera.orthographicSize=preferredSize;
    }

    public void PanSelectionByPixels(float pixels)
    {
        if(!hasSelectionOverview||previewTarget!=null||target!=null&&target.gameObject.activeInHierarchy)return;
        if(sceneBounds==null)sceneBounds=FindAnyObjectByType<AbyssBar.Hub.HubSceneBounds>();
        if(sceneBounds==null&&background==null)return;
        var bounds=sceneBounds!=null?sceneBounds.WorldBounds:background.bounds;
        float halfWidth=preferredSize*hubCamera.aspect;
        float next=selectionOverview.x+pixels*(2*preferredSize/Mathf.Max(1,hubCamera.pixelHeight));
        float min=bounds.min.x+halfWidth,max=bounds.max.x-halfWidth;
        selectionOverview.x=max>min?Mathf.Clamp(next,min,max):bounds.center.x;
        transform.position=new Vector3(selectionOverview.x,selectionOverview.y,transform.position.z);
        hubCamera.orthographicSize=preferredSize;
    }

    private void LateUpdate()
    {
        if (previewTarget != null)
        {
            var renderer = previewTarget.GetComponent<SpriteRenderer>();
            Vector3 center = renderer != null ? renderer.bounds.center : previewTarget.position;
            center.z = transform.position.z;
            float blend = 1 - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, center, blend);
            hubCamera.orthographicSize = Mathf.Lerp(hubCamera.orthographicSize, previewSize, blend);
            return;
        }
        if(sceneBounds==null)sceneBounds=FindAnyObjectByType<AbyssBar.Hub.HubSceneBounds>();
        if (target == null || background == null&&sceneBounds==null) return;

        // Before the first selection the player is deliberately inactive: frame the seats,
        // rather than following its obsolete scene position in the enlarged room.
        if(!target.gameObject.activeInHierarchy&&hasSelectionOverview)
        {
            transform.position=new Vector3(selectionOverview.x,selectionOverview.y,transform.position.z);
            hubCamera.orthographicSize=preferredSize;
            return;
        }

        Bounds bounds = sceneBounds!=null?sceneBounds.WorldBounds:background.bounds;
        float sizeToFit = Mathf.Min(
            bounds.extents.y, bounds.extents.x / hubCamera.aspect);
        hubCamera.orthographicSize = Mathf.Lerp(hubCamera.orthographicSize, Mathf.Min(preferredSize, sizeToFit),
            1 - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));

        float halfWidth = hubCamera.orthographicSize * hubCamera.aspect;
        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float targetX = Mathf.Clamp(target.position.x, minX, maxX);

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(
            Mathf.Lerp(position.x, targetX, smoothSpeed * Time.deltaTime),
            minX, maxX);
        position.y = Mathf.Lerp(position.y, preferredY, 1 - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));

        transform.position = position;
    }
}
