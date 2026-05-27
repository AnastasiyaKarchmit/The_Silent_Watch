using System;
using Core.Input.Runtime;
using R3;
using UnityEngine;

namespace Core.Input.Contracts
{
    public interface IInputService
    {
        InputMode CurrentMode { get; }

        IGameplayInput Gameplay { get; }
        IUIInput UI { get; }

        Observable<InputMode> ModeChanged { get; }

        void SetMode(InputMode mode);
        void SetSelectedGameObject(GameObject selectedObject);
        void ClearSelectedGameObject();
    }
}