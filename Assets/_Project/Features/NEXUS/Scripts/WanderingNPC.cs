using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Simple waypoint wanderer for Room 2 student NPCs.
    /// Smoothly moves between waypoints and pauses briefly at each stop.
    /// Waypoints are set automatically by NexusSceneBuilder; you can override in Inspector.
    /// </summary>
    public class WanderingNPC : MonoBehaviour
    {
        [Tooltip("World-space positions the NPC walks between (auto-set by builder).")]
        public Vector3[] waypoints;

        public float moveSpeed = 0.7f;
        public float waitTime  = 3f;
        public float turnSpeed = 90f;   // degrees/sec

        private int   _idx;
        private float _waitUntil;
        private bool  _waiting;

        private void Start()
        {
            // If no waypoints assigned, generate a small loop around starting position
            if (waypoints == null || waypoints.Length == 0)
            {
                Vector3 p = transform.position;
                waypoints = new[]
                {
                    p + new Vector3( 2f, 0,  0),
                    p + new Vector3( 2f, 0,  2f),
                    p + new Vector3(-2f, 0,  2f),
                    p + new Vector3(-2f, 0, -2f),
                    p
                };
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            if (_waiting)
            {
                if (Time.time >= _waitUntil) _waiting = false;
                return;
            }

            Vector3 target = waypoints[_idx];
            target.y = transform.position.y;

            Vector3 dir = target - transform.position;

            if (dir.sqrMagnitude > 0.02f)
            {
                // Face the direction of travel
                Quaternion look = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, look, turnSpeed * Time.deltaTime);

                transform.position = Vector3.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
            }
            else
            {
                _idx = (_idx + 1) % waypoints.Length;
                _waitUntil = Time.time + waitTime;
                _waiting = true;
            }
        }
    }
}
