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

    [CreateAssetMenu(fileName = "技能_", menuName = "宝可梦/配置/技能配置")]
    public class SkillData : ScriptableObject
    {
        [Tooltip("技能唯一标识代码")]
        public string Id;

        [Tooltip("游戏内显示名称")]
        public string DisplayName;

        [Header("显示信息")]
        [Tooltip("技能按钮和技能详情中显示的技能图标")]
        [SerializeField] private Sprite icon;

        [Tooltip("鼠标悬停技能按钮时显示的技能说明")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        public Sprite Icon => icon;
        public string Description => description;

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

        [Header("主效果")]
        [Tooltip("技能成立所必需的核心效果，例如伤害、治疗或反应效果")]
        [SerializeField]
        private SkillEffectSO primaryEffect;

        [Header("副效果列表（可设触发几率,默认1.0=必定触发）")]
        [SerializeField]
        private List<EffectConfig> secondaryEffects = new List<EffectConfig>();

        [Obsolete("Legacy field, kept for asset backward compatibility. Use primaryEffect and secondaryEffects instead.")]
        [HideInInspector]
        [SerializeField]
        private List<EffectConfig> effectConfigs = new List<EffectConfig>();

        [Obsolete("Legacy field, kept for asset backward compatibility. Use primaryEffect and secondaryEffects instead.")]
        [HideInInspector]
        public List<SkillEffectSO> Effects = new List<SkillEffectSO>();

        /// <summary>
        /// 获取技能的主效果；未迁移资产会从旧效果列表中读取第一个效果。
        /// </summary>
        /// <returns>技能主效果；没有配置时返回 null。</returns>
        public ISkillEffect GetPrimaryEffect()
        {
            if (UsesPrimaryAndSecondaryEffects())
                return primaryEffect;

#pragma warning disable CS0618
            if (Effects.Count > 0)
                return Effects[0];

            return effectConfigs.Count > 0
                ? effectConfigs[0].Effect
                : null;
#pragma warning restore CS0618
        }

        /// <summary>
        /// 获取本次技能实际触发的副效果，并在读取时完成概率判定。
        /// </summary>
        /// <returns>按配置顺序排列且通过概率判定的副效果。</returns>
        public IEnumerable<ISkillEffect> GetTriggeredSecondaryEffects()
        {
            if (UsesPrimaryAndSecondaryEffects())
            {
                foreach (EffectConfig config in secondaryEffects)
                {
                    if (config.Effect != null && Random.value <= config.Chance)
                        yield return config.Effect;
                }

                yield break;
            }

#pragma warning disable CS0618
            if (Effects.Count > 0)
            {
                for (int i = 1; i < Effects.Count; i++)
                {
                    SkillEffectSO effect = Effects[i];
                    if (effect == null) continue;

                    EffectConfig probabilityConfig = effectConfigs.Find(
                        config => config.Effect == effect);
                    if (probabilityConfig == null ||
                        Random.value <= probabilityConfig.Chance)
                    {
                        yield return effect;
                    }
                }

                yield break;
            }

            for (int i = 1; i < effectConfigs.Count; i++)
            {
                EffectConfig config = effectConfigs[i];
                if (config.Effect != null && Random.value <= config.Chance)
                    yield return config.Effect;
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// 获取本回合按主效果、副效果顺序实际触发的全部效果。
        /// </summary>
        public IEnumerable<ISkillEffect> GetEffects()
        {
            ISkillEffect mainEffect = GetPrimaryEffect();
            if (mainEffect != null)
                yield return mainEffect;

            foreach (ISkillEffect secondaryEffect in GetTriggeredSecondaryEffects())
                yield return secondaryEffect;
        }

        /// <summary>
        /// 判断技能是否已经使用主效果和副效果的新配置结构。
        /// </summary>
        /// <returns>任一新配置字段含有内容时返回 true。</returns>
        private bool UsesPrimaryAndSecondaryEffects()
        {
            return primaryEffect != null || secondaryEffects.Count > 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 将旧效果列表迁移为一个主效果和多个带概率的副效果。
        /// </summary>
        [ContextMenu("迁移为主效果和副效果")]
        public void MigrateEffectsToPrimaryAndSecondary()
        {
            if (UsesPrimaryAndSecondaryEffects())
            {
                Debug.Log($"[{DisplayName}] 已经使用主效果和副效果配置。");
                return;
            }

#pragma warning disable CS0618
            if (Effects.Count > 0)
            {
                primaryEffect = Effects[0];
                for (int i = 1; i < Effects.Count; i++)
                {
                    SkillEffectSO effect = Effects[i];
                    if (effect == null) continue;

                    EffectConfig oldConfig = effectConfigs.Find(
                        config => config.Effect == effect);
                    secondaryEffects.Add(new EffectConfig
                    {
                        Effect = effect,
                        Chance = oldConfig != null ? oldConfig.Chance : 1f
                    });
                }
            }
            else if (effectConfigs.Count > 0)
            {
                primaryEffect = effectConfigs[0].Effect;
                for (int i = 1; i < effectConfigs.Count; i++)
                {
                    EffectConfig config = effectConfigs[i];
                    if (config.Effect != null)
                    {
                        secondaryEffects.Add(new EffectConfig
                        {
                            Effect = config.Effect,
                            Chance = config.Chance
                        });
                    }
                }
            }

            Effects.Clear();
            effectConfigs.Clear();
#pragma warning restore CS0618

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{DisplayName}] 已迁移为主效果和副效果配置。");
        }

        /// <summary>
        /// Editor-only: 将 Effects 迁移到 effectConfigs（Chance=1.0）。
        /// 在 Inspector 中右键技能资产即可使用。
        /// </summary>
        [ContextMenu("Migrate Effects → EffectConfigs")]
        public void MigrateEffectsToConfigs()
        {
#pragma warning disable CS0618
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
#pragma warning restore CS0618
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{DisplayName}] 已将 Effects 迁移到 effectConfigs。");
        }
#endif
    }
}
