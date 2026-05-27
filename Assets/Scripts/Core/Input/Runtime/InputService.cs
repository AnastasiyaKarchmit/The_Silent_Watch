using System;
using Core.Input.Contracts;
using R3;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Core.Input.Runtime
{
    public enum InputMode
    {
        Disabled,
        UIOnly,
        Gameplay
    }
    
    public sealed class InputService : IInputService, IInitializable, IDisposable
    {
        private readonly EventSystem _eventSystem;
        private readonly InputSystemUIInputModule _uiInputModule;

        private InputActions _inputActions;
        private bool _isDisposed;
        
        private readonly ReactiveCommand<InputMode> _modeChangedCommand = new();

        public InputMode CurrentMode { get; private set; } = InputMode.Disabled;

        public IGameplayInput Gameplay { get; private set; }
        public IUIInput UI { get; private set; }
        
        public Observable<InputMode> ModeChanged => _modeChangedCommand;

        public InputService(
            EventSystem eventSystem,
            InputSystemUIInputModule uiInputModule)
        {
            _eventSystem = eventSystem;
            _uiInputModule = uiInputModule;
        }

        public void Initialize()
        {
            _inputActions = new InputActions();

            Gameplay = new GameplayInput(_inputActions.Player);
            UI = new UIInput(_inputActions.UI);

            if (_uiInputModule != null)
                _uiInputModule.actionsAsset = _inputActions.asset;

            SetMode(InputMode.UIOnly);
        }

        public void SetMode(InputMode mode)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            if (CurrentMode == mode)
                return;

            CurrentMode = mode;

            switch (mode)
            {
                case InputMode.Disabled:
                    DisableGameplay();
                    DisableUI();
                    break;

                case InputMode.UIOnly:
                    DisableGameplay();
                    EnableUI();
                    break;

                case InputMode.Gameplay:
                    EnableGameplay();
                    EnableUI();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            _modeChangedCommand.Execute(mode);
        }

        public void SetSelectedGameObject(GameObject selectedObject)
        {
            if (_eventSystem == null)
                return;

            _eventSystem.SetSelectedGameObject(selectedObject);
        }

        public void ClearSelectedGameObject()
        {
            if (_eventSystem == null)
                return;

            _eventSystem.SetSelectedGameObject(null);
        }

        private void EnableGameplay()
        {
            _inputActions.Player.Enable();
            Gameplay.SetActive(true);
        }

        private void DisableGameplay()
        {
            _inputActions.Player.Disable();
            Gameplay.SetActive(false);
        }

        private void EnableUI()
        {
            _inputActions.UI.Enable();
            UI.SetActive(true);

            if (_uiInputModule != null)
                _uiInputModule.enabled = true;
        }

        private void DisableUI()
        {
            _inputActions.UI.Disable();
            UI.SetActive(false);

            if (_uiInputModule != null)
                _uiInputModule.enabled = false;
        }

        private void EnsureInitialized()
        {
            if (_inputActions == null)
            {
                Initialize();
                //throw new InvalidOperationException($"{nameof(InputService)} is not initialized.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(InputService));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_inputActions == null)
                return;

            DisableGameplay();
            DisableUI();
            
            _modeChangedCommand.Dispose();

            _inputActions.Dispose();
            _inputActions = null;
        }
    }
}