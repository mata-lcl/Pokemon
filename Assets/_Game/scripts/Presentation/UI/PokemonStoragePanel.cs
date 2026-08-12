using System;
using System.Collections.Generic;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonStoragePanel : MonoBehaviour
    {
        [Header("页面")]
        [SerializeField] private GameObject overviewPage;
        [SerializeField] private GameObject detailPage;
        [SerializeField] private GameObject storagePage;

        [Header("精灵槽位")]
        [SerializeField] private PokemonSlotView slotPrefab;
        [SerializeField] private Transform overviewPartyContent;
        [SerializeField] private Transform storagePartyContent;
        [SerializeField] private Transform storageContent;
        [SerializeField] private int partySlotCount = 6;

        [Header("队伍预览")]
        [SerializeField] private Image previewAvatar;
        [SerializeField] private TMP_Text previewNameText;
        [SerializeField] private TMP_Text previewLevelText;
        [SerializeField] private TMP_Text previewHintText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button detailButton;
        [SerializeField] private Button openStorageButton;

        [Header("详情页")]
        [SerializeField] private PokemonDetailView detailView;
        [SerializeField] private Button detailBackButton;

        [Header("仓库操作")]
        [SerializeField] private TMP_Text selectionHintText;
        [SerializeField] private Button swapButton;
        [SerializeField] private Button moveToPartyButton;
        [SerializeField] private Button moveToStorageButton;
        [SerializeField] private Button storageBackButton;

        private readonly List<PokemonSlotView> _overviewPartySlots = new List<PokemonSlotView>();
        private readonly List<PokemonSlotView> _storagePartySlots = new List<PokemonSlotView>();
        private readonly List<PokemonSlotView> _storageSlots = new List<PokemonSlotView>();
        private List<MonsterRuntime> _party = new List<MonsterRuntime>();
        private List<MonsterRuntime> _storage = new List<MonsterRuntime>();
        private int _overviewSelection = -1;
        private int _partySelection = -1;
        private int _storageSelection = -1;
        private bool _initialized;

        public event Action<int, int> SwapRequested;
        public event Action<int> MoveToPartyRequested;
        public event Action<int> MoveToStorageRequested;
        public event Action<PokemonCollectionType, MonsterRuntime, MonsterRuntime> PokemonReorderRequested;

        private void Awake()
        {
            Initialize();
        }

        public void Show(
            IReadOnlyList<MonsterRuntime> party,
            IReadOnlyList<MonsterRuntime> storage)
        {
            Initialize();
            CopyData(party, storage);

            _overviewSelection = _party.Count > 0 ? 0 : -1;
            _partySelection = -1;
            _storageSelection = -1;

            SetPage(overviewPage);
            RefreshAll();
            gameObject.SetActive(true);
        }

        public void Refresh(
            IReadOnlyList<MonsterRuntime> party,
            IReadOnlyList<MonsterRuntime> storage)
        {
            Initialize();
            CopyData(party, storage);

            _overviewSelection = ClampSelection(_overviewSelection, _party.Count);
            _partySelection = -1;
            _storageSelection = -1;
            RefreshAll();
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            if (detailButton != null)
                detailButton.onClick.AddListener(OpenDetailPage);
            if (openStorageButton != null)
                openStorageButton.onClick.AddListener(OpenStoragePage);
            if (detailBackButton != null)
                detailBackButton.onClick.AddListener(ReturnToOverview);
            if (storageBackButton != null)
                storageBackButton.onClick.AddListener(ReturnToOverview);
            if (swapButton != null)
                swapButton.onClick.AddListener(RequestSwap);
            if (moveToPartyButton != null)
                moveToPartyButton.onClick.AddListener(RequestMoveToParty);
            if (moveToStorageButton != null)
                moveToStorageButton.onClick.AddListener(RequestMoveToStorage);

            CreatePartySlots(overviewPartyContent, _overviewPartySlots);
            CreatePartySlots(storagePartyContent, _storagePartySlots);
            _initialized = true;
        }

        private void CopyData(
            IReadOnlyList<MonsterRuntime> party,
            IReadOnlyList<MonsterRuntime> storage)
        {
            _party = party == null
                ? new List<MonsterRuntime>()
                : new List<MonsterRuntime>(party);
            _storage = storage == null
                ? new List<MonsterRuntime>()
                : new List<MonsterRuntime>(storage);
        }

        private void CreatePartySlots(
            Transform content,
            List<PokemonSlotView> destination)
        {
            if (content == null)
                return;

            destination.AddRange(content.GetComponentsInChildren<PokemonSlotView>(true));
            if (slotPrefab == null)
                return;

            while (destination.Count < Mathf.Max(0, partySlotCount))
                destination.Add(Instantiate(slotPrefab, content));
        }

        private void EnsureStorageSlots()
        {
            if (storageContent == null || slotPrefab == null)
                return;

            while (_storageSlots.Count < _storage.Count)
                _storageSlots.Add(Instantiate(slotPrefab, storageContent));
        }

        private void RefreshAll()
        {
            RefreshOverviewSlots();
            RefreshPreview();
            RefreshStorageSlots();
            RefreshCapacity();
            RefreshActionState();
        }

        private void RefreshOverviewSlots()
        {
            BindPartySlots(
                _overviewPartySlots,
                index =>
                {
                    _overviewSelection = index;
                    RefreshOverviewSlots();
                    RefreshPreview();
                },
                _overviewSelection);
        }

        private void BindPartySlots(
            List<PokemonSlotView> slots,
            Action<int> onSelected,
            int selectedIndex)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                PokemonSlotView slot = slots[i];
                slot.gameObject.SetActive(true);

                if (i >= _party.Count || _party[i] == null)
                {
                    slot.Clear();
                    continue;
                }

                int slotIndex = i;
                MonsterRuntime pokemon = _party[i];
                slot.Bind(
                    pokemon,
                    false,
                    pokemon.IsFainted,
                    _ => onSelected(slotIndex),
                    true,
                    (draggedPokemon, targetPokemon) =>
                        RequestPokemonReorder(
                            PokemonCollectionType.Party,
                            draggedPokemon,
                            targetPokemon));
                slot.SetSelected(i == selectedIndex);
            }
        }

        private void RefreshPreview()
        {
            MonsterRuntime selected = GetAt(_party, _overviewSelection);

            if (previewAvatar != null)
            {
                previewAvatar.sprite = selected != null && selected.Species != null
                    ? selected.Species.BattleSprite
                    : null;
                previewAvatar.enabled = previewAvatar.sprite != null;
            }

            if (previewNameText != null)
                previewNameText.text = selected?.Species != null
                    ? selected.Species.DisplayName
                    : "未选择精灵";
            if (previewLevelText != null)
                previewLevelText.text = selected != null ? $"Lv.{selected.Level}" : string.Empty;
            if (previewHintText != null)
                previewHintText.text = selected != null
                    ? "可以查看详细能力，或打开仓库调整队伍。"
                    : "当前队伍中没有精灵。";
            if (detailButton != null)
                detailButton.interactable = selected != null;
        }

        private void RefreshStorageSlots()
        {
            BindPartySlots(
                _storagePartySlots,
                index =>
                {
                    _partySelection = index;
                    RefreshStorageSlots();
                    RefreshActionState();
                },
                _partySelection);

            EnsureStorageSlots();
            for (int i = 0; i < _storageSlots.Count; i++)
            {
                PokemonSlotView slot = _storageSlots[i];
                if (i >= _storage.Count || _storage[i] == null)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.gameObject.SetActive(true);
                int slotIndex = i;
                MonsterRuntime pokemon = _storage[i];
                slot.Bind(
                    pokemon,
                    false,
                    pokemon.IsFainted,
                    _ =>
                    {
                        _storageSelection = slotIndex;
                        RefreshStorageSlots();
                        RefreshActionState();
                    },
                    true,
                    (draggedPokemon, targetPokemon) =>
                        RequestPokemonReorder(
                            PokemonCollectionType.Storage,
                            draggedPokemon,
                            targetPokemon));
                slot.SetSelected(i == _storageSelection);
            }
        }

        /// <summary>
        /// 将槽位发出的精灵重排请求转交给外层控制器。
        /// </summary>
        /// <param name="collectionType">需要重排的队伍或仓库集合。</param>
        /// <param name="pokemon">需要移动的精灵。</param>
        /// <param name="targetPokemon">作为目标位置的精灵。</param>
        private void RequestPokemonReorder(
            PokemonCollectionType collectionType,
            MonsterRuntime pokemon,
            MonsterRuntime targetPokemon)
        {
            PokemonReorderRequested?.Invoke(collectionType, pokemon, targetPokemon);
        }

        private void RefreshCapacity()
        {
            if (capacityText != null)
                capacityText.text = $"队伍 {_party.Count}/{partySlotCount}    仓库 {_storage.Count}";
        }

        private void RefreshActionState()
        {
            bool hasPartySelection = GetAt(_party, _partySelection) != null;
            bool hasStorageSelection = GetAt(_storage, _storageSelection) != null;

            if (swapButton != null)
                swapButton.interactable = hasPartySelection && hasStorageSelection;
            if (moveToPartyButton != null)
                moveToPartyButton.interactable = hasStorageSelection && _party.Count < partySlotCount;
            if (moveToStorageButton != null)
                moveToStorageButton.interactable = hasPartySelection && _party.Count > 1;

            if (selectionHintText == null)
                return;

            string partyName = GetPokemonName(GetAt(_party, _partySelection));
            string storageName = GetPokemonName(GetAt(_storage, _storageSelection));
            selectionHintText.text =
                $"队伍选择：{partyName}\n仓库选择：{storageName}";
        }

        private void OpenDetailPage()
        {
            MonsterRuntime selected = GetAt(_party, _overviewSelection);
            if (selected == null)
                return;

            if (detailView != null)
                detailView.Show(selected);
            SetPage(detailPage);
        }

        private void OpenStoragePage()
        {
            _partySelection = -1;
            _storageSelection = -1;
            RefreshStorageSlots();
            RefreshActionState();
            SetPage(storagePage);
        }

        private void ReturnToOverview()
        {
            RefreshOverviewSlots();
            RefreshPreview();
            SetPage(overviewPage);
        }

        private void RequestSwap()
        {
            if (GetAt(_party, _partySelection) == null ||
                GetAt(_storage, _storageSelection) == null)
                return;

            SwapRequested?.Invoke(_partySelection, _storageSelection);
        }

        private void RequestMoveToParty()
        {
            if (GetAt(_storage, _storageSelection) != null)
                MoveToPartyRequested?.Invoke(_storageSelection);
        }

        private void RequestMoveToStorage()
        {
            if (GetAt(_party, _partySelection) != null)
                MoveToStorageRequested?.Invoke(_partySelection);
        }

        private void SetPage(GameObject visiblePage)
        {
            if (overviewPage != null)
                overviewPage.SetActive(visiblePage == overviewPage);
            if (detailPage != null)
                detailPage.SetActive(visiblePage == detailPage);
            if (storagePage != null)
                storagePage.SetActive(visiblePage == storagePage);
        }

        private static int ClampSelection(int selection, int count)
        {
            if (count <= 0)
                return -1;
            return Mathf.Clamp(selection, 0, count - 1);
        }

        private static MonsterRuntime GetAt(List<MonsterRuntime> pokemon, int index)
        {
            return index >= 0 && index < pokemon.Count ? pokemon[index] : null;
        }

        private static string GetPokemonName(MonsterRuntime pokemon)
        {
            return pokemon?.Species != null ? pokemon.Species.DisplayName : "未选择";
        }
    }
}
