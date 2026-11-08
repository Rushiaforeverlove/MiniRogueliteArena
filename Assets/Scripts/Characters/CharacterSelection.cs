using UnityEngine;

// Scene changes keep this choice. This is session state, not a disk save.
public static class CharacterSelection
{
    public static CharacterData Current { get; set; }

    // Also clear the choice when entering Play Mode with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        Current = null;
    }
}
