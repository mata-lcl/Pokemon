using System;
using Pokemon.Domain;
using UnityEngine;

namespace Pokemon.Application
{
    public class SaveAssetCatalog : MonoBehaviour
    {
        [SerializeField] private PokemonSpeciesData[] species;
        [SerializeField] private ItemData[] items;
        [SerializeField] private SkillData[] skills;
        [SerializeField] private AbilityData[] abilities;

        /// <summary>
        /// 按资源名和种类编号返回存档对应的精灵种类资源。
        /// </summary>
        /// <param name="id">精灵种类编号。</param>
        /// <param name="assetName">精灵种类资源名。</param>
        public PokemonSpeciesData ResolveSpecies(int id, string assetName)
        {
            for (int i = 0; i < species.Length; i++)
            {
                if (species[i] != null && species[i].ID == id && species[i].name == assetName)
                    return species[i];
            }

            throw new InvalidOperationException($"未绑定存档需要的精灵资源：{assetName} ({id})");
        }

        /// <summary>
        /// 按资源名和道具编号返回存档对应的道具资源。
        /// </summary>
        /// <param name="id">道具编号。</param>
        /// <param name="assetName">道具资源名。</param>
        public ItemData ResolveItem(string id, string assetName)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Id == id && items[i].name == assetName)
                    return items[i];
            }

            throw new InvalidOperationException($"未绑定存档需要的道具资源：{assetName} ({id})");
        }

        /// <summary>
        /// 按资源名和技能编号返回存档对应的技能资源。
        /// </summary>
        /// <param name="id">技能编号。</param>
        /// <param name="assetName">技能资源名。</param>
        public SkillData ResolveSkill(string id, string assetName)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null && skills[i].Id == id && skills[i].name == assetName)
                    return skills[i];
            }

            throw new InvalidOperationException($"未绑定存档需要的技能资源：{assetName} ({id})");
        }

        /// <summary>
        /// 按资源名和特性编号返回存档对应的特性资源。
        /// </summary>
        /// <param name="id">特性编号。</param>
        /// <param name="assetName">特性资源名。</param>
        public AbilityData ResolveAbility(int id, string assetName)
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] != null && abilities[i].Id == id && abilities[i].name == assetName)
                    return abilities[i];
            }

            throw new InvalidOperationException($"未绑定存档需要的特性资源：{assetName} ({id})");
        }
    }
}
