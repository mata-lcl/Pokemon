using System.Collections.Generic;
using System.Text;
using Pokemon.Domain;
using TMPro;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class QuestJournalView : MonoBehaviour
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private TMP_Text questText;

        /// <summary>
        /// 面板启用时监听任务变化并刷新任务列表。
        /// </summary>
        private void OnEnable()
        {
            questManager.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// 面板禁用时解除任务变化监听。
        /// </summary>
        private void OnDisable()
        {
            questManager.Changed -= Refresh;
        }

        /// <summary>
        /// 显示已接取、待提交和已完成任务的状态与目标进度。
        /// </summary>
        private void Refresh()
        {
            StringBuilder builder = new StringBuilder();
            IReadOnlyList<QuestDefinition> quests = questManager.QuestDefinitions;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null || !questManager.TryGetRuntimeData(quest, out QuestRuntimeData runtimeData))
                    continue;
                if (!IsVisibleState(runtimeData.state))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append('【').Append(GetStateText(runtimeData.state)).Append("】");
                builder.AppendLine(quest.Title);
                if (!string.IsNullOrWhiteSpace(quest.Description))
                    builder.AppendLine(quest.Description);

                AppendObjectives(builder, quest, runtimeData);
            }

            questText.text = builder.Length > 0
                ? builder.ToString()
                : "当前没有已接取的任务。";
        }

        /// <summary>
        /// 判断任务状态是否需要显示在任务栏中。
        /// </summary>
        /// <param name="state">需要判断的任务状态。</param>
        private static bool IsVisibleState(QuestState state)
        {
            return state == QuestState.Active ||
                   state == QuestState.ReadyToSubmit ||
                   state == QuestState.Completed;
        }

        /// <summary>
        /// 返回任务栏使用的中文任务状态。
        /// </summary>
        /// <param name="state">需要转换为文字的任务状态。</param>
        private static string GetStateText(QuestState state)
        {
            switch (state)
            {
                case QuestState.Active:
                    return "进行中";
                case QuestState.ReadyToSubmit:
                    return "可提交";
                case QuestState.Completed:
                    return "已完成";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 将任务目标及其当前进度添加到任务栏文字中。
        /// </summary>
        /// <param name="builder">用于拼接任务栏内容的文字构建器。</param>
        /// <param name="quest">需要显示目标的任务配置。</param>
        /// <param name="runtimeData">任务当前的运行数据。</param>
        private static void AppendObjectives(
            StringBuilder builder,
            QuestDefinition quest,
            QuestRuntimeData runtimeData)
        {
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];
                builder.Append("- ").Append(objective.GetDisplayText()).Append("  ");
                builder.Append(runtimeData.objectiveProgress[i]).Append('/');
                builder.Append(objective.RequiredAmount);
                if (i < quest.Objectives.Count - 1)
                    builder.AppendLine();
            }
        }
    }
}
