using Pokemon.Application;
using Pokemon.Domain;
using Pokemon.Presentation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterLike.Presentation
{
    public class BattleCoordinator : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PokemonSpeciesData playerSpecies;
        [SerializeField] private PokemonSpeciesData enemySpecies;
        [SerializeField] private TypeChartData typeChart;

        [Header("UI Reference")]
        [SerializeField] private BattleUIController uiController;

        // 设置播放每个步骤的停顿时间
        [Header("Settings")]
        [SerializeField] private float stepDelaySeconds = 1.5f;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private ExecuteTurnUseCase _turnUseCase;
        private List<SkillData> _playerSkills;
        private bool _battleEnded;

        private void Start()
        {
            if (uiController == null) return;
            InitBattle();
        }

        private void InitBattle()
        {
            _player = new MonsterRuntime(playerSpecies);
            _enemy = new MonsterRuntime(enemySpecies);
            _turnUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));

            _playerSkills = new List<SkillData>();
            foreach (var skill in _player.GetSkillPP().Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);
            uiController.UpdateHp(_player.CurrentHP, _player.Species.BaseHP, _enemy.CurrentHP, _enemy.Species.BaseHP);
            uiController.RefreshSkills(_playerSkills, _player.GetSkillPP());
            uiController.SetLog("战斗开始！请选择技能。");
        }

        private void OnDestroy()
        {
            if (uiController != null) uiController.OnSkillClicked -= HandlePlayerAction;
        }

        private void HandlePlayerAction(int skillIndex)
        {
            if (_battleEnded) return;

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            // 瞬间生成本回合的“录像”
            List<TurnStep> steps = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);

            // 启动协程，按时间慢慢播放录像
            StartCoroutine(PlayTurnRoutine(steps));
        }

        private IEnumerator PlayTurnRoutine(List<TurnStep> steps)
        {
            // 锁定UI，防止连点
            uiController.SetInteractable(false);

            foreach (var step in steps)
            {
                // 更新UI显示
                uiController.SetLog(step.Message);
                uiController.UpdateHp(step.PlayerHpAfter, _player.Species.BaseHP, step.EnemyHpAfter, _enemy.Species.BaseHP);

                // 停顿，让玩家看清文字（未来可以在这里播放动画/特效）
                yield return new WaitForSeconds(stepDelaySeconds);

                if (step.IsBattleEnd)
                {
                    _battleEnded = true;
                    uiController.SetInteractable(false);
                    yield break; // 结束协程
                }
            }

            // 回合播放完毕没死，解锁UI并刷新PP文本
            if (!_battleEnded)
            {
                uiController.SetLog("请选择下一步行动。");
                uiController.RefreshSkills(_playerSkills, _player.GetSkillPP());
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            foreach (var pair in monster.GetSkillPP())
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null; // 这里暂不处理敌人也耗尽PP的边角情况
        }
    }
}