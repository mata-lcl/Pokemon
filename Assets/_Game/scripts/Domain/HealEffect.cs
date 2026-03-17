namespace Pokemon.Domain.Effects
{
    public class HealEffect : ISkillEffect
    {
        private readonly int _amount;
        public HealEffect(int amount) => _amount = amount;

        public bool CanProcess(EffectContext context) => true;

        public void Execute(EffectContext context)
        {
            context.User.Heal(_amount);
            context.Steps.Add(new Application.TurnStep
            {
                Message = $"{context.User.Species.DisplayName} 恢复了 {_amount} 点生命值！"
            });
        }
    }
}