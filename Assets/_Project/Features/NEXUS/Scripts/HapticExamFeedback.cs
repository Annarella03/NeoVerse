using System.Collections.Generic;
using Convai.Scripts.Runtime.Core;
using UnityEngine;
using UnityEngine.XR;

namespace Nexus
{
    /// <summary>
    /// Fires a short haptic pulse on both XR controllers every time the professor NPC
    /// starts speaking (a new exam question). Subscribes to Convai's
    /// OnCharacterTalkingChanged event exposed by ConvaiNPCAudioManager.
    /// </summary>
    public class HapticExamFeedback : MonoBehaviour
    {
        [Tooltip("ConvaiNPC that drives the exam (Professor Moretti). Auto-found via tag if null.")]
        public ConvaiNPC targetNpc;

        [Tooltip("Tag used to auto-find the professor NPC.")]
        public string professorTag = "ProfessorNPC";

        [Range(0f, 1f)] public float amplitude = 0.3f;
        public float duration = 0.15f;

        private ConvaiNPCAudioManager _audioManager;
        private static readonly List<InputDevice> _leftDevices = new();
        private static readonly List<InputDevice> _rightDevices = new();

        private void OnEnable()
        {
            if (targetNpc == null)
            {
                var tagged = GameObject.FindGameObjectsWithTag(professorTag);
                foreach (var go in tagged)
                {
                    targetNpc = go.GetComponent<ConvaiNPC>();
                    if (targetNpc == null) targetNpc = go.GetComponentInChildren<ConvaiNPC>();
                    if (targetNpc != null) break;
                }
            }

            if (targetNpc == null)
            {
                Debug.LogWarning("[HapticExamFeedback] No target ConvaiNPC found. Waiting.");
                Invoke(nameof(TryLateSubscribe), 1f);
                return;
            }

            Subscribe();
        }

        private void TryLateSubscribe()
        {
            if (targetNpc == null || _audioManager != null) return;
            Subscribe();
        }

        private void Subscribe()
        {
            _audioManager = targetNpc.GetComponent<ConvaiNPCAudioManager>();
            if (_audioManager == null)
            {
                Debug.LogError("[HapticExamFeedback] Target NPC has no ConvaiNPCAudioManager.");
                return;
            }
            _audioManager.OnCharacterTalkingChanged += OnCharacterTalkingChanged;
        }

        private void OnDisable()
        {
            if (_audioManager != null)
                _audioManager.OnCharacterTalkingChanged -= OnCharacterTalkingChanged;
        }

        private void OnCharacterTalkingChanged(bool isTalking)
        {
            if (!isTalking) return;
            SendHaptic();
        }

        private void SendHaptic()
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, _leftDevices);
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, _rightDevices);

            foreach (var d in _leftDevices)
            {
                if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    d.SendHapticImpulse(0u, amplitude, duration);
            }
            foreach (var d in _rightDevices)
            {
                if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    d.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
