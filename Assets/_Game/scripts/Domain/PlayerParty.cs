using System;
using System.Collections.Generic;
using System.Linq;

namespace Pokemon.Domain
{
    public enum PokemonCollectionType
    {
        Party,
        Storage
    }

    public readonly struct InventoryItemStack
    {
        public ItemData Item { get; }
        public int Count { get; }

        /// <summary>
        /// 创建供界面读取的道具堆叠数据。
        /// </summary>
        /// <param name="item">道具定义。</param>
        /// <param name="count">道具数量。</param>
        public InventoryItemStack(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    public static class PlayerParty
    {
        public const int MaxPartySize = 6;

        public static MonsterRuntime ActivePokemon { get; set; }

        // 保留队伍列表的公开访问，以兼容现有代码；后续写入操作逐步迁移到下方方法中。
        public static List<MonsterRuntime> Party = new List<MonsterRuntime>();
        public static List<MonsterRuntime> Storage = new List<MonsterRuntime>();
        public static Dictionary<ItemData, int> Inventory = new Dictionary<ItemData, int>();

        private static readonly List<ItemData> ItemOrder = new List<ItemData>();

        public static event Action PartyChanged;
        public static event Action InventoryChanged;

        /// <summary>
        /// 移除无效的队伍成员，将超出队伍上限的成员移入仓库，并在当前出战宝可梦为空
        /// 或不再属于队伍时恢复当前出战宝可梦引用。
        /// </summary>
        public static void NormalizeParty()
        {
            Party.RemoveAll(monster => monster == null);

            while (Party.Count > MaxPartySize)
            {
                int lastIndex = Party.Count - 1;
                MonsterRuntime overflow = Party[lastIndex];
                Party.RemoveAt(lastIndex);
                if (overflow != null && !Storage.Contains(overflow))
                    Storage.Add(overflow);
            }

            EnsureActivePokemon();
        }

        /// <summary>
        /// 使用队伍中的第一只宝可梦作为默认出战宝可梦。
        /// 如果当前出战选择仍然有效，则保留现有选择。
        /// </summary>
        public static void EnsureActivePokemon()
        {
            if (Party.Count == 0)
            {
                ActivePokemon = null;
                return;
            }

            if (ActivePokemon == null || !Party.Contains(ActivePokemon))
                ActivePokemon = Party[0];
        }

        /// <summary>
        /// 返回可以替换当前出战宝可梦的队伍成员。
        /// </summary>
        public static List<MonsterRuntime> GetSwitchablePokemon()
        {
            NormalizeParty();
            return Party
                .Where(monster => monster != null && monster != ActivePokemon && !monster.IsFainted)
                .ToList();
        }

        /// <summary>
        /// 当指定成员属于当前队伍时，将其设置为出战宝可梦。
        /// </summary>
        public static bool TrySetActivePokemon(MonsterRuntime monster)
        {
            NormalizeParty();
            if (monster == null || !Party.Contains(monster) || monster.IsFainted)
                return false;

            ActivePokemon = monster;
            PartyChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 队伍有空位时将宝可梦加入队伍，否则将其存入仓库。
        /// </summary>
        public static void AddMonster(MonsterRuntime monster)
        {
            if (monster == null || Party.Contains(monster) || Storage.Contains(monster))
                return;

            NormalizeParty();
            if (Party.Count < MaxPartySize)
                Party.Add(monster);
            else
                Storage.Add(monster);

            EnsureActivePokemon();
            PartyChanged?.Invoke();
        }

        /// <summary>
        /// 将队伍成员与仓库中的宝可梦交换，同时保留原队伍位置。
        /// </summary>
        public static bool TrySwapWithStorage(int partyIndex, int storageIndex)
        {
            NormalizeParty();
            if (partyIndex < 0 || partyIndex >= Party.Count ||
                storageIndex < 0 || storageIndex >= Storage.Count)
                return false;

            MonsterRuntime partyPokemon = Party[partyIndex];
            MonsterRuntime storagePokemon = Storage[storageIndex];
            if (partyPokemon == null || storagePokemon == null)
                return false;

            bool wasActive = ActivePokemon == partyPokemon;
            Party[partyIndex] = storagePokemon;
            Storage[storageIndex] = partyPokemon;
            if (wasActive)
                ActivePokemon = storagePokemon;
            EnsureActivePokemon();
            PartyChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 将仓库中的宝可梦移入空闲的队伍位置。
        /// </summary>
        public static bool TryMoveToParty(int storageIndex)
        {
            NormalizeParty();
            if (Party.Count >= MaxPartySize || storageIndex < 0 || storageIndex >= Storage.Count)
                return false;

            MonsterRuntime pokemon = Storage[storageIndex];
            if (pokemon == null)
                return false;

            Storage.RemoveAt(storageIndex);
            Party.Add(pokemon);
            EnsureActivePokemon();
            PartyChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 将队伍中的宝可梦移入仓库，同时至少保留一名队伍成员。
        /// </summary>
        public static bool TryMoveToStorage(int partyIndex)
        {
            NormalizeParty();
            if (partyIndex < 0 || partyIndex >= Party.Count || Party.Count <= 1)
                return false;

            MonsterRuntime pokemon = Party[partyIndex];
            if (pokemon == null)
                return false;

            Party.RemoveAt(partyIndex);
            Storage.Add(pokemon);
            EnsureActivePokemon();
            PartyChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 返回当前队伍数据的只读快照。
        /// </summary>
        public static IReadOnlyList<MonsterRuntime> GetPartySnapshot()
        {
            NormalizeParty();
            return new List<MonsterRuntime>(Party);
        }

        /// <summary>
        /// 返回精灵仓库数据的只读快照。
        /// </summary>
        public static IReadOnlyList<MonsterRuntime> GetStorageSnapshot()
        {
            NormalizeParty();
            return new List<MonsterRuntime>(Storage);
        }

        /// <summary>
        /// 按当前手动顺序返回有效道具及其数量。
        /// </summary>
        public static IReadOnlyList<InventoryItemStack> GetInventorySnapshot()
        {
            SyncItemOrder();

            List<InventoryItemStack> items = new List<InventoryItemStack>(ItemOrder.Count);
            for (int i = 0; i < ItemOrder.Count; i++)
            {
                ItemData item = ItemOrder[i];
                items.Add(new InventoryItemStack(item, Inventory[item]));
            }

            return items;
        }

        /// <summary>
        /// 判断背包中是否拥有指定道具。
        /// </summary>
        /// <param name="item">需要检查的道具。</param>
        public static bool HasItem(ItemData item)
        {
            return item != null && Inventory.TryGetValue(item, out int count) && count > 0;
        }

        /// <summary>
        /// 返回指定道具的持有数量。
        /// </summary>
        /// <param name="item">需要查询数量的道具。</param>
        public static int GetItemCount(ItemData item)
        {
            return item != null && Inventory.TryGetValue(item, out int count) ? count : 0;
        }

        /// <summary>
        /// 将指定道具移动到目标道具所在的顺序位置。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        public static bool TryReorderItem(ItemData item, ItemData targetItem)
        {
            SyncItemOrder();
            int sourceIndex = ItemOrder.IndexOf(item);
            int targetIndex = ItemOrder.IndexOf(targetItem);
            if (!TryMoveEntry(ItemOrder, sourceIndex, targetIndex))
                return false;

            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 将指定精灵移动到同一集合中目标精灵所在的顺序位置。
        /// </summary>
        /// <param name="collectionType">需要重排的队伍或仓库集合。</param>
        /// <param name="pokemon">需要移动的精灵。</param>
        /// <param name="targetPokemon">作为目标位置的精灵。</param>
        public static bool TryReorderPokemon(
            PokemonCollectionType collectionType,
            MonsterRuntime pokemon,
            MonsterRuntime targetPokemon)
        {
            NormalizeParty();
            List<MonsterRuntime> collection = GetPokemonCollection(collectionType);
            int sourceIndex = collection.IndexOf(pokemon);
            int targetIndex = collection.IndexOf(targetPokemon);
            if (!TryMoveEntry(collection, sourceIndex, targetIndex))
                return false;

            PartyChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 按指定比较规则调整道具顺序，供后续排序功能调用。
        /// </summary>
        /// <param name="comparison">道具顺序比较规则。</param>
        public static void SortItems(Comparison<ItemData> comparison)
        {
            if (comparison == null)
                return;

            SyncItemOrder();
            ItemOrder.Sort(comparison);
            InventoryChanged?.Invoke();
        }

        /// <summary>
        /// 按指定属性条件查找道具，结果保持当前道具顺序。
        /// </summary>
        /// <param name="predicate">判断道具是否符合条件的方法。</param>
        public static IReadOnlyList<InventoryItemStack> FindItems(Predicate<ItemData> predicate)
        {
            IReadOnlyList<InventoryItemStack> items = GetInventorySnapshot();
            if (predicate == null)
                return items;

            List<InventoryItemStack> result = new List<InventoryItemStack>();
            for (int i = 0; i < items.Count; i++)
            {
                if (predicate(items[i].Item))
                    result.Add(items[i]);
            }

            return result;
        }

        /// <summary>
        /// 按指定比较规则调整队伍或仓库中的精灵顺序，供后续排序功能调用。
        /// </summary>
        /// <param name="collectionType">需要排序的队伍或仓库集合。</param>
        /// <param name="comparison">精灵顺序比较规则。</param>
        public static void SortPokemon(
            PokemonCollectionType collectionType,
            Comparison<MonsterRuntime> comparison)
        {
            if (comparison == null)
                return;

            NormalizeParty();
            GetPokemonCollection(collectionType).Sort(comparison);
            PartyChanged?.Invoke();
        }

        /// <summary>
        /// 按指定属性条件查找队伍或仓库中的精灵，结果保持当前精灵顺序。
        /// </summary>
        /// <param name="collectionType">需要查找的队伍或仓库集合。</param>
        /// <param name="predicate">判断精灵是否符合条件的方法。</param>
        public static IReadOnlyList<MonsterRuntime> FindPokemon(
            PokemonCollectionType collectionType,
            Predicate<MonsterRuntime> predicate)
        {
            NormalizeParty();
            List<MonsterRuntime> collection = GetPokemonCollection(collectionType);
            return predicate == null
                ? new List<MonsterRuntime>(collection)
                : collection.FindAll(predicate);
        }

        public static void AddItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0)
                return;

            if (Inventory.ContainsKey(item))
            {
                Inventory[item] += count;
            }
            else
            {
                Inventory[item] = count;
                ItemOrder.Add(item);
            }

            InventoryChanged?.Invoke();
        }

        // 只有对应操作成功后才移除道具。
        public static void RemoveItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0 || !Inventory.ContainsKey(item))
                return;

            Inventory[item] -= count;
            if (Inventory[item] <= 0)
            {
                Inventory.Remove(item);
                ItemOrder.Remove(item);
            }

            InventoryChanged?.Invoke();
        }

        // 返回所有可供界面显示的可使用道具定义。
        public static List<ItemData> GetUsableItems() =>
            GetInventorySnapshot()
                .Where(entry => entry.Item is IUsable)
                .Select(entry => entry.Item)
                .ToList();

        /// <summary>
        /// 按当前手动顺序返回可使用道具及其数量。
        /// </summary>
        public static IReadOnlyList<InventoryItemStack> GetUsableItemSnapshot()
        {
            return GetInventorySnapshot()
                .Where(entry => entry.Item is IUsable)
                .ToList();
        }

        // 返回所有可供界面显示的持有道具定义。
        public static List<ItemData> GetHeldItems() =>
            GetInventorySnapshot()
                .Where(entry => entry.Item is IHeldTrigger)
                .Select(entry => entry.Item)
                .ToList();

        /// <summary>
        /// 返回需要重排或查询的精灵集合。
        /// </summary>
        /// <param name="collectionType">队伍或仓库集合类型。</param>
        private static List<MonsterRuntime> GetPokemonCollection(PokemonCollectionType collectionType)
        {
            return collectionType == PokemonCollectionType.Party ? Party : Storage;
        }

        /// <summary>
        /// 同步公开背包字典与道具顺序列表，兼容现有代码直接写入背包的情况。
        /// </summary>
        private static void SyncItemOrder()
        {
            ItemOrder.RemoveAll(item =>
                item == null || !Inventory.TryGetValue(item, out int count) || count <= 0);

            foreach (KeyValuePair<ItemData, int> entry in Inventory)
            {
                if (entry.Key != null && entry.Value > 0 && !ItemOrder.Contains(entry.Key))
                    ItemOrder.Add(entry.Key);
            }
        }

        /// <summary>
        /// 将列表中的一项移动到目标索引，并保持其他项的相对顺序。
        /// </summary>
        /// <typeparam name="T">列表元素类型。</typeparam>
        /// <param name="items">需要重排的列表。</param>
        /// <param name="sourceIndex">来源索引。</param>
        /// <param name="targetIndex">目标索引。</param>
        private static bool TryMoveEntry<T>(List<T> items, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= items.Count ||
                targetIndex < 0 || targetIndex >= items.Count ||
                sourceIndex == targetIndex)
                return false;

            T entry = items[sourceIndex];
            items.RemoveAt(sourceIndex);
            items.Insert(targetIndex, entry);
            return true;
        }
    }
}
