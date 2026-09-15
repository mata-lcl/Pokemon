using Pokemon.Domain;
using System;

namespace Pokemon.Application
{
    public sealed class BattleOperations : IBattleOperations
    {
        private readonly IBattleEventDispatcher _eventDispatcher;

        /// <summary>
        /// 创建一个使用独立事件分发器的统一战斗操作入口。
        /// </summary>
        public BattleOperations()
            : this(new BattleEventDispatcher())
        {
        }

        /// <summary>
        /// 创建统一战斗操作入口，并把结算结果发布给指定事件分发器。
        /// </summary>
        /// <param name="eventDispatcher">接收伤害、治疗和状态结果的事件分发器。</param>
        public BattleOperations(IBattleEventDispatcher eventDispatcher)
        {
            _eventDispatcher = eventDispatcher;
        }

        /// <summary>
        /// 通过统一战斗入口对目标造成伤害并返回实际扣除结果。
        /// </summary>
        /// <param name="request">伤害来源、目标、数值和原因。</param>
        /// <returns>目标扣血前后状态及实际伤害。</returns>
        public BattleDamageResult DealDamage(BattleDamageRequest request)
        {
            int requestedAmount = Math.Max(0, request.Amount);
            int previousHP = request.Target.CurrentHP;
            request.Target.ApplyDamage(requestedAmount);
            var result = new BattleDamageResult
            {
                Source = request.Source,
                Target = request.Target,
                Skill = request.Skill,
                RequestedAmount = requestedAmount,
                Cause = request.Cause,
                Origin = request.Origin,
                PreviousHP = previousHP,
                CurrentHP = request.Target.CurrentHP,
                AppliedDamage = previousHP - request.Target.CurrentHP
            };
            _eventDispatcher.Publish(new DamageAppliedEvent { Result = result });
            return result;
        }

        /// <summary>
        /// 通过统一战斗入口治疗目标并返回实际恢复结果。
        /// </summary>
        /// <param name="request">治疗来源、目标、数值和原因。</param>
        /// <returns>目标治疗前后状态及实际恢复量。</returns>
        public BattleHealResult Heal(BattleHealRequest request)
        {
            int requestedAmount = Math.Max(0, request.Amount);
            int previousHP = request.Target.CurrentHP;
            request.Target.Heal(requestedAmount);
            var result = new BattleHealResult
            {
                Source = request.Source,
                Target = request.Target,
                Skill = request.Skill,
                RequestedAmount = requestedAmount,
                Cause = request.Cause,
                Origin = request.Origin,
                PreviousHP = previousHP,
                CurrentHP = request.Target.CurrentHP,
                AppliedHealing = request.Target.CurrentHP - previousHP
            };
            _eventDispatcher.Publish(new HealingAppliedEvent { Result = result });
            return result;
        }

        /// <summary>
        /// 通过统一战斗入口为目标附加异常状态。
        /// </summary>
        /// <param name="request">状态来源、目标、状态类型和原因。</param>
        /// <returns>附加前后状态及是否成功。</returns>
        public StatusApplyResult ApplyStatus(StatusApplyRequest request)
        {
            StatusCondition previousStatus = request.Target.CurrentStatus;
            bool applied = request.Target.TryApplyStatus(request.Status);
            var result = new StatusApplyResult
            {
                Source = request.Source,
                Target = request.Target,
                Skill = request.Skill,
                RequestedStatus = request.Status,
                Cause = request.Cause,
                Origin = request.Origin,
                PreviousStatus = previousStatus,
                CurrentStatus = request.Target.CurrentStatus,
                Applied = applied
            };
            _eventDispatcher.Publish(new StatusAppliedEvent { Result = result });
            return result;
        }
    }

    internal sealed class SkillActionOperations : IBattleOperations
    {
        private readonly IBattleOperations _operations;
        private readonly SkillActionResult _actionResult;

        /// <summary>
        /// 创建一次技能行动专用的操作适配器，并自动收集该行动的伤害结果。
        /// </summary>
        /// <param name="operations">实际修改战斗状态的统一操作入口。</param>
        /// <param name="actionResult">接收本次行动伤害记录的结果对象。</param>
        public SkillActionOperations(
            IBattleOperations operations,
            SkillActionResult actionResult)
        {
            _operations = operations;
            _actionResult = actionResult;
        }

        /// <summary>
        /// 执行伤害操作并自动记录本次技能行动的实际伤害。
        /// </summary>
        /// <param name="request">伤害来源、目标、数值和原因。</param>
        /// <returns>目标扣血前后状态及实际伤害。</returns>
        public BattleDamageResult DealDamage(BattleDamageRequest request)
        {
            BattleDamageResult result = _operations.DealDamage(request);
            _actionResult.RecordDamage(result);
            return result;
        }

        /// <summary>
        /// 执行治疗操作并返回目标的实际恢复结果。
        /// </summary>
        /// <param name="request">治疗来源、目标、数值和原因。</param>
        /// <returns>目标治疗前后状态及实际恢复量。</returns>
        public BattleHealResult Heal(BattleHealRequest request)
        {
            BattleHealResult result = _operations.Heal(request);
            _actionResult.RecordHealing(result);
            return result;
        }

        /// <summary>
        /// 执行异常状态附加操作并返回实际结果。
        /// </summary>
        /// <param name="request">状态来源、目标、状态类型和原因。</param>
        /// <returns>附加前后状态及是否成功。</returns>
        public StatusApplyResult ApplyStatus(StatusApplyRequest request)
        {
            StatusApplyResult result = _operations.ApplyStatus(request);
            _actionResult.RecordStatusApplication(result);
            return result;
        }
    }
}
