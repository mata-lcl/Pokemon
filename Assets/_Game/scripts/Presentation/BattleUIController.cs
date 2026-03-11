using Pokemon.Domain;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterLike.Presentation
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

        // 向外暴露玩家点击事件
        public event Action<int> OnSkillClicked;

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