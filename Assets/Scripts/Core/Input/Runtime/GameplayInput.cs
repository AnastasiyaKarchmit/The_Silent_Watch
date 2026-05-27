using Core.Input.Contracts;
using UnityEngine;

namespace Core.Input.Runtime
{
    public sealed class GameplayInput : IGameplayInput
    {
        public IInputAction<Vector2> Move { get; }
        public IInputAction<Vector2> Look { get; }
        public IInputAction<bool> Attack { get; }
        public IInputAction<bool> Interact { get; }
        public IInputAction<bool> Sprint { get; }
        public IInputAction<bool> Jump { get; }
        public IInputAction<bool> Crouch { get; }

        public GameplayInput(InputActions.PlayerActions actions)
        {
            Move = new UnityInputAction<Vector2>(
                actions.Move,
                () => actions.Move.ReadValue<Vector2>(),
                context => context.ReadValue<Vector2>());

            Look = new UnityInputAction<Vector2>(
                actions.Look,
                () => actions.Look.ReadValue<Vector2>(),
                context => context.ReadValue<Vector2>());

            Attack = new UnityInputAction<bool>(
                actions.Attack,
                () => actions.Attack.IsPressed(),
                context => context.ReadValueAsButton());

            Interact = new UnityInputAction<bool>(
                actions.Interact,
                () => actions.Interact.WasPressedThisFrame(),
                context => context.ReadValueAsButton());

            Sprint = new UnityInputAction<bool>(
                actions.Sprint,
                () => actions.Sprint.WasPressedThisFrame(),
                context => context.ReadValueAsButton());
            
            Jump = new UnityInputAction<bool>(
                actions.Jump,
                () => actions.Jump.WasPressedThisFrame(),
                context => context.ReadValueAsButton());
            
            Crouch = new UnityInputAction<bool>(
                actions.Crouch,
                () => actions.Crouch.WasPressedThisFrame(),
                context => context.ReadValueAsButton());
        }

        public void SetActive(bool active)
        {
            Move.SetActive(active);
            Look.SetActive(active);
            Attack.SetActive(active);
            Interact.SetActive(active);
            Sprint.SetActive(active);
        }
    }
}