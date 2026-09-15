using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "附加异常状态效果", menuName = "宝可梦/技能效果/附加异常状态")]
    public class ApplyStatusEffect : SkillEffectSO
    {
        public StatusCondition StatusToApply;

        public override bool CanProcess(SkillEffectContext context)
        {
            return context.Target != null && !context.Target.IsFainted && context.Target.CurrentStatus == StatusCondition.None;
        }

        public override void Execute(SkillEffectContext context)
        {
            StatusApplyResult result =
                context.Operations.ApplyStatus(new StatusApplyRequest
                {
                    Source = context.User,
                    Target = context.Target,
                    Skill = context.Skill,
                    Status = StatusToApply,
                    Cause = BattleStatusCause.Skill,
                    Origin = this
                });
        }
    }
}
