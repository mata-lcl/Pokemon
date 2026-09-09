using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 接收战斗状态变化与回合生命周期事件。
    /// </summary>
    public interface IBattleEventListener
    {
        /// <summary>
        /// 在伤害通过统一入口实际结算后接收结果。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        void OnDamageApplied(BattleDamageResult result);

        /// <summary>
        /// 在治疗通过统一入口实际结算后接收结果。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        void OnHealingApplied(BattleHealResult result);

        /// <summary>
        /// 在异常状态通过统一入口尝试附加后接收结果。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        void OnStatusApplied(StatusApplyResult result);

        /// <summary>
        /// 在一个完整战斗回合开始时接收通知。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        void OnTurnStarted(TurnContext context);

        /// <summary>
        /// 在一个完整战斗回合结束时接收通知。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        void OnTurnEnded(TurnContext context);
    }

    /// <summary>
    /// 为只关心部分事件的监听者提供空实现。
    /// </summary>
    public abstract class BattleEventListener : IBattleEventListener
    {
        /// <summary>
        /// 在伤害结算后处理事件，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        public virtual void OnDamageApplied(BattleDamageResult result)
        {
        }

        /// <summary>
        /// 在治疗结算后处理事件，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        public virtual void OnHealingApplied(BattleHealResult result)
        {
        }

        /// <summary>
        /// 在异常状态附加后处理事件，默认不执行逻辑。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        public virtual void OnStatusApplied(StatusApplyResult result)
        {
        }

        /// <summary>
        /// 在回合开始时处理事件，默认不执行逻辑。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public virtual void OnTurnStarted(TurnContext context)
        {
        }

        /// <summary>
        /// 在回合结束时处理事件，默认不执行逻辑。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public virtual void OnTurnEnded(TurnContext context)
        {
        }
    }

    /// <summary>
    /// 对外开放的战斗事件订阅入口，不允许订阅者主动伪造事件。
    /// </summary>
    public interface IBattleEventSource
    {
        /// <summary>
        /// 注册一个战斗事件监听者。
        /// </summary>
        /// <param name="listener">需要接收后续事件的监听者。</param>
        void AddListener(IBattleEventListener listener);

        /// <summary>
        /// 移除一个战斗事件监听者。
        /// </summary>
        /// <param name="listener">不再接收事件的监听者。</param>
        void RemoveListener(IBattleEventListener listener);

    }

    /// <summary>
    /// 由战斗流程内部使用的事件发布入口。
    /// </summary>
    public interface IBattleEventDispatcher : IBattleEventSource
    {

        /// <summary>
        /// 发布一次已经完成的伤害结算。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        void PublishDamageApplied(BattleDamageResult result);

        /// <summary>
        /// 发布一次已经完成的治疗结算。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        void PublishHealingApplied(BattleHealResult result);

        /// <summary>
        /// 发布一次已经完成的异常状态附加尝试。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        void PublishStatusApplied(StatusApplyResult result);

        /// <summary>
        /// 发布完整战斗回合开始事件。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        void PublishTurnStarted(TurnContext context);

        /// <summary>
        /// 发布完整战斗回合结束事件。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        void PublishTurnEnded(TurnContext context);
    }

    public sealed class BattleEventDispatcher : IBattleEventDispatcher
    {
        private readonly List<IBattleEventListener> _listeners =
            new List<IBattleEventListener>();

        /// <summary>
        /// 注册一个战斗事件监听者，已注册的实例不会重复添加。
        /// </summary>
        /// <param name="listener">需要接收后续事件的监听者。</param>
        public void AddListener(IBattleEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        /// <summary>
        /// 移除一个战斗事件监听者。
        /// </summary>
        /// <param name="listener">不再接收事件的监听者。</param>
        public void RemoveListener(IBattleEventListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// 向当前监听者发布一次已经完成的伤害结算。
        /// </summary>
        /// <param name="result">本次伤害的实际结算结果。</param>
        public void PublishDamageApplied(BattleDamageResult result)
        {
            foreach (IBattleEventListener listener in _listeners.ToArray())
                listener.OnDamageApplied(result);
        }

        /// <summary>
        /// 向当前监听者发布一次已经完成的治疗结算。
        /// </summary>
        /// <param name="result">本次治疗的实际结算结果。</param>
        public void PublishHealingApplied(BattleHealResult result)
        {
            foreach (IBattleEventListener listener in _listeners.ToArray())
                listener.OnHealingApplied(result);
        }

        /// <summary>
        /// 向当前监听者发布一次已经完成的异常状态附加尝试。
        /// </summary>
        /// <param name="result">本次异常状态附加结果。</param>
        public void PublishStatusApplied(StatusApplyResult result)
        {
            foreach (IBattleEventListener listener in _listeners.ToArray())
                listener.OnStatusApplied(result);
        }

        /// <summary>
        /// 向当前监听者发布完整战斗回合开始事件。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void PublishTurnStarted(TurnContext context)
        {
            foreach (IBattleEventListener listener in _listeners.ToArray())
                listener.OnTurnStarted(context);
        }

        /// <summary>
        /// 向当前监听者发布完整战斗回合结束事件。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void PublishTurnEnded(TurnContext context)
        {
            foreach (IBattleEventListener listener in _listeners.ToArray())
                listener.OnTurnEnded(context);
        }
    }
}
