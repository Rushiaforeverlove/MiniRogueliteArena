using UnityEngine;

namespace AbyssBar.Enemies
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public GameObject prefab;

        public bool Validate(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(enemyId)) error = "Enemy definition needs a stable ID.";
            else if (prefab == null || prefab.GetComponent<EnemyController>() == null
                || prefab.GetComponent<EnemyHealth>() == null || !prefab.GetComponent<EnemyHealth>().HasValidStartingHealth)
                error = "Enemy " + enemyId + " needs a prefab with EnemyController and valid EnemyHealth.";
            else if (prefab.GetComponent<IEnemyCombatBinding>() is IEnemyCombatBinding combat && !combat.IsConfigured)
                error = "Enemy " + enemyId + " has missing/invalid combat settings.";
            return error == null;
        }
    }
}
