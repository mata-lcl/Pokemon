using System;
using System.Collections;
using Pokemon.Presentation.Animation;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteFrameAnimationPlayer frameAnimationPlayer;

        private Vector3 _originalPosition;
        private SpriteFrameAnimationProfile _animationProfile;

        private void Awake()
        {
            _originalPosition = transform.position;
        }

        public void Setup(Sprite sprite)
        {
            Setup(sprite, null);
        }

        /// <summary>
        /// 设置精灵基础图片和当前种族使用的序列帧动画配置。
        /// </summary>
        /// <param name="sprite">没有播放序列帧时显示的基础图片。</param>
        /// <param name="animationProfile">当前精灵的四类序列帧动画配置。</param>
        public void Setup(Sprite sprite, SpriteFrameAnimationProfile animationProfile)
        {
            _animationProfile = animationProfile;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }

            PlayIdleAnimation();
        }

        /// <summary>
        /// 播放指定类型的技能序列帧，并向外转发动画配置中的通用帧事件。
        /// </summary>
        /// <param name="animationType">物理、特殊或状态技能对应的动画类型。</param>
        /// <param name="isPlayer">是否为玩家侧精灵，用于旧动画的移动方向。</param>
        /// <param name="speedMultiplier">动画播放速度倍率。</param>
        /// <param name="frameEventCallback">收到序列帧事件时调用的回调。</param>
        public IEnumerator PlaySkillAnimation(
            SpriteFrameAnimationType animationType,
            bool isPlayer,
            float speedMultiplier,
            Action<SpriteFrameEventContext> frameEventCallback)
        {
            SpriteFrameAnimationClip clip = _animationProfile != null
                ? _animationProfile.GetClip(animationType)
                : null;

            if (frameAnimationPlayer == null || clip == null)
            {
                yield return PlayAttackAnimation(
                    isPlayer,
                    speedMultiplier,
                    () => frameEventCallback?.Invoke(new SpriteFrameEventContext(
                        null,
                        SpriteFrameAnimationEventIds.Impact,
                        -1)));
                yield break;
            }

            Action<SpriteFrameEventContext> handler =
                context => frameEventCallback?.Invoke(context);
            frameAnimationPlayer.FrameEventRaised += handler;
            frameAnimationPlayer.Play(clip, speedMultiplier);

            while (frameAnimationPlayer.IsPlaying &&
                   frameAnimationPlayer.CurrentClip == clip)
            {
                yield return null;
            }

            frameAnimationPlayer.FrameEventRaised -= handler;
            PlayIdleAnimation();
        }

        /// <summary>
        /// 播放当前精灵配置中的循环待机动画。
        /// </summary>
        private void PlayIdleAnimation()
        {
            if (frameAnimationPlayer == null || _animationProfile == null) return;

            SpriteFrameAnimationClip idleClip =
                _animationProfile.GetClip(SpriteFrameAnimationType.Idle);
            if (idleClip != null)
                frameAnimationPlayer.Play(idleClip);
        }

        // 简单的“向前撞击”动画
        public IEnumerator PlayAttackAnimation(bool isPlayer)
        {
            yield return PlayAttackAnimation(isPlayer, 1f);
        }

        /// <summary>
        /// Plays the attack animation at the requested speed.
        /// </summary>
        public IEnumerator PlayAttackAnimation(bool isPlayer, float speedMultiplier)
        {
            yield return PlayAttackAnimation(isPlayer, speedMultiplier, null);
        }

        /// <summary>
        /// 播放旧版位移动画，并在精灵到达最前方时发送一次打击通知。
        /// </summary>
        /// <param name="isPlayer">是否为玩家侧精灵，用于确定移动方向。</param>
        /// <param name="speedMultiplier">动画播放速度倍率。</param>
        /// <param name="impactCallback">精灵到达攻击位置时调用的回调。</param>
        private IEnumerator PlayAttackAnimation(
            bool isPlayer,
            float speedMultiplier,
            Action impactCallback)
        {
            Vector3 targetPos = _originalPosition + (isPlayer ? Vector3.right : Vector3.left) * 1.5f;
            float safeSpeed = Mathf.Max(0.01f, speedMultiplier);
            float forwardDuration = 0.1f / safeSpeed;
            float returnDuration = 0.15f / safeSpeed;

            // 冲过去
            float t = 0;
            while (t < forwardDuration)
            {
                transform.position = Vector3.Lerp(_originalPosition, targetPos, t / forwardDuration);
                t += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            impactCallback?.Invoke();

            // 退回来
            t = 0;
            while (t < returnDuration)
            {
                transform.position = Vector3.Lerp(targetPos, _originalPosition, t / returnDuration);
                t += Time.deltaTime;
                yield return null;
            }
            transform.position = _originalPosition;
        }

        // 简单的“受击闪红”动画
        public IEnumerator PlayHitAnimation()
        {
            yield return PlayHitAnimation(1f);
        }

        /// <summary>
        /// Plays the hit flash animation at the requested speed.
        /// </summary>
        public IEnumerator PlayHitAnimation(float speedMultiplier)
        {
            if (spriteRenderer == null) yield break;

            float safeSpeed = Mathf.Max(0.01f, speedMultiplier);

            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f / safeSpeed);
            spriteRenderer.color = Color.white;
        }
    }
}
