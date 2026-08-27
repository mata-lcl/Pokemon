using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    public enum DialoguePortraitSide
    {
        [InspectorName("左侧")]
        Left,
        [InspectorName("右侧")]
        Right
    }

    public enum DialogueBranchCondition
    {
        [InspectorName("始终可用（普通对话）")]
        Always,
        [InspectorName("指定任务状态")]
        QuestState,
        [InspectorName("任务可推进（简化模式）")]
        QuestCanProgress
    }

    public enum DialogueCompletionAction
    {
        [InspectorName("无任务操作")]
        None,
        [InspectorName("接取任务")]
        AcceptQuest,
        [InspectorName("完成当前 NPC 对话目标")]
        ReportNpcTalked,
        [InspectorName("提交任务并发放奖励")]
        SubmitQuest,
        [InspectorName("一次完成 NPC 任务并发奖")]
        CompleteNpcQuest
    }

    [Serializable]
    public class DialogueLineDefinition
    {
        [SerializeField] private string speakerName;
        [SerializeField] private Sprite portrait;
        [SerializeField] private DialoguePortraitSide portraitSide;
        [TextArea(2, 5)]
        [SerializeField] private string text;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private string animationTrigger;

        public string SpeakerName => speakerName;
        public Sprite Portrait => portrait;
        public DialoguePortraitSide PortraitSide => portraitSide;
        public string Text => text;
        public AudioClip VoiceClip => voiceClip;
        public string AnimationTrigger => animationTrigger;
    }

    [Serializable]
    public class DialogueBranchDefinition
    {
        [SerializeField] private DialogueBranchCondition condition;
        [SerializeField] private QuestDefinition quest;
        [SerializeField] private QuestState requiredQuestState;
        [SerializeField] private List<DialogueLineDefinition> lines =
            new List<DialogueLineDefinition>();
        [SerializeField] private DialogueCompletionAction completionAction;

        public DialogueBranchCondition Condition => condition;
        public QuestDefinition Quest => quest;
        public QuestState RequiredQuestState => requiredQuestState;
        public IReadOnlyList<DialogueLineDefinition> Lines => lines;
        public DialogueCompletionAction CompletionAction => completionAction;

        /// <summary>
        /// 判断当前任务进度是否满足该对话分支条件。
        /// </summary>
        public bool IsAvailable()
        {
            if (condition == DialogueBranchCondition.Always)
                return true;

            if (quest == null ||
                !QuestService.TryGetRuntimeData(quest.QuestId, out QuestRuntimeData runtimeData))
                return false;

            if (condition == DialogueBranchCondition.QuestCanProgress)
            {
                return runtimeData.state == QuestState.Available ||
                       runtimeData.state == QuestState.Active ||
                       runtimeData.state == QuestState.ReadyToSubmit;
            }

            return runtimeData.state == requiredQuestState;
        }
    }

    [CreateAssetMenu(fileName = "NPC对话_", menuName = "宝可梦/配置/NPC 对话配置")]
    public class NpcDialogueDefinition : ScriptableObject
    {
        [Header("NPC 信息")]
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite defaultPortrait;

        [Header("按顺序匹配，第一个满足条件的分支会被播放")]
        [SerializeField] private List<DialogueBranchDefinition> branches =
            new List<DialogueBranchDefinition>();

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public Sprite DefaultPortrait => defaultPortrait;

        /// <summary>
        /// 按配置顺序返回第一个满足当前任务状态的对话分支。
        /// </summary>
        /// <param name="branch">返回当前可播放的对话分支。</param>
        public bool TryGetAvailableBranch(out DialogueBranchDefinition branch)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i].IsAvailable())
                {
                    branch = branches[i];
                    return true;
                }
            }

            branch = null;
            return false;
        }
    }
}
