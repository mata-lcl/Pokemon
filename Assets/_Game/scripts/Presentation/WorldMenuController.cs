using System;
using Pokemon.Application;
using Pokemon.Presentation.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    /// <summary>
    /// 控制局外菜单，不依赖各面板的内部 UI 结构。
    /// UI 按钮和键盘快捷键统一调用相同的公开方法。
    /// </summary>
    public class WorldMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject savePanel;
        [SerializeField] private GameObject bagPanel;
        [SerializeField] private GameObject storagePanel;
        [SerializeField] private GameObject questPanel;
        [SerializeField] private WorldBagController bagController;
        [SerializeField] private WorldPokemonStorageController storageController;
        [SerializeField] private WorldGmController gmPanel;
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private ConfirmationDialogController exitDialog;

        [Header("菜单按钮")]
        [SerializeField] private GameObject saveButton;
        [SerializeField] private GameObject bagButton;
        [SerializeField] private GameObject storageButton;
        [SerializeField] private Button questButton;
        [SerializeField] private Button gmButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject closeButton;

        [Header("Scene")]
        [SerializeField] private string loadSceneName = "Load";

        [Header("Input")]
        [SerializeField] private KeyCode bagKey = KeyCode.B;
        [SerializeField] private KeyCode storageKey = KeyCode.P;
        [SerializeField] private KeyCode questKey = KeyCode.J;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("World Input")]
        [SerializeField] private PlayerMovement playerMovement;

        private GameObject _currentPanel;

        public event Action SaveRequested;

        private void Awake()
        {
            transform.localScale = Vector3.one;
            settingsButton.onClick.AddListener(ShowSettings);
            gmButton.onClick.AddListener(ShowGm);
            exitButton.onClick.AddListener(ShowExitConfirmation);
            if (questButton != null)
                questButton.onClick.AddListener(ToggleQuest);
            gmPanel.CloseRequested += ShowMenu;
            settingsPanel.CloseRequested += ShowMenu;
            if (storageController != null)
                storageController.ConfigureStorageCapacity();
            HideContentPanels();
            SetMenuVisible(false);
            SetMainMenuButtonsVisible(true);
            SetCloseButtonVisible(false);
            SetPlayerMovementEnabled(true);
        }

        private void Update()
        {
            if (IsCloseRequested())
            {
                if (_currentPanel == null)
                    ShowMenu();
                else
                    CloseCurrentPanel();
            }
            else if (Input.GetKeyDown(bagKey))
                ToggleBag();
            else if (Input.GetKeyDown(storageKey))
                ToggleStorage();
            else if (Input.GetKeyDown(questKey))
                ToggleQuest();
        }

        /// <summary>
        /// 检查键盘关闭键或项目 Cancel 输入是否在当前帧触发。
        /// </summary>
        /// <returns>当前帧触发关闭输入时返回 true。</returns>
        private bool IsCloseRequested()
        {
            return Input.GetKeyDown(closeKey) || Input.GetButtonDown("Cancel");
        }

        private void OnDisable()
        {
            // 控制器被禁用或重新构建时，确保玩家不会一直处于锁定状态。
            SetPlayerMovementEnabled(true);
        }

        /// <summary>
        /// 解除设置、退出按钮和设置面板事件绑定。
        /// </summary>
        private void OnDestroy()
        {
            settingsButton.onClick.RemoveListener(ShowSettings);
            gmButton.onClick.RemoveListener(ShowGm);
            exitButton.onClick.RemoveListener(ShowExitConfirmation);
            if (questButton != null)
                questButton.onClick.RemoveListener(ToggleQuest);
            gmPanel.CloseRequested -= ShowMenu;
            settingsPanel.CloseRequested -= ShowMenu;
        }

        public void ToggleBag()
        {
            if (_currentPanel == bagPanel)
                CloseCurrentPanel();
            else
                ShowBag();
        }

        public void ToggleStorage()
        {
            if (_currentPanel == storagePanel)
                CloseCurrentPanel();
            else
                ShowStorage();
        }

        /// <summary>
        /// 在任务面板和世界场景之间切换显示状态。
        /// </summary>
        public void ToggleQuest()
        {
            if (_currentPanel == questPanel)
                CloseCurrentPanel();
            else
                ShowQuest();
        }

        /// <summary>
        /// 显示场景主菜单并暂停玩家移动。
        /// </summary>
        public void ShowMenu()
        {
            HideContentPanels();
            SetMenuVisible(true);
            SetMainMenuButtonsVisible(true);
            SetCloseButtonVisible(true);
            _currentPanel = menuPanel;
            SetPlayerMovementEnabled(false);
        }

        /// <summary>
        /// 显示存档功能页。
        /// </summary>
        public void ShowSave()
        {
            ShowPanel(savePanel, "save");
        }

        /// <summary>
        /// 向后续接入的存档数据服务发送存档请求。
        /// </summary>
        public void RequestSave()
        {
            SaveRequested?.Invoke();
        }

        // 将此方法绑定到局外背包按钮。
        public void ShowBag()
        {
            ShowPanel(bagPanel, "bag");

            if (_currentPanel != bagPanel)
                return;

            if (bagController != null)
                bagController.Show();
            else
                Debug.LogWarning("WorldMenuController 无法刷新背包：控制器引用未绑定。", this);
        }

        // 将此方法绑定到局外仓库按钮。
        public void ShowStorage()
        {
            ShowPanel(storagePanel, "storage");

            if (_currentPanel != storagePanel)
                return;

            if (storageController != null)
                storageController.Show();
            else
                Debug.LogWarning("WorldMenuController 无法刷新仓库：控制器引用未绑定。", this);
        }

        /// <summary>
        /// 显示任务面板并暂停玩家移动。
        /// </summary>
        public void ShowQuest()
        {
            ShowPanel(questPanel, "quest");
        }

        // 将此方法绑定到面板关闭按钮。
        public void CloseCurrentPanel()
        {
            HideContentPanels();
            SetMenuVisible(false);
            SetMainMenuButtonsVisible(true);
            SetCloseButtonVisible(false);
            SetPlayerMovementEnabled(true);
        }

        /// <summary>
        /// 显示声音设置面板。
        /// </summary>
        public void ShowSettings()
        {
            ShowPanel(settingsPanel.gameObject, "settings");
        }

        /// <summary>
        /// 显示场外 GM 测试面板。
        /// </summary>
        public void ShowGm()
        {
            ShowPanel(gmPanel.gameObject, "gm");
        }

        /// <summary>
        /// 显示返回主菜单或退出游戏的确认弹窗。
        /// </summary>
        public void ShowExitConfirmation()
        {
            HideContentPanels();
            SetMenuVisible(false);
            exitDialog.Show(
                "是否退出到主菜单",
                "是",
                ReturnToLoadScene,
                "退出游戏",
                GameApplicationService.QuitGame);
            _currentPanel = exitDialog.gameObject;
            SetCloseButtonVisible(true);
            SetPlayerMovementEnabled(false);
        }

        private void ShowPanel(GameObject target, string panelName)
        {
            if (target == null)
            {
                Debug.LogWarning($"WorldMenuController cannot open {panelName}: panel reference is missing.", this);
                return;
            }

            HideContentPanels();
            SetMenuVisible(false);
            target.SetActive(true);
            _currentPanel = target;

            SetCloseButtonVisible(true);
            SetPlayerMovementEnabled(false);
        }

        private void HideContentPanels()
        {
            if (bagPanel != null)
                bagPanel.SetActive(false);
            if (storagePanel != null)
                storagePanel.SetActive(false);
            if (questPanel != null)
                questPanel.SetActive(false);
            if (savePanel != null)
                savePanel.SetActive(false);
            gmPanel.Hide();
            settingsPanel.Hide();
            exitDialog.Hide();

            _currentPanel = null;
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuPanel != null)
                menuPanel.SetActive(visible);
        }

        private void SetMainMenuButtonsVisible(bool visible)
        {
            if (saveButton != null)
                saveButton.SetActive(visible);
            if (bagButton != null)
                bagButton.SetActive(visible);
            if (storageButton != null)
                storageButton.SetActive(visible);
            if (questButton != null)
                questButton.gameObject.SetActive(visible);
            gmButton.gameObject.SetActive(visible);
            settingsButton.gameObject.SetActive(visible);
            exitButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 返回 Load 主菜单场景。
        /// </summary>
        private void ReturnToLoadScene()
        {
            SceneManager.LoadScene(loadSceneName);
        }

        private void SetCloseButtonVisible(bool visible)
        {
            if (closeButton != null)
                closeButton.SetActive(visible);
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            if (playerMovement != null)
                playerMovement.SetMovementEnabled(enabled);
        }
    }
}
