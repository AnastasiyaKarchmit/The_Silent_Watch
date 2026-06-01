using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Gameplay
{
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkTestPlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Input Send")]
        [SerializeField] private float sendRate = 20f;

        [Header("Local Only")]
        [SerializeField] private GameObject localOnlyObjects;

        private Vector2 _serverMoveInput;
        private Vector2 _lastSentInput;
        private float _nextSendTime;

        public override void OnStartClient()
        {
            base.OnStartClient();

            Debug.Log($"[Player] OnStartClient | netId={netId} | isLocalPlayer={isLocalPlayer} | isOwned={isOwned}");

            if (localOnlyObjects != null)
                localOnlyObjects.SetActive(false);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            Debug.Log($"[Player] OnStartLocalPlayer | netId={netId}");

            if (localOnlyObjects != null)
                localOnlyObjects.SetActive(true);
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            Debug.Log($"[Player] OnStartAuthority | netId={netId}");
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            Vector2 input = ReadMoveInput();

            bool inputChanged = input != _lastSentInput;
            bool sendTick = Time.time >= _nextSendTime;

            if (!inputChanged && !sendTick)
                return;

            _lastSentInput = input;
            _nextSendTime = Time.time + 1f / sendRate;

            CmdSetMoveInput(input);
        }

        [Command(requiresAuthority = false)]
        private void CmdSetMoveInput(Vector2 input, NetworkConnectionToClient sender = null)
        {
            if (sender == null || sender.identity != netIdentity)
            {
                Debug.LogWarning($"[Player] Rejected input for netId={netId}");
                return;
            }

            _serverMoveInput = Vector2.ClampMagnitude(input, 1f);
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            Vector3 direction = new Vector3(_serverMoveInput.x, 0f, _serverMoveInput.y);

            if (direction.sqrMagnitude <= 0.001f)
                return;

            transform.position += direction * (moveSpeed * Time.fixedDeltaTime);

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
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.wKey.isPressed) input.y += 1f;
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