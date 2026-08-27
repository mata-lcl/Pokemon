using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "附加异常状态效果", menuName = "宝可梦/技能效果/附加异常状态")]
    public class ApplyStatusEffect : SkillEffectSO
    {
        public StatusCondition StatusToApply;

        public override bool CanProcess(EffectContext context)
        {
            return context.Target != null && !context.Target.IsFainted && context.Target.CurrentStatus == StatusCondition.None;
        }

        public override void Execute(EffectContext context)
        {
            if (context.Target.TryApplyStatus(StatusToApply))
            {
                context.AddStep($"{context.Target.Species.DisplayName}{StatusToApply.ToChineseName()} 了！");
            }
        }
    }
}
