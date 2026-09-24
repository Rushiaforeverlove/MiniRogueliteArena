using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField] private int experienceValue = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerExperience playerExperience = other.GetComponent<PlayerExperience>();

        if (playerExperience == null)
        {
            return;
        }

        playerExperience.AddExperience(experienceValue);
        Destroy(gameObject);
    }
}