using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "复合技能效果", menuName = "宝可梦/技能效果/复合效果")]
    public class CompositeSkillEffect : SkillEffectSO
    {
        [Tooltip("按列表顺序执行的子效果")]
        [SerializeField] private List<SkillEffectSO> effects = new List<SkillEffectSO>();

        /// <summary>
        /// 判断复合效果中是否至少有一个子效果可以执行。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        /// <returns>存在可执行子效果时返回 true。</returns>
        public override bool CanProcess(SkillEffectContext context)
        {
            foreach (SkillEffectSO effect in effects)
            {
                if (effect != null && effect.CanProcess(context))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 按配置顺序执行所有满足条件的子效果。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        public override void Execute(SkillEffectContext context)
        {
            foreach (SkillEffectSO effect in effects)
            {
                if (effect == null || !effect.CanProcess(context)) continue;

                effect.Execute(context);
                if (context.User.IsFainted || context.Target.IsFainted) break;
            }
        }
    }
}
