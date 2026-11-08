using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField] private int experienceValue = 1;
    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.01f)] private float collectDistance = 0.2f;

    private PlayerExperience target;
    private AbyssBar.Upgrades.RunUpgradeRuntime runtime;
    private float nextSearch;

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
        if (Time.timeScale <= 0) return;
        if (target == null)
        {
            if (runtime == null && Time.time >= nextSearch)
            { runtime = FindAnyObjectByType<AbyssBar.Upgrades.RunUpgradeRuntime>(); nextSearch = Time.time + 1; }
            var run = runtime;
            var trigger = GetComponent<CircleCollider2D>();
            float radius = trigger != null ? trigger.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) : 1;
            if (run != null && run.Active && Vector2.Distance(transform.position, run.Position) <= radius * (1 + run.PickupBonus)) target = run.GetComponent<PlayerExperience>();
            if (target == null) return;
        }

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

