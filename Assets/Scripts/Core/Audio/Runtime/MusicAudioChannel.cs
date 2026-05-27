using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio.Runtime
{
    /// <summary>
    /// Handles one dedicated music AudioSource.
    /// Responsible only for music playback, fading, stopping, and music volume.
    /// </summary>
    internal sealed class MusicAudioChannel : IDisposable
    {
        private const float MaxFadeDeltaTime = 1f / 20f;

        private readonly AudioSource _source;
        private readonly float _fadeDuration;

        private CancellationTokenSource _fadeCts;
        private bool _isDisposed;

        private float _musicVolume;
        private float _masterVolume;

        private float TargetVolume => _musicVolume * _masterVolume;

        public MusicAudioChannel(
            Transform parent,
            AudioMixerGroup mixerGroup,
            float fadeDuration,
            float initialMusicVolume,
            float initialMasterVolume)
        {
            _fadeDuration = Mathf.Max(0f, fadeDuration);

            _musicVolume = Mathf.Clamp01(initialMusicVolume);
            _masterVolume = Mathf.Clamp01(initialMasterVolume);

            var gameObject = new GameObject("Music Audio");
            gameObject.transform.SetParent(parent);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.volume = TargetVolume;
            _source.outputAudioMixerGroup = mixerGroup;
        }

        public async UniTask PlayAsync(
            AudioClip clip,
            bool restartIfSame,
            CancellationToken token)
        {
            if (_isDisposed || clip == null)
                return;

            var fadeToken = RestartFade(token);

            try
            {
                if (_source.clip == clip && _source.isPlaying && !restartIfSame)
                {
                    await FadeToAsync(TargetVolume, _fadeDuration, fadeToken);
                    return;
                }

                if (_source.isPlaying)
                    await FadeToAsync(0f, _fadeDuration, fadeToken);

                _source.clip = clip;
                _source.loop = true;
                _source.volume = 0f;
                _source.Play();

                await FadeToAsync(TargetVolume, _fadeDuration, fadeToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async UniTask StopAsync(CancellationToken token)
        {
            if (_source == null)
                return;
            
            if (_isDisposed || !_source.isPlaying)
                return;

            var fadeToken = RestartFade(token);

            try
            {
                await FadeToAsync(0f, _fadeDuration, fadeToken);

                _source.Stop();
                _source.clip = null;
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void SetMusicVolume(float volume)
        {
            if (_isDisposed)
                return;

            _musicVolume = Mathf.Clamp01(volume);
            ApplyCurrentVolumeInstantly();
        }

        public void SetMasterVolume(float volume)
        {
            if (_isDisposed)
                return;

            _masterVolume = Mathf.Clamp01(volume);
            ApplyCurrentVolumeInstantly();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            CancelFade();

            _fadeCts?.Dispose();
            _fadeCts = null;
        }

        private void ApplyCurrentVolumeInstantly()
        {
            CancelFade();

            if (_source != null)
                _source.volume = TargetVolume;
        }

        private CancellationToken RestartFade(CancellationToken externalToken)
        {
            CancelFade();

            _fadeCts?.Dispose();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            return _fadeCts.Token;
        }

        private void CancelFade()
        {
            if (_fadeCts == null)
                return;

            if (!_fadeCts.IsCancellationRequested)
                _fadeCts.Cancel();
        }

        private async UniTask FadeToAsync(
            float targetVolume,
            float duration,
            CancellationToken token)
        {
            if (_source == null)
                return;

            targetVolume = Mathf.Clamp01(targetVolume);

            if (duration <= 0f)
            {
                _source.volume = targetVolume;
                return;
            }

            float startVolume = _source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeDeltaTime);

                float t = Mathf.Clamp01(elapsed / duration);
                float smoothed = t * t * (3f - 2f * t);

                _source.volume = Mathf.Lerp(startVolume, targetVolume, smoothed);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            _source.volume = targetVolume;
        }
    }
}