using System;
using System.Collections.Generic;
using System.IO;
using Pokemon.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pokemon.Application
{
    [Serializable]
    public class SaveGameData
    {
        public int version = 1;
        public int slotIndex;
        public string savedAt;
        public string sceneName;
        public Vector3 playerPosition;
        public int activePartyIndex;
        public List<MonsterSaveData> party = new List<MonsterSaveData>();
        public List<MonsterSaveData> storage = new List<MonsterSaveData>();
        public List<InventorySaveData> inventory = new List<InventorySaveData>();
    }

    [Serializable]
    public class MonsterSaveData
    {
        public int speciesId;
        public string speciesAssetName;
        public int level;
        public int currentExp;
        public int currentHP;
        public int currentStatus;
        public int ivHP;
        public int ivAttack;
        public int ivDefense;
        public int ivSpeed;
        public int ivSpecialAttack;
        public int ivSpecialDefense;
        public int evHP;
        public int evAttack;
        public int evDefense;
        public int evSpeed;
        public int evSpecialAttack;
        public int evSpecialDefense;
        public int abilityId;
        public string abilityAssetName;
        public string heldItemId;
        public string heldItemAssetName;
        public List<SkillPPSaveData> skillPP = new List<SkillPPSaveData>();
    }

    [Serializable]
    public class SkillPPSaveData
    {
        public string skillId;
        public string skillAssetName;
        public int currentPP;
    }

    [Serializable]
    public class InventorySaveData
    {
        public string itemId;
        public string itemAssetName;
        public int count;
    }

    public readonly struct SaveSlotSummary
    {
        public int SlotIndex { get; }
        public bool HasSave { get; }
        public string SavedAt { get; }
        public string SceneName { get; }
        public int PartyCount { get; }

        /// <summary>
        /// 创建供存档界面显示的槽位摘要。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="hasSave">当前存档栏是否已有存档。</param>
        /// <param name="savedAt">保存时间文本。</param>
        /// <param name="sceneName">保存时所在场景名称。</param>
        /// <param name="partyCount">保存时的队伍精灵数量。</param>
        public SaveSlotSummary(
            int slotIndex,
            bool hasSave,
            string savedAt,
            string sceneName,
            int partyCount)
        {
            SlotIndex = slotIndex;
            HasSave = hasSave;
            SavedAt = savedAt;
            SceneName = sceneName;
            PartyCount = partyCount;
        }
    }

    public static class SaveGameService
    {
        public const int SlotCount = 3;

        /// <summary>
        /// 将当前玩家位置、队伍、仓库和背包信息写入指定本地存档栏。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="playerPosition">保存时玩家所在位置。</param>
        public static void Save(int slotIndex, Vector3 playerPosition)
        {
            ValidateSlotIndex(slotIndex);
            SaveGameData data = CreateSaveData(slotIndex, playerPosition);
            Directory.CreateDirectory(GetSaveDirectory());
            File.WriteAllText(GetSlotPath(slotIndex), JsonUtility.ToJson(data, true));
        }

        /// <summary>
        /// 返回全部存档栏的摘要信息。
        /// </summary>
        public static IReadOnlyList<SaveSlotSummary> GetSlotSummaries()
        {
            List<SaveSlotSummary> summaries = new List<SaveSlotSummary>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
                summaries.Add(GetSlotSummary(i));
            return summaries;
        }

        /// <summary>
        /// 返回指定存档栏是否已有本地存档。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        public static bool HasSave(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return File.Exists(GetSlotPath(slotIndex));
        }

        /// <summary>
        /// 生成当前游戏状态的可序列化存档数据。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        /// <param name="playerPosition">保存时玩家所在位置。</param>
        private static SaveGameData CreateSaveData(int slotIndex, Vector3 playerPosition)
        {
            IReadOnlyList<MonsterRuntime> party = PlayerParty.GetPartySnapshot();
            IReadOnlyList<MonsterRuntime> storage = PlayerParty.GetStorageSnapshot();
            SaveGameData data = new SaveGameData
            {
                slotIndex = slotIndex,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = playerPosition,
                activePartyIndex = IndexOf(party, PlayerParty.ActivePokemon)
            };

            AddMonsters(data.party, party);
            AddMonsters(data.storage, storage);

            IReadOnlyList<InventoryItemStack> inventory = PlayerParty.GetInventorySnapshot();
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
        private static MonsterSaveData CreateMonsterSaveData(MonsterRuntime monster)
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
                abilityId = monster.ActiveAbility != null ? monster.ActiveAbility.Id : -1,
                abilityAssetName = monster.ActiveAbility != null ? monster.ActiveAbility.name : string.Empty,
                heldItemId = monster.HeldItem != null ? monster.HeldItem.Id : string.Empty,
                heldItemAssetName = monster.HeldItem != null ? monster.HeldItem.name : string.Empty
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
        /// 读取指定存档栏的摘要信息。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private static SaveSlotSummary GetSlotSummary(int slotIndex)
        {
            string path = GetSlotPath(slotIndex);
            if (!File.Exists(path))
                return new SaveSlotSummary(slotIndex, false, string.Empty, string.Empty, 0);

            SaveGameData data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            return new SaveSlotSummary(
                slotIndex,
                true,
                data.savedAt,
                data.sceneName,
                data.party != null ? data.party.Count : 0);
        }

        /// <summary>
        /// 返回指定精灵在只读集合中的索引。
        /// </summary>
        /// <param name="monsters">需要查询的精灵集合。</param>
        /// <param name="target">需要定位的精灵。</param>
        private static int IndexOf(IReadOnlyList<MonsterRuntime> monsters, MonsterRuntime target)
        {
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] == target)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 返回本地存档文件夹路径。
        /// </summary>
        private static string GetSaveDirectory()
        {
            return Path.Combine(UnityEngine.Application.persistentDataPath, "Saves");
        }

        /// <summary>
        /// 返回指定存档栏的本地 JSON 文件路径。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private static string GetSlotPath(int slotIndex)
        {
            return Path.Combine(GetSaveDirectory(), $"slot_{slotIndex + 1}.json");
        }

        /// <summary>
        /// 检查存档栏索引是否在有效范围内。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}
