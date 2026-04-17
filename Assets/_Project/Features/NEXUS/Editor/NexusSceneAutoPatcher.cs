#if UNITY_EDITOR
using System.Linq;
using Nexus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Hackathon safety net:
    /// - When you open Unisa scene: ensures 3 extra room portal windows exist near existing portals.
    /// - When you open Room2/Room3 scenes: ensures EXIT portal loads Unisa.
    /// This makes changes visible in Unity scenes even if you forgot to rebuild.
    /// </summary>
    [InitializeOnLoad]
    public static class NexusSceneAutoPatcher
    {
        const string UnisaSceneName = "Unisa";
        const string HubScene = "NexusHub";
        const string Room2Scene = "Room2_CohortMap";
        const string Room3Scene = "Room3_PracticeRoom";

        static NexusSceneAutoPatcher()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!scene.IsValid()) return;

            if (scene.name == UnisaSceneName)
            {
                EnsureCampusPortals(scene);
                return;
            }

            if (scene.name == Room2Scene || scene.name == Room3Scene)
            {
                EnsureExitLoadsUnisa(scene);
                return;
            }
        }

        static void EnsureExitLoadsUnisa(Scene scene)
        {
            var portals = Object.FindObjectsByType<RoomPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool changed = false;

            foreach (var p in portals)
            {
                if (p == null) continue;
                if (p.roomLabel != null && p.roomLabel.ToUpperInvariant().Contains("EXIT"))
                {
                    if (p.targetSceneName != UnisaSceneName)
                    {
                        p.targetSceneName = UnisaSceneName;
                        EditorUtility.SetDirty(p);
                        changed = true;
                    }
                }
            }

            // If no exit portal exists at all, create one.
            if (!portals.Any(p => p != null && (p.roomLabel?.ToUpperInvariant().Contains("EXIT") ?? false)))
            {
                var pos = ComputeExitPositionFromFloor();
                var rot = Quaternion.Euler(0, 0f, 0f);
                SpawnPortalWindow(scene, pos, rot, "EXIT\nUNISA CAMPUS", UnisaSceneName, template: portals.FirstOrDefault(p => p != null));
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[NexusSceneAutoPatcher] Patched EXIT portal to load {UnisaSceneName} in {scene.name}.");
            }
        }

        static void EnsureCampusPortals(Scene scene)
        {
            // If they already exist, bail.
            var existing = Object.FindObjectsByType<RoomPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Any(p => p != null && p.targetSceneName == Room2Scene && p.gameObject.name.StartsWith("CampusPortal_")))
                return;

            // Clone the look/scale of an existing campus portal so the new ones match perfectly.
            var template = existing.FirstOrDefault(p => p != null)?.gameObject;
            if (template == null)
            {
                // Fallback: use our simple window if we can't find a template.
                var anchorFallback = ComputeAnchor(existing);
                SpawnPortal(scene, anchorFallback + new Vector3(-2.2f, 0f, -0.2f), Quaternion.Euler(0, 15f, 0), "NEXUS HUB", HubScene, new Color(0.25f, 0.75f, 1f));
                SpawnPortal(scene, anchorFallback + new Vector3(0.0f, 0f, -0.2f), Quaternion.Euler(0, 0f, 0), "ROOM 2\nCOHORT MAP", Room2Scene, new Color(0.2f, 0.7f, 1f));
                SpawnPortal(scene, anchorFallback + new Vector3(2.2f, 0f, -0.2f), Quaternion.Euler(0, -15f, 0), "ROOM 3\nEXAM", Room3Scene, new Color(0.55f, 1f, 0.4f));
            }
            else
            {
                // Place new portals in the same row as the existing two portals.
                var existingPortals = existing.Where(p => p != null).Select(p => p.transform).ToArray();
                var (startPos, dir, spacing, baseRot) = ComputePortalRow(existingPortals);

                SpawnPortalWindow(scene, startPos + dir * (spacing * 1f), baseRot, "NEXUS HUB", HubScene, template: template);
                SpawnPortalWindow(scene, startPos + dir * (spacing * 2f), baseRot, "ROOM 2\nCOHORT MAP", Room2Scene, template: template);
                SpawnPortalWindow(scene, startPos + dir * (spacing * 3f), baseRot, "ROOM 3\nEXAM", Room3Scene, template: template);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NexusSceneAutoPatcher] Spawned 3 campus portals in Unisa scene.");
        }

        static (Vector3 startPos, Vector3 dir, float spacing, Quaternion rot) ComputePortalRow(Transform[] portals)
        {
            // If we have at least 2 portals, extend the row past the "rightmost" portal.
            if (portals != null && portals.Length >= 2)
            {
                var ordered = portals.OrderBy(t => t.position.x).ToArray();
                var a = ordered[0].position;
                var b = ordered[ordered.Length - 1].position;
                var spacing = Mathf.Max(1.6f, Vector3.Distance(a, b));
                var dir = (b - a);
                dir.y = 0f;
                dir = dir.sqrMagnitude < 0.001f ? Vector3.right : dir.normalized;
                return (b, dir, spacing, ordered[0].rotation);
            }

            var anchor = portals != null && portals.Length == 1 ? portals[0].position : Vector3.zero;
            return (anchor, Vector3.right, 2.2f, portals != null && portals.Length == 1 ? portals[0].rotation : Quaternion.identity);
        }

        static Vector3 ComputeExitPositionFromFloor()
        {
            // Place exit portal near the "south" side of the room based on the floor bounds.
            var floor = GameObject.Find("Floor");
            if (floor != null)
            {
                var r = floor.GetComponent<Renderer>();
                if (r != null)
                {
                    var b = r.bounds;
                    return new Vector3(b.center.x, 0f, b.min.z + 1.8f);
                }
            }
            return new Vector3(0f, 0f, -6f);
        }

        static void SpawnPortalWindow(Scene scene, Vector3 pos, Quaternion rot, string label, string targetScene, GameObject template)
        {
            GameObject root;
            if (template != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(template);
                if (root == null) root = Object.Instantiate(template);
                root.name = "CampusPortal_" + label.Replace("\n", "_").Replace(" ", "");
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = pos;
                root.transform.rotation = rot;

                var portal = root.GetComponent<RoomPortal>() ?? root.GetComponentInChildren<RoomPortal>(true);
                if (portal == null) portal = root.AddComponent<RoomPortal>();
                portal.roomLabel = label.Replace("\n", " ");
                portal.targetSceneName = targetScene;

                // Update any TMP text we can find so the label matches.
                foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))
                    t.text = label;
                foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    t.text = label;
            }
            else
            {
                // Absolute fallback
                SpawnPortal(scene, pos, rot, label, targetScene, new Color(0.25f, 0.75f, 1f));
            }
        }

        static Vector3 ComputeAnchor(RoomPortal[] portals)
        {
            // Anchor near existing portals if present, otherwise near origin.
            if (portals != null)
            {
                var pts = portals.Where(p => p != null).Select(p => p.transform.position).ToArray();
                if (pts.Length > 0)
                {
                    var avg = Vector3.zero;
                    foreach (var p in pts) avg += p;
                    avg /= pts.Length;
                    avg.y = 0f;
                    return avg;
                }
            }
            return Vector3.zero;
        }

        static void SpawnPortal(Scene scene, Vector3 pos, Quaternion rot, string label, string targetScene, Color glow)
        {
            var root = new GameObject("CampusPortal_" + label.Replace("\n", "_").Replace(" ", ""));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = pos;
            root.transform.rotation = rot;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0, 1.6f, 0);
            frame.transform.localScale = new Vector3(2.3f, 2.8f, 0.18f);
            Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial = MakeEmissive(glow, "CampusPortal_Frame");

            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0, 1.6f, -0.11f);
            face.transform.localScale = new Vector3(2.05f, 2.55f, 1f);
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.GetComponent<Renderer>().sharedMaterial = MakeFace(glow, "CampusPortal_Face");

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(root.transform, false);
            textGO.transform.localPosition = new Vector3(0, 3.3f, -0.05f);
            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 0.28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.4f, 3.0f, 0.8f);
            box.center = new Vector3(0, 1.6f, 0);

            var portal = root.AddComponent<RoomPortal>();
            portal.roomLabel = label.Replace("\n", " ");
            portal.targetSceneName = targetScene;
        }

        static Material MakeEmissive(Color glow, string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.12f, 0.14f, 0.22f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.12f, 0.14f, 0.22f));
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow * 0.55f);
            }
            return mat;
        }

        static Material MakeFace(Color glow, string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };
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
#endif

