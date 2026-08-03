using System;
using System.Collections.Generic;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class BagPanel : MonoBehaviour
    {
        [SerializeField] private Transform itemContent;
        [SerializeField] private ItemSlotView itemSlotPrefab;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text itemCountText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button useButton;
        [SerializeField] private Button cancelButton;

        private readonly List<ItemSlotView> _slots = new List<ItemSlotView>();
        private ItemData _selectedItem;

        public event Action<ItemData> OnUseConfirmed;
        public event Action OnCancelled;

        private void Awake()
        {
            if (useButton != null)
                useButton.onClick.AddListener(ConfirmUse);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());

            ClearDetails();
        }

        public void Refresh(
            IReadOnlyList<ItemData> items,
            IReadOnlyDictionary<ItemData, int> counts)
        {
            if (itemContent == null || itemSlotPrefab == null)
                return;

            for (int i = 0; i < _slots.Count; i++)
                Destroy(_slots[i].gameObject);
            _slots.Clear();

            if (items != null)
            {
                foreach (ItemData item in items)
                {
                    if (item == null) continue;
                    int count = counts != null && counts.TryGetValue(item, out int value)
                        ? value
                        : 0;

                    ItemSlotView slot = Instantiate(itemSlotPrefab, itemContent);
                    slot.Bind(item, count, PreviewItem);
                    _slots.Add(slot);
                }
            }

            _selectedItem = null;
            ClearDetails();
        }

        private void PreviewItem(ItemData item)
        {
            _selectedItem = item;

            if (itemIcon != null)
                itemIcon.sprite = item.Icon;
            if (itemNameText != null)
                itemNameText.text = item.DisplayName;
            if (itemDescriptionText != null)
                itemDescriptionText.text = item.Description;
            if (itemCountText != null)
                itemCountText.text = string.Empty;
            if (useButton != null)
                useButton.interactable = item is IUsable;

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(_slots[i].Item == item);
        }

        private void ConfirmUse()
        {
            if (_selectedItem != null)
                OnUseConfirmed?.Invoke(_selectedItem);
        }

        private void ClearDetails()
        {
            if (itemIcon != null)
                itemIcon.sprite = null;
            if (itemNameText != null)
                itemNameText.text = "未选择道具";
            if (itemDescriptionText != null)
                itemDescriptionText.text = string.Empty;
            if (itemCountText != null)
                itemCountText.text = string.Empty;
            if (useButton != null)
                useButton.interactable = false;
        }
    }
}
