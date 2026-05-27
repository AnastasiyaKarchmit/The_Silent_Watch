using UnityEngine;

namespace Core.Input.Contracts
{
    public interface IGameplayInput
    {
        IInputAction<Vector2> Move { get; }
        IInputAction<Vector2> Look { get; }
        IInputAction<bool> Attack { get; }
        IInputAction<bool> Interact { get; }
        IInputAction<bool> Sprint { get; }
        IInputAction<bool> Jump { get; }
        IInputAction<bool> Crouch { get; }
        void SetActive(bool active);
    }
}