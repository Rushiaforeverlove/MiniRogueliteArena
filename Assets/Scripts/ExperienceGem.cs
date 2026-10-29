using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField] private int experienceValue = 1;
    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.01f)] private float collectDistance = 0.2f;

    private PlayerExperience target;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerExperience player = other.GetComponent<PlayerExperience>();

        if (player != null)
        {
            target = player;
        }
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 targetPosition = target.transform.position;
        targetPosition.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        float distance = Vector2.Distance(
            transform.position, targetPosition);

        if (distance <= collectDistance)
        {
            enabled = false;
            target.AddExperience(experienceValue);
            Destroy(gameObject);
        }
    }
}