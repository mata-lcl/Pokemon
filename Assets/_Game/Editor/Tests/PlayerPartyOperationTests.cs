using NUnit.Framework;
using Pokemon.Domain;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class PlayerPartyOperationTests
    {
        /// <summary>
        /// 验证第七只精灵会进入仓库并保留第一只为出战精灵。
        /// </summary>
        [Test]
        public void SeventhMonsterMovesToStorage()
        {
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            PlayerParty.ResetState();
            try
            {
                MonsterRuntime first = null;
                for (int i = 0; i < 7; i++)
                {
                    var monster = new MonsterRuntime(species, 5);
                    first ??= monster;
                    Assert.That(PlayerParty.TryAddMonster(monster), Is.True);
                }

                Assert.That(PlayerParty.GetPartySnapshot(), Has.Count.EqualTo(6));
                Assert.That(PlayerParty.GetStorageSnapshot(), Has.Count.EqualTo(1));
                Assert.That(PlayerParty.ActivePokemon, Is.SameAs(first));
            }
            finally
            {
                PlayerParty.ResetState();
                Object.DestroyImmediate(species);
            }
        }

        /// <summary>
        /// 验证队伍和仓库都满时拒绝继续接收精灵。
        /// </summary>
        [Test]
        public void FullPartyAndStorageRejectMonster()
        {
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            PlayerParty.ResetState();
            PlayerParty.ConfigureStorageCapacity(1, 1);
            try
            {
                for (int i = 0; i < 7; i++)
                    Assert.That(
                        PlayerParty.TryAddMonster(new MonsterRuntime(species, 5)),
                        Is.True);

                Assert.That(
                    PlayerParty.TryAddMonster(new MonsterRuntime(species, 5)),
                    Is.False);
                Assert.That(PlayerParty.CanReceiveMonster, Is.False);
            }
            finally
            {
                PlayerParty.ResetState();
                PlayerParty.ConfigureStorageCapacity(30, 10);
                Object.DestroyImmediate(species);
            }
        }

        /// <summary>
        /// 验证增加和移除道具会更新数量，并只在增加时发布获得事件。
        /// </summary>
        [Test]
        public void ItemOperationsUpdateCountAndPublishAddedEvent()
        {
            var item = ScriptableObject.CreateInstance<PlayerPartyTestItemData>();
            int reportedCount = 0;
            Action<ItemData, int> handler =
                (addedItem, count) => reportedCount += count;
            PlayerParty.ResetState();
            PlayerParty.ItemAdded += handler;
            try
            {
                PlayerParty.AddItem(item, 3);
                PlayerParty.RemoveItem(item, 2);

                Assert.That(PlayerParty.GetItemCount(item), Is.EqualTo(1));
                Assert.That(reportedCount, Is.EqualTo(3));
            }
            finally
            {
                PlayerParty.ItemAdded -= handler;
                PlayerParty.ResetState();
                Object.DestroyImmediate(item);
            }
        }
    }

    public sealed class PlayerPartyTestItemData : ItemData
    {
    }
}
