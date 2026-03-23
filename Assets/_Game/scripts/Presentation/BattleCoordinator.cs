using Pokemon.Application;
using Pokemon.Domain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Settings")]
        [SerializeField] private float stepDelaySeconds = 1.5f;

        [Header("Views")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        public ItemData testPotion;

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
            PlayerParty.AddItem(testPotion, 3);

            if (PlayerParty.ActivePokemon == null)
            {
                PlayerParty.ActivePokemon = new MonsterRuntime(playerSpecies, 5);
            }
            _player = PlayerParty.ActivePokemon;
            _enemy = new MonsterRuntime(enemySpecies, Random.Range(3, 7));

            _turnUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));
            _playerSkills = new List<SkillData>();

            foreach (var skill in _player.CurrentPP.Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);
            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
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

            List<TurnStep> steps = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);
            StartCoroutine(PlayTurnRoutine(steps));
        }

        public void HandleUseItem(ItemData item)
        {
            if (_battleEnded) return;
            if (PlayerParty.UseItem(item))
            {
                StartCoroutine(ItemTurnRoutine(item));
            }
        }

        private IEnumerator ItemTurnRoutine(ItemData item)
        {
            uiController.SetInteractable(false);

            if (item.Type == ItemType.HealHP)
            {
                _player.Heal(item.EffectValue);
                uiController.SetLog($"使用了 {item.DisplayName}！{_player.Species.DisplayName} 恢复了生命值。");
                uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
                yield return new WaitForSeconds(stepDelaySeconds);
            }

            if (!_enemy.IsFainted)
            {
                var enemySkill = _enemy.Species.InitialSkills[Random.Range(0, _enemy.Species.InitialSkills.Count)];
                uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 乘机发起了攻击！");

                ExecuteTurnUseCase mockUseCase = new ExecuteTurnUseCase(new DamageCalculator(typeChart));
                var steps = mockUseCase.Execute(_player, null, _enemy, enemySkill);
                yield return StartCoroutine(PlayTurnRoutine(steps));
            }
        }

        private IEnumerator PlayTurnRoutine(List<TurnStep> steps)
        {
            uiController.SetInteractable(false);

            foreach (var step in steps)
            {
                // 如果这一步没有任何消息，就跳过 UI 更新
                if (string.IsNullOrEmpty(step.Message)) continue;

                uiController.SetLog(step.Message);
                uiController.UpdateHp(step.PlayerHpAfter, _player.MaxHP, step.EnemyHpAfter, _enemy.MaxHP);// 更新血条（使用当前步数值）

                if (step.AnimType == StepAnimType.PlayerAttack) yield return playerView.PlayAttackAnimation(true);
                else if (step.AnimType == StepAnimType.EnemyAttack) yield return enemyView.PlayAttackAnimation(false);
                else if (step.AnimType == StepAnimType.EnemyHit) yield return enemyView.PlayHitAnimation();
                else if (step.AnimType == StepAnimType.PlayerHit) yield return playerView.PlayHitAnimation();

                yield return new WaitForSeconds(stepDelaySeconds);

                if (step.IsBattleEnd)
                {
                    _battleEnded = true;
                    uiController.SetInteractable(false);

                    if (step.PlayerWon)
                    {
                        // 1. 努力值获得
                        _player.AddEVs(
                            _enemy.Species.EvYieldHP,
                            _enemy.Species.EvYieldAttack,
                            _enemy.Species.EvYieldDefense,
                            _enemy.Species.EvYieldSpeed,
                            _enemy.Species.EvYieldSpAttack,
                            _enemy.Species.EvYieldSpDefense);

                        // 2. 经验结算
                        int gainedExp = (_enemy.Species.BaseExpYield * _enemy.Level) / 7;
                        uiController.SetLog($"获得了 {gainedExp} 点经验值！");
                        yield return new WaitForSeconds(stepDelaySeconds);

                        // --- 修复点：调用不带 out 参数的 AddExp ---
                        bool leveledUp = _player.AddExp(gainedExp);

                        if (leveledUp)
                        {
                            uiController.SetLog($"{_player.Species.DisplayName} 升到了 Lv.{_player.Level}！");
                            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
                            yield return new WaitForSeconds(stepDelaySeconds);

                            uiController.SetLog($"数值得到了提升！攻击: {_player.Attack}, 速度: {_player.Speed}");
                            yield return new WaitForSeconds(stepDelaySeconds);
                        }
                    }
                    else
                    {
                        // --- 修正逻辑：只有 PlayerWon 为 false 才是战败 ---
                        uiController.SetLog("你眼前一黑...");
                        yield return new WaitForSeconds(stepDelaySeconds);
                    }

                    uiController.SetLog("正在寻找下一个对手...");
                    yield return new WaitForSeconds(2f);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    yield break;
                }
            }

            if (!_battleEnded)
            {
                uiController.SetLog("请选择下一步行动。");
                uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
                uiController.SetInteractable(true);
            }
        }

        private SkillData PickFirstAvailableSkill(MonsterRuntime monster)
        {
            foreach (var pair in monster.CurrentPP)
            {
                if (pair.Key != null && pair.Value > 0) return pair.Key;
            }
            return null;
        }
    }
}