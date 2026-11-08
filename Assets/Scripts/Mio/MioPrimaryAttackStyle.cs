using AbyssBar.Combat;
using UnityEngine;
using System.Linq;
using AbyssBar.Upgrades;

namespace AbyssBar.Mio
{
    [CreateAssetMenu(menuName = "Abyss Bar/Mio/Greatsword Primary Style")]
    public sealed class MioPrimaryAttackStyle : PrimaryAttackStyle, IUpgradeCardDescription
    {
        public MioCharacterDefinition definition;
        public float BaseDamage => definition != null ? definition.tuning.tapDamage : 180;
        public override PrimaryAttackSession CreateSession() => new Session(definition);
        public string DescribeUpgrade(UpgradeCardData card)
        {
            if(card==null)return string.Empty;
            if(card.effectKey=="attack.extra_projectiles")
            {
                var number=card.numbers.FirstOrDefault(n=>n!=null&&n.key=="extraCounts");
                string counts=number?.values!=null?string.Join("／",number.values.Select(n=>Mathf.RoundToInt(n).ToString())):"1";
                return "普攻額外 +"+counts+" 道副擊；近斬為副斬，劍氣為副劍氣。每道造成主擊 "+Mathf.RoundToInt(card.Number("coefficient",1,.6f)*100)+"% 傷害，整次仍只計一個普攻動作。";
            }
            if(card.effectKey=="mio.circular_space")return "E 旋斬額外擊退 "+card.Number("push")+"；之後 "+card.Number("window")+" 秒內第一次點按，將扇形內敵人輕拉向前方。";
            return card.description;
        }
        private sealed class Session : PrimaryAttackSession
        {
            private readonly MioCharacterDefinition definition;
            public Session(MioCharacterDefinition data) { definition = data; }
            public override void Tick(PlayerAutoAttack actor, PrimaryAttackInput input)
            {
                var combat = actor.GetComponent<MioCombat>();
                if (combat == null || definition == null) return;
                combat.SamplePrimary(input);
            }
            public override void Cancel(PlayerAutoAttack actor) => actor.GetComponent<MioCombat>()?.CancelPrimary();
        }
    }
}
