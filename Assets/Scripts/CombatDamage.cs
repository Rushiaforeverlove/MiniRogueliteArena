using UnityEngine;

public static class CombatDamage
{
    public static int WithBonus(int baseDamage, int bonus)
    {
        int basis = Mathf.Max(0, baseDamage);
        return basis + Mathf.Min(Mathf.Max(0, bonus), int.MaxValue - basis);
    }
}
