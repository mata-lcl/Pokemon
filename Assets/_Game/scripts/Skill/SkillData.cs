using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
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

        [Tooltip("技能的命中率，0-100")] // 由于你用int，我建议提示写0-100
        [Range(0, 100)]
        public int Accuracy;

        [Tooltip("技能最大PP值")]
        [Range(1, 100)]
        public int MaxPP;

        // ---------- 接下来是重头戏：模块化组合配置 ----------

        [Header("技能效果列表 (拖拽模板资源到这里)")]
        [Tooltip("系统会从上到下依次执行这里的效果")]
        public List<SkillEffectSO> Effects = new List<SkillEffectSO>();

        // 外部获取实际技能效果接口
        public List<ISkillEffect> GetEffects()
        {
            // 将编辑器配置的列表转换为对战系统用的接口列表返回
            return new List<ISkillEffect>(Effects);
        }
    }
}