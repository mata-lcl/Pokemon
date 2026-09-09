using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokemon.Presentation
{
    public sealed class SkillButtonView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Content")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text ppText;
        [SerializeField] private Image typeIcon;
        [SerializeField] private Image typeBackground;
        [SerializeField] private TMP_Text typeText;

        [Header("State")]
        [SerializeField] private GameObject focusFrame;
        [SerializeField] private GameObject disabledOverlay;
        [SerializeField] private Button button;

        private SkillData _skill;
        private int _currentPP;
        private int _skillIndex;
        private bool _inputInteractable = true;
        private Action<int> _onClicked;
        private Action<SkillData, int> _onPreviewRequested;
        private Action _onPreviewEnded;

        /// <summary>
        /// 绑定按钮点击事件并初始化悬停状态。
        /// </summary>
        private void Awake()
        {
            button.onClick.AddListener(HandleClicked);
            SetFocusVisible(false);
        }

        /// <summary>
        /// 组件销毁时移除按钮点击事件。
        /// </summary>
        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>
        /// 将一个技能及其当前 PP 绑定到按钮视图。
        /// </summary>
        /// <param name="skill">需要显示和选择的技能。</param>
        /// <param name="currentPP">技能当前剩余 PP。</param>
        /// <param name="skillIndex">技能在当前角色技能列表中的下标。</param>
        /// <param name="typeVisual">技能属性对应的视觉配置。</param>
        /// <param name="onClicked">按钮点击后的技能下标回调。</param>
        /// <param name="onPreviewRequested">鼠标悬停或选中时的技能预览回调。</param>
        /// <param name="onPreviewEnded">鼠标离开或取消选中时的预览关闭回调。</param>
        public void Bind(
            SkillData skill,
            int currentPP,
            int skillIndex,
            PokemonTypeVisual typeVisual,
            Action<int> onClicked,
            Action<SkillData, int> onPreviewRequested,
            Action onPreviewEnded)
        {
            _skill = skill;
            _currentPP = currentPP;
            _skillIndex = skillIndex;
            _onClicked = onClicked;
            _onPreviewRequested = onPreviewRequested;
            _onPreviewEnded = onPreviewEnded;

            skillIcon.sprite = skill.Icon;
            skillIcon.gameObject.SetActive(skill.Icon != null);
            skillNameText.text = skill.DisplayName;
            ppText.text = $"{currentPP}/{skill.MaxPP}";
            disabledOverlay.SetActive(currentPP <= 0);

            ApplyTypeVisual(skill.Type, typeVisual);
            SetInteractable(_inputInteractable);
            SetFocusVisible(false);
        }

        /// <summary>
        /// 设置技能按钮是否允许响应点击，PP 为零时始终不可点击。
        /// </summary>
        /// <param name="interactable">当前战斗界面是否允许输入。</param>
        public void SetInteractable(bool interactable)
        {
            _inputInteractable = interactable;
            button.interactable = interactable && _skill != null && _currentPP > 0;
            if (!interactable)
                EndPreview();
        }

        /// <summary>
        /// 鼠标进入按钮时显示选中框并请求技能预览。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            BeginPreview();
        }

        /// <summary>
        /// 鼠标离开按钮时隐藏选中框并结束技能预览。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            EndPreview();
        }

        /// <summary>
        /// 键盘或手柄选中按钮时显示选中框并请求技能预览。
        /// </summary>
        /// <param name="eventData">当前选中事件数据。</param>
        public void OnSelect(BaseEventData eventData)
        {
            BeginPreview();
        }

        /// <summary>
        /// 键盘或手柄取消选中时隐藏选中框并结束技能预览。
        /// </summary>
        /// <param name="eventData">当前取消选中事件数据。</param>
        public void OnDeselect(BaseEventData eventData)
        {
            EndPreview();
        }

        /// <summary>
        /// 按钮隐藏时结束可能仍在显示的技能预览。
        /// </summary>
        private void OnDisable()
        {
            EndPreview();
        }

        /// <summary>
        /// 将按钮点击转换为当前技能下标回调。
        /// </summary>
        private void HandleClicked()
        {
            _onClicked?.Invoke(_skillIndex);
        }

        /// <summary>
        /// 开始显示当前绑定技能的焦点状态和详情预览。
        /// </summary>
        private void BeginPreview()
        {
            if (!_inputInteractable || _skill == null) return;

            SetFocusVisible(true);
            _onPreviewRequested?.Invoke(_skill, _currentPP);
        }

        /// <summary>
        /// 结束当前按钮的焦点状态和详情预览。
        /// </summary>
        private void EndPreview()
        {
            SetFocusVisible(false);
            _onPreviewEnded?.Invoke();
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
        /// 设置鼠标悬停或键盘选中使用的焦点框显隐状态。
        /// </summary>
        /// <param name="visible">是否显示焦点框。</param>
        private void SetFocusVisible(bool visible)
        {
            focusFrame.SetActive(visible);
        }
    }
}
