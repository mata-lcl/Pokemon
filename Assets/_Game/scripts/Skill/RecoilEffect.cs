using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "反作用伤害效果", menuName = "宝可梦/技能效果/反作用伤害")]
    public class RecoilEffect : SkillEffectSO
    {
        [Range(0f, 3f)]
        [Tooltip("反伤倍率")]
        public float RecoilMultiplier = 1.5f;

        public override bool CanProcess(SkillEffectContext context)
        {
            return context.ActionResult.GetTotalDamage(
                context.User,
                context.Target,
                Application.BattleDamageCause.Skill) > 0;
        }

        public override void Execute(SkillEffectContext context)
        {
            int skillDamage = context.ActionResult.GetTotalDamage(
                context.User,
                context.Target,
                Application.BattleDamageCause.Skill);
            int recoilDamage = Mathf.RoundToInt(skillDamage * RecoilMultiplier);
            // 反伤应打在使用者自己身上
            Application.BattleDamageResult recoilResult =
                context.Operations.DealDamage(new Application.BattleDamageRequest
                {
                    Source = context.User,
                    Target = context.User,
                    Skill = context.Skill,
                    Amount = recoilDamage,
                    Cause = Application.BattleDamageCause.Recoil,
                    Origin = this
                });
            StepAnimType hitAnim = context.IsPlayerAttacking ? StepAnimType.PlayerHit : StepAnimType.EnemyHit;
            context.AddStep($"{context.User.Species.DisplayName} 受到了 {recoilResult.AppliedDamage} 点反伤", hitAnim);
        }
    }
}
