using Pokemon.Domain;
using Unity.VisualScripting.Dependencies.NCalc;

namespace Pokemon.Application
{
    /// <summary>
    /// 战斗回合执行用例，采用命令模式和用例模式设计
    /// </summary>
    public sealed class ExecuteTurnUseCase_Old
    {
        public struct TurnResult
        {
            public bool PlayerActed;    //玩家回合
            public bool EnemyActed;     //敌方回合
            public bool PlayerHit;      //玩家命中
            public bool EnemyHit;       //敌人命中
            public int DamageToEnemy;   //对敌人伤害
            public int DamageToPlayer;  //对玩家伤害
            public bool BattleEnded;    //对战结束
            public bool PlayerWon;      //玩家胜利
        }

        //只读 确保线程安全和不可变性
        private readonly DamageCalculator _damageCalculator;

        /// <summary>
        /// 构造函数，通过构造函数注入damageCalculator
        /// 这个类只负责处理回合逻辑，伤害计算交给DamageCalculator
        /// </summary>
        /// <param name="damageCalculator"></param>
        public ExecuteTurnUseCase_Old(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        /// <summary>
        /// 核心方法
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerSkill"></param>
        /// <param name="enemy"></param>
        /// <param name="enemySkill"></param>
        /// <returns></returns>
        public TurnResult Execute(
            MonsterRuntime player,
            SkillData playerSkill,
            MonsterRuntime enemy,
            SkillData enemySkill)
        {
            TurnResult result = new();

            bool playerFirst = player.Speed >= enemy.Speed;

            if (playerFirst)
            {
                ResolveAction(player, playerSkill, enemy, ref result, isPlayerAction: true);
                if (!enemy.IsFainted)
                    ResolveAction(enemy, enemySkill, player, ref result, isPlayerAction: false);
            }
            else
            {
                ResolveAction(enemy, enemySkill, player, ref result, isPlayerAction: false);
                if (!player.IsFainted)
                    ResolveAction(player, playerSkill, enemy, ref result, isPlayerAction: true);
            }

            if (player.IsFainted || enemy.IsFainted)
            {
                result.BattleEnded = true;
                result.PlayerWon = enemy.IsFainted && !player.IsFainted;
            }

            return result;
        }

        /// <summary>
        /// 执行单次攻击的逻辑链
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="skill"></param>
        /// <param name="defender"></param>
        /// <param name="result"></param>
        /// <param name="isPlayerAction"></param>
        private void ResolveAction(
            MonsterRuntime attacker,
            SkillData skill,
            MonsterRuntime defender,
            ref TurnResult result,
            bool isPlayerAction)
        {
            //判断是否还有技能PP
            if (!attacker.TryConsumePP(skill))
                return;

            //检查是否命中
            bool hit = _damageCalculator.CheckHit(skill);
            //int damage = 0;
            var damage = _damageCalculator.CalculateDamage(attacker, defender, skill);

            if (hit)
            {

                //damage = _damageCalculator.CalculateDamage(attacker, defender, skill);
                defender.ApplyDamage(damage.FinalDamage);
            }

            if (isPlayerAction)
            {
                result.PlayerActed = true;
                result.PlayerHit = hit;
                result.DamageToEnemy = damage.FinalDamage;
            }
            else
            {
                result.EnemyActed = true;
                result.EnemyHit = hit;
                result.DamageToPlayer = damage.FinalDamage;
            }
        }
    }
}