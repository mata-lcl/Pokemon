using System;
using System.Collections.Generic;

namespace Pokemon.Domain
{
    public static class QuestService
    {
        private static readonly Dictionary<string, QuestDefinition> Definitions =
            new Dictionary<string, QuestDefinition>();
        private static readonly List<QuestRuntimeData> RuntimeData =
            new List<QuestRuntimeData>();

        public static event Action Changed;

        /// <summary>
        /// 注册当前游戏使用的任务配置，并为新任务建立初始状态。
        /// </summary>
        /// <param name="definitions">需要注册的任务配置集合。</param>
        public static void RegisterDefinitions(IReadOnlyList<QuestDefinition> definitions)
        {
            Definitions.Clear();
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition != null && !string.IsNullOrWhiteSpace(definition.QuestId))
                    Definitions[definition.QuestId] = definition;
            }

            foreach (QuestDefinition definition in Definitions.Values)
                EnsureRuntimeData(definition);

            RefreshAvailableQuests();
            Changed?.Invoke();
        }

        /// <summary>
        /// 清空全部任务进度并按已注册任务重新建立初始状态。
        /// </summary>
        public static void ResetState()
        {
            RuntimeData.Clear();
            foreach (QuestDefinition definition in Definitions.Values)
                EnsureRuntimeData(definition);

            RefreshAvailableQuests();
            Changed?.Invoke();
        }

        /// <summary>
        /// 使用存档数据替换当前任务进度。
        /// </summary>
        /// <param name="savedData">存档中的任务运行数据。</param>
        public static void RestoreState(IReadOnlyList<QuestRuntimeData> savedData)
        {
            RuntimeData.Clear();
            if (savedData != null)
            {
                for (int i = 0; i < savedData.Count; i++)
                    RuntimeData.Add(CloneRuntimeData(savedData[i]));
            }

            foreach (QuestDefinition definition in Definitions.Values)
                EnsureRuntimeData(definition);

            RefreshAvailableQuests();
            Changed?.Invoke();
        }

        /// <summary>
        /// 创建可直接写入存档的任务进度副本。
        /// </summary>
        public static List<QuestRuntimeData> CreateSaveData()
        {
            List<QuestRuntimeData> result = new List<QuestRuntimeData>(RuntimeData.Count);
            for (int i = 0; i < RuntimeData.Count; i++)
                result.Add(CloneRuntimeData(RuntimeData[i]));
            return result;
        }

        /// <summary>
        /// 返回指定任务当前的运行状态。
        /// </summary>
        /// <param name="questId">需要查询的任务编号。</param>
        /// <param name="runtimeData">返回匹配的任务运行数据。</param>
        public static bool TryGetRuntimeData(string questId, out QuestRuntimeData runtimeData)
        {
            runtimeData = FindRuntimeData(questId);
            return runtimeData != null;
        }

        /// <summary>
        /// 接取当前处于可接取状态的任务。
        /// </summary>
        /// <param name="definition">需要接取的任务配置。</param>
        public static bool AcceptQuest(QuestDefinition definition)
        {
            QuestRuntimeData runtimeData = GetRegisteredRuntimeData(definition);
            if (runtimeData == null || runtimeData.state != QuestState.Available)
                return false;

            runtimeData.state = definition.Objectives.Count == 0
                ? QuestState.ReadyToSubmit
                : QuestState.Active;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 提交已达成全部目标的任务并发放奖励。
        /// </summary>
        /// <param name="definition">需要提交的任务配置。</param>
        public static bool SubmitQuest(QuestDefinition definition)
        {
            QuestRuntimeData runtimeData = GetRegisteredRuntimeData(definition);
            if (runtimeData == null || runtimeData.state != QuestState.ReadyToSubmit)
                return false;

            runtimeData.state = QuestState.Completed;
            GrantRewards(definition);
            RefreshAvailableQuests();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 记录玩家击败指定种类宝可梦的次数。
        /// </summary>
        /// <param name="species">被击败的宝可梦种类。</param>
        /// <param name="amount">本次击败数量。</param>
        public static void ReportPokemonDefeated(PokemonSpeciesData species, int amount = 1)
        {
            ReportProgress(
                QuestObjectiveType.DefeatPokemon,
                objective => objective.TargetPokemon == species,
                amount);
        }

        /// <summary>
        /// 记录玩家新获得指定道具的数量。
        /// </summary>
        /// <param name="item">本次获得的道具。</param>
        /// <param name="amount">本次获得数量。</param>
        public static void ReportItemCollected(ItemData item, int amount)
        {
            ReportProgress(
                QuestObjectiveType.CollectItem,
                objective => objective.TargetItem == item,
                amount);
        }

        /// <summary>
        /// 记录玩家完成与指定 NPC 的对话。
        /// </summary>
        /// <param name="npcId">NPC 的稳定编号。</param>
        public static void ReportNpcTalked(string npcId)
        {
            ReportProgress(
                QuestObjectiveType.TalkToNpc,
                objective => objective.TargetNpcId == npcId,
                1);
        }

        /// <summary>
        /// 按目标类型和匹配条件推进所有进行中的任务。
        /// </summary>
        /// <param name="objectiveType">需要推进的任务目标类型。</param>
        /// <param name="matches">判断目标是否匹配本次事件的方法。</param>
        /// <param name="amount">本次增加的进度。</param>
        private static void ReportProgress(
            QuestObjectiveType objectiveType,
            Predicate<QuestObjectiveDefinition> matches,
            int amount)
        {
            if (amount <= 0)
                return;

            bool changed = false;
            foreach (QuestDefinition definition in Definitions.Values)
            {
                QuestRuntimeData runtimeData = FindRuntimeData(definition.QuestId);
                if (runtimeData == null || runtimeData.state != QuestState.Active)
                    continue;

                for (int i = 0; i < definition.Objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.Objectives[i];
                    if (objective.Type != objectiveType || !matches(objective))
                        continue;

                    runtimeData.objectiveProgress[i] = Math.Min(
                        objective.RequiredAmount,
                        runtimeData.objectiveProgress[i] + amount);
                    changed = true;
                }

                if (AreObjectivesCompleted(definition, runtimeData))
                    runtimeData.state = QuestState.ReadyToSubmit;
            }

            if (changed)
                Changed?.Invoke();
        }

        /// <summary>
        /// 为任务建立运行数据，并使目标进度数量与配置保持一致。
        /// </summary>
        /// <param name="definition">需要初始化运行数据的任务配置。</param>
        private static void EnsureRuntimeData(QuestDefinition definition)
        {
            QuestRuntimeData runtimeData = FindRuntimeData(definition.QuestId);
            if (runtimeData == null)
            {
                runtimeData = new QuestRuntimeData
                {
                    questId = definition.QuestId,
                    state = QuestState.Locked
                };
                RuntimeData.Add(runtimeData);
            }

            while (runtimeData.objectiveProgress.Count < definition.Objectives.Count)
                runtimeData.objectiveProgress.Add(0);
            if (runtimeData.objectiveProgress.Count > definition.Objectives.Count)
            {
                runtimeData.objectiveProgress.RemoveRange(
                    definition.Objectives.Count,
                    runtimeData.objectiveProgress.Count - definition.Objectives.Count);
            }
        }

        /// <summary>
        /// 将前置任务均已完成的锁定任务更新为可接取状态。
        /// </summary>
        private static void RefreshAvailableQuests()
        {
            foreach (QuestDefinition definition in Definitions.Values)
            {
                QuestRuntimeData runtimeData = FindRuntimeData(definition.QuestId);
                if (runtimeData.state == QuestState.Locked && ArePrerequisitesCompleted(definition))
                    runtimeData.state = QuestState.Available;
            }
        }

        /// <summary>
        /// 判断指定任务的全部前置任务是否已经完成。
        /// </summary>
        /// <param name="definition">需要检查前置条件的任务配置。</param>
        private static bool ArePrerequisitesCompleted(QuestDefinition definition)
        {
            for (int i = 0; i < definition.Prerequisites.Count; i++)
            {
                QuestDefinition prerequisite = definition.Prerequisites[i];
                QuestRuntimeData runtimeData = prerequisite != null
                    ? FindRuntimeData(prerequisite.QuestId)
                    : null;
                if (runtimeData == null || runtimeData.state != QuestState.Completed)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 判断指定任务的全部目标是否已经达到要求。
        /// </summary>
        /// <param name="definition">需要检查的任务配置。</param>
        /// <param name="runtimeData">任务当前运行数据。</param>
        private static bool AreObjectivesCompleted(
            QuestDefinition definition,
            QuestRuntimeData runtimeData)
        {
            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                if (runtimeData.objectiveProgress[i] < definition.Objectives[i].RequiredAmount)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 向玩家发放指定任务配置的全部奖励。
        /// </summary>
        /// <param name="definition">包含奖励内容的任务配置。</param>
        private static void GrantRewards(QuestDefinition definition)
        {
            for (int i = 0; i < definition.Rewards.Count; i++)
            {
                QuestRewardDefinition reward = definition.Rewards[i];
                if (reward.Type == QuestRewardType.Money)
                    PlayerParty.AddMoney(reward.Amount);
                else
                    PlayerParty.AddItem(reward.Item, reward.Amount);
            }
        }

        /// <summary>
        /// 返回已注册任务对应的运行数据。
        /// </summary>
        /// <param name="definition">需要查询的任务配置。</param>
        private static QuestRuntimeData GetRegisteredRuntimeData(QuestDefinition definition)
        {
            if (definition == null || !Definitions.ContainsKey(definition.QuestId))
                return null;
            return FindRuntimeData(definition.QuestId);
        }

        /// <summary>
        /// 按任务编号查找运行数据。
        /// </summary>
        /// <param name="questId">需要查找的任务编号。</param>
        private static QuestRuntimeData FindRuntimeData(string questId)
        {
            for (int i = 0; i < RuntimeData.Count; i++)
            {
                if (RuntimeData[i].questId == questId)
                    return RuntimeData[i];
            }
            return null;
        }

        /// <summary>
        /// 创建一份与来源任务进度互不共享列表的副本。
        /// </summary>
        /// <param name="source">需要复制的任务运行数据。</param>
        private static QuestRuntimeData CloneRuntimeData(QuestRuntimeData source)
        {
            return new QuestRuntimeData
            {
                questId = source.questId,
                state = source.state,
                objectiveProgress = new List<int>(source.objectiveProgress)
            };
        }
    }
}
