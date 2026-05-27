using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI.Animations
{
    public sealed class AnimatedButton : Button
    {
        [SerializeField] private SelectableAnimationSettings animationSettings = new();
        [SerializeField] private GameObject arrows;

        private SelectableScaleAnimator _animator;
        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();

            _rectTransform = transform as RectTransform;

            _animator = new SelectableScaleAnimator(
                _rectTransform,
                arrows,
                animationSettings,
                gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _animator?.Reset();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (_animator == null)
                return;

            if (!IsInteractable())
            {
                _animator.SetDeselected();
                return;
            }

            switch (state)
            {
                case SelectionState.Normal:
                    _animator.SetDeselected();
                    break;

                case SelectionState.Highlighted:
                case SelectionState.Selected:
                    _animator.SetSelected();
                    break;

                case SelectionState.Pressed:
                    _animator.SetPressed();
                    break;

                case SelectionState.Disabled:
                    _animator.SetDeselected();
                    break;
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            if (!IsInteractable())
                return;

            _animator.SetSelected();
            
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            
            if (!IsInteractable())
                return;
            
            _animator.SetDeselected();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);

            if (!IsInteractable())
                return;

            _animator?.PlaySubmitAnimation(IsSelected());
        }

        private bool IsSelected()
        {
            return EventSystem.current != null &&
                   EventSystem.current.currentSelectedGameObject == gameObject;
        }
    }
}