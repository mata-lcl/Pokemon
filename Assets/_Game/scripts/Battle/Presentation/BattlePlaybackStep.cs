using Pokemon.Domain;

namespace Pokemon.Presentation
{
    public sealed class BattlePlaybackStep
    {
        public string Message { get; set; }
        public int PlayerHpAfter { get; set; }
        public int EnemyHpAfter { get; set; }
        public StepAnimType AnimType { get; set; }
        public SkillCategory? SkillCategory { get; set; }
        public bool PresentAtAttackImpact { get; set; }
    }
}
