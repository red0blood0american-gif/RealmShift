using UnityEngine;

namespace RealmShift
{
    /// <summary>
    /// Banjo-style third-person move: camera-relative, snappy accel, variable jump, a little coyote time.
    /// CharacterController only. Falls below the level kill height respawn to the last safe ground.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Move")]
        public float walkSpeed = 6.1f;
        public float sprintSpeed = 10.2f;
        public float groundAcceleration = 90f;
        public float airAcceleration = 28f;
        public float rotationSpeed = 840f;
        public float iceControl = 0.38f;

        [Header("Jump")]
        public float gravity = 32f;
        public float jumpHeight = 2.35f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.1f;
        public float jumpCutMultiplier = 0.45f;

        [Header("Safety")]
        public float fallbackKillHeight = -12f;

        public bool IsGrounded => grounded;
        public float FeetY => transform.position.y;
        public float LowestFeetY { get; private set; }
        public bool AutomationActive { get; private set; }

        CharacterController controller;
        PlayerInputReader input;
        OrbitCamera orbit;
        Vector3 horizontal;
        float verticalVelocity;
        bool grounded;
        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        bool jumpCutApplied;
        float iceUntil;
        Vector3 lastSafe;
        Quaternion lastSafeRot;
        bool hasSafe;
        float safeTimer;
        float respawnLockout;
        Vector3 automationTarget;
        bool automationSprint;
        bool placed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            var body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }
            LowestFeetY = transform.position.y;
        }

        void Start()
        {
            orbit = FindFirstObjectByType<OrbitCamera>();
            PlaceAtSpawn();
            if (!Application.isBatchMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            LowestFeetY = transform.position.y;
        }

        public void ResetFloorTrail()
        {
            LowestFeetY = transform.position.y;
        }

        public void DriveTo(Vector3 worldTarget, bool sprint)
        {
            AutomationActive = true;
            automationTarget = worldTarget;
            automationSprint = sprint;
        }

        public void StopDriving()
        {
            AutomationActive = false;
            horizontal = Vector3.zero;
        }

        public void NotifyIce()
        {
            iceUntil = Time.time + 0.12f;
        }

        void PlaceAtSpawn()
        {
            string id = string.IsNullOrEmpty(PlayerSession.NextSpawnId) ? "default" : PlayerSession.NextSpawnId;
            SpawnPoint chosen = null;
            SpawnPoint fallback = null;
            var spawns = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var spawn in spawns)
            {
                if (spawn.spawnId == "default")
                    fallback = spawn;
                if (spawn.spawnId == id)
                    chosen = spawn;
            }
            if (chosen == null)
                chosen = fallback;
            if (chosen != null)
            {
                controller.enabled = false;
                transform.SetPositionAndRotation(chosen.transform.position, chosen.transform.rotation);
                Physics.SyncTransforms();
                controller.enabled = true;
                if (orbit != null)
                    orbit.SnapBehind(transform);
            }
            PlayerSession.NextSpawnId = "default";
            lastSafe = transform.position;
            lastSafeRot = transform.rotation;
            hasSafe = true;
            verticalVelocity = 0f;
            horizontal = Vector3.zero;
            placed = true;
        }

        void Update()
        {
            if (!placed)
                return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f)
                return;

            ReadWish(out Vector3 wishDir, out bool sprint, out bool jumpPressed, out bool jumpHeld);
            grounded = controller.isGrounded;
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (verticalVelocity < 0f)
                    verticalVelocity = -2.4f;
                jumpCutApplied = false;
            }

            if (jumpPressed)
                lastJumpPressedTime = Time.time;

            bool canJump = grounded || (Time.time - lastGroundedTime) <= coyoteTime;
            if (canJump && verticalVelocity <= 0.2f && (Time.time - lastJumpPressedTime) <= jumpBufferTime)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
                lastJumpPressedTime = -10f;
                lastGroundedTime = -10f;
                jumpCutApplied = false;
                grounded = false;
            }

            if (!jumpHeld && verticalVelocity > 0f && !jumpCutApplied)
            {
                verticalVelocity *= jumpCutMultiplier;
                jumpCutApplied = true;
            }

            verticalVelocity -= gravity * dt;

            bool onIce = Time.time < iceUntil;
            float control = onIce ? iceControl : 1f;
            float accel = (grounded ? groundAcceleration : airAcceleration) * control;
            float speed = sprint ? sprintSpeed : walkSpeed;
            Vector3 wish = wishDir * speed;
            horizontal = Vector3.MoveTowards(horizontal, wish, accel * dt);

            Vector3 motion = horizontal * dt + Vector3.up * verticalVelocity * dt;
            controller.Move(motion);

            if (wishDir.sqrMagnitude > 0.04f)
            {
                var face = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, face, rotationSpeed * dt);
            }

            if (transform.position.y < LowestFeetY)
                LowestFeetY = transform.position.y;

            if (grounded && verticalVelocity <= 0.1f)
            {
                safeTimer += dt;
                if (safeTimer > 0.2f)
                {
                    lastSafe = transform.position;
                    lastSafeRot = transform.rotation;
                    hasSafe = true;
                }
            }
            else
            {
                safeTimer = 0f;
            }

            float kill = fallbackKillHeight;
            var level = FindFirstObjectByType<LevelSettings>();
            if (level != null)
                kill = level.killHeight;
            if (transform.position.y < kill && Time.time >= respawnLockout)
                Respawn();
        }

        void ReadWish(out Vector3 wishDir, out bool sprint, out bool jumpPressed, out bool jumpHeld)
        {
            if (AutomationActive)
            {
                Vector3 to = automationTarget - transform.position;
                to.y = 0f;
                wishDir = to.sqrMagnitude > 0.04f ? to.normalized : Vector3.zero;
                sprint = automationSprint;
                jumpPressed = false;
                jumpHeld = false;
                return;
            }

            Vector2 move = input != null ? input.Move : Vector2.zero;
            sprint = input != null && input.SprintHeld;
            jumpPressed = input != null && input.JumpPressed;
            jumpHeld = input != null && input.JumpHeld;

            Vector3 forward = orbit != null ? orbit.FlatForward : transform.forward;
            Vector3 right = orbit != null ? orbit.FlatRight : transform.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;
            forward.Normalize();
            right.Normalize();
            wishDir = forward * move.y + right * move.x;
            if (wishDir.sqrMagnitude > 1f)
                wishDir.Normalize();
        }

        void Respawn()
        {
            respawnLockout = Time.time + 0.4f;
            Vector3 pos = hasSafe ? lastSafe : transform.position;
            Quaternion rot = hasSafe ? lastSafeRot : transform.rotation;
            controller.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            Physics.SyncTransforms();
            controller.enabled = true;
            verticalVelocity = 0f;
            horizontal = Vector3.zero;
            if (orbit != null)
                orbit.SnapBehind(transform);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<IceSurface>() != null)
                NotifyIce();
        }
    }
}
