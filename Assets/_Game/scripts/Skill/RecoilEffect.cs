using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "反作用伤害效果", menuName = "宝可梦/技能效果/反作用伤害")]
    public class RecoilEffect : SkillEffectSO
    {
        [Range(0f, 3f)]
        [Tooltip("反伤倍率")]
        public float RecoilMultiplier = 1.5f;

        public override bool CanProcess(EffectContext context)
        {
            return context.Damage != null && context.Damage.Value.FinalDamage > 0;
        }

        public override void Execute(EffectContext context)
        {
            int recoilDamage = Mathf.RoundToInt(context.Damage.Value.FinalDamage * RecoilMultiplier);
            // 反伤应打在使用者自己身上
            context.User.ApplyDamage(recoilDamage);

            StepAnimType hitAnim = context.IsPlayerAttacking ? StepAnimType.PlayerHit : StepAnimType.EnemyHit;
            context.AddStep($"{context.User.Species.DisplayName} 受到了 {recoilDamage} 点反伤", hitAnim);
        }
    }
}
