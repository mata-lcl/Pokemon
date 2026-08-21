using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class NpcInteractor : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMP_Text interactionPromptText;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        private readonly List<NpcInteractable> _nearbyNpcs = new List<NpcInteractable>();
        private NpcInteractable _currentNpc;

        /// <summary>
        /// 初始化时隐藏 NPC 交互提示。
        /// </summary>
        private void Awake()
        {
            interactionPrompt.SetActive(false);
        }

        /// <summary>
        /// 每帧选择最近的可交互 NPC，并响应交互按键。
        /// </summary>
        private void Update()
        {
            RefreshCurrentNpc();
            RefreshPrompt();

            if (_currentNpc != null &&
                !dialogueController.IsPlaying &&
                Input.GetKeyDown(interactionKey))
            {
                dialogueController.Play(_currentNpc);
                RefreshPrompt();
            }
        }

        /// <summary>
        /// 玩家进入 NPC 交互范围时登记该 NPC。
        /// </summary>
        /// <param name="other">进入玩家触发器的碰撞体。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            NpcInteractable npc = other.GetComponentInParent<NpcInteractable>();
            if (npc != null && !_nearbyNpcs.Contains(npc))
                _nearbyNpcs.Add(npc);
        }

        /// <summary>
        /// 玩家离开 NPC 交互范围时移除该 NPC。
        /// </summary>
        /// <param name="other">离开玩家触发器的碰撞体。</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            NpcInteractable npc = other.GetComponentInParent<NpcInteractable>();
            if (npc != null)
                _nearbyNpcs.Remove(npc);
        }

        /// <summary>
        /// 从交互范围内选择距离玩家最近且存在可用对话的 NPC。
        /// </summary>
        private void RefreshCurrentNpc()
        {
            _currentNpc = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < _nearbyNpcs.Count; i++)
            {
                NpcInteractable npc = _nearbyNpcs[i];
                if (!npc.CanInteract())
                    continue;

                float distance = (npc.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _currentNpc = npc;
                }
            }
        }

        /// <summary>
        /// 根据当前 NPC 和对话播放状态刷新交互提示。
        /// </summary>
        private void RefreshPrompt()
        {
            bool visible = _currentNpc != null && !dialogueController.IsPlaying;
            interactionPrompt.SetActive(visible);
            if (visible)
                interactionPromptText.text = $"按 {interactionKey} 与 {_currentNpc.DisplayName} 对话";
        }
    }
}
