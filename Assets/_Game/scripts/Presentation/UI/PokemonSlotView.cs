using System;
using Pokemon.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokemon.Presentation.UI
{
    public class PokemonSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private const float LongPressDuration = 1f;

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
        private Action<MonsterRuntime, MonsterRuntime> _onReorderRequested;
        private float _pointerDownTime;
        private bool _pointerHeld;
        private bool _isDragging;
        private bool _suppressClick;
        private bool _selected;

        public MonsterRuntime Pokemon => _pokemon;

        public void Bind(
            MonsterRuntime pokemon,
            bool isActive,
            bool isFainted,
            Action<MonsterRuntime> onClicked,
            bool allowFaintedSelection = false,
            Action<MonsterRuntime, MonsterRuntime> onReorderRequested = null)
        {
            _pokemon = pokemon;
            _onClicked = onClicked;
            _onReorderRequested = onReorderRequested;
            _selected = isActive;
            ResetPointerState();

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
                button.interactable = allowFaintedSelection || !isFainted;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (!_suppressClick)
                        _onClicked?.Invoke(_pokemon);
                });
            }
        }

        public void Clear()
        {
            _pokemon = null;
            _onReorderRequested = null;
            _selected = false;
            ResetPointerState();

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

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selectedFrame != null)
                selectedFrame.SetActive(selected || _isDragging);
        }

        /// <summary>
        /// 记录指针按下时间，开始等待长按拖拽。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            _suppressClick = false;
            if (_pokemon == null || _onReorderRequested == null)
                return;

            _pointerHeld = true;
            _pointerDownTime = Time.unscaledTime;
        }

        /// <summary>
        /// 在指针松开时结束拖拽，并向目标槽位发起重排请求。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            _pointerHeld = false;
            if (!_isDragging)
                return;

            PokemonSlotView targetSlot = GetDropTarget(eventData);
            EndDragVisual();
            if (targetSlot != null && targetSlot != this && targetSlot.Pokemon != null)
                _onReorderRequested?.Invoke(_pokemon, targetSlot.Pokemon);
        }

        /// <summary>
        /// 在拖动过程中检查是否已经满足一秒长按条件。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
            TryBeginDrag();
        }

        /// <summary>
        /// 持续检查静止长按，使按住一秒后再移动也能进入拖拽状态。
        /// </summary>
        private void Update()
        {
            TryBeginDrag();
        }

        /// <summary>
        /// 组件禁用时清理未结束的长按拖拽状态。
        /// </summary>
        private void OnDisable()
        {
            ResetPointerState();
        }

        /// <summary>
        /// 达到长按时间后进入拖拽状态并显示槽位高亮。
        /// </summary>
        private void TryBeginDrag()
        {
            if (!_pointerHeld || _isDragging ||
                Time.unscaledTime - _pointerDownTime < LongPressDuration)
                return;

            _isDragging = true;
            _suppressClick = true;
            if (selectedFrame != null)
                selectedFrame.SetActive(true);
        }

        /// <summary>
        /// 获取指针松开位置对应的精灵槽位。
        /// </summary>
        /// <param name="eventData">当前指针事件数据。</param>
        private static PokemonSlotView GetDropTarget(PointerEventData eventData)
        {
            GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
            return targetObject != null
                ? targetObject.GetComponentInParent<PokemonSlotView>()
                : null;
        }

        /// <summary>
        /// 结束当前拖拽并恢复槽位原有选择状态。
        /// </summary>
        private void EndDragVisual()
        {
            _isDragging = false;
            if (selectedFrame != null)
                selectedFrame.SetActive(_selected);
        }

        /// <summary>
        /// 重置槽位的指针和拖拽状态。
        /// </summary>
        private void ResetPointerState()
        {
            _pointerHeld = false;
            _isDragging = false;
            _suppressClick = false;
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
