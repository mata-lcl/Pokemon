using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Pokemon/Effects/Apply Status")]
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
