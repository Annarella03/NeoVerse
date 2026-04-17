using Fusion.XR.Shared.Rig;
using TMPro;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Animated building sign for UNISA hub/campus.
    /// Pulses gently; on player proximity shows an info popup with building name + description.
    /// The popup faces the player automatically.
    /// </summary>
    public class BuildingSign : MonoBehaviour
    {
        [Header("Content")]
        public string buildingName   = "Building";
        [TextArea(2, 5)]
        public string description    = "Department info here.";

        [Header("References — auto-wired by builder")]
        public Renderer  signRenderer;
        public GameObject infoPopup;    // world-space Canvas or Billboard GO
        public TextMeshPro infoText;

        [Header("Glow Settings")]
        public float idleIntensity   = 0.6f;
        public float hoverIntensity  = 2.5f;
        public float pulseSpeed      = 1.8f;
        public Color signColor       = Color.cyan;

        private Material _mat;
        private bool     _playerNear;
        private Camera   _cam;

        private void Start()
        {
            if (signRenderer)
            {
                _mat = signRenderer.material;
                if (_mat.HasProperty("_EmissionColor"))
                    _mat.EnableKeyword("_EMISSION");
            }

            if (infoPopup) infoPopup.SetActive(false);
            if (infoText) infoText.text = $"<b>{buildingName}</b>\n\n{description}";
        }

        private void Update()
        {
            UpdateGlow();
            FacePopupToCamera();
        }

        private void UpdateGlow()
        {
            if (_mat == null || !_mat.HasProperty("_EmissionColor")) return;
            float target  = _playerNear ? hoverIntensity : idleIntensity;
            float pulse   = _playerNear ? 1f : (0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed));
            Color emissive = signColor * target * pulse;
            _mat.SetColor("_EmissionColor", emissive);
        }

        private void FacePopupToCamera()
        {
            if (!_playerNear || infoPopup == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            Vector3 dir = _cam.transform.position - infoPopup.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                infoPopup.transform.rotation = Quaternion.LookRotation(-dir);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<HardwareRig>() == null) return;
            _playerNear = true;
            if (infoPopup) infoPopup.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<HardwareRig>() == null) return;
            _playerNear = false;
            if (infoPopup) infoPopup.SetActive(false);
        }
    }
}
