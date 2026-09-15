using NUnit.Framework;
using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class QuestCoordinationTests
    {
        /// <summary>
        /// 验证玩家获得道具后由应用层协调器推进对应的任务目标。
        /// </summary>
        [Test]
        public void ItemAddedEventAdvancesCollectObjective()
        {
            var item = ScriptableObject.CreateInstance<TestItemData>();
            var objective = new QuestObjectiveDefinition();
            SetPrivateField(objective, "type", QuestObjectiveType.CollectItem);
            SetPrivateField(objective, "targetItem", item);
            SetPrivateField(objective, "requiredAmount", 2);

            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            SetPrivateField(quest, "questId", "collect-item-test");
            SetPrivateField(
                quest,
                "objectives",
                new List<QuestObjectiveDefinition> { objective });

            QuestApplicationCoordinator.Initialize();
            PlayerParty.ResetState();
            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
            QuestService.RegisterDefinitions(new List<QuestDefinition> { quest });

            Assert.That(QuestService.AcceptQuest(quest), Is.True);
            PlayerParty.AddItem(item, 2);
            Assert.That(
                QuestService.TryGetRuntimeData(
                    quest.QuestId,
                    out QuestRuntimeData runtimeData),
                Is.True);
            Assert.That(runtimeData.objectiveProgress[0], Is.EqualTo(2));
            Assert.That(runtimeData.state, Is.EqualTo(QuestState.ReadyToSubmit));

            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
            PlayerParty.ResetState();
            Object.DestroyImmediate(quest);
            Object.DestroyImmediate(item);
        }

        /// <summary>
        /// 验证任务完成事件由应用层协调器转换为玩家奖励。
        /// </summary>
        [Test]
        public void QuestCompletedEventGrantsConfiguredReward()
        {
            var reward = new QuestRewardDefinition();
            SetPrivateField(reward, "type", QuestRewardType.Money);
            SetPrivateField(reward, "amount", 25);

            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            SetPrivateField(quest, "questId", "reward-test");
            SetPrivateField(
                quest,
                "rewards",
                new List<QuestRewardDefinition> { reward });

            QuestApplicationCoordinator.Initialize();
            PlayerParty.ResetState();
            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
            QuestService.RegisterDefinitions(new List<QuestDefinition> { quest });

            Assert.That(QuestService.AcceptQuest(quest), Is.True);
            Assert.That(QuestService.SubmitQuest(quest), Is.True);
            Assert.That(PlayerParty.Money, Is.EqualTo(25));

            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
            PlayerParty.ResetState();
            Object.DestroyImmediate(quest);
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

    public sealed class TestItemData : ItemData
    {
    }
}
