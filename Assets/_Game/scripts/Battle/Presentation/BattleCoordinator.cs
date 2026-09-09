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

        [Header("初始道具（拖入对应的 ScriptableObject 资产）")]
        [SerializeField] private ItemData defaultPokeball;
        [SerializeField] private int defaultPokeballCount = 5;

        [Header("UI Reference")]
        [SerializeField] private BattleUIController uiController;
        [SerializeField] private BattleTurnPlayback turnPlayback;

        [Header("Settings")]
        [Tooltip("测试模式：开启后战斗结束会继续匹配下一只，关闭则返回世界场景")]
        [SerializeField] private bool testMode = false;

        private BattleSession _session;
        private BattleOutcomeService _outcomeService;
        private bool _inputLocked;

        public MonsterRuntime CurrentPlayer => _session?.Player;
        public MonsterRuntime CurrentEnemy => _session?.Enemy;

        private void Start()
        {
            if (uiController == null || turnPlayback == null) return;

            InitBattle();
            turnPlayback.SetupCombatants(_session.Player, _session.Enemy);
            InitBattleUI();
        }

        private void InitBattle()
        {
            var sessionFactory = new BattleSessionFactory();
            _session = sessionFactory.Create(
                playerSpecies,
                enemySpecies,
                typeChart,
                defaultPokeball,
                defaultPokeballCount);
            _outcomeService = new BattleOutcomeService();

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.OnPlaybackModeClicked += HandlePlaybackModeClicked;
            uiController.OnItemClicked += HandleUseItem;      // 订阅道具点击
            uiController.OnRunClicked += HandleRunAttempt;    // 订阅逃跑点击
            uiController.OnPokemonClicked += HandlePokemonClicked;
            uiController.OnPokemonSwitchCancelled += HandlePokemonSwitchCancelled;
            uiController.OnPokemonSelected += HandlePokemonSelected;
        }

        /// <summary>
        /// 初始化战斗界面的双方信息、技能菜单和提示文本。
        /// </summary>
        private void InitBattleUI()
        {
            uiController.SetupNames(
                _session.Player.Species.DisplayName,
                _session.Enemy.Species.DisplayName);
            RefreshBattlePresentation();
            uiController.RefreshSkills(
                _session.PlayerSkills,
                _session.Player.CurrentPP);
            uiController.ResetToMain();
            uiController.SetLog($"野生的 {_session.Enemy.Species.DisplayName} 出现了！");
        }

        private void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnSkillClicked -= HandlePlayerAction;
                uiController.OnPlaybackModeClicked -= HandlePlaybackModeClicked;
                uiController.OnItemClicked -= HandleUseItem;
                uiController.OnRunClicked -= HandleRunAttempt;
                uiController.OnPokemonClicked -= HandlePokemonClicked;
                uiController.OnPokemonSwitchCancelled -= HandlePokemonSwitchCancelled;
                uiController.OnPokemonSelected -= HandlePokemonSelected;
            }
        }

        /// <summary>
        /// 将当前战斗双方的生命值和状态同步到战斗界面。
        /// </summary>
        public void RefreshBattlePresentation()
        {
            if (_session == null) return;

            uiController.UpdateHp(
                _session.Player.CurrentHP,
                _session.Player.MaxHP,
                _session.Enemy.CurrentHP,
                _session.Enemy.MaxHP);
            uiController.UpdatePokemonIndicators(_session.Player, _session.Enemy);
        }

        /// <summary>
        /// 响应播放模式按钮并交由表现控制器切换播放模式。
        /// </summary>
        private void HandlePlaybackModeClicked()
        {
            turnPlayback.CyclePlaybackMode();
        }

        /// <summary>
        /// 辅助方法，用于检测输入
        /// Returns false when a battle action is already being played.
        /// </summary>
        private bool TryBeginInput()
        {
            if (_session.BattleEnded || _inputLocked)
                return false;

            _inputLocked = true;
            uiController.SetInteractable(false);
            return true;
        }

        /// <summary>
        /// 解锁UI
        /// </summary>
        private void UnlockInput()
        {
            if (_session.BattleEnded) return;

            _inputLocked = false;
            uiController.SetInteractable(true);
        }

        /// <summary>
        /// 响应玩家技能按钮，锁定输入后启动完整技能回合播放协程。
        /// </summary>
        /// <param name="skillIndex">玩家技能列表中的下标。</param>
        private void HandlePlayerAction(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= _session.PlayerSkills.Count)
                return;

            if (!TryBeginInput())
                return;

            // 立即隐藏 UI，进入演算阶段
            uiController.HideAllPanels();

            SkillData playerSkill = _session.PlayerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_session.Enemy);

            List<TurnStep> steps = _session.TurnUseCase.Execute(
                _session.Player,
                playerSkill,
                _session.Enemy,
                enemySkill);
            StartCoroutine(SkillTurnRoutine(steps));
        }

        // 逃跑处理
        private void HandlePokemonClicked()
        {
            if (_session.BattleEnded || _inputLocked) return;

            List<MonsterRuntime> switchable = PlayerParty.GetSwitchablePokemon();
            if (switchable.Count == 0)
            {
                uiController.SetLog("没有可替换的精灵！");
                return;
            }

            _inputLocked = true;
            uiController.SetInteractable(false);
            uiController.ShowPokemonSelection(
                PlayerParty.GetPartySnapshot(),
                PlayerParty.ActivePokemon ?? _session.Player);
        }

        private void HandlePokemonSwitchCancelled()
        {
            if (!_inputLocked || _session.BattleEnded) return;

            uiController.ResetToMain();
            UnlockInput();
        }

        private void HandlePokemonSelected(MonsterRuntime selectedPokemon)
        {
            if (!_inputLocked || selectedPokemon == null || selectedPokemon == _session.Player)
                return;

            MonsterRuntime previousPokemon = _session.Player;
            if (!PlayerParty.TrySetActivePokemon(selectedPokemon))
                return;

            _session.SetPlayer(selectedPokemon);
            RefreshPlayerPresentation();
            uiController.HideAllPanels();
            StartCoroutine(PokemonSwitchTurnRoutine(previousPokemon));
        }

        private void RefreshPlayerPresentation()
        {
            _session.RefreshPlayerSkills();
            turnPlayback.RefreshPlayer(_session.Player);
            uiController.SetupNames(
                _session.Player.Species.DisplayName,
                _session.Enemy.Species.DisplayName);
            RefreshBattlePresentation();
            uiController.RefreshSkills(
                _session.PlayerSkills,
                _session.Player.CurrentPP);
        }

        private IEnumerator PokemonSwitchTurnRoutine(MonsterRuntime previousPokemon)
        {
            uiController.SetLog($"{previousPokemon.Species.DisplayName} 回到精灵球，{_session.Player.Species.DisplayName} 出战！");
            yield return turnPlayback.WaitForStep();

            if (!_session.BattleEnded && !_session.Enemy.IsFainted)
            {
                uiController.SetLog($"野生的 {_session.Enemy.Species.DisplayName} 发起了攻击！");
                yield return turnPlayback.WaitForStep();

                SkillData enemySkill = PickFirstAvailableSkill(_session.Enemy);
                List<TurnStep> counterAttackSteps = _session.TurnUseCase.ExecuteSingleAction(
                    _session.Player,
                    _session.Enemy,
                    enemySkill,
                    isPlayerAction: false);

                if (!counterAttackSteps.Exists(step => step.IsBattleEnd))
                {
                    counterAttackSteps.AddRange(
                        _session.TurnUseCase.ExecuteEndOfTurn(
                            _session.Player,
                            _session.Enemy));
                }

                yield return PlayTurnRoutine(counterAttackSteps);
            }

            if (!_session.BattleEnded)
                FinishTurn();
        }

        private void HandleRunAttempt()
        {
            if (!TryBeginInput()) return;
            uiController.HideAllPanels();

            // 简单逻辑：直接成功。复杂逻辑可以加随机率或速度判定
            uiController.SetLog("逃跑成功！");
            //Invoke(nameof(ReturnToWorld), 1.5f);
            SceneTransitionManager.Instance.ReturnToWorld();
        }

        /// <summary>
        /// 响应玩家道具按钮，执行道具效果并启动完整道具回合协程。
        /// </summary>
        /// <param name="item">玩家选择使用的道具资产。</param>
        public void HandleUseItem(ItemData item)
        {
            if (!TryBeginInput()) return;
            uiController.HideAllPanels();

            if (item is IUsable usable)
            {
                var context = new EffectContext
                {
                    User = _session.Player,
                    Target = _session.Enemy,
                    Steps = new List<TurnStep>(),
                    Operations = _session.TurnUseCase.Operations,
                    RuntimeEffects = _session.TurnUseCase.RuntimeEffects,
                    PlayerRef = _session.Player,
                    EnemyRef = _session.Enemy
                };

                if (usable.CanUse(context))
                {
                    usable.OnUse(context);

                    if (usable.IsConsumable) PlayerParty.RemoveItem(item, 1);

                    // 执行道具流程
                    StartCoroutine(ItemTurnRoutine(context.Steps));
                }
                else
                {
                    uiController.SetLog("现在无法使用该道具！");
                    uiController.ResetToMain();
                    UnlockInput();
                }
            }
            else
            {
                uiController.ResetToMain();
                UnlockInput();
            }
        }

        /// <summary>
        /// 播放道具步骤、处理捕捉结果、执行敌方反击和回合末结算。
        /// </summary>
        /// <param name="itemSteps">道具使用产生的播放步骤。</param>
        private IEnumerator ItemTurnRoutine(List<TurnStep> itemSteps)
        {
            uiController.SetInteractable(false);

            // 1. 播放道具使用的动画和文字信息
            yield return PlayTurnRoutine(itemSteps);

            // 2. 检查是否捕捉成功
            if (itemSteps.Exists(step => step.CaughtSuccess))
            {
                // 将精灵加入背包
                PlayerParty.TryAddMonster(new MonsterRuntime(
                    _session.Enemy.Species,
                    _session.Enemy.Level));
                _session.BattleEnded = true;

                uiController.SetLog($"{_session.Enemy.Species.DisplayName} 加入了背包！");
                yield return turnPlayback.WaitForStep();
                SceneTransitionManager.Instance.ReturnToWorld();
                yield break;
            }

            // 3. 如果战斗没结束且敌人没倒下，敌人进行反击
            if (!_session.BattleEnded && !_session.Enemy.IsFainted)
            {
                uiController.SetLog($"野生的 {_session.Enemy.Species.DisplayName} 趁机发起了攻击！");
                yield return turnPlayback.WaitForStep();

                SkillData enemySkill = PickFirstAvailableSkill(_session.Enemy);
                List<TurnStep> counterAttackSteps = _session.TurnUseCase.ExecuteSingleAction(
                    _session.Player,
                    _session.Enemy,
                    enemySkill,
                    isPlayerAction: false);

                // 敌方行动没有结束战斗时，继续结算中毒、灼伤等回合末效果。
                if (!counterAttackSteps.Exists(step => step.IsBattleEnd))
                {
                    counterAttackSteps.AddRange(
                        _session.TurnUseCase.ExecuteEndOfTurn(
                            _session.Player,
                            _session.Enemy));
                }

                yield return PlayTurnRoutine(counterAttackSteps);
            }

            if (!_session.BattleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 播放普通技能回合的全部步骤，并在完整回合结束后重新开放操作。
        /// </summary>
        /// <param name="steps">回合执行器生成的播放步骤。</param>
        private IEnumerator SkillTurnRoutine(List<TurnStep> steps)
        {
            yield return PlayTurnRoutine(steps);

            if (!_session.BattleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 播放传入的步骤，并在发现战斗结束步骤时启动对应结算。
        /// </summary>
        /// <param name="steps">需要依次显示和播放的步骤。</param>
        private IEnumerator PlayTurnRoutine(List<TurnStep> steps)
        {
            yield return turnPlayback.PlaySteps(steps);

            if (!TryGetBattleEndStep(steps, out TurnStep battleEndStep))
                yield break;

            _session.BattleEnded = true;
            uiController.SetInteractable(false);

            // 捕捉成功：直接跳出，由 ItemTurnRoutine 处理后续逻辑
            if (battleEndStep.CaughtSuccess)
                yield break;

            yield return ResolveBattleEndRoutine(battleEndStep);
        }

        /// <summary>
        /// 处理战斗胜负结算、结算文本和离开战斗场景的流程。
        /// </summary>
        /// <param name="battleEndStep">描述本场战斗胜负结果的结束步骤。</param>
        private IEnumerator ResolveBattleEndRoutine(TurnStep battleEndStep)
        {
            if (battleEndStep.PlayerWon)
            {
                BattleOutcome outcome = _outcomeService.ResolveVictory(
                    _session.Player,
                    _session.Enemy);

                uiController.SetLog($"获得了 {outcome.GainedExperience} 点经验值！");
                yield return turnPlayback.WaitForStep();

                if (outcome.LeveledUp)
                {
                    uiController.SetLog($"{_session.Player.Species.DisplayName} 升到了 Lv.{_session.Player.Level}！");
                    RefreshBattlePresentation();
                    yield return turnPlayback.WaitForStep();

                    uiController.SetLog($"数值得到了提升！攻击: {_session.Player.Attack}, 速度: {_session.Player.Speed}");
                    yield return turnPlayback.WaitForStep();
                }
            }
            else
            {
                // --- 修正逻辑：只有 PlayerWon 为 false 才是战败 ---
                uiController.SetLog("你眼前一黑...");
                yield return turnPlayback.WaitForStep();
            }

            uiController.SetLog("正在返回...");
            yield return turnPlayback.WaitForDuration(2f);

            if (testMode)
            {
                // 测试模式：继续匹配下一只野生精灵
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                // 正常流程：返回世界场景
                SceneTransitionManager.Instance.ReturnToWorld();
            }
        }

        /// <summary>
        /// 从一组回合步骤中查找首个战斗结束步骤。
        /// </summary>
        /// <param name="steps">需要检查的回合步骤。</param>
        /// <param name="battleEndStep">找到的首个战斗结束步骤。</param>
        /// <returns>找到战斗结束步骤时返回 true。</returns>
        private static bool TryGetBattleEndStep(
            IReadOnlyList<TurnStep> steps,
            out TurnStep battleEndStep)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (!steps[i].IsBattleEnd) continue;

                battleEndStep = steps[i];
                return true;
            }

            battleEndStep = default;
            return false;
        }

        /// <summary>
        /// 在完整回合播放结束后刷新菜单并释放输入锁。
        /// </summary>
        private void FinishTurn()
        {
            if (_session.BattleEnded)
                return;

            _session.RefreshPlayerSkills();
            uiController.SetLog("要做什么？");
            uiController.RefreshSkills(
                _session.PlayerSkills,
                _session.Player.CurrentPP);
            turnPlayback.RefreshPlaybackModeLabel();
            uiController.ResetToMain();
            UnlockInput();
        }

        /// <summary>
        /// 返回敌方当前第一个仍有 PP 的技能。
        /// </summary>
        /// <param name="monster">需要选择技能的敌方宝可梦。</param>
        /// <returns>第一个可用技能；没有可用技能时返回 null。</returns>
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
