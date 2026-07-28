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

        [Header("行动规则")]
        [Tooltip("优先级越高越先行动；相同优先级再比较速度")]
        [Range(-7, 7)]
        public int Priority;

        // ---------- 效果配置（统一入口） ----------

        [Header("效果配置列表（可设触发几率,默认1.0=必定触发）")]
        [SerializeField]
        private List<EffectConfig> effectConfigs = new List<EffectConfig>();

        [Obsolete("Legacy field, kept for asset backward compatibility. Use effectConfigs instead.")]
        [HideInInspector]
        public List<SkillEffectSO> Effects = new List<SkillEffectSO>();

        /// <summary>
        /// 获取本回合实际触发的效果（已做概率筛选）
        /// 旧资产（Effects 非空）：全部触发，忽略 effectConfigs
        /// 已迁移资产（Effects 为空）：按 effectConfigs 的 Chance 概率触发
        /// </summary>
        public IEnumerable<ISkillEffect> GetEffects()
        {
            if (Effects.Count > 0)
            {
                // 旧资产回退：所有效果必定触发
#pragma warning disable CS0612
                foreach (var effect in Effects)
                    if (effect != null)
                        yield return effect;
#pragma warning restore CS0612
            }
            else
            {
                // 已迁移：按概率触发
                foreach (var config in effectConfigs)
                {
                    if (config.Effect != null && Random.value <= config.Chance)
                        yield return config.Effect;
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: 将 Effects 迁移到 effectConfigs（Chance=1.0）。
        /// 在 Inspector 中右键技能资产即可使用。
        /// </summary>
        [ContextMenu("Migrate Effects → EffectConfigs")]
        public void MigrateEffectsToConfigs()
        {
            if (Effects.Count == 0)
            {
                Debug.Log($"[{DisplayName}] 没有需要迁移的旧版 Effects。");
                return;
            }

            foreach (var effect in Effects)
            {
                if (effect != null && effectConfigs.TrueForAll(c => c.Effect != effect))
                    effectConfigs.Add(new EffectConfig { Effect = effect, Chance = 1f });
            }

            Effects.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{DisplayName}] 已将 Effects 迁移到 effectConfigs。");
        }
#endif
    }
}
