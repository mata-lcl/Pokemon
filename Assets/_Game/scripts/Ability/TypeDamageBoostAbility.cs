using Pokemon.Application;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    // 增加一个菜单项，可以创建出不同的实例
    [CreateAssetMenu(fileName = "NewTypeBoostAbility", menuName = "Pokemon/Abilities/Type Boost")]
    public class TypeDamageBoostAbility : AbilityData
    {
        [Header("触发设置")]
        [SerializeField] private PokemonType targetType; // 触发加成的属性（如：Grass）
        [SerializeField] private float hpThreshold = 0.333f; // 触发的血量阀值（1/3）
        [SerializeField] private float multiplier = 1.5f;    // 加成倍率  

        public override float GetDamageMultiplier(MonsterRuntime owner, MonsterRuntime opponent, SkillData skill, List<TurnStep> steps)
        {
            // 1. 检查属性
            if (skill.Type != targetType) return 1.0f;

            // 2. 检查血量（使用乘法避免浮点误差）
            bool isLowHP = (owner.CurrentHP <= owner.MaxHP * hpThreshold);

            if (isLowHP)
            {
                //······················
                //if (steps != null)
                //{
                //    // 在您的架构中，我们需要判断当前 owner 是谁来填充 HP 快照
                //    // 假设我们处于触发者的回合(owner 即为攻击者)
                //    steps?.Add(new TurnStep
                //    {
                //        //如果需要可以在这里增加其他技能的说明
                //        Message = $"{owner.Species.DisplayName} 的 {this.AbilityName} 发动了！",

                //        // 这里需要一种方式知道谁是玩家。
                //        // 暂时记录当前血量。注意：这里需要确保 UI 不会因为这些快照导致血条回退
                //        PlayerHpAfter = owner.CurrentHP, // 这部分建议通过注入或参数获取实际的实时引用
                //        EnemyHpAfter = opponent.CurrentHP,
                //        IsBattleEnd = false,
                //        AnimType = StepAnimType.None
                //    });
                //}
                //······················
                Debug.Log($"<color=orange>[特性触发]</color> {owner.Species.DisplayName} 触发了猛火！倍率 x{multiplier}");
                return multiplier; // 通常为 1.5f
            }

            return 1.0f;
        }

        // 只有自己知道什么时候该弹窗
        public override bool CheckAndProcessNotification(MonsterRuntime owner, List<TurnStep> steps, MonsterRuntime playerRef, MonsterRuntime enemyRef)
        {
            // 如果血量低于 1/3 且未倒下
            if (owner.CurrentHP <= owner.MaxHP * hpThreshold && !owner.HasTriggeredCrisisAbility)
            {
                // 这里可以加一个标记防止重复弹出，例如 owner.HasAbilityTriggered
                owner.HasTriggeredCrisisAbility = true; // 标记本场战斗已触发过，防止刷屏
                steps.Add(new TurnStep
                {
                    Message = $"{owner.Species.DisplayName} 的 {AbilityName} 发动了！",
                    PlayerHpAfter = playerRef.CurrentHP,
                    EnemyHpAfter = enemyRef.CurrentHP,
                    AnimType = StepAnimType.None
                });
                return true;
            }
            return false;
        }
    }
}