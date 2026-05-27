using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Animations
{
    public sealed class LoadingProgressIcon : MonoBehaviour
    {
        [Header("Images")]
        [SerializeField] private Image fillImage;

        [Header("Animation")]
        [SerializeField] private float fullPunchScale = 0.12f;
        [SerializeField] private float fullPunchDuration = 0.2f;

        private bool _wasFull;

        private void Awake()
        {
            if (fillImage == null)
                fillImage = GetComponent<Image>();
            
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;
                fillImage.fillClockwise = true;
                fillImage.fillAmount = 0f;
            }
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            _wasFull = false;
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (fillImage != null)
                fillImage.fillAmount = progress;

            bool isFull = progress >= 0.999f;

            if (isFull && !_wasFull)
            {
                transform.DOKill();
                transform.localScale = Vector3.one;

                transform
                    .DOPunchScale(Vector3.one * fullPunchScale, fullPunchDuration, 1, 0.5f)
                    .SetLink(gameObject);
            }

            _wasFull = isFull;
        }

        public void ResetIcon()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            if (fillImage != null)
                fillImage.fillAmount = 0f;

            _wasFull = false;
        }
    }
}