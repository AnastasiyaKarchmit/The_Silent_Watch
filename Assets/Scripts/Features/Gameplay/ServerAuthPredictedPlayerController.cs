using Core.Input.Contracts;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Gameplay
{
     [RequireComponent(typeof(NetworkIdentity))]
    public sealed class ServerAuthPredictedPlayerController : NetworkBehaviour
    {
        private const int BufferSize = 1024;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Input Batching")]
        [SerializeField] private float inputSendRate = 20f;
        [SerializeField] private int maxInputsPerBatch = 8;
        [SerializeField] private int redundantInputCount = 2;

        [Header("Prediction")]
        [SerializeField] private float reconciliationThreshold = 0.15f;

        [Header("Remote Interpolation")]
        [SerializeField] private float remotePositionLerpSpeed = 14f;
        [SerializeField] private float remoteRotationLerpSpeed = 14f;

        private readonly InputFrame[] _inputBuffer = new InputFrame[BufferSize];

        private IInputService _inputService;

        private int _localSequence;
        private int _lastReconciledSequence;
        private int _pendingInputCount;

        private int _serverLastProcessedSequence;

        private float _nextInputSendTime;

        private Vector3 _remoteTargetPosition;
        private Quaternion _remoteTargetRotation;
        private bool _hasRemoteTarget;

        private struct InputFrame
        {
            public int Sequence;
            public Vector2 MoveInput;
            public Vector3 PredictedPosition;
            public Quaternion PredictedRotation;
            public bool IsValid;
        }

        public struct MoveInputCommand
        {
            public int Sequence;
            public Vector2 MoveInput;
        }

        public void InitializeLocalInput(IInputService inputService)
        {
            _inputService = inputService;
            Debug.Log($"[PlayerInput] Input initialized for local player netId={netId}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _remoteTargetPosition = transform.position;
            _remoteTargetRotation = transform.rotation;
            _hasRemoteTarget = true;

            Debug.Log(
                $"[Player] OnStartClient | netId={netId} | " +
                $"isLocalPlayer={isLocalPlayer} | isOwned={isOwned} | pos={transform.position}");
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            Debug.Log($"[Player] OnStartLocalPlayer | netId={netId}");
        }

        private void FixedUpdate()
        {
            if (CanPredictLocally())
                ClientPredictFixedUpdate();
        }

        private void Update()
        {
            if (!isClient)
                return;

            if (isLocalPlayer)
                return;

            InterpolateRemotePlayer();
        }

        private void ClientPredictFixedUpdate()
        {
            Vector2 moveInput = ReadMoveInput();

            _localSequence++;

            SimulateMovement(moveInput, Time.fixedDeltaTime);

            int bufferIndex = _localSequence % BufferSize;

            _inputBuffer[bufferIndex] = new InputFrame
            {
                Sequence = _localSequence,
                MoveInput = moveInput,
                PredictedPosition = transform.position,
                PredictedRotation = transform.rotation,
                IsValid = true
            };

            _pendingInputCount++;

            bool enoughInputs = _pendingInputCount >= Mathf.Max(1, maxInputsPerBatch - redundantInputCount);
            bool sendTick = Time.unscaledTime >= _nextInputSendTime;

            if (enoughInputs || sendTick)
                FlushInputBatch();
        }

        private void FlushInputBatch()
        {
            if (_pendingInputCount <= 0)
                return;

            int redundantCount = Mathf.Max(0, redundantInputCount);
            int maxBatchSize = Mathf.Max(1, maxInputsPerBatch);

            int firstSequence = Mathf.Max(
                1,
                _localSequence - _pendingInputCount - redundantCount + 1);

            int totalCount = _localSequence - firstSequence + 1;

            if (totalCount > maxBatchSize)
            {
                firstSequence = _localSequence - maxBatchSize + 1;
                totalCount = maxBatchSize;
            }

            var batch = new MoveInputCommand[totalCount];

            for (int i = 0; i < totalCount; i++)
            {
                int sequence = firstSequence + i;
                int bufferIndex = sequence % BufferSize;
                InputFrame frame = _inputBuffer[bufferIndex];

                batch[i] = new MoveInputCommand
                {
                    Sequence = sequence,
                    MoveInput = frame.IsValid && frame.Sequence == sequence
                        ? frame.MoveInput
                        : Vector2.zero
                };
            }

            _pendingInputCount = 0;
            _nextInputSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, inputSendRate);

            CmdSubmitInputBatch(batch);
        }

        private bool CanPredictLocally()
        {
            if (!isClient)
                return false;

            if (!isLocalPlayer)
                return false;

            if (!isOwned)
                return false;

            if (NetworkClient.localPlayer == null)
                return false;

            if (NetworkClient.localPlayer.netId != netId)
                return false;

            if (!Application.isFocused)
                return false;

            if (_inputService == null)
                return false;

            return true;
        }

        [Command(requiresAuthority = true, channel = Channels.Unreliable)]
        private void CmdSubmitInputBatch(MoveInputCommand[] batch)
        {
            if (batch == null || batch.Length == 0)
                return;

            int lastProcessedSequence = _serverLastProcessedSequence;
            bool processedAnyInput = false;

            for (int i = 0; i < batch.Length; i++)
            {
                MoveInputCommand command = batch[i];

                if (command.Sequence <= _serverLastProcessedSequence)
                    continue;

                Vector2 moveInput = Vector2.ClampMagnitude(command.MoveInput, 1f);

                SimulateMovement(moveInput, Time.fixedDeltaTime);

                _serverLastProcessedSequence = command.Sequence;
                lastProcessedSequence = command.Sequence;
                processedAnyInput = true;
            }

            if (!processedAnyInput)
                return;

            TargetReceiveAuthoritativeState(
                connectionToClient,
                lastProcessedSequence,
                transform.position,
                transform.rotation);

            RpcReceiveRemoteState(
                lastProcessedSequence,
                transform.position,
                transform.rotation);
        }

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetReceiveAuthoritativeState(
            NetworkConnectionToClient target,
            int sequence,
            Vector3 serverPosition,
            Quaternion serverRotation)
        {
            Reconcile(sequence, serverPosition, serverRotation);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcReceiveRemoteState(
            int sequence,
            Vector3 serverPosition,
            Quaternion serverRotation)
        {
            if (isLocalPlayer)
                return;

            _remoteTargetPosition = serverPosition;
            _remoteTargetRotation = serverRotation;
            _hasRemoteTarget = true;
        }

        private void Reconcile(
            int sequence,
            Vector3 serverPosition,
            Quaternion serverRotation)
        {
            if (sequence <= _lastReconciledSequence)
                return;

            _lastReconciledSequence = sequence;

            int bufferIndex = sequence % BufferSize;
            InputFrame frame = _inputBuffer[bufferIndex];

            if (!frame.IsValid || frame.Sequence != sequence)
                return;

            float positionError = Vector3.Distance(
                frame.PredictedPosition,
                serverPosition);

            if (positionError <= reconciliationThreshold)
                return;

            Debug.Log(
                $"[Prediction] Reconcile | netId={netId} | seq={sequence} | error={positionError:F3}");

            transform.SetPositionAndRotation(serverPosition, serverRotation);

            int replaySequence = sequence + 1;

            while (replaySequence <= _localSequence)
            {
                int replayIndex = replaySequence % BufferSize;
                InputFrame replayFrame = _inputBuffer[replayIndex];

                if (!replayFrame.IsValid || replayFrame.Sequence != replaySequence)
                    break;

                SimulateMovement(replayFrame.MoveInput, Time.fixedDeltaTime);

                replayFrame.PredictedPosition = transform.position;
                replayFrame.PredictedRotation = transform.rotation;

                _inputBuffer[replayIndex] = replayFrame;
                replaySequence++;
            }
        }

        private void SimulateMovement(Vector2 input, float deltaTime)
        {
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            if (direction.sqrMagnitude <= 0.001f)
                return;

            direction.Normalize();

            transform.position += direction * moveSpeed * deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * deltaTime);
        }

        private void InterpolateRemotePlayer()
        {
            if (!_hasRemoteTarget)
                return;

            transform.position = Vector3.Lerp(
                transform.position,
                _remoteTargetPosition,
                remotePositionLerpSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _remoteTargetRotation,
                remoteRotationLerpSpeed * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            if (_inputService == null)
                return Vector2.zero;

            return Vector2.ClampMagnitude(
                _inputService.Gameplay.Move.Value,
                1f);
        }
    }
}