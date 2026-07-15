using Pokemon.Domain;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class BattleUIController : MonoBehaviour
    {
        // --- 面板引用 ---
        [Header("菜单控制")]
        [SerializeField] private GameObject mainActionPanel; // 包含主按钮的父物体
        [SerializeField] private GameObject skillPanel;      // 包含技能按钮的父物体
        [SerializeField] private GameObject itemPanel;       // 包含道具列表的父物体

        [Header("主菜单按钮")]
        [SerializeField] private Button fightBtn;    // 战斗按钮
        [SerializeField] private Button bagBtn;      // 道具按钮
        [SerializeField] private Button runBtn;      // 逃走按钮
        [SerializeField] private Button skillBackBtn; // 技能页面的"返回"按钮
        [SerializeField] private Button BagBackBtn;  // 道具页面的"返回"按钮

        // --- UI文本 ---
        [Header("UI文本")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text logText;

        [Header("UI技能按钮")]
        [SerializeField] private Button[] skillButtons;
        [SerializeField] private TMP_Text[] skillBtnTexts;

        [Header("UI道具按钮")]
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private TMP_Text[] itemBtnTexts;

        // --- 事件 ---
        public event Action<int> OnSkillClicked;
        public event Action<ItemData> OnItemClicked;
        public event Action OnRunClicked;

        private List<ItemData> _cachedItems;

        private void Awake()
        {
            // 主菜单：切换到技能面板
            fightBtn.onClick.AddListener(() => ShowSubPanel(skillPanel));

            // 主菜单：进入道具面板
            bagBtn.onClick.AddListener(() => {
                RefreshItemList();
                ShowSubPanel(itemPanel);
            });

            // 返回按钮
            skillBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));
            BagBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));

            // 逃跑
            runBtn.onClick.AddListener(() => OnRunClicked?.Invoke());

            // 技能按钮绑定
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i;
                skillButtons[i].onClick.AddListener(() => OnSkillClicked?.Invoke(index));
            }

            // 道具按钮绑定
            for (int i = 0; i < itemButtons.Length; i++)
            {
                int index = i;
                itemButtons[i].onClick.AddListener(() => {
                    if (index < _cachedItems.Count)
                        ItemButtonCallback(_cachedItems[index]);
                });
            }
        }

        /// <summary>
        /// 切换面板的通用方法
        /// </summary>
        private void ShowSubPanel(GameObject targetPanel)
        {
            mainActionPanel.SetActive(targetPanel == mainActionPanel);
            skillPanel.SetActive(targetPanel == skillPanel);
            if (itemPanel != null) itemPanel.SetActive(targetPanel == itemPanel);
        }

        /// <summary>
        /// 战斗开始时，强制关闭所有面板
        /// </summary>
        public void HideAllPanels()
        {
            mainActionPanel.SetActive(false);
            skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
        }

        /// <summary>
        /// 重置到主菜单面板
        /// </summary>
        public void ResetToMain()
        {
            mainActionPanel.SetActive(true);
            skillPanel.SetActive(false);
            itemPanel?.SetActive(false);
        }

        /// <summary>
        /// 道具按钮点击回调
        /// </summary>
        public void ItemButtonCallback(ItemData item)
        {
            OnItemClicked?.Invoke(item);
        }

        /// <summary>
        /// 刷新道具列表（从 PlayerParty.Inventory 读取）
        /// </summary>
        public void RefreshItemList()
        {
            _cachedItems = PlayerParty.GetUsableItems();

            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (i < _cachedItems.Count)
                {
                    ItemData item = _cachedItems[i];
                    int count = PlayerParty.Inventory[item];
                    itemButtons[i].gameObject.SetActive(true);
                    itemButtons[i].interactable = count > 0;
                    itemBtnTexts[i].text = $"{item.DisplayName} x{count}";
                }
                else
                {
                    itemButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void SetupNames(string playerName, string enemyName)
        {
            playerNameText.text = playerName;
            enemyNameText.text = enemyName;
        }

        public void UpdateHp(int playerHp, int playerMax, int enemyHp, int enemyMax)
        {
            playerHpText.text = $"HP: {playerHp}/{playerMax}";
            enemyHpText.text = $"HP: {enemyHp}/{enemyMax}";
        }

        public void SetLog(string message)
        {
            Debug.Log($"[UI LOG] 正在尝试显示: {message}");
            logText.text = message;
        }

        public void SetInteractable(bool interactable)
        {
            foreach (var btn in skillButtons)
            {
                if (btn.gameObject.activeSelf)
                    btn.interactable = interactable;
            }
            foreach (var btn in itemButtons)
            {
                if (btn.gameObject.activeSelf)
                    btn.interactable = interactable;
            }
        }

        /// <summary>
        /// 技能UI刷新
        /// </summary>
        public void RefreshSkills(List<SkillData> skills, IReadOnlyDictionary<SkillData, int> ppMap)
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (i < skills.Count)
                {
                    SkillData skill = skills[i];
                    int currentPP = ppMap[skill];

                    skillButtons[i].gameObject.SetActive(true);
                    skillButtons[i].interactable = currentPP > 0;
                    skillBtnTexts[i].text = $"{skill.DisplayName} ({currentPP})";
                }
                else
                {
                    skillButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
