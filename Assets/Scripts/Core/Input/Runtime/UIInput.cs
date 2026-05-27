using Core.Input.Contracts;
using UnityEngine;

namespace Core.Input.Runtime
{
    public sealed class UIInput : IUIInput
    {
        public IInputAction<Vector2> Navigate { get; }
        public IInputAction<bool> Submit { get; }
        public IInputAction<bool> Cancel { get; }
        public IInputAction<Vector2> Point { get; }
        public IInputAction<bool> Click { get; }
        public IInputAction<Vector2> ScrollWheel { get; }
        public IInputAction<bool> RightClick { get; }
        public IInputAction<bool> MiddleClick { get; }

        public UIInput(InputActions.UIActions actions)
        {
            Navigate = new UnityInputAction<Vector2>(
                actions.Navigate,
                () => actions.Navigate.ReadValue<Vector2>(),
                context => context.ReadValue<Vector2>());

            Submit = new UnityInputAction<bool>(
                actions.Submit,
                () => actions.Submit.WasPressedThisFrame(),
                context => context.ReadValueAsButton());

            Cancel = new UnityInputAction<bool>(
                actions.Cancel,
                () => actions.Cancel.WasPressedThisFrame(),
                context => context.ReadValueAsButton());

            Point = new UnityInputAction<Vector2>(
                actions.Point,
                () => actions.Point.ReadValue<Vector2>(),
                context => context.ReadValue<Vector2>());

            Click = new UnityInputAction<bool>(
                actions.Click,
                () => actions.Click.WasPressedThisFrame(),
                context => context.ReadValueAsButton());

            ScrollWheel = new UnityInputAction<Vector2>(
                actions.ScrollWheel,
                () => actions.ScrollWheel.ReadValue<Vector2>(),
                context => context.ReadValue<Vector2>());

            RightClick = new UnityInputAction<bool>(
                actions.RightClick,
                () => actions.RightClick.WasPressedThisFrame(),
                context => context.ReadValueAsButton());

            MiddleClick = new UnityInputAction<bool>(
                actions.MiddleClick,
                () => actions.MiddleClick.WasPressedThisFrame(),
                context => context.ReadValueAsButton());
        }

        public void SetActive(bool active)
        {
            Navigate.SetActive(active);
            Submit.SetActive(active);
            Cancel.SetActive(active);
            Point.SetActive(active);
            Click.SetActive(active);
            ScrollWheel.SetActive(active);
            RightClick.SetActive(active);
            MiddleClick.SetActive(active);
        }
    }
}