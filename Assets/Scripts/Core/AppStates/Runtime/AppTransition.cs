using System.Threading;
using Core.AppStates.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core.AppStates.Runtime
{
    public sealed class AppTransition : IAppTransition
    {
        private const float FadeInDuration = 0f;
        private const float FadeOutDuration = 0.3f;
        private const int OverlaySortOrder = -10;
        private const float MaxAnimationDeltaTime = 1f / 20f;

        private CanvasGroup _overlayCanvasGroup;
        private bool _isInitialized;
        
        public async UniTask ShowAsync(CancellationToken token = default)
        {
            EnsureOverlayCreated();
            _overlayCanvasGroup.gameObject.SetActive(true);
            await AnimateAlphaAsync(0f, 1f, FadeInDuration);
        }

        public async UniTask HideAsync(CancellationToken token = default)
        {
            if (!_isInitialized)
                return;

            float startAlpha = _overlayCanvasGroup.alpha;
            if (startAlpha <= 0.001f)
            {
                _overlayCanvasGroup.gameObject.SetActive(false);
                return;
            }

            _overlayCanvasGroup.gameObject.SetActive(true);
            await AnimateAlphaAsync(startAlpha, 0f, FadeOutDuration);
            _overlayCanvasGroup.gameObject.SetActive(false);
        }
        
        private void EnsureOverlayCreated()
        {
            if (_isInitialized)
                return;

            var overlayObject = new GameObject("TransitionOverlay");
            Object.DontDestroyOnLoad(overlayObject);

            var canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;

            overlayObject.AddComponent<CanvasScaler>();

            _overlayCanvasGroup = overlayObject.AddComponent<CanvasGroup>();
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.blocksRaycasts = true;
            _overlayCanvasGroup.interactable = false;

            var imageObject = new GameObject("Background");
            imageObject.transform.SetParent(overlayObject.transform, false);

            var image = imageObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            overlayObject.SetActive(false);
            _isInitialized = true;
        }

        private async UniTask AnimateAlphaAsync(float from, float to, float duration)
        {
            _overlayCanvasGroup.alpha = from;
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxAnimationDeltaTime);
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothed = SmoothStep(t);
                _overlayCanvasGroup.alpha = Mathf.Lerp(from, to, smoothed);
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }

            _overlayCanvasGroup.alpha = to;
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);
    }
}