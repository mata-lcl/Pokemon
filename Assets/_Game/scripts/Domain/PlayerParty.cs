using System.Collections.Generic;
using System.Linq; // 用于方便筛选

namespace Pokemon.Domain
{
    public static class PlayerParty
    {
        public static MonsterRuntime ActivePokemon { get; set; }
        public static Dictionary<ItemData, int> Inventory = new Dictionary<ItemData, int>();

        public static void AddItem(ItemData item, int count = 1)
        {
            if (Inventory.ContainsKey(item)) Inventory[item] += count;
            else Inventory[item] = count;
        }

        // 仅在逻辑执行成功后扣除（由 BattleCoordinator 调用）
        public static void RemoveItem(ItemData item, int count = 1)
        {
            if (Inventory.ContainsKey(item))
            {
                Inventory[item] -= count;
                if (Inventory[item] <= 0) Inventory.Remove(item);
            }
        }

        // --- 辅助方法：获取不同类型的道具列表（用于 UI 显示） ---

        // 获取所有可主动使用的道具（伤药、精灵球）
        public static List<ItemData> GetUsableItems() =>
            Inventory.Keys.Where(i => i is IUsable).ToList();

        // 获取所有可携带的道具（丝绸围巾等）
        public static List<ItemData> GetHeldItems() =>
            Inventory.Keys.Where(i => i is IHeldTrigger).ToList();
    }
}