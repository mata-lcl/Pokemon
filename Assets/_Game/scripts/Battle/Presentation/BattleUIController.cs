using Pokemon.Domain;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Pokemon.Presentation.UI;

namespace Pokemon.Presentation
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Action panels")]
        [SerializeField] private GameObject mainActionPanel;
        [SerializeField] private GameObject skillPanel;
        // Kept as a fallback until the new BagPanel is assigned in the scene.
        [SerializeField] private GameObject itemPanel;
        [SerializeField] private BagPanel bagPanel;

        [Header("Action buttons")]
        [SerializeField] private Button fightBtn;
        [SerializeField] private Button bagBtn;
        [SerializeField] private Button runBtn;
        [SerializeField] private Button skillBackBtn;
        [SerializeField] private Button BagBackBtn;

        [Header("Pokemon switch")]
        [SerializeField] private Button pokemonBtn;
        [SerializeField] private PokemonCollectionPanel pokemonCollectionPanel;
        // Legacy fields are kept only so existing scene data can migrate safely.
        [HideInInspector, SerializeField] private GameObject pokemonPanel;
        [HideInInspector, SerializeField] private Button pokemonBackBtn;
        [HideInInspector, SerializeField] private Button[] pokemonButtons;
        [HideInInspector, SerializeField] private TMP_Text[] pokemonBtnTexts;

        [Header("Battle text")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text logText;

        [Header("HP bars")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private Image enemyHpFill;

        [Header("Pokemon indicators")]
        [SerializeField] private Image playerPrimaryTypeImage;
        [SerializeField] private Image playerSecondaryTypeImage;
        [SerializeField] private Image enemyPrimaryTypeImage;
        [SerializeField] private Image enemySecondaryTypeImage;
        [SerializeField] private Image playerStatusImage;
        [SerializeField] private Image enemyStatusImage;

        [Header("Status icons")]
        [SerializeField] private Sprite poisonStatusIcon;
        [SerializeField] private Sprite burnStatusIcon;
        [SerializeField] private Sprite paralyzeStatusIcon;
        [SerializeField] private Sprite sleepStatusIcon;
        [SerializeField] private Sprite freezeStatusIcon;

        [Header("Skill buttons")]
        [SerializeField] private Transform skillButtonContainer;
        [SerializeField] private SkillButtonView skillButtonPrefab;
        [SerializeField] private SkillTooltipView skillTooltip;
        [SerializeField] private PokemonTypeVisualCatalog typeVisualCatalog;
        [HideInInspector, SerializeField] private Button[] skillButtons;
        [HideInInspector, SerializeField] private TMP_Text[] skillBtnTexts;

        [Header("Item buttons")]
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private TMP_Text[] itemBtnTexts;

        [Header("Playback control")]
        [SerializeField] private Button playbackModeButton;
        [SerializeField] private TMP_Text playbackModeText;

        public event Action<int> OnSkillClicked;
        public event Action<ItemData> OnItemClicked;
        public event Action OnBagClicked;
        public event Action<ItemData, ItemData> OnItemReorderRequested;
        public event Action OnRunClicked;
        public event Action OnPlaybackModeClicked;
        public event Action OnPokemonClicked;
        public event Action OnPokemonSwitchCancelled;
        public event Action<MonsterRuntime> OnPokemonSelected;

        private List<ItemData> _cachedItems = new List<ItemData>();
        private List<MonsterRuntime> _cachedPokemon = new List<MonsterRuntime>();
        private readonly List<SkillButtonView> _skillButtonViews =
            new List<SkillButtonView>();
        private bool _skillInputInteractable = true;

        private void Awake()
        {
            skillTooltip.Hide();
            if (fightBtn != null) fightBtn.onClick.AddListener(() => ShowSubPanel(skillPanel));
            if (bagBtn != null)
                bagBtn.onClick.AddListener(HandleBagClicked);
            if (skillBackBtn != null) skillBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));
            if (BagBackBtn != null) BagBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));
            if (runBtn != null) runBtn.onClick.AddListener(() => OnRunClicked?.Invoke());

            if (pokemonCollectionPanel == null)
                pokemonCollectionPanel = FindObjectOfType<PokemonCollectionPanel>(true);

            if (bagPanel == null)
                bagPanel = FindObjectOfType<BagPanel>(true);

            if (pokemonCollectionPanel != null)
            {
                pokemonCollectionPanel.OnConfirmed += HandleCollectionConfirmed;
                pokemonCollectionPanel.OnCancelled += HandleCollectionCancelled;
            }
            else
            {
                Debug.LogError("BattleUIController requires a PokemonCollectionPanel reference.");
            }

            if (bagPanel != null)
            {
                bagPanel.OnUseConfirmed += HandleBagUseConfirmed;
                bagPanel.OnCancelled += HandleBagCancelled;
                bagPanel.ItemReorderRequested += HandleItemReorderRequested;
            }

            if (pokemonBtn != null) pokemonBtn.onClick.AddListener(() => OnPokemonClicked?.Invoke());

            if (playbackModeButton != null)
                playbackModeButton.onClick.AddListener(() => OnPlaybackModeClicked?.Invoke());

            DisableLegacySkillButtons();

            for (int i = 0; i < itemButtons.Length; i++)
            {
                int index = i;
                if (itemButtons[i] != null)
                    itemButtons[i].onClick.AddListener(() =>
                    {
                        if (index < _cachedItems.Count) OnItemClicked?.Invoke(_cachedItems[index]);
                    });
            }
        }

        private void OnDestroy()
        {
            if (pokemonCollectionPanel != null)
            {
                pokemonCollectionPanel.OnConfirmed -= HandleCollectionConfirmed;
                pokemonCollectionPanel.OnCancelled -= HandleCollectionCancelled;
            }

            if (bagPanel != null)
            {
                bagPanel.OnUseConfirmed -= HandleBagUseConfirmed;
                bagPanel.OnCancelled -= HandleBagCancelled;
                bagPanel.ItemReorderRequested -= HandleItemReorderRequested;
            }
        }

        private void HandleCollectionConfirmed(MonsterRuntime pokemon)
        {
            OnPokemonSelected?.Invoke(pokemon);
        }

        private void HandleCollectionCancelled()
        {
            OnPokemonSwitchCancelled?.Invoke();
        }

        private void ShowSubPanel(GameObject targetPanel)
        {
            skillTooltip.Hide();
            if (mainActionPanel != null) mainActionPanel.SetActive(targetPanel == mainActionPanel);
            if (skillPanel != null) skillPanel.SetActive(targetPanel == skillPanel);
            if (itemPanel != null) itemPanel.SetActive(targetPanel == itemPanel);
            if (bagPanel != null) bagPanel.gameObject.SetActive(false);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.Hide();
        }

        /// <summary>
        /// 通知战斗协调器读取并显示最新背包数据。
        /// </summary>
        private void HandleBagClicked()
        {
            OnBagClicked?.Invoke();
        }

        /// <summary>
        /// 使用应用层提供的道具快照打开战斗背包界面。
        /// </summary>
        /// <param name="items">保持当前顺序的可使用道具及数量。</param>
        public void ShowBagPanel(IReadOnlyList<InventoryItemStack> items)
        {
            if (bagPanel == null)
            {
                // Fall back to the legacy list until the new prefab is assigned.
                RefreshItemList(items);
                ShowSubPanel(itemPanel);
                return;
            }

            if (mainActionPanel != null) mainActionPanel.SetActive(false);
            if (skillPanel != null) skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.Hide();

            bagPanel.gameObject.SetActive(true);
            bagPanel.Refresh(items);
        }

        private void HandleBagUseConfirmed(ItemData item)
        {
            OnItemClicked?.Invoke(item);
        }

        /// <summary>
        /// 将战斗背包中的道具顺序调整请求转发给战斗协调器。
        /// </summary>
        /// <param name="item">需要移动的道具。</param>
        /// <param name="targetItem">作为目标位置的道具。</param>
        private void HandleItemReorderRequested(ItemData item, ItemData targetItem)
        {
            OnItemReorderRequested?.Invoke(item, targetItem);
        }

        private void HandleBagCancelled()
        {
            if (bagPanel != null)
                bagPanel.gameObject.SetActive(false);
            ResetToMain();
        }

        public void HideAllPanels()
        {
            skillTooltip.Hide();
            if (mainActionPanel != null) mainActionPanel.SetActive(false);
            if (skillPanel != null) skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
            if (bagPanel != null) bagPanel.gameObject.SetActive(false);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.Hide();
        }

        public void ResetToMain()
        {
            ShowSubPanel(mainActionPanel);
        }

        public void ShowPokemonSelection(IReadOnlyList<MonsterRuntime> pokemon)
        {
            ShowPokemonSelection(pokemon, null);
        }

        public void ShowPokemonSelection(
            IReadOnlyList<MonsterRuntime> pokemon,
            MonsterRuntime activePokemon)
        {
            _cachedPokemon = pokemon == null
                ? new List<MonsterRuntime>()
                : new List<MonsterRuntime>(pokemon);

            if (pokemonCollectionPanel != null)
            {
                ShowSubPanel(null);
                pokemonCollectionPanel.Show(_cachedPokemon, activePokemon);
                return;
            }
        }

        private void EnsurePokemonPanel()
        {
            if (pokemonPanel != null && pokemonButtons != null && pokemonButtons.Length > 0) return;

            Transform parent = mainActionPanel != null ? mainActionPanel.transform.parent : transform;
            GameObject panelObject = new GameObject("PokemonSwitchPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500f, 360f);
            panelObject.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.96f);

            CreateLabel(panelObject.transform, "选择出战精灵", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f), 24f);

            const int maxButtons = 6;
            pokemonButtons = new Button[maxButtons];
            pokemonBtnTexts = new TMP_Text[maxButtons];
            for (int i = 0; i < maxButtons; i++)
            {
                GameObject buttonObject = new GameObject($"PokemonButton{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panelObject.transform, false);
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.08f, 0.68f - i * 0.095f);
                buttonRect.anchorMax = new Vector2(0.92f, 0.76f - i * 0.095f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                pokemonButtons[i] = buttonObject.GetComponent<Button>();
                pokemonBtnTexts[i] = CreateLabel(buttonObject.transform, string.Empty, Vector2.zero, Vector2.one, 18f);
            }

            GameObject backObject = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backObject.transform.SetParent(panelObject.transform, false);
            RectTransform backRect = backObject.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.3f, 0.03f);
            backRect.anchorMax = new Vector2(0.7f, 0.13f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            CreateLabel(backObject.transform, "返回", Vector2.zero, Vector2.one, 18f);

            pokemonPanel = panelObject;
            pokemonBackBtn = backObject.GetComponent<Button>();
            pokemonPanel.SetActive(false);
        }

        private static TMP_Text CreateLabel(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            return label;
        }

        public void ItemButtonCallback(ItemData item) => OnItemClicked?.Invoke(item);

        /// <summary>
        /// 使用应用层提供的道具快照刷新旧版战斗道具按钮。
        /// </summary>
        /// <param name="items">保持当前顺序的可使用道具及数量。</param>
        public void RefreshItemList(IReadOnlyList<InventoryItemStack> items)
        {
            _cachedItems.Clear();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                    _cachedItems.Add(items[i].Item);
            }

            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (itemButtons[i] == null) continue;
                bool available = i < _cachedItems.Count;
                itemButtons[i].gameObject.SetActive(available);
                if (!available) continue;
                ItemData item = _cachedItems[i];
                int count = items[i].Count;
                itemButtons[i].interactable = count > 0;
                if (i < itemBtnTexts.Length && itemBtnTexts[i] != null)
                    itemBtnTexts[i].text = $"{item.DisplayName} x{count}";
            }
        }

        public void SetupNames(string playerName, string enemyName)
        {
            if (playerNameText != null) playerNameText.text = playerName;
            if (enemyNameText != null) enemyNameText.text = enemyName;
        }

        public void UpdateHp(int playerHp, int playerMax, int enemyHp, int enemyMax)
        {
            if (playerHpText != null) playerHpText.text = $"HP: {playerHp}/{playerMax}";
            if (enemyHpText != null) enemyHpText.text = $"HP: {enemyHp}/{enemyMax}";
            if (playerHpFill != null) playerHpFill.fillAmount = Mathf.Clamp01((float)playerHp / playerMax);
            if (enemyHpFill != null) enemyHpFill.fillAmount = Mathf.Clamp01((float)enemyHp / enemyMax);
        }

        /// <summary>
        /// 刷新双方精灵的属性图标和异常状态图标。
        /// </summary>
        /// <param name="player">当前玩家精灵。</param>
        /// <param name="enemy">当前敌方精灵。</param>
        public void UpdatePokemonIndicators(MonsterRuntime player, MonsterRuntime enemy)
        {
            UpdateTypeImages(
                player.Species,
                playerPrimaryTypeImage,
                playerSecondaryTypeImage);
            UpdateTypeImages(
                enemy.Species,
                enemyPrimaryTypeImage,
                enemySecondaryTypeImage);
            UpdateStatusImage(player.CurrentStatus, playerStatusImage);
            UpdateStatusImage(enemy.CurrentStatus, enemyStatusImage);
        }

        /// <summary>
        /// 根据精灵配置刷新主属性和副属性图片。
        /// </summary>
        /// <param name="species">需要显示属性的精灵配置。</param>
        /// <param name="primaryImage">主属性图片组件。</param>
        /// <param name="secondaryImage">副属性图片组件。</param>
        private static void UpdateTypeImages(
            PokemonSpeciesData species,
            Image primaryImage,
            Image secondaryImage)
        {
            primaryImage.sprite = species.PrimaryTypeIcon;
            primaryImage.gameObject.SetActive(species.PrimaryTypeIcon != null);
            secondaryImage.sprite = species.SecondaryTypeIcon;
            secondaryImage.gameObject.SetActive(species.SecondaryTypeIcon != null);
        }

        /// <summary>
        /// 根据当前异常状态刷新对应图片。
        /// </summary>
        /// <param name="status">需要显示的异常状态。</param>
        /// <param name="statusImage">承载异常状态图片的组件。</param>
        private void UpdateStatusImage(StatusCondition status, Image statusImage)
        {
            Sprite statusIcon = GetStatusIcon(status);
            statusImage.sprite = statusIcon;
            statusImage.gameObject.SetActive(statusIcon != null);
        }

        /// <summary>
        /// 获取指定异常状态在战斗界面使用的图片。
        /// </summary>
        /// <param name="status">需要查询的异常状态。</param>
        /// <returns>对应的异常状态图片；无状态时返回 null。</returns>
        private Sprite GetStatusIcon(StatusCondition status)
        {
            return status switch
            {
                StatusCondition.Poison => poisonStatusIcon,
                StatusCondition.Burn => burnStatusIcon,
                StatusCondition.Paralyze => paralyzeStatusIcon,
                StatusCondition.Sleep => sleepStatusIcon,
                StatusCondition.Freeze => freezeStatusIcon,
                _ => null
            };
        }

        public void SetLog(string message)
        {
            Debug.Log($"[UI LOG] {message}");
            if (logText != null) logText.text = message;
        }

        public void SetPlaybackModeLabel(string label)
        {
            if (playbackModeText != null) playbackModeText.text = label;
        }

        public void SetPlaybackControlInteractable(bool interactable)
        {
            if (playbackModeButton != null) playbackModeButton.interactable = interactable;
        }

        public void SetInteractable(bool interactable)
        {
            _skillInputInteractable = interactable;
            if (!interactable)
                skillTooltip.Hide();

            SetInteractable(fightBtn, interactable);
            SetInteractable(bagBtn, interactable);
            SetInteractable(runBtn, interactable);
            SetInteractable(skillBackBtn, interactable);
            SetInteractable(BagBackBtn, interactable);
            SetInteractable(pokemonBtn, interactable);
            SetInteractable(pokemonBackBtn, interactable);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.SetInteractable(interactable);
            SetInteractable(skillButtons, interactable);
            for (int i = 0; i < _skillButtonViews.Count; i++)
                _skillButtonViews[i].SetInteractable(interactable);
            SetInteractable(itemButtons, interactable);
            SetInteractable(pokemonButtons, interactable);
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        private static void SetInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null) return;
            foreach (Button button in buttons) SetInteractable(button, interactable);
        }

        public void RefreshSkills(List<SkillData> skills, IReadOnlyDictionary<SkillData, int> ppMap)
        {
            int skillCount = skills != null ? skills.Count : 0;
            EnsureSkillButtonCount(skillCount);
            skillTooltip.Hide();

            for (int i = 0; i < _skillButtonViews.Count; i++)
            {
                SkillButtonView buttonView = _skillButtonViews[i];
                bool available = i < skillCount;
                buttonView.gameObject.SetActive(available);
                if (!available) continue;

                SkillData skill = skills[i];
                int currentPP = ppMap.ContainsKey(skill) ? ppMap[skill] : 0;
                PokemonTypeVisual typeVisual =
                    typeVisualCatalog.GetVisual(skill.Type);
                buttonView.Bind(
                    skill,
                    currentPP,
                    i,
                    typeVisual,
                    HandleSkillButtonClicked,
                    HandleSkillPreviewRequested,
                    HandleSkillPreviewEnded);
                buttonView.SetInteractable(_skillInputInteractable);
            }
        }

        /// <summary>
        /// 创建足够数量的技能按钮视图，已经创建的按钮会继续复用。
        /// </summary>
        /// <param name="requiredCount">当前技能列表需要的按钮数量。</param>
        private void EnsureSkillButtonCount(int requiredCount)
        {
            while (_skillButtonViews.Count < requiredCount)
            {
                SkillButtonView buttonView = Instantiate(
                    skillButtonPrefab,
                    skillButtonContainer);
                _skillButtonViews.Add(buttonView);
            }
        }

        /// <summary>
        /// 将技能按钮点击事件继续转发为原有的技能下标事件。
        /// </summary>
        /// <param name="skillIndex">玩家当前技能列表中的下标。</param>
        private void HandleSkillButtonClicked(int skillIndex)
        {
            OnSkillClicked?.Invoke(skillIndex);
        }

        /// <summary>
        /// 根据当前悬停或选中的技能刷新共享描述面板。
        /// </summary>
        /// <param name="skill">需要预览的技能。</param>
        /// <param name="currentPP">技能当前剩余 PP。</param>
        private void HandleSkillPreviewRequested(
            SkillData skill,
            int currentPP)
        {
            PokemonTypeVisual typeVisual =
                typeVisualCatalog.GetVisual(skill.Type);
            skillTooltip.Show(skill, currentPP, typeVisual);
        }

        /// <summary>
        /// 结束技能按钮预览并隐藏共享描述面板。
        /// </summary>
        private void HandleSkillPreviewEnded()
        {
            skillTooltip.Hide();
        }

        /// <summary>
        /// 隐藏场景中旧版固定技能按钮，避免与新 Prefab 按钮重复显示。
        /// </summary>
        private void DisableLegacySkillButtons()
        {
            if (skillButtons != null)
            {
                for (int i = 0; i < skillButtons.Length; i++)
                {
                    if (skillButtons[i] != null)
                        skillButtons[i].gameObject.SetActive(false);
                }
            }

            if (skillBtnTexts == null) return;
            for (int i = 0; i < skillBtnTexts.Length; i++)
            {
                if (skillBtnTexts[i] != null)
                    skillBtnTexts[i].text = string.Empty;
            }
        }
    }
}
