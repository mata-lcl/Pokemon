using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pokemon.Domain
{
    [System.Serializable] 
    public class EffectConfig
    {
        public SkillEffectSO Effect;
        [Range(0f, 1f)] public float Chance = 1f; // 触发几率
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "Pokemon/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Tooltip("技能唯一标识代码")]
        public string Id;

        [Tooltip("游戏内显示名称")]
        public string DisplayName;

        [Tooltip("属性（火、水、草等）")]
        public PokemonType Type;

        [Tooltip("分类（物理、特殊、变化）")]
        public SkillCategory Category;

        [Tooltip("技能的威力（参与伤害计算）")]
        public int Power;

        [Tooltip("技能的命中率，0-1")]
        [Range(0f, 1f)]
        public float Accuracy;

        [Tooltip("技能最大PP值")]
        [Range(1, 100)]
        public int MaxPP;

        // ---------- 模块化组合配置 ----------

        [Header("概率效果列表（可设触发几率）")]
        [SerializeField]
        private List<EffectConfig> effectConfigs = new List<EffectConfig>();

        [Header("技能效果列表（拖拽模板资源到这里）")]
        [Tooltip("系统会从上到下依次执行这里的效果")]
        public List<SkillEffectSO> Effects = new List<SkillEffectSO>();

        // 外部获取实际技能效果接口
        public List<ISkillEffect> GetEffects()
        {
            return new List<ISkillEffect>(Effects);
        }

        // 返回本回合实际触发的效果（已做概率筛选）
        public IEnumerable<SkillEffectSO> GetEffectConfigs()
        {
            foreach (var config in effectConfigs)
            {
                if (config.Effect != null && Random.value <= config.Chance)
                {
                    //Debug.Log($"技能 {DisplayName} 触发了效果 {config.Effect.name} (概率 {config.Chance * 100}%)");
                    yield return config.Effect;
                }
            }
        }
    }
}