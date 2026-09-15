using Pokemon.Domain;
using Pokemon.Presentation.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleTurnPlayback : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private BattleUIController uiController;

        [Header("Views")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        [Header("动画")]
        [SerializeField] private PokemonBattleAnimationCatalog animationCatalog;

        [Header("Settings")]
        [SerializeField] private float stepDelaySeconds = 1.5f;
        [Tooltip("加速倍率,倍率越小加速越快")]
        [SerializeField, Range(0.05f, 1f)] private float fastPlaybackMultiplier = 0.25f;

        private MonsterRuntime _player;
        private MonsterRuntime _enemy;
        private PlaybackMode _playbackMode = PlaybackMode.Normal;

        /// <summary>
        /// 设置当前战斗双方，并初始化双方的战斗视图。
        /// </summary>
        /// <param name="player">当前玩家出战精灵。</param>
        /// <param name="enemy">当前敌方精灵。</param>
        public void SetupCombatants(MonsterRuntime player, MonsterRuntime enemy)
        {
            _player = player;
            _enemy = enemy;
            RefreshPlayer(player);

            if (enemyView != null)
            {
                enemyView.Setup(
                    enemy.Species.BattleSprite,
                    GetAnimationProfile(enemy.Species));
            }

            RefreshPlaybackModeLabel();
        }

        /// <summary>
        /// 刷新当前玩家精灵的战斗视图。
        /// </summary>
        /// <param name="player">新的玩家出战精灵。</param>
        public void RefreshPlayer(MonsterRuntime player)
        {
            _player = player;
            if (playerView != null)
            {
                playerView.Setup(
                    player.Species.BattleSprite,
                    GetAnimationProfile(player.Species));
            }
        }

        /// <summary>
        /// 循环切换普通、快速和跳过等待三种播放模式。
        /// </summary>
        public void CyclePlaybackMode()
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

            RefreshPlaybackModeLabel();
        }

        /// <summary>
        /// 刷新播放模式按钮上显示的文本。
        /// </summary>
        public void RefreshPlaybackModeLabel()
        {
            uiController.SetPlaybackModeLabel(GetPlaybackModeLabel());
        }

        /// <summary>
        /// 按当前播放模式等待一次标准步骤时长。
        /// </summary>
        /// <returns>可由协调器等待的协程。</returns>
        public IEnumerator WaitForStep()
        {
            yield return WaitForDuration(stepDelaySeconds);
        }

        /// <summary>
        /// 按当前播放模式等待指定时长。
        /// </summary>
        /// <param name="duration">普通播放模式下的等待秒数。</param>
        /// <returns>可由协调器等待的协程。</returns>
        public IEnumerator WaitForDuration(float duration)
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
        /// 依次显示并播放回合执行器生成的表现步骤。
        /// </summary>
        /// <param name="steps">需要依次播放的回合步骤。</param>
        /// <returns>播放全部步骤或遇到战斗结束步骤时完成的协程。</returns>
        public IEnumerator PlaySteps(IReadOnlyList<BattlePlaybackStep> steps)
        {
            var presentedImpactStepIndices = new HashSet<int>();
            for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                if (presentedImpactStepIndices.Contains(stepIndex)) continue;

                BattlePlaybackStep step = steps[stepIndex];
                if (string.IsNullOrEmpty(step.Message)) continue;

                PresentTurnStep(step);
                if (IsAttackStep(step))
                {
                    List<int> impactStepIndices = GetImpactStepIndices(steps, stepIndex);
                    int nextImpactStep = 0;

                    Action<SpriteFrameEventContext> frameEventCallback = eventContext =>
                    {
                        if (!string.Equals(
                                eventContext.EventId,
                                SpriteFrameAnimationEventIds.Impact,
                                StringComparison.Ordinal) ||
                            nextImpactStep >= impactStepIndices.Count)
                        {
                            return;
                        }

                        int impactStepIndex = impactStepIndices[nextImpactStep];
                        nextImpactStep++;
                        if (!presentedImpactStepIndices.Add(impactStepIndex)) return;

                        BattlePlaybackStep impactStep = steps[impactStepIndex];
                        PresentTurnStep(impactStep);
                        StartCoroutine(PlayStepAnimation(impactStep));
                    };

                    yield return PlayAttackStepAnimation(step, frameEventCallback);

                    if (nextImpactStep == 0 && impactStepIndices.Count > 0)
                    {
                        int impactStepIndex = impactStepIndices[0];
                        presentedImpactStepIndices.Add(impactStepIndex);

                        BattlePlaybackStep impactStep = steps[impactStepIndex];
                        PresentTurnStep(impactStep);
                        yield return PlayStepAnimation(impactStep);
                    }
                }
                else
                {
                    yield return PlayStepAnimation(step);
                }

                yield return WaitForStep();
            }
        }

        /// <summary>
        /// 返回当前播放模式对应的按钮文本。
        /// </summary>
        /// <returns>普通、快速或跳过等待模式的显示文本。</returns>
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
        /// 播放非技能施放步骤对应的攻击或受击动画。
        /// </summary>
        /// <param name="step">需要播放动画的回合步骤。</param>
        /// <returns>动画播放完成时结束的协程。</returns>
        private IEnumerator PlayStepAnimation(BattlePlaybackStep step)
        {
            if (_playbackMode == PlaybackMode.SkipWait)
            {
                yield return null;
                yield break;
            }

            float speedMultiplier = GetAnimationSpeedMultiplier();

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
        /// 播放技能施放动画，并把序列帧事件转发给当前回合的表现逻辑。
        /// </summary>
        /// <param name="step">包含攻击方和技能分类的施放步骤。</param>
        /// <param name="frameEventCallback">动画到达已配置事件帧时调用的回调。</param>
        /// <returns>技能施放动画完成时结束的协程。</returns>
        private IEnumerator PlayAttackStepAnimation(
            BattlePlaybackStep step,
            Action<SpriteFrameEventContext> frameEventCallback)
        {
            if (_playbackMode == PlaybackMode.SkipWait)
            {
                frameEventCallback?.Invoke(new SpriteFrameEventContext(
                    null,
                    SpriteFrameAnimationEventIds.Impact,
                    -1));
                yield return null;
                yield break;
            }

            bool isPlayer = step.AnimType == StepAnimType.PlayerAttack;
            BattleUnitView attackerView = isPlayer ? playerView : enemyView;

            if (attackerView == null)
            {
                frameEventCallback?.Invoke(new SpriteFrameEventContext(
                    null,
                    SpriteFrameAnimationEventIds.Impact,
                    -1));
                yield return null;
                yield break;
            }

            yield return attackerView.PlaySkillAnimation(
                GetSkillAnimationType(step),
                isPlayer,
                GetAnimationSpeedMultiplier(),
                frameEventCallback);
        }

        /// <summary>
        /// 返回当前播放模式对应的动画速度倍率。
        /// </summary>
        /// <returns>用于战斗视图动画的播放速度倍率。</returns>
        private float GetAnimationSpeedMultiplier()
        {
            return _playbackMode == PlaybackMode.Fast
                ? 1f / Mathf.Clamp(fastPlaybackMultiplier, 0.05f, 1f)
                : 1f;
        }

        /// <summary>
        /// 将技能分类转换为动画模块定义的播放类型。
        /// </summary>
        /// <param name="step">带有技能分类的施放步骤。</param>
        /// <returns>对应的物理、特殊或状态技能动画类型。</returns>
        private static SpriteFrameAnimationType GetSkillAnimationType(BattlePlaybackStep step)
        {
            if (step.SkillCategory == SkillCategory.Special)
                return SpriteFrameAnimationType.SpecialAttack;
            if (step.SkillCategory == SkillCategory.Status)
                return SpriteFrameAnimationType.StatusSkill;
            return SpriteFrameAnimationType.PhysicalAttack;
        }

        /// <summary>
        /// 判断当前步骤是否为一方的技能施放动画步骤。
        /// </summary>
        /// <param name="step">需要判断的回合播放步骤。</param>
        /// <returns>玩家或敌方攻击步骤返回 true。</returns>
        private static bool IsAttackStep(BattlePlaybackStep step)
        {
            return step.AnimType == StepAnimType.PlayerAttack ||
                   step.AnimType == StepAnimType.EnemyAttack;
        }

        /// <summary>
        /// 收集当前技能施放之后需要由打击帧触发的表现步骤。
        /// </summary>
        /// <param name="steps">本次回合的全部播放步骤。</param>
        /// <param name="attackStepIndex">当前技能施放步骤的下标。</param>
        /// <returns>紧随施放步骤之后的连续打击帧步骤下标。</returns>
        private static List<int> GetImpactStepIndices(
            IReadOnlyList<BattlePlaybackStep> steps,
            int attackStepIndex)
        {
            var indices = new List<int>();
            for (int i = attackStepIndex + 1; i < steps.Count; i++)
            {
                BattlePlaybackStep candidate = steps[i];
                if (!candidate.PresentAtAttackImpact) break;
                indices.Add(i);
            }

            return indices;
        }

        /// <summary>
        /// 将步骤中的日志与双方 HP 快照同步到战斗 UI。
        /// </summary>
        /// <param name="step">需要显示的回合播放步骤。</param>
        private void PresentTurnStep(BattlePlaybackStep step)
        {
            uiController.SetLog(step.Message);
            uiController.UpdateHp(
                step.PlayerHpAfter,
                _player.MaxHP,
                step.EnemyHpAfter,
                _enemy.MaxHP);
            uiController.UpdatePokemonIndicators(_player, _enemy);
        }

        /// <summary>
        /// 从动画目录中获取指定精灵的战斗动画配置。
        /// </summary>
        /// <param name="species">需要查找动画配置的精灵种族。</param>
        /// <returns>对应的动画配置；目录未收录时返回 null。</returns>
        private SpriteFrameAnimationProfile GetAnimationProfile(PokemonSpeciesData species)
        {
            return animationCatalog != null
                ? animationCatalog.GetProfile(species)
                : null;
        }
    }
}
