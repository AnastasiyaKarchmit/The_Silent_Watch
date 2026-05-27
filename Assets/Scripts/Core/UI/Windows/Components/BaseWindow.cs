using System;
using System.Threading;
using Core.UI.Windows.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.UI.Windows.Components
{
    public abstract class BaseWindow : MonoBehaviour, IWindow
    {
        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        private const float MaxAnimationDeltaTime = 1f / 20f;
        
        private CancellationTokenSource _animationCts;
        private bool _isDestroyed;
        
        public RectTransform RootRectTransform => rootRectTransform;
        public bool IsActive { get; private set; }
        public bool IsInteractable => canvasGroup == null || canvasGroup.interactable;

        public event Action<IWindow> Destroyed;

        public virtual async UniTask ShowAsync()
        {
            if (!IsAlive())
                return;

            CancellationToken token = RestartAnimation();

            try
            {
                gameObject.SetActive(true);

                await AnimateAlphaAsync(0f, 1f, fadeInDuration, token);

                if (!IsAlive())
                    return;

                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                IsActive = true;
            }
            catch (OperationCanceledException)
            {
                // Window was hidden, destroyed, or another animation started.
            }
        }

        public virtual async UniTask HideAsync()
        {
            if (!IsAlive())
                return;

            CancellationToken token = RestartAnimation();

            try
            {
                IsActive = false;

                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                float startAlpha = canvasGroup.alpha;

                if (startAlpha <= 0.001f)
                {
                    if (IsAlive())
                        gameObject.SetActive(false);

                    return;
                }

                await AnimateAlphaAsync(startAlpha, 0f, fadeOutDuration, token);

                if (IsAlive())
                    gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                // Window was shown again, destroyed, or another animation started.
            }
        }

        public virtual void ShowInstantly()
        {
            if (!IsAlive())
                return;

            CancelCurrentAnimation();

            gameObject.SetActive(true);

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            IsActive = true;
        }

        public virtual void HideInstantly()
        {
            if (!IsAlive())
                return;

            CancelCurrentAnimation();

            IsActive = false;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            _isDestroyed = true;

            CancelCurrentAnimation();

            _animationCts?.Dispose();
            _animationCts = null;

            Destroyed?.Invoke(this);
        }

        private async UniTask AnimateAlphaAsync(
            float from,
            float to,
            float duration,
            CancellationToken token)
        {
            if (!IsAlive())
                return;

            canvasGroup.alpha = from;

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();

                if (!IsAlive())
                    return;

                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxAnimationDeltaTime);

                float t = Mathf.Clamp01(elapsed / duration);
                float smoothed = SmoothStep(t);

                canvasGroup.alpha = Mathf.Lerp(from, to, smoothed);

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
            }

            if (IsAlive())
                canvasGroup.alpha = to;
        }

        private CancellationToken RestartAnimation()
        {
            CancelCurrentAnimation();

            _animationCts?.Dispose();

            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

            return _animationCts.Token;
        }

        private void CancelCurrentAnimation()
        {
            if (_animationCts == null)
                return;

            if (!_animationCts.IsCancellationRequested)
                _animationCts.Cancel();
        }

        private bool IsAlive()
        {
            return !_isDestroyed && this != null && gameObject != null && canvasGroup != null;
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}