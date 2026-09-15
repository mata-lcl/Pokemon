using NUnit.Framework;
using Pokemon.Application;
using Pokemon.Domain;
using Pokemon.Presentation;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class BattleLayeringTests
    {
        /// <summary>
        /// 验证统一伤害入口同时返回实际结果并发布对应领域事件。
        /// </summary>
        [Test]
        public void DealDamageReturnsResultAndPublishesDomainEvent()
        {
            MonsterRuntime attacker = CreateMonster("攻击方");
            MonsterRuntime target = CreateMonster("防御方");
            int previousHP = target.CurrentHP;
            var dispatcher = new BattleEventDispatcher();
            var operations = new BattleOperations(dispatcher);

            BattleDamageResult result = operations.DealDamage(
                new BattleDamageRequest
                {
                    Source = attacker,
                    Target = target,
                    Amount = 5,
                    Cause = BattleDamageCause.Skill
                });
            IReadOnlyList<IBattleDomainEvent> events =
                dispatcher.GetEventsSince(0);

            Assert.That(result.PreviousHP, Is.EqualTo(previousHP));
            Assert.That(result.CurrentHP, Is.EqualTo(target.CurrentHP));
            Assert.That(result.AppliedDamage, Is.EqualTo(5));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<DamageAppliedEvent>());
            Assert.That(
                ((DamageAppliedEvent)events[0]).Result,
                Is.SameAs(result));

            Object.DestroyImmediate(attacker.Species);
            Object.DestroyImmediate(target.Species);
        }

        /// <summary>
        /// 验证表现层根据领域事件生成攻击和命中帧步骤及正确的生命值快照。
        /// </summary>
        [Test]
        public void PlaybackBuilderMapsDamageEventToImpactStep()
        {
            MonsterRuntime player = CreateMonster("玩家");
            MonsterRuntime enemy = CreateMonster("敌方");
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.DisplayName = "测试技能";
            skill.Category = SkillCategory.Physical;
            int damageAfter = enemy.CurrentHP - 7;
            var damageResult = new BattleDamageResult
            {
                Source = player,
                Target = enemy,
                Skill = skill,
                AppliedDamage = 7,
                PreviousHP = enemy.CurrentHP,
                CurrentHP = damageAfter,
                Cause = BattleDamageCause.Skill
            };
            var events = new List<IBattleDomainEvent>
            {
                new SkillUseStartedEvent { Actor = player, Skill = skill },
                new DamageAppliedEvent { Result = damageResult }
            };
            var result = new TurnExecutionResult(
                BattleConclusion.Ongoing,
                new BattleStateSnapshot(player.CurrentHP, enemy.CurrentHP),
                events);

            List<BattlePlaybackStep> steps = new BattlePlaybackPlanBuilder()
                .Build(result, player, enemy);

            Assert.That(steps, Has.Count.EqualTo(2));
            Assert.That(steps[0].AnimType, Is.EqualTo(StepAnimType.PlayerAttack));
            Assert.That(steps[1].AnimType, Is.EqualTo(StepAnimType.EnemyHit));
            Assert.That(steps[1].PresentAtAttackImpact, Is.True);
            Assert.That(steps[1].EnemyHpAfter, Is.EqualTo(damageAfter));

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(player.Species);
            Object.DestroyImmediate(enemy.Species);
        }

        /// <summary>
        /// 验证连续执行结果合并后保留事件顺序与最终战斗结论。
        /// </summary>
        [Test]
        public void CombinePreservesEventOrderAndFinalConclusion()
        {
            var firstEvent = new SkillMissedEvent();
            var secondEvent = new CombatantFaintedEvent();
            var first = new TurnExecutionResult(
                BattleConclusion.Ongoing,
                new BattleStateSnapshot(20, 20),
                new List<IBattleDomainEvent> { firstEvent });
            var second = new TurnExecutionResult(
                BattleConclusion.PlayerVictory,
                new BattleStateSnapshot(20, 0),
                new List<IBattleDomainEvent> { secondEvent });

            TurnExecutionResult combined = TurnExecutionResult.Combine(
                first,
                second);

            Assert.That(
                combined.Conclusion,
                Is.EqualTo(BattleConclusion.PlayerVictory));
            Assert.That(combined.Events, Has.Count.EqualTo(2));
            Assert.That(combined.Events[0], Is.SameAs(firstEvent));
            Assert.That(combined.Events[1], Is.SameAs(secondEvent));
            Assert.That(combined.InitialState.PlayerHP, Is.EqualTo(20));
        }

        /// <summary>
        /// 创建测试使用的最小宝可梦运行时对象。
        /// </summary>
        /// <param name="displayName">测试精灵的显示名称。</param>
        /// <returns>具有默认基础数值的宝可梦运行时对象。</returns>
        private static MonsterRuntime CreateMonster(string displayName)
        {
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            species.DisplayName = displayName;
            return new MonsterRuntime(species, 5);
        }
    }
}
