using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Pokemon/Effects/Heal")]
    public class HealEffect : SkillEffectSO // 改为继承 SkillEffectSO
    {
        [Tooltip("固定回复生命值的数值")]
        [SerializeField] private int _amount;

        public override bool CanProcess(EffectContext context) => true;

        public override void Execute(EffectContext context)
        {
            context.User.Heal(_amount);

            context.AddStep($"{context.User.Species.DisplayName} 恢复了 {_amount} 点生命值！");
        }
    }
}