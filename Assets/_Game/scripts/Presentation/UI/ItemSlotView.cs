using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class ItemSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private const float LongPressDuration = 1f;

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private Button button;

        private ItemData _item;
        private Action<ItemData> _onClicked;
        private Action<ItemData, ItemData> _onReorderRequested;
        private float _pointerDownTime;
        private bool _pointerHeld;
        private bool _isDragging;
        private bool _suppressClick;
        private bool _selected;

        public ItemData Item => _item;

        public void Bind(
            ItemData item,
            int count,
            Action<ItemData> onClicked,
            Action<ItemData, ItemData> onReorderRequested = null)
        {
            _item = item;
            _onClicked = onClicked;
            _onReorderRequested = onReorderRequested;
            _selected = false;
            ResetPointerState();

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
                button.onClick.AddListener(() =>
                {
                    if (!_suppressClick)
                        _onClicked?.Invoke(_item);
                });
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selectedFrame != null)
                selectedFrame.SetActive(selected || _isDragging);
        }

        /// <summary>
        /// 记录指针按下时间，开始等待长按拖拽。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            _suppressClick = false;
            if (_item == null || _onReorderRequested == null)
                return;

            _pointerHeld = true;
            _pointerDownTime = Time.unscaledTime;
        }

        /// <summary>
        /// 在指针松开时结束拖拽，并向目标槽位发起重排请求。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            _pointerHeld = false;
            if (!_isDragging)
                return;

            ItemSlotView targetSlot = GetDropTarget(eventData);
            EndDragVisual();
            if (targetSlot != null && targetSlot != this && targetSlot.Item != null)
                _onReorderRequested?.Invoke(_item, targetSlot.Item);
        }

        /// <summary>
        /// 在拖动过程中检查是否已经满足一秒长按条件。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
            TryBeginDrag();
        }

        /// <summary>
        /// 持续检查静止长按，使按住一秒后再移动也能进入拖拽状态。
        /// </summary>
        private void Update()
        {
            TryBeginDrag();
        }

        /// <summary>
        /// 组件禁用时清理未结束的长按拖拽状态。
        /// </summary>
        private void OnDisable()
        {
            ResetPointerState();
        }

        /// <summary>
        /// 达到长按时间后进入拖拽状态并显示槽位高亮。
        /// </summary>
        private void TryBeginDrag()
        {
            if (!_pointerHeld || _isDragging ||
                Time.unscaledTime - _pointerDownTime < LongPressDuration)
                return;

            _isDragging = true;
            _suppressClick = true;
            if (selectedFrame != null)
                selectedFrame.SetActive(true);
        }

        /// <summary>
        /// 获取指针松开位置对应的道具槽位。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        private static ItemSlotView GetDropTarget(PointerEventData eventData)
        {
            GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
            return targetObject != null
                ? targetObject.GetComponentInParent<ItemSlotView>()
                : null;
        }

        /// <summary>
        /// 结束当前拖拽并恢复槽位原有选择状态。
        /// </summary>
        private void EndDragVisual()
        {
            _isDragging = false;
            if (selectedFrame != null)
                selectedFrame.SetActive(_selected);
        }

        /// <summary>
        /// 重置槽位的指针和拖拽状态。
        /// </summary>
        private void ResetPointerState()
        {
            _pointerHeld = false;
            _isDragging = false;
            _suppressClick = false;
        }
    }
}
