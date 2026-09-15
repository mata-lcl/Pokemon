using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    public static class QuestApplicationCoordinator
    {
        /// <summary>
        /// 在场景加载前建立玩家资产事件与任务系统之间的应用层协调关系。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Initialize();
        }

        /// <summary>
        /// 建立玩家资产事件与任务系统之间的协调关系，重复调用不会重复订阅。
        /// </summary>
        public static void Initialize()
        {
            PlayerParty.ItemAdded -= HandleItemAdded;
            QuestService.QuestCompleted -= HandleQuestCompleted;
            PlayerParty.ItemAdded += HandleItemAdded;
            QuestService.QuestCompleted += HandleQuestCompleted;
        }

        /// <summary>
        /// 将玩家获得道具的领域事件转交给任务进度系统。
        /// </summary>
        /// <param name="item">本次获得的道具。</param>
        /// <param name="count">本次获得数量。</param>
        private static void HandleItemAdded(ItemData item, int count)
        {
            QuestService.ReportItemCollected(item, count);
        }

        /// <summary>
        /// 在任务完成后向玩家发放任务配置中的全部奖励。
        /// </summary>
        /// <param name="definition">已经完成并包含奖励内容的任务配置。</param>
        private static void HandleQuestCompleted(QuestDefinition definition)
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
    }
}
