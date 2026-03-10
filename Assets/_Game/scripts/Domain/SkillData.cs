using UnityEngine;

namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "Pokemon/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("身份")]
        public string Id;
        public string DisplayName;

        [Header("技能信息")]
        public ElementType Type = ElementType.Normal;
        public TargetType TargetType = TargetType.SingleEnemy;
        public int Power = 40;
        [Range(0f, 1f)] public float Accuracy = 1f;
        public int MaxPP = 20;
    }
}