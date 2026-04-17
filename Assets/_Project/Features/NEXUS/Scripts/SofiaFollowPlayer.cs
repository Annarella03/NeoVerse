using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Makes Sofia (or any NPC) follow the player's XR camera at a short, slightly-
    /// behind-and-to-the-right offset. Uses smooth lerp so it never jitters, and
    /// includes a dead zone so the NPC stops completely when already in range.
    /// </summary>
    public class SofiaFollowPlayer : MonoBehaviour
    {
        [Tooltip("Metres behind and slightly to the right of the player's camera.")]
        public Vector3 offset = new Vector3(0.5f, 0f, -1.5f);

        [Tooltip("Lerp speed. 3 = smooth, relaxed follow.")]
        public float followSpeed = 3f;

        [Tooltip("How fast Sofia rotates to face the player.")]
        public float rotationSpeed = 4f;

        [Tooltip("Dead zone radius: when already within this many metres of target, don't move.")]
        public float deadZone = 0.5f;

        [Tooltip("Keep Sofia on the ground plane (ignore camera Y).")]
        public bool lockY = true;

        [Tooltip("If locked, Sofia uses this Y (defaults to her spawn Y).")]
        public float lockedY;

        private Transform _cam;

        private void Start()
        {
            lockedY = transform.position.y;
            FindCamera();
        }

        private void FindCamera()
        {
            var camGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGO) _cam = camGO.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                FindCamera();
                if (_cam == null) return;
            }

            Vector3 targetPos = _cam.position + _cam.rotation * offset;
            if (lockY) targetPos.y = lockedY;

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance > deadZone)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            }

            Vector3 lookTarget = _cam.position;
            if (lockY) lookTarget.y = transform.position.y;
            Vector3 toPlayer = lookTarget - transform.position;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }
}
