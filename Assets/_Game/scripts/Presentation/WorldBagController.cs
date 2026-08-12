using Pokemon.Domain;
using Pokemon.Presentation.UI;
using UnityEngine;

namespace Pokemon.Presentation
{
    public class WorldBagController : MonoBehaviour
    {
        [SerializeField] private BagPanel panel;
        [SerializeField] private ItemData starterItem;
        [SerializeField] private int starterItemCount = 5;

        private static bool _starterItemsInitialized;

        /// <summary>
        /// 订阅背包数据和拖拽重排事件。
        /// </summary>
        private void OnEnable()
        {
            if (panel != null)
                panel.ItemReorderRequested += ReorderItem;
            PlayerParty.InventoryChanged += Refresh;
        }

        /// <summary>
        /// 取消订阅背包数据和拖拽重排事件。
        /// </summary>
        private void OnDisable()
        {
            if (panel != null)
                panel.ItemReorderRequested -= ReorderItem;
            PlayerParty.InventoryChanged -= Refresh;
        }

        public void Show()
        {
            EnsureStarterItems();
            Refresh();
        }

        private void EnsureStarterItems()
        {
            if (_starterItemsInitialized)
                return;

            _starterItemsInitialized = true;
            if (starterItem != null &&
                starterItemCount > 0 &&
                !PlayerParty.HasItem(starterItem))
            {
                PlayerParty.AddItem(starterItem, starterItemCount);
            }
        }

        private void Refresh()
        {
            if (panel == null)
            {
                Debug.LogWarning("WorldBagController 无法刷新背包：BagPanel 引用未绑定。", this);
                return;
            }

            panel.Refresh(PlayerParty.GetInventorySnapshot());
        }

        /// <summary>
        /// 调用数据层方法调整道具顺序。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        private void ReorderItem(ItemData item, ItemData targetItem)
        {
            PlayerParty.TryReorderItem(item, targetItem);
        }
    }
}
