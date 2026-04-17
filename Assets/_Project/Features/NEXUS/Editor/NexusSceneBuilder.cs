#if UNITY_EDITOR
using System.Collections.Generic;
using Fusion.XR.Shared.Grabbing;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus.EditorTools
{
    /// <summary>
    /// One-click NEXUS scene builder — professional quality.
    ///
    /// Menu: NEXUS -> Build Everything
    /// Creates 3 scenes (Hub, Room2 Cohort Map, Room3 Practice Exam) with full geometry,
    /// proper lighting, post-processing, Convai NPC wiring, and all script connections.
    /// </summary>
    public static class NexusSceneBuilder
    {
        // ---- Prefab paths ----
        const string RigPrefabPath     = "Assets/_Project/Features/XR/Rigs/Prefabs/HardwareRig Variant.prefab";
        const string RunnerPrefabPath  = "Assets/_Project/Shared/Prefabs/MetaverseRunner.prefab";
        const string MapPinPrefabPath  = "Assets/_Project/Features/NEXUS/Prefabs/MapPin.prefab";
        const string SofiaPrefabPath   = "Assets/ThirdParty/Convai/Demo/Avatars/Convai NPC Amelia.prefab";
        const string ProfPrefabPath    = "Assets/ThirdParty/Convai/Demo/Characters/Prefabs/Convai NPC Mike Carter.prefab";

        // ---- Convai Character IDs ----
        const string ProfMorettiCharId = "0037028c-3a35-11f1-93c7-42010a7be02c";
        const string EuropeMapTexturePath = "Assets/_Project/Features/NEXUS/Resources/EuropeMap.png";

        // ---- Scene/folder paths ----
        const string ScenesFolder      = "Assets/_Project/Spaces/Nexus/Scenes";
        const string PrefabsFolder     = "Assets/_Project/Features/NEXUS/Prefabs";
        const string HubScene          = "NexusHub";
        const string Room2Scene        = "Room2_CohortMap";
        const string Room3Scene        = "Room3_PracticeRoom";
        const string UnisaScene        = "Unisa";
        const string UnisaScenePath    = "Assets/_Project/Spaces/Unisa/Unisa.unity";

        // ---- Europe map approximate pin positions (x = west→east, z = south→north) on a 3.8 x 2.3 surface ----
        // Relative to map center. Values are approximate lat/lon projections on the flat map.
        // Pin positions tuned for the Europe area on the EuropeMap texture surface (3.8 x 2.4).
        // (x = west→east, z = south→north) relative to map center.
        static readonly (string name, string uni, string field, string color, float x, float z)[] StudentPins =
        {
            // Germany (Bielefeld)
            ("Jonas Weber",     "TU Bielefeld",          "Computer Science",  "blue",   0.22f,  0.34f),
            // France (Tours)
            ("Marie Dupont",    "Univ. de Tours",        "Biology",           "blue",  -0.48f,  0.22f),
            // Romania (Suceava)
            ("Andrei Popescu",  "Univ. Suceava",         "Economics",         "blue",   0.78f,  0.22f),
            // Spain (Jaen)
            ("Elena Garcia",    "Univ. Jaen",            "Law",               "blue",  -0.78f, -0.18f),
            // Cyprus (Nicosia) — slightly SE
            ("Univ. Nicosia",   "NEOLAiA Partner",       "Partner",           "gold",   0.98f, -0.40f),
            // Partner pin near Germany
            ("Univ. Bielefeld", "NEOLAiA Partner",       "Partner",           "gold",   0.25f,  0.48f),
        };

        static readonly (string label, float x, float z)[] CountryLabels =
        {
            ("DE", 0.22f, 0.42f), ("FR", -0.42f, 0.30f), ("IT", 0.38f, -0.05f),
            ("RO", 0.78f, 0.34f), ("ES", -0.72f, -0.12f), ("CY", 0.98f, -0.36f),
            ("UK", -0.62f, 0.62f), ("PL", 0.55f, 0.55f), ("GR", 0.62f, -0.26f),
        };

        // =========================================================
        // ENTRY POINTS
        // =========================================================

        [MenuItem("NEXUS/Build Everything")]
        public static void BuildAll()
        {
            try
            {
                EnsureFolders();
                EnsureTag("Locomotion");
                EnsureTag("ProfessorNPC");
                EnsureTag("SofiaNPC");

                CreateMapPinPrefab();

                BuildHubScene();
                BuildRoom2Scene();
                BuildRoom3Scene();

                AddScenesToBuildSettings();

                EditorSceneManager.OpenScene($"{ScenesFolder}/{HubScene}.unity");

                EditorUtility.DisplayDialog(
                    "NEXUS Build Complete ✓",
                    "All 3 scenes built with geometry, lighting, post-processing, and script wiring.\n\n" +
                    "REQUIRED NEXT STEPS:\n" +
                    "1. Create Sofia on convai.com → paste Character ID into SofiaNPC in each scene.\n" +
                    "2. Create Professor Moretti on convai.com → paste ID into ProfessorNPC in Room3.\n" +
                    "3. Create PlayerProfile asset: Assets → Create → NEXUS → Player Profile.\n" +
                    "   Place it in Assets/_Project/Features/NEXUS/Resources/PlayerProfile.\n\n" +
                    "OPTIONAL: Replace capsule NPCs with real Convai avatar prefabs.",
                    "Understood");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[NexusSceneBuilder] BUILD FAILED: " + e);
                EditorUtility.DisplayDialog("NEXUS Build Failed",
                    e.Message + "\n\nSee Console for full stack trace.", "OK");
            }
        }

        [MenuItem("NEXUS/Rebuild MapPin Prefab Only")]
        public static void RebuildMapPinOnly()
        {
            EnsureFolders();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MapPinPrefabPath) != null)
                AssetDatabase.DeleteAsset(MapPinPrefabPath);
            CreateMapPinPrefab();
            EditorUtility.DisplayDialog("NEXUS", "MapPin prefab rebuilt.", "OK");
        }

        // =========================================================
        // FOLDERS + TAGS
        // =========================================================

        static void EnsureFolders()
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder("Assets/_Project/Features/NEXUS/Resources");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void EnsureTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var tags = so.FindProperty("tags");
            if (tags == null) return;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.arraySize++;
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // =========================================================
        // MAPPIN PREFAB
        // =========================================================

        static void CreateMapPinPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MapPinPrefabPath) != null) return;

            var root = new GameObject("MapPin");
            try
            {
                var rb = root.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                var col = root.AddComponent<SphereCollider>();
                col.radius = 0.08f;

                // Stem
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(root.transform, false);
                stem.transform.localPosition = new Vector3(0, -0.08f, 0);
                stem.transform.localScale = new Vector3(0.012f, 0.08f, 0.012f);
                Object.DestroyImmediate(stem.GetComponent<Collider>());
                stem.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("PinStem", new Color(0.75f, 0.75f, 0.75f), 0.8f, 0.15f);

                // Head (sphere)
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(root.transform, false);
                head.transform.localPosition = Vector3.zero;
                head.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
                Object.DestroyImmediate(head.GetComponent<Collider>());
                head.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("PinHead_Blue", new Color(0.1f, 0.4f, 1f), 0.2f, 0.6f);

                // Grab
                var grab = root.AddComponent<Grabbable>();
                grab.applyVelocityOnRelease = false;

                // PinData
                var pinData = root.AddComponent<Nexus.PinData>();
                pinData.showDistance = 2.8f;

                // Info card canvas (world space)
                var card = new GameObject("InfoCard");
                card.transform.SetParent(root.transform, false);
                card.transform.localPosition = new Vector3(0, 0.28f, 0);
                var canvas = card.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                card.AddComponent<CanvasScaler>();
                card.AddComponent<GraphicRaycaster>();
                var rt = card.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(280, 140);
                rt.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

                // Card bg with rounded feel
                var bg = new GameObject("Background");
                bg.transform.SetParent(card.transform, false);
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.06f, 0.08f, 0.18f, 0.92f);
                var bgRT = bg.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

                // Accent top bar
                var bar = new GameObject("AccentBar");
                bar.transform.SetParent(card.transform, false);
                var barImg = bar.AddComponent<Image>();
                barImg.color = new Color(0.1f, 0.5f, 1f, 1f);
                var barRT = bar.GetComponent<RectTransform>();
                barRT.anchorMin = new Vector2(0, 1); barRT.anchorMax = new Vector2(1, 1);
                barRT.sizeDelta = new Vector2(0, 8); barRT.anchoredPosition = new Vector2(0, -4);

                var nameText  = CreateTMPChild(card.transform, "Name",       22, 42f, FontStyles.Bold);
                var uniText   = CreateTMPChild(card.transform, "University", 16, 12f, FontStyles.Normal);
                var fieldText = CreateTMPChild(card.transform, "Field",      15, -16f, FontStyles.Italic);

                nameText.color  = Color.white;
                uniText.color   = new Color(0.85f, 0.92f, 1f);
                fieldText.color = new Color(0.7f, 0.85f, 1f);

                pinData.infoCard       = card;
                pinData.nameText       = nameText;
                pinData.universityText = uniText;
                pinData.fieldText      = fieldText;

                card.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, MapPinPrefabPath);
                Debug.Log("[NexusSceneBuilder] MapPin prefab created.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static TMP_Text CreateTMPChild(Transform parent, string name, int fontSize, float yOffset, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = style;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(-20, 30); rt.anchoredPosition = new Vector2(0, yOffset);
            return tmp;
        }

        // =========================================================
        // COMMON SCENE SETUP
        // =========================================================

        static Scene NewEmptyScene()
            => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static void AddEssentials(Vector3 spawnPos)
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab != null)
                ((GameObject)PrefabUtility.InstantiatePrefab(rigPrefab)).transform.position = spawnPos;
            else
                Debug.LogWarning("[NexusSceneBuilder] HardwareRig prefab not found.");

            var runnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RunnerPrefabPath);
            if (runnerPrefab != null)
                PrefabUtility.InstantiatePrefab(runnerPrefab);
        }

        static void AddDirectionalLight(Vector3 eulerAngles, float intensity, Color color)
        {
            var go = new GameObject("Directional Light");
            var l  = go.AddComponent<Light>();
            l.type      = LightType.Directional;
            l.intensity = intensity;
            l.color     = color;
            l.shadows   = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(eulerAngles);
        }

        static GameObject AddPointLight(Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("PointLight");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = color;
            l.intensity = intensity;
            l.range     = range;
            return go;
        }

        static int _ppProfileIndex = 0;

        /// <summary>Add a URP post-processing volume for cinematic look.</summary>
        static void AddPostProcessing(Color tint, float bloomIntensity = 0.4f, float saturation = 10f)
        {
            var go  = new GameObject("PostProcessing Volume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Bloom
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value  = bloomIntensity;
            bloom.threshold.value  = 0.9f;
            bloom.scatter.value    = 0.5f;
            bloom.active           = true;

            // Color Adjustments
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.value    = saturation;
            ca.contrast.value      = 12f;
            ca.colorFilter.value   = tint;
            ca.active              = true;

            // Tone Mapping
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.value          = TonemappingMode.ACES;
            tm.active              = true;

            // Vignette
            var vig = profile.Add<Vignette>(true);
            vig.intensity.value    = 0.2f;
            vig.smoothness.value   = 0.5f;
            vig.active             = true;

            // Save profile as asset so it persists with the scene
            string profileDir  = "Assets/_Project/Features/NEXUS/Resources";
            string profilePath = $"{profileDir}/PPProfile_{++_ppProfileIndex}.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
                AssetDatabase.DeleteAsset(profilePath);
            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();

            vol.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        }

        static void AddRoomBox(Vector3 center, float w, float d, float h,
                               Material floorMat, Material wallMat, Material ceilMat)
        {
            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor"; floor.tag = "Locomotion";
            floor.transform.position   = center + new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(w, 0.1f, d);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Ceiling
            var ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceil.name = "Ceiling";
            ceil.transform.position   = center + new Vector3(0, h + 0.05f, 0);
            ceil.transform.localScale = new Vector3(w, 0.1f, d);
            ceil.GetComponent<Renderer>().sharedMaterial = ceilMat;
            Object.DestroyImmediate(ceil.GetComponent<Collider>());

            // Walls (N / S / E / W)
            CreateWallCube(center + new Vector3(0,    h/2f,  d/2f), new Vector3(w,    h,    0.18f), wallMat, "Wall_N");
            CreateWallCube(center + new Vector3(0,    h/2f, -d/2f), new Vector3(w,    h,    0.18f), wallMat, "Wall_S");
            CreateWallCube(center + new Vector3(w/2f, h/2f,  0),    new Vector3(0.18f, h,   d),    wallMat, "Wall_E");
            CreateWallCube(center + new Vector3(-w/2f, h/2f, 0),    new Vector3(0.18f, h,   d),    wallMat, "Wall_W");
        }

        static void CreateWallCube(Vector3 pos, Vector3 size, Material mat, string name = "Wall")
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.position   = pos;
            w.transform.localScale = size;
            w.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(w.GetComponent<Collider>());
        }

        static void AddGlowStrip(Vector3 start, Vector3 end, Color emissiveColor, string name = "GlowStrip")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Vector3 mid = (start + end) / 2f;
            go.transform.position = mid;
            float length = Vector3.Distance(start, end);
            go.transform.localScale = new Vector3(length, 0.04f, 0.04f);
            go.transform.LookAt(end);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = MakeEmissiveMaterial("GlowStrip_" + name, emissiveColor, 2f);
        }

        static void SaveScene(Scene scene, string sceneName)
        {
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{sceneName}.unity");
        }

        // =========================================================
        // HUB SCENE — NEXUS connector space
        // =========================================================

        static void BuildHubScene()
        {
            var scene = NewEmptyScene();
            const float W = 22f, D = 22f, H = 5f;
            Vector3 center = Vector3.zero;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.25f, 0.35f);

            AddDirectionalLight(new Vector3(45f, -30f, 0), 0.8f, new Color(0.9f, 0.95f, 1f));
            AddPostProcessing(new Color(1f, 1f, 1.05f), 0.5f, 15f);

            var floorMat = MakePBRMaterial("Hub_Floor", new Color(0.12f, 0.14f, 0.22f), 0.05f, 0.95f);
            var wallMat  = MakePBRMaterial("Hub_Wall",  new Color(0.18f, 0.22f, 0.35f), 0.0f,  0.7f);
            var ceilMat  = MakePBRMaterial("Hub_Ceil",  new Color(0.1f,  0.12f, 0.2f),  0.0f,  0.4f);
            AddRoomBox(center, W, D, H, floorMat, wallMat, ceilMat);

            // Glow strips along floor edges for sci-fi look
            AddGlowStrip(new Vector3(-W/2f+0.5f, 0.05f, -D/2f+0.5f),
                         new Vector3( W/2f-0.5f, 0.05f, -D/2f+0.5f),
                         new Color(0.2f, 0.6f, 1f), "GlowStrip_S");
            AddGlowStrip(new Vector3(-W/2f+0.5f, 0.05f,  D/2f-0.5f),
                         new Vector3( W/2f-0.5f, 0.05f,  D/2f-0.5f),
                         new Color(0.2f, 0.6f, 1f), "GlowStrip_N");

            // Overhead ambient fill lights
            AddPointLight(new Vector3(-5f, 4f, 0),  new Color(0.4f, 0.6f, 1f),  3f, 12f);
            AddPointLight(new Vector3( 5f, 4f, 0),  new Color(0.4f, 0.6f, 1f),  3f, 12f);
            AddPointLight(new Vector3( 0f, 4f, 5f), new Color(0.5f, 0.7f, 1f),  2.5f, 10f);

            // Central NEXUS logo / title
            CreateWorldText3D(center + new Vector3(0, 4.2f, 0), "NEXUS", 0.5f, new Color(0.3f, 0.85f, 1f), bold: true);
            CreateWorldText3D(center + new Vector3(0, 3.55f, 0), "Immersive Erasmus Orientation — UNISA", 0.18f, new Color(0.8f, 0.9f, 1f));

            // 3 portals arranged in front arc
            BuildPortalArch(new Vector3(-5.5f, 0f, 5f),  Quaternion.Euler(0, 15f, 0),  "Room 1\nDiscover",  null,       new Color(0.9f, 0.5f, 0.1f));
            BuildPortalArch(new Vector3(0,     0f, 6.5f), Quaternion.identity,           "Room 2\nConnect",   Room2Scene, new Color(0.2f, 0.7f, 1f));
            BuildPortalArch(new Vector3(5.5f,  0f, 5f),  Quaternion.Euler(0, -15f, 0),  "Room 3\nPractise",  Room3Scene, new Color(0.5f, 1f, 0.4f));

            // Sofia NPC (guide)
            CreateNPCPlaceholder(center + new Vector3(0, 0, 1f), "SofiaNPC",
                new Color(1f, 0.65f, 0.85f), SofiaPrefabPath, "SofiaNPC", attachFollow: true);

            // ---- UNISA Campus Building Signs ----
            // Place signs in the "middle yard" cluster so they're easy to find.
            BuildBuildingSign(center + new Vector3(-2.6f, 0, 2.2f),  Quaternion.Euler(0, 35f, 0),
                "Informatics\nDept.", "Faculty of Computer Science\nand Engineering",
                new Color(0.2f, 0.5f, 1f));

            BuildBuildingSign(center + new Vector3(-3.0f, 0, -0.8f), Quaternion.Euler(0, 15f, 0),
                "Chemistry\nDept.", "Faculty of Sciences\nand Technology",
                new Color(0.2f, 0.85f, 0.4f));

            BuildBuildingSign(center + new Vector3(2.8f, 0, 2.0f),   Quaternion.Euler(0, -25f, 0),
                "Social\nScience", "Faculty of Political\nand Social Sciences",
                new Color(1f, 0.6f, 0.2f));

            BuildBuildingSign(center + new Vector3(3.2f, 0, -0.9f),  Quaternion.Euler(0, -15f, 0),
                "IT Support", "Student Technical\nAssistance Desk",
                new Color(0.8f, 0.3f, 1f));

            BuildBuildingSign(center + new Vector3(0.2f, 0, 3.4f),    Quaternion.Euler(0, 180f, 0),
                "International\nWelcome Desk", "Erasmus & Exchange\nStudent Services",
                new Color(1f, 0.85f, 0.2f));

            AddEssentials(center + new Vector3(0, 0, -7f));

            SaveScene(scene, HubScene);
        }

        static GameObject BuildPortalArch(Vector3 pos, Quaternion rot, string label,
                                           string targetScene, Color glowColor)
        {
            var root = new GameObject("Portal_" + label.Replace("\n", "_").Replace(" ", ""));
            root.transform.position = pos;
            root.transform.rotation = rot;

            float archW = 2.2f, archH = 3.2f, thickness = 0.22f;

            // Left pillar
            var lp = CreateCube("Pillar_L", root.transform,
                new Vector3(-archW/2f, archH/2f, 0), new Vector3(thickness, archH, thickness),
                MakePBRMaterial("PortalFrame", new Color(0.15f, 0.18f, 0.28f), 0.6f, 0.3f));
            // Right pillar
            var rp = CreateCube("Pillar_R", root.transform,
                new Vector3(archW/2f, archH/2f, 0), new Vector3(thickness, archH, thickness),
                lp.GetComponent<Renderer>().sharedMaterial);
            // Top lintel
            CreateCube("Lintel", root.transform,
                new Vector3(0, archH - thickness/2f, 0), new Vector3(archW + thickness, thickness, thickness),
                lp.GetComponent<Renderer>().sharedMaterial);

            // Emissive portal face (glowing plane inside arch)
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "PortalFace";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0, archH/2f - 0.05f, -0.02f);
            face.transform.localScale    = new Vector3(archW - thickness, archH - thickness*1.5f, 1f);
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.GetComponent<Renderer>().sharedMaterial =
                MakeEmissiveMaterial("PortalFace_" + label, glowColor * 0.25f, 0.8f);

            // Glow edges
            float edgeIntensity = 2.5f;
            var edgeMat = MakeEmissiveMaterial("PortalEdge_" + label, glowColor, edgeIntensity);
            CreateEdgeBar("Edge_L", root.transform, new Vector3(-archW/2f, archH/2f, 0.01f),
                new Vector3(0.04f, archH, 0.04f), edgeMat);
            CreateEdgeBar("Edge_R", root.transform, new Vector3( archW/2f, archH/2f, 0.01f),
                new Vector3(0.04f, archH, 0.04f), edgeMat);
            CreateEdgeBar("Edge_T", root.transform, new Vector3(0, archH - 0.02f, 0.01f),
                new Vector3(archW, 0.04f, 0.04f), edgeMat);
            AddPointLight(pos + Vector3.up * (archH * 0.5f), glowColor, 2f, 5f);

            // Label above arch
            CreateWorldText3D(pos + new Vector3(0, archH + 0.4f, 0), label, 0.28f,
                              glowColor * 1.3f, bold: true);

            // Trigger + portal script
            var trig = root.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size   = new Vector3(archW, archH, 0.8f);
            trig.center = new Vector3(0, archH / 2f, 0);

            var portal = root.AddComponent<Nexus.RoomPortal>();
            portal.roomLabel = label.Replace("\n", " ");
            if (!string.IsNullOrEmpty(targetScene))
                portal.targetSceneName = targetScene;

            return root;
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void CreateEdgeBar(string name, Transform parent, Vector3 lp, Vector3 ls, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale    = ls;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // =========================================================
        // ROOM 2 — COHORT MAP (killer feature)
        // =========================================================

        static void BuildRoom2Scene()
        {
            var scene = NewEmptyScene();
            const float W = 18f, D = 18f, H = 4.5f;
            Vector3 center = Vector3.zero;

            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.38f, 0.5f);

            AddDirectionalLight(new Vector3(40f, -20f, 0), 0.9f, new Color(1f, 0.98f, 0.92f));
            AddPostProcessing(new Color(0.98f, 1f, 1.05f), 0.35f, 12f);

            // Warm academic but airy
            var floorMat = MakePBRMaterial("R2_Floor",  new Color(0.55f, 0.48f, 0.38f), 0.0f, 0.4f);
            var wallMat  = MakePBRMaterial("R2_Wall",   new Color(0.88f, 0.85f, 0.78f), 0.0f, 0.5f);
            var ceilMat  = MakePBRMaterial("R2_Ceil",   new Color(0.92f, 0.90f, 0.85f), 0.0f, 0.6f);
            AddRoomBox(center, W, D, H, floorMat, wallMat, ceilMat);

            // Lighting: warm overhead + accent
            AddPointLight(new Vector3(0, H - 0.3f, 0),   new Color(1f, 0.95f, 0.8f), 4f, 14f);
            AddPointLight(new Vector3(-4f, H - 0.5f, 0), new Color(0.9f, 0.95f, 1f), 2f, 10f);
            AddPointLight(new Vector3( 4f, H - 0.5f, 0), new Color(0.9f, 0.95f, 1f), 2f, 10f);
            // Blue accent under map for drama
            AddPointLight(new Vector3(0, 0.2f, 0),        new Color(0.3f, 0.6f, 1f),  1.5f, 6f);

            // Room title
            CreateWorldText3D(center + new Vector3(0, H - 0.4f, D/2f - 0.3f),
                "COHORT MAP — EUROPE", 0.32f, new Color(0.2f, 0.35f, 0.6f), bold: true);

            // ---- Map table ----
            var tableRoot = new GameObject("MapTable");
            // Legs
            for (int xi = -1; xi <= 1; xi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg";
                leg.transform.SetParent(tableRoot.transform, false);
                leg.transform.position = center + new Vector3(xi * 1.7f, 0.5f, zi * 1.0f);
                leg.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
                Object.DestroyImmediate(leg.GetComponent<Collider>());
                leg.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("LegMat", new Color(0.3f, 0.22f, 0.15f), 0.4f, 0.2f);
            }

            // Pedestal top
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "TableTop";
            pedestal.transform.SetParent(tableRoot.transform, false);
            pedestal.transform.position   = center + new Vector3(0, 1.0f, 0);
            pedestal.transform.localScale = new Vector3(4.0f, 0.1f, 2.6f);
            pedestal.GetComponent<Renderer>().sharedMaterial =
                MakePBRMaterial("TableTop", new Color(0.4f, 0.28f, 0.16f), 0.3f, 0.5f);

            // Map surface
            var map = GameObject.CreatePrimitive(PrimitiveType.Cube);
            map.name = "EuropeMap";
            map.transform.SetParent(tableRoot.transform, false);
            map.transform.position   = center + new Vector3(0, 1.065f, 0);
            map.transform.localScale = new Vector3(3.8f, 0.03f, 2.4f);
            map.GetComponent<Renderer>().sharedMaterial = MakeEuropeMapMaterial();

            // Country labels floating above map
            float labelY = 1.18f;
            foreach (var (label, x, z) in CountryLabels)
                CreateSmallLabel(center + new Vector3(x, labelY, z), label);

            // Subtle grid lines on map
            for (int i = -3; i <= 3; i++)
            {
                var lineV = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lineV.name = "GridV_" + i;
                lineV.transform.SetParent(tableRoot.transform, false);
                lineV.transform.position   = center + new Vector3(i * 0.55f, 1.09f, 0);
                lineV.transform.localScale = new Vector3(0.005f, 0.01f, 2.4f);
                Object.DestroyImmediate(lineV.GetComponent<Collider>());
                lineV.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("GridLine", new Color(0.3f, 0.45f, 0.3f, 0.3f), 0f, 0f);
            }

            // ---- Pins ----
            var pinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPinPrefabPath);
            if (pinPrefab != null)
            {
                foreach (var (name, uni, field, colorStr, x, z) in StudentPins)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(pinPrefab);
                    instance.name = "Pin_" + name;
                    instance.transform.position = center + new Vector3(x, 1.12f, z);
                    var pd = instance.GetComponent<Nexus.PinData>();
                    if (pd != null) pd.SetStudentInfo(name, uni, field);

                    // Apply color to head
                    var headT = instance.transform.Find("Head");
                    if (headT != null)
                    {
                        Color pinColor = colorStr == "gold"
                            ? new Color(1f, 0.8f, 0.1f)
                            : new Color(0.15f, 0.45f, 1f);
                        headT.GetComponent<Renderer>().sharedMaterial =
                            MakePBRMaterial("PinHead_" + colorStr, pinColor, 0.1f, 0.7f);
                    }
                }
            }

            // ---- Player pin stand ----
            var standRoot = new GameObject("PlayerPinStand");
            standRoot.transform.position = center + new Vector3(-4.5f, 0f, -4f);

            var standBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            standBase.name = "StandBase";
            standBase.transform.SetParent(standRoot.transform, false);
            standBase.transform.localPosition = new Vector3(0, 0.05f, 0);
            standBase.transform.localScale    = new Vector3(0.5f, 0.05f, 0.5f);
            standBase.GetComponent<Renderer>().sharedMaterial =
                MakeEmissiveMaterial("StandMat", new Color(0.2f, 0.8f, 0.3f), 0.8f);

            CreateWorldText3D(standRoot.transform.position + new Vector3(0, 0.5f, 0),
                "YOUR PIN\nGrab & place on the map", 0.11f,
                new Color(0.3f, 1f, 0.4f));

            if (pinPrefab != null)
            {
                var playerPin = (GameObject)PrefabUtility.InstantiatePrefab(pinPrefab);
                playerPin.name = "PlayerPin";
                playerPin.transform.SetParent(standRoot.transform, false);
                playerPin.transform.localPosition = new Vector3(0, 0.15f, 0);

                var head = playerPin.transform.Find("Head");
                if (head != null)
                    head.GetComponent<Renderer>().sharedMaterial =
                        MakeEmissiveMaterial("PlayerPinHead", new Color(0.15f, 1f, 0.3f), 1.2f);

                var pd = playerPin.GetComponent<Nexus.PinData>();
                if (pd != null) pd.isPlayerPin = true;

                var holder = standRoot.AddComponent<Nexus.PinHolder>();
                holder.playerPin = pd;
            }
            AddPointLight(standRoot.transform.position + new Vector3(0, 1f, 0),
                new Color(0.2f, 1f, 0.4f), 1.5f, 3f);

            // ---- Sofia NPC — Erasmus guide ----
            CreateNPCPlaceholder(center + new Vector3(-7f, 0f, 0f), "SofiaNPC",
                new Color(1f, 0.65f, 0.85f), SofiaPrefabPath, "SofiaNPC", attachFollow: true);

            // ---- Wandering student NPCs (user will add Convai IDs in Inspector) ----
            CreateWanderingStudent(center + new Vector3(4f, 0, 3f), "StudentNPC_Female",
                new Color(1f, 0.6f, 0.7f), SofiaPrefabPath,
                new[] {
                    center + new Vector3(4f, 0, 3f),
                    center + new Vector3(-2f, 0, 4f),
                    center + new Vector3(-4f, 0, 0f),
                    center + new Vector3(0f, 0, -3f),
                });

            CreateWanderingStudent(center + new Vector3(-4f, 0, 4f), "StudentNPC_Male",
                new Color(0.5f, 0.8f, 1f), ProfPrefabPath,
                new[] {
                    center + new Vector3(-4f, 0, 4f),
                    center + new Vector3(3f, 0, 2f),
                    center + new Vector3(5f, 0, -2f),
                    center + new Vector3(0f, 0, 4f),
                });

            // ---- Shared Whiteboard (east wall) ----
            BuildWhiteboard(center + new Vector3(W/2f - 0.08f, 1.8f, 1f));

            // ---- Return portal — clearly labeled EXIT ----
            BuildPortalArch(center + new Vector3(0, 0, -D/2f + 1.5f), Quaternion.identity,
                "EXIT\nUNISA Campus", UnisaScene, new Color(0.4f, 0.5f, 0.8f));

            AddEssentials(center + new Vector3(0, 0, -D/2f + 3f));
            SaveScene(scene, Room2Scene);
        }

        static void CreateSmallLabel(Vector3 pos, string text)
        {
            var go = new GameObject("Label_" + text);
            go.transform.position = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text      = text;
            tmp.fontSize  = 0.07f;
            tmp.color     = new Color(0.2f, 0.3f, 0.55f, 0.85f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            var rt = go.GetComponent<RectTransform>();
            if (rt) rt.sizeDelta = new Vector2(0.6f, 0.2f);
        }

        // =========================================================
        // ROOM 3 — PRACTICE EXAM
        // =========================================================

        static void BuildRoom3Scene()
        {
            var scene = NewEmptyScene();
            const float W = 12f, D = 14f, H = 4f;
            Vector3 center = Vector3.zero;

            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.33f, 0.25f);

            AddDirectionalLight(new Vector3(38f, -15f, 0), 0.7f, new Color(1f, 0.95f, 0.85f));
            AddPostProcessing(new Color(1f, 0.97f, 0.9f), 0.25f, 8f);

            // Warm academic tones
            var floorMat  = MakePBRMaterial("R3_Floor",  new Color(0.38f, 0.28f, 0.18f), 0.0f, 0.3f);
            var wallMat   = MakePBRMaterial("R3_Wall",   new Color(0.72f, 0.65f, 0.52f), 0.0f, 0.5f);
            var ceilMat   = MakePBRMaterial("R3_Ceil",   new Color(0.85f, 0.80f, 0.70f), 0.0f, 0.6f);
            var woodMat   = MakePBRMaterial("R3_Wood",   new Color(0.35f, 0.2f, 0.1f),  0.0f, 0.2f);
            AddRoomBox(center, W, D, H, floorMat, wallMat, ceilMat);

            // Warm overhead + desk lamp
            AddPointLight(new Vector3(0, H - 0.3f, 1f),  new Color(1f, 0.9f, 0.75f), 3.5f, 12f);
            AddPointLight(new Vector3(0, 1.5f, 2.2f),    new Color(1f, 0.85f, 0.6f), 2f,  4f);   // desk lamp
            AddPointLight(new Vector3(-3.5f, 2f, 3f),    new Color(0.8f, 0.9f, 1f),  1f,  6f);   // bookshelf fill

            // Room title — on the NORTH wall, facing south so player reads it on entry
            CreateWorldText3D(center + new Vector3(0, H - 0.4f, D/2f - 0.25f),
                "PRACTICE EXAM ROOM", 0.28f, new Color(0.55f, 0.35f, 0.15f), bold: true, rotY: 180f);

            // ---- Desk (professor's side) ----
            var desk = new GameObject("ProfessorDesk");
            // Desk top
            var deskTop = CreateCube("DeskTop", desk.transform,
                center + new Vector3(0, 0.82f, 2.5f), new Vector3(2.4f, 0.07f, 1.0f), woodMat);
            // Desk body
            CreateCube("DeskBody", desk.transform,
                center + new Vector3(0, 0.4f, 2.5f), new Vector3(2.4f, 0.75f, 0.9f), woodMat);
            // Desk modesty panel
            CreateCube("DeskFront", desk.transform,
                center + new Vector3(0, 0.4f, 2.05f), new Vector3(2.4f, 0.8f, 0.06f), woodMat);
            // Desk lamp
            CreateDeskLamp(desk.transform, center + new Vector3(0.7f, 0.86f, 2.3f), woodMat);
            // Papers/books on desk (decorative)
            for (int i = 0; i < 3; i++)
            {
                var paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                paper.name = "Paper_" + i;
                paper.transform.SetParent(desk.transform, false);
                paper.transform.position   = center + new Vector3(-0.4f + i * 0.12f, 0.87f, 2.6f + i * 0.02f);
                paper.transform.localScale = new Vector3(0.3f, 0.01f, 0.22f);
                paper.transform.rotation   = Quaternion.Euler(0, i * 5 - 5, 0);
                Object.DestroyImmediate(paper.GetComponent<Collider>());
                paper.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("Paper", new Color(0.97f, 0.95f, 0.88f), 0f, 0.2f);
            }

            // ---- Chairs ----
            BuildChair(center + new Vector3(0, 0, 3.4f), "ProfessorChair", woodMat,
                MakePBRMaterial("ProfChairPad", new Color(0.18f, 0.12f, 0.08f), 0f, 0.1f));
            BuildChair(center + new Vector3(0, 0, 0.8f), "StudentChair", woodMat,
                MakePBRMaterial("StuChairPad",  new Color(0.22f, 0.16f, 0.1f),  0f, 0.1f));

            // ---- Extra student desks / classroom rows (back half of room) ----
            BuildStudentDeskRow(center + new Vector3(0, 0, -1.5f), woodMat, 3);   // row 1
            BuildStudentDeskRow(center + new Vector3(0, 0, -3.0f), woodMat, 3);   // row 2

            // ---- Bookshelf ----
            BuildBookshelf(center + new Vector3(-W/2f + 0.5f, 0, 3.5f), woodMat);

            // ---- Window (decorative, east wall) ----
            var winFrame = CreateCube("WindowFrame", new GameObject("Window").transform,
                center + new Vector3(W/2f - 0.05f, 2f, 1f),
                new Vector3(0.15f, 1.8f, 1.5f), woodMat);
            var winGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winGlass.name = "WindowGlass";
            winGlass.transform.position   = center + new Vector3(W/2f - 0.07f, 2f, 1f);
            winGlass.transform.localScale = new Vector3(0.05f, 1.7f, 1.3f);
            winGlass.GetComponent<Renderer>().sharedMaterial =
                MakeEmissiveMaterial("Glass", new Color(0.75f, 0.88f, 1f), 0.3f);
            Object.DestroyImmediate(winGlass.GetComponent<Collider>());
            AddPointLight(center + new Vector3(W/2f - 1f, 2f, 1f), new Color(0.8f, 0.92f, 1f), 1.5f, 5f);

            // ---- Diploma/painting on wall ----
            var diploma = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diploma.name = "Diploma";
            diploma.transform.position   = center + new Vector3(-W/2f + 0.12f, 2.2f, 2f);
            diploma.transform.localScale = new Vector3(0.05f, 0.7f, 0.95f);
            Object.DestroyImmediate(diploma.GetComponent<Collider>());
            diploma.GetComponent<Renderer>().sharedMaterial =
                MakePBRMaterial("DiplomaFrame", new Color(0.6f, 0.5f, 0.3f), 0.5f, 0.3f);

            // ---- Professor NPC — Professor Moretti with real Convai character ----
            var prof = CreateNPCPlaceholder(
                center + new Vector3(0, 0, -4.3f),   // welcome position (clear of desks)
                "ProfessorNPC",
                new Color(0.3f, 0.45f, 0.9f),
                ProfPrefabPath,
                "ProfessorNPC",
                attachFollow: false,
                characterId: ProfMorettiCharId);
            prof.tag = "ProfessorNPC";
            prof.transform.rotation = Quaternion.Euler(0, 0f, 0f); // face into the room while welcoming

            // Welcome & desk waypoints
            var welcomeWP = new GameObject("ProfWelcomeWP");
            welcomeWP.transform.position = center + new Vector3(0, 0, -4.3f);
            welcomeWP.transform.rotation = Quaternion.Euler(0, 0, 0); // face south (toward door)

            var deskWP = new GameObject("ProfDeskWP");
            deskWP.transform.position = center + new Vector3(0, 0, 3.4f);
            deskWP.transform.rotation = Quaternion.Euler(0, 180f, 0); // face south (toward student)

            var profBehav = prof.AddComponent<Nexus.ProfessorBehavior>();
            profBehav.welcomePosition = welcomeWP.transform;
            profBehav.deskPosition    = deskWP.transform;

            // ---- Exam settings world-space panel — facing player as they enter ----
            var examSettingsGO = CreateExamSettingsCanvas(center + new Vector3(-3.5f, 1.5f, -2f));

            // ---- Exam Finish Panel ----
            var finishPanel = CreateExamFinishCanvas(center + new Vector3(0, 1.5f, 0.5f));

            // ---- Sit Trigger on student chair area ----
            var sitTriggerGO = new GameObject("SitTrigger");
            sitTriggerGO.transform.position = center + new Vector3(0, 0, 0.8f);
            var sitCol = sitTriggerGO.AddComponent<BoxCollider>();
            sitCol.isTrigger = true;
            sitCol.size = new Vector3(1.2f, 2f, 1.2f);
            sitCol.center = new Vector3(0, 1f, 0);

            var sitTrig = sitTriggerGO.AddComponent<Nexus.SitTrigger>();
            sitTrig.professor = profBehav;
            // Wire ExamSettingsPanel if we can find the component
            var settingsComp = examSettingsGO?.GetComponent<Nexus.ExamSettingsPanel>();
            sitTrig.examSettings = settingsComp;
            var finishComp = finishPanel?.GetComponent<Nexus.ExamFinishPanel>();
            if (finishComp != null)
            {
                finishComp.sitTrigger  = sitTrig;
                finishComp.hubSceneName = HubScene;
            }
            sitTrig.finishPanel = finishComp;

            // Sit prompt label
            CreateWorldText3D(center + new Vector3(0, 1.4f, 0.8f),
                "Approach chair · Press Trigger to Begin Exam", 0.07f,
                new Color(1f, 0.9f, 0.3f), bold: true, rotY: 180f);

            // ---- Haptic feedback ----
            new GameObject("HapticExamFeedback").AddComponent<Nexus.HapticExamFeedback>();

            // ---- Return portal ----
            BuildPortalArch(center + new Vector3(0, 0, -D/2f + 1.5f), Quaternion.identity,
                "EXIT\nUNISA Campus", UnisaScene, new Color(0.5f, 0.55f, 0.9f));

            AddEssentials(center + new Vector3(0, 0, -D/2f + 3f));
            SaveScene(scene, Room3Scene);
        }

        static void CreateDeskLamp(Transform parent, Vector3 pos, Material baseMat)
        {
            var lampRoot = new GameObject("DeskLamp");
            lampRoot.transform.SetParent(parent, false);
            lampRoot.transform.position = pos;

            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "LampBase"; b.transform.SetParent(lampRoot.transform, false);
            b.transform.localPosition = Vector3.zero;
            b.transform.localScale    = new Vector3(0.08f, 0.02f, 0.08f);
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.GetComponent<Renderer>().sharedMaterial = baseMat;

            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "LampArm"; arm.transform.SetParent(lampRoot.transform, false);
            arm.transform.localPosition = new Vector3(0, 0.15f, 0);
            arm.transform.localScale    = new Vector3(0.015f, 0.15f, 0.015f);
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.GetComponent<Renderer>().sharedMaterial = baseMat;

            var shade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shade.name = "LampShade"; shade.transform.SetParent(lampRoot.transform, false);
            shade.transform.localPosition = new Vector3(0, 0.32f, 0);
            shade.transform.localScale    = new Vector3(0.12f, 0.07f, 0.12f);
            Object.DestroyImmediate(shade.GetComponent<Collider>());
            shade.GetComponent<Renderer>().sharedMaterial =
                MakeEmissiveMaterial("LampShade", new Color(1f, 0.9f, 0.6f), 1f);
        }

        static void BuildChair(Vector3 pos, string name, Material frameMat, Material padMat)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            void Cube(string n, Vector3 lp, Vector3 ls, Material m)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = n; go.transform.SetParent(root.transform, false);
                go.transform.localPosition = lp; go.transform.localScale = ls;
                go.GetComponent<Renderer>().sharedMaterial = m;
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            // Legs
            float[] lx = { -0.2f, 0.2f, -0.2f, 0.2f };
            float[] lz = { -0.2f, -0.2f, 0.2f, 0.2f };
            for (int i = 0; i < 4; i++)
                Cube("Leg_" + i, new Vector3(lx[i], 0.2f, lz[i]), new Vector3(0.04f, 0.4f, 0.04f), frameMat);

            Cube("Seat",     new Vector3(0, 0.42f,  0f),    new Vector3(0.5f, 0.06f, 0.5f), padMat);
            Cube("SeatPad",  new Vector3(0, 0.46f,  0f),    new Vector3(0.48f, 0.04f, 0.48f), padMat);
            Cube("BackRail", new Vector3(0, 0.72f, -0.24f), new Vector3(0.5f, 0.06f, 0.04f), frameMat);
            Cube("BackTop",  new Vector3(0, 0.96f, -0.24f), new Vector3(0.5f, 0.06f, 0.04f), frameMat);
            Cube("BackPad",  new Vector3(0, 0.84f, -0.24f), new Vector3(0.44f, 0.28f, 0.04f), padMat);
            // Armrests
            Cube("ArmL", new Vector3(-0.27f, 0.58f, 0f), new Vector3(0.04f, 0.04f, 0.45f), frameMat);
            Cube("ArmR", new Vector3( 0.27f, 0.58f, 0f), new Vector3(0.04f, 0.04f, 0.45f), frameMat);
        }

        static void BuildStudentDeskRow(Vector3 center, Material woodMat, int count)
        {
            float spacing = 2.2f;
            float startX  = -((count - 1) * spacing) / 2f;
            var padMat = MakePBRMaterial("StuPad", new Color(0.22f, 0.16f, 0.1f), 0f, 0.1f);

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * spacing;
                var deskRoot = new GameObject($"StudentDesk_{i}_{center.z:F0}");

                // Desk top
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "Top"; top.transform.SetParent(deskRoot.transform, false);
                top.transform.position   = center + new Vector3(x, 0.76f, 0);
                top.transform.localScale = new Vector3(1.1f, 0.05f, 0.6f);
                Object.DestroyImmediate(top.GetComponent<Collider>());
                top.GetComponent<Renderer>().sharedMaterial = woodMat;

                // Desk body
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body"; body.transform.SetParent(deskRoot.transform, false);
                body.transform.position   = center + new Vector3(x, 0.38f, 0);
                body.transform.localScale = new Vector3(1.1f, 0.7f, 0.55f);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = woodMat;

                // Chair
                BuildChair(center + new Vector3(x, 0, -0.6f), $"StuChair_{i}", woodMat, padMat);
            }
        }

        static void BuildBookshelf(Vector3 pos, Material woodMat)
        {
            var root = new GameObject("Bookshelf");
            root.transform.position = pos;

            Color[] bookColors =
            {
                new Color(0.7f, 0.15f, 0.1f),  new Color(0.15f, 0.3f, 0.65f),
                new Color(0.1f, 0.45f, 0.15f), new Color(0.65f, 0.55f, 0.1f),
                new Color(0.45f, 0.1f, 0.5f),  new Color(0.6f, 0.3f, 0.1f),
                new Color(0.1f, 0.45f, 0.5f),  new Color(0.7f, 0.7f, 0.6f),
            };

            void Panel(string n, Vector3 lp, Vector3 ls)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = n; g.transform.SetParent(root.transform, false);
                g.transform.localPosition = lp; g.transform.localScale = ls;
                g.GetComponent<Renderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(g.GetComponent<Collider>());
            }

            Panel("Back",   new Vector3(0.15f, 1.2f, 0),      new Vector3(0.08f, 2.4f, 1.2f));
            Panel("Bottom", new Vector3(0, 0.04f, 0),          new Vector3(0.4f, 0.08f, 1.2f));
            Panel("Top",    new Vector3(0, 2.42f, 0),          new Vector3(0.4f, 0.08f, 1.2f));
            Panel("ShelfA", new Vector3(0, 0.78f, 0),          new Vector3(0.38f, 0.05f, 1.15f));
            Panel("ShelfB", new Vector3(0, 1.56f, 0),          new Vector3(0.38f, 0.05f, 1.15f));
            Panel("SideL",  new Vector3(0, 1.2f, -0.55f),     new Vector3(0.4f, 2.4f, 0.08f));
            Panel("SideR",  new Vector3(0, 1.2f,  0.55f),     new Vector3(0.4f, 2.4f, 0.08f));

            // Books on shelves
            float[] shelfY = { 0.22f, 1.0f, 1.78f };
            foreach (var sy in shelfY)
            {
                float xOff = -0.45f;
                int bIdx = 0;
                while (xOff < 0.42f && bIdx < bookColors.Length)
                {
                    float bW = Random.Range(0.05f, 0.1f);
                    float bH = Random.Range(0.18f, 0.3f);
                    var bk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bk.name = "Book";
                    bk.transform.SetParent(root.transform, false);
                    bk.transform.localPosition = new Vector3(-bW / 2f, sy + bH / 2f, xOff + bW / 2f);
                    bk.transform.localScale    = new Vector3(0.08f, bH, bW);
                    bk.transform.rotation      = Quaternion.Euler(0, 0, Random.Range(-3f, 3f));
                    Object.DestroyImmediate(bk.GetComponent<Collider>());
                    bk.GetComponent<Renderer>().sharedMaterial =
                        MakePBRMaterial("Book_" + bIdx, bookColors[bIdx % bookColors.Length], 0f, 0.3f);
                    xOff += bW + 0.01f;
                    bIdx++;
                }
            }
        }

        static GameObject CreateExamSettingsCanvas(Vector3 pos)
        {
            var canvasGO = new GameObject("ExamSettingsPanel");
            canvasGO.transform.position = pos;
            // Face south (toward entering player) so text is readable on entry.
            canvasGO.transform.rotation = Quaternion.Euler(0, 180f, 0);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 520);
            canvasGO.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // Dark background
            var bg = new GameObject("BG"); bg.transform.SetParent(canvasGO.transform, false);
            bg.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.1f, 0.95f);
            Stretch(bg.GetComponent<RectTransform>());

            // Amber top bar
            AddUIBar(canvasGO.transform, new Color(0.55f, 0.35f, 0.08f), new Vector2(0, -22f), new Vector2(0, 44f));

            AddTMPLabel(canvasGO.transform, "PRACTICE EXAM", 54, FontStyles.Bold,
                Color.white, new Vector2(0, -80f), new Vector2(-40f, 70f));

            AddTMPLabel(canvasGO.transform,
                "Default: Computer Science · English · Easy\n" +
                "Walk to the chair and press trigger to begin.\n" +
                "Or press BEGIN EXAM to send exam context first.",
                24, FontStyles.Normal, new Color(0.85f, 0.78f, 0.6f),
                new Vector2(0, -200f), new Vector2(-60f, 120f));

            // Info rows
            string[] rows =
            {
                "Subject:     Computer Science",
                "Language:  English",
                "Difficulty:   Easy",
            };
            float rowY = -300f;
            foreach (var row in rows)
            {
                AddTMPLabel(canvasGO.transform, row, 28, FontStyles.Normal,
                    new Color(0.9f, 0.85f, 0.7f), new Vector2(0, rowY), new Vector2(-60f, 40f));
                rowY -= 48f;
            }

            // BEGIN EXAM button
            var btnGO = new GameObject("BeginButton");
            btnGO.transform.SetParent(canvasGO.transform, false);
            btnGO.AddComponent<Image>().color = new Color(0.18f, 0.52f, 0.18f);
            var btn = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
            btnRT.sizeDelta = new Vector2(480, 90);
            btnRT.anchoredPosition = new Vector2(0, 65f);

            var btnTxt = new GameObject("Label"); btnTxt.transform.SetParent(btnGO.transform, false);
            var t = btnTxt.AddComponent<TextMeshProUGUI>();
            t.text = "BEGIN EXAM"; t.fontSize = 36; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            Stretch(btnTxt.GetComponent<RectTransform>());

            // ExamSettingsPanel — dropdowns intentionally null → defaults to CS/English/Easy
            var panel = canvasGO.AddComponent<Nexus.ExamSettingsPanel>();
            panel.confirmButton = btn;
            panel.rootPanel     = canvasGO;
            return canvasGO;
        }

        static GameObject CreateExamFinishCanvas(Vector3 pos)
        {
            var go = new GameObject("ExamFinishPanel");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 180f, 0);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 320);
            go.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>().color = new Color(0.05f, 0.1f, 0.05f, 0.95f);
            Stretch(bg.GetComponent<RectTransform>());

            AddUIBar(go.transform, new Color(0.1f, 0.5f, 0.15f), new Vector2(0, -22f), new Vector2(0, 44f));
            AddTMPLabel(go.transform, "EXAM COMPLETE", 52, FontStyles.Bold,
                Color.white, new Vector2(0, -90f), new Vector2(-40f, 65f));
            AddTMPLabel(go.transform, "Would you like to finish or retake?", 26, FontStyles.Normal,
                new Color(0.85f, 1f, 0.85f), new Vector2(0, -170f), new Vector2(-40f, 40f));

            // Finish button
            var finishGO = new GameObject("FinishButton"); finishGO.transform.SetParent(go.transform, false);
            finishGO.AddComponent<Image>().color = new Color(0.15f, 0.5f, 0.15f);
            var finishBtn = finishGO.AddComponent<Button>();
            var frt = finishGO.GetComponent<RectTransform>();
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0f);
            frt.sizeDelta = new Vector2(240, 80); frt.anchoredPosition = new Vector2(-140f, 55f);
            var fLbl = new GameObject("L"); fLbl.transform.SetParent(finishGO.transform, false);
            var fT = fLbl.AddComponent<TextMeshProUGUI>();
            fT.text = "FINISH"; fT.fontSize = 34; fT.fontStyle = FontStyles.Bold;
            fT.alignment = TextAlignmentOptions.Center; fT.color = Color.white;
            Stretch(fLbl.GetComponent<RectTransform>());

            // Retake button
            var retakeGO = new GameObject("RetakeButton"); retakeGO.transform.SetParent(go.transform, false);
            retakeGO.AddComponent<Image>().color = new Color(0.5f, 0.35f, 0.05f);
            var retakeBtn = retakeGO.AddComponent<Button>();
            var rrt = retakeGO.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.sizeDelta = new Vector2(240, 80); rrt.anchoredPosition = new Vector2(140f, 55f);
            var rLbl = new GameObject("L"); rLbl.transform.SetParent(retakeGO.transform, false);
            var rT = rLbl.AddComponent<TextMeshProUGUI>();
            rT.text = "RETAKE"; rT.fontSize = 34; rT.fontStyle = FontStyles.Bold;
            rT.alignment = TextAlignmentOptions.Center; rT.color = Color.white;
            Stretch(rLbl.GetComponent<RectTransform>());

            var comp = go.AddComponent<Nexus.ExamFinishPanel>();
            comp.finishButton = finishBtn;
            comp.retakeButton = retakeBtn;
            go.SetActive(false);
            return go;
        }



        static TMP_Text AddTMPLabel(Transform parent, string text, int fontSize,
                                    FontStyles style, Color color, Vector2 aPos, Vector2 aSizeDelta)
        {
            var go = new GameObject("TMP_" + text.Substring(0, Mathf.Min(8, text.Length)));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            var rt = go.GetComponent<RectTransform>();
            // Stretch horizontally so aSizeDelta.x acts as padding (negative = margins from edges).
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(aSizeDelta.x, Mathf.Abs(aSizeDelta.y));
            rt.anchoredPosition = new Vector2(0f, aPos.y);
            return tmp;
        }

        static void AddUIBar(Transform parent, Color color, Vector2 aPos, Vector2 aSizeDelta)
        {
            var go = new GameObject("UIBar"); go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = aSizeDelta; rt.anchoredPosition = aPos;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // =========================================================
        // BUILDING SIGNS (Hub / UNISA Campus)
        // =========================================================

        static void BuildBuildingSign(Vector3 pos, Quaternion rot, string signLabel,
                                       string description, Color glowColor)
        {
            var root = new GameObject("BuildingSign_" + signLabel.Replace("\n","_").Replace(" ",""));
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Sign post
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post"; post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0, 1.2f, 0);
            post.transform.localScale    = new Vector3(0.08f, 1.2f, 0.08f);
            Object.DestroyImmediate(post.GetComponent<Collider>());
            post.GetComponent<Renderer>().sharedMaterial =
                MakePBRMaterial("SignPost", new Color(0.6f, 0.55f, 0.45f), 0.5f, 0.4f);

            // Sign board
            var boardGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardGO.name = "SignBoard"; boardGO.transform.SetParent(root.transform, false);
            boardGO.transform.localPosition = new Vector3(0, 2.6f, 0);
            boardGO.transform.localScale    = new Vector3(1.8f, 0.9f, 0.12f);
            Object.DestroyImmediate(boardGO.GetComponent<Collider>());
            var boardMat = MakePBRMaterial("SignBoard_" + signLabel, new Color(0.12f, 0.14f, 0.22f), 0.1f, 0.6f);
            boardMat.EnableKeyword("_EMISSION");
            boardMat.SetColor("_EmissionColor", glowColor * 0.4f);
            boardGO.GetComponent<Renderer>().sharedMaterial = boardMat;

            // Sign label (front face)
            CreateWorldText3D(pos + new Vector3(0, 2.6f, 0.1f), signLabel, 0.2f, glowColor * 1.5f, bold: true);

            // Trigger for proximity popup
            var trigGO = new GameObject("SignTrigger");
            trigGO.transform.SetParent(root.transform, false);
            trigGO.transform.localPosition = Vector3.zero;
            var col = trigGO.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 2.5f;

            // Info popup (world-space TMP billboard)
            var popup = new GameObject("InfoPopup");
            popup.transform.SetParent(root.transform, false);
            popup.transform.localPosition = new Vector3(0, 3.8f, 0);
            var popupTMP = popup.AddComponent<TextMeshPro>();
            popupTMP.text = $"<b>{signLabel.Replace("\n"," ")}</b>\n{description}";
            popupTMP.fontSize = 0.18f;
            popupTMP.color = Color.white;
            popupTMP.alignment = TextAlignmentOptions.Center;
            popupTMP.overflowMode = TextOverflowModes.Overflow;
            popupTMP.enableWordWrapping = true;
            var popRT = popup.GetComponent<RectTransform>();
            if (popRT) popRT.sizeDelta = new Vector2(3f, 2f);
            popup.SetActive(false);

            // Wire BuildingSign component
            var sign = root.AddComponent<Nexus.BuildingSign>();
            sign.buildingName = signLabel.Replace("\n", " ");
            sign.description  = description;
            sign.signRenderer = boardGO.GetComponent<Renderer>();
            sign.infoPopup    = popup;
            sign.infoText     = popupTMP;
            sign.signColor    = glowColor;
        }

        // =========================================================
        // WHITEBOARD
        // =========================================================

        static void BuildWhiteboard(Vector3 pos)
        {
            var root = new GameObject("SharedWhiteboard");

            // Board surface
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board"; board.transform.SetParent(root.transform, false);
            board.transform.position   = pos;
            board.transform.localScale = new Vector3(0.05f, 1.6f, 2.4f);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.GetComponent<Renderer>().sharedMaterial =
                MakePBRMaterial("Whiteboard", new Color(0.97f, 0.97f, 0.95f), 0f, 0.25f);

            // Frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame"; frame.transform.SetParent(root.transform, false);
            frame.transform.position   = pos;
            frame.transform.localScale = new Vector3(0.06f, 1.72f, 2.52f);
            Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial =
                MakePBRMaterial("WBFrame", new Color(0.3f, 0.22f, 0.12f), 0.3f, 0.3f);

            // Label
            CreateWorldText3D(pos + new Vector3(-0.06f, 0.6f, 0),
                "STUDY COORDINATION", 0.1f, new Color(0.2f, 0.35f, 0.65f), bold: true, rotY: 90f);

            // Accent glow strip on top of board
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "GlowTop"; strip.transform.SetParent(root.transform, false);
            strip.transform.position   = pos + new Vector3(-0.04f, 0.9f, 0);
            strip.transform.localScale = new Vector3(0.03f, 0.04f, 2.4f);
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            strip.GetComponent<Renderer>().sharedMaterial =
                MakeEmissiveMaterial("WBGlow", new Color(0.3f, 0.7f, 1f), 1.5f);
        }

        // =========================================================
        // WANDERING STUDENT HELPER
        // =========================================================

        static void CreateWanderingStudent(Vector3 pos, string goName, Color color,
                                            string prefabPath, Vector3[] waypoints)
        {
            var npc = CreateNPCPlaceholder(pos, goName, color, prefabPath, "Untagged", false);
            var wander = npc.AddComponent<Nexus.WanderingNPC>();
            wander.waypoints = waypoints;
        }

        // =========================================================
        // NPC PLACEMENT
        // =========================================================

        static GameObject CreateNPCPlaceholder(Vector3 pos, string goName, Color capsuleColor,
                                                string convaiPrefabPath, string tag,
                                                bool attachFollow, string characterId = "")
        {
            var convaiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(convaiPrefabPath);

            GameObject root;
            if (convaiPrefab != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(convaiPrefab);
                root.name = goName;
                root.transform.position = pos;

                // Wire ConvaiNPC Character ID via reflection (field name: characterID)
                if (!string.IsNullOrEmpty(characterId))
                {
                    var npc = root.GetComponent<Convai.Scripts.Runtime.Core.ConvaiNPC>()
                           ?? root.GetComponentInChildren<Convai.Scripts.Runtime.Core.ConvaiNPC>(true);
                    if (npc != null)
                    {
                        var field = npc.GetType().GetField("characterID",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(npc, characterId);
                            Debug.Log($"[NexusSceneBuilder] Set characterID={characterId} on {goName}");
                        }
                    }
                }

                Debug.Log($"[NexusSceneBuilder] Placed real Convai NPC: {convaiPrefabPath}");
            }
            else
            {
                root = new GameObject(goName);
                root.transform.position = pos;
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Visual";
                capsule.transform.SetParent(root.transform, false);
                capsule.transform.localPosition = new Vector3(0, 1f, 0);
                capsule.transform.localScale    = new Vector3(0.45f, 1f, 0.45f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
                capsule.GetComponent<Renderer>().sharedMaterial =
                    MakePBRMaterial("NPC_" + goName, capsuleColor, 0.2f, 0.4f);

                // Subtle emissive outline
                var outline = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                outline.name = "Outline"; outline.transform.SetParent(root.transform, false);
                outline.transform.localPosition = new Vector3(0, 1f, 0);
                outline.transform.localScale    = new Vector3(0.48f, 1.01f, 0.48f);
                Object.DestroyImmediate(outline.GetComponent<Collider>());
                outline.GetComponent<Renderer>().sharedMaterial =
                    MakeEmissiveMaterial("NPC_Outline_" + goName, capsuleColor, 0.4f);

                // Name label
                CreateWorldText3D(pos + new Vector3(0, 2.6f, 0), goName, 0.18f, capsuleColor, bold: true);
                CreateWorldText3D(pos + new Vector3(0, 2.3f, 0),
                    "Add ConvaiNPC + Character ID", 0.1f,
                    new Color(1f, 0.85f, 0.2f));

                Debug.LogWarning($"[NexusSceneBuilder] Convai NPC prefab not found: {convaiPrefabPath}. Created capsule placeholder.");
            }

            if (!string.IsNullOrEmpty(tag))
                root.tag = tag;

            if (attachFollow)
                root.AddComponent<Nexus.SofiaFollowPlayer>();

            return root;
        }

        // =========================================================
        // WORLD-SPACE TEXT (TextMeshPro — no Canvas needed)
        // =========================================================

        static void CreateWorldText3D(Vector3 pos, string text, float fontSize,
                                       Color color, bool bold = false, float rotY = 0f)
        {
            string safeName = text.Replace("\n", "").Replace(" ", "_");
            safeName = safeName.Substring(0, Mathf.Min(16, safeName.Length));
            var go  = new GameObject("Label_" + safeName);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text         = text;
            tmp.fontSize     = fontSize;
            tmp.color        = color;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableWordWrapping = false;
            if (bold) tmp.fontStyle = FontStyles.Bold;

            // Size the rect large enough to never clip
            float w = Mathf.Max(fontSize * 20f, 4f);
            float h = Mathf.Max(fontSize * 6f,  1f);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(w, h);
        }

        // =========================================================
        // MATERIAL HELPERS  (URP-aware, PBR-correct)
        // =========================================================

        static Material MakeEuropeMapMaterial()
        {
            // Prefer an actual texture if present; otherwise fall back to a flat "map-like" tint.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(EuropeMapTexturePath);
            var mat = MakePBRMaterial("EuropeMapSurface", new Color(0.55f, 0.72f, 0.5f), 0.0f, 0.35f);
            if (tex == null) return mat;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            return mat;
        }

        static Material MakePBRMaterial(string name, Color baseColor,
                                         float metallic = 0f, float smoothness = 0.5f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor"))     mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color"))         mat.SetColor("_Color",     baseColor);
            if (mat.HasProperty("_Metallic"))      mat.SetFloat("_Metallic",  metallic);
            if (mat.HasProperty("_Smoothness"))    mat.SetFloat("_Smoothness",smoothness);
            if (mat.HasProperty("_Glossiness"))    mat.SetFloat("_Glossiness",smoothness);
            return mat;
        }

        static Material MakeEmissiveMaterial(string name, Color color, float intensity)
        {
            var mat = MakePBRMaterial(name, color * 0.3f, 0f, 0.1f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * intensity);
            }
            return mat;
        }

        // =========================================================
        // BUILD SETTINGS
        // =========================================================

        static void AddScenesToBuildSettings()
        {
            var scenes   = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string[] add = {
                $"{ScenesFolder}/{HubScene}.unity",
                $"{ScenesFolder}/{Room2Scene}.unity",
                $"{ScenesFolder}/{Room3Scene}.unity",
            };
            foreach (var s in add)
                if (!scenes.Exists(x => x.path == s))
                    scenes.Add(new EditorBuildSettingsScene(s, true));

            // Ensure Unisa campus scene is in build settings so portals can load it.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(UnisaScenePath) != null &&
                !scenes.Exists(x => x.path == UnisaScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(UnisaScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
