using Pokemon.Domain;
using System;
using System.Collections.Generic;

namespace Pokemon.Application
{
    /// <summary>
    /// 表示战斗过程中的一个可播放步骤，并保存该时刻双方 HP 快照。
    /// </summary>
    public struct TurnStep
    {
        public string Message;
        public int PlayerHpAfter;
        public int EnemyHpAfter;
        public bool IsBattleEnd;
        public bool PlayerWon;
        public bool CaughtSuccess;
        public StepAnimType AnimType;
        public SkillCategory? SkillCategory;
        public bool PresentAtAttackImpact;
    }

    /// <summary>
    /// 战斗回合用例的对外入口，负责创建上下文并启动固定回合管线。
    /// </summary>
    public sealed class ExecuteTurnUseCase
    {
        private readonly TurnPipeline _turnPipeline;
        private readonly SkillActionExecutor _skillActionExecutor;
        private readonly EndOfTurnResolver _endOfTurnResolver;
        private readonly BattleEndResolver _battleEndResolver;

        /// <summary>
        /// 创建回合用例并组装当前玩法需要的排序、行动和回合末阶段。
        /// </summary>
        /// <param name="damageCalculator">负责命中判定和伤害数值计算的领域服务。</param>
        public ExecuteTurnUseCase(
            DamageCalculator damageCalculator,
            ITurnActionOrderComparer actionOrderComparer = null)
        {
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
                new ActionOrderPhase(actionOrderComparer),
                new ActionExecutionPhase(
                    actionDispatcher,
                    reactionTurnResolver,
                    _battleEndResolver),
                new EndOfTurnPhase(
                    _endOfTurnResolver,
                    _battleEndResolver)
            });
        }

        /// <summary>
        /// 执行玩家和敌人的完整技能回合，并返回 UI 可以依次播放的步骤。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerSkill">玩家本回合选择的技能。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="enemySkill">敌方本回合选择的技能。</param>
        /// <returns>按实际发生顺序排列的回合播放步骤。</returns>
        public List<TurnStep> Execute(
            MonsterRuntime player,
            SkillData playerSkill,
            MonsterRuntime enemy,
            SkillData enemySkill)
        {
            ValidateCombatants(player, enemy);

            var playerAction = new SkillTurnAction(
                player, enemy, playerSkill, isPlayerAction: true);
            var enemyAction = new SkillTurnAction(
                enemy, player, enemySkill, isPlayerAction: false);
            var context = new TurnContext(
                player, enemy, playerAction, enemyAction);

            _turnPipeline.Execute(context);
            return context.Steps;
        }

        /// <summary>
        /// 只执行玩家或敌方的一次技能行动，不自动处理回合末状态。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <param name="skill">本次单独执行的技能。</param>
        /// <param name="isPlayerAction">为 true 时由玩家行动，否则由敌方行动。</param>
        /// <returns>该次行动产生的播放步骤。</returns>
        public List<TurnStep> ExecuteSingleAction(
            MonsterRuntime player,
            MonsterRuntime enemy,
            SkillData skill,
            bool isPlayerAction)
        {
            ValidateCombatants(player, enemy);

            var action = new SkillTurnAction(
                isPlayerAction ? player : enemy,
                isPlayerAction ? enemy : player,
                skill,
                isPlayerAction);
            var context = new TurnContext(player, enemy);

            _skillActionExecutor.Execute(context, action);
            _battleEndResolver.TryResolve(context);
            return context.Steps;
        }

        /// <summary>
        /// 单独执行一次回合末状态结算，供道具等非完整技能回合复用。
        /// </summary>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <returns>中毒、灼伤及可能倒下产生的播放步骤。</returns>
        public List<TurnStep> ExecuteEndOfTurn(
            MonsterRuntime player,
            MonsterRuntime enemy)
        {
            ValidateCombatants(player, enemy);

            var context = new TurnContext(player, enemy);
            _endOfTurnResolver.Resolve(context);
            _battleEndResolver.TryResolve(context);
            return context.Steps;
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
