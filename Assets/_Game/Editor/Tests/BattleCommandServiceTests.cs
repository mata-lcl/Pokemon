using NUnit.Framework;
using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class BattleCommandServiceTests
    {
        private readonly List<Object> _assets = new List<Object>();
        private BattleCommandService _service;

        /// <summary>
        /// 清空玩家状态并创建每项测试使用的应用层服务。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            PlayerParty.ResetState();
            _service = new BattleCommandService();
        }

        /// <summary>
        /// 清理测试创建的玩家状态和临时资源。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            PlayerParty.ResetState();
            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        /// <summary>
        /// 验证无效技能下标不会启动回合或消耗技能 PP。
        /// </summary>
        [Test]
        public void InvalidSkillIndexDoesNotExecuteTurn()
        {
            SkillData playerSkill = CreateSkill("player-skill", 3);
            BattleSession session = CreateSession(
                new[] { playerSkill },
                new[] { CreateSkill("enemy-skill", 3) });

            bool executed = _service.TryExecuteSkillTurn(
                session,
                -1,
                out TurnExecutionResult result);

            Assert.That(executed, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(session.Player.CurrentPP[playerSkill], Is.EqualTo(3));
        }

        /// <summary>
        /// 验证应用层换人会同时更新玩家当前出战精灵和战斗会话。
        /// </summary>
        [Test]
        public void SwitchingPokemonUpdatesPartyAndSession()
        {
            BattleSession session = CreateSession(
                new SkillData[0],
                new SkillData[0]);
            MonsterRuntime replacement = new MonsterRuntime(
                CreateSpecies("replacement"),
                6);
            PlayerParty.TryAddMonster(session.Player);
            PlayerParty.TryAddMonster(replacement);

            bool switched = _service.TrySwitchPokemon(
                session,
                replacement,
                out MonsterRuntime previousPokemon);

            Assert.That(switched, Is.True);
            Assert.That(previousPokemon, Is.Not.SameAs(replacement));
            Assert.That(PlayerParty.ActivePokemon, Is.SameAs(replacement));
            Assert.That(session.Player, Is.SameAs(replacement));
        }

        /// <summary>
        /// 验证成功使用消耗型战斗道具后只扣除一件库存。
        /// </summary>
        [Test]
        public void SuccessfulConsumableItemRemovesOneInventoryItem()
        {
            BattleSession session = CreateSession(
                new SkillData[0],
                new SkillData[0]);
            BattleCommandTestItemData item = CreateItem(canUse: true);
            PlayerParty.AddItem(item, 2);

            bool recognized = _service.TryExecuteItem(
                session,
                item,
                out ItemUseExecutionResult result);

            Assert.That(recognized, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(PlayerParty.GetItemCount(item), Is.EqualTo(1));
        }

        /// <summary>
        /// 验证不满足使用条件的消耗型道具不会减少库存。
        /// </summary>
        [Test]
        public void FailedItemUseDoesNotRemoveInventoryItem()
        {
            BattleSession session = CreateSession(
                new SkillData[0],
                new SkillData[0]);
            BattleCommandTestItemData item = CreateItem(canUse: false);
            PlayerParty.AddItem(item, 2);

            bool recognized = _service.TryExecuteItem(
                session,
                item,
                out ItemUseExecutionResult result);

            Assert.That(recognized, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(PlayerParty.GetItemCount(item), Is.EqualTo(2));
        }

        /// <summary>
        /// 验证战斗背包查询保留道具数量，且重排操作由应用层完成。
        /// </summary>
        [Test]
        public void InventorySnapshotAndReorderAreCoordinatedByApplication()
        {
            BattleCommandTestItemData firstItem = CreateItem(canUse: true);
            BattleCommandTestItemData secondItem = CreateItem(canUse: true);
            PlayerParty.AddItem(firstItem, 1);
            PlayerParty.AddItem(secondItem, 2);

            IReadOnlyList<InventoryItemStack> initialItems =
                _service.GetUsableItemSnapshot();
            bool reordered = _service.TryReorderItem(secondItem, firstItem);
            IReadOnlyList<InventoryItemStack> reorderedItems =
                _service.GetUsableItemSnapshot();

            Assert.That(initialItems[0].Item, Is.SameAs(firstItem));
            Assert.That(initialItems[1].Count, Is.EqualTo(2));
            Assert.That(reordered, Is.True);
            Assert.That(reorderedItems[0].Item, Is.SameAs(secondItem));
            Assert.That(reorderedItems[1].Item, Is.SameAs(firstItem));
        }

        /// <summary>
        /// 验证完成捕获会保存敌方精灵并标记战斗结束。
        /// </summary>
        [Test]
        public void CompletingCaptureStoresEnemyAndEndsBattle()
        {
            BattleSession session = CreateSession(
                new SkillData[0],
                new SkillData[0]);

            bool stored = _service.CompleteCapture(session);

            IReadOnlyList<MonsterRuntime> party =
                PlayerParty.GetPartySnapshot();
            Assert.That(stored, Is.True);
            Assert.That(session.BattleEnded, Is.True);
            Assert.That(party, Has.Count.EqualTo(1));
            Assert.That(party[0].Species, Is.SameAs(session.Enemy.Species));
            Assert.That(party[0].Level, Is.EqualTo(session.Enemy.Level));
        }

        /// <summary>
        /// 验证敌方回应会跳过零 PP 技能，并合并回合末状态伤害事件。
        /// </summary>
        [Test]
        public void EnemyResponseUsesFirstAvailableSkillAndIncludesEndOfTurn()
        {
            SkillData exhaustedSkill = CreateSkill("exhausted", 1);
            SkillData availableSkill = CreateSkill("available", 2);
            BattleSession session = CreateSession(
                new SkillData[0],
                new[] { exhaustedSkill, availableSkill });
            session.Enemy.TryConsumePP(exhaustedSkill);
            session.Player.TryApplyStatus(StatusCondition.Poison);
            int previousPlayerHP = session.Player.CurrentHP;

            bool executed = _service.TryExecuteEnemyResponse(
                session,
                out TurnExecutionResult result);

            SkillUseStartedEvent skillEvent = result.Events
                .OfType<SkillUseStartedEvent>()
                .Single();
            DamageAppliedEvent statusDamageEvent = result.Events
                .OfType<DamageAppliedEvent>()
                .Single(domainEvent =>
                    domainEvent.Result.Cause == BattleDamageCause.Status);
            Assert.That(executed, Is.True);
            Assert.That(skillEvent.Skill, Is.SameAs(availableSkill));
            Assert.That(session.Enemy.CurrentPP[availableSkill], Is.EqualTo(1));
            Assert.That(statusDamageEvent.Result.Target, Is.SameAs(session.Player));
            Assert.That(session.Player.CurrentHP, Is.LessThan(previousPlayerHP));
        }

        /// <summary>
        /// 验证当前直接成功的逃跑规则仅允许在未结束的战斗中执行。
        /// </summary>
        [Test]
        public void EscapeSucceedsOnlyWhileBattleIsActive()
        {
            BattleSession session = CreateSession(
                new SkillData[0],
                new SkillData[0]);

            Assert.That(_service.TryEscape(session), Is.True);
            session.BattleEnded = true;
            Assert.That(_service.TryEscape(session), Is.False);
        }

        /// <summary>
        /// 创建包含指定双方技能的最小战斗会话。
        /// </summary>
        /// <param name="playerSkills">玩家精灵的初始技能。</param>
        /// <param name="enemySkills">敌方精灵的初始技能。</param>
        /// <returns>使用独立回合用例的测试战斗会话。</returns>
        private BattleSession CreateSession(
            IReadOnlyList<SkillData> playerSkills,
            IReadOnlyList<SkillData> enemySkills)
        {
            PokemonSpeciesData playerSpecies = CreateSpecies(
                "player",
                playerSkills);
            PokemonSpeciesData enemySpecies = CreateSpecies(
                "enemy",
                enemySkills);
            return new BattleSession(
                new MonsterRuntime(playerSpecies, 5),
                new MonsterRuntime(enemySpecies, 5),
                new ExecuteTurnUseCase(new DamageCalculator(null)));
        }

        /// <summary>
        /// 创建测试使用的精灵种类并配置初始技能。
        /// </summary>
        /// <param name="assetName">测试资源名称。</param>
        /// <param name="skills">需要加入种类初始技能列表的技能。</param>
        /// <returns>已经登记为待清理资源的精灵种类。</returns>
        private PokemonSpeciesData CreateSpecies(
            string assetName,
            IReadOnlyList<SkillData> skills = null)
        {
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            species.name = assetName;
            species.DisplayName = assetName;
            if (skills != null)
            {
                foreach (SkillData skill in skills)
                    species.InitialSkills.Add(skill);
            }

            _assets.Add(species);
            return species;
        }

        /// <summary>
        /// 创建测试使用的状态技能。
        /// </summary>
        /// <param name="assetName">测试资源名称。</param>
        /// <param name="maxPP">技能初始 PP。</param>
        /// <returns>已经登记为待清理资源的技能。</returns>
        private SkillData CreateSkill(string assetName, int maxPP)
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            skill.name = assetName;
            skill.DisplayName = assetName;
            skill.Category = SkillCategory.Status;
            skill.Accuracy = 1f;
            skill.MaxPP = maxPP;
            _assets.Add(skill);
            return skill;
        }

        /// <summary>
        /// 创建具有指定使用条件的消耗型测试道具。
        /// </summary>
        /// <param name="canUse">道具本次是否允许使用。</param>
        /// <returns>已经登记为待清理资源的测试道具。</returns>
        private BattleCommandTestItemData CreateItem(bool canUse)
        {
            BattleCommandTestItemData item =
                ScriptableObject.CreateInstance<BattleCommandTestItemData>();
            item.CanUseResult = canUse;
            _assets.Add(item);
            return item;
        }
    }

    public sealed class BattleCommandTestItemData : ItemData, IUsable
    {
        public bool CanUseResult { get; set; }
        public bool IsConsumable => true;

        /// <summary>
        /// 返回测试配置的道具使用条件。
        /// </summary>
        /// <param name="context">当前战斗效果上下文。</param>
        /// <returns>测试预先设置的使用结果。</returns>
        public bool CanUse(EffectContext context)
        {
            return CanUseResult;
        }

        /// <summary>
        /// 执行不产生额外效果的测试道具。
        /// </summary>
        /// <param name="context">当前战斗效果上下文。</param>
        public void OnUse(EffectContext context)
        {
        }
    }
}
