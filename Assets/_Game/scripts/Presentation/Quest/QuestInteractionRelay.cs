using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class QuestInteractionRelay : MonoBehaviour
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private string npcId;

        /// <summary>
        /// 接取 Inspector 中绑定的任务，供按钮或对话事件调用。
        /// </summary>
        public void AcceptQuest()
        {
            questManager.AcceptQuest(quest);
        }

        /// <summary>
        /// 提交 Inspector 中绑定的任务，供按钮或对话事件调用。
        /// </summary>
        public void SubmitQuest()
        {
            questManager.SubmitQuest(quest);
        }

        /// <summary>
        /// 上报已完成与 Inspector 中配置 NPC 的对话。
        /// </summary>
        public void ReportNpcTalked()
        {
            questManager.ReportNpcTalked(npcId);
        }
    }
}
