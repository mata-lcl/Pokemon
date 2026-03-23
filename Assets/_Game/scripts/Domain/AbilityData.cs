using Pokemon.Application;
using System.Collections.Generic;
using UnityEngine;

namespace Pokemon.Domain
{
    [CreateAssetMenu(fileName = "Ability", menuName = "Pokemon/Ability")]
    public class AbilityData : ScriptableObject
    {
        public int Id;
        public string AbilityName;
        [TextArea] public string Description;

        // 伤害倍率加成
        public virtual float GetDamageMultiplier(MonsterRuntime owner, MonsterRuntime opponent, SkillData skill, List<TurnStep> steps)
        {
            // 子类如果需要写日志，就往 steps 里 Add
            return 1.0f;
        }

        //虚函数，可以让子类选择性重写来处理特定的通知事件，比如回合开始、技能使用后等。
        public virtual bool CheckAndProcessNotification(MonsterRuntime owner, List<TurnStep> steps, MonsterRuntime playerRef, MonsterRuntime enemyRef)
        {
            // 默认不处理任何通知
            return false;
        }
    }

    // 未来可以在这里扩展特性的逻辑钩子，比如：
    // public virtual void OnBattleStart(MonsterRuntime owner, BattleContext context) { }
    // public virtual void OnDamageCalculated(DamageModifierContext context) { }
}
