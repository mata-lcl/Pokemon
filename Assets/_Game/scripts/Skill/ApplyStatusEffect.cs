using Pokemon.Application;
using UnityEngine;

namespace Pokemon.Domain.Effects
{
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Pokemon/Effects/Apply Status")]
    public class ApplyStatusEffect : SkillEffectSO
    {
        public StatusCondition StatusToApply;
        //[Range(0f, 1f)] public float Chance = 1f;

        public override bool CanProcess(EffectContext context)
        {
            return context.Target != null && !context.Target.IsFainted && context.Target.CurrentStatus == StatusCondition.None;
        }

        public override void Execute(EffectContext context)
        {
            if (context.Target.TryApplyStatus(StatusToApply))
            {
                context.Steps.Add(new TurnStep
                {
                    Message = $"{context.Target.Species.DisplayName}{StatusToApply.ToChineseName()} 了!",
                    PlayerHpAfter = context.PlayerRef.CurrentHP,
                    EnemyHpAfter = context.EnemyRef.CurrentHP,
                    AnimType = Domain.StepAnimType.None // 挂状态不需要受击动画，或者你可以加个特定动画
                });
            }
            return;
            // 触发几率判定
            //if (Random.value <= Chance)
            //{
            //    // 【完美适配】：调用你写好的 TryApplyStatus 方法！
            //    bool success = context.Target.TryApplyStatus(StatusToApply);

            //    // 如果成功挂上了状态（之前没有其他状态且未倒下）
            //    if (success)
            //    {
            //        // 判断一下名字用于播报
            //        string statusName = StatusToApply == StatusCondition.Poison ? "中毒" :
            //                            StatusToApply == StatusCondition.Burn ? "灼烧" : StatusToApply.ToString();

            //        context.Steps.Add(new Application.TurnStep
            //        {
            //            Message = $"{context.Target.Species.DisplayName} {statusName}了！",
            //            PlayerHpAfter = context.PlayerRef.CurrentHP,
            //            EnemyHpAfter = context.EnemyRef.CurrentHP,
            //            AnimType = Domain.StepAnimType.None // 挂状态不需要受击动画，或者你可以加个特定动画
            //        });
            //    }
            //}
        }
    }
}