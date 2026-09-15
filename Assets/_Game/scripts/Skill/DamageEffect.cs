using UnityEngine;

namespace Pokemon.Domain.Effects
{
    // 添加 CreateAssetMenu，使其可以在 Project 窗口中右键创建
    [CreateAssetMenu(fileName = "伤害效果", menuName = "宝可梦/技能效果/造成伤害")]
    public class DamageEffect : SkillEffectSO // 改为继承 SkillEffectSO
    {
        // 使用 override 重写基类的抽象方法
        public override bool CanProcess(SkillEffectContext context) => context.Damage != null;

        public override void Execute(SkillEffectContext context)
        {
            int damage = context.Damage.Value.FinalDamage;
            if (context.IncomingReaction != null &&
                context.IncomingReaction.TryInterceptDamage(
                    context.ReactionContext, context, damage))
            {
                return;
            }

            context.Operations.DealDamage(new BattleDamageRequest
                {
                    Source = context.User,
                    Target = context.Target,
                    Skill = context.Skill,
                    Amount = damage,
                    Cause = BattleDamageCause.Skill,
                    Origin = this
                });
        }
    }
}
