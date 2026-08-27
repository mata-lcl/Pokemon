namespace Pokemon.Presentation.Animation
{
    public enum SpriteFrameAnimationType
    {
        [UnityEngine.InspectorName("待机")]
        Idle,
        [UnityEngine.InspectorName("物理攻击")]
        PhysicalAttack,
        [UnityEngine.InspectorName("特殊攻击")]
        SpecialAttack,
        [UnityEngine.InspectorName("状态技能")]
        StatusSkill
    }
}
