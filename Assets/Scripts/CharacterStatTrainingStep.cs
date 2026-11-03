using UnityEngine;

public enum TrainingStatKind { MaxHealth = 0, Damage = 1, AttackSpeed = 2, DodgeDistance = 3 }

// One data entry per node-2 purchase. AttackSpeed uses 0.1 for +10%.
[System.Serializable]
public class CharacterStatTrainingStep
{
    [SerializeField] private TrainingStatKind kind;
    [SerializeField, Min(0.01f)] private float amount = 1f;
    [SerializeField, Min(1)] private int cost = 60;
    public TrainingStatKind Kind => kind;
    public float Amount => amount;
    public int Cost => cost;
    public bool HasValidEffect
    {
        get
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f || amount > 1000f) return false;
            if (kind == TrainingStatKind.MaxHealth || kind == TrainingStatKind.Damage)
                return amount == Mathf.RoundToInt(amount);
            return kind == TrainingStatKind.AttackSpeed || kind == TrainingStatKind.DodgeDistance;
        }
    }
    public bool IsConfigured => HasValidEffect && cost > 0;
    public string Description
    {
        get
        {
            if (!HasValidEffect) return "效果設定錯誤";
            switch (kind)
            {
                case TrainingStatKind.MaxHealth: return $"生命 +{amount:0}";
                case TrainingStatKind.Damage: return $"傷害 +{amount:0}（普攻與 E／Q）";
                case TrainingStatKind.AttackSpeed: return $"普攻攻速 +{amount * 100f:0.#}%";
                default: return $"閃避距離 +{amount:0.##}";
            }
        }
    }
}

// Battle snapshot. Asset definitions and PlayerPrefs are never modified by applying it.
public struct CharacterTrainingBonuses
{
    public int MaxHealth;
    public int Damage;
    public float AttackSpeed;
    public float DodgeDistance;
}
