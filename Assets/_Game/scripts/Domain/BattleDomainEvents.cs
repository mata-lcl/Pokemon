namespace Pokemon.Domain
{
    public sealed class SkillUseStartedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Actor { get; set; }
        public SkillData Skill { get; set; }
        public bool IsReactionAnnouncement { get; set; }
    }

    public sealed class SkillPpExhaustedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Actor { get; set; }
        public SkillData Skill { get; set; }
    }

    public sealed class SkillMissedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Actor { get; set; }
        public SkillData Skill { get; set; }
    }

    public sealed class DamageAppliedEvent : IBattleDomainEvent
    {
        public BattleDamageResult Result { get; set; }
    }

    public sealed class HealingAppliedEvent : IBattleDomainEvent
    {
        public BattleHealResult Result { get; set; }
    }

    public sealed class StatusAppliedEvent : IBattleDomainEvent
    {
        public StatusApplyResult Result { get; set; }
    }

    public sealed class AbilityActivatedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Owner { get; set; }
        public AbilityData Ability { get; set; }
    }

    public enum ReactionResolution
    {
        Reflected,
        StatusPenalty,
        Failed
    }

    public sealed class ReactionResolvedEvent : IBattleDomainEvent
    {
        public MonsterRuntime User { get; set; }
        public MonsterRuntime Opponent { get; set; }
        public SkillData Skill { get; set; }
        public ReactionResolution Resolution { get; set; }
        public BattleDamageResult DamageResult { get; set; }
    }

    public sealed class StoredDamageEffectStartedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Owner { get; set; }
        public SkillData Skill { get; set; }
        public int StorageTurns { get; set; }
    }

    public sealed class StoredDamageEffectReleasedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Owner { get; set; }
        public MonsterRuntime Target { get; set; }
        public BattleDamageResult DamageResult { get; set; }
    }

    public sealed class CaptureResolvedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Target { get; set; }
        public bool Succeeded { get; set; }
    }

    public sealed class CombatantFaintedEvent : IBattleDomainEvent
    {
        public MonsterRuntime Combatant { get; set; }
    }
}
