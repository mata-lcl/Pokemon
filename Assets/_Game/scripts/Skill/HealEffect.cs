using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "治疗效果", menuName = "宝可梦/技能效果/回复生命")]
    public class HealEffect : SkillEffectSO // 改为继承 SkillEffectSO
    {
        [Tooltip("固定回复生命值的数值")]
        [SerializeField] private int _amount;

        public override bool CanProcess(SkillEffectContext context) => true;

        public override void Execute(SkillEffectContext context)
        {
            context.Operations.Heal(new BattleHealRequest
                {
                    Source = context.User,
                    Target = context.User,
                    Skill = context.Skill,
                    Amount = _amount,
                    Cause = BattleHealCause.Skill,
                    Origin = this
                });
        }
    }
}
