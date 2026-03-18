using UnityEngine;

namespace Pokemon.Domain
{
    // 异常状态机制类：专门存放各种状态的硬核运算规则
    public static class StatusMechanics
    {
        /// <summary>
        /// 尝试获取回合末尾的异常状态伤害
        /// </summary>
        /// <returns>如果有伤害则返回 true，否则返回 false</returns>
        public static bool TryGetEndOfTurnDamage(StatusCondition status, int maxHp, out int damage, out string statusName)
        {
            switch (status)
            {
                case StatusCondition.Poison:
                    damage = Mathf.Max(1, maxHp / 8); // 中毒扣 1/8
                    statusName = "中毒";
                    return true;

                case StatusCondition.Burn:
                    damage = Mathf.Max(1, maxHp / 16); // 灼烧扣 1/16
                    statusName = "灼烧";
                    return true;

                // 未来你可以在这里加上"寄生种子"、"剧毒（递增伤害）"等逻辑
                // case StatusCondition.BadlyPoisoned: ...

                default:
                    // 睡眠、麻痹、冰冻 或者 None，回合末不扣血
                    damage = 0;
                    statusName = string.Empty;
                    return false;
            }
        }
    }
}