using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Place on the player's pin stand. Finds the PinData child and stamps it
    /// with PlayerProfile values at Start.
    /// </summary>
    public class PinHolder : MonoBehaviour
    {
        [Tooltip("Leave null to auto-find in children.")]
        public PinData playerPin;

        private void Start()
        {
            if (playerPin == null)
                playerPin = GetComponentInChildren<PinData>(true);
            if (playerPin == null)
            {
                Debug.LogError("[PinHolder] No PinData found in children.");
                return;
            }
            playerPin.isPlayerPin = true;
        }
    }
}
