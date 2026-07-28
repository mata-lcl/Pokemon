using UnityEngine;

namespace Pokemon.Domain.Effects
{
    // 添加 CreateAssetMenu，使其可以在 Project 窗口中右键创建
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Pokemon/Effects/Damage")]
    public class DamageEffect : SkillEffectSO // 改为继承 SkillEffectSO
    {
        // 使用 override 重写基类的抽象方法
        public override bool CanProcess(EffectContext context) => context.Damage != null;

        public override void Execute(EffectContext context)
        {
            int damage = context.Damage.Value.FinalDamage;
            if (context.IncomingReaction != null &&
                context.IncomingReaction.TryInterceptDamage(
                    context.ReactionContext, context, damage))
            {
                return;
            }

            context.Target.ApplyDamage(damage);

            // 判断是谁在挨打，分配对应的受击动画
            StepAnimType hitAnim = context.IsPlayerAttacking
                ? StepAnimType.EnemyHit
                : StepAnimType.PlayerHit;

            context.AddStep(
                $"{context.User.Species.DisplayName} 造成了 {damage} 点伤害",
                hitAnim);
        }
    }
}
