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
            context.Target.ApplyDamage(context.Damage.Value.FinalDamage);

            // 2. 【关键逻辑】：判断是谁在挨打，分配对应的受击动画！
            // 如果是玩家发起的攻击，那挨打的就是敌人 (EnemyHit)
            // 如果不是玩家发起的攻击，那挨打的就是玩家 (PlayerHit)
            Application.StepAnimType hitAnim = context.IsPlayerAttacking
                ? Application.StepAnimType.EnemyHit
                : Application.StepAnimType.PlayerHit;

            context.Steps.Add(new Application.TurnStep
            {
                Message = $"{context.User.Species.DisplayName} 造成了 {context.Damage.Value.FinalDamage} 点伤害",
                PlayerHpAfter = context.User.CurrentHP,
                EnemyHpAfter = context.Target.CurrentHP,
                AnimType = hitAnim 
            });
        }
    }
}