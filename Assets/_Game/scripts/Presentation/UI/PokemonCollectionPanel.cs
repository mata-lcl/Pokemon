using System;
using System.Collections.Generic;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonCollectionPanel : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform slotContent;
        [SerializeField] private PokemonSlotView slotPrefab;
        [SerializeField] private int slotCount = 6;

        [Header("Detail")]
        [SerializeField] private PokemonDetailView detailView;

        [Header("Actions")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmButtonText;

        private readonly List<PokemonSlotView> _slots = new List<PokemonSlotView>();
        private List<MonsterRuntime> _pokemon = new List<MonsterRuntime>();
        private MonsterRuntime _activePokemon;
        private MonsterRuntime _selectedPokemon;
        private bool _initialized;

        public event Action<MonsterRuntime> OnConfirmed;
        public event Action OnCancelled;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSelection);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSelection);

            CreateSlots();
            _initialized = true;
        }

        private void CreateSlots()
        {
            if (slotContent == null)
                return;

            _slots.AddRange(slotContent.GetComponentsInChildren<PokemonSlotView>(true));

            if (slotPrefab == null)
                return;

            while (_slots.Count < Mathf.Max(0, slotCount))
            {
                PokemonSlotView slot = Instantiate(slotPrefab, slotContent);
                _slots.Add(slot);
            }
        }

        public void Show(IReadOnlyList<MonsterRuntime> pokemon, MonsterRuntime activePokemon)
        {
            Initialize();

            _pokemon = pokemon == null
                ? new List<MonsterRuntime>()
                : new List<MonsterRuntime>(pokemon);
            _activePokemon = activePokemon;
            _selectedPokemon = null;

            if (detailView != null)
                detailView.Clear();

            for (int i = 0; i < _slots.Count; i++)
            {
                PokemonSlotView slot = _slots[i];
                slot.gameObject.SetActive(true);

                if (i < _pokemon.Count)
                {
                    MonsterRuntime pokemonEntry = _pokemon[i];
                    slot.Bind(
                        pokemonEntry,
                        pokemonEntry == _activePokemon,
                        pokemonEntry.IsFainted,
                        PreviewPokemon);
                }
                else
                {
                    slot.Clear();
                }
            }

            SetConfirmInteractable(false);
            if (cancelButton != null)
                cancelButton.interactable = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetConfirmButtonText(string text)
        {
            if (confirmButtonText != null)
                confirmButtonText.text = text;
        }

        public void SetInteractable(bool interactable)
        {
            if (cancelButton != null)
                cancelButton.interactable = interactable;

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetInteractable(interactable);

            bool canConfirm = interactable
                && _selectedPokemon != null
                && _selectedPokemon != _activePokemon
                && !_selectedPokemon.IsFainted;
            SetConfirmInteractable(canConfirm);
        }

        private void PreviewPokemon(MonsterRuntime pokemon)
        {
            if (pokemon == null)
                return;

            _selectedPokemon = pokemon;
            if (detailView != null)
                detailView.Show(pokemon);

            SetConfirmInteractable(
                pokemon != _activePokemon && !pokemon.IsFainted);
        }

        private void ConfirmSelection()
        {
            if (_selectedPokemon == null || _selectedPokemon == _activePokemon)
                return;
            if (_selectedPokemon.IsFainted)
                return;

            OnConfirmed?.Invoke(_selectedPokemon);
        }

        private void CancelSelection()
        {
            OnCancelled?.Invoke();
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (confirmButton != null)
                confirmButton.interactable = interactable;
        }
    }
}
