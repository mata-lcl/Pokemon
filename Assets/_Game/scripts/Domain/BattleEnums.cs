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
        None = 0,
        Poison = 1,   // 中毒
        Burn = 2,     // 灼伤
        Paralyze = 3, // 麻痹 (减速，概率不能行动)
        Sleep = 4,    // 睡眠 (几回合内不能行动)
        Freeze = 5    // 冰冻 (不能行动，受火系伤害解除)
    }

    public enum SkillCategory
    {
        Physical, // 物理攻击：受攻击者的攻击力(Attack)和防御力(Defense)影响
        Special,  // 特殊攻击：受攻击者的特攻(SpAtk)和特防(SpDef)影响
        Status    // 变化/状态技能：不直接造成伤害，而是改变属性、状态或天气
    }

    public enum StepAnimType
    {
        None,
        PlayerAttack,
        EnemyAttack,
        PlayerHit,
        EnemyHit
    }
}
