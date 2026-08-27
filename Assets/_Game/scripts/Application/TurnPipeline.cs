using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 定义所有回合行动都必须提供的排序与阵营信息。
    /// </summary>
    public interface ITurnAction
    {
        MonsterRuntime Actor { get; }
        bool IsPlayerAction { get; }
        int Priority { get; }
        int Speed { get; }
    }

    /// <summary>
    /// Compares two actions without coupling the turn phase to a concrete
    /// priority rule. A positive result means the first action goes first.
    /// </summary>
    public interface ITurnActionOrderComparer
    {
        int Compare(ITurnAction first, ITurnAction second);
    }

    /// <summary>
    /// Default Pokemon-style ordering: priority, then speed.
    /// A custom comparer can be supplied later for abilities, items, or modes.
    /// </summary>
    public sealed class PrioritySpeedActionOrderComparer : ITurnActionOrderComparer
    {
        public int Compare(ITurnAction first, ITurnAction second)
        {
            if (first == null) return second == null ? 0 : -1;
            if (second == null) return 1;

            int priorityComparison = first.Priority.CompareTo(second.Priority);
            if (priorityComparison != 0) return priorityComparison;

            return first.Speed.CompareTo(second.Speed);
        }
    }

    /// <summary>
    /// 描述本回合中一方已经选择的技能行动。
    /// 后续加入换人、道具等行动时，可以在同一层增加新的行动类型。
    /// </summary>
    public sealed class SkillTurnAction : ITurnAction
    {
        public MonsterRuntime Actor { get; }
        public MonsterRuntime Target { get; }
        public SkillData Skill { get; }
        public bool IsPlayerAction { get; }
        public int Priority => Skill != null ? Skill.Priority : 0;
        public int Speed => Actor.Speed;

        /// <summary>
        /// 创建一次技能行动的数据对象，不在构造时执行任何战斗规则。
        /// </summary>
        /// <param name="actor">本次行动的技能使用者。</param>
        /// <param name="target">本次行动的技能目标。</param>
        /// <param name="skill">使用的技能；为空时表示没有可执行技能。</param>
        /// <param name="isPlayerAction">该行动是否属于玩家阵营。</param>
        public SkillTurnAction(
            MonsterRuntime actor,
            MonsterRuntime target,
            SkillData skill,
            bool isPlayerAction)
        {
            Actor = actor;
            Target = target;
            Skill = skill;
            IsPlayerAction = isPlayerAction;
        }
    }

    /// <summary>
    /// 保存一整个回合在各阶段之间共享的数据。
    /// </summary>
    public sealed class TurnContext
    {
        public MonsterRuntime Player { get; }
        public MonsterRuntime Enemy { get; }
        public ITurnAction PlayerAction { get; }
        public ITurnAction EnemyAction { get; }
        public List<ITurnAction> OrderedActions { get; } = new List<ITurnAction>();
        public List<TurnStep> Steps { get; } = new List<TurnStep>();
        public bool IsBattleEnded { get; set; }

        /// <summary>
        /// 创建本回合的共享上下文。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="playerAction">玩家本回合的技能行动；单行动流程中可以为空。</param>
        /// <param name="enemyAction">敌方本回合的技能行动；单行动流程中可以为空。</param>
        public TurnContext(
            MonsterRuntime player,
            MonsterRuntime enemy,
            ITurnAction playerAction = null,
            ITurnAction enemyAction = null)
        {
            Player = player;
            Enemy = enemy;
            PlayerAction = playerAction;
            EnemyAction = enemyAction;
        }

        /// <summary>
        /// 添加一个带有双方当前 HP 快照的播放步骤。
        /// </summary>
        /// <param name="message">战斗日志中显示的文字。</param>
        /// <param name="animType">该步骤需要播放的动画类型。</param>
        public void AddStep(string message, StepAnimType animType = StepAnimType.None)
        {
            Steps.Add(new TurnStep
            {
                Message = message,
                PlayerHpAfter = Player.CurrentHP,
                EnemyHpAfter = Enemy.CurrentHP,
                AnimType = animType
            });
        }

        /// <summary>
        /// 添加技能施放步骤，并记录表现层选择攻击动画所需的技能分类。
        /// </summary>
        /// <param name="message">战斗日志中显示的文字。</param>
        /// <param name="animType">该步骤需要播放的攻击方动画。</param>
        /// <param name="skillCategory">技能对应的物理、特殊或状态分类。</param>
        public void AddSkillStep(
            string message,
            StepAnimType animType,
            SkillCategory skillCategory)
        {
            Steps.Add(new TurnStep
            {
                Message = message,
                PlayerHpAfter = Player.CurrentHP,
                EnemyHpAfter = Enemy.CurrentHP,
                AnimType = animType,
                SkillCategory = skillCategory
            });
        }
    }

    /// <summary>
    /// 定义某种回合行动对应的执行处理器。
    /// </summary>
    public interface ITurnActionHandler
    {
        /// <summary>
        /// 判断当前处理器是否支持指定行动类型。
        /// </summary>
        /// <param name="action">需要判断的回合行动。</param>
        /// <returns>支持该行动时返回 true。</returns>
        bool CanHandle(ITurnAction action);

        /// <summary>
        /// 执行指定行动并把结果写入当前回合。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <param name="action">需要执行的回合行动。</param>
        void Execute(TurnContext context, ITurnAction action);
    }

    /// <summary>
    /// 根据行动类型寻找对应处理器，使管线无需了解技能、换人等具体实现。
    /// </summary>
    public sealed class TurnActionDispatcher
    {
        private readonly IReadOnlyList<ITurnActionHandler> _handlers;

        /// <summary>
        /// 创建行动分发器。
        /// </summary>
        /// <param name="handlers">已经注册的行动类型处理器。</param>
        public TurnActionDispatcher(IReadOnlyList<ITurnActionHandler> handlers)
        {
            _handlers = handlers;
        }

        /// <summary>
        /// 查找支持当前行动的处理器并执行该行动。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <param name="action">需要分发的回合行动。</param>
        /// <exception cref="System.InvalidOperationException">没有处理器支持该行动类型时抛出。</exception>
        public void Execute(TurnContext context, ITurnAction action)
        {
            foreach (ITurnActionHandler handler in _handlers)
            {
                if (!handler.CanHandle(action)) continue;

                handler.Execute(context, action);
                return;
            }

            throw new System.InvalidOperationException(
                $"没有注册可执行 {action.GetType().Name} 的行动处理器。");
        }
    }

    /// <summary>
    /// 定义回合管线中的一个执行阶段。
    /// </summary>
    public interface ITurnPhase
    {
        /// <summary>
        /// 执行当前阶段，并通过共享上下文读取或写入本回合数据。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        void Execute(TurnContext context);
    }

    /// <summary>
    /// 按固定顺序运行回合阶段，并在战斗结束时停止后续阶段。
    /// </summary>
    public sealed class TurnPipeline
    {
        private readonly IReadOnlyList<ITurnPhase> _phases;

        /// <summary>
        /// 创建一条固定阶段顺序的回合管线。
        /// </summary>
        /// <param name="phases">需要按顺序执行的阶段列表。</param>
        public TurnPipeline(IReadOnlyList<ITurnPhase> phases)
        {
            _phases = phases;
        }

        /// <summary>
        /// 从第一个阶段开始执行，直到全部完成或战斗结束。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void Execute(TurnContext context)
        {
            foreach (ITurnPhase phase in _phases)
            {
                if (context.IsBattleEnded) break;
                phase.Execute(context);
            }
        }
    }
}
