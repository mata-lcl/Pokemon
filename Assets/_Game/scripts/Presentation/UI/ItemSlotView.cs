using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class ItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private Button button;

        private ItemData _item;
        private Action<ItemData> _onClicked;

        public ItemData Item => _item;

        public void Bind(ItemData item, int count, Action<ItemData> onClicked)
        {
            _item = item;
            _onClicked = onClicked;

            if (icon != null)
                icon.sprite = item != null ? item.Icon : null;
            if (nameText != null)
                nameText.text = item != null ? item.DisplayName : string.Empty;
            if (countText != null)
                countText.text = item != null ? $"x{count}" : string.Empty;
            if (selectedFrame != null)
                selectedFrame.SetActive(false);

            if (button != null)
            {
                button.interactable = item != null && count > 0;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClicked?.Invoke(_item));
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }
    }
}
