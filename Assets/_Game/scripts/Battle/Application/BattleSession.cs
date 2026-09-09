using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public sealed class BattleSession
    {
        public MonsterRuntime Player { get; private set; }
        public MonsterRuntime Enemy { get; }
        public ExecuteTurnUseCase TurnUseCase { get; }
        public List<SkillData> PlayerSkills { get; } = new List<SkillData>();
        public bool BattleEnded { get; set; }

        /// <summary>
        /// 创建一场战斗的运行时会话。
        /// </summary>
        /// <param name="player">当前玩家出战精灵。</param>
        /// <param name="enemy">当前敌方精灵。</param>
        /// <param name="turnUseCase">负责执行回合规则的用例。</param>
        public BattleSession(
            MonsterRuntime player,
            MonsterRuntime enemy,
            ExecuteTurnUseCase turnUseCase)
        {
            Player = player;
            Enemy = enemy;
            TurnUseCase = turnUseCase;
            RefreshPlayerSkills();
        }

        /// <summary>
        /// 切换当前玩家出战精灵并刷新其技能列表。
        /// </summary>
        /// <param name="player">新的玩家出战精灵。</param>
        public void SetPlayer(MonsterRuntime player)
        {
            Player = player;
            RefreshPlayerSkills();
        }

        /// <summary>
        /// 根据当前出战精灵的 PP 数据刷新可用技能列表。
        /// </summary>
        public void RefreshPlayerSkills()
        {
            PlayerSkills.Clear();
            foreach (SkillData skill in Player.EquippedSkills)
            {
                if (skill != null) PlayerSkills.Add(skill);
            }
        }
    }
}
