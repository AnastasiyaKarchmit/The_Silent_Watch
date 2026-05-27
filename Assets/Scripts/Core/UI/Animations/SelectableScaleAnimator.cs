using DG.Tweening;
using UnityEngine;

namespace Core.UI.Animations
{
    public sealed class SelectableScaleAnimator
    {
        private readonly RectTransform _rectTransform;
        private readonly GameObject _arrows;
        private readonly SelectableAnimationSettings _settings;
        private readonly GameObject _owner;

        private Tween _scaleTween;

        public SelectableScaleAnimator(
            RectTransform rectTransform,
            GameObject arrows,
            SelectableAnimationSettings settings,
            GameObject owner)
        {
            _rectTransform = rectTransform;
            _arrows = arrows;
            _settings = settings;
            _owner = owner;
        }

        public void SetSelected()
        {
            SetArrowsActive(true);
            ScaleTo(_settings.SelectedScale, _settings.ReleaseDuration, _settings.ReleaseEase);
        }

        public void SetDeselected()
        {
            SetArrowsActive(false);
            ScaleTo(_settings.NormalScale, _settings.ReleaseDuration, _settings.ReleaseEase);
        }

        public void SetPressed()
        {
            SetArrowsActive(true);
            ScaleTo(_settings.PressedScale, _settings.PressDuration, _settings.PressEase);
        }

        public void PlaySubmitAnimation(bool returnToSelected)
        {
            KillTween();

            float targetScale = returnToSelected
                ? _settings.SelectedScale
                : _settings.NormalScale;

            _scaleTween = DOTween.Sequence()
                .Append(_rectTransform.DOScale(_settings.PressedScale, _settings.PressDuration)
                    .SetEase(_settings.PressEase))
                .Append(_rectTransform.DOScale(targetScale, _settings.ReleaseDuration)
                    .SetEase(_settings.ReleaseEase))
                .SetUpdate(true)
                .SetLink(_owner);
        }

        public void Reset()
        {
            KillTween();

            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one * _settings.NormalScale;

            SetArrowsActive(false);
        }

        private void ScaleTo(float scale, float duration, Ease ease)
        {
            if (_rectTransform == null)
                return;

            KillTween();

            _scaleTween = _rectTransform
                .DOScale(scale, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(_owner);
        }

        private void KillTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
                _scaleTween.Kill();
        }

        private void SetArrowsActive(bool active)
        {
            if (_arrows != null)
                _arrows.SetActive(active);
        }
    }
}