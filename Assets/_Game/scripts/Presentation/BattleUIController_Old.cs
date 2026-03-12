using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class BattleUIController_Old : MonoBehaviour
    {
        /// <summary>
        /// 序列化
        /// 通过Unity编辑器拖拽赋值，实现数据与逻辑分离
        /// </summary>
        [Header("数据")]
        [SerializeField] private PokemonSpeciesData playerSpecies;
        [SerializeField] private PokemonSpeciesData enemySpecies;
        [SerializeField] private TypeChartData typeChart;

        [Header("UI文本")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text logText;

        [Header("技能按钮(最多4个)")]
        [SerializeField] private Button[] skillButtons;      // 按顺序拖4个按钮
        [SerializeField] private TMP_Text[] skillBtnTexts;   // 每个按钮上的文字（同顺序）

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private DamageCalculator _damageCalculator;
        private ExecuteTurnUseCase_Old _turnUseCase;

        private readonly List<SkillData> _playerSkills = new List<SkillData>();
        private bool _battleEnded;

        private void Start()
        {
            if (!ValidateRefs())
            {
                enabled = false;
                return;
            }

            _player = new MonsterRuntime(playerSpecies);
            _enemy = new MonsterRuntime(enemySpecies);

            _damageCalculator = new DamageCalculator(typeChart);
            _turnUseCase = new ExecuteTurnUseCase_Old(_damageCalculator);

            playerNameText.text = _player.Species.DisplayName;
            enemyNameText.text = _enemy.Species.DisplayName;

            BuildPlayerSkillList();
            BindSkillButtons();
            RefreshHpUI();
            SetLog("战斗开始！请选择技能。");
        }

        private bool ValidateRefs()
        {
            if (playerSpecies == null || enemySpecies == null)
            {
                Debug.LogError("[BattleUIController] playerSpecies/enemySpecies 未绑定。");
                return false;
            }

            if (skillButtons == null || skillBtnTexts == null || skillButtons.Length == 0)
            {
                Debug.LogError("[BattleUIController] 请绑定技能按钮和按钮文字。");
                return false;
            }

            if (skillButtons.Length != skillBtnTexts.Length)
            {
                Debug.LogError("[BattleUIController] skillButtons 与 skillBtnTexts 数量需一致。");
                return false;
            }

            return true;
        }

        private void BuildPlayerSkillList()
        {
            _playerSkills.Clear();

            foreach (var kv in _player.GetSkillPP())
            {
                if (kv.Key != null)
                    _playerSkills.Add(kv.Key);
            }
        }

        /// <summary>
        /// 技能按钮绑定，动态激活/禁用按钮：有技能的按钮显示，多余的隐藏
        /// 关键技巧：int idx = i解决循环变量闭包问题？
        /// </summary>
        private void BindSkillButtons()
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int idx = i;
                skillButtons[i].onClick.RemoveAllListeners();

                if (i < _playerSkills.Count)
                {
                    SkillData skill = _playerSkills[i];
                    skillButtons[i].gameObject.SetActive(true);
                    skillButtons[i].interactable = true;        //设置按钮为可交互
                    skillBtnTexts[i].text = skill.DisplayName;

                    skillButtons[i].onClick.AddListener(() => OnClickPlayerSkill(idx));
                }
                else
                {
                    skillButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnClickPlayerSkill(int skillIndex)
        {
            if (_battleEnded) return;
            if (skillIndex < 0 || skillIndex >= _playerSkills.Count) return;

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            if (enemySkill == null)
            {
                EndBattle(playerWon: true, "敌人PP耗尽，无法行动。你赢了！");
                return;
            }

            var result = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);

            string log = "";
            if (result.PlayerActed)
                log += $"我方使用 {playerSkill.DisplayName}：{(result.PlayerHit ? "命中" : "未命中")} 伤害 {result.DamageToEnemy}\n";
            if (result.EnemyActed)
                log += $"敌方使用 {enemySkill.DisplayName}：{(result.EnemyHit ? "命中" : "未命中")} 伤害 {result.DamageToPlayer}\n";

            RefreshHpUI();
            RefreshSkillButtonState();
            SetLog(log);

            if (result.BattleEnded)
            {
                EndBattle(result.PlayerWon, result.PlayerWon ? "战斗结束，你赢了！" : "战斗结束，你输了！");
            }
        }

        private void RefreshHpUI()
        {
            playerHpText.text = $"HP: {_player.CurrentHP}/{_player.Species.BaseHP}";
            enemyHpText.text = $"HP: {_enemy.CurrentHP}/{_enemy.Species.BaseHP}";
        }

        private void RefreshSkillButtonState()
        {
            for (int i = 0; i < _playerSkills.Count && i < skillButtons.Length; i++)
            {
                SkillData skill = _playerSkills[i];
                bool canUse = _player.CanUseSkill(skill);
                skillButtons[i].interactable = canUse;

                int pp = _player.GetSkillPP()[skill];
                skillBtnTexts[i].text = $"{skill.DisplayName} ({pp})";
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            IReadOnlyDictionary<SkillData, int> ppMap = monster.GetSkillPP();
            foreach (var pair in ppMap)
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null;
        }

        private void EndBattle(bool playerWon, string msg)
        {
            _battleEnded = true;
            SetLog(msg);

            for (int i = 0; i < skillButtons.Length; i++)
                skillButtons[i].interactable = false;
        }

        private void SetLog(string msg)
        {
            logText.text = msg;
            Debug.Log(msg);
        }
    }
}