using System;
using DG.Tweening;
using UnityEngine;

namespace Core.UI.Animations
{
    [Serializable]
    public sealed class SelectableAnimationSettings
    {
        [Header("Scale")]
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float selectedScale = 1.05f;
        [SerializeField] private float normalScale = 1f;

        [Header("Timing")]
        [SerializeField] private float pressDuration = 0.08f;
        [SerializeField] private float releaseDuration = 0.12f;

        [Header("Ease")]
        [SerializeField] private Ease pressEase = Ease.OutQuad;
        [SerializeField] private Ease releaseEase = Ease.OutBack;

        public float PressedScale => pressedScale;
        public float SelectedScale => selectedScale;
        public float NormalScale => normalScale;

        public float PressDuration => pressDuration;
        public float ReleaseDuration => releaseDuration;

        public Ease PressEase => pressEase;
        public Ease ReleaseEase => releaseEase;
    }
}