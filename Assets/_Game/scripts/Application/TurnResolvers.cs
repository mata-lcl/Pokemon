using Pokemon.Domain;

namespace Pokemon.Application
{
    /// <summary>
    /// 检查双方倒下状态，并把战斗结束结果写入回合上下文。
    /// </summary>
    public sealed class BattleEndResolver
    {
        /// <summary>
        /// 检查双方是否有人倒下；若有则记录结束步骤并停止管线。
        /// </summary>
        /// <param name="context">包含双方运行状态和步骤列表的回合上下文。</param>
        /// <returns>战斗已经结束时返回 true，否则返回 false。</returns>
        public bool TryResolve(TurnContext context)
        {
            if (!context.Player.IsFainted && !context.Enemy.IsFainted)
                return false;

            context.Steps.Add(new TurnStep
            {
                Message = context.Player.IsFainted
                    ? $"{context.Player.Species.DisplayName} 倒下了..."
                    : $"{context.Enemy.Species.DisplayName} 倒下了！",
                PlayerHpAfter = context.Player.CurrentHP,
                EnemyHpAfter = context.Enemy.CurrentHP,
                IsBattleEnd = true,
                PlayerWon = context.Enemy.IsFainted,
                AnimType = StepAnimType.None
            });

            context.IsBattleEnded = true;
            return true;
        }
    }

    /// <summary>
    /// 执行一次普通技能行动，包括 PP、命中、伤害预计算和技能效果。
    /// </summary>
    public sealed class SkillActionExecutor
    {
        private readonly DamageCalculator _damageCalculator;

        /// <summary>
        /// 创建技能行动执行器。
        /// </summary>
        /// <param name="damageCalculator">负责命中判定和伤害数值计算的领域服务。</param>
        public SkillActionExecutor(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        /// <summary>
        /// 执行指定技能行动，并把产生的所有画面步骤追加到当前回合。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <param name="action">需要执行的技能行动。</param>
        /// <param name="incomingReaction">防御方准备好的伤害拦截效果；普通行动时为空。</param>
        /// <param name="reactionContext">反应效果需要的上下文；普通行动时为空。</param>
        public void Execute(
            TurnContext context,
            SkillTurnAction action,
            IReactionSkillEffect incomingReaction = null,
            SkillReactionContext reactionContext = null)
        {
            if (action.Skill == null) return;

            if (!action.Actor.TryConsumePP(action.Skill))
            {
                context.AddStep(
                    $"{action.Actor.Species.DisplayName} 的 {action.Skill.DisplayName} PP已耗尽！");
                return;
            }

            context.AddSkillStep(
                $"{action.Actor.Species.DisplayName} 使用了 {action.Skill.DisplayName}！",
                action.IsPlayerAction ? StepAnimType.PlayerAttack : StepAnimType.EnemyAttack,
                action.Skill.Category);

            if (!_damageCalculator.CheckHit(action.Skill))
            {
                context.AddStep($"{action.Skill.DisplayName} 未命中！");
                return;
            }

            DamageResult? damage = null;
            if (action.Skill.Category != SkillCategory.Status)
            {
                damage = _damageCalculator.CalculateDamage(
                    action.Actor,
                    action.Target,
                    action.Skill,
                    context.Steps);
            }

            var effectContext = new EffectContext
            {
                User = action.Actor,
                Target = action.Target,
                Skill = action.Skill,
                Damage = damage,
                Steps = context.Steps,
                IsPlayerAttacking = action.IsPlayerAction,
                IncomingReaction = incomingReaction,
                ReactionContext = reactionContext,
                PlayerRef = context.Player,
                EnemyRef = context.Enemy
            };

            foreach (ISkillEffect effect in action.Skill.GetEffects())
            {
                if (!effect.CanProcess(effectContext)) continue;

                effect.Execute(effectContext);
                if (action.Actor.IsFainted || action.Target.IsFainted) break;
            }

            CheckAbilityCrisisTrigger(
                action.Target,
                context.Steps,
                context.Player,
                context.Enemy);
        }

        /// <summary>
        /// 在技能效果结束后通知目标检查低血量等危机特性。
        /// </summary>
        /// <param name="target">可能触发特性的宝可梦。</param>
        /// <param name="steps">用于记录特性提示的回合步骤列表。</param>
        /// <param name="playerRef">始终指向玩家宝可梦的引用。</param>
        /// <param name="enemyRef">始终指向敌方宝可梦的引用。</param>
        private static void CheckAbilityCrisisTrigger(
            MonsterRuntime target,
            System.Collections.Generic.List<TurnStep> steps,
            MonsterRuntime playerRef,
            MonsterRuntime enemyRef)
        {
            target.ActiveAbility?.CheckAndProcessNotification(
                target, steps, playerRef, enemyRef);
        }
    }

    /// <summary>
    /// 把通用回合行动分发请求转换为现有的技能行动执行。
    /// </summary>
    public sealed class SkillTurnActionHandler : ITurnActionHandler
    {
        private readonly SkillActionExecutor _skillActionExecutor;

        /// <summary>
        /// 创建技能行动处理器。
        /// </summary>
        /// <param name="skillActionExecutor">实际执行技能规则的执行器。</param>
        public SkillTurnActionHandler(SkillActionExecutor skillActionExecutor)
        {
            _skillActionExecutor = skillActionExecutor;
        }

        /// <summary>
        /// 判断传入行动是否为技能行动。
        /// </summary>
        /// <param name="action">需要判断的通用回合行动。</param>
        /// <returns>行动是 SkillTurnAction 时返回 true。</returns>
        public bool CanHandle(ITurnAction action) => action is SkillTurnAction;

        /// <summary>
        /// 把通用行动转换为技能行动并交给技能执行器。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <param name="action">需要执行的技能行动。</param>
        public void Execute(TurnContext context, ITurnAction action)
        {
            _skillActionExecutor.Execute(context, (SkillTurnAction)action);
        }
    }

    /// <summary>
    /// 处理后手反应技能的通用时序，不包含听桥等具体技能规则。
    /// </summary>
    public sealed class ReactionTurnResolver
    {
        private readonly SkillActionExecutor _skillActionExecutor;
        private readonly BattleEndResolver _battleEndResolver;

        /// <summary>
        /// 创建反应技能处理器。
        /// </summary>
        /// <param name="skillActionExecutor">用于执行对方技能的行动执行器。</param>
        /// <param name="battleEndResolver">用于在反应前后检查倒下状态的处理器。</param>
        public ReactionTurnResolver(
            SkillActionExecutor skillActionExecutor,
            BattleEndResolver battleEndResolver)
        {
            _skillActionExecutor = skillActionExecutor;
            _battleEndResolver = battleEndResolver;
        }

        /// <summary>
        /// 从技能配置中寻找第一个实现反应接口的技能效果。
        /// </summary>
        /// <param name="skill">需要检查的后手技能。</param>
        /// <param name="reactionEffect">找到时返回对应的反应效果。</param>
        /// <returns>技能包含反应效果时返回 true，否则返回 false。</returns>
        public bool TryGetReactionEffect(
            SkillData skill,
            out IReactionSkillEffect reactionEffect)
        {
            if (skill != null)
            {
                foreach (ISkillEffect effect in skill.GetEffects())
                {
                    if (effect is IReactionSkillEffect reaction)
                    {
                        reactionEffect = reaction;
                        return true;
                    }
                }
            }

            reactionEffect = null;
            return false;
        }

        /// <summary>
        /// 准备后手反应技能、执行先手行动，并在行动后完成反应效果。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        /// <param name="reactionAction">后手选择的反应技能行动。</param>
        /// <param name="opponentAction">需要先执行的对方行动。</param>
        /// <param name="reactionEffect">后手技能配置的通用反应效果。</param>
        public void Execute(
            TurnContext context,
            SkillTurnAction reactionAction,
            SkillTurnAction opponentAction,
            IReactionSkillEffect reactionEffect)
        {
            bool canReact = reactionAction.Actor.TryConsumePP(reactionAction.Skill);
            var reactionContext = new SkillReactionContext
            {
                User = reactionAction.Actor,
                Opponent = opponentAction.Actor,
                Skill = reactionAction.Skill,
                OpponentSkill = opponentAction.Skill,
                Steps = context.Steps,
                PlayerRef = context.Player,
                EnemyRef = context.Enemy,
                IsUserPlayer = reactionAction.IsPlayerAction
            };

            if (opponentAction.Skill != null)
            {
                _skillActionExecutor.Execute(
                    context,
                    opponentAction,
                    canReact ? reactionEffect : null,
                    canReact ? reactionContext : null);
            }

            if (!canReact)
            {
                context.AddStep(
                    $"{reactionAction.Actor.Species.DisplayName} 的 {reactionAction.Skill.DisplayName} PP已耗尽！");
            }

            if (_battleEndResolver.TryResolve(context)) return;

            if (canReact)
            {
                reactionEffect.ResolveAfterOpponentAction(reactionContext);
            }

            _battleEndResolver.TryResolve(context);
        }
    }

    /// <summary>
    /// 处理一回合结束时的中毒、灼伤等持续状态伤害。
    /// </summary>
    public sealed class EndOfTurnResolver
    {
        /// <summary>
        /// 按玩家、敌人的顺序结算回合末状态；玩家倒下后停止敌方结算。
        /// </summary>
        /// <param name="context">当前回合的共享上下文。</param>
        public void Resolve(TurnContext context)
        {
            ProcessStatusDamage(context.Player, true, context);
            if (!context.Player.IsFainted)
            {
                ProcessStatusDamage(context.Enemy, false, context);
            }
        }

        /// <summary>
        /// 查询并应用指定宝可梦当前状态造成的回合末伤害。
        /// </summary>
        /// <param name="target">需要结算状态伤害的宝可梦。</param>
        /// <param name="isPlayer">目标是否属于玩家，用于选择受击动画。</param>
        /// <param name="context">当前回合的共享上下文。</param>
        private static void ProcessStatusDamage(
            MonsterRuntime target,
            bool isPlayer,
            TurnContext context)
        {
            if (target.IsFainted) return;

            if (!StatusMechanics.TryGetEndOfTurnDamage(
                    target.CurrentStatus,
                    target.MaxHP,
                    out int damage,
                    out string statusName))
            {
                return;
            }

            target.ApplyDamage(damage);
            context.AddStep(
                $"{target.Species.DisplayName} 因为{statusName}受到了 {damage} 点伤害！",
                isPlayer ? StepAnimType.PlayerHit : StepAnimType.EnemyHit);

            target.ActiveAbility?.CheckAndProcessNotification(
                target,
                context.Steps,
                context.Player,
                context.Enemy);
        }
    }
}
