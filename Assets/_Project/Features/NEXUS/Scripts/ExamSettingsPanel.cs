using Convai.Scripts.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus
{
    /// <summary>
    /// UI panel for Room 3 exam context. Three dropdowns (Subject / Language / Difficulty)
    /// and a Confirm button that sends the exam opening context to the ConvaiNPC tagged
    /// "ProfessorNPC". Can be placed on the wrist menu or as a world-space panel inside
    /// Room 3 near the entrance (fallback if wrist menu extension is too complex).
    /// </summary>
    public class ExamSettingsPanel : MonoBehaviour
    {
        [Header("Dropdowns")]
        public TMP_Dropdown subjectDropdown;
        public TMP_Dropdown languageDropdown;
        public TMP_Dropdown difficultyDropdown;

        [Header("Confirm button")]
        public Button confirmButton;

        [Header("Optional")]
        [Tooltip("Panel to hide on confirm so the player can continue to the chair.")]
        public GameObject rootPanel;

        [Tooltip("Tag used to find the professor NPC in the scene.")]
        public string professorTag = "ProfessorNPC";

        private void Awake()
        {
            if (subjectDropdown && subjectDropdown.options.Count == 0)
            {
                subjectDropdown.options.Clear();
                subjectDropdown.options.Add(new TMP_Dropdown.OptionData("Computer Science"));
                subjectDropdown.options.Add(new TMP_Dropdown.OptionData("Biology"));
                subjectDropdown.options.Add(new TMP_Dropdown.OptionData("Economics"));
                subjectDropdown.options.Add(new TMP_Dropdown.OptionData("Law"));
                subjectDropdown.RefreshShownValue();
            }
            if (languageDropdown && languageDropdown.options.Count == 0)
            {
                languageDropdown.options.Clear();
                languageDropdown.options.Add(new TMP_Dropdown.OptionData("English"));
                languageDropdown.options.Add(new TMP_Dropdown.OptionData("Italian"));
                languageDropdown.RefreshShownValue();
            }
            if (difficultyDropdown && difficultyDropdown.options.Count == 0)
            {
                difficultyDropdown.options.Clear();
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Easy"));
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Hard"));
                difficultyDropdown.RefreshShownValue();
            }

            if (confirmButton) confirmButton.onClick.AddListener(SendExamContext);
        }

        public void SendExamContext()
        {
            string subject = subjectDropdown ? subjectDropdown.options[subjectDropdown.value].text : "Computer Science";
            string language = languageDropdown ? languageDropdown.options[languageDropdown.value].text : "English";
            string difficulty = difficultyDropdown ? difficultyDropdown.options[difficultyDropdown.value].text : "Easy";

            string message = $"We are beginning a practice oral exam. Subject: {subject}. " +
                             $"Language: {language}. Difficulty: {difficulty}. " +
                             $"Please begin with your first question now.";

            var npc = FindProfessorNPC();
            if (npc == null)
            {
                Debug.LogError($"[ExamSettingsPanel] No ConvaiNPC with tag '{professorTag}' found in scene.");
                return;
            }

            Debug.Log($"[NEXUS] Sending exam context to {npc.characterName}: {message}");
            npc.SendTextDataAsync(message);

            if (rootPanel) rootPanel.SetActive(false);
        }

        // Keep the room trigger wiring simple: the chair flow can invoke the same confirm behavior.
        public void OnConfirm()
        {
            SendExamContext();
        }

        private ConvaiNPC FindProfessorNPC()
        {
            var tagged = GameObject.FindGameObjectsWithTag(professorTag);
            foreach (var go in tagged)
            {
                var npc = go.GetComponent<ConvaiNPC>();
                if (npc == null) npc = go.GetComponentInChildren<ConvaiNPC>();
                if (npc != null) return npc;
            }
            return null;
        }
    }
}
