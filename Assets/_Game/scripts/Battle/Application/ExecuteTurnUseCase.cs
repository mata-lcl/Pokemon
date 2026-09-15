using Pokemon.Domain;
using System;
using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 战斗回合用例的对外入口，负责创建上下文并启动固定回合管线。
    /// </summary>
    public sealed class ExecuteTurnUseCase
    {
        private readonly TurnPipeline _turnPipeline;
        private readonly SkillActionExecutor _skillActionExecutor;
        private readonly EndOfTurnResolver _endOfTurnResolver;
        private readonly BattleEndResolver _battleEndResolver;
        private readonly IBattleEventDispatcher _eventDispatcher;
        private readonly BattleRuntimeEffectController _runtimeEffectController;

        public IBattleOperations Operations { get; }
        public IBattleRuntimeEffectRegistry RuntimeEffects { get; }
        public IBattleEventSource Events => _eventDispatcher;

        /// <summary>
        /// 创建回合用例并组装当前玩法需要的排序、行动和回合末阶段。
        /// </summary>
        /// <param name="damageCalculator">负责命中判定和伤害数值计算的领域服务。</param>
        public ExecuteTurnUseCase(
            DamageCalculator damageCalculator,
            ITurnActionOrderComparer actionOrderComparer = null)
        {
            _eventDispatcher = new BattleEventDispatcher();
            _runtimeEffectController = new BattleRuntimeEffectController();
            _eventDispatcher.AddListener(_runtimeEffectController);
            Operations = new BattleOperations(_eventDispatcher);
            RuntimeEffects = _runtimeEffectController;
            _battleEndResolver = new BattleEndResolver();
            _skillActionExecutor = new SkillActionExecutor(damageCalculator);
            _endOfTurnResolver = new EndOfTurnResolver();
            var actionDispatcher = new TurnActionDispatcher(new ITurnActionHandler[]
            {
                new SkillTurnActionHandler(_skillActionExecutor)
            });

            var reactionTurnResolver = new ReactionTurnResolver(
                _skillActionExecutor,
                _battleEndResolver);

            _turnPipeline = new TurnPipeline(new ITurnPhase[]
            {
                new TurnStartedPhase(
                    _runtimeEffectController,
                    _battleEndResolver),
                new ActionOrderPhase(actionOrderComparer),
                new ActionExecutionPhase(
                    actionDispatcher,
                    reactionTurnResolver,
                    _battleEndResolver),
                new EndOfTurnPhase(
                    _endOfTurnResolver,
                    _runtimeEffectController,
                    _battleEndResolver)
            });
        }

        /// <summary>
        /// 执行玩家和敌人的完整技能回合，并返回业务结论与领域事件。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerSkill">玩家本回合选择的技能。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="enemySkill">敌方本回合选择的技能。</param>
        /// <returns>包含战斗结论、初始快照和有序领域事件的执行结果。</returns>
        public TurnExecutionResult Execute(
            MonsterRuntime player,
            SkillData playerSkill,
            MonsterRuntime enemy,
            SkillData enemySkill)
        {
            ValidateCombatants(player, enemy);
            int eventStartIndex = _eventDispatcher.EventCount;
            var initialState = new BattleStateSnapshot(
                player.CurrentHP,
                enemy.CurrentHP);

            var playerAction = new SkillTurnAction(
                player, enemy, playerSkill, isPlayerAction: true);
            var enemyAction = new SkillTurnAction(
                enemy, player, enemySkill, isPlayerAction: false);
            var context = new TurnContext(
                player,
                enemy,
                Operations,
                RuntimeEffects,
                _eventDispatcher,
                playerAction,
                enemyAction);

            _turnPipeline.Execute(context);
            return CreateResult(context, initialState, eventStartIndex);
        }

        /// <summary>
        /// 只执行玩家或敌方的一次技能行动，不自动处理回合末状态。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="skill">本次单独执行的技能。</param>
        /// <param name="isPlayerAction">为 true 时由玩家行动，否则由敌方行动。</param>
        /// <returns>该次行动产生的业务结论与领域事件。</returns>
        public TurnExecutionResult ExecuteSingleAction(
            MonsterRuntime player,
            MonsterRuntime enemy,
            SkillData skill,
            bool isPlayerAction)
        {
            ValidateCombatants(player, enemy);
            int eventStartIndex = _eventDispatcher.EventCount;
            var initialState = new BattleStateSnapshot(
                player.CurrentHP,
                enemy.CurrentHP);

            var action = new SkillTurnAction(
                isPlayerAction ? player : enemy,
                isPlayerAction ? enemy : player,
                skill,
                isPlayerAction);
            var context = new TurnContext(
                player,
                enemy,
                Operations,
                RuntimeEffects,
                _eventDispatcher);

            _runtimeEffectController.OnTurnStarted(context);
            if (_battleEndResolver.TryResolve(context))
                return CreateResult(context, initialState, eventStartIndex);
            _skillActionExecutor.Execute(context, action);
            _battleEndResolver.TryResolve(context);
            return CreateResult(context, initialState, eventStartIndex);
        }

        /// <summary>
        /// 单独执行一次回合末状态结算，供道具等非完整技能回合复用。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <returns>中毒、灼伤及可能倒下产生的业务结论与领域事件。</returns>
        public TurnExecutionResult ExecuteEndOfTurn(
            MonsterRuntime player,
            MonsterRuntime enemy)
        {
            ValidateCombatants(player, enemy);
            int eventStartIndex = _eventDispatcher.EventCount;
            var initialState = new BattleStateSnapshot(
                player.CurrentHP,
                enemy.CurrentHP);

            var context = new TurnContext(
                player,
                enemy,
                Operations,
                RuntimeEffects,
                _eventDispatcher);
            _endOfTurnResolver.Resolve(context);
            if (_battleEndResolver.TryResolve(context))
                return CreateResult(context, initialState, eventStartIndex);

            _runtimeEffectController.OnTurnEnded(context);
            _battleEndResolver.TryResolve(context);
            return CreateResult(context, initialState, eventStartIndex);
        }

        /// <summary>
        /// 在统一战斗上下文中检查并执行一个玩家道具。
        /// </summary>
        /// <param name="usable">需要尝试使用的道具效果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <returns>道具是否成功使用及其产生的战斗结果。</returns>
        public ItemUseExecutionResult ExecuteItem(
            IUsable usable,
            MonsterRuntime player,
            MonsterRuntime enemy)
        {
            ValidateCombatants(player, enemy);
            int eventStartIndex = _eventDispatcher.EventCount;
            var initialState = new BattleStateSnapshot(
                player.CurrentHP,
                enemy.CurrentHP);
            var context = new EffectContext
            {
                User = player,
                Target = enemy,
                Operations = Operations,
                RuntimeEffects = RuntimeEffects,
                Events = _eventDispatcher
            };

            if (!usable.CanUse(context))
            {
                return new ItemUseExecutionResult(
                    false,
                    usable.IsConsumable,
                    new TurnExecutionResult(
                        BattleConclusion.Ongoing,
                        initialState,
                        _eventDispatcher.GetEventsSince(eventStartIndex)));
            }

            usable.OnUse(context);
            IReadOnlyList<IBattleDomainEvent> events =
                _eventDispatcher.GetEventsSince(eventStartIndex);
            BattleConclusion conclusion = HasSuccessfulCapture(events)
                ? BattleConclusion.Captured
                : BattleConclusion.Ongoing;
            return new ItemUseExecutionResult(
                true,
                usable.IsConsumable,
                new TurnExecutionResult(conclusion, initialState, events));
        }

        /// <summary>
        /// 将当前回合上下文转换成不包含 UI 播放信息的业务结果。
        /// </summary>
        /// <param name="context">已经执行完成的回合上下文。</param>
        /// <param name="initialState">执行开始前的双方生命值快照。</param>
        /// <param name="eventStartIndex">本次执行开始前的事件数量。</param>
        /// <returns>战斗结论、初始状态和本次领域事件。</returns>
        private TurnExecutionResult CreateResult(
            TurnContext context,
            BattleStateSnapshot initialState,
            int eventStartIndex)
        {
            return new TurnExecutionResult(
                context.Conclusion,
                initialState,
                _eventDispatcher.GetEventsSince(eventStartIndex));
        }

        /// <summary>
        /// 检查本次执行的事件中是否存在成功捕获结果。
        /// </summary>
        /// <param name="events">本次执行产生的领域事件。</param>
        /// <returns>存在捕获成功事件时返回 true。</returns>
        private static bool HasSuccessfulCapture(
            IReadOnlyList<IBattleDomainEvent> events)
        {
            foreach (IBattleDomainEvent domainEvent in events)
            {
                if (domainEvent is CaptureResolvedEvent captureEvent &&
                    captureEvent.Succeeded)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 确保所有回合入口都收到有效的双方运行时对象。
        /// </summary>
        /// <param name="player">需要验证的玩家宝可梦。</param>
        /// <param name="enemy">需要验证的敌方宝可梦。</param>
        /// <exception cref="ArgumentNullException">任意一方为空时抛出。</exception>
        private static void ValidateCombatants(
            MonsterRuntime player,
            MonsterRuntime enemy)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
        }
    }
}
