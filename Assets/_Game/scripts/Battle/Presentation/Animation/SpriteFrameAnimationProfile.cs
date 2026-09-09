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
