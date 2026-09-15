using NUnit.Framework;
using Pokemon.Application;
using Pokemon.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class SaveGameRoundTripTests
    {
        /// <summary>
        /// 验证运行状态经过映射、临时文件写入和读取后能够完整恢复。
        /// </summary>
        [Test]
        public void MapperAndRepositoryPreserveRuntimeState()
        {
            string saveDirectory = Path.Combine(
                Path.GetTempPath(),
                "PokemonSaveTests",
                Guid.NewGuid().ToString("N"));
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            SkillData skill = ScriptableObject.CreateInstance<SkillData>();
            var item = ScriptableObject.CreateInstance<SaveTestItemData>();
            var catalogObject = new GameObject("SaveAssetCatalog-Test");
            SaveAssetCatalog catalog = catalogObject.AddComponent<SaveAssetCatalog>();

            ConfigureAssets(species, skill, item, catalog);
            ResetRuntimeState();
            try
            {
                var monster = new MonsterRuntime(species, 12);
                monster.ApplyDamage(3);
                monster.TryApplyStatus(StatusCondition.Poison);
                monster.AddEVs(1, 2, 3, 4, 5, 6);
                monster.HeldItem = item;
                monster.TryConsumePP(skill);
                int expectedHP = monster.CurrentHP;
                int expectedPP = monster.CurrentPP[skill];

                PlayerParty.TryAddMonster(monster);
                PlayerParty.AddItem(item, 4);
                PlayerParty.AddMoney(125);
                QuestService.RestoreState(new List<QuestRuntimeData>
                {
                    new QuestRuntimeData
                    {
                        questId = "save-quest",
                        state = QuestState.Active,
                        objectiveProgress = new List<int> { 2 }
                    }
                });

                var mapper = new SaveGameMapper();
                SaveGameData source = mapper.CreateSaveData(
                    1,
                    "2026-09-15 10:00:00",
                    "World",
                    new Vector3(3f, 4f, 0f));
                var repository = new SaveFileRepository(saveDirectory);
                repository.Write(source);

                Assert.That(repository.Exists(1), Is.True);
                Assert.That(repository.GetSlotSummary(1).PartyCount, Is.EqualTo(1));
                SaveGameData loaded = repository.Read(1);

                ResetRuntimeState();
                mapper.RestoreState(loaded, catalog);

                IReadOnlyList<MonsterRuntime> restoredParty =
                    PlayerParty.GetPartySnapshot();
                MonsterRuntime restoredMonster = restoredParty[0];
                Assert.That(restoredParty, Has.Count.EqualTo(1));
                Assert.That(restoredMonster.Species, Is.SameAs(species));
                Assert.That(restoredMonster.Level, Is.EqualTo(12));
                Assert.That(restoredMonster.CurrentHP, Is.EqualTo(expectedHP));
                Assert.That(restoredMonster.CurrentStatus, Is.EqualTo(StatusCondition.Poison));
                Assert.That(restoredMonster.EvSpecialDefense, Is.EqualTo(6));
                Assert.That(restoredMonster.CurrentPP[skill], Is.EqualTo(expectedPP));
                Assert.That(restoredMonster.HeldItem, Is.SameAs(item));
                Assert.That(PlayerParty.GetItemCount(item), Is.EqualTo(4));
                Assert.That(PlayerParty.Money, Is.EqualTo(125));
                Assert.That(
                    QuestService.TryGetRuntimeData(
                        "save-quest",
                        out QuestRuntimeData restoredQuest),
                    Is.True);
                Assert.That(restoredQuest.objectiveProgress[0], Is.EqualTo(2));
                Assert.That(loaded.sceneName, Is.EqualTo("World"));
                Assert.That(loaded.playerPosition, Is.EqualTo(new Vector3(3f, 4f, 0f)));
            }
            finally
            {
                ResetRuntimeState();
                Object.DestroyImmediate(catalogObject);
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(species);
                if (Directory.Exists(saveDirectory))
                    Directory.Delete(saveDirectory, true);
            }
        }

        /// <summary>
        /// 验证待读档位置只会被匹配的场景消费一次。
        /// </summary>
        [Test]
        public void PendingPlayerPositionIsConsumedOnceByMatchingScene()
        {
            Vector3 expectedPosition = new Vector3(8f, 9f, 0f);
            LoadedGameContext.ClearPendingPlayerPosition();
            LoadedGameContext.SetPendingPlayerPosition("World", expectedPosition);

            Assert.That(
                LoadedGameContext.TryConsumePlayerPosition("Battle", out _),
                Is.False);
            Assert.That(
                LoadedGameContext.TryConsumePlayerPosition(
                    "World",
                    out Vector3 restoredPosition),
                Is.True);
            Assert.That(restoredPosition, Is.EqualTo(expectedPosition));
            Assert.That(
                LoadedGameContext.TryConsumePlayerPosition("World", out _),
                Is.False);
        }

        /// <summary>
        /// 验证保存入口要求表现层显式提供当前场景名称。
        /// </summary>
        [Test]
        public void SaveContractRequiresSceneNameFromPresentation()
        {
            MethodInfo saveWithScene = typeof(SaveGameService).GetMethod(
                nameof(SaveGameService.Save),
                new[] { typeof(int), typeof(string), typeof(Vector3) });
            MethodInfo saveWithoutScene = typeof(SaveGameService).GetMethod(
                nameof(SaveGameService.Save),
                new[] { typeof(int), typeof(Vector3) });

            Assert.That(saveWithScene, Is.Not.Null);
            Assert.That(saveWithoutScene, Is.Null);
        }

        /// <summary>
        /// 配置存档往返测试所需的精灵、技能、道具和资源目录。
        /// </summary>
        /// <param name="species">测试精灵种类。</param>
        /// <param name="skill">测试技能。</param>
        /// <param name="item">测试道具。</param>
        /// <param name="catalog">用于读档解析的资源目录。</param>
        private static void ConfigureAssets(
            PokemonSpeciesData species,
            SkillData skill,
            ItemData item,
            SaveAssetCatalog catalog)
        {
            species.ID = 25;
            species.name = "save-species";
            skill.Id = "save-skill";
            skill.name = "save-skill-asset";
            skill.MaxPP = 10;
            item.Id = "save-item";
            item.name = "save-item-asset";
            species.InitialSkills.Add(skill);

            SetPrivateField(catalog, "species", new[] { species });
            SetPrivateField(catalog, "items", new[] { item });
            SetPrivateField(catalog, "skills", new[] { skill });
            SetPrivateField(catalog, "abilities", new AbilityData[0]);
        }

        /// <summary>
        /// 清除玩家和任务运行状态，隔离存档测试。
        /// </summary>
        private static void ResetRuntimeState()
        {
            PlayerParty.ResetState();
            QuestService.RegisterDefinitions(new List<QuestDefinition>());
            QuestService.ResetState();
        }

        /// <summary>
        /// 为测试组件设置指定的私有序列化字段。
        /// </summary>
        /// <typeparam name="TValue">需要写入的字段值类型。</typeparam>
        /// <param name="target">需要设置字段的测试组件。</param>
        /// <param name="fieldName">私有字段名称。</param>
        /// <param name="value">需要写入的字段值。</param>
        private static void SetPrivateField<TValue>(
            SaveAssetCatalog target,
            string fieldName,
            TValue value)
        {
            FieldInfo field = typeof(SaveAssetCatalog).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }

    public sealed class SaveTestItemData : ItemData
    {
    }
}
