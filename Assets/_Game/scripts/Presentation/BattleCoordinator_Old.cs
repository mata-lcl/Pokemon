using Pokemon.Application;
using Pokemon.Domain;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleCoordinator_Old : MonoBehaviour
    {
        [Header("数据")]
        [SerializeField] private PokemonSpeciesData playerSpecies;
        [SerializeField] private PokemonSpeciesData enemySpecies;
        [SerializeField] private TypeChartData typeChart;

        [Header("UI Reference")]
        [SerializeField] private BattleUIController uiController;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private ExecuteTurnUseCase_Old _turnUseCase;
        private List<SkillData> _playerSkills;
        private bool _battleEnded;

        private void Start()
        {
            if (uiController == null)
            {
                Debug.LogError("[BattleCoordinator] UI Controller 未绑定！");
                return;
            }

            InitBattle();
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        private void InitBattle()
        {
            _player = new MonsterRuntime(playerSpecies);
            _enemy = new MonsterRuntime(enemySpecies);

            _turnUseCase = new ExecuteTurnUseCase_Old(new DamageCalculator(typeChart));

            // 提取玩家技能列表（固定顺序）
            _playerSkills = new List<SkillData>();
            foreach (var skill in _player.GetSkillPP().Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            // 监听UI点击事件
            uiController.OnSkillClicked += HandlePlayerAction;

            // 初始化UI表现
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);
            uiController.UpdateHp(_player.CurrentHP, _player.Species.BaseHP, _enemy.CurrentHP, _enemy.Species.BaseHP);
            uiController.RefreshSkills(_playerSkills, _player.GetSkillPP());
            uiController.SetLog("战斗开始！请选择技能。");
        }

        private void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnSkillClicked -= HandlePlayerAction;
            }
        }

        private void HandlePlayerAction(int skillIndex)
        {
            if (_battleEnded) return;
            if (skillIndex < 0 || skillIndex >= _playerSkills.Count) return;

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            if (enemySkill == null)
            {
                EndBattle("敌人PP耗尽，无法行动。你赢了！");
                return;
            }

            // 锁定UI，防止连点（这在后续加动画时极其重要）
            uiController.SetInteractable(false);

            // 执行核心回合逻辑
            var result = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);

            // 构建战报日志
            string log = "";
            if (result.PlayerActed)
                log += $"{_player.Species.DisplayName}使用 {playerSkill.DisplayName}：{(result.PlayerHit ? "命中" : "未命中")} 伤害 {result.DamageToEnemy}\n";
            if (result.EnemyActed)
                log += $"{_enemy.Species.DisplayName}使用 {enemySkill.DisplayName}：{(result.EnemyHit ? "命中" : "未命中")} 伤害 {result.DamageToPlayer}\n";

            // 更新UI
            uiController.UpdateHp(_player.CurrentHP, _player.Species.BaseHP, _enemy.CurrentHP, _enemy.Species.BaseHP);
            uiController.RefreshSkills(_playerSkills, _player.GetSkillPP());
            uiController.SetLog(log.TrimEnd());

            if (result.BattleEnded)
            {
                EndBattle(result.PlayerWon ? "战斗结束，你赢了！" : "战斗结束，你输了！");
            }
            else
            {
                // 战斗没结束，刷新技能状态并解锁UI
                uiController.RefreshSkills(_playerSkills, _player.GetSkillPP());
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            foreach (var pair in monster.GetSkillPP())
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null;
        }

        private void EndBattle(string finalMessage)
        {
            _battleEnded = true;
            uiController.SetLog(finalMessage);
            uiController.SetInteractable(false);
        }
    }
}