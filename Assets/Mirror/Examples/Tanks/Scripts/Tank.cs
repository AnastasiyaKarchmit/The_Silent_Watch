using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Mirror.Examples.Tanks
{
    public class Tank : NetworkBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator  animator;
        public TextMesh  healthBar;
        public Transform turret;

        [Header("Movement")]
        public float rotationSpeed = 100;

        [Header("Firing")]
        public KeyCode shootKey = KeyCode.Space;
        public GameObject projectilePrefab;
        public Transform  projectileMount;

        [Header("Stats")]
        [SyncVar] public int health = 5;

        // naming for easier debugging
        public override void OnStartClient()
        {
            name = $"Player[{netId}|{(isLocalPlayer ? "local" : "remote")}]";
        }

        public override void OnStartServer()
        {
            name = $"Player[{netId}|server]";
        }

        private void Update()
        {
            UpdateHealthBar();

            if (!Application.isFocused)
                return;

            if (!isLocalPlayer)
                return;

            Vector2 moveInput = ReadMoveInput();

            RotateTank(moveInput.x);
            MoveTank(moveInput.y);
            HandleShooting();
            RotateTurret();
        }

        private void UpdateHealthBar()
        {
            if (healthBar != null)
                healthBar.text = new string('-', health);
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

        private void RotateTank(float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) <= 0.01f)
                return;

            transform.Rotate(
                0f,
                horizontalInput * rotationSpeed * Time.deltaTime,
                0f);
        }

        private void MoveTank(float verticalInput)
        {
            if (agent == null)
                return;

            float forwardInput = Mathf.Max(verticalInput, 0f);

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            agent.velocity = forward * forwardInput * agent.speed;

            if (animator != null)
                animator.SetBool("Moving", agent.velocity != Vector3.zero);
        }

        private void HandleShooting()
        {
            bool shootPressed = false;

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                shootPressed = true;

            Gamepad gamepad = Gamepad.current;

            if (gamepad != null && gamepad.rightTrigger.wasPressedThisFrame)
                shootPressed = true;

            if (shootPressed)
                CmdFire();
        }

        [Command]
        private void CmdFire()
        {
            GameObject projectile = Instantiate(
                projectilePrefab,
                projectileMount.position,
                projectileMount.rotation);

            NetworkServer.Spawn(projectile);

            RpcOnFire();
        }

        [ClientRpc]
        private void RpcOnFire()
        {
            if (animator != null)
                animator.SetTrigger("Shoot");
        }

        private void RotateTurret()
        {
            if (turret == null)
                return;

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.DrawLine(ray.origin, hit.point);

                Vector3 lookPosition = new Vector3(
                    hit.point.x,
                    turret.position.y,
                    hit.point.z);

                turret.LookAt(lookPosition);
            }
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>() == null)
                return;

            health--;

            if (health <= 0)
                NetworkServer.Destroy(gameObject);
        }
    }
}
