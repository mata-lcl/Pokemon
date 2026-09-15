using NUnit.Framework;
using Pokemon.Domain;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class QuestProgressionTests
    {
        /// <summary>
        /// 验证多目标任务只接受匹配事件、限制最大进度并在全部完成后可提交。
        /// </summary>
        [Test]
        public void MatchingEventsAdvanceAndCapMultipleObjectives()
        {
            PokemonSpeciesData targetSpecies =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            PokemonSpeciesData otherSpecies =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            QuestDefinition quest = ScriptableObject.CreateInstance<QuestDefinition>();
            QuestObjectiveDefinition defeatObjective = CreateObjective(
                QuestObjectiveType.DefeatPokemon,
                targetSpecies,
                null,
                null,
                2);
            QuestObjectiveDefinition talkObjective = CreateObjective(
                QuestObjectiveType.TalkToNpc,
                null,
                null,
                "npc-test",
                1);
            SetPrivateField(quest, "questId", "multi-objective-test");
            SetPrivateField(
                quest,
                "objectives",
                new List<QuestObjectiveDefinition>
                {
                    defeatObjective,
                    talkObjective
                });

            ResetQuestState();
            try
            {
                QuestService.RegisterDefinitions(
                    new List<QuestDefinition> { quest });
                Assert.That(QuestService.AcceptQuest(quest), Is.True);

                QuestService.ReportPokemonDefeated(otherSpecies, 1);
                QuestService.ReportPokemonDefeated(targetSpecies, 5);
                QuestService.ReportNpcTalked("other-npc");
                Assert.That(
                    QuestService.TryGetRuntimeData(
                        quest.QuestId,
                        out QuestRuntimeData runtimeData),
                    Is.True);
                Assert.That(runtimeData.objectiveProgress[0], Is.EqualTo(2));
                Assert.That(runtimeData.objectiveProgress[1], Is.EqualTo(0));
                Assert.That(runtimeData.state, Is.EqualTo(QuestState.Active));

                QuestService.ReportNpcTalked("npc-test");
                Assert.That(runtimeData.objectiveProgress[1], Is.EqualTo(1));
                Assert.That(runtimeData.state, Is.EqualTo(QuestState.ReadyToSubmit));
            }
            finally
            {
                ResetQuestState();
                Object.DestroyImmediate(quest);
                Object.DestroyImmediate(targetSpecies);
                Object.DestroyImmediate(otherSpecies);
            }
        }

        /// <summary>
        /// 验证完成前置任务会解锁后续任务，且已完成任务不能重复提交。
        /// </summary>
        [Test]
        public void CompletedPrerequisiteUnlocksNextQuestOnce()
        {
            QuestDefinition prerequisite =
                ScriptableObject.CreateInstance<QuestDefinition>();
            QuestDefinition nextQuest =
                ScriptableObject.CreateInstance<QuestDefinition>();
            SetPrivateField(prerequisite, "questId", "prerequisite-test");
            SetPrivateField(nextQuest, "questId", "next-quest-test");
            SetPrivateField(
                nextQuest,
                "prerequisites",
                new List<QuestDefinition> { prerequisite });

            ResetQuestState();
            try
            {
                QuestService.RegisterDefinitions(new List<QuestDefinition>
                {
                    prerequisite,
                    nextQuest
                });
                QuestService.TryGetRuntimeData(
                    nextQuest.QuestId,
                    out QuestRuntimeData nextRuntimeData);
                Assert.That(nextRuntimeData.state, Is.EqualTo(QuestState.Locked));

                Assert.That(QuestService.AcceptQuest(prerequisite), Is.True);
                Assert.That(QuestService.SubmitQuest(prerequisite), Is.True);
                Assert.That(nextRuntimeData.state, Is.EqualTo(QuestState.Available));
                Assert.That(QuestService.SubmitQuest(prerequisite), Is.False);
            }
            finally
            {
                ResetQuestState();
                Object.DestroyImmediate(prerequisite);
                Object.DestroyImmediate(nextQuest);
            }
        }

        /// <summary>
        /// 创建具有指定匹配条件的任务目标。
        /// </summary>
        /// <param name="type">任务目标类型。</param>
        /// <param name="species">目标精灵种类。</param>
        /// <param name="item">目标道具。</param>
        /// <param name="npcId">目标 NPC 编号。</param>
        /// <param name="requiredAmount">目标要求数量。</param>
        private static QuestObjectiveDefinition CreateObjective(
            QuestObjectiveType type,
            PokemonSpeciesData species,
            ItemData item,
            string npcId,
            int requiredAmount)
        {
            var objective = new QuestObjectiveDefinition();
            SetPrivateField(objective, "type", type);
            SetPrivateField(objective, "targetPokemon", species);
            SetPrivateField(objective, "targetItem", item);
            SetPrivateField(objective, "targetNpcId", npcId);
            SetPrivateField(objective, "requiredAmount", requiredAmount);
            return objective;
        }

        /// <summary>
        /// 清除任务配置和运行进度，隔离各项测试。
        /// </summary>
        private static void ResetQuestState()
        {
            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
        }

        /// <summary>
        /// 为测试对象设置指定的私有序列化字段。
        /// </summary>
        /// <typeparam name="TTarget">包含目标字段的对象类型。</typeparam>
        /// <typeparam name="TValue">需要写入的字段值类型。</typeparam>
        /// <param name="target">需要设置字段的测试对象。</param>
        /// <param name="fieldName">私有字段名称。</param>
        /// <param name="value">需要写入的字段值。</param>
        private static void SetPrivateField<TTarget, TValue>(
            TTarget target,
            string fieldName,
            TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
