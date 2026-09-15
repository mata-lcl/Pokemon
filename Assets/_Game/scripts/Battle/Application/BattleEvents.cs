using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public interface IBattleEventSource
    {
        /// <summary>
        /// 注册一个战斗领域事件监听者。
        /// </summary>
        /// <param name="listener">需要接收后续事件的监听者。</param>
        void AddListener(IBattleDomainEventListener listener);

        /// <summary>
        /// 移除一个战斗领域事件监听者。
        /// </summary>
        /// <param name="listener">不再接收事件的监听者。</param>
        void RemoveListener(IBattleDomainEventListener listener);
    }

    public interface IBattleEventDispatcher :
        IBattleEventSource,
        IBattleDomainEventSink
    {
        int EventCount { get; }

        /// <summary>
        /// 返回指定位置之后按发生顺序记录的领域事件副本。
        /// </summary>
        /// <param name="startIndex">需要开始读取的事件下标。</param>
        /// <returns>本次执行范围内产生的领域事件。</returns>
        IReadOnlyList<IBattleDomainEvent> GetEventsSince(int startIndex);
    }

    public sealed class BattleEventDispatcher : IBattleEventDispatcher
    {
        private readonly List<IBattleDomainEventListener> _listeners =
            new List<IBattleDomainEventListener>();
        private readonly List<IBattleDomainEvent> _events =
            new List<IBattleDomainEvent>();

        public int EventCount => _events.Count;

        /// <summary>
        /// 注册一个战斗领域事件监听者，已注册的实例不会重复添加。
        /// </summary>
        /// <param name="listener">需要接收后续事件的监听者。</param>
        public void AddListener(IBattleDomainEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        /// <summary>
        /// 移除一个战斗领域事件监听者。
        /// </summary>
        /// <param name="listener">不再接收事件的监听者。</param>
        public void RemoveListener(IBattleDomainEventListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// 按发生顺序记录领域事件并同步通知当前监听者。
        /// </summary>
        /// <param name="domainEvent">需要记录和分发的领域事件。</param>
        public void Publish(IBattleDomainEvent domainEvent)
        {
            _events.Add(domainEvent);
            foreach (IBattleDomainEventListener listener in _listeners.ToArray())
                listener.OnEvent(domainEvent);
        }

        /// <summary>
        /// 返回指定位置之后按发生顺序记录的领域事件副本。
        /// </summary>
        /// <param name="startIndex">需要开始读取的事件下标。</param>
        /// <returns>本次执行范围内产生的领域事件。</returns>
        public IReadOnlyList<IBattleDomainEvent> GetEventsSince(int startIndex)
        {
            return _events.GetRange(startIndex, _events.Count - startIndex);
        }
    }
}
