using Spine.Unity;
using UnityEngine;

namespace Pokemon.Presentation.Animation
{
    [CreateAssetMenu(
        fileName = "精灵动画配置_",
        menuName = "宝可梦/动画/精灵动画配置")]
    public sealed class SpriteFrameAnimationProfile : ScriptableObject
    {
        [Tooltip("精灵没有执行技能时循环播放的动画。")]
        [SerializeField] private SpriteFrameAnimationClip idle;
        [Tooltip("物理分类技能使用的动画。")]
        [SerializeField] private SpriteFrameAnimationClip physicalAttack;
        [Tooltip("特殊分类技能使用的动画。")]
        [SerializeField] private SpriteFrameAnimationClip specialAttack;
        [Tooltip("状态分类技能使用的动画。")]
        [SerializeField] private SpriteFrameAnimationClip statusSkill;
        [Tooltip("绑定后使用 Spine 动画；为空时使用序列帧动画。")]
        [SerializeField] private SkeletonDataAsset spineSkeletonData;
        [Tooltip("Spine 待机动画的名称。")]
        [SerializeField] private string spineIdleAnimation;
        [Tooltip("Spine 物理攻击动画的名称。")]
        [SerializeField] private string spinePhysicalAttackAnimation;
        [Tooltip("Spine 特殊攻击动画的名称。")]
        [SerializeField] private string spineSpecialAttackAnimation;
        [Tooltip("Spine 状态技能动画的名称。")]
        [SerializeField] private string spineStatusSkillAnimation;
        [Tooltip("自动统一高度后的尺寸微调倍率，通常保持为 1。")]
        [SerializeField] private float spineScale = 1f;

        public SkeletonDataAsset SpineSkeletonData => spineSkeletonData;
        public float SpineScale => spineScale;

        /// <summary>
        /// 获取指定战斗动作对应的 Spine 动画名称。
        /// </summary>
        /// <param name="animationType">需要查询的战斗动画类型。</param>
        /// <returns>当前类型配置的 Spine 动画名称。</returns>
        public string GetSpineAnimationName(SpriteFrameAnimationType animationType)
        {
            switch (animationType)
            {
                case SpriteFrameAnimationType.PhysicalAttack:
                    return spinePhysicalAttackAnimation;
                case SpriteFrameAnimationType.SpecialAttack:
                    return spineSpecialAttackAnimation;
                case SpriteFrameAnimationType.StatusSkill:
                    return spineStatusSkillAnimation;
                default:
                    return spineIdleAnimation;
            }
        }

        /// <summary>
        /// 根据动画类型返回对应的序列帧动画配置。
        /// </summary>
        /// <param name="animationType">需要获取的动画类型。</param>
        /// <returns>与动画类型对应的序列帧动画配置。</returns>
        public SpriteFrameAnimationClip GetClip(SpriteFrameAnimationType animationType)
        {
            switch (animationType)
            {
                case SpriteFrameAnimationType.PhysicalAttack:
                    return physicalAttack;
                case SpriteFrameAnimationType.SpecialAttack:
                    return specialAttack;
                case SpriteFrameAnimationType.StatusSkill:
                    return statusSkill;
                default:
                    return idle;
            }
        }
    }
}
