using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Abyss Bar/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private string displayName = "Pistol";
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.15f)] private float attackInterval = 0.6f;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;

    public string DisplayName => displayName;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float AttackInterval => attackInterval;
    public float AttackRange => attackRange;
}