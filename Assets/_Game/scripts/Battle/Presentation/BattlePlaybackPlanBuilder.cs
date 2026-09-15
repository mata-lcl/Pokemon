using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Presentation
{
    public sealed class BattlePlaybackPlanBuilder
    {
        /// <summary>
        /// 将应用层返回的领域事件转换成 UI 可以依次播放的步骤。
        /// </summary>
        /// <param name="result">本次战斗执行的业务结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="enemy">当前敌方出战宝可梦。</param>
        /// <returns>仅包含文字、生命值快照和动画提示的播放步骤。</returns>
        public List<BattlePlaybackStep> Build(
            TurnExecutionResult result,
            MonsterRuntime player,
            MonsterRuntime enemy)
        {
            int playerHP = result.InitialState.PlayerHP;
            int enemyHP = result.InitialState.EnemyHP;
            var steps = new List<BattlePlaybackStep>();

            foreach (IBattleDomainEvent domainEvent in result.Events)
            {
                if (domainEvent is SkillUseStartedEvent skillUse)
                {
                    steps.Add(CreateStep(
                        $"{skillUse.Actor.Species.DisplayName} 使用了 {skillUse.Skill.DisplayName}！",
                        playerHP,
                        enemyHP,
                        skillUse.IsReactionAnnouncement
                            ? StepAnimType.None
                            : GetAttackAnimType(skillUse.Actor, player),
                        skillUse.Skill.Category));
                }
                else if (domainEvent is SkillPpExhaustedEvent ppExhausted)
                {
                    steps.Add(CreateStep(
                        $"{ppExhausted.Actor.Species.DisplayName} 的 {ppExhausted.Skill.DisplayName} PP已耗尽！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is SkillMissedEvent missed)
                {
                    steps.Add(CreateStep(
                        $"{missed.Skill.DisplayName} 未命中！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is DamageAppliedEvent damageEvent)
                {
                    ApplyDamageSnapshot(
                        damageEvent.Result,
                        player,
                        ref playerHP,
                        ref enemyHP);
                    AddDamageStep(
                        steps,
                        damageEvent.Result,
                        player,
                        playerHP,
                        enemyHP);
                }
                else if (domainEvent is HealingAppliedEvent healingEvent)
                {
                    ApplyHealingSnapshot(
                        healingEvent.Result,
                        player,
                        ref playerHP,
                        ref enemyHP);
                    steps.Add(CreateStep(
                        $"{healingEvent.Result.Target.Species.DisplayName} 恢复了 {healingEvent.Result.AppliedHealing} 点生命值！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is StatusAppliedEvent statusEvent &&
                         statusEvent.Result.Applied)
                {
                    steps.Add(CreateStep(
                        $"{statusEvent.Result.Target.Species.DisplayName}{statusEvent.Result.CurrentStatus.ToChineseName()} 了！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is AbilityActivatedEvent abilityEvent)
                {
                    steps.Add(CreateStep(
                        $"{abilityEvent.Owner.Species.DisplayName} 的 {abilityEvent.Ability.AbilityName} 发动了！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is ReactionResolvedEvent reactionEvent)
                {
                    steps.Add(CreateReactionStep(
                        reactionEvent,
                        player,
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is StoredDamageEffectStartedEvent storedStarted)
                {
                    steps.Add(CreateStep(
                        $"{storedStarted.Owner.Species.DisplayName} 开始蓄积伤害，将在接下来的 {storedStarted.StorageTurns} 回合后释放！",
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is StoredDamageEffectReleasedEvent storedReleased)
                {
                    steps.Add(CreateStoredDamageReleaseStep(
                        storedReleased,
                        player,
                        playerHP,
                        enemyHP));
                }
                else if (domainEvent is CaptureResolvedEvent captureEvent)
                {
                    string message = captureEvent.Succeeded
                        ? $"成功捕捉了 {captureEvent.Target.Species.DisplayName}！"
                        : $"哎呀，{captureEvent.Target.Species.DisplayName} 挣脱了！";
                    steps.Add(CreateStep(message, playerHP, enemyHP));
                }
                else if (domainEvent is CombatantFaintedEvent faintedEvent)
                {
                    string suffix = faintedEvent.Combatant == player ? "..." : "！";
                    steps.Add(CreateStep(
                        $"{faintedEvent.Combatant.Species.DisplayName} 倒下了{suffix}",
                        playerHP,
                        enemyHP));
                }
            }

            return steps;
        }

        /// <summary>
        /// 根据伤害原因生成普通伤害、状态伤害或反伤播放步骤。
        /// </summary>
        /// <param name="steps">接收播放步骤的列表。</param>
        /// <param name="result">已经应用的伤害结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerHP">事件发生后的玩家生命值。</param>
        /// <param name="enemyHP">事件发生后的敌方生命值。</param>
        private static void AddDamageStep(
            List<BattlePlaybackStep> steps,
            BattleDamageResult result,
            MonsterRuntime player,
            int playerHP,
            int enemyHP)
        {
            if (result.Cause == BattleDamageCause.Reaction ||
                result.Cause == BattleDamageCause.RuntimeEffect)
            {
                return;
            }

            string message;
            if (result.Cause == BattleDamageCause.Status &&
                result.Origin is StatusCondition status)
            {
                message = $"{result.Target.Species.DisplayName} 因为{status.ToChineseName()}受到了 {result.AppliedDamage} 点伤害！";
            }
            else if (result.Cause == BattleDamageCause.Recoil)
            {
                message = $"{result.Target.Species.DisplayName} 受到了 {result.AppliedDamage} 点反伤";
            }
            else
            {
                message = $"{result.Source.Species.DisplayName} 造成了 {result.AppliedDamage} 点伤害";
            }

            steps.Add(CreateStep(
                message,
                playerHP,
                enemyHP,
                GetHitAnimType(result.Target, player),
                presentAtAttackImpact: result.Cause == BattleDamageCause.Skill));
        }

        /// <summary>
        /// 生成反应技能的成功、惩罚或失败播放步骤。
        /// </summary>
        /// <param name="reactionEvent">反应技能的领域结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerHP">事件发生后的玩家生命值。</param>
        /// <param name="enemyHP">事件发生后的敌方生命值。</param>
        /// <returns>对应的反应技能播放步骤。</returns>
        private static BattlePlaybackStep CreateReactionStep(
            ReactionResolvedEvent reactionEvent,
            MonsterRuntime player,
            int playerHP,
            int enemyHP)
        {
            if (reactionEvent.Resolution == ReactionResolution.Failed)
            {
                return CreateStep(
                    $"{reactionEvent.User.Species.DisplayName} 的 {reactionEvent.Skill.DisplayName} 失败了！",
                    playerHP,
                    enemyHP);
            }

            string message = reactionEvent.Resolution == ReactionResolution.Reflected
                ? $"{reactionEvent.User.Species.DisplayName} 将 {reactionEvent.DamageResult.AppliedDamage} 点伤害反弹了回去！"
                : $"面对变化技能，{reactionEvent.User.Species.DisplayName} 损失了 {reactionEvent.DamageResult.AppliedDamage} 点生命值！";
            return CreateStep(
                message,
                playerHP,
                enemyHP,
                GetHitAnimType(reactionEvent.DamageResult.Target, player));
        }

        /// <summary>
        /// 生成蓄积伤害到期后的播放步骤。
        /// </summary>
        /// <param name="releaseEvent">蓄积伤害的领域结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerHP">事件发生后的玩家生命值。</param>
        /// <param name="enemyHP">事件发生后的敌方生命值。</param>
        /// <returns>有伤害时带受击动画，否则仅带说明文字的播放步骤。</returns>
        private static BattlePlaybackStep CreateStoredDamageReleaseStep(
            StoredDamageEffectReleasedEvent releaseEvent,
            MonsterRuntime player,
            int playerHP,
            int enemyHP)
        {
            if (releaseEvent.DamageResult == null)
            {
                return CreateStep(
                    $"{releaseEvent.Owner.Species.DisplayName} 没有蓄积到可以释放的伤害。",
                    playerHP,
                    enemyHP);
            }

            return CreateStep(
                $"{releaseEvent.Owner.Species.DisplayName} 将蓄积的伤害释放，对 {releaseEvent.Target.Species.DisplayName} 造成了 {releaseEvent.DamageResult.AppliedDamage} 点伤害！",
                playerHP,
                enemyHP,
                GetHitAnimType(releaseEvent.Target, player));
        }

        /// <summary>
        /// 将伤害结果更新到播放计划维护的生命值快照。
        /// </summary>
        /// <param name="result">已经应用的伤害结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerHP">需要更新的玩家生命值。</param>
        /// <param name="enemyHP">需要更新的敌方生命值。</param>
        private static void ApplyDamageSnapshot(
            BattleDamageResult result,
            MonsterRuntime player,
            ref int playerHP,
            ref int enemyHP)
        {
            if (result.Target == player)
                playerHP = result.CurrentHP;
            else
                enemyHP = result.CurrentHP;
        }

        /// <summary>
        /// 将治疗结果更新到播放计划维护的生命值快照。
        /// </summary>
        /// <param name="result">已经应用的治疗结果。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <param name="playerHP">需要更新的玩家生命值。</param>
        /// <param name="enemyHP">需要更新的敌方生命值。</param>
        private static void ApplyHealingSnapshot(
            BattleHealResult result,
            MonsterRuntime player,
            ref int playerHP,
            ref int enemyHP)
        {
            if (result.Target == player)
                playerHP = result.CurrentHP;
            else
                enemyHP = result.CurrentHP;
        }

        /// <summary>
        /// 根据行动者阵营选择对应的技能施放动画。
        /// </summary>
        /// <param name="actor">技能行动者。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <returns>玩家或敌方的攻击动画类型。</returns>
        private static StepAnimType GetAttackAnimType(
            MonsterRuntime actor,
            MonsterRuntime player)
        {
            return actor == player
                ? StepAnimType.PlayerAttack
                : StepAnimType.EnemyAttack;
        }

        /// <summary>
        /// 根据承受结果的目标阵营选择对应的受击动画。
        /// </summary>
        /// <param name="target">承受结果的宝可梦。</param>
        /// <param name="player">当前玩家出战宝可梦。</param>
        /// <returns>玩家或敌方的受击动画类型。</returns>
        private static StepAnimType GetHitAnimType(
            MonsterRuntime target,
            MonsterRuntime player)
        {
            return target == player
                ? StepAnimType.PlayerHit
                : StepAnimType.EnemyHit;
        }

        /// <summary>
        /// 创建一个只包含表现信息的播放步骤。
        /// </summary>
        /// <param name="message">需要显示的战斗文字。</param>
        /// <param name="playerHP">该步骤对应的玩家生命值。</param>
        /// <param name="enemyHP">该步骤对应的敌方生命值。</param>
        /// <param name="animType">该步骤需要播放的动画。</param>
        /// <param name="skillCategory">技能施放动画使用的技能分类。</param>
        /// <param name="presentAtAttackImpact">是否在攻击命中帧展示。</param>
        /// <returns>可以交给战斗播放器的表现步骤。</returns>
        private static BattlePlaybackStep CreateStep(
            string message,
            int playerHP,
            int enemyHP,
            StepAnimType animType = StepAnimType.None,
            SkillCategory? skillCategory = null,
            bool presentAtAttackImpact = false)
        {
            return new BattlePlaybackStep
            {
                Message = message,
                PlayerHpAfter = playerHP,
                EnemyHpAfter = enemyHP,
                AnimType = animType,
                SkillCategory = skillCategory,
                PresentAtAttackImpact = presentAtAttackImpact
            };
        }
    }
}
