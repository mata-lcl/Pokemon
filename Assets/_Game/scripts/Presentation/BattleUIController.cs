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
        [Header("UI文本")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text logText;

        [Header("UI技能按钮")]
        [SerializeField] private Button[] skillButtons;
        [SerializeField] private TMP_Text[] skillBtnTexts;

        /// <summary>
        /// 向外暴露玩家点击事件
        /// 核心设计：公开一个事件，将用户输入转发给外部
        /// 实现了观察者模式(Observer Pattern)，UI不处理业务逻辑
        /// </summary>
        public event Action<int> OnSkillClicked;    //委托，参数是技能索引

        private void Awake()
        {
            // 初始化按钮监听，将点击事件转发给外部（协调器）
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i;
                skillButtons[i].onClick.AddListener(() =>
                {
                    OnSkillClicked?.Invoke(index);
                });
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