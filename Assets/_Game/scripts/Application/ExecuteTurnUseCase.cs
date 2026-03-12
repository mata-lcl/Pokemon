using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public enum StepAnimType
    {
        None,
        PlayerAttack,
        EnemyAttack,
        PlayerHit,
        EnemyHit
    }
    // 定义一个回合内的单个“步骤/画面”
    //不可变的数据快照
    public struct TurnStep
    {
        public string Message;
        public int PlayerHpAfter;
        public int EnemyHpAfter;
        public bool IsBattleEnd;
        public bool PlayerWon;
        //新增2DSprite
        public StepAnimType AnimType;
    }

    public sealed class ExecuteTurnUseCase
    {
        
        private readonly DamageCalculator _damageCalculator;
        /// <summary>
        /// 构造函数，通过构造函数注入damageCalculator
        /// 这个类只负责处理回合逻辑，伤害计算交给DamageCalculator
        /// </summary>
        public ExecuteTurnUseCase(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        /// <summary>
        /// 核心步骤，返回步骤而不是结果
        /// 允许UI展示战斗过程
        /// </summary>
        /// <param 玩家宝可梦="player"></param>
        /// <param 玩家宝可梦技能="playerSkill"></param>
        /// <param 敌方="enemy"></param>
        /// <param 敌方技能="enemySkill"></param>
        /// <returns></returns>
        public List<TurnStep> Execute(
            MonsterRuntime player, SkillData playerSkill,
            MonsterRuntime enemy, SkillData enemySkill)
        {
            var steps = new List<TurnStep>();

            // 1. 判断先后手
            bool playerFirst = player.Speed >= enemy.Speed;

            if (playerFirst)
            {
                ResolveAction(player, playerSkill, enemy, true, steps);
                if (!enemy.IsFainted)
                    ResolveAction(enemy, enemySkill, player, false, steps);
            }
            else
            {
                ResolveAction(enemy, enemySkill, player, false, steps);
                if (!player.IsFainted)
                    ResolveAction(player, playerSkill, enemy, true, steps);
            }

            // 2. 检查胜负
            if (player.IsFainted || enemy.IsFainted)
            {
                steps.Add(new TurnStep
                {
                    Message = player.IsFainted ? "我方倒下了，战斗失败..." : "敌方倒下了，战斗胜利！",
                    PlayerHpAfter = player.CurrentHP,
                    EnemyHpAfter = enemy.CurrentHP,
                    IsBattleEnd = true,
                    PlayerWon = enemy.IsFainted
                });
            }

            return steps;
        }

        /// <summary>
        /// 处理回合信息，输出信息
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="skill"></param>
        /// <param name="defender"></param>
        /// <param name="isPlayerAttacking"></param>
        /// <param name="steps"></param>
        private void ResolveAction(
            MonsterRuntime attacker, SkillData skill,
            MonsterRuntime defender, bool isPlayerAttacking,
            List<TurnStep> steps)
        {
            //string attackerName = isPlayerAttacking ? "我方" : "敌方";
            string attackerName = attacker.Species.DisplayName;

            if (!attacker.TryConsumePP(skill))
            {
                steps.Add(CreateStep($"{attackerName}想使用 {skill.DisplayName}，但是PP不足！", attacker, defender, isPlayerAttacking));
                return;
            }

            bool hit = _damageCalculator.CheckHit(skill);
            if (!hit)
            {
                steps.Add(CreateStep($"{attackerName}使用 {skill.DisplayName}，但是未命中！", attacker, defender, isPlayerAttacking));
                return;
            }

            // 获取详细的伤害结果
            var damageResult = _damageCalculator.CalculateDamage(attacker, defender, skill);
            defender.ApplyDamage(damageResult.FinalDamage);

            //插入伤害提示
            //steps.Add(CreateStep($"{attackerName}使用 {skill.DisplayName}，造成了 {damageResult.FinalDamage} 点伤害！", attacker, defender, isPlayerAttacking));
            //新增：受伤动画
            var attackStep = CreateStep($"{attackerName}使用了 {skill.DisplayName}！", attacker, defender, isPlayerAttacking);
            attackStep.AnimType = isPlayerAttacking ? StepAnimType.PlayerAttack : StepAnimType.EnemyAttack;
            steps.Add(attackStep);


            // --- 步骤 2: 伤害与受击闪烁 ---
            var hitStep = CreateStep($"造成了 {damageResult.FinalDamage} 点伤害！", attacker, defender, isPlayerAttacking);
            hitStep.AnimType = isPlayerAttacking ? StepAnimType.EnemyHit : StepAnimType.PlayerHit;
            steps.Add(hitStep);

            //根据倍率插入克制提示 (像宝可梦一样的经典节奏)
            if (damageResult.TypeMultiplier > 1.1f)
            {
                steps.Add(CreateStep("效果拔群！", attacker, defender, isPlayerAttacking));
            }
            else if (damageResult.TypeMultiplier < 0.9f && damageResult.TypeMultiplier > 0.1f)
            {
                steps.Add(CreateStep("效果不太好...", attacker, defender, isPlayerAttacking));
            }
            else if (damageResult.TypeMultiplier <= 0.1f)
            {
                steps.Add(CreateStep("似乎没有效果...", attacker, defender, isPlayerAttacking));
            }
        }

        /// <summary>
        /// 辅助方法，用于快速生成当前的快照状态
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="attacker"></param>
        /// <param name="defender"></param>
        /// <param name="isPlayerAttacking"></param>
        /// <returns></returns>
        private TurnStep CreateStep(string msg, MonsterRuntime attacker, MonsterRuntime defender, bool isPlayerAttacking)
        {
            return new TurnStep
            {
                Message = msg,
                PlayerHpAfter = isPlayerAttacking ? attacker.CurrentHP : defender.CurrentHP,
                EnemyHpAfter = isPlayerAttacking ? defender.CurrentHP : attacker.CurrentHP,
                IsBattleEnd = false,
                AnimType = StepAnimType.None
            };
        }
    }
}