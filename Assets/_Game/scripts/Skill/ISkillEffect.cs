using System.Collections.Generic;

namespace Pokemon.Domain
{
    // 战斗信息数据包，包含执行效果所需的全部信息
    public class EffectContext
    {
        public MonsterRuntime User;      // 使用者
        public MonsterRuntime Target;    // 目标
        public SkillData Skill;          // 技能数据
        public DamageResult? Damage;     // 威力计算结果（可选）
        public List<Application.TurnStep> Steps; // 记录步骤的引用
        public bool IsPlayerAttacking;
    }

    public interface ISkillEffect
    {
        // 核心检查：这个效果是否应该触发？（比如只有命中才触发，或者必定触发）
        bool CanProcess(EffectContext context);

        // 执行逻辑
        void Execute(EffectContext context);
    }
}