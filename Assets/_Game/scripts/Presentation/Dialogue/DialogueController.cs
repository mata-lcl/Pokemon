using System.Collections;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class DialogueController : MonoBehaviour
    {
        [Header("界面")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject continueIndicator;

        [Header("立绘")]
        [SerializeField] private GameObject leftPortraitRoot;
        [SerializeField] private Image leftPortraitImage;
        [SerializeField] private GameObject rightPortraitRoot;
        [SerializeField] private Image rightPortraitImage;

        [Header("播放")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private AudioSource voiceAudioSource;
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 35f;
        [SerializeField] private KeyCode advanceKey = KeyCode.Space;
        [SerializeField] private KeyCode alternateAdvanceKey = KeyCode.Return;

        private NpcInteractable _currentNpc;
        private DialogueBranchDefinition _currentBranch;
        private int _lineIndex;
        private int _openedFrame;
        private bool _isTyping;
        private Coroutine _typingRoutine;

        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 初始化时隐藏对话界面和两侧立绘。
        /// </summary>
        private void Awake()
        {
            dialogueRoot.SetActive(false);
            if (leftPortraitRoot != null)
                leftPortraitRoot.SetActive(false);
            if (rightPortraitRoot != null)
                rightPortraitRoot.SetActive(false);
        }

        /// <summary>
        /// 对话播放期间响应继续按键或确认输入。
        /// </summary>
        private void Update()
        {
            if (!IsPlaying || Time.frameCount == _openedFrame)
                return;

            if (Input.GetKeyDown(advanceKey) ||
                Input.GetKeyDown(alternateAdvanceKey) ||
                Input.GetButtonDown("Submit"))
                Advance();
        }

        /// <summary>
        /// 控制器禁用时关闭当前对话并恢复玩家移动。
        /// </summary>
        private void OnDisable()
        {
            if (IsPlaying)
                CloseDialogue();
        }

        /// <summary>
        /// 播放指定 NPC 当前满足任务状态的对话分支。
        /// </summary>
        /// <param name="npc">玩家当前交互的 NPC。</param>
        public bool Play(NpcInteractable npc)
        {
            if (IsPlaying ||
                !npc.DialogueDefinition.TryGetAvailableBranch(out DialogueBranchDefinition branch) ||
                branch.Lines.Count == 0)
                return false;

            _currentNpc = npc;
            _currentBranch = branch;
            _lineIndex = 0;
            _openedFrame = Time.frameCount;
            IsPlaying = true;
            dialogueRoot.SetActive(true);
            playerMovement.SetMovementEnabled(false);
            ShowCurrentLine();
            return true;
        }

        /// <summary>
        /// 完成立即显示当前文字，或切换到下一句对话。
        /// </summary>
        public void Advance()
        {
            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            _lineIndex++;
            if (_lineIndex >= _currentBranch.Lines.Count)
            {
                CompleteDialogue();
                return;
            }

            ShowCurrentLine();
        }

        /// <summary>
        /// 根据当前对话行刷新说话人、立绘、语音、动画和文字。
        /// </summary>
        private void ShowCurrentLine()
        {
            DialogueLineDefinition line = _currentBranch.Lines[_lineIndex];
            speakerNameText.text = string.IsNullOrWhiteSpace(line.SpeakerName)
                ? _currentNpc.DialogueDefinition.DisplayName
                : line.SpeakerName;
            Sprite portrait = line.Portrait != null
                ? line.Portrait
                : _currentNpc.DialogueDefinition.DefaultPortrait;
            ShowPortrait(portrait, line.PortraitSide);

            if (voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                if (line.VoiceClip != null)
                {
                    voiceAudioSource.clip = line.VoiceClip;
                    voiceAudioSource.Play();
                }
            }

            _currentNpc.PlayAnimationTrigger(line.AnimationTrigger);
            dialogueText.text = line.Text;
            dialogueText.maxVisibleCharacters = 0;
            if (continueIndicator != null)
                continueIndicator.SetActive(false);
            _typingRoutine = StartCoroutine(TypeCurrentLine());
        }

        /// <summary>
        /// 按当前文字速度逐字显示完整对话文本。
        /// </summary>
        private IEnumerator TypeCurrentLine()
        {
            _isTyping = true;
            dialogueText.ForceMeshUpdate();
            int characterCount = dialogueText.textInfo.characterCount;
            float visibleCharacters = 0f;
            while (dialogueText.maxVisibleCharacters < characterCount)
            {
                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                dialogueText.maxVisibleCharacters = Mathf.Min(
                    characterCount,
                    Mathf.FloorToInt(visibleCharacters));
                yield return null;
            }

            _isTyping = false;
            _typingRoutine = null;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        /// <summary>
        /// 跳过当前逐字播放并立即显示完整文本。
        /// </summary>
        private void FinishTyping()
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
            _isTyping = false;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        /// <summary>
        /// 根据对话配置在左侧或右侧显示当前说话人的立绘。
        /// </summary>
        /// <param name="portrait">当前对话行使用的立绘。</param>
        /// <param name="side">立绘显示在对话框的左侧或右侧。</param>
        private void ShowPortrait(Sprite portrait, DialoguePortraitSide side)
        {
            bool showLeft = portrait != null && side == DialoguePortraitSide.Left;
            bool showRight = portrait != null && side == DialoguePortraitSide.Right;
            if (leftPortraitRoot != null)
                leftPortraitRoot.SetActive(showLeft);
            if (rightPortraitRoot != null)
                rightPortraitRoot.SetActive(showRight);
            if (showLeft && leftPortraitImage != null)
                leftPortraitImage.sprite = portrait;
            if (showRight && rightPortraitImage != null)
                rightPortraitImage.sprite = portrait;
        }

        /// <summary>
        /// 执行对话分支配置的任务操作并关闭对话界面。
        /// </summary>
        private void CompleteDialogue()
        {
            switch (_currentBranch.CompletionAction)
            {
                case DialogueCompletionAction.AcceptQuest:
                    QuestService.AcceptQuest(_currentBranch.Quest);
                    break;
                case DialogueCompletionAction.ReportNpcTalked:
                    QuestService.ReportNpcTalked(_currentNpc.DialogueDefinition.NpcId);
                    break;
                case DialogueCompletionAction.SubmitQuest:
                    QuestService.SubmitQuest(_currentBranch.Quest);
                    break;
                case DialogueCompletionAction.CompleteNpcQuest:
                    QuestService.AcceptQuest(_currentBranch.Quest);
                    QuestService.ReportNpcTalked(_currentNpc.DialogueDefinition.NpcId);
                    QuestService.SubmitQuest(_currentBranch.Quest);
                    break;
            }

            CloseDialogue();
        }

        /// <summary>
        /// 隐藏对话界面、停止语音并恢复玩家移动。
        /// </summary>
        private void CloseDialogue()
        {
            if (_typingRoutine != null)
                StopCoroutine(_typingRoutine);
            _typingRoutine = null;
            _isTyping = false;
            IsPlaying = false;
            dialogueRoot.SetActive(false);
            if (voiceAudioSource != null)
                voiceAudioSource.Stop();
            playerMovement.SetMovementEnabled(true);
            _currentNpc = null;
            _currentBranch = null;
        }
    }
}
