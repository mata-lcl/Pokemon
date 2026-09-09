using Pokemon.Domain;

namespace Pokemon.Application
{
    public readonly struct BattleOutcome
    {
        public int GainedExperience { get; }
        public bool LeveledUp { get; }

        /// <summary>
        /// 创建战斗胜利后的结算结果。
        /// </summary>
        /// <param name="gainedExperience">本次战斗获得的经验值。</param>
        /// <param name="leveledUp">当前精灵是否在结算后升级。</param>
        public BattleOutcome(int gainedExperience, bool leveledUp)
        {
            GainedExperience = gainedExperience;
            LeveledUp = leveledUp;
        }
    }

    public sealed class BattleOutcomeService
    {
        /// <summary>
        /// 结算玩家胜利后的任务进度、努力值和经验值。
        /// </summary>
        /// <param name="player">获得战斗奖励的玩家精灵。</param>
        /// <param name="enemy">被击败并提供奖励的敌方精灵。</param>
        /// <returns>供表现层显示的经验值和升级结果。</returns>
        public BattleOutcome ResolveVictory(MonsterRuntime player, MonsterRuntime enemy)
        {
            QuestService.ReportPokemonDefeated(enemy.Species);

            player.AddEVs(
                enemy.Species.EvYieldHP,
                enemy.Species.EvYieldAttack,
                enemy.Species.EvYieldDefense,
                enemy.Species.EvYieldSpeed,
                enemy.Species.EvYieldSpAttack,
                enemy.Species.EvYieldSpDefense);

            int gainedExperience = (enemy.Species.BaseExpYield * enemy.Level) / 7;
            bool leveledUp = player.AddExp(gainedExperience);
            return new BattleOutcome(gainedExperience, leveledUp);
        }
    }
}
