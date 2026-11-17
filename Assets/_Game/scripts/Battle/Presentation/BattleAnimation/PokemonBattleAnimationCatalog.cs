using System;
using System.Collections.Generic;
using Pokemon.Domain;
using Pokemon.Presentation.Animation;
using Spine.Unity;
using UnityEngine;

namespace Pokemon.Presentation
{
    [Serializable]
    public sealed class PokemonBattleAnimationEntry
    {
        [Tooltip("需要使用该动画配置的精灵种族。")]
        [SerializeField] private PokemonSpeciesData species;
        [Tooltip("该精灵对应的四类战斗动画配置。")]
        [SerializeField] private SpriteFrameAnimationProfile animationProfile;
        [Tooltip("绑定后使用 Spine 动画；为空时继续使用序列帧动画。")]
        [SerializeField] private SkeletonDataAsset spineSkeletonData;
        [Tooltip("Spine 待机动画的名称。")]
        [SerializeField] private string spineIdleAnimation;
        [Tooltip("Spine 物理攻击动画的名称。")]
        [SerializeField] private string spinePhysicalAttackAnimation;
        [Tooltip("Spine 特殊攻击动画的名称。")]
        [SerializeField] private string spineSpecialAttackAnimation;
        [Tooltip("Spine 状态技能动画的名称。")]
        [SerializeField] private string spineStatusSkillAnimation;
        [Tooltip("Spine 在战斗画面中的显示缩放。")]
        [SerializeField] private float spineScale = 1f;

        public PokemonSpeciesData Species => species;
        public SpriteFrameAnimationProfile AnimationProfile => animationProfile;
        public SkeletonDataAsset SpineSkeletonData =>
            animationProfile != null && animationProfile.SpineSkeletonData != null
                ? animationProfile.SpineSkeletonData
                : spineSkeletonData;
        public float SpineScale =>
            animationProfile != null && animationProfile.SpineSkeletonData != null
                ? animationProfile.SpineScale
                : spineScale;

        /// <summary>
        /// 获取指定战斗动作对应的 Spine 动画名称。
        /// </summary>
        /// <param name="animationType">需要播放的待机或技能动画类型。</param>
        /// <returns>配置的 Spine 动画名称。</returns>
        public string GetSpineAnimationName(SpriteFrameAnimationType animationType)
        {
            if (animationProfile != null && animationProfile.SpineSkeletonData != null)
                return animationProfile.GetSpineAnimationName(animationType);

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
    }

    [CreateAssetMenu(
        fileName = "精灵战斗动画目录",
        menuName = "宝可梦/动画/精灵战斗动画目录")]
    public sealed class PokemonBattleAnimationCatalog : ScriptableObject
    {
        [Tooltip("按照精灵种族查找对应动画配置的映射列表。")]
        [SerializeField] private List<PokemonBattleAnimationEntry> entries =
            new List<PokemonBattleAnimationEntry>();

        /// <summary>
        /// 获取指定精灵的序列帧与 Spine 动画配置。
        /// </summary>
        /// <param name="species">需要查询动画配置的精灵种族。</param>
        /// <returns>对应的动画条目；目录未收录时返回 null。</returns>
        public PokemonBattleAnimationEntry GetEntry(PokemonSpeciesData species)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                PokemonBattleAnimationEntry entry = entries[i];
                if (entry.Species == species)
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// 根据精灵种族数据查找对应的战斗动画配置。
        /// </summary>
        /// <param name="species">需要查找动画的精灵种族数据。</param>
        /// <returns>找到的动画配置；没有配置时返回 null。</returns>
        public SpriteFrameAnimationProfile GetProfile(PokemonSpeciesData species)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                PokemonBattleAnimationEntry entry = entries[i];
                if (entry.Species == species)
                    return entry.AnimationProfile;
            }

            return null;
        }
    }
}
