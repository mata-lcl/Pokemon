using Pokemon.Domain;

namespace Pokemon.Application
{
    /// <summary>
    /// 根据技能优先级和速度确定双方本回合的行动顺序。
    /// </summary>
    public sealed class ActionOrderPhase : ITurnPhase
    {
        private readonly ITurnActionOrderComparer _orderComparer;

        public ActionOrderPhase(ITurnActionOrderComparer orderComparer = null)
        {
            _orderComparer = orderComparer ?? new PrioritySpeedActionOrderComparer();
        }

        /// <summary>
        /// 将玩家和敌方行动按“优先级、速度、玩家平速优先”排列。
        /// </summary>
        /// <param name="context">包含双方待执行行动的回合上下文。</param>
        public void Execute(TurnContext context)
        {
            context.OrderedActions.Clear();

            ITurnAction playerAction = context.PlayerAction;
            ITurnAction enemyAction = context.EnemyAction;
            // Keep ordering policy injectable so future action types can provide
            // their own priority rules without changing this phase.
            int comparison = _orderComparer.Compare(playerAction, enemyAction);
            bool playerFirst = comparison >= 0;

            context.OrderedActions.Add(playerFirst ? playerAction : enemyAction);
            context.OrderedActions.Add(playerFirst ? enemyAction : playerAction);
        }
    }

    /// <summary>
    /// 执行排序后的行动，并协调普通技能与后手反应技能的不同时间线。
    /// </summary>
    public sealed class ActionExecutionPhase : ITurnPhase
    {
        private readonly TurnActionDispatcher _actionDispatcher;
        private readonly ReactionTurnResolver _reactionTurnResolver;
        private readonly BattleEndResolver _battleEndResolver;

        /// <summary>
        /// 创建行动执行阶段。
        /// </summary>
        /// <param name="actionDispatcher">负责把不同行动交给对应处理器的分发器。</param>
        /// <param name="reactionTurnResolver">后手反应技能时序处理器。</param>
        /// <param name="battleEndResolver">每次行动后使用的胜负处理器。</param>
        public ActionExecutionPhase(
            TurnActionDispatcher actionDispatcher,
            ReactionTurnResolver reactionTurnResolver,
            BattleEndResolver battleEndResolver)
        {
            _actionDispatcher = actionDispatcher;
            _reactionTurnResolver = reactionTurnResolver;
            _battleEndResolver = battleEndResolver;
        }

        /// <summary>
        /// 执行本回合行动；后手带反应效果时走通用反应流程，否则依次执行双方技能。
        /// </summary>
        /// <param name="context">已经由排序阶段填充行动顺序的回合上下文。</param>
        public void Execute(TurnContext context)
        {
            if (context.OrderedActions.Count < 2) return;

            ITurnAction firstAction = context.OrderedActions[0];
            ITurnAction secondAction = context.OrderedActions[1];

            if (firstAction is SkillTurnAction firstSkillAction &&
                secondAction is SkillTurnAction secondSkillAction &&
                _reactionTurnResolver.TryGetReactionEffect(
                    secondSkillAction.Skill,
                    out IReactionSkillEffect reactionEffect))
            {
                _reactionTurnResolver.Execute(
                    context,
                    secondSkillAction,
                    firstSkillAction,
                    reactionEffect);
                return;
            }

            foreach (ITurnAction action in context.OrderedActions)
            {
                _actionDispatcher.Execute(context, action);
                if (_battleEndResolver.TryResolve(context)) return;
            }
        }
    }

    /// <summary>
    /// 在双方行动完成后处理状态伤害，并检查是否因此结束战斗。
    /// </summary>
    public sealed class EndOfTurnPhase : ITurnPhase
    {
        private readonly EndOfTurnResolver _endOfTurnResolver;
        private readonly BattleEndResolver _battleEndResolver;

        /// <summary>
        /// 创建回合末阶段。
        /// </summary>
        /// <param name="endOfTurnResolver">中毒、灼伤等持续效果处理器。</param>
        /// <param name="battleEndResolver">状态结算后的胜负处理器。</param>
        public EndOfTurnPhase(
            EndOfTurnResolver endOfTurnResolver,
            BattleEndResolver battleEndResolver)
        {
            _endOfTurnResolver = endOfTurnResolver;
            _battleEndResolver = battleEndResolver;
        }

        /// <summary>
        /// 执行回合末状态结算，并记录可能出现的倒下结果。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void Execute(TurnContext context)
        {
            _endOfTurnResolver.Resolve(context);
            _battleEndResolver.TryResolve(context);
        }
    }
}
