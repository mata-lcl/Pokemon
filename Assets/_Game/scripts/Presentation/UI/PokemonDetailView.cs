using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonDetailView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Image largeAvatar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text typeText;

        [Header("HP")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image hpFill;

        [Header("Stats")]
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text specialAttackText;
        [SerializeField] private TMP_Text specialDefenseText;

        public MonsterRuntime CurrentPokemon { get; private set; }

        public void Show(MonsterRuntime pokemon)
        {
            CurrentPokemon = pokemon;

            if (pokemon == null)
            {
                Clear();
                return;
            }

            if (largeAvatar != null)
                largeAvatar.sprite = pokemon.Species.BattleSprite;

            if (nameText != null)
                nameText.text = pokemon.Species.DisplayName;

            if (levelText != null)
                levelText.text = $"Lv.{pokemon.Level}";

            if (typeText != null)
            {
                typeText.text = pokemon.Species.SecondaryType == PokemonType.None
                    ? pokemon.Species.PrimaryType.ToString()
                    : $"{pokemon.Species.PrimaryType} / {pokemon.Species.SecondaryType}";
            }

            if (hpText != null)
                hpText.text = $"HP {pokemon.CurrentHP}/{pokemon.MaxHP}";

            if (hpFill != null)
            {
                hpFill.fillAmount = pokemon.MaxHP <= 0
                    ? 0f
                    : (float)pokemon.CurrentHP / pokemon.MaxHP;
            }

            if (attackText != null)
                attackText.text = $"攻击：{pokemon.Attack}";

            if (defenseText != null)
                defenseText.text = $"防御：{pokemon.Defense}";

            if (speedText != null)
                speedText.text = $"速度：{pokemon.Speed}";

            if (specialAttackText != null)
                specialAttackText.text = $"特攻：{pokemon.SpecialAttack}";

            if (specialDefenseText != null)
                specialDefenseText.text = $"特防：{pokemon.SpecialDefense}";
        }

        public void Clear()
        {
            CurrentPokemon = null;

            if (largeAvatar != null)
                largeAvatar.sprite = null;

            if (nameText != null)
                nameText.text = "未选择精灵";

            if (levelText != null)
                levelText.text = string.Empty;

            if (typeText != null)
                typeText.text = string.Empty;

            if (hpText != null)
                hpText.text = string.Empty;

            if (hpFill != null)
                hpFill.fillAmount = 0f;

            if (attackText != null)
                attackText.text = string.Empty;

            if (defenseText != null)
                defenseText.text = string.Empty;

            if (speedText != null)
                speedText.text = string.Empty;

            if (specialAttackText != null)
                specialAttackText.text = string.Empty;

            if (specialDefenseText != null)
                specialDefenseText.text = string.Empty;
        }
    }
}