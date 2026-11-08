using UnityEngine;

namespace AbyssBar.Mio
{
    public static class MioBootstrap
    {
        public static void Configure(GameObject actor, CharacterData character, bool hub)
        {
            var data = MioCharacterDefinition.Load();
            bool match = data != null && data.Matches(character);
            var combat = actor.GetComponent<MioCombat>();
            var presentation = actor.GetComponent<MioAnimationPresenter>();
            if (!match)
            {
                if (combat != null) { combat.CancelSkillActions(); combat.enabled = false; }
                if (presentation != null) presentation.SetDefinition(null, hub);
                return;
            }
            if (presentation == null) presentation = actor.AddComponent<MioAnimationPresenter>();
            presentation.SetDefinition(data, hub);
            if (!hub)
            {
                if (combat == null) combat = actor.AddComponent<MioCombat>();
                combat.enabled = true; combat.Configure(data, presentation);
            }
        }
    }
}
