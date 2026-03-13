using Pokemon.Application;
using Pokemon.Domain;
// 注意这里的命名空间我统一帮你改成了 Pokemon.Presentation 保持一致
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation
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

        [Header("Views")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private ExecuteTurnUseCase _turnUseCase;
        private List<SkillData> _playerSkills;
        private bool _battleEnded;

        private void Start()
        {
            if (uiController == null) return;
            InitBattle();
            if (playerView != null) playerView.Setup(playerSpecies.BattleSprite);
            if (enemyView != null) enemyView.Setup(enemySpecies.BattleSprite);
        }

        private void InitBattle()
        {
            // 【修复1】这里必须传入等级，我们暂定双方都是 5 级
            _player = new MonsterRuntime(playerSpecies, 5);
            _enemy = new MonsterRuntime(enemySpecies, 5);

            _turnUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));

            _playerSkills = new List<SkillData>();

            // 【修复3】GetSkillPP() 替换为了公有属性 CurrentPP
            foreach (var skill in _player.CurrentPP.Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);

            // 【修复2】界面的最大血量不再是 Species.BaseHP，而是经过个体值和等级计算后的 MaxHP
            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);

            // 【修复3】同样替换为 CurrentPP
            uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
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

                // 【修复2】这里也改成 MaxHP
                uiController.UpdateHp(step.PlayerHpAfter, _player.MaxHP, step.EnemyHpAfter, _enemy.MaxHP);

                // 根据标记播放对应动画，并等待动画完成
                if (step.AnimType == StepAnimType.PlayerAttack) yield return playerView.PlayAttackAnimation(true);
                else if (step.AnimType == StepAnimType.EnemyAttack) yield return enemyView.PlayAttackAnimation(false);
                else if (step.AnimType == StepAnimType.EnemyHit) yield return enemyView.PlayHitAnimation();
                else if (step.AnimType == StepAnimType.PlayerHit) yield return playerView.PlayHitAnimation();

                // 停顿，让玩家看清文字
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

                // 【修复3】替换为 CurrentPP
                uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            // 【修复3】替换为 CurrentPP
            foreach (var pair in monster.CurrentPP)
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null;
        }
    }
}