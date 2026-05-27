using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core.Enums;

namespace Features.Bootstrap.Startup.Tasks
{
    public sealed class InitializeTweeningTask : IStartupTask
    {
        public string Description => "Initializing animation system";

        public UniTask ExecuteAsync(CancellationToken token)
        {
            DOTween.Init().SetCapacity(240, 30);
            DOTween.safeModeLogBehaviour = SafeModeLogBehaviour.None;
            DOTween.defaultAutoKill = true;
            DOTween.defaultRecyclable = true;
            DOTween.useSmoothDeltaTime = true;

            return UniTask.CompletedTask;
        }
    }
}