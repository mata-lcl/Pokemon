using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(
        fileName = "储存伤害释放效果",
        menuName = "宝可梦/技能效果/专属效果/储存伤害后释放")]
    public sealed class StoredDamageReleaseEffect : SkillEffectSO
    {
        [Tooltip("从施放后的下一个完整回合开始，持续记录伤害的回合数")]
        [SerializeField, Min(1)] private int storageTurns = 3;

        [Tooltip("到期时释放伤害占累计实际受伤量的倍率")]
        [SerializeField, Min(0f)] private float releaseMultiplier = 1f;

        /// <summary>
        /// 判断使用者是否可以进入伤害储存状态。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        /// <returns>使用者没有倒下时返回 true。</returns>
        public override bool CanProcess(SkillEffectContext context)
        {
            return !context.User.IsFainted;
        }

        /// <summary>
        /// 为使用者注册一份独立的伤害储存运行时效果。
        /// </summary>
        /// <param name="context">当前技能效果的执行上下文。</param>
        public override void Execute(SkillEffectContext context)
        {
            context.RuntimeEffects.Register(
                new StoredDamageRuntimeEffect(
                    context.User,
                    context.Skill,
                    this,
                    storageTurns,
                    releaseMultiplier));

            context.Events.Publish(new StoredDamageEffectStartedEvent
            {
                Owner = context.User,
                Skill = context.Skill,
                StorageTurns = storageTurns
            });
        }

        /// <summary>
        /// 保存单次施放对应的累计伤害、剩余回合和释放倍率。
        /// </summary>
        private sealed class StoredDamageRuntimeEffect : BattleRuntimeEffect
        {
            private int _remainingTurns;
            private readonly float _releaseMultiplier;
            private int _storedDamage;
            private bool _hasStarted;

            /// <summary>
            /// 创建一份只属于本次施放的伤害储存状态。
            /// </summary>
            /// <param name="owner">持有伤害储存状态的角色。</param>
            /// <param name="sourceSkill">创建状态的技能。</param>
            /// <param name="origin">创建状态的技能效果资产。</param>
            /// <param name="storageTurns">需要记录伤害的完整回合数。</param>
            /// <param name="releaseMultiplier">累计伤害转换为释放伤害的倍率。</param>
            public StoredDamageRuntimeEffect(
                MonsterRuntime owner,
                SkillData sourceSkill,
                object origin,
                int storageTurns,
                float releaseMultiplier)
                : base(owner, sourceSkill, origin)
            {
                _remainingTurns = storageTurns;
                _releaseMultiplier = releaseMultiplier;
            }

            /// <summary>
            /// 从技能施放后的下一个完整回合开始记录伤害。
            /// </summary>
            /// <param name="context">当前回合的运行时效果上下文。</param>
            public override void OnTurnStarted(BattleRuntimeEffectContext context)
            {
                _hasStarted = true;
            }

            /// <summary>
            /// 累计效果生效期间持有者实际承受的伤害。
            /// </summary>
            /// <param name="result">统一伤害入口产生的实际结算结果。</param>
            public override void OnDamageApplied(BattleDamageResult result)
            {
                if (!_hasStarted || result.Target != Owner) return;

                _storedDamage += result.AppliedDamage;
            }

            /// <summary>
            /// 扣除剩余回合，并在最后一回合结束时向当前对手释放累计伤害。
            /// </summary>
            /// <param name="context">当前回合的运行时效果上下文。</param>
            public override void OnTurnEnded(BattleRuntimeEffectContext context)
            {
                if (!_hasStarted) return;

                _remainingTurns--;
                if (_remainingTurns > 0) return;

                MonsterRuntime target = Owner == context.Player
                    ? context.Enemy
                    : context.Player;
                int releaseDamage = Mathf.RoundToInt(
                    _storedDamage * _releaseMultiplier);

                if (releaseDamage > 0)
                {
                    BattleDamageResult result = context.Operations.DealDamage(
                        new BattleDamageRequest
                        {
                            Source = Owner,
                            Target = target,
                            Skill = SourceSkill,
                            Amount = releaseDamage,
                            Cause = BattleDamageCause.RuntimeEffect,
                            Origin = this
                        });
                    context.Events.Publish(new StoredDamageEffectReleasedEvent
                    {
                        Owner = Owner,
                        Target = target,
                        DamageResult = result
                    });
                }
                else
                {
                    context.Events.Publish(new StoredDamageEffectReleasedEvent
                    {
                        Owner = Owner,
                        Target = target
                    });
                }

                Expire();
            }
        }
    }
}
