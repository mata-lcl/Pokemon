namespace Pokemon.Domain
{
    public enum PokemonType
    {
        [UnityEngine.InspectorName("无")]
        None = 0,
        [UnityEngine.InspectorName("一般")]
        Normal = 1,
        [UnityEngine.InspectorName("水")]
        Water = 2,
        [UnityEngine.InspectorName("火")]
        Fire = 3,
        [UnityEngine.InspectorName("草")]
        Grass = 4,
        [UnityEngine.InspectorName("电")]
        Electric = 5,
        [UnityEngine.InspectorName("冰")]
        Ice = 6,
        [UnityEngine.InspectorName("格斗")]
        Fighting = 7,
        [UnityEngine.InspectorName("毒")]
        Poison = 8,
        [UnityEngine.InspectorName("地面")]
        Ground = 9,
        [UnityEngine.InspectorName("飞行")]
        Flying = 10,
        [UnityEngine.InspectorName("超能力")]
        Psychic = 11,
        [UnityEngine.InspectorName("虫")]
        Bug = 12,
        [UnityEngine.InspectorName("岩石")]
        Rock = 13,
        [UnityEngine.InspectorName("幽灵")]
        Ghost = 14,
        [UnityEngine.InspectorName("龙")]
        Dragon = 15,
        [UnityEngine.InspectorName("恶")]
        Dark = 16,
        [UnityEngine.InspectorName("钢")]
        Steel = 17,
        [UnityEngine.InspectorName("妖精")]
        Fairy = 18
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

    public enum ItemType
    {
        HealHP,    // 回复HP
        HealPP,    // 回复PP (未来扩展)
        Pokeball   // 精灵球 (未来扩展)
    }
}
