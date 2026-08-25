using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    public enum QuestCategory
    {
        [InspectorName("主线任务")]
        Main,
        [InspectorName("支线任务")]
        Side,
        [InspectorName("日常任务")]
        Daily
    }

    public enum QuestState
    {
        [InspectorName("未解锁")]
        Locked,
        [InspectorName("可接取")]
        Available,
        [InspectorName("进行中")]
        Active,
        [InspectorName("可提交")]
        ReadyToSubmit,
        [InspectorName("已完成")]
        Completed,
        [InspectorName("已失败（预留）")]
        Failed
    }

    public enum QuestObjectiveType
    {
        [InspectorName("击败宝可梦")]
        DefeatPokemon,
        [InspectorName("收集道具")]
        CollectItem,
        [InspectorName("与 NPC 对话")]
        TalkToNpc
    }

    public enum QuestRewardType
    {
        [InspectorName("金币")]
        Money,
        [InspectorName("道具")]
        Item
    }

    [Serializable]
    public class QuestObjectiveDefinition
    {
        [SerializeField] private QuestObjectiveType type;
        [SerializeField] private PokemonSpeciesData targetPokemon;
        [SerializeField] private ItemData targetItem;
        [SerializeField] private string targetNpcId;
        [Min(1)]
        [SerializeField] private int requiredAmount = 1;

        public QuestObjectiveType Type => type;
        public PokemonSpeciesData TargetPokemon => targetPokemon;
        public ItemData TargetItem => targetItem;
        public string TargetNpcId => targetNpcId;
        public int RequiredAmount => requiredAmount;

        /// <summary>
        /// 返回任务追踪界面使用的目标说明。
        /// </summary>
        public string GetDisplayText()
        {
            switch (type)
            {
                case QuestObjectiveType.DefeatPokemon:
                    return $"击败 {targetPokemon.DisplayName}";
                case QuestObjectiveType.CollectItem:
                    return $"收集 {targetItem.DisplayName}";
                case QuestObjectiveType.TalkToNpc:
                    return $"与 {targetNpcId} 对话";
                default:
                    return string.Empty;
            }
        }
    }

    [Serializable]
    public class QuestRewardDefinition
    {
        [SerializeField] private QuestRewardType type;
        [SerializeField] private ItemData item;
        [Min(1)]
        [SerializeField] private int amount = 1;

        public QuestRewardType Type => type;
        public ItemData Item => item;
        public int Amount => amount;
    }

    [CreateAssetMenu(fileName = "Quest_", menuName = "Pokemon/任务配置")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("基本信息")]
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private QuestCategory category;

        [Header("任务关系")]
        [SerializeField] private List<QuestDefinition> prerequisites = new List<QuestDefinition>();

        [Header("目标与奖励")]
        [SerializeField] private List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private List<QuestRewardDefinition> rewards = new List<QuestRewardDefinition>();

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public QuestCategory Category => category;
        public IReadOnlyList<QuestDefinition> Prerequisites => prerequisites;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => objectives;
        public IReadOnlyList<QuestRewardDefinition> Rewards => rewards;
    }
}
