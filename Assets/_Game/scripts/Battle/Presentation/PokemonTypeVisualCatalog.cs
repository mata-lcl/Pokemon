using System;
using System.Collections.Generic;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Presentation
{
    [Serializable]
    public sealed class PokemonTypeVisual
    {
        [SerializeField] private PokemonType type;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color color = Color.white;

        public PokemonType Type => type;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color Color => color;
    }

    [CreateAssetMenu(
        fileName = "属性视觉配置",
        menuName = "宝可梦/配置/UI/属性视觉配置")]
    public sealed class PokemonTypeVisualCatalog : ScriptableObject
    {
        [SerializeField]
        private List<PokemonTypeVisual> visuals = new List<PokemonTypeVisual>();

        /// <summary>
        /// 获取指定宝可梦属性对应的名称、图标和颜色配置。
        /// </summary>
        /// <param name="type">需要查询的宝可梦属性。</param>
        /// <returns>匹配的视觉配置；未配置时返回 null。</returns>
        public PokemonTypeVisual GetVisual(PokemonType type)
        {
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i].Type == type)
                    return visuals[i];
            }

            return null;
        }
    }
}
