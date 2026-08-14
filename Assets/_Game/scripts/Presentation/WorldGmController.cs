using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class WorldGmController : MonoBehaviour
    {
        [Header("测试资源")]
        [SerializeField] private ItemData pokeballItem;
        [Min(1)]
        [SerializeField] private int pokeballAddAmount = 10;

        [Header("界面")]
        [SerializeField] private Button addPokeballsButton;
        [SerializeField] private TMP_Text heldCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;

        public event Action CloseRequested;

        /// <summary>
        /// 显示 GM 测试面板。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏 GM 测试面板。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 面板启用时刷新测试数据并绑定按钮事件。
        /// </summary>
        private void OnEnable()
        {
            addPokeballsButton.onClick.AddListener(AddPokeballs);
            closeButton.onClick.AddListener(RequestClose);
            statusText.text = "请选择需要执行的测试操作。";
            RefreshHeldCount();
        }

        /// <summary>
        /// 面板禁用时解除按钮事件绑定。
        /// </summary>
        private void OnDisable()
        {
            addPokeballsButton.onClick.RemoveListener(AddPokeballs);
            closeButton.onClick.RemoveListener(RequestClose);
        }

        /// <summary>
        /// 向玩家背包增加 Inspector 设置数量的精灵球。
        /// </summary>
        public void AddPokeballs()
        {
            PlayerParty.AddItem(pokeballItem, pokeballAddAmount);
            RefreshHeldCount();
            statusText.text = $"已增加 {pokeballAddAmount} 个{pokeballItem.DisplayName}。";
        }

        /// <summary>
        /// 刷新 GM 面板显示的精灵球当前持有数量。
        /// </summary>
        private void RefreshHeldCount()
        {
            heldCountText.text =
                $"当前持有：{PlayerParty.GetItemCount(pokeballItem)} 个{pokeballItem.DisplayName}";
        }

        /// <summary>
        /// 通知场外菜单关闭 GM 测试面板。
        /// </summary>
        private void RequestClose()
        {
            CloseRequested?.Invoke();
        }
    }
}
