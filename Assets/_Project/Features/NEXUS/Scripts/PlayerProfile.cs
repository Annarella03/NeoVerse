using UnityEngine;

namespace Nexus
{
    [CreateAssetMenu(fileName = "PlayerProfile", menuName = "NEXUS/Player Profile", order = 1)]
    public class PlayerProfile : ScriptableObject
    {
        public string playerName = "Bekhruz";
        public string homeUniversity = "Apple Developer Academy - UniSA";
        public string fieldOfStudy = "Software Engineering";
        public string arrivalSemester = "Spring 2027";
        public string homeCountry = "Uzbekistan";

        private static PlayerProfile _instance;

        public static PlayerProfile Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PlayerProfile>("PlayerProfile");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[PlayerProfile] No asset found in Resources/PlayerProfile. Creating runtime default.");
                        _instance = CreateInstance<PlayerProfile>();
                    }
                }
                return _instance;
            }
        }

        public void SetAll(string name, string university, string field, string semester, string country)
        {
            if (!string.IsNullOrEmpty(name)) playerName = name;
            if (!string.IsNullOrEmpty(university)) homeUniversity = university;
            if (!string.IsNullOrEmpty(field)) fieldOfStudy = field;
            if (!string.IsNullOrEmpty(semester)) arrivalSemester = semester;
            if (!string.IsNullOrEmpty(country)) homeCountry = country;
        }
    }
}
