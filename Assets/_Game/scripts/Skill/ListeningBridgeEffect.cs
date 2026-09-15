using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "聆听桥接效果", menuName = "宝可梦/技能效果/聆听桥接")]
    public class ListeningBridgeEffect : SkillEffectSO, IReactionSkillEffect
    {
        [SerializeField, Range(0f, 1f)]
        private float statusPenaltyFraction = 1f / 3f;

        /// <summary>
        /// 阻止该效果在普通技能效果阶段重复执行。
        /// </summary>
        /// <param name="context">当前技能效果上下文。</param>
        /// <returns>始终返回 false，实际逻辑由反应技能管线调用。</returns>
        public override bool CanProcess(SkillEffectContext context) => false;

        /// <summary>
        /// 保留普通技能效果接口；听桥不在此阶段执行任何逻辑。
        /// </summary>
        /// <param name="context">当前技能效果上下文。</param>
        public override void Execute(SkillEffectContext context)
        {
        }

        /// <summary>
        /// 拦截攻击技能的伤害，并把同等伤害施加给原攻击者。
        /// </summary>
        /// <param name="reactionContext">听桥使用者及对方行动信息。</param>
        /// <param name="incomingContext">对方当前技能效果上下文。</param>
        /// <param name="incomingDamage">原本即将造成给听桥使用者的伤害。</param>
        /// <returns>成功反弹伤害时返回 true，否则返回 false。</returns>
        public bool TryInterceptDamage(
            SkillReactionContext reactionContext,
            SkillEffectContext incomingContext,
            int incomingDamage)
        {
            if (reactionContext.OpponentSkill == null ||
                reactionContext.OpponentSkill.Category == SkillCategory.Status ||
                incomingDamage <= 0)
            {
                return false;
            }

            reactionContext.AnnounceUse();
            BattleDamageResult reflectedResult =
                reactionContext.Operations.DealDamage(
                    new BattleDamageRequest
                    {
                        Source = reactionContext.User,
                        Target = incomingContext.User,
                        Skill = reactionContext.Skill,
                        Amount = incomingDamage,
                        Cause = BattleDamageCause.Reaction,
                        Origin = this
                    });
            reactionContext.HasTriggered = true;

            reactionContext.Events.Publish(new ReactionResolvedEvent
            {
                User = reactionContext.User,
                Opponent = incomingContext.User,
                Skill = reactionContext.Skill,
                Resolution = ReactionResolution.Reflected,
                DamageResult = reflectedResult
            });
            return true;
        }

        /// <summary>
        /// 在对方行动结束后处理未反弹或遇到变化技能时的听桥结果。
        /// </summary>
        /// <param name="context">听桥使用者及对方行动信息。</param>
        public void ResolveAfterOpponentAction(SkillReactionContext context)
        {
            if (context.OpponentSkill != null &&
                context.OpponentSkill.Category == SkillCategory.Status)
            {
                context.AnnounceUse();

                int penaltyDamage = Mathf.Max(
                    1,
                    Mathf.FloorToInt(context.User.MaxHP * statusPenaltyFraction));

                BattleDamageResult penaltyResult =
                    context.Operations.DealDamage(
                        new BattleDamageRequest
                        {
                            Source = context.Opponent,
                            Target = context.User,
                            Skill = context.Skill,
                            Amount = penaltyDamage,
                            Cause = BattleDamageCause.Reaction,
                            Origin = this
                        });
                context.Events.Publish(new ReactionResolvedEvent
                {
                    User = context.User,
                    Opponent = context.Opponent,
                    Skill = context.Skill,
                    Resolution = ReactionResolution.StatusPenalty,
                    DamageResult = penaltyResult
                });
                return;
            }

            if (!context.HasTriggered)
            {
                context.AnnounceUse();
                context.Events.Publish(new ReactionResolvedEvent
                {
                    User = context.User,
                    Opponent = context.Opponent,
                    Skill = context.Skill,
                    Resolution = ReactionResolution.Failed
                });
            }
        }
    }
}
