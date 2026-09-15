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
        private BattleCommandService _commandService;
        private BattleOutcomeService _outcomeService;
        private BattlePlaybackPlanBuilder _playbackPlanBuilder;
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
            PokemonSpeciesData encounterSpecies = null;
            int encounterLevel = 0;
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TryGetPendingEncounter(
                    out encounterSpecies,
                    out encounterLevel);
            }

            _session = sessionFactory.Create(
                playerSpecies,
                enemySpecies,
                typeChart,
                defaultPokeball,
                defaultPokeballCount,
                encounterSpecies,
                encounterLevel);
            _commandService = new BattleCommandService();
            _outcomeService = new BattleOutcomeService();
            _playbackPlanBuilder = new BattlePlaybackPlanBuilder();

            uiController.OnSkillClicked += HandlePlayerAction;
            uiController.OnPlaybackModeClicked += HandlePlaybackModeClicked;
            uiController.OnBagClicked += HandleBagClicked;
            uiController.OnItemClicked += HandleUseItem;      // 订阅道具点击
            uiController.OnItemReorderRequested += HandleItemReorderRequested;
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
                uiController.OnBagClicked -= HandleBagClicked;
                uiController.OnItemClicked -= HandleUseItem;
                uiController.OnItemReorderRequested -= HandleItemReorderRequested;
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
        /// 读取当前可使用道具并交给战斗界面显示。
        /// </summary>
        private void HandleBagClicked()
        {
            uiController.ShowBagPanel(
                _commandService.GetUsableItemSnapshot());
        }

        /// <summary>
        /// 通过应用层调整道具顺序，并使用最新快照刷新战斗背包。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        private void HandleItemReorderRequested(
            ItemData item,
            ItemData targetItem)
        {
            if (_commandService.TryReorderItem(item, targetItem))
            {
                uiController.ShowBagPanel(
                    _commandService.GetUsableItemSnapshot());
            }
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
            if (!_commandService.CanExecuteSkillTurn(_session, skillIndex))
                return;

            if (!TryBeginInput())
                return;

            // 立即隐藏 UI，进入演算阶段
            uiController.HideAllPanels();

            _commandService.TryExecuteSkillTurn(
                _session,
                skillIndex,
                out TurnExecutionResult result);
            StartCoroutine(SkillTurnRoutine(result));
        }

        // 逃跑处理
        private void HandlePokemonClicked()
        {
            if (_session.BattleEnded || _inputLocked) return;

            IReadOnlyList<MonsterRuntime> switchable =
                _commandService.GetSwitchablePokemon();
            if (switchable.Count == 0)
            {
                uiController.SetLog("没有可替换的精灵！");
                return;
            }

            _inputLocked = true;
            uiController.SetInteractable(false);
            uiController.ShowPokemonSelection(
                _commandService.GetPartySnapshot(),
                _session.Player);
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

            if (!_commandService.TrySwitchPokemon(
                    _session,
                    selectedPokemon,
                    out MonsterRuntime previousPokemon))
                return;

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

            if (_commandService.CanExecuteEnemyResponse(_session))
            {
                uiController.SetLog($"野生的 {_session.Enemy.Species.DisplayName} 发起了攻击！");
                yield return turnPlayback.WaitForStep();

                _commandService.TryExecuteEnemyResponse(
                    _session,
                    out TurnExecutionResult counterAttackResult);
                yield return PlayTurnRoutine(counterAttackResult);
            }

            if (!_session.BattleEnded)
                FinishTurn();
        }

        private void HandleRunAttempt()
        {
            if (!TryBeginInput()) return;
            uiController.HideAllPanels();

            if (_commandService.TryEscape(_session))
            {
                // 简单逻辑：直接成功。复杂逻辑可以加随机率或速度判定
                uiController.SetLog("逃跑成功！");
                //Invoke(nameof(ReturnToWorld), 1.5f);
                SceneTransitionManager.Instance.ReturnToWorld();
            }
        }

        /// <summary>
        /// 响应玩家道具按钮，执行道具效果并启动完整道具回合协程。
        /// </summary>
        /// <param name="item">玩家选择使用的道具资产。</param>
        public void HandleUseItem(ItemData item)
        {
            if (!TryBeginInput()) return;
            uiController.HideAllPanels();

            if (_commandService.TryExecuteItem(
                    _session,
                    item,
                    out ItemUseExecutionResult result))
            {
                if (result.Succeeded)
                {
                    // 执行道具流程
                    StartCoroutine(ItemTurnRoutine(result.BattleResult));
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
        /// <param name="itemResult">道具使用产生的业务结果和领域事件。</param>
        private IEnumerator ItemTurnRoutine(TurnExecutionResult itemResult)
        {
            uiController.SetInteractable(false);

            // 1. 播放道具使用的动画和文字信息
            yield return PlayTurnRoutine(itemResult);

            // 2. 检查是否捕捉成功
            if (itemResult.Conclusion == BattleConclusion.Captured)
            {
                // 将精灵加入背包
                _commandService.CompleteCapture(_session);

                uiController.SetLog($"{_session.Enemy.Species.DisplayName} 加入了背包！");
                yield return turnPlayback.WaitForStep();
                SceneTransitionManager.Instance.ReturnToWorld();
                yield break;
            }

            // 3. 如果战斗没结束且敌人没倒下，敌人进行反击
            if (_commandService.CanExecuteEnemyResponse(_session))
            {
                uiController.SetLog($"野生的 {_session.Enemy.Species.DisplayName} 趁机发起了攻击！");
                yield return turnPlayback.WaitForStep();

                _commandService.TryExecuteEnemyResponse(
                    _session,
                    out TurnExecutionResult counterAttackResult);
                yield return PlayTurnRoutine(counterAttackResult);
            }

            if (!_session.BattleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 播放普通技能回合的全部步骤，并在完整回合结束后重新开放操作。
        /// </summary>
        /// <param name="result">回合执行器生成的业务结果和领域事件。</param>
        private IEnumerator SkillTurnRoutine(TurnExecutionResult result)
        {
            yield return PlayTurnRoutine(result);

            if (!_session.BattleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 将领域事件转换为播放步骤，并根据业务结果启动对应结算。
        /// </summary>
        /// <param name="result">需要播放并处理的战斗执行结果。</param>
        private IEnumerator PlayTurnRoutine(TurnExecutionResult result)
        {
            List<BattlePlaybackStep> steps = _playbackPlanBuilder.Build(
                result,
                _session.Player,
                _session.Enemy);
            yield return turnPlayback.PlaySteps(steps);

            if (result.Conclusion == BattleConclusion.Ongoing)
                yield break;

            _session.BattleEnded = true;
            uiController.SetInteractable(false);

            // 捕捉成功：直接跳出，由 ItemTurnRoutine 处理后续逻辑
            if (result.Conclusion == BattleConclusion.Captured)
                yield break;

            yield return ResolveBattleEndRoutine(result.Conclusion);
        }

        /// <summary>
        /// 处理战斗胜负结算、结算文本和离开战斗场景的流程。
        /// </summary>
        /// <param name="conclusion">应用层已经确定的战斗结论。</param>
        private IEnumerator ResolveBattleEndRoutine(BattleConclusion conclusion)
        {
            if (conclusion == BattleConclusion.PlayerVictory)
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
                // --- 修正逻辑：只有应用层明确返回玩家胜利时才执行胜利结算 ---
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

    }
}
