using System;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AbyssBar.Characters
{
    // Read-only, shared text composition. The same assets can feed a future
    // portrait panel, roster browser or localization layer without role switches.
    public static class CharacterProfileText
    {
        private static string Safe(string value) => (value ?? string.Empty).Replace("&", "＆").Replace("<", "＜").Replace(">", "＞");
        private static string Number(float value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);
        public static string Overview(CharacterData character)
            => Overview(character, true);
        public static string Overview(CharacterData character, bool includeName)
        {
            if (character == null) return string.Empty;
            var text = new StringBuilder();
            if (includeName) text.Append("<b>").Append(Safe(character.DisplayName)).Append("</b>");
            var p = character.IdentityProfile;
            if (p == null) return text.ToString();
            text.Append(includeName ? "\n" : "").Append("<color=#D7B878>").Append(Safe(p.roleTitle)).Append("｜").Append(Safe(p.combatRole)).Append("</color>");
            if (!string.IsNullOrWhiteSpace(p.summary)) text.Append("\n\n").Append(Safe(p.summary));
            string tags = string.Join(" · ", (p.specialties ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(Safe));
            if (tags.Length > 0) text.Append("\n\n<color=#8DCCCF>").Append(tags).Append("</color>");
            if (!string.IsNullOrWhiteSpace(p.strength)) text.Append("\n\n<b>角色優勢</b>\n").Append(Safe(p.strength));
            if (!string.IsNullOrWhiteSpace(p.caution)) text.Append("\n\n<b>操作提醒</b>\n").Append(Safe(p.caution));
            return text.ToString();
        }
        public static string Details(CharacterData character)
            => character == null ? string.Empty : Stats(character) + "\n\n" + Skill(character, false) + "\n\n" + Skill(character, true);
        public static string Stats(CharacterData character)
        {
            if (character == null) return string.Empty;
            CharacterTrainingBonuses bonuses = CharacterProgressionSave.GetBonuses(character);
            var weapon = character.StartingWeapon;
            string interval = weapon != null
                ? Number(Mathf.Max(.02f, weapon.AttackInterval / (1 + Mathf.Max(0, bonuses.AttackSpeed))), "0.00") + " 秒"
                : "—";
            var text = new StringBuilder("<b>出戰數值</b>\n");
            text.Append("生命 ").Append(CurrencySave.GetStartingMaxHealth(character)).Append("　移速 ").Append(Number(character.BaseMoveSpeed));
            text.Append("\n普攻間隔 ").Append(interval).Append("　傷害加成 +").Append(bonuses.Damage);
            text.Append("\n武器 ").Append(weapon != null ? Safe(weapon.DisplayName) : "—");
            text.Append("\n閃避距離加成 +").Append(Number(bonuses.DodgeDistance));
            if(weapon!=null&&weapon.PrimaryAttackStyle is AbyssBar.Mio.MioPrimaryAttackStyle sword&&sword.definition!=null)
            {
                var tuning=sword.definition.tuning;
                text.Append("\n近斬傷害 ").Append(Number(tuning.tapDamage+bonuses.Damage)).Append("　蓄力傷害 ").Append(Number(tuning.chargedDamage+bonuses.Damage));
                text.Append("\n蓄力 ").Append(Number(Mathf.Max(.12f,tuning.chargeSeconds-bonuses.ChargeTimeReduction))).Append(" 秒　E 基礎充能 2");
            }
            return text.ToString();
        }
        public static string Skill(CharacterData character, bool ultimate)
        {
            if (character == null) return string.Empty;
            var p = character.IdentityProfile;
            var data = ultimate ? CharacterProgressionSave.GetBattleQSkill(character) : CharacterProgressionSave.GetBattleESkill(character);
            bool unlocked = !ultimate || CharacterProgressionSave.IsQUnlocked(character);
            bool enhanced = data != null && (ultimate
                ? CharacterProgressionSave.IsQEnhanced(character) && character.HasConfiguredEnhancedQSkill && data == character.EnhancedQSkill
                : CharacterProgressionSave.IsEEnhanced(character) && character.HasConfiguredEnhancedESkill && data == character.EnhancedESkill);
            string key = ultimate ? "Q 大招" : "E 技能";
            if (data == null || !data.IsConfigured) return "<b>" + key + "</b>｜尚不可用";
            string status = !unlocked ? "未解鎖・完成培養節點 1 開放" : enhanced ? "強化版" : "基礎版";
            string hint = p == null ? string.Empty : ultimate
                ? enhanced && !string.IsNullOrWhiteSpace(p.enhancedQHint) ? p.enhancedQHint : p.baseQHint
                : enhanced && !string.IsNullOrWhiteSpace(p.enhancedEHint) ? p.enhancedEHint : p.baseEHint;
            return "<b>" + key + "：" + Safe(data.DisplayName) + "</b>\n" + status + "｜基礎冷卻 " + Number(data.Cooldown) + " 秒"
                + (string.IsNullOrWhiteSpace(hint) ? string.Empty : "\n" + (!unlocked ? "能力預覽：" : string.Empty) + Safe(hint));
        }
    }
}
