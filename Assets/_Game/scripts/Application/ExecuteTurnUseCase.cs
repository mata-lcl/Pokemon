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
            if (skill == null) return;

            // 【修复 1】：在使用技能前，先生成一个预告步骤，触发攻击（冲撞）动画！
            steps.Add(CreateStep(
                $"{attacker.Species.DisplayName} 使用了 {skill.DisplayName}！",
                attacker, defender, isPlayerAttacking,
                isPlayerAttacking ? StepAnimType.PlayerAttack : StepAnimType.EnemyAttack // 设定攻击动画
            ));

            // 1. 命中判定
            if (!_damageCalculator.CheckHit(skill))
            {
                steps.Add(new TurnStep { Message = $"{skill.DisplayName} 未命中！" });
                return;
            }

            // 2. 预计算伤害 (如果是物理/特殊攻击)
            DamageResult? dmg = null;
            if (skill.Category != SkillCategory.Status)
            {
                dmg = _damageCalculator.CalculateDamage(attacker, defender, skill);
            }

            // 3. 构建上下文
            var context = new EffectContext
            {
                User = attacker,
                Target = defender,
                Skill = skill,
                Damage = dmg,
                Steps = steps,
                IsPlayerAttacking = isPlayerAttacking // 【新增】传给特效模块
            };

            // 4. 执行所有效果 (自动遍历，不再需要 if-else)
            foreach (var effect in skill.GetEffects())
            {
                if (effect.CanProcess(context))
                {
                    effect.Execute(context);
                }
            }

            // 5. 更新每一步的最新的血量状态 (统一修正)
            UpdateStepsHp(steps, isPlayerAttacking, attacker, defender);
        }

        // 统一更新血量的方法，防止在每个 Effect 里判断谁是玩家
        private void UpdateStepsHp(List<TurnStep> steps, bool isPlayerAttacking, MonsterRuntime attacker, MonsterRuntime defender)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                if (isPlayerAttacking)
                {
                    s.PlayerHpAfter = attacker.CurrentHP;
                    s.EnemyHpAfter = defender.CurrentHP;
                }
                else
                {
                    s.PlayerHpAfter = defender.CurrentHP;
                    s.EnemyHpAfter = attacker.CurrentHP;
                }
                steps[i] = s;
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
            // 1. 基础拦截：如果已经倒下，直接返回
            if (target.IsFainted) return;

            // 2. 将状态和最大血量丢给规则类，让它告诉我扣多少血、叫什么名字
            if (StatusMechanics.TryGetEndOfTurnDamage(target.CurrentStatus, target.MaxHP, out int damage, out string statusName))
            {
                // 3. 只有当返回 true 时（确实需要扣血），才执行扣血和动画逻辑
                target.ApplyDamage(damage);

                steps.Add(new TurnStep
                {
                    Message = $"{target.Species.DisplayName} 因为{statusName}受到了 {damage} 点伤害！",
                    PlayerHpAfter = playerRef.CurrentHP,
                    EnemyHpAfter = enemyRef.CurrentHP,
                    IsBattleEnd = false,
                    AnimType = isPlayer ? StepAnimType.PlayerHit : StepAnimType.EnemyHit
                });
            }
        }

        // 【修复 2】：修改你原有的 CreateStep，让它支持传入 AnimType，减少重复代码
        private TurnStep CreateStep(string msg, MonsterRuntime attacker, MonsterRuntime defender, bool isPlayerAttacking, StepAnimType animType = StepAnimType.None)
        {
            return new TurnStep
            {
                Message = msg,
                PlayerHpAfter = isPlayerAttacking ? attacker.CurrentHP : defender.CurrentHP,
                EnemyHpAfter = isPlayerAttacking ? defender.CurrentHP : attacker.CurrentHP,
                IsBattleEnd = false,
                AnimType = animType // 赋予动画类型
            };
        }
    }
}