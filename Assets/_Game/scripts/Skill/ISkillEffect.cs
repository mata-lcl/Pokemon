namespace Pokemon.Domain
{
    // 战斗信息数据包，包含执行效果所需的全部信息
    public class EffectContext
    {
        public MonsterRuntime User;      // 使用者
        public MonsterRuntime Target;    // 目标
        public IBattleOperations Operations;
        public IBattleRuntimeEffectRegistry RuntimeEffects;
        public IBattleDomainEventSink Events;
    }

    /// <summary>
    /// 技能效果执行期间使用的上下文，提供技能数据、计算结果和统一战斗操作入口。
    /// </summary>
    public sealed class SkillEffectContext : EffectContext
    {
        public SkillData Skill;
        public DamageResult? Damage;
        public SkillActionResult ActionResult;
        public IReactionSkillEffect IncomingReaction;
        public SkillReactionContext ReactionContext;
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
        public bool HasTriggered;
        public IBattleOperations Operations;
        public IBattleRuntimeEffectRegistry RuntimeEffects;
        public IBattleDomainEventSink Events;
        public SkillActionResult ActionResult;

        private bool _useAnnounced;

        /// <summary>
        /// 添加一次反应技能发动提示；同一反应上下文最多只添加一次。
        /// </summary>
        public void AnnounceUse()
        {
            if (_useAnnounced) return;
            _useAnnounced = true;
            Events.Publish(new SkillUseStartedEvent
            {
                Actor = User,
                Skill = Skill,
                IsReactionAnnouncement = true
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
            SkillEffectContext incomingContext,
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
        bool CanProcess(SkillEffectContext context);

        /// <summary>
        /// 执行效果并按需修改运行时数据、发布领域事件。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        void Execute(SkillEffectContext context);
    }
}
