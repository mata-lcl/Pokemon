using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public sealed class SkillTooltipView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private Image typeIcon;
        [SerializeField] private Image typeBackground;
        [SerializeField] private TMP_Text typeText;

        [Header("Details")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text ppText;
        [SerializeField] private TMP_Text priorityText;

        /// <summary>
        /// 显示指定技能的描述、属性和战斗参数。
        /// </summary>
        /// <param name="skill">需要显示的技能资源。</param>
        /// <param name="currentPP">该技能当前剩余的 PP。</param>
        /// <param name="typeVisual">技能属性对应的视觉配置。</param>
        public void Show(
            SkillData skill,
            int currentPP,
            PokemonTypeVisual typeVisual)
        {
            gameObject.SetActive(true);

            skillIcon.sprite = skill.Icon;
            skillIcon.gameObject.SetActive(skill.Icon != null);
            skillNameText.text = skill.DisplayName;
            descriptionText.text = string.IsNullOrWhiteSpace(skill.Description)
                ? "暂无技能描述。"
                : skill.Description;
            categoryText.text = $"分类：{GetCategoryDisplayName(skill.Category)}";
            powerText.text = skill.Category == SkillCategory.Status
                ? "威力：--"
                : $"威力：{skill.Power}";
            accuracyText.text =
                $"命中：{Mathf.RoundToInt(skill.Accuracy * 100f)}%";
            ppText.text = $"PP：{currentPP}/{skill.MaxPP}";
            priorityText.text = $"优先级：{skill.Priority}";

            ApplyTypeVisual(skill.Type, typeVisual);
        }

        /// <summary>
        /// 隐藏当前技能描述面板。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新技能属性徽章的文字、图标和颜色。
        /// </summary>
        /// <param name="type">技能配置中的属性。</param>
        /// <param name="typeVisual">该属性对应的视觉配置。</param>
        private void ApplyTypeVisual(
            PokemonType type,
            PokemonTypeVisual typeVisual)
        {
            bool hasVisual = typeVisual != null;
            typeIcon.sprite = hasVisual ? typeVisual.Icon : null;
            typeIcon.gameObject.SetActive(hasVisual && typeVisual.Icon != null);
            typeText.text = hasVisual
                ? typeVisual.DisplayName
                : type.ToString();
            typeBackground.color = hasVisual ? typeVisual.Color : Color.white;
        }

        /// <summary>
        /// 将技能分类转换成战斗界面使用的中文名称。
        /// </summary>
        /// <param name="category">技能的物理、特殊或变化分类。</param>
        /// <returns>分类对应的中文名称。</returns>
        private static string GetCategoryDisplayName(SkillCategory category)
        {
            return category switch
            {
                SkillCategory.Physical => "物理",
                SkillCategory.Special => "特殊",
                SkillCategory.Status => "变化",
                _ => category.ToString()
            };
        }
    }
}
