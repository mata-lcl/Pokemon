using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 对技能、道具开放的跨回合效果注册入口。
    /// </summary>
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

    /// <summary>
    /// 跨回合效果在生命周期节点使用的受限战斗上下文。
    /// </summary>
    public sealed class BattleRuntimeEffectContext
    {
        public MonsterRuntime Player { get; }
        public MonsterRuntime Enemy { get; }
        public IBattleOperations Operations { get; }
        public List<TurnStep> Steps { get; }

        /// <summary>
        /// 从回合共享数据创建跨回合效果上下文。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        internal BattleRuntimeEffectContext(TurnContext context)
        {
            Player = context.Player;
            Enemy = context.Enemy;
            Operations = context.Operations;
            Steps = context.Steps;
        }

        /// <summary>
        /// 添加一个带有双方当前生命值快照的播放步骤。
        /// </summary>
        /// <param name="message">战斗日志中显示的文字。</param>
        /// <param name="animType">该步骤需要播放的动画类型。</param>
        public void AddStep(
            string message,
            StepAnimType animType = StepAnimType.None)
        {
            Steps.Add(new TurnStep
            {
                Message = message,
                PlayerHpAfter = Player.CurrentHP,
                EnemyHpAfter = Enemy.CurrentHP,
                AnimType = animType
            });
        }
    }

    /// <summary>
    /// 保存某个复杂效果在本场战斗中的独立状态，并接收所需战斗事件。
    /// ScriptableObject 资产只负责创建实例，不应自行保存这些运行时数据。
    /// </summary>
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
        /// <param name="context">提供双方角色、播放步骤和统一操作入口的上下文。</param>
        public virtual void OnTurnStarted(BattleRuntimeEffectContext context)
        {
        }

        /// <summary>
        /// 在持有者参与的完整回合结束时处理效果。
        /// </summary>
        /// <param name="context">提供双方角色、播放步骤和统一操作入口的上下文。</param>
        public virtual void OnTurnEnded(BattleRuntimeEffectContext context)
        {
        }

        /// <summary>
        /// 标记当前运行时效果已经结束，由注册器在本次事件后移除。
        /// </summary>
        protected void Expire()
        {
            IsExpired = true;
        }
    }

    internal sealed class BattleRuntimeEffectController :
        BattleEventListener,
        IBattleRuntimeEffectRegistry
    {
        private readonly List<BattleRuntimeEffect> _activeEffects =
            new List<BattleRuntimeEffect>();

        public IReadOnlyList<BattleRuntimeEffect> ActiveEffects => _activeEffects;

        /// <summary>
        /// 注册一个包含独立运行状态的跨回合效果。
        /// </summary>
        /// <param name="effect">需要加入本场战斗的运行时效果实例。</param>
        public void Register(BattleRuntimeEffect effect)
        {
            _activeEffects.Add(effect);
        }

        /// <summary>
        /// 提前移除一个跨回合效果。
        /// </summary>
        /// <param name="effect">需要移除的运行时效果实例。</param>
        /// <returns>成功找到并移除时返回 true。</returns>
        public bool Remove(BattleRuntimeEffect effect)
        {
            return _activeEffects.Remove(effect);
        }

        /// <summary>
        /// 把伤害结果转发给本场战斗中的全部运行时效果。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        public override void OnDamageApplied(BattleDamageResult result)
        {
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired)
                    effect.OnDamageApplied(result);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 把治疗结果转发给本场战斗中的全部运行时效果。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        public override void OnHealingApplied(BattleHealResult result)
        {
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired)
                    effect.OnHealingApplied(result);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 把异常状态附加结果转发给本场战斗中的全部运行时效果。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        public override void OnStatusApplied(StatusApplyResult result)
        {
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired)
                    effect.OnStatusApplied(result);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 通知当前参战角色持有的运行时效果处理回合开始。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public override void OnTurnStarted(TurnContext context)
        {
            var runtimeContext = new BattleRuntimeEffectContext(context);
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired && IsOwnerActive(effect, context))
                    effect.OnTurnStarted(runtimeContext);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 通知当前参战角色持有的运行时效果处理回合结束。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public override void OnTurnEnded(TurnContext context)
        {
            var runtimeContext = new BattleRuntimeEffectContext(context);
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired && IsOwnerActive(effect, context))
                    effect.OnTurnEnded(runtimeContext);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 判断运行时效果持有者是否正在参与当前回合。
        /// </summary>
        /// <param name="effect">需要检查持有者的运行时效果。</param>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <returns>持有者是当前玩家或敌方出战角色时返回 true。</returns>
        private static bool IsOwnerActive(
            BattleRuntimeEffect effect,
            TurnContext context)
        {
            return effect.Owner == context.Player || effect.Owner == context.Enemy;
        }

        /// <summary>
        /// 清理已经标记结束的运行时效果。
        /// </summary>
        private void RemoveExpiredEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].IsExpired)
                    _activeEffects.RemoveAt(i);
            }
        }
    }
}
