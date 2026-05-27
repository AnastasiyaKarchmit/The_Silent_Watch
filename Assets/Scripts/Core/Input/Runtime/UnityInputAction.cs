using System;
using Core.Input.Contracts;
using R3;
using UnityEngine.InputSystem;

namespace Core.Input.Runtime
{
    public sealed class UnityInputAction<T> : IInputAction<T>
    {
        private readonly InputAction _action;
        private readonly Func<T> _readCurrentValue;
        private readonly Func<InputAction.CallbackContext, T> _readEventValue;

        private readonly Observable<T> _started;
        private readonly Observable<T> _performed;
        private readonly Observable<T> _canceled;

        public bool Enabled { get; private set; }

        public T Value => Enabled ? _readCurrentValue() : default;

        public Observable<T> Started => _started;
        public Observable<T> Performed => _performed;
        public Observable<T> Canceled => _canceled;

        public UnityInputAction(
            InputAction action,
            Func<T> readCurrentValue,
            Func<InputAction.CallbackContext, T> readEventValue)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _readCurrentValue = readCurrentValue ?? throw new ArgumentNullException(nameof(readCurrentValue));
            _readEventValue = readEventValue ?? throw new ArgumentNullException(nameof(readEventValue));

            _started = CreateStartedObservable();
            _performed = CreatePerformedObservable();
            _canceled = CreateCanceledObservable();
        }

        public void SetActive(bool active)
        {
            Enabled = active;
        }

        private Observable<T> CreateStartedObservable()
        {
            return Observable
                .FromEvent<InputAction.CallbackContext>(
                    handler => _action.started += handler,
                    handler => _action.started -= handler)
                .Where(_ => Enabled)
                .Select(_readEventValue);
        }

        private Observable<T> CreatePerformedObservable()
        {
            return Observable
                .FromEvent<InputAction.CallbackContext>(
                    handler => _action.performed += handler,
                    handler => _action.performed -= handler)
                .Where(_ => Enabled)
                .Select(_readEventValue);
        }

        private Observable<T> CreateCanceledObservable()
        {
            return Observable
                .FromEvent<InputAction.CallbackContext>(
                    handler => _action.canceled += handler,
                    handler => _action.canceled -= handler)
                .Where(_ => Enabled)
                .Select(_readEventValue);
        }
    }
}