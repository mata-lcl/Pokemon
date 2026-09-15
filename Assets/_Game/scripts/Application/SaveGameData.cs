using System;
using System.Collections.Generic;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    [Serializable]
    public class SaveGameData
    {
        public int version = 2;
        public int slotIndex;
        public string savedAt;
        public string sceneName;
        public Vector3 playerPosition;
        public int activePartyIndex;
        public int money;
        public List<MonsterSaveData> party = new List<MonsterSaveData>();
        public List<MonsterSaveData> storage = new List<MonsterSaveData>();
        public List<InventorySaveData> inventory = new List<InventorySaveData>();
        public List<QuestRuntimeData> quests = new List<QuestRuntimeData>();
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
}
