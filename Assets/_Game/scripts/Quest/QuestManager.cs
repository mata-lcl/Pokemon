using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestDefinition[] questDefinitions;

        public IReadOnlyList<QuestDefinition> QuestDefinitions => questDefinitions;
        public event Action Changed;

        /// <summary>
        /// 注册 Inspector 中绑定的全部任务配置。
        /// </summary>
        private void Awake()
        {
            QuestService.RegisterDefinitions(questDefinitions);
        }

        /// <summary>
        /// 组件启用时监听任务数据变化。
        /// </summary>
        private void OnEnable()
        {
            QuestService.Changed += HandleQuestChanged;
        }

        /// <summary>
        /// 组件禁用时解除任务数据变化监听。
        /// </summary>
        private void OnDisable()
        {
            QuestService.Changed -= HandleQuestChanged;
        }

        /// <summary>
        /// 接取指定任务配置。
        /// </summary>
        /// <param name="quest">需要接取的任务。</param>
        public bool AcceptQuest(QuestDefinition quest)
        {
            return QuestService.AcceptQuest(quest);
        }

        /// <summary>
        /// 提交指定任务配置并领取奖励。
        /// </summary>
        /// <param name="quest">需要提交的任务。</param>
        public bool SubmitQuest(QuestDefinition quest)
        {
            return QuestService.SubmitQuest(quest);
        }

        /// <summary>
        /// 返回指定任务当前的运行数据。
        /// </summary>
        /// <param name="quest">需要查询的任务。</param>
        /// <param name="runtimeData">返回任务运行数据。</param>
        public bool TryGetRuntimeData(QuestDefinition quest, out QuestRuntimeData runtimeData)
        {
            runtimeData = null;
            return quest != null && QuestService.TryGetRuntimeData(quest.QuestId, out runtimeData);
        }

        /// <summary>
        /// 返回第一个正在进行或等待提交的任务，供追踪界面显示。
        /// </summary>
        public QuestDefinition GetFirstTrackableQuest()
        {
            for (int i = 0; i < questDefinitions.Length; i++)
            {
                QuestDefinition quest = questDefinitions[i];
                if (quest == null || !TryGetRuntimeData(quest, out QuestRuntimeData runtimeData))
                    continue;

                if (runtimeData.state == QuestState.Active ||
                    runtimeData.state == QuestState.ReadyToSubmit)
                    return quest;
            }
            return null;
        }

        /// <summary>
        /// 上报完成与指定 NPC 的对话。
        /// </summary>
        /// <param name="npcId">NPC 的稳定编号。</param>
        public void ReportNpcTalked(string npcId)
        {
            QuestService.ReportNpcTalked(npcId);
        }

        /// <summary>
        /// 将全局任务变化转发给当前场景中的任务界面。
        /// </summary>
        private void HandleQuestChanged()
        {
            Changed?.Invoke();
        }
    }
}
