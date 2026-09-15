using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public sealed class BattleRuntimeEffectController :
        IBattleDomainEventListener,
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
        /// 将战斗领域事件转发给关注对应结算类型的运行时效果。
        /// </summary>
        /// <param name="domainEvent">已经发生的战斗领域事件。</param>
        public void OnEvent(IBattleDomainEvent domainEvent)
        {
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (effect.IsExpired) continue;

                if (domainEvent is DamageAppliedEvent damageEvent)
                    effect.OnDamageApplied(damageEvent.Result);
                else if (domainEvent is HealingAppliedEvent healingEvent)
                    effect.OnHealingApplied(healingEvent.Result);
                else if (domainEvent is StatusAppliedEvent statusEvent)
                    effect.OnStatusApplied(statusEvent.Result);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 通知当前参战角色持有的运行时效果处理回合开始。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void OnTurnStarted(TurnContext context)
        {
            BattleRuntimeEffectContext runtimeContext = CreateContext(context);
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
        public void OnTurnEnded(TurnContext context)
        {
            BattleRuntimeEffectContext runtimeContext = CreateContext(context);
            foreach (BattleRuntimeEffect effect in _activeEffects.ToArray())
            {
                if (!effect.IsExpired && IsOwnerActive(effect, context))
                    effect.OnTurnEnded(runtimeContext);
            }

            RemoveExpiredEffects();
        }

        /// <summary>
        /// 从应用层回合上下文创建领域运行时效果上下文。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <returns>不包含应用层类型的领域上下文。</returns>
        private static BattleRuntimeEffectContext CreateContext(TurnContext context)
        {
            return new BattleRuntimeEffectContext(
                context.Player,
                context.Enemy,
                context.Operations,
                context.Events);
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
