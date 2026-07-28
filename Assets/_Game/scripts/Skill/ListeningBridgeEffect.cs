using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "ListeningBridgeEffect", menuName = "Pokemon/Effects/Listening Bridge")]
    public class ListeningBridgeEffect : SkillEffectSO, IReactionSkillEffect
    {
        [SerializeField, Range(0f, 1f)]
        private float statusPenaltyFraction = 1f / 3f;

        /// <summary>
        /// 阻止该效果在普通技能效果阶段重复执行。
        /// </summary>
        /// <param name="context">当前技能效果上下文。</param>
        /// <returns>始终返回 false，实际逻辑由反应技能管线调用。</returns>
        public override bool CanProcess(EffectContext context) => false;

        /// <summary>
        /// 保留普通技能效果接口；听桥不在此阶段执行任何逻辑。
        /// </summary>
        /// <param name="context">当前技能效果上下文。</param>
        public override void Execute(EffectContext context)
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
            EffectContext incomingContext,
            int incomingDamage)
        {
            if (reactionContext.OpponentSkill == null ||
                reactionContext.OpponentSkill.Category == SkillCategory.Status ||
                incomingDamage <= 0)
            {
                return false;
            }

            reactionContext.AnnounceUse();
            incomingContext.User.ApplyDamage(incomingDamage);
            reactionContext.HasTriggered = true;

            StepAnimType reflectedHitAnim = reactionContext.IsUserPlayer
                ? StepAnimType.EnemyHit
                : StepAnimType.PlayerHit;

            reactionContext.AddStep(
                $"{reactionContext.User.Species.DisplayName} 将 {incomingDamage} 点伤害反弹了回去！",
                reflectedHitAnim);
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

                context.User.ApplyDamage(penaltyDamage);
                context.AddStep(
                    $"面对变化技能，{context.User.Species.DisplayName} 损失了 {penaltyDamage} 点生命值！",
                    context.IsUserPlayer ? StepAnimType.PlayerHit : StepAnimType.EnemyHit);
                return;
            }

            if (!context.HasTriggered)
            {
                context.AnnounceUse();
                context.AddStep(
                    $"{context.User.Species.DisplayName} 的 {context.Skill.DisplayName} 失败了！");
            }
        }
    }
}
