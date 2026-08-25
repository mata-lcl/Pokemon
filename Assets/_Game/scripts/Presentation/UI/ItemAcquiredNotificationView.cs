using System.Collections;
using System.Collections.Generic;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class ItemAcquiredNotificationView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text messageText;
        [Min(0.1f)]
        [SerializeField] private float displayDuration = 2f;

        private readonly Queue<InventoryItemStack> _pendingItems =
            new Queue<InventoryItemStack>();
        private Coroutine _displayRoutine;

        /// <summary>
        /// 组件启用时监听玩家获得道具事件并隐藏提示。
        /// </summary>
        private void OnEnable()
        {
            PlayerParty.ItemAdded += HandleItemAdded;
            popupRoot.SetActive(false);
        }

        /// <summary>
        /// 组件禁用时解除事件监听并清空尚未显示的道具提示。
        /// </summary>
        private void OnDisable()
        {
            PlayerParty.ItemAdded -= HandleItemAdded;
            if (_displayRoutine != null)
                StopCoroutine(_displayRoutine);

            _displayRoutine = null;
            _pendingItems.Clear();
            popupRoot.SetActive(false);
        }

        /// <summary>
        /// 将新获得的道具加入提示队列并开始依次显示。
        /// </summary>
        /// <param name="item">玩家本次获得的道具。</param>
        /// <param name="count">玩家本次获得的数量。</param>
        private void HandleItemAdded(ItemData item, int count)
        {
            _pendingItems.Enqueue(new InventoryItemStack(item, count));
            if (_displayRoutine == null)
                _displayRoutine = StartCoroutine(DisplayPendingItems());
        }

        /// <summary>
        /// 依次显示队列中的道具名称、图标和获得数量。
        /// </summary>
        private IEnumerator DisplayPendingItems()
        {
            while (_pendingItems.Count > 0)
            {
                InventoryItemStack entry = _pendingItems.Dequeue();
                itemIcon.sprite = entry.Item.Icon;
                itemIcon.enabled = entry.Item.Icon != null;
                messageText.text = $"获得 {entry.Item.DisplayName} ×{entry.Count}";
                popupRoot.SetActive(true);
                yield return new WaitForSecondsRealtime(displayDuration);
                popupRoot.SetActive(false);
            }

            _displayRoutine = null;
        }
    }
}
