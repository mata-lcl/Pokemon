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
            Application.StatusApplyResult result =
                context.Operations.ApplyStatus(new Application.StatusApplyRequest
                {
                    Source = context.User,
                    Target = context.Target,
                    Skill = context.Skill,
                    Status = StatusToApply,
                    Cause = Application.BattleStatusCause.Skill,
                    Origin = this
                });

            if (result.Applied)
            {
                context.AddStep($"{context.Target.Species.DisplayName}{StatusToApply.ToChineseName()} 了！");
            }
        }
    }
}
