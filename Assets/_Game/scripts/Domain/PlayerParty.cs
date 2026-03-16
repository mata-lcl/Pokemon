using System.Collections.Generic;

namespace Pokemon.Domain
{
    /// <summary>
    /// 全局玩家队伍管理器。由于是 static，它的数据在游戏运行期间会一直驻留在内存中。
    /// </summary>
    public static class PlayerParty
    {
        public static MonsterRuntime ActivePokemon { get; set; }

        // 背包数据：道具名称(或ID) -> 持有数量
        // 为了简单，我们直接用 Dictionary 存数量
        public static Dictionary<ItemData, int> Inventory = new Dictionary<ItemData, int>();

        // 辅助方法：添加道具
        public static void AddItem(ItemData item, int count = 1)
        {
            if (Inventory.ContainsKey(item)) Inventory[item] += count;
            else Inventory[item] = count;
        }

        // 辅助方法：使用道具
        public static bool UseItem(ItemData item)
        {
            if (Inventory.ContainsKey(item) && Inventory[item] > 0)
            {
                Inventory[item]--;
                return true;
            }
            return false;
        }
    }
}