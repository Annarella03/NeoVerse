using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Professor Moretti stands at the welcome position (near door) until the exam starts.
    /// When OnExamStarted() is called (by SitTrigger), he smoothly moves to the desk chair
    /// and faces the student seat.
    /// </summary>
    public class ProfessorBehavior : MonoBehaviour
    {
        [Header("Waypoints — assign Empty GameObjects in Inspector")]
        [Tooltip("Position near the door where professor greets the student.")]
        public Transform welcomePosition;

        [Tooltip("Position at the desk where professor sits for the exam.")]
        public Transform deskPosition;

        [Header("Movement")]
        public float moveSpeed = 1.2f;
        public float turnSpeed = 120f;   // degrees/sec

        private bool  _examStarted;
        private bool  _moving;
        private Vector3    _targetPos;
        private Quaternion _targetRot;

        private void Start()
        {
            if (welcomePosition != null)
            {
                transform.position = welcomePosition.position;
                transform.rotation = welcomePosition.rotation;
            }
            _targetPos = transform.position;
            _targetRot = transform.rotation;
        }

        /// <summary>Called by SitTrigger when the student sits down.</summary>
        public void OnExamStarted()
        {
            if (_examStarted) return;
            _examStarted = true;
            if (deskPosition != null)
            {
                _targetPos = deskPosition.position;
                _targetRot = deskPosition.rotation;
                _moving   = true;
            }
        }

        private void Update()
        {
            if (!_moving) return;

            transform.position = Vector3.MoveTowards(
                transform.position, _targetPos, moveSpeed * Time.deltaTime);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, _targetRot, turnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetPos) < 0.05f)
                _moving = false;
        }
    }
}
