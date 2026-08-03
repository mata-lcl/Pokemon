using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonSlotView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject faintedOverlay;
        [SerializeField] private GameObject emptyState;

        [Header("Interaction")]
        [SerializeField] private Button button;

        private MonsterRuntime _pokemon;
        private Action<MonsterRuntime> _onClicked;

        public MonsterRuntime Pokemon => _pokemon;

        public void Bind(
            MonsterRuntime pokemon,
            bool isActive,
            bool isFainted,
            Action<MonsterRuntime> onClicked)
        {
            _pokemon = pokemon;
            _onClicked = onClicked;

            if (emptyState != null)
                emptyState.SetActive(pokemon == null);

            if (pokemon == null)
            {
                SetContentVisible(false);
                if (button != null)
                    button.interactable = false;

                return;
            }

            SetContentVisible(true);

            if (avatar != null)
                avatar.sprite = pokemon.Species.BattleSprite;

            if (nameText != null)
                nameText.text = pokemon.Species.DisplayName;

            if (levelText != null)
                levelText.text = $"Lv.{pokemon.Level}";

            if (hpText != null)
                hpText.text = $"HP {pokemon.CurrentHP}/{pokemon.MaxHP}";

            if (selectedFrame != null)
                selectedFrame.SetActive(isActive);

            if (faintedOverlay != null)
                faintedOverlay.SetActive(isFainted);

            if (button != null)
            {
                // button.interactable = !isActive && !isFainted;
                button.interactable = !isFainted;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClicked?.Invoke(_pokemon));
            }
        }

        public void Clear()
        {
            _pokemon = null;

            SetContentVisible(false);

            if (emptyState != null)
                emptyState.SetActive(true);

            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void SetContentVisible(bool visible)
        {
            if (avatar != null) avatar.gameObject.SetActive(visible);
            if (nameText != null) nameText.gameObject.SetActive(visible);
            if (levelText != null) levelText.gameObject.SetActive(visible);
            if (hpText != null) hpText.gameObject.SetActive(visible);
            if (selectedFrame != null && !visible) selectedFrame.SetActive(false);
            if (faintedOverlay != null && !visible) faintedOverlay.SetActive(false);
        }
    }
}
