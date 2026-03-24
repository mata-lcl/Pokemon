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

            // 1. 订阅 UI 事件
            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.OnItemClicked += HandleUseItem;      // 订阅道具点击
            uiController.OnRunClicked += HandleRunAttempt;    // 订阅逃跑点击

            // 2. 初始 UI 设置
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);
            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
            uiController.RefreshSkills(_playerSkills, _player.CurrentPP);

            // 3. 确保初始显示主菜单
            uiController.ResetToMain();
            uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 出现了！");
        }

        private void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnSkillClicked -= HandlePlayerAction;
                uiController.OnItemClicked -= HandleUseItem;
                uiController.OnRunClicked -= HandleRunAttempt;
            }
        }

        private void HandlePlayerAction(int skillIndex)
        {
            if (_battleEnded) return;

            // 立即隐藏 UI，进入演算阶段
            uiController.HideAllPanels();

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            List<TurnStep> steps = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);
            StartCoroutine(PlayTurnRoutine(steps));
        }
        // 逃跑处理
        private void HandleRunAttempt()
        {
            if (_battleEnded) return;
            uiController.HideAllPanels();

            // 简单逻辑：直接成功。复杂逻辑可以加随机率或速度判定
            uiController.SetLog("逃跑成功！");
            Invoke("ResetScene", 1.5f);
        }

        public void HandleUseItem(ItemData item)
        {
            if (_battleEnded) return;

            if (item is IUsable usable)
            {
                var context = new EffectContext { User = _player, Target = _enemy, Steps = new List<TurnStep>() };

                if (usable.CanUse(context))
                {
                    //uiController.HideAllActionUI(); // 隐藏 UI
                    usable.OnUse(context);

                    if (usable.IsConsumable) PlayerParty.RemoveItem(item, 1);

                    // 执行道具流程
                    StartCoroutine(ItemTurnRoutine(context.Steps));
                }
                else
                {
                    uiController.SetLog("现在无法使用该道具！");
                }
            }
        }

        private IEnumerator ItemTurnRoutine(List<TurnStep> itemSteps)
        {
            uiController.SetInteractable(false);

            // 1. 播放道具使用的动画和文字信息
            yield return StartCoroutine(PlayTurnRoutine(itemSteps));

            // 2. 如果战斗没结束且敌人没倒下，敌人进行反击
            if (!_battleEnded && !_enemy.IsFainted)
            {
                uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 趁机发起了攻击！");
                yield return new WaitForSeconds(stepDelaySeconds);

                SkillData enemySkill = PickFirstAvailableSkill(_enemy);

                // 注意：这里只执行敌人的单方面行动
                // 为此你可以给 ExecuteTurnUseCase 增加一个简单的单目标执行方法，或者通过 null 传参
                List<TurnStep> counterAttackSteps = _turnUseCase.Execute(null, null, _enemy, enemySkill);

                yield return StartCoroutine(PlayTurnRoutine(counterAttackSteps));
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
                // 回合结束，重置到主菜单供玩家重新选择
                uiController.SetLog("要做什么？");
                uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
                uiController.ResetToMain(); // 这里调用你 UI 控制器里的切换到主面板的方法
                uiController.SetInteractable(true); // 恢复交互
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