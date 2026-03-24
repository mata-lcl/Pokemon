using System.Collections.Generic;

namespace Pokemon.Domain
{
    // 主动使用接口：用于玩家在战斗菜单或背包中点击使用
    public interface IUsable
    {
        // 消耗性检查：使用成功后是否减少数量
        bool IsConsumable { get; }
        // 逻辑检查：现在能用吗？（比如血满时不能用伤药）
        bool CanUse(EffectContext context);
        // 执行效果
        void OnUse(EffectContext context);
    }

    // 携带触发接口：用于战斗逻辑中的生命周期钩子
    public interface IHeldTrigger
    {
        // 攻击前钩子：用于威力加成
        void OnBeforeAttack(EffectContext context);
        // 回合结束钩子：用于每回合回血等
        void OnTurnEnd(MonsterRuntime owner, List<Application.TurnStep> steps);
    }
}