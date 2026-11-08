using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HubPlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float minX = -18f;
    [SerializeField] private float maxX = 18f;
    [SerializeField] private bool canMove;

    private Rigidbody2D body;
    private float horizontalInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        horizontalInput = canMove ? AbyssBar.Inputs.GameInput.Horizontal : 0f;
    }

    private void FixedUpdate()
    {
        if (!canMove || Time.timeScale<=0 || AbyssBar.Inputs.GameInput.Blocked) return;

        float nextX = body.position.x
            + horizontalInput * moveSpeed * Time.fixedDeltaTime;
        nextX = Mathf.Clamp(nextX, minX, maxX);
        body.MovePosition(new Vector2(nextX, body.position.y));
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        horizontalInput = 0f;

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }
    public void SetBounds(float left,float right){minX=Mathf.Min(left,right);maxX=Mathf.Max(left,right);}
    public void SetWalkingSpeed(float value){moveSpeed=Mathf.Max(.1f,value);}
}
