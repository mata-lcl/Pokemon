namespace Pokemon.Domain.Effects
{
    public class DamageEffect : ISkillEffect
    {
        public bool CanProcess(EffectContext context) => context.Damage != null;

        public void Execute(EffectContext context)
        {
            context.Target.ApplyDamage(context.Damage.Value.FinalDamage);

            context.Steps.Add(new Application.TurnStep
            {
                Message = $"{context.User.Species.DisplayName} 造成了 {context.Damage.Value.FinalDamage} 点伤害！",
                PlayerHpAfter = context.User.CurrentHP, // 这里逻辑稍后在UseCase优化
                EnemyHpAfter = context.Target.CurrentHP,
                AnimType = Application.StepAnimType.None // 暂时设为None，由外层控制主程序
            });
        }
    }
}