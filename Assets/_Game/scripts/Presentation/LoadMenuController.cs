using Pokemon.Application;
using Pokemon.Domain;
using Pokemon.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public class LoadMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject loadSlotPanel;

        [Header("Main Menu")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Overlays")]
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private ConfirmationDialogController exitDialog;

        [Header("Load Slots")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotTexts;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Save Assets")]
        [SerializeField] private SaveAssetCatalog assetCatalog;

        [Header("New Game")]
        [SerializeField] private PokemonSpeciesData starterSpecies;
        [SerializeField] private ItemData starterItem;
        [SerializeField] private int starterItemCount = 5;
        [SerializeField] private string newGameSceneName = "World";

        /// <summary>
        /// 绑定主菜单、读档栏和返回按钮事件。
        /// </summary>
        private void Awake()
        {
            newGameButton.onClick.AddListener(StartNewGame);
            loadGameButton.onClick.AddListener(ShowLoadSlots);
            settingsButton.onClick.AddListener(ShowSettings);
            exitButton.onClick.AddListener(ShowExitConfirmation);
            backButton.onClick.AddListener(ShowMainMenu);
            settingsPanel.CloseRequested += ShowMainMenu;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotIndex = i;
                slotButtons[i].onClick.AddListener(() => LoadSlot(slotIndex));
            }
        }

        /// <summary>
        /// 场景启用时显示主菜单并刷新读档栏状态。
        /// </summary>
        private void OnEnable()
        {
            ShowMainMenu();
            RefreshLoadSlots();
        }

        /// <summary>
        /// 清空旧运行数据、初始化新游戏精灵和道具并进入世界场景。
        /// </summary>
        private void StartNewGame()
        {
            SaveGameService.ClearPendingPlayerPosition();
            PlayerParty.ResetState();
            QuestService.ResetState();
            PlayerParty.AddMonster(new MonsterRuntime(starterSpecies, 5));
            PlayerParty.AddItem(starterItem, starterItemCount);
            SceneManager.LoadScene(newGameSceneName);
        }

        /// <summary>
        /// 显示读档栏界面并刷新三个存档栏摘要。
        /// </summary>
        private void ShowLoadSlots()
        {
            mainMenuPanel.SetActive(false);
            loadSlotPanel.SetActive(true);
            statusText.text = string.Empty;
            RefreshLoadSlots();
        }

        /// <summary>
        /// 返回主菜单界面。
        /// </summary>
        private void ShowMainMenu()
        {
            settingsPanel.Hide();
            exitDialog.Hide();
            mainMenuPanel.SetActive(true);
            loadSlotPanel.SetActive(false);
        }

        /// <summary>
        /// 显示声音设置面板。
        /// </summary>
        private void ShowSettings()
        {
            mainMenuPanel.SetActive(false);
            loadSlotPanel.SetActive(false);
            settingsPanel.Show();
        }

        /// <summary>
        /// 显示退出游戏确认弹窗。
        /// </summary>
        private void ShowExitConfirmation()
        {
            mainMenuPanel.SetActive(false);
            loadSlotPanel.SetActive(false);
            exitDialog.Show(
                "是否退出游戏",
                "是",
                GameApplicationService.QuitGame,
                "否",
                ShowMainMenu);
        }

        /// <summary>
        /// 解除主菜单按钮和设置面板事件绑定。
        /// </summary>
        private void OnDestroy()
        {
            newGameButton.onClick.RemoveListener(StartNewGame);
            loadGameButton.onClick.RemoveListener(ShowLoadSlots);
            settingsButton.onClick.RemoveListener(ShowSettings);
            exitButton.onClick.RemoveListener(ShowExitConfirmation);
            backButton.onClick.RemoveListener(ShowMainMenu);
            settingsPanel.CloseRequested -= ShowMainMenu;
        }

        /// <summary>
        /// 读取指定存档栏并进入该存档记录的场景。
        /// </summary>
        /// <param name="slotIndex">从零开始的存档栏索引。</param>
        private void LoadSlot(int slotIndex)
        {
            SaveGameData data = SaveGameService.Load(slotIndex, assetCatalog);
            SceneManager.LoadScene(data.sceneName);
        }

        /// <summary>
        /// 刷新三个读档栏的时间、场景、队伍数量和可点击状态。
        /// </summary>
        private void RefreshLoadSlots()
        {
            var summaries = SaveGameService.GetSlotSummaries();
            for (int i = 0; i < slotTexts.Length; i++)
            {
                SaveSlotSummary summary = summaries[i];
                slotButtons[i].interactable = summary.HasSave;
                slotTexts[i].text = summary.HasSave
                    ? $"存档栏 {i + 1}\n{summary.SavedAt}  {summary.SceneName}  队伍 {summary.PartyCount}"
                    : $"存档栏 {i + 1}\n空存档";
            }
        }
    }
}
