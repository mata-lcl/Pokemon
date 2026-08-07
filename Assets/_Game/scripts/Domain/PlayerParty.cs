using System;
using System.Collections.Generic;
using System.Linq;

namespace Pokemon.Domain
{
    public static class PlayerParty
    {
        public const int MaxPartySize = 6;

        public static MonsterRuntime ActivePokemon { get; set; }

        // Keep Party public for compatibility while all writes migrate to the methods below.
        public static List<MonsterRuntime> Party = new List<MonsterRuntime>();
        public static List<MonsterRuntime> Storage = new List<MonsterRuntime>();
        public static Dictionary<ItemData, int> Inventory = new Dictionary<ItemData, int>();

        public static event Action PartyChanged;

        /// <summary>
        /// Removes invalid party entries, moves overflow into storage, and restores
        /// the active Pokemon reference when it is missing or no longer in the party.
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
        /// Uses the first party member as the default active Pokemon.
        /// Existing valid active selections are preserved.
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
        /// Returns party members that can replace the current active Pokemon.
        /// </summary>
        public static List<MonsterRuntime> GetSwitchablePokemon()
        {
            NormalizeParty();
            return Party
                .Where(monster => monster != null && monster != ActivePokemon && !monster.IsFainted)
                .ToList();
        }

        /// <summary>
        /// Changes the active Pokemon when the requested member belongs to this party.
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
        /// Adds a Pokemon to the party when there is room; otherwise stores it.
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
        /// Exchanges a party member with a stored Pokemon while preserving the party slot.
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
        /// Moves a stored Pokemon into a free party slot.
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
        /// Moves a party Pokemon to storage, keeping at least one party member.
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

        public static void AddItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0)
                return;

            if (Inventory.ContainsKey(item)) Inventory[item] += count;
            else Inventory[item] = count;
        }

        // Remove items only after the corresponding action succeeds.
        public static void RemoveItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0 || !Inventory.ContainsKey(item))
                return;

            Inventory[item] -= count;
            if (Inventory[item] <= 0) Inventory.Remove(item);
        }

        // Returns all usable item definitions for UI display.
        public static List<ItemData> GetUsableItems() =>
            Inventory.Keys.Where(i => i is IUsable).ToList();

        // Returns all held-item definitions for UI display.
        public static List<ItemData> GetHeldItems() =>
            Inventory.Keys.Where(i => i is IHeldTrigger).ToList();
    }
}
