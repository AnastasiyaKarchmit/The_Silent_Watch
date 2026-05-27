using System.Threading;
using Core.SceneManagement.Loading.Contracts;
using Core.UI.Windows.Components;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.SceneManagement.Loading.Runtime
{
    public class LoadingScreen : BaseWindow, ILoadingScreen
    {
        [Header("References")] 
        [SerializeField] private Slider progressSlider;
        
        public override void ShowInstantly()
        {
            base.ShowInstantly();
            SetProgress(0f);
            gameObject.SetActive(true);
        }

        public override void HideInstantly()
        {
            Destroy(gameObject);
        }

        public override async UniTask HideAsync()
        {
            await base.HideAsync();
            Destroy(gameObject);
        }

        public void SetProgress(float progress) => progressSlider.value = progress;
    }
}