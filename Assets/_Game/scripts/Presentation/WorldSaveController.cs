using Pokemon.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class WorldSaveController : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotTexts;
        [SerializeField] private GameObject overwriteDialog;
        [SerializeField] private TMP_Text overwritePromptText;
        [SerializeField] private Button confirmOverwriteButton;
        [SerializeField] private Button cancelOverwriteButton;
        [SerializeField] private TMP_Text statusText;

        private int _pendingSlotIndex = -1;

        /// <summary>
        /// 绑定存档栏及覆盖确认按钮事件。
        /// </summary>
        private void Awake()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotIndex = i;
                slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
            }

            confirmOverwriteButton.onClick.AddListener(ConfirmOverwrite);
            cancelOverwriteButton.onClick.AddListener(CancelOverwrite);
        }

        /// <summary>
        /// 每次打开存档页面时刷新全部存档栏信息。
        /// </summary>
        private void OnEnable()
        {
            _pendingSlotIndex = -1;
            overwriteDialog.SetActive(false);
            statusText.text = string.Empty;
            RefreshSlots();
        }

        /// <summary>
        /// 处理存档栏选择；已有存档时先显示覆盖确认。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private void SelectSlot(int slotIndex)
        {
            if (SaveGameService.HasSave(slotIndex))
            {
                _pendingSlotIndex = slotIndex;
                overwritePromptText.text = $"存档栏 {slotIndex + 1} 已有存档，是否覆盖？";
                overwriteDialog.SetActive(true);
                return;
            }

            SaveToSlot(slotIndex);
        }

        /// <summary>
        /// 确认覆盖当前待处理的存档栏。
        /// </summary>
        private void ConfirmOverwrite()
        {
            int slotIndex = _pendingSlotIndex;
            CancelOverwrite();
            SaveToSlot(slotIndex);
        }

        /// <summary>
        /// 取消覆盖并关闭确认弹窗。
        /// </summary>
        private void CancelOverwrite()
        {
            _pendingSlotIndex = -1;
            overwriteDialog.SetActive(false);
        }

        /// <summary>
        /// 将当前游戏信息保存到指定存档栏并刷新显示。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private void SaveToSlot(int slotIndex)
        {
            SaveGameService.Save(slotIndex, playerTransform.position);
            statusText.text = $"已保存到存档栏 {slotIndex + 1}";
            RefreshSlots();
        }

        /// <summary>
        /// 刷新三个存档栏的时间、场景和队伍信息。
        /// </summary>
        private void RefreshSlots()
        {
            var summaries = SaveGameService.GetSlotSummaries();
            for (int i = 0; i < slotTexts.Length; i++)
            {
                SaveSlotSummary summary = summaries[i];
                slotTexts[i].text = summary.HasSave
                    ? $"存档栏 {i + 1}\n{summary.SavedAt}  {summary.SceneName}  队伍 {summary.PartyCount}"
                    : $"存档栏 {i + 1}\n空存档";
            }
        }
    }
}
