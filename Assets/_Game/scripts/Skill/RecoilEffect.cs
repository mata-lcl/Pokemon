using Pokemon.Domain;
using UnityEngine;

namespace okemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "RecoilEffect", menuName = "Pokemon/Effects/Recoil")]
    public class RecoilEffect : SkillEffectSO
    {
        [Range(0f,3f)]
        [Tooltip("反伤倍率")]
        public float RecoilMultiplier = 1.5f;

        public override bool CanProcess(EffectContext context)
        {
            return context.Damage != null && context.Damage.Value.FinalDamage > 0;
        }

        public override void Execute(EffectContext context)
        {
           int recoilDamage = Mathf.RoundToInt(context.Damage.Value.FinalDamage * RecoilMultiplier);
            context.Target.ApplyDamage(recoilDamage);

            context.Steps.Add(new Pokemon.Application.TurnStep  
            {
               Message = $"{context.Target.Species.DisplayName} 受到了 {recoilDamage} 点反伤",
               PlayerHpAfter = context.PlayerRef.CurrentHP,
               EnemyHpAfter = context.EnemyRef.CurrentHP,
               AnimType = context.IsPlayerAttacking ? StepAnimType.PlayerHit : StepAnimType.EnemyHit
            });
        }
    }

}