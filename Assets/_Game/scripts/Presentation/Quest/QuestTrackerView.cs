using System.Text;
using Pokemon.Domain;
using TMPro;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class QuestTrackerView : MonoBehaviour
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text objectivesText;

        /// <summary>
        /// 界面启用时监听任务变化并立即刷新追踪内容。
        /// </summary>
        private void OnEnable()
        {
            questManager.Changed += Refresh;
            Refresh();
        }

        /// <summary>
        /// 界面禁用时解除任务变化监听。
        /// </summary>
        private void OnDisable()
        {
            questManager.Changed -= Refresh;
        }

        /// <summary>
        /// 显示当前第一个进行中任务的标题、目标和进度。
        /// </summary>
        private void Refresh()
        {
            QuestDefinition quest = questManager.GetFirstTrackableQuest();
            contentRoot.SetActive(quest != null);
            if (quest == null)
                return;

            questManager.TryGetRuntimeData(quest, out QuestRuntimeData runtimeData);
            titleText.text = quest.Title;

            if (runtimeData.state == QuestState.ReadyToSubmit)
            {
                objectivesText.text = "任务目标已完成，请返回提交。";
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];
                builder.Append(objective.GetDisplayText());
                builder.Append("  ");
                builder.Append(runtimeData.objectiveProgress[i]);
                builder.Append('/');
                builder.Append(objective.RequiredAmount);
                if (i < quest.Objectives.Count - 1)
                    builder.AppendLine();
            }
            objectivesText.text = builder.ToString();
        }
    }
}
