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
        [SerializeField] private GameObject itemPanel;

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

        [Header("Skill buttons")]
        [SerializeField] private Button[] skillButtons;
        [SerializeField] private TMP_Text[] skillBtnTexts;

        [Header("Item buttons")]
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private TMP_Text[] itemBtnTexts;

        [Header("Playback control")]
        [SerializeField] private Button playbackModeButton;
        [SerializeField] private TMP_Text playbackModeText;

        public event Action<int> OnSkillClicked;
        public event Action<ItemData> OnItemClicked;
        public event Action OnRunClicked;
        public event Action OnPlaybackModeClicked;
        public event Action OnPokemonClicked;
        public event Action OnPokemonSwitchCancelled;
        public event Action<MonsterRuntime> OnPokemonSelected;

        private List<ItemData> _cachedItems = new List<ItemData>();
        private List<MonsterRuntime> _cachedPokemon = new List<MonsterRuntime>();

        private void Awake()
        {
            if (fightBtn != null) fightBtn.onClick.AddListener(() => ShowSubPanel(skillPanel));
            if (bagBtn != null)
            {
                bagBtn.onClick.AddListener(RefreshItemList);
                bagBtn.onClick.AddListener(() => ShowSubPanel(itemPanel));
            }
            if (skillBackBtn != null) skillBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));
            if (BagBackBtn != null) BagBackBtn.onClick.AddListener(() => ShowSubPanel(mainActionPanel));
            if (runBtn != null) runBtn.onClick.AddListener(() => OnRunClicked?.Invoke());

            if (pokemonCollectionPanel == null)
                pokemonCollectionPanel = FindObjectOfType<PokemonCollectionPanel>(true);

            if (pokemonCollectionPanel != null)
            {
                pokemonCollectionPanel.OnConfirmed += HandleCollectionConfirmed;
                pokemonCollectionPanel.OnCancelled += HandleCollectionCancelled;
            }
            else
            {
                Debug.LogError("BattleUIController requires a PokemonCollectionPanel reference.");
            }

            if (pokemonBtn != null) pokemonBtn.onClick.AddListener(() => OnPokemonClicked?.Invoke());

            if (playbackModeButton != null)
                playbackModeButton.onClick.AddListener(() => OnPlaybackModeClicked?.Invoke());

            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i;
                if (skillButtons[i] != null)
                    skillButtons[i].onClick.AddListener(() => OnSkillClicked?.Invoke(index));
            }

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
            if (mainActionPanel != null) mainActionPanel.SetActive(targetPanel == mainActionPanel);
            if (skillPanel != null) skillPanel.SetActive(targetPanel == skillPanel);
            if (itemPanel != null) itemPanel.SetActive(targetPanel == itemPanel);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.Hide();
        }

        public void HideAllPanels()
        {
            if (mainActionPanel != null) mainActionPanel.SetActive(false);
            if (skillPanel != null) skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
            if (pokemonCollectionPanel != null)
                pokemonCollectionPanel.Hide();
        }

        public void ResetToMain()
        {
            ShowSubPanel(mainActionPanel);
        }

        public void ShowPokemonSelection(IReadOnlyList<MonsterRuntime> pokemon)
        {
            ShowPokemonSelection(pokemon, PlayerParty.ActivePokemon);
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

        public void RefreshItemList()
        {
            _cachedItems = PlayerParty.GetUsableItems();
            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (itemButtons[i] == null) continue;
                bool available = i < _cachedItems.Count;
                itemButtons[i].gameObject.SetActive(available);
                if (!available) continue;
                ItemData item = _cachedItems[i];
                int count = PlayerParty.Inventory[item];
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
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null) continue;
                bool available = skills != null && i < skills.Count;
                skillButtons[i].gameObject.SetActive(available);
                if (!available) continue;
                SkillData skill = skills[i];
                int currentPP = ppMap.ContainsKey(skill) ? ppMap[skill] : 0;
                skillButtons[i].interactable = currentPP > 0;
                if (i < skillBtnTexts.Length && skillBtnTexts[i] != null)
                    skillBtnTexts[i].text = $"{skill.DisplayName} ({currentPP})";
            }
        }
    }
}
