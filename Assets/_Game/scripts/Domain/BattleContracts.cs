using System.Collections.Generic;

namespace Pokemon.Domain
{
    public enum BattleDamageCause
    {
        Skill,
        Status,
        Recoil,
        Reaction,
        RuntimeEffect,
        Field,
        Item
    }

    public enum BattleHealCause
    {
        Skill,
        Item,
        RuntimeEffect,
        Field
    }

    public enum BattleStatusCause
    {
        Skill,
        Item,
        RuntimeEffect,
        Field
    }

    public sealed class BattleDamageRequest
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public int Amount { get; set; }
        public BattleDamageCause Cause { get; set; }
        public object Origin { get; set; }
    }

    public sealed class BattleDamageResult
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public int RequestedAmount { get; set; }
        public BattleDamageCause Cause { get; set; }
        public object Origin { get; set; }
        public int PreviousHP { get; set; }
        public int CurrentHP { get; set; }
        public int AppliedDamage { get; set; }
    }

    public sealed class BattleHealRequest
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public int Amount { get; set; }
        public BattleHealCause Cause { get; set; }
        public object Origin { get; set; }
    }

    public sealed class BattleHealResult
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public int RequestedAmount { get; set; }
        public BattleHealCause Cause { get; set; }
        public object Origin { get; set; }
        public int PreviousHP { get; set; }
        public int CurrentHP { get; set; }
        public int AppliedHealing { get; set; }
    }

    public sealed class StatusApplyRequest
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public StatusCondition Status { get; set; }
        public BattleStatusCause Cause { get; set; }
        public object Origin { get; set; }
    }

    public sealed class StatusApplyResult
    {
        public MonsterRuntime Source { get; set; }
        public MonsterRuntime Target { get; set; }
        public SkillData Skill { get; set; }
        public StatusCondition RequestedStatus { get; set; }
        public BattleStatusCause Cause { get; set; }
        public object Origin { get; set; }
        public StatusCondition PreviousStatus { get; set; }
        public StatusCondition CurrentStatus { get; set; }
        public bool Applied { get; set; }
    }

    public interface IBattleOperations
    {
        /// <summary>
        /// 通过统一战斗入口对目标造成伤害并返回实际扣除结果。
        /// </summary>
        /// <param name="request">伤害来源、目标、数值和原因。</param>
        /// <returns>目标扣血前后状态及实际伤害。</returns>
        BattleDamageResult DealDamage(BattleDamageRequest request);

        /// <summary>
        /// 通过统一战斗入口治疗目标并返回实际恢复结果。
        /// </summary>
        /// <param name="request">治疗来源、目标、数值和原因。</param>
        /// <returns>目标治疗前后状态及实际恢复量。</returns>
        BattleHealResult Heal(BattleHealRequest request);

        /// <summary>
        /// 通过统一战斗入口为目标附加异常状态。
        /// </summary>
        /// <param name="request">状态来源、目标、状态类型和原因。</param>
        /// <returns>附加前后状态及是否成功。</returns>
        StatusApplyResult ApplyStatus(StatusApplyRequest request);
    }

    public interface IBattleDomainEvent
    {
    }

    public interface IBattleDomainEventSink
    {
        /// <summary>
        /// 发布一个已经发生的战斗领域事件。
        /// </summary>
        /// <param name="domainEvent">需要发布的领域事件。</param>
        void Publish(IBattleDomainEvent domainEvent);
    }

    public interface IBattleDomainEventListener
    {
        /// <summary>
        /// 接收一个已经发生的战斗领域事件。
        /// </summary>
        /// <param name="domainEvent">需要处理的领域事件。</param>
        void OnEvent(IBattleDomainEvent domainEvent);
    }

    public interface IBattleRuntimeEffectRegistry
    {
        IReadOnlyList<BattleRuntimeEffect> ActiveEffects { get; }

        /// <summary>
        /// 注册一个包含独立运行状态的跨回合效果。
        /// </summary>
        /// <param name="effect">需要加入本场战斗的运行时效果实例。</param>
        void Register(BattleRuntimeEffect effect);

        /// <summary>
        /// 提前移除一个跨回合效果。
        /// </summary>
        /// <param name="effect">需要移除的运行时效果实例。</param>
        /// <returns>成功找到并移除时返回 true。</returns>
        bool Remove(BattleRuntimeEffect effect);
    }

    public sealed class BattleRuntimeEffectContext
    {
        public MonsterRuntime Player { get; }
        public MonsterRuntime Enemy { get; }
        public IBattleOperations Operations { get; }
        public IBattleDomainEventSink Events { get; }

        /// <summary>
        /// 创建跨回合效果在生命周期节点使用的领域上下文。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="operations">统一战斗状态修改入口。</param>
        /// <param name="events">战斗领域事件发布入口。</param>
        public BattleRuntimeEffectContext(
            MonsterRuntime player,
            MonsterRuntime enemy,
            IBattleOperations operations,
            IBattleDomainEventSink events)
        {
            Player = player;
            Enemy = enemy;
            Operations = operations;
            Events = events;
        }
    }

    public abstract class BattleRuntimeEffect
    {
        public MonsterRuntime Owner { get; }
        public SkillData SourceSkill { get; }
        public object Origin { get; }
        public bool IsExpired { get; private set; }

        /// <summary>
        /// 创建一个归属于指定角色和技能的跨回合效果实例。
        /// </summary>
        /// <param name="owner">持有该运行时效果的角色。</param>
        /// <param name="sourceSkill">创建该效果的技能。</param>
        /// <param name="origin">创建该效果的具体资产或逻辑对象。</param>
        protected BattleRuntimeEffect(
            MonsterRuntime owner,
            SkillData sourceSkill,
            object origin)
        {
            Owner = owner;
            SourceSkill = sourceSkill;
            Origin = origin;
        }

        /// <summary>
        /// 接收统一入口已经完成的伤害结果，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        public virtual void OnDamageApplied(BattleDamageResult result)
        {
        }

        /// <summary>
        /// 接收统一入口已经完成的治疗结果，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        public virtual void OnHealingApplied(BattleHealResult result)
        {
        }

        /// <summary>
        /// 接收统一入口已经完成的异常状态附加结果，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        public virtual void OnStatusApplied(StatusApplyResult result)
        {
        }

        /// <summary>
        /// 在持有者参与的完整回合开始时处理效果。
        /// </summary>
        /// <param name="context">当前战斗的受限领域上下文。</param>
        public virtual void OnTurnStarted(BattleRuntimeEffectContext context)
        {
        }

        /// <summary>
        /// 在持有者参与的完整回合结束时处理效果。
        /// </summary>
        /// <param name="context">当前战斗的受限领域上下文。</param>
        public virtual void OnTurnEnded(BattleRuntimeEffectContext context)
        {
        }

        /// <summary>
        /// 标记当前运行时效果已经结束。
        /// </summary>
        protected void Expire()
        {
            IsExpired = true;
        }
    }
}
