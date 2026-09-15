using NUnit.Framework;
using Pokemon.Domain;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pokemon.Tests
{
    public sealed class PlayerPartyEncapsulationTests
    {
        /// <summary>
        /// 验证玩家队伍、仓库和背包集合不再作为公开字段暴露。
        /// </summary>
        [Test]
        public void MutableCollectionsAreNotPublic()
        {
            Assert.That(GetPublicStaticField("Party"), Is.Null);
            Assert.That(GetPublicStaticField("Storage"), Is.Null);
            Assert.That(GetPublicStaticField("Inventory"), Is.Null);
        }

        /// <summary>
        /// 验证修改队伍快照不会改变玩家队伍的内部集合。
        /// </summary>
        [Test]
        public void PartySnapshotCannotMutateInternalCollection()
        {
            PlayerParty.ResetState();
            PokemonSpeciesData species =
                ScriptableObject.CreateInstance<PokemonSpeciesData>();
            try
            {
                PlayerParty.AddMonster(new MonsterRuntime(species, 5));

                IReadOnlyList<MonsterRuntime> snapshot =
                    PlayerParty.GetPartySnapshot();
                if (snapshot is ICollection<MonsterRuntime> mutableSnapshot &&
                    !mutableSnapshot.IsReadOnly)
                {
                    mutableSnapshot.Clear();
                }

                Assert.That(PlayerParty.GetPartySnapshot(), Has.Count.EqualTo(1));
            }
            finally
            {
                PlayerParty.ResetState();
                Object.DestroyImmediate(species);
            }
        }

        /// <summary>
        /// 返回 PlayerParty 上指定名称的公开静态字段。
        /// </summary>
        /// <param name="fieldName">需要查找的字段名称。</param>
        /// <returns>找到的公开静态字段；不存在时返回空。</returns>
        private static FieldInfo GetPublicStaticField(string fieldName)
        {
            return typeof(PlayerParty).GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.Static);
        }
    }
}
