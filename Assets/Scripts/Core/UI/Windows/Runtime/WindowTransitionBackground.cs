using Core.UI.Windows.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Windows.Runtime
{
    public class WindowTransitionBackground : IWindowTransitionBackground
    {
        private CanvasGroup _overlayCanvasGroup;
        private bool _isInitialized;
        private const int OverlaySortOrder = -10;
        
        public void Create()
        {
            EnsureOverlayCreated();
            _overlayCanvasGroup.gameObject.SetActive(true);
        }
        
        private void EnsureOverlayCreated()
        {
            if (_isInitialized)
                return;

            var overlayObject = new GameObject("TransitionOverlay");

            var canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;

            overlayObject.AddComponent<CanvasScaler>();

            _overlayCanvasGroup = overlayObject.AddComponent<CanvasGroup>();
            _overlayCanvasGroup.alpha = 1f;
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

        public void Dispose() { }
    }
}