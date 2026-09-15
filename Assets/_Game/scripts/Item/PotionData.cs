using UnityEngine;
using Pokemon.Domain;

[CreateAssetMenu(fileName = "恢复药_", menuName = "宝可梦/配置/道具/恢复药")]
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
        context.Operations.Heal(new BattleHealRequest
        {
            Source = context.User,
            Target = context.User,
            Amount = HealAmount,
            Cause = BattleHealCause.Item,
            Origin = this
        });

    }
}
