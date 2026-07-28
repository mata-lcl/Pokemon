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
        public IReactionSkillEffect IncomingReaction;
        public SkillReactionContext ReactionContext;

        // 【新增】：不论谁施法，这两个变量永远指向正确的 UI 阵营
        public MonsterRuntime PlayerRef;
        public MonsterRuntime EnemyRef;

        /// <summary>
        /// 添加一个 TurnStep 并自动填充双方 HP 快照，简化效果内部代码。
        /// </summary>
        /// <param name="message">战斗日志中显示的文字。</param>
        /// <param name="animType">该步骤需要播放的动画类型。</param>
        public void AddStep(string message, StepAnimType animType = StepAnimType.None)
        {
            Steps.Add(new Application.TurnStep
            {
                Message = message,
                PlayerHpAfter = PlayerRef.CurrentHP,
                EnemyHpAfter = EnemyRef.CurrentHP,
                AnimType = animType
            });
        }
    }

    /// <summary>
    /// 后手反应技能的执行上下文。具体反应规则由技能效果实现。
    /// </summary>
    public sealed class SkillReactionContext
    {
        public MonsterRuntime User;
        public MonsterRuntime Opponent;
        public SkillData Skill;
        public SkillData OpponentSkill;
        public List<Application.TurnStep> Steps;
        public MonsterRuntime PlayerRef;
        public MonsterRuntime EnemyRef;
        public bool IsUserPlayer;
        public bool HasTriggered;

        private bool _useAnnounced;

        /// <summary>
        /// 添加一次反应技能发动提示；同一反应上下文最多只添加一次。
        /// </summary>
        public void AnnounceUse()
        {
            if (_useAnnounced) return;
            _useAnnounced = true;
            AddStep($"{User.Species.DisplayName} 使用了 {Skill.DisplayName}！");
        }

        /// <summary>
        /// 为反应技能添加一个带有双方当前 HP 快照的播放步骤。
        /// </summary>
        /// <param name="message">战斗日志中显示的文字。</param>
        /// <param name="animType">该步骤需要播放的动画类型。</param>
        public void AddStep(string message, StepAnimType animType = StepAnimType.None)
        {
            Steps.Add(new Application.TurnStep
            {
                Message = message,
                PlayerHpAfter = PlayerRef.CurrentHP,
                EnemyHpAfter = EnemyRef.CurrentHP,
                AnimType = animType
            });
        }
    }

    /// <summary>
    /// 需要观察并响应对方行动的技能效果，例如伤害反弹或守护。
    /// </summary>
    public interface IReactionSkillEffect
    {
        /// <summary>
        /// 在伤害真正落到目标前尝试接管本次伤害。
        /// </summary>
        /// <param name="reactionContext">后手反应技能的上下文。</param>
        /// <param name="incomingContext">对方当前技能效果的上下文。</param>
        /// <param name="incomingDamage">原本即将造成的伤害。</param>
        /// <returns>已经接管伤害时返回 true，原 DamageEffect 将不再扣血。</returns>
        bool TryInterceptDamage(
            SkillReactionContext reactionContext,
            EffectContext incomingContext,
            int incomingDamage);

        /// <summary>
        /// 在对方行动结束后处理反应技能尚未完成的结果。
        /// </summary>
        /// <param name="context">后手反应技能的上下文。</param>
        void ResolveAfterOpponentAction(SkillReactionContext context);
    }

    public interface ISkillEffect
    {
        /// <summary>
        /// 判断该效果在当前技能上下文中是否允许执行。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        /// <returns>可以执行时返回 true，否则返回 false。</returns>
        bool CanProcess(EffectContext context);

        /// <summary>
        /// 执行效果并按需修改运行时数据、追加播放步骤。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        void Execute(EffectContext context);
    }
}
