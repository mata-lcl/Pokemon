using Pokemon.Domain;
using System.Collections.Generic;

namespace Pokemon.Application
{
    public sealed class SkillActionResult
    {
        private readonly List<BattleDamageResult> _damageResults =
            new List<BattleDamageResult>();
        private readonly List<BattleHealResult> _healResults =
            new List<BattleHealResult>();
        private readonly List<StatusApplyResult> _statusResults =
            new List<StatusApplyResult>();

        public bool HitSucceeded { get; private set; }
        public IReadOnlyList<BattleDamageResult> DamageResults => _damageResults;
        public IReadOnlyList<BattleHealResult> HealResults => _healResults;
        public IReadOnlyList<StatusApplyResult> StatusResults => _statusResults;
        public BattleDamageResult LastDamage => _damageResults.Count > 0
            ? _damageResults[_damageResults.Count - 1]
            : null;
        public BattleHealResult LastHealing => _healResults.Count > 0
            ? _healResults[_healResults.Count - 1]
            : null;
        public StatusApplyResult LastStatusApplication => _statusResults.Count > 0
            ? _statusResults[_statusResults.Count - 1]
            : null;

        /// <summary>
        /// 标记本次技能行动已经通过命中判定。
        /// </summary>
        public void MarkHitSucceeded()
        {
            HitSucceeded = true;
        }

        /// <summary>
        /// 记录本次技能行动通过统一入口产生的伤害结果。
        /// </summary>
        /// <param name="result">需要记录的实际伤害结果。</param>
        public void RecordDamage(BattleDamageResult result)
        {
            _damageResults.Add(result);
        }

        /// <summary>
        /// 记录本次技能行动通过统一入口产生的治疗结果。
        /// </summary>
        /// <param name="result">需要记录的实际治疗结果。</param>
        public void RecordHealing(BattleHealResult result)
        {
            _healResults.Add(result);
        }

        /// <summary>
        /// 记录本次技能行动通过统一入口产生的异常状态附加结果。
        /// </summary>
        /// <param name="result">需要记录的状态附加结果。</param>
        public void RecordStatusApplication(StatusApplyResult result)
        {
            _statusResults.Add(result);
        }

        /// <summary>
        /// 查找指定来源、目标和原因对应的最近一次伤害结果。
        /// </summary>
        /// <param name="source">伤害来源。</param>
        /// <param name="target">承受伤害的目标。</param>
        /// <param name="cause">需要匹配的伤害原因。</param>
        /// <returns>最近一次匹配的伤害结果；没有时返回 null。</returns>
        public BattleDamageResult GetLatestDamage(
            MonsterRuntime source,
            MonsterRuntime target,
            BattleDamageCause cause)
        {
            for (int i = _damageResults.Count - 1; i >= 0; i--)
            {
                BattleDamageResult result = _damageResults[i];
                if (result.Source == source &&
                    result.Target == target &&
                    result.Cause == cause)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 汇总指定来源、目标和原因在本次行动中实际造成的全部伤害。
        /// </summary>
        /// <param name="source">伤害来源。</param>
        /// <param name="target">承受伤害的目标。</param>
        /// <param name="cause">需要匹配的伤害原因。</param>
        /// <returns>所有匹配伤害结果的实际伤害总和。</returns>
        public int GetTotalDamage(
            MonsterRuntime source,
            MonsterRuntime target,
            BattleDamageCause cause)
        {
            int totalDamage = 0;
            foreach (BattleDamageResult result in _damageResults)
            {
                if (result.Source == source &&
                    result.Target == target &&
                    result.Cause == cause)
                {
                    totalDamage += result.AppliedDamage;
                }
            }

            return totalDamage;
        }
    }
}
