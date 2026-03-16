using UnityEngine;

namespace Pokemon.Domain
{
    public enum SkillCategory
    {
        Physical, // 物理伤害
        Special,  // 特殊伤害 (目前可以统归为伤害类)
        Status,   // 变化类技能 (如回血、加 Buff 等)
    }
    //状态
    public enum StatusCondition
    {
        None,
        Poison, // 中毒：每回合扣血
        BuffAttack,  // 攻击提升
        BuffDefense, // 防御提升
        Heal
    }


    [CreateAssetMenu(fileName = "Skill_", menuName = "Pokemon/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("技能")]
        public string Id;
        public string DisplayName;

        [Header("技能信息")]
        public ElementType Type = ElementType.Normal;
        public SkillCategory Category = SkillCategory.Physical;
        public TargetType TargetType = TargetType.SingleEnemy;
        public int Power = 40;
        [Range(0f, 1f)] public float Accuracy = 1f;
        public int MaxPP = 20;

        [Header("特殊效果")]
        public StatusCondition ApplyStatus = StatusCondition.None; // 该技能可能造成的异常状态
        [Range(0f, 1f)] public float StatusChance = 0f;            // 造成异常状态的几率
        public int EffectValue = 0;
    }
}