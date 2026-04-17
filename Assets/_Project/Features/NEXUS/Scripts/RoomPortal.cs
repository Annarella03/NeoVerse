using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Portal: when the player enters the trigger, either
    ///  (1) teleports the HardwareRig to <see cref="destination"/> in the same scene, OR
    ///  (2) loads a different scene if <see cref="targetSceneName"/> is set.
    /// Scene loading takes priority over same-scene teleport.
    /// </summary>
    public class RoomPortal : MonoBehaviour
    {
        [Header("Mode 1: Same-scene teleport (leave targetSceneName empty)")]
        [Tooltip("Empty GameObject at the destination. The HardwareRig will be moved here.")]
        public Transform destination;

        [Header("Mode 2: Load a different scene")]
        [Tooltip("Name of a scene in Build Settings. If set, this portal loads the scene instead of teleporting.")]
        public string targetSceneName = "";

        [Header("Display")]
        public string roomLabel = "Room";

        [Header("Cooldown")]
        public float cooldownSeconds = 2f;
        private float _lastTeleportTime = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastTeleportTime < cooldownSeconds) return;

            var rig = other.GetComponentInParent<HardwareRig>();
            if (rig == null) return;

            _lastTeleportTime = Time.time;

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log($"[RoomPortal] Loading scene: {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
                return;
            }

            if (destination != null)
            {
                Debug.Log($"[RoomPortal] Teleporting to {roomLabel} at {destination.position}");
                rig.Teleport(destination.position);
            }
        }
    }
}
