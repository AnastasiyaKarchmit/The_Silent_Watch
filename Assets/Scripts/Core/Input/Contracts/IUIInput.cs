using UnityEngine;

namespace Core.Input.Contracts
{
    public interface IUIInput
    {
        IInputAction<Vector2> Navigate { get; }
        IInputAction<bool> Submit { get; }
        IInputAction<bool> Cancel { get; }
        IInputAction<Vector2> Point { get; }
        IInputAction<bool> Click { get; }
        IInputAction<Vector2> ScrollWheel { get; }

        void SetActive(bool active);
    }
}