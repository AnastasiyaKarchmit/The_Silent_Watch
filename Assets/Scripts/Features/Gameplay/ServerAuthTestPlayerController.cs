using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Gameplay
{
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class ServerAuthTestPlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Input")]
        [SerializeField] private float inputSendRate = 20f;

        private Vector2 _serverInput;
        private Vector2 _lastSentInput;
        private float _nextInputSendTime;

        public override void OnStartClient()
        {
            base.OnStartClient();

            Debug.Log(
                $"[Player] OnStartClient | netId={netId} | " +
                $"isLocalPlayer={isLocalPlayer} | isOwned={isOwned}");
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            Debug.Log($"[Player] OnStartLocalPlayer | netId={netId}");
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            Vector2 input = ReadMoveInput();

            bool inputChanged = input != _lastSentInput;
            bool sendTick = Time.time >= _nextInputSendTime;

            if (!inputChanged && !sendTick)
                return;

            _lastSentInput = input;
            _nextInputSendTime = Time.time + 1f / inputSendRate;

            CmdSetInput(input);
        }

        [Command]
        private void CmdSetInput(Vector2 input)
        {
            _serverInput = Vector2.ClampMagnitude(input, 1f);
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            Vector3 direction = new Vector3(_serverInput.x, 0f, _serverInput.y);

            if (direction.sqrMagnitude <= 0.001f)
                return;

            direction.Normalize();

            transform.position += direction * moveSpeed * Time.fixedDeltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.sKey.isPressed) input.y += 1f;
                if (keyboard.wKey.isPressed) input.y -= 1f;
            }

            Gamepad gamepad = Gamepad.current;

            if (gamepad != null)
            {
                Vector2 stickInput = gamepad.leftStick.ReadValue();

                if (stickInput.sqrMagnitude > input.sqrMagnitude)
                    input = stickInput;
            }

            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}