using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public enum BattleConclusion
    {
        Ongoing,
        PlayerVictory,
        PlayerDefeat,
        Captured
    }

    public readonly struct BattleStateSnapshot
    {
        public int PlayerHP { get; }
        public int EnemyHP { get; }

        /// <summary>
        /// 创建一个战斗双方的生命值快照。
        /// </summary>
        /// <param name="playerHP">快照时玩家宝可梦的生命值。</param>
        /// <param name="enemyHP">快照时敌方宝可梦的生命值。</param>
        public BattleStateSnapshot(int playerHP, int enemyHP)
        {
            PlayerHP = playerHP;
            EnemyHP = enemyHP;
        }
    }

    public sealed class TurnExecutionResult
    {
        public BattleConclusion Conclusion { get; }
        public BattleStateSnapshot InitialState { get; }
        public IReadOnlyList<IBattleDomainEvent> Events { get; }

        /// <summary>
        /// 创建一次战斗执行的业务结果。
        /// </summary>
        /// <param name="conclusion">执行完成后的战斗结论。</param>
        /// <param name="initialState">执行开始前的战斗状态快照。</param>
        /// <param name="events">执行期间按顺序产生的领域事件。</param>
        public TurnExecutionResult(
            BattleConclusion conclusion,
            BattleStateSnapshot initialState,
            IReadOnlyList<IBattleDomainEvent> events)
        {
            Conclusion = conclusion;
            InitialState = initialState;
            Events = events;
        }

        /// <summary>
        /// 按执行顺序合并两次连续战斗执行的结果。
        /// </summary>
        /// <param name="first">先发生的执行结果。</param>
        /// <param name="second">后发生的执行结果。</param>
        /// <returns>保留初始快照、全部事件和最终战斗结论的合并结果。</returns>
        public static TurnExecutionResult Combine(
            TurnExecutionResult first,
            TurnExecutionResult second)
        {
            var events = new List<IBattleDomainEvent>(
                first.Events.Count + second.Events.Count);
            events.AddRange(first.Events);
            events.AddRange(second.Events);

            BattleConclusion conclusion = second.Conclusion != BattleConclusion.Ongoing
                ? second.Conclusion
                : first.Conclusion;
            return new TurnExecutionResult(
                conclusion,
                first.InitialState,
                events);
        }
    }

    public sealed class ItemUseExecutionResult
    {
        public bool Succeeded { get; }
        public bool IsConsumable { get; }
        public TurnExecutionResult BattleResult { get; }

        /// <summary>
        /// 创建一次战斗内道具使用结果。
        /// </summary>
        /// <param name="succeeded">道具是否满足条件并已经执行。</param>
        /// <param name="isConsumable">成功使用后是否需要消耗库存。</param>
        /// <param name="battleResult">道具执行产生的战斗结果和领域事件。</param>
        public ItemUseExecutionResult(
            bool succeeded,
            bool isConsumable,
            TurnExecutionResult battleResult)
        {
            Succeeded = succeeded;
            IsConsumable = isConsumable;
            BattleResult = battleResult;
        }
    }
}
