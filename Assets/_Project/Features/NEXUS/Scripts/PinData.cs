using TMPro;
using UnityEngine;
using Fusion.XR.Shared.Grabbing;

namespace Nexus
{
    /// <summary>
    /// Local-mode cohort map pin. No Fusion networking required.
    /// Uses the project's existing Grabbable component (same as whiteboard pens).
    /// When released, pin stays wherever it was dropped.
    /// Info card (name/uni/field) shows when the camera is within showDistance.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class PinData : MonoBehaviour
    {
        [Header("Student Info")]
        public string studentName;
        public string homeUniversity;
        public string fieldOfStudy;

        [Header("Is this the player's own pin?")]
        public bool isPlayerPin = false;

        [Header("Info Card — assign child Canvas + TMP_Text fields")]
        public GameObject infoCard;
        public TMP_Text nameText;
        public TMP_Text universityText;
        public TMP_Text fieldText;

        [Range(1f, 5f)]
        public float showDistance = 2.5f;

        private Grabbable _grabbable;
        private Camera _cam;
        private bool _placed = false;
        private Rigidbody _rb;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _rb = GetComponent<Rigidbody>();

            if (_grabbable != null)
            {
                _grabbable.onGrab.AddListener(OnGrabbed);
                _grabbable.onUngrab.AddListener(OnReleased);
            }

            if (infoCard) infoCard.SetActive(false);
            UpdateCardTexts();
        }

        private void Start()
        {
            // If this is the player's pin, apply profile data
            if (isPlayerPin)
            {
                var p = PlayerProfile.Instance;
                studentName = p.playerName;
                homeUniversity = p.homeUniversity;
                fieldOfStudy = p.fieldOfStudy;
                UpdateCardTexts();
            }
        }

        private void OnGrabbed()
        {
            _placed = false;
            if (infoCard) infoCard.SetActive(false);
        }

        private void OnReleased()
        {
            _placed = true;
            // Freeze in place so it doesn't roll away
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (!_placed || infoCard == null) return;

            if (_cam == null)
            {
                var camGO = GameObject.FindGameObjectWithTag("MainCamera");
                if (camGO) _cam = camGO.GetComponent<Camera>();
                if (_cam == null) return;
            }

            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            bool show = dist <= showDistance;
            if (infoCard.activeSelf != show) infoCard.SetActive(show);

            // Face card toward camera
            if (show)
            {
                Vector3 dir = _cam.transform.position - infoCard.transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    infoCard.transform.rotation = Quaternion.LookRotation(-dir);
            }
        }

        public void SetStudentInfo(string name, string university, string field)
        {
            studentName = name;
            homeUniversity = university;
            fieldOfStudy = field;
            UpdateCardTexts();
        }

        private void UpdateCardTexts()
        {
            if (nameText) nameText.text = studentName;
            if (universityText) universityText.text = homeUniversity;
            if (fieldText) fieldText.text = fieldOfStudy;
        }
    }
}
