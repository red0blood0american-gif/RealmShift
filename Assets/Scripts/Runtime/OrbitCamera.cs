using UnityEngine;

namespace RealmShift
{
    /// <summary>
    /// Third-person orbit camera. Mouse (when the cursor is locked) and the right stick yaw/pitch it.
    /// A spherecast keeps the camera from sitting inside walls.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float pivotHeight = 1.55f;
        public float distance = 5.4f;
        public float minDistance = 0.65f;
        public float maxDistance = 8f;
        public float mouseSensitivity = 0.09f;
        public float stickSensitivity = 150f;
        public float defaultPitch = 16f;
        public float minPitch = -25f;
        public float maxPitch = 62f;
        public float collisionRadius = 0.28f;
        public LayerMask collisionMask = ~0;
        public bool suspend;

        float yaw;
        float pitch;
        bool snapped;

        public float Yaw => yaw;
        public Vector3 FlatForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 FlatRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapBehind(newTarget);
        }

        public void SnapBehind(Transform newTarget)
        {
            if (newTarget == null)
                return;
            target = newTarget;
            yaw = newTarget.eulerAngles.y;
            pitch = defaultPitch;
            snapped = true;
            ApplyImmediate();
        }

        void Start()
        {
            if (!snapped && target != null)
                SnapBehind(target);
        }

        void LateUpdate()
        {
            if (suspend || target == null)
                return;

            var player = target.GetComponent<PlayerController>();
            var input = target.GetComponent<PlayerInputReader>();
            bool automated = player != null && player.AutomationActive;
            if (input != null && !automated)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    yaw += input.LookMouse.x * mouseSensitivity;
                    pitch -= input.LookMouse.y * mouseSensitivity;
                }
                yaw += input.LookStick.x * stickSensitivity * Time.deltaTime;
                pitch -= input.LookStick.y * stickSensitivity * Time.deltaTime;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            ApplyImmediate();
        }

        void ApplyImmediate()
        {
            if (target == null)
                return;
            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            float desiredDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            Vector3 desired = pivot - rot * Vector3.forward * desiredDistance;
            Vector3 offset = desired - pivot;
            float dist = offset.magnitude;
            if (dist > 0.01f)
            {
                Vector3 dir = offset / dist;
                if (Physics.SphereCast(pivot, collisionRadius, dir, out var hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float pull = Mathf.Max(hit.distance - 0.12f, minDistance * 0.5f);
                    desired = pivot + dir * pull;
                }
            }

            transform.SetPositionAndRotation(desired, rot);
        }
    }
}
