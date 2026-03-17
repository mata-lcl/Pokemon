using System.Collections.Generic;
using UnityEngine;
// 技能分类
namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "Pokemon/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Tooltip("技能的唯一标识符，用于代码中引用")]
        public string Id;

        [Tooltip("技能在游戏中显示的名称")]
        public string DisplayName;

        [Tooltip("技能所属的属性类型（火、水、电等）")]
        public PokemonType Type;

        [Tooltip("技能类别：物理、特殊、变化")]
        public SkillCategory Category;

        [Tooltip("技能的基础威力（伤害技能有效）")]
        public int Power;

        [Tooltip("技能的基础命中率（0-1）")]
        [Range(0, 1)]
        public int Accuracy;

        [Tooltip("技能的最大PP值（使用次数）")]
        [Range(1, 100)]
        public int MaxPP;

        // 特殊效果配置（旧字段保留，作为配置项）
        public StatusCondition ApplyStatus;
        [Range(0, 1)] public float StatusChance;
        public int EffectValue;

        // --- 核心重构：获取此技能的所有效果 ---
        public List<ISkillEffect> GetEffects()
        {
            var results = new List<ISkillEffect>();

            // 1. 如果有威力，添加伤害效果
            if (Power > 0)
                results.Add(new Effects.DamageEffect());

            // 2. 如果是回复逻辑
            if (ApplyStatus == StatusCondition.Heal)
                results.Add(new Effects.HealEffect(EffectValue));

            // 3. TODO: 未来你可以通过判断 ApplyStatus == Poison 
            //    来 new 一个 AddStatusEffect(StatusCondition.Poison)

            return results;
        }
    }
}