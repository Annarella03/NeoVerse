using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Ensures the UNISA campus map scene has portal windows to the NEXUS rooms.
    /// This avoids manual scene editing during hackathon crunch.
    /// </summary>
    public class UnisaPortalBootstrap : MonoBehaviour
    {
        const string UnisaSceneName = "Unisa";

        const string HubScene = "NexusHub";
        const string Room2Scene = "Room2_CohortMap";
        const string Room3Scene = "Room3_PracticeRoom";

        static bool _wired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            if (_wired) return;
            _wired = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != UnisaSceneName) return;

            // If portals already exist, don't duplicate.
            var existing = Object.FindObjectsByType<RoomPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Any(p => p != null && p.targetSceneName == Room2Scene)) return;

            var anchor = ComputeAnchor(existing);
            SpawnPortal(anchor + new Vector3(-2.2f, 0f, 0.0f), Quaternion.Euler(0, 15f, 0), "NEXUS HUB", HubScene, new Color(0.25f, 0.75f, 1f));
            SpawnPortal(anchor + new Vector3(0.0f, 0f, 0.0f), Quaternion.Euler(0, 0f, 0), "ROOM 2\nCOHORT MAP", Room2Scene, new Color(0.2f, 0.7f, 1f));
            SpawnPortal(anchor + new Vector3(2.2f, 0f, 0.0f), Quaternion.Euler(0, -15f, 0), "ROOM 3\nEXAM", Room3Scene, new Color(0.55f, 1f, 0.4f));
        }

        static Vector3 ComputeAnchor(RoomPortal[] portals)
        {
            // Prefer anchoring near the existing two portals in the center of the yard.
            if (portals != null && portals.Length > 0)
            {
                var points = portals.Where(p => p != null).Select(p => p.transform.position).ToArray();
                if (points.Length > 0)
                {
                    var avg = Vector3.zero;
                    foreach (var p in points) avg += p;
                    avg /= points.Length;
                    avg.y = 0f;
                    return avg;
                }
            }
            return Vector3.zero;
        }

        static void SpawnPortal(Vector3 pos, Quaternion rot, string label, string targetScene, Color glow)
        {
            var root = new GameObject("CampusPortal_" + label.Replace("\n", "_").Replace(" ", ""));
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Simple "window"
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0, 1.6f, 0);
            frame.transform.localScale = new Vector3(2.3f, 2.8f, 0.18f);
            Object.Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial = MakeEmissive(glow);

            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0, 1.6f, -0.11f);
            face.transform.localScale = new Vector3(2.05f, 2.55f, 1f);
            Object.Destroy(face.GetComponent<Collider>());
            face.GetComponent<Renderer>().sharedMaterial = MakeFace(glow);

            // Label
            var textGO = new GameObject("Label");
            textGO.transform.SetParent(root.transform, false);
            textGO.transform.localPosition = new Vector3(0, 3.3f, -0.05f);
            textGO.transform.localRotation = Quaternion.identity;
            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 0.28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            // Trigger + portal behaviour
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.4f, 3.0f, 0.8f);
            box.center = new Vector3(0, 1.6f, 0);

            var portal = root.AddComponent<RoomPortal>();
            portal.roomLabel = label.Replace("\n", " ");
            portal.targetSceneName = targetScene;
        }

        static Material MakeEmissive(Color glow)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "CampusPortal_Frame" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.12f, 0.14f, 0.22f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.12f, 0.14f, 0.22f));
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow * 0.55f);
            }
            return mat;
        }

        static Material MakeFace(Color glow)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "CampusPortal_Face" };
            var c = new Color(glow.r, glow.g, glow.b, 0.25f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow * 0.85f);
            }
            return mat;
        }
    }
}

