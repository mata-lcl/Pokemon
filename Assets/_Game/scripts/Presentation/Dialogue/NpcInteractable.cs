using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class NpcInteractable : MonoBehaviour
    {
        [SerializeField] private NpcDialogueDefinition dialogueDefinition;
        [SerializeField] private Animator npcAnimator;

        public string DisplayName => dialogueDefinition.DisplayName;
        public NpcDialogueDefinition DialogueDefinition => dialogueDefinition;

        /// <summary>
        /// 返回当前 NPC 是否存在符合任务状态的可播放对话。
        /// </summary>
        public bool CanInteract()
        {
            return dialogueDefinition.TryGetAvailableBranch(out _);
        }

        /// <summary>
        /// 播放对话行配置的 NPC 动画触发器。
        /// </summary>
        /// <param name="animationTrigger">Animator 中配置的触发器名称。</param>
        public void PlayAnimationTrigger(string animationTrigger)
        {
            if (npcAnimator != null && !string.IsNullOrWhiteSpace(animationTrigger))
                npcAnimator.SetTrigger(animationTrigger);
        }
    }
}
