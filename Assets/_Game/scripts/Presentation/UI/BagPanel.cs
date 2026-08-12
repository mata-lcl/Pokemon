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
        private IReadOnlyList<InventoryItemStack> _items;
        private ItemData _selectedItem;

        public event Action<ItemData> OnUseConfirmed;
        public event Action OnCancelled;
        public event Action<ItemData, ItemData> ItemReorderRequested;

        private void Awake()
        {
            if (useButton != null)
                useButton.onClick.AddListener(ConfirmUse);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());

            ClearDetails();
        }

        public void Refresh(IReadOnlyList<InventoryItemStack> items)
        {
            _items = items;

            if (itemContent == null || itemSlotPrefab == null)
                return;

            for (int i = 0; i < _slots.Count; i++)
                Destroy(_slots[i].gameObject);
            _slots.Clear();

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ItemData item = items[i].Item;
                    if (item == null) continue;

                    ItemSlotView slot = Instantiate(itemSlotPrefab, itemContent);
                    slot.Bind(item, items[i].Count, PreviewItem, RequestItemReorder);
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
                itemCountText.text = $"x{GetItemCount(item)}";
            if (useButton != null)
                useButton.interactable = item is IUsable;

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(_slots[i].Item == item);
        }

        /// <summary>
        /// 将槽位发出的道具重排请求转交给外层控制器。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        private void RequestItemReorder(ItemData item, ItemData targetItem)
        {
            ItemReorderRequested?.Invoke(item, targetItem);
        }

        /// <summary>
        /// 从当前只读快照中取得指定道具数量。
        /// </summary>
        /// <param name="item">需要查询数量的道具。</param>
        private int GetItemCount(ItemData item)
        {
            if (_items == null)
                return 0;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Item == item)
                    return _items[i].Count;
            }

            return 0;
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
