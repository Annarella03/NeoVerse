using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    public class ProfileSetupUI : MonoBehaviour
    {
        [Header("Input fields (assign in Inspector)")]
        public TMP_InputField nameField;
        public TMP_InputField universityField;
        public TMP_InputField fieldOfStudyField;
        public TMP_InputField semesterField;
        public TMP_InputField countryField;

        [Header("Confirm button")]
        public Button confirmButton;

        [Tooltip("Panel to hide on confirm (optional).")]
        public GameObject rootPanel;

        private void Awake()
        {
            var p = PlayerProfile.Instance;
            if (nameField) nameField.text = p.playerName;
            if (universityField) universityField.text = p.homeUniversity;
            if (fieldOfStudyField) fieldOfStudyField.text = p.fieldOfStudy;
            if (semesterField) semesterField.text = p.arrivalSemester;
            if (countryField) countryField.text = p.homeCountry;

            if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);
        }

        public void OnConfirm()
        {
            PlayerProfile.Instance.SetAll(
                nameField ? nameField.text : null,
                universityField ? universityField.text : null,
                fieldOfStudyField ? fieldOfStudyField.text : null,
                semesterField ? semesterField.text : null,
                countryField ? countryField.text : null
            );
            Debug.Log($"[NEXUS] Player profile saved: {PlayerProfile.Instance.playerName} / {PlayerProfile.Instance.homeCountry}");
            if (rootPanel) rootPanel.SetActive(false);
        }
    }
}
