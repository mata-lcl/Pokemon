namespace Pokemon.Domain
{
    public enum PokemonType
    {
        None = 0,
        Normal = 1,
        Water = 2,
        Fire = 3,
        Grass = 4
    }

    public enum TargetType
    {
        Self = 0,
        SingleEnemy = 1
    }

    public enum StatusCondition
    {
        None,
        Poison,   // 中毒
        Heal,     // 治疗
        Paralyze, // 麻痹
        Sleep,    // 睡眠
        Burn,     // 灼伤
        Freeze    // 冰冻
    }

    public enum SkillCategory
    {
        Physical, // 物理攻击：受攻击者的攻击力(Attack)和防御力(Defense)影响
        Special,  // 特殊攻击：受攻击者的特攻(SpAtk)和特防(SpDef)影响
        Status    // 变化/状态技能：不直接造成伤害，而是改变属性、状态或天气
    }
}
