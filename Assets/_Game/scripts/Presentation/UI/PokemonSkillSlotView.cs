using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pokemon.Presentation.UI
{
    public class PokemonSkillSlotView : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private RectTransform dragLayer;

        private SkillData _skill;
        private Action _onClicked;
        private Action<SkillData> _onDropped;
        private bool _canDrag;
        private TMP_Text _dragLabel;
        public PokemonSkillReplacementPanel Owner { get; private set; }

        /// <summary>绑定技能槽的文字和交互。</summary>
        /// <param name="skill">槽位中的技能，空槽传入 null。</param>
        /// <param name="text">显示的技能信息。</param>
        /// <param name="owner">所属的技能替换界面。</param>
        /// <param name="onClicked">点击槽位时执行的操作。</param>
        /// <param name="onDropped">候选技能拖入时执行的操作。</param>
        /// <param name="canDrag">是否允许拖出技能。</param>
        /// <param name="selected">是否显示选择高亮。</param>
        public void Bind(SkillData skill, string text, PokemonSkillReplacementPanel owner,
            Action onClicked, Action<SkillData> onDropped, bool canDrag, bool selected)
        {
            _skill = skill;
            label.text = text;
            Owner = owner;
            _onClicked = onClicked;
            _onDropped = onDropped;
            _canDrag = canDrag;
            selectedFrame.SetActive(selected);
        }

        /// <summary>点击槽位以选择候选技能或替换目标。</summary>
        /// <param name="eventData">当前指针事件。</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
                _onClicked?.Invoke();
        }

        /// <summary>开始拖动候选技能并创建跟随指针的文字。</summary>
        /// <param name="eventData">当前指针事件。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canDrag || eventData.button != PointerEventData.InputButton.Left)
                return;
            _dragLabel = Instantiate(label, dragLayer);
            _dragLabel.raycastTarget = false;
            _dragLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _dragLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _dragLabel.rectTransform.sizeDelta = label.rectTransform.rect.size;
            _dragLabel.rectTransform.SetAsLastSibling();
            OnDrag(eventData);
        }

        /// <summary>将拖动文字移动到指针位置。</summary>
        /// <param name="eventData">当前指针事件。</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (_dragLabel == null)
                return;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(dragLayer,
                eventData.position, eventData.pressEventCamera, out Vector3 position);
            _dragLabel.rectTransform.position = position;
        }

        /// <summary>接收同一界面中拖入的候选技能。</summary>
        /// <param name="eventData">当前指针事件。</param>
        public void OnDrop(PointerEventData eventData)
        {
            PokemonSkillSlotView source = eventData.pointerDrag.GetComponent<PokemonSkillSlotView>();
            if (source != null && source.Owner == Owner && source._dragLabel != null)
                _onDropped?.Invoke(source._skill);
        }

        /// <summary>拖动结束时清除临时文字。</summary>
        /// <param name="eventData">当前指针事件。</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            ClearDrag();
        }

        /// <summary>隐藏槽位时清除拖动文字。</summary>
        private void OnDisable()
        {
            ClearDrag();
        }

        /// <summary>销毁当前技能的拖动文字。</summary>
        private void ClearDrag()
        {
            if (_dragLabel != null)
                Destroy(_dragLabel.gameObject);
            _dragLabel = null;
        }
    }
}
