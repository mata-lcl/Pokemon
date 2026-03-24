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
        // --- 新增：面板引用 ---
        [Header("菜单控制")]
        [SerializeField] private GameObject mainActionPanel; // 包含 4 个主按钮的父物体
        [SerializeField] private GameObject skillPanel;      // 包含技能按钮的父物体
        [SerializeField] private GameObject itemPanel;       // 包含道具列表的父物体

        [Header("主菜单按钮")]
        [SerializeField] private Button fightBtn;    // 战斗按钮
        [SerializeField] private Button bagBtn;      // 道具按钮
        [SerializeField] private Button runBtn;      // 逃走按钮
        [SerializeField] private Button skillBackBtn; // 技能页面的“返回”按钮

        // --- 原有槽位保留 ---
        [Header("UI文本")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text logText;

        [Header("UI技能按钮")]
        [SerializeField] private Button[] skillButtons;
        [SerializeField] private TMP_Text[] skillBtnTexts;

        // --- 事件 ---
        public event Action<int> OnSkillClicked;
        public event Action<ItemData> OnItemClicked;
        public event Action OnRunClicked; // 新增：逃跑点击事件

        /// <summary>
        /// 向外暴露玩家点击事件
        /// 核心设计：公开一个事件，将用户输入转发给外部
        /// 实现了观察者模式(Observer Pattern)，UI不处理业务逻辑
        /// </summary>

        private void Awake()
        {
            // --- 核心逻辑修改 ---

            // 1. 主菜单点击：切换到技能面板
            fightBtn.onClick.AddListener(() => ShowSubPanel(skillPanel));

            // 2. 主菜单点击：进入背包（可以在这里调用刷新背包列表的方法）
            bagBtn.onClick.AddListener(() => {
                //RefreshItemList(); //TODO
                ShowSubPanel(itemPanel);
            });

            // 3. 返回按钮：从技能/道具页面回到主菜单
            skillBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));

            // 4. 逃跑
            runBtn.onClick.AddListener(() => OnRunClicked?.Invoke());

            // 5. 保留原有技能按钮初始化（索引转发）
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i;
                skillButtons[i].onClick.AddListener(() => OnSkillClicked?.Invoke(index));
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
        /// 战斗开始执行时，强制关闭所有面板
        /// </summary>
        public void HideAllPanels()
        {
            mainActionPanel.SetActive(false);
            skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
        }

        // 当你想让玩家重新选择时（例如回合结束），调用这个
        public void ResetToMain()
        {
            // 1. 必须显示包含“战斗/道具/逃跑”按钮的父物体
            mainActionPanel.SetActive(true);

            // 2. 隐藏其他的子菜单面板
            skillPanel.SetActive(false);
            itemPanel?.SetActive(false);
        }
        // 在道具 UI 按钮点击的代码逻辑中触发它
        public void ItemButtonCallback(ItemData item)
        {
            OnItemClicked?.Invoke(item);
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
                // 仅当按钮处于激活状态时才改变交互性
                if (btn.gameObject.activeSelf)
                {
                    btn.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// 技能UI刷新，遍历所有技能槽，
        /// 将有对应技能：激活按钮，设置交互性（PP>0），更新文本
        /// </summary>
        /// <param 要显示的技能列表="skills"></param>
        /// <param 技能到PP值的映射="ppMap"></param>
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