using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// World-space panel shown after the exam conversation ends.
    /// Player can choose to Finish (→ hub) or Retake the exam.
    /// Place this as a world-space Canvas near the student chair.
    /// Toggle visibility via Show() / Hide().
    /// </summary>
    public class ExamFinishPanel : MonoBehaviour
    {
        [Header("Buttons")]
        public Button finishButton;
        public Button retakeButton;

        [Header("References")]
        public SitTrigger sitTrigger;

        [Header("Navigation")]
        public string hubSceneName = "NexusHub";

        private void Awake()
        {
            gameObject.SetActive(false);
            if (finishButton) finishButton.onClick.AddListener(OnFinish);
            if (retakeButton) retakeButton.onClick.AddListener(OnRetake);
        }

        /// <summary>Call this when the professor finishes talking.</summary>
        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void OnFinish()
        {
            Hide();
            if (!string.IsNullOrEmpty(hubSceneName))
                SceneManager.LoadScene(hubSceneName);
        }

        private void OnRetake()
        {
            Hide();
            sitTrigger?.ResetForRetake();
        }
    }
}
