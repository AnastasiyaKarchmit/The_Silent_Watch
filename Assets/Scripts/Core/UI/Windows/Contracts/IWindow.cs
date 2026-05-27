using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.UI.Windows.Contracts
{
    public interface IWindow
    {
        RectTransform RootRectTransform { get; }

        bool IsActive { get; }
        bool IsInteractable { get; }

        event Action<IWindow> Destroyed;

        UniTask ShowAsync();
        UniTask HideAsync();
    }
}