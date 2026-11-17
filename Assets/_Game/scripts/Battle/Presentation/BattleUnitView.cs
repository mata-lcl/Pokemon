using System;
using System.Collections;
using Pokemon.Presentation.Animation;
using Spine.Unity;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteFrameAnimationPlayer frameAnimationPlayer;
        [SerializeField] private float spineTargetHeight = 2.5f;

        private Vector3 _originalPosition;
        private SpriteFrameAnimationProfile _animationProfile;
        private PokemonBattleAnimationEntry _animationEntry;
        private SkeletonAnimation _spineAnimation;

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
            Setup(sprite, animationProfile, null);
        }

        /// <summary>
        /// 根据精灵动画条目选择 Spine 或序列帧显示方式。
        /// </summary>
        /// <param name="sprite">序列帧模式使用的基础图片。</param>
        /// <param name="animationProfile">序列帧模式使用的动画配置。</param>
        /// <param name="animationEntry">当前精灵的可选 Spine 动画配置。</param>
        public void Setup(
            Sprite sprite,
            SpriteFrameAnimationProfile animationProfile,
            PokemonBattleAnimationEntry animationEntry)
        {
            _animationProfile = animationProfile;
            _animationEntry = animationEntry;
            if (frameAnimationPlayer != null)
                frameAnimationPlayer.Stop();
            if (_spineAnimation != null)
            {
                _spineAnimation.gameObject.SetActive(false);
                Destroy(_spineAnimation.gameObject);
                _spineAnimation = null;
            }

            bool useSpine = animationEntry != null &&
                animationEntry.SpineSkeletonData != null;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.enabled = !useSpine;
            }

            if (useSpine)
            {
                _spineAnimation = SkeletonAnimation.NewSkeletonAnimationGameObject(
                    animationEntry.SpineSkeletonData);
                _spineAnimation.transform.SetParent(transform, false);
                MeshRenderer spineRenderer = _spineAnimation.GetComponent<MeshRenderer>();
                spineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                spineRenderer.sortingOrder = spriteRenderer.sortingOrder;
            }

            PlayIdleAnimation();
            if (useSpine)
                NormalizeSpineHeight(animationEntry.SpineScale);
        }

        /// <summary>
        /// 按待机姿态的角色部件高度统一 Spine 精灵的战斗显示尺寸。
        /// </summary>
        /// <param name="scaleAdjustment">当前精灵的尺寸微调倍率。</param>
        private void NormalizeSpineHeight(float scaleAdjustment)
        {
            _spineAnimation.LateUpdate();
            Spine.Skeleton skeleton = _spineAnimation.Skeleton;
            float[] vertices = new float[8];
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (Spine.Slot slot in skeleton.DrawOrder)
            {
                if (slot.A == 0) continue;

                Spine.RegionAttachment region = slot.Attachment as Spine.RegionAttachment;
                Spine.MeshAttachment mesh = slot.Attachment as Spine.MeshAttachment;
                string path = region != null ? region.Path : mesh != null ? mesh.Path : null;
                if (path == null || IsEffectAttachment(slot.Data.Name, path)) continue;

                int vertexCount = region != null ? 8 : mesh.WorldVerticesLength;
                if (vertices.Length < vertexCount)
                    vertices = new float[vertexCount];
                if (region != null)
                    region.ComputeWorldVertices(slot.Bone, vertices, 0);
                else
                    mesh.ComputeWorldVertices(slot, 0, vertexCount, vertices, 0);

                float slotMinX = float.MaxValue;
                float slotMaxX = float.MinValue;
                float slotMinY = float.MaxValue;
                float slotMaxY = float.MinValue;
                for (int i = 0; i < vertexCount; i += 2)
                {
                    slotMinX = Mathf.Min(slotMinX, vertices[i]);
                    slotMaxX = Mathf.Max(slotMaxX, vertices[i]);
                    slotMinY = Mathf.Min(slotMinY, vertices[i + 1]);
                    slotMaxY = Mathf.Max(slotMaxY, vertices[i + 1]);
                }
                if (slotMaxX - slotMinX < 0.001f ||
                    slotMaxY - slotMinY < 0.001f) continue;

                minY = Mathf.Min(minY, slotMinY);
                maxY = Mathf.Max(maxY, slotMaxY);
            }

            float height = maxY - minY;
            if (height <= 0f) return;
            float parentScale = Mathf.Abs(transform.lossyScale.y);
            _spineAnimation.transform.localScale = Vector3.one *
                (spineTargetHeight / (height * parentScale) * scaleAdjustment);
        }

        /// <summary>
        /// 判断插槽或贴图路径是否属于不参与角色尺寸计算的特效。
        /// </summary>
        /// <param name="slotName">Spine 插槽名称。</param>
        /// <param name="attachmentPath">当前贴图附件的路径。</param>
        /// <returns>属于特效时返回 true。</returns>
        private static bool IsEffectAttachment(string slotName, string attachmentPath)
        {
            return slotName.StartsWith("TX", StringComparison.OrdinalIgnoreCase) ||
                   slotName.StartsWith("effect", StringComparison.OrdinalIgnoreCase) ||
                   slotName.StartsWith("texiao", StringComparison.OrdinalIgnoreCase) ||
                   attachmentPath.StartsWith("TX/", StringComparison.OrdinalIgnoreCase) ||
                   attachmentPath.StartsWith("effect/", StringComparison.OrdinalIgnoreCase);
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
            if (_spineAnimation != null)
            {
                yield return PlaySpineSkillAnimation(
                    animationType,
                    speedMultiplier,
                    frameEventCallback);
                yield break;
            }

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
        /// 播放 Spine 技能动画，并在动画中点通知战斗表现结算打击。
        /// </summary>
        /// <param name="animationType">当前技能对应的攻击动画类型。</param>
        /// <param name="speedMultiplier">战斗加速倍率。</param>
        /// <param name="frameEventCallback">动画到达打击时机时调用的回调。</param>
        /// <returns>技能动画结束时完成的协程。</returns>
        private IEnumerator PlaySpineSkillAnimation(
            SpriteFrameAnimationType animationType,
            float speedMultiplier,
            Action<SpriteFrameEventContext> frameEventCallback)
        {
            string animationName = _animationEntry.GetSpineAnimationName(animationType);
            Spine.Animation animation = _spineAnimation.Skeleton.Data.FindAnimation(
                animationName);
            Spine.TrackEntry track = _spineAnimation.AnimationState.SetAnimation(
                0,
                animation,
                false);
            track.TimeScale = Mathf.Max(0.01f, speedMultiplier);

            float duration = animation.Duration / track.TimeScale;
            float elapsed = 0f;
            bool impactSent = false;
            while (elapsed < duration)
            {
                if (!impactSent && elapsed >= duration * 0.5f)
                {
                    frameEventCallback?.Invoke(new SpriteFrameEventContext(
                        null,
                        SpriteFrameAnimationEventIds.Impact,
                        -1));
                    impactSent = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!impactSent)
                frameEventCallback?.Invoke(new SpriteFrameEventContext(
                    null,
                    SpriteFrameAnimationEventIds.Impact,
                    -1));
            PlayIdleAnimation();
        }

        /// <summary>
        /// 播放当前精灵配置中的循环待机动画。
        /// </summary>
        private void PlayIdleAnimation()
        {
            if (_spineAnimation != null)
            {
                _spineAnimation.AnimationState.SetAnimation(
                    0,
                    _animationEntry.GetSpineAnimationName(SpriteFrameAnimationType.Idle),
                    true);
                return;
            }

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

            SetVisualColor(Color.red);
            yield return new WaitForSeconds(0.1f / safeSpeed);
            SetVisualColor(Color.white);
            yield return new WaitForSeconds(0.1f / safeSpeed);
            SetVisualColor(Color.red);
            yield return new WaitForSeconds(0.1f / safeSpeed);
            SetVisualColor(Color.white);
        }

        /// <summary>
        /// 同步设置序列帧或 Spine 精灵的受击颜色。
        /// </summary>
        /// <param name="color">当前需要显示的颜色。</param>
        private void SetVisualColor(Color color)
        {
            spriteRenderer.color = color;
            if (_spineAnimation == null) return;

            _spineAnimation.Skeleton.R = color.r;
            _spineAnimation.Skeleton.G = color.g;
            _spineAnimation.Skeleton.B = color.b;
        }
    }
}
