using Pokemon.Domain;
using System.Collections.Generic;
using UnityEngine;

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
    // 定义一个回合内的单个"步骤/画面"
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

        public ExecuteTurnUseCase(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        public List<TurnStep> Execute(
            MonsterRuntime player, SkillData playerSkill,
            MonsterRuntime enemy, SkillData enemySkill)
        {
            var steps = new List<TurnStep>();

            // 1. 判断先后手
            bool playerFirst = player.Speed >= enemy.Speed;

            if (playerFirst)
            {
                // 玩家行动
                ResolveAction(player, playerSkill, enemy, true, steps);
                // 立即检查敌人是否倒下
                if (CheckAndRecordFaint(player, enemy, steps)) return steps; 

                // 敌人行动
                ResolveAction(enemy, enemySkill, player, false, steps);
                // 立即检查玩家是否倒下
                if (CheckAndRecordFaint(player, enemy, steps)) return steps;
            }
            else
            {
                // 敌人行动
                ResolveAction(enemy, enemySkill, player, false, steps);
                if (CheckAndRecordFaint(player, enemy, steps)) return steps;

                // 玩家行动
                ResolveAction(player, playerSkill, enemy, true, steps);
                if (CheckAndRecordFaint(player, enemy, steps)) return steps;
            }

            // 2. 回合末尾结算阶段（例如中毒扣血）
            ResolveEndOfTurn(player, enemy, steps);
            
            // 再次检查末尾阶段是否有人被毒死
            CheckAndRecordFaint(player, enemy, steps);

            return steps;
        }

        // 抽取出来的辅助方法：检查是否有宝可梦倒下并记录
        private bool CheckAndRecordFaint(MonsterRuntime player, MonsterRuntime enemy, List<TurnStep> steps)
        {
            if (player.IsFainted || enemy.IsFainted)
            {
                steps.Add(new TurnStep
                {
                    Message = player.IsFainted ? $"{player.Species.DisplayName} 倒下了..." : $"{enemy.Species.DisplayName} 倒下了！",
                    PlayerHpAfter = player.CurrentHP,
                    EnemyHpAfter = enemy.CurrentHP,
                    IsBattleEnd = true,
                    PlayerWon = enemy.IsFainted,
                    AnimType = StepAnimType.None
                });
                return true;
            }
            return false;
        }

        private void ResolveAction(
            MonsterRuntime attacker, SkillData skill,
            MonsterRuntime defender, bool isPlayerAttacking,
            List<TurnStep> steps)
        {
            string attackerName = attacker.Species.DisplayName;

            // 1. PP 检查
            if (skill == null || !attacker.TryConsumePP(skill))
            {
                steps.Add(CreateStep($"{attackerName}想使用技能，但是PP不足！", attacker, defender, isPlayerAttacking));
                return;
            }

            // 2. 命中检查
            bool hit = _damageCalculator.CheckHit(skill);
            
            // 提示：使用了XX技能 (无论中没中都要先提示)
            var useStep = CreateStep($"{attackerName}使用了 {skill.DisplayName}！", attacker, defender, isPlayerAttacking);
            useStep.AnimType = isPlayerAttacking ? StepAnimType.PlayerAttack : StepAnimType.EnemyAttack;
            steps.Add(useStep);

            if (!hit)
            {
                steps.Add(CreateStep("但是未命中！", attacker, defender, isPlayerAttacking));
                return;
            }

            // 3. 效果处理
            if (skill.Category == SkillCategory.Status && skill.ApplyStatus == StatusCondition.Heal)
            {
                attacker.Heal(skill.EffectValue);
                steps.Add(CreateStep($"{attackerName} 回复了 {skill.EffectValue} 点生命值！", attacker, defender, isPlayerAttacking));
            }
            else
            {
                // 伤害计算
                var damageResult = _damageCalculator.CalculateDamage(attacker, defender, skill);
                defender.ApplyDamage(damageResult.FinalDamage);

                // 受击动画步骤
                var hitStep = CreateStep($"造成了 {damageResult.FinalDamage} 点伤害！", attacker, defender, isPlayerAttacking);
                hitStep.AnimType = isPlayerAttacking ? StepAnimType.EnemyHit : StepAnimType.PlayerHit;
                steps.Add(hitStep);

                // 克制关系文本
                if (damageResult.TypeMultiplier > 1.1f) steps.Add(CreateStep("效果拔群！", attacker, defender, isPlayerAttacking));
                else if (damageResult.TypeMultiplier < 0.9f && damageResult.TypeMultiplier > 0.1f) steps.Add(CreateStep("效果不太好...", attacker, defender, isPlayerAttacking));
                else if (damageResult.TypeMultiplier <= 0.1f) steps.Add(CreateStep("似乎没有效果...", attacker, defender, isPlayerAttacking));

                // 异常状态附加
                if (!defender.IsFainted && skill.ApplyStatus != StatusCondition.None && skill.ApplyStatus != StatusCondition.Heal)
                {
                    if (Random.value <= skill.StatusChance)
                    {
                        if (defender.TryApplyStatus(skill.ApplyStatus))
                        {
                            string statusMsg = skill.ApplyStatus == StatusCondition.Poison ? "中毒了！" : "进入了特殊状态";
                            steps.Add(CreateStep($"{defender.Species.DisplayName} {statusMsg}", attacker, defender, isPlayerAttacking));
                        }
                    }
                }
            }
        }

        private void ResolveEndOfTurn(MonsterRuntime player, MonsterRuntime enemy, List<TurnStep> steps)
        {
            ProcessStatusDamage(player, true, steps, player, enemy);
            if (!enemy.IsFainted) // 如果玩家被毒死了就不用判敌人了
                ProcessStatusDamage(enemy, false, steps, player, enemy);
        }

        private void ProcessStatusDamage(MonsterRuntime target, bool isPlayer, List<TurnStep> steps, MonsterRuntime playerRef, MonsterRuntime enemyRef)
        {
            if (target.IsFainted || target.CurrentStatus != StatusCondition.Poison) return;

            int poisonDamage = Mathf.Max(1, target.MaxHP / 8); // 应该是基于最大生命值 MaxHP
            target.ApplyDamage(poisonDamage);

            steps.Add(new TurnStep
            {
                Message = $"{target.Species.DisplayName} 因为中毒受到了 {poisonDamage} 点伤害！",
                PlayerHpAfter = playerRef.CurrentHP,
                EnemyHpAfter = enemyRef.CurrentHP,
                IsBattleEnd = false,
                AnimType = isPlayer ? StepAnimType.PlayerHit : StepAnimType.EnemyHit
            });
        }

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