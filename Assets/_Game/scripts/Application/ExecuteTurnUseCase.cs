using Pokemon.Domain;

namespace Pokemon.Application
{
    public sealed class ExecuteTurnUseCase
    {
        public struct TurnResult
        {
            public bool PlayerActed;
            public bool EnemyActed;
            public bool PlayerHit;
            public bool EnemyHit;
            public int DamageToEnemy;
            public int DamageToPlayer;
            public bool BattleEnded;
            public bool PlayerWon;
        }

        //只读 确保线程安全和不可变性
        private readonly DamageCalculator _damageCalculator;

        /// <summary>
        /// 构造函数，通过构造函数注入damageCalculator
        /// 这个类只负责处理回合逻辑，伤害计算交给DamageCalculator
        /// </summary>
        /// <param name="damageCalculator"></param>
        public ExecuteTurnUseCase(DamageCalculator damageCalculator)
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
            TurnResult result = new TurnResult();

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
            if (!attacker.TryConsumePP(skill))
                return;

            bool hit = _damageCalculator.CheckHit(skill);
            int damage = 0;

            if (hit)
            {
                damage = _damageCalculator.CalculateDamage(attacker, defender, skill);
                defender.ApplyDamage(damage);
            }

            if (isPlayerAction)
            {
                result.PlayerActed = true;
                result.PlayerHit = hit;
                result.DamageToEnemy = damage;
            }
            else
            {
                result.EnemyActed = true;
                result.EnemyHit = hit;
                result.DamageToPlayer = damage;
            }
        }
    }
}