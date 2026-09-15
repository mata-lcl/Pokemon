using System.Collections.Generic;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    public sealed class SaveGameMapper
    {
        /// <summary>
        /// 将当前运行状态转换为可序列化的存档数据。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="savedAt">存档创建时间文本。</param>
        /// <param name="sceneName">保存时所在场景名称。</param>
        /// <param name="playerPosition">保存时玩家所在位置。</param>
        /// <returns>包含当前玩家与任务状态的存档数据。</returns>
        public SaveGameData CreateSaveData(
            int slotIndex,
            string savedAt,
            string sceneName,
            Vector3 playerPosition)
        {
            IReadOnlyList<MonsterRuntime> party = PlayerParty.GetPartySnapshot();
            IReadOnlyList<MonsterRuntime> storage = PlayerParty.GetStorageSnapshot();
            SaveGameData data = new SaveGameData
            {
                slotIndex = slotIndex,
                savedAt = savedAt,
                sceneName = sceneName,
                playerPosition = playerPosition,
                activePartyIndex = IndexOf(party, PlayerParty.ActivePokemon),
                money = PlayerParty.Money,
                quests = QuestService.CreateSaveData()
            };

            AddMonsters(data.party, party);
            AddMonsters(data.storage, storage);

            IReadOnlyList<InventoryItemStack> inventory =
                PlayerParty.GetInventorySnapshot();
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemData item = inventory[i].Item;
                data.inventory.Add(new InventorySaveData
                {
                    itemId = item.Id,
                    itemAssetName = item.name,
                    count = inventory[i].Count
                });
            }

            return data;
        }

        /// <summary>
        /// 将存档数据解析并恢复到玩家与任务运行状态。
        /// </summary>
        /// <param name="data">需要恢复的存档数据。</param>
        /// <param name="assetCatalog">用于解析存档资源标识的资源目录。</param>
        public void RestoreState(
            SaveGameData data,
            SaveAssetCatalog assetCatalog)
        {
            List<MonsterRuntime> party = RestoreMonsters(
                data.party,
                assetCatalog);
            List<MonsterRuntime> storage = RestoreMonsters(
                data.storage,
                assetCatalog);
            List<InventoryItemStack> inventory =
                new List<InventoryItemStack>();
            for (int i = 0; i < data.inventory.Count; i++)
            {
                InventorySaveData itemData = data.inventory[i];
                inventory.Add(new InventoryItemStack(
                    assetCatalog.ResolveItem(
                        itemData.itemId,
                        itemData.itemAssetName),
                    itemData.count));
            }

            PlayerParty.RestoreState(
                party,
                storage,
                data.activePartyIndex,
                inventory);
            PlayerParty.RestoreMoney(data.money);
            QuestService.RestoreState(data.quests);
        }

        /// <summary>
        /// 将精灵集合转换为可序列化存档数据并保持当前顺序。
        /// </summary>
        /// <param name="destination">接收精灵存档数据的列表。</param>
        /// <param name="monsters">需要保存的精灵集合。</param>
        private static void AddMonsters(
            List<MonsterSaveData> destination,
            IReadOnlyList<MonsterRuntime> monsters)
        {
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] != null && monsters[i].Species != null)
                    destination.Add(CreateMonsterSaveData(monsters[i]));
            }
        }

        /// <summary>
        /// 将一只运行时精灵转换为可序列化存档数据。
        /// </summary>
        /// <param name="monster">需要保存的运行时精灵。</param>
        private static MonsterSaveData CreateMonsterSaveData(
            MonsterRuntime monster)
        {
            MonsterSaveData data = new MonsterSaveData
            {
                speciesId = monster.Species.ID,
                speciesAssetName = monster.Species.name,
                level = monster.Level,
                currentExp = monster.CurrentExp,
                currentHP = monster.CurrentHP,
                currentStatus = (int)monster.CurrentStatus,
                ivHP = monster.IvHP,
                ivAttack = monster.IvAttack,
                ivDefense = monster.IvDefense,
                ivSpeed = monster.IvSpeed,
                ivSpecialAttack = monster.IvSpecialAttack,
                ivSpecialDefense = monster.IvSpecialDefense,
                evHP = monster.EvHP,
                evAttack = monster.EvAttack,
                evDefense = monster.EvDefense,
                evSpeed = monster.EvSpeed,
                evSpecialAttack = monster.EvSpecialAttack,
                evSpecialDefense = monster.EvSpecialDefense,
                abilityId = monster.ActiveAbility != null
                    ? monster.ActiveAbility.Id
                    : -1,
                abilityAssetName = monster.ActiveAbility != null
                    ? monster.ActiveAbility.name
                    : string.Empty,
                heldItemId = monster.HeldItem != null
                    ? monster.HeldItem.Id
                    : string.Empty,
                heldItemAssetName = monster.HeldItem != null
                    ? monster.HeldItem.name
                    : string.Empty
            };

            foreach (KeyValuePair<SkillData, int> skill in monster.CurrentPP)
            {
                if (skill.Key == null)
                    continue;

                data.skillPP.Add(new SkillPPSaveData
                {
                    skillId = skill.Key.Id,
                    skillAssetName = skill.Key.name,
                    currentPP = skill.Value
                });
            }

            return data;
        }

        /// <summary>
        /// 将精灵存档集合恢复为运行时精灵集合并保持原有顺序。
        /// </summary>
        /// <param name="savedMonsters">需要恢复的精灵存档集合。</param>
        /// <param name="assetCatalog">用于解析精灵相关资源的资源目录。</param>
        private static List<MonsterRuntime> RestoreMonsters(
            IReadOnlyList<MonsterSaveData> savedMonsters,
            SaveAssetCatalog assetCatalog)
        {
            List<MonsterRuntime> monsters =
                new List<MonsterRuntime>(savedMonsters.Count);
            for (int i = 0; i < savedMonsters.Count; i++)
            {
                MonsterSaveData savedMonster = savedMonsters[i];
                PokemonSpeciesData species = assetCatalog.ResolveSpecies(
                    savedMonster.speciesId,
                    savedMonster.speciesAssetName);
                AbilityData ability = string.IsNullOrEmpty(
                    savedMonster.abilityAssetName)
                    ? null
                    : assetCatalog.ResolveAbility(
                        savedMonster.abilityId,
                        savedMonster.abilityAssetName);
                ItemData heldItem = string.IsNullOrEmpty(
                    savedMonster.heldItemAssetName)
                    ? null
                    : assetCatalog.ResolveItem(
                        savedMonster.heldItemId,
                        savedMonster.heldItemAssetName);
                Dictionary<SkillData, int> skillPP =
                    new Dictionary<SkillData, int>();
                for (int skillIndex = 0;
                     skillIndex < savedMonster.skillPP.Count;
                     skillIndex++)
                {
                    SkillPPSaveData savedSkill =
                        savedMonster.skillPP[skillIndex];
                    skillPP.Add(
                        assetCatalog.ResolveSkill(
                            savedSkill.skillId,
                            savedSkill.skillAssetName),
                        savedSkill.currentPP);
                }

                MonsterRuntime monster =
                    new MonsterRuntime(species, savedMonster.level);
                monster.RestoreState(
                    savedMonster.level,
                    savedMonster.currentExp,
                    savedMonster.currentHP,
                    (StatusCondition)savedMonster.currentStatus,
                    savedMonster.ivHP,
                    savedMonster.ivAttack,
                    savedMonster.ivDefense,
                    savedMonster.ivSpeed,
                    savedMonster.ivSpecialAttack,
                    savedMonster.ivSpecialDefense,
                    savedMonster.evHP,
                    savedMonster.evAttack,
                    savedMonster.evDefense,
                    savedMonster.evSpeed,
                    savedMonster.evSpecialAttack,
                    savedMonster.evSpecialDefense,
                    ability,
                    heldItem,
                    skillPP);
                monsters.Add(monster);
            }

            return monsters;
        }

        /// <summary>
        /// 返回指定精灵在只读集合中的索引。
        /// </summary>
        /// <param name="monsters">需要查询的精灵集合。</param>
        /// <param name="target">需要定位的精灵。</param>
        private static int IndexOf(
            IReadOnlyList<MonsterRuntime> monsters,
            MonsterRuntime target)
        {
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] == target)
                    return i;
            }

            return -1;
        }
    }
}
