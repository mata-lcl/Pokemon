using UnityEngine;
using Pokemon.Domain;
using Pokemon.Application;

[CreateAssetMenu(fileName = "Potion", menuName = "Pokemon/Item/Potion")]
public class PotionData : ItemData, IUsable
{
    public int HealAmount = 20;

    public bool IsConsumable => true; // 药水用完就没了

    // 逻辑检查：如果血满，则不能使用
    public bool CanUse(EffectContext context)
    {
        return context.User.CurrentHP < context.User.MaxHP;
    }

    public void OnUse(EffectContext context)
    {
        // 1. 执行血量恢复
        context.User.Heal(HealAmount);

        // 2. 添加到动画/消息队列中
        context.Steps.Add(new TurnStep
        {
            Message = $"{context.User.Species.DisplayName} 恢复了 {HealAmount} 点生命值！",
            PlayerHpAfter = context.User.CurrentHP, // 更新当前血量到 UI
            EnemyHpAfter = context.Target.CurrentHP,
            AnimType = StepAnimType.None // 或者添加一个恢复特效的枚举
        });
    }
}