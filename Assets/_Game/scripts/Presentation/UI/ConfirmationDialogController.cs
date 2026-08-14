using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class ConfirmationDialogController : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text primaryButtonText;
        [SerializeField] private TMP_Text secondaryButtonText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;

        private Action _primaryAction;
        private Action _secondaryAction;

        /// <summary>
        /// 按指定提示、按钮文字和回调显示确认弹窗。
        /// </summary>
        /// <param name="message">弹窗提示文字。</param>
        /// <param name="primaryLabel">主要按钮文字。</param>
        /// <param name="primaryAction">点击主要按钮后执行的操作。</param>
        /// <param name="secondaryLabel">次要按钮文字。</param>
        /// <param name="secondaryAction">点击次要按钮后执行的操作。</param>
        public void Show(
            string message,
            string primaryLabel,
            Action primaryAction,
            string secondaryLabel,
            Action secondaryAction)
        {
            messageText.text = message;
            primaryButtonText.text = primaryLabel;
            secondaryButtonText.text = secondaryLabel;
            _primaryAction = primaryAction;
            _secondaryAction = secondaryAction;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏确认弹窗并清除当前按钮回调。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _primaryAction = null;
            _secondaryAction = null;
        }

        /// <summary>
        /// 弹窗启用时绑定两个按钮事件。
        /// </summary>
        private void OnEnable()
        {
            primaryButton.onClick.AddListener(ConfirmPrimaryAction);
            secondaryButton.onClick.AddListener(ConfirmSecondaryAction);
        }

        /// <summary>
        /// 弹窗禁用时解除两个按钮事件。
        /// </summary>
        private void OnDisable()
        {
            primaryButton.onClick.RemoveListener(ConfirmPrimaryAction);
            secondaryButton.onClick.RemoveListener(ConfirmSecondaryAction);
        }

        /// <summary>
        /// 关闭弹窗并执行主要按钮回调。
        /// </summary>
        private void ConfirmPrimaryAction()
        {
            Action action = _primaryAction;
            Hide();
            action?.Invoke();
        }

        /// <summary>
        /// 关闭弹窗并执行次要按钮回调。
        /// </summary>
        private void ConfirmSecondaryAction()
        {
            Action action = _secondaryAction;
            Hide();
            action?.Invoke();
        }
    }
}
