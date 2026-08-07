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

        [Header("Settings")]
        [SerializeField] private float stepDelaySeconds = 1.5f;
        [Tooltip("加速倍率,倍率越小加速越快")]
        [SerializeField, Range(0.05f, 1f)] private float fastPlaybackMultiplier = 0.25f;
        [Tooltip("测试模式：开启后战斗结束会继续匹配下一只，关闭则返回世界场景")]
        [SerializeField] private bool testMode = false;

        [Header("Views")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private ExecuteTurnUseCase _turnUseCase;
        private List<SkillData> _playerSkills;
        private bool _battleEnded;
        private bool _inputLocked;
        private PlaybackMode _playbackMode = PlaybackMode.Normal;

        private void Start()
        {
            if (uiController == null) return;
            InitBattle();
            if (playerView != null) playerView.Setup(playerSpecies.BattleSprite);
            if (enemyView != null) enemyView.Setup(enemySpecies.BattleSprite);
        }

        private void InitBattle()
        {
            // 初始化默认道具（仅首次）
            if (defaultPokeball != null && !PlayerParty.Inventory.ContainsKey(defaultPokeball))
            {
                PlayerParty.AddItem(defaultPokeball, defaultPokeballCount);
            }

            // Normalize legacy data first, then use the first party member as the
            // default active Pokemon when no valid active selection exists.
            PlayerParty.NormalizeParty();
            if (PlayerParty.Party.Count == 0)
                PlayerParty.AddMonster(new MonsterRuntime(playerSpecies, 5));
            PlayerParty.EnsureActivePokemon();
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
            uiController.OnPlaybackModeClicked += HandlePlaybackModeClicked;
            uiController.OnItemClicked += HandleUseItem;      // 订阅道具点击
            uiController.OnRunClicked += HandleRunAttempt;    // 订阅逃跑点击

            // 2. 初始 UI 设置
            uiController.OnPokemonClicked += HandlePokemonClicked;
            uiController.OnPokemonSwitchCancelled += HandlePokemonSwitchCancelled;
            uiController.OnPokemonSelected += HandlePokemonSelected;

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
                uiController.OnPlaybackModeClicked -= HandlePlaybackModeClicked;
                uiController.OnItemClicked -= HandleUseItem;
                uiController.OnRunClicked -= HandleRunAttempt;
                uiController.OnPokemonClicked -= HandlePokemonClicked;
                uiController.OnPokemonSwitchCancelled -= HandlePokemonSwitchCancelled;
                uiController.OnPokemonSelected -= HandlePokemonSelected;
            }
        }


        /// <summary>
        /// Cycles playback mode without changing battle state or rules.
        /// </summary>
        private void HandlePlaybackModeClicked()
        {
            switch (_playbackMode)
            {
                case PlaybackMode.Normal:
                    _playbackMode = PlaybackMode.Fast;
                    break;
                case PlaybackMode.Fast:
                    _playbackMode = PlaybackMode.SkipWait;
                    break;
                default:
                    _playbackMode = PlaybackMode.Normal;
                    break;
            }

            uiController.SetPlaybackModeLabel(GetPlaybackModeLabel());
        }

        /// <summary>
        /// Returns the label shown by the playback mode button.
        /// </summary>
        private string GetPlaybackModeLabel()
        {
            switch (_playbackMode)
            {
                case PlaybackMode.Fast:
                    return "Fast";
                case PlaybackMode.SkipWait:
                    return "Skip";
                default:
                    return "Normal";
            }
        }

        /// <summary>
        /// Waits for a step according to the selected playback mode.
        /// </summary>
        private IEnumerator WaitForPlayback(float duration)
        {
            if (_playbackMode == PlaybackMode.SkipWait)
            {
                yield return null;
                yield break;
            }

            float multiplier = _playbackMode == PlaybackMode.Fast
                ? Mathf.Clamp(fastPlaybackMultiplier, 0.05f, 1f)
                : 1f;
            yield return new WaitForSeconds(Mathf.Max(0f, duration * multiplier));
        }

        /// <summary>
        /// Plays the animation for a step, or skips only the visual part.
        /// </summary>
        private IEnumerator PlayStepAnimation(TurnStep step)
        {
            if (_playbackMode == PlaybackMode.SkipWait)
            {
                yield return null;
                yield break;
            }

            float speedMultiplier = _playbackMode == PlaybackMode.Fast
                ? 1f / Mathf.Clamp(fastPlaybackMultiplier, 0.05f, 1f)
                : 1f;

            if (step.AnimType == StepAnimType.PlayerAttack && playerView != null)
                yield return playerView.PlayAttackAnimation(true, speedMultiplier);
            else if (step.AnimType == StepAnimType.EnemyAttack && enemyView != null)
                yield return enemyView.PlayAttackAnimation(false, speedMultiplier);
            else if (step.AnimType == StepAnimType.EnemyHit && enemyView != null)
                yield return enemyView.PlayHitAnimation(speedMultiplier);
            else if (step.AnimType == StepAnimType.PlayerHit && playerView != null)
                yield return playerView.PlayHitAnimation(speedMultiplier);
        }

        /// <summary>
        /// 辅助方法，用于检测输入
        /// Returns false when a battle action is already being played.
        /// </summary>
        private bool TryBeginInput()
        {
            if(_battleEnded || _inputLocked)
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
            if (_battleEnded) return;

            _inputLocked = false;
            uiController.SetInteractable(true);
        }

        /// <summary>
        /// 响应玩家技能按钮，锁定输入后启动完整技能回合播放协程。
        /// </summary>
        /// <param name="skillIndex">玩家技能列表中的下标。</param>
        private void HandlePlayerAction(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= _playerSkills.Count)
                return;

            if (!TryBeginInput())
                return;

            // 立即隐藏 UI，进入演算阶段
            uiController.HideAllPanels();

            SkillData playerSkill = _playerSkills[skillIndex];
            SkillData enemySkill = PickFirstAvailableSkill(_enemy);

            List<TurnStep> steps = _turnUseCase.Execute(_player, playerSkill, _enemy, enemySkill);
            StartCoroutine(SkillTurnRoutine(steps));
        }

        // 逃跑处理
        private void HandlePokemonClicked()
        {
            if (_battleEnded || _inputLocked) return;

            List<MonsterRuntime> switchable = PlayerParty.GetSwitchablePokemon();
            if (switchable.Count == 0)
            {
                uiController.SetLog("没有可替换的精灵！");
                return;
            }

            _inputLocked = true;
            uiController.SetInteractable(false);
            uiController.ShowPokemonSelection(PlayerParty.Party, PlayerParty.ActivePokemon ?? _player);
        }

        private void HandlePokemonSwitchCancelled()
        {
            if (!_inputLocked || _battleEnded) return;

            uiController.ResetToMain();
            UnlockInput();
        }

        private void HandlePokemonSelected(MonsterRuntime selectedPokemon)
        {
            if (!_inputLocked || selectedPokemon == null || selectedPokemon == _player)
                return;

            MonsterRuntime previousPokemon = _player;
            if (!PlayerParty.TrySetActivePokemon(selectedPokemon))
                return;

            _player = selectedPokemon;
            RefreshPlayerPresentation();
            uiController.HideAllPanels();
            StartCoroutine(PokemonSwitchTurnRoutine(previousPokemon));
        }

        private void RefreshPlayerPresentation()
        {
            _playerSkills.Clear();
            foreach (SkillData skill in _player.CurrentPP.Keys)
            {
                if (skill != null) _playerSkills.Add(skill);
            }

            if (playerView != null) playerView.Setup(_player.Species.BattleSprite);
            uiController.SetupNames(_player.Species.DisplayName, _enemy.Species.DisplayName);
            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
            uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
        }

        private IEnumerator PokemonSwitchTurnRoutine(MonsterRuntime previousPokemon)
        {
            uiController.SetLog($"{previousPokemon.Species.DisplayName} 回到精灵球，{_player.Species.DisplayName} 出战！");
            yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

            if (!_battleEnded && !_enemy.IsFainted)
            {
                uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 发起了攻击！");
                yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

                SkillData enemySkill = PickFirstAvailableSkill(_enemy);
                List<TurnStep> counterAttackSteps = _turnUseCase.ExecuteSingleAction(
                    _player, _enemy, enemySkill, isPlayerAction: false);

                if (!counterAttackSteps.Exists(step => step.IsBattleEnd))
                    counterAttackSteps.AddRange(_turnUseCase.ExecuteEndOfTurn(_player, _enemy));

                yield return StartCoroutine(PlayTurnRoutine(counterAttackSteps));
            }

            if (!_battleEnded)
                FinishTurn();
        }

        private void HandleRunAttempt()
        {
            if(!TryBeginInput()) return;
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
            if(!TryBeginInput()) return;
            uiController.HideAllPanels();

            if (item is IUsable usable)
            {
                var context = new EffectContext
                {
                    User = _player,
                    Target = _enemy,
                    Steps = new List<TurnStep>(),
                    IsPlayerAttacking = true,
                    PlayerRef = _player,
                    EnemyRef = _enemy
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
            yield return StartCoroutine(PlayTurnRoutine(itemSteps));

            // 2. 检查是否捕捉成功
            if (itemSteps.Exists(s => s.CaughtSuccess))
            {
                // 将精灵加入背包
                PlayerParty.AddMonster(new MonsterRuntime(_enemy.Species, _enemy.Level));
                _battleEnded = true;

                uiController.SetLog($"{_enemy.Species.DisplayName} 加入了背包！");
                yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));
                SceneTransitionManager.Instance.ReturnToWorld();
                yield break;
            }

            // 3. 如果战斗没结束且敌人没倒下，敌人进行反击
            if (!_battleEnded && !_enemy.IsFainted)
            {
                uiController.SetLog($"野生的 {_enemy.Species.DisplayName} 趁机发起了攻击！");
                yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

                SkillData enemySkill = PickFirstAvailableSkill(_enemy);
                List<TurnStep> counterAttackSteps = _turnUseCase.ExecuteSingleAction(
                    _player, _enemy, enemySkill, isPlayerAction: false);

                // 敌方行动没有结束战斗时，继续结算中毒、灼伤等回合末效果。
                if (!counterAttackSteps.Exists(step => step.IsBattleEnd))
                {
                    counterAttackSteps.AddRange(_turnUseCase.ExecuteEndOfTurn(_player, _enemy));
                }

                yield return StartCoroutine(PlayTurnRoutine(counterAttackSteps));
            }

            if (!_battleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 播放普通技能回合的全部步骤，并在完整回合结束后重新开放操作。
        /// </summary>
        /// <param name="steps">回合执行器生成的播放步骤。</param>
        private IEnumerator SkillTurnRoutine(List<TurnStep> steps)
        {
            yield return StartCoroutine(
                PlayTurnRoutine(steps));

            if (!_battleEnded)
                FinishTurn();
        }

        /// <summary>
        /// 只播放传入的步骤，不负责判断整个技能或道具回合是否结束。
        /// </summary>
        /// <param name="steps">需要依次显示和播放的步骤。</param>
        private IEnumerator PlayTurnRoutine(List<TurnStep> steps)
        {
            // uiController.SetInteractable(false);

            foreach (var step in steps)
            {
                // 如果这一步没有任何消息，就跳过 UI 更新
                if (string.IsNullOrEmpty(step.Message)) continue;

                uiController.SetLog(step.Message);
                uiController.UpdateHp(step.PlayerHpAfter, _player.MaxHP, step.EnemyHpAfter, _enemy.MaxHP);// 更新血条（使用当前步数值）

                yield return StartCoroutine(PlayStepAnimation(step));
                yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

                if (step.IsBattleEnd)
                {
                    _battleEnded = true;
                    uiController.SetInteractable(false);

                    // 捕捉成功：直接跳出，由 ItemTurnRoutine 处理后续逻辑
                    if (step.CaughtSuccess)
                        yield break;

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
                        yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

                        // --- 修复点：调用不带 out 参数的 AddExp ---
                        bool leveledUp = _player.AddExp(gainedExp);

                        if (leveledUp)
                        {
                            uiController.SetLog($"{_player.Species.DisplayName} 升到了 Lv.{_player.Level}！");
                            uiController.UpdateHp(_player.CurrentHP, _player.MaxHP, _enemy.CurrentHP, _enemy.MaxHP);
                            yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));

                            uiController.SetLog($"数值得到了提升！攻击: {_player.Attack}, 速度: {_player.Speed}");
                            yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));
                        }
                    }
                    else
                    {
                        // --- 修正逻辑：只有 PlayerWon 为 false 才是战败 ---
                        uiController.SetLog("你眼前一黑...");
                        yield return StartCoroutine(WaitForPlayback(stepDelaySeconds));
                    }

                    uiController.SetLog("正在返回...");
                    yield return StartCoroutine(WaitForPlayback(2f));

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
                    yield break;
                }

            }

        }

        /// <summary>
        /// 在完整回合播放结束后刷新菜单并释放输入锁。
        /// </summary>
        private void FinishTurn()
        {
            if (_battleEnded)
                return;

            uiController.SetLog("要做什么？");
            uiController.RefreshSkills(_playerSkills, _player.CurrentPP);
            uiController.SetPlaybackModeLabel(GetPlaybackModeLabel());
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
