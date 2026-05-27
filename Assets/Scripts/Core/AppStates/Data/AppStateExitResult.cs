using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.AppStates.Data
{
    public readonly struct AppStateExitResult
    {
        public AppStateId? NextState { get; }
        public object Payload { get; }
        public AppStateSwitchOptions SwitchOptions { get; }

        public bool HasNextState => NextState.HasValue;

        private AppStateExitResult(
            AppStateId? nextState,
            object payload,
            AppStateSwitchOptions switchOptions)
        {
            NextState = nextState;
            Payload = payload;
            SwitchOptions = switchOptions;
        }

        public static AppStateExitResult None => new(null, null, null);

        public static AppStateExitResult SwitchTo(
            AppStateId stateId,
            object payload = null,
            AppStateSwitchOptions switchOptions = null)
        {
            return new AppStateExitResult(stateId, payload, switchOptions);
        }
    }
    public sealed class AppStateSwitchOptions
    {
        public IProgress<float> Progress { get; }
        public Func<CancellationToken, UniTask> OnBeforeLoadAsync { get; }
        public Func<CancellationToken, UniTask> OnAfterEnterAsync { get; }

        public AppStateSwitchOptions(
            IProgress<float> progress = null,
            Func<CancellationToken, UniTask> onBeforeLoadAsync = null,
            Func<CancellationToken, UniTask> onAfterEnterAsync = null)
        {
            Progress = progress;
            OnBeforeLoadAsync = onBeforeLoadAsync;
            OnAfterEnterAsync = onAfterEnterAsync;
        }
    }
    
}