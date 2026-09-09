using System;
using System.Collections.Generic;
using Pokemon.Domain;
using Pokemon.Presentation.Animation;
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

        public PokemonSpeciesData Species => species;
        public SpriteFrameAnimationProfile AnimationProfile => animationProfile;
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
