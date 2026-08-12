using Pokemon.Domain;
using Pokemon.Presentation.UI;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class WorldPokemonStorageController : MonoBehaviour
    {
        [SerializeField] private PokemonStoragePanel panel;

        private bool _initialized;

        private void OnEnable()
        {
            Initialize();
            PlayerParty.PartyChanged += Refresh;
        }

        private void OnDisable()
        {
            PlayerParty.PartyChanged -= Refresh;
        }

        public void Show()
        {
            Initialize();
            PlayerParty.NormalizeParty();

            if (panel != null)
                panel.Show(
                    PlayerParty.GetPartySnapshot(),
                    PlayerParty.GetStorageSnapshot());
        }

        private void Initialize()
        {
            if (_initialized || panel == null)
                return;

            panel.SwapRequested += Swap;
            panel.MoveToPartyRequested += MoveToParty;
            panel.MoveToStorageRequested += MoveToStorage;
            panel.PokemonReorderRequested += ReorderPokemon;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (!_initialized || panel == null)
                return;

            panel.SwapRequested -= Swap;
            panel.MoveToPartyRequested -= MoveToParty;
            panel.MoveToStorageRequested -= MoveToStorage;
            panel.PokemonReorderRequested -= ReorderPokemon;
        }

        private void Swap(int partyIndex, int storageIndex)
        {
            PlayerParty.TrySwapWithStorage(partyIndex, storageIndex);
        }

        private void MoveToParty(int storageIndex)
        {
            PlayerParty.TryMoveToParty(storageIndex);
        }

        private void MoveToStorage(int partyIndex)
        {
            PlayerParty.TryMoveToStorage(partyIndex);
        }

        /// <summary>
        /// 调用数据层方法调整指定精灵集合中的顺序。
        /// </summary>
        /// <param name="collectionType">需要重排的队伍或仓库集合。</param>
        /// <param name="pokemon">需要移动的精灵。</param>
        /// <param name="targetPokemon">作为目标位置的精灵。</param>
        private void ReorderPokemon(
            PokemonCollectionType collectionType,
            MonsterRuntime pokemon,
            MonsterRuntime targetPokemon)
        {
            PlayerParty.TryReorderPokemon(collectionType, pokemon, targetPokemon);
        }

        private void Refresh()
        {
            if (panel != null)
                panel.Refresh(
                    PlayerParty.GetPartySnapshot(),
                    PlayerParty.GetStorageSnapshot());
        }
    }
}
