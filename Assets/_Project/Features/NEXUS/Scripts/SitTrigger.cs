using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// Attach to a BoxCollider on the student chair area.
    /// When the player enters and presses the controller trigger (or Space in Editor),
    /// the exam begins: professor is notified, exam context is sent to Convai.
    /// A finish/retake panel is shown after the exam.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SitTrigger : MonoBehaviour
    {
        [Header("Scene References")]
        public ProfessorBehavior professor;
        public ExamSettingsPanel  examSettings;
        public ExamFinishPanel    finishPanel;

        [Header("Prompt — assign a world-space UI GameObject")]
        public GameObject promptUI;          // "Press Trigger to Begin Exam"

        [Header("Cooldown")]
        public float cooldownSeconds = 2f;

        private bool  _examActive;
        private bool  _playerInside;
        private float _lastTriggerTime = -99f;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            if (promptUI) promptUI.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<HardwareRig>() == null) return;
            _playerInside = true;
            if (!_examActive && promptUI) promptUI.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<HardwareRig>() == null) return;
            _playerInside = false;
            if (promptUI) promptUI.SetActive(false);
        }

        private void Update()
        {
            if (!_playerInside || _examActive) return;
            if (Time.time - _lastTriggerTime < cooldownSeconds) return;

            bool pressed = Input.GetKeyDown(KeyCode.Space) || RightTriggerPressed();
            if (!pressed) return;

            _lastTriggerTime = Time.time;
            StartExam();
        }

        private static bool RightTriggerPressed()
        {
            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Controller |
                UnityEngine.XR.InputDeviceCharacteristics.Right, devices);
            foreach (var d in devices)
            {
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed) && pressed)
                    return true;
            }
            return false;
        }

        private void StartExam()
        {
            _examActive = true;
            if (promptUI) promptUI.SetActive(false);
            professor?.OnExamStarted();
            examSettings?.OnConfirm();
            Debug.Log("[SitTrigger] Exam started — professor notified.");
        }

        /// <summary>Called by ExamFinishPanel's Retake button.</summary>
        public void ResetForRetake()
        {
            _examActive = false;
            if (_playerInside && promptUI) promptUI.SetActive(true);
        }
    }
}
