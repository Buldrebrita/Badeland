using System.Collections.Generic;
using System.IO;
using System.Linq;
using Badeland.CameraSystem;
using Badeland.Player;
using Badeland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Builds the first area of Chapter 2, "The Waking Shore": the party wakes up inside the creature, in a living
    /// world with its own light, water and weather, all of it breathing. A compact, enclosed area: a shore with glowing
    /// mushrooms, a hold-a-button gate, the shrine of the people who gave themselves to the creature, a breathing lake
    /// and a great door at the far side, with the notes of two lost travellers along the way.
    /// Menu: Badeland > Create Chapter 2 - Inside the Monster (first area). See docs/CHAPTER2.md and docs/STORY.md.
    /// </summary>
    public static class ChapterTwoBuilder
    {
        public const string SceneName = "Inside_01_WakingShore";
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";
        const string PrefabFolder = "Assets/_Project/Prefabs/Characters";

        // The playable area: x from -40 to 38, z from -24 to 24. The shore is in the west, the lake in the middle and the far bank in the east.
        const float MinX = -40f, MaxX = 38f, HalfZ = 24f;

        static readonly Color Flesh = new Color(0.42f, 0.17f, 0.2f);
        static readonly Color Sand = new Color(0.5f, 0.34f, 0.31f);
        static readonly Color Stone = new Color(0.3f, 0.27f, 0.3f);
        static readonly Color Bone = new Color(0.82f, 0.78f, 0.66f);

        [MenuItem("Badeland/Create Chapter 2 - Inside the Monster (first area)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(PrefabFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            string settingsPath = SettingsFolder + "/MovementSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            var rng = new System.Random(2027);
            var glowLights = new List<Light>();

            BuildMood();
            BuildGround();
            BuildBoundary();
            BuildWalls(rng);
            BuildLivingWalls(rng, glowLights);
            BuildRibs();
            var lake = BuildLake();
            BuildDock();
            BuildGateAndPuzzle();
            BuildMushrooms(rng, glowLights);
            BuildShrine();
            BuildNotes();
            BuildJellyfish(rng);
            BuildExitDoor(glowLights);
            BuildBreathing(lake, glowLights);

            // ---- Where the party wakes up, the camera, and the on-screen text.
            var spawnRoot = new GameObject("Wake Up Spots");
            var spawns = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var spot = new GameObject("Wake Up Spot " + i).transform;
                spot.SetParent(spawnRoot.transform);
                spot.position = new Vector3(-36f + (i % 2) * 2.5f, 0.2f, -2f + (i / 2) * 3f);
                spot.rotation = Quaternion.Euler(0f, 90f, 0f);
                spawns.Add(spot);
            }

            var safety = new GameObject("Fall Respawn").AddComponent<FallRespawn>();
            safety.fallbackPoint = spawns[0];

            var offlinePlayer = S3SceneBuilder.CreatePlayerObject(settings);
            offlinePlayer.name = "OfflinePlayer";
            string offlinePath = PrefabFolder + "/OfflinePlayer.prefab";
            AssetDatabase.DeleteAsset(offlinePath);
            var offlinePrefab = PrefabUtility.SaveAsPrefabAsset(offlinePlayer, offlinePath);
            Object.DestroyImmediate(offlinePlayer);

            var cam = Camera.main;
            var rigObject = new GameObject("CameraRig");
            rigObject.AddComponent<IsoCameraRig>();
            if (cam != null)
            {
                cam.transform.SetParent(rigObject.transform, false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.01f, 0.03f, 0.04f);
            }

            var startObject = new GameObject("Chapter Start");
            var start = startObject.AddComponent<ChapterStart>();
            start.spawnPoints = spawns.ToArray();
            start.offlinePlayerPrefab = offlinePrefab;
            start.chapterTitle = "Chapter 2";
            start.chapterSubtitle = "Inside";

            new GameObject("Info Hud").AddComponent<InfoHud>();

            string scenePath = SceneFolder + "/" + SceneName + ".unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AddToBuildSettings(scenePath);

            Debug.Log("Badeland: Chapter 2 area created at " + scenePath + ". Press Play. (Online, the monster fight leads here by itself.)");
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ mood

        static void BuildMood()
        {
            // Dark and close, lit by the creature's own cold green-blue light and the glowing plants.
            var sunObject = GameObject.Find("Directional Light");
            if (sunObject != null)
            {
                var sun = sunObject.GetComponent<Light>();
                sun.color = new Color(0.35f, 0.8f, 0.75f);
                sun.intensity = 0.5f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(65f, 30f, 0f);
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.18f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.09f, 0.11f);
            RenderSettings.fogDensity = 0.012f;
        }

        // ------------------------------------------------------------------ ground, boundary, lake

        static void BuildGround()
        {
            // The shore in the west, the far bank in the east, and the lake bed between them.
            S3SceneBuilder.Box("Shore", new Vector3(-20f, -0.5f, 0f), new Vector3(40f, 1f, HalfZ * 2f), Sand);
            S3SceneBuilder.Box("Far Bank", new Vector3(33f, -0.5f, 0f), new Vector3(10f, 1f, HalfZ * 2f), Sand);
            S3SceneBuilder.Box("Lake Bed", new Vector3(14f, -7.5f, 0f), new Vector3(28f, 1f, HalfZ * 2f), new Color(0.12f, 0.1f, 0.14f));
        }

        // An unbroken invisible wall all around the area, too high to jump over: you cannot fall off or leave it.
        static void BuildBoundary()
        {
            var root = new GameObject("Boundary").transform;
            Rim(root, "Rim North", new Vector3((MinX + MaxX) * 0.5f, 6f, HalfZ + 0.5f), new Vector3(MaxX - MinX + 4f, 12f, 1f));
            Rim(root, "Rim South", new Vector3((MinX + MaxX) * 0.5f, 6f, -HalfZ - 0.5f), new Vector3(MaxX - MinX + 4f, 12f, 1f));
            Rim(root, "Rim West", new Vector3(MinX - 0.5f, 6f, 0f), new Vector3(1f, 12f, HalfZ * 2f + 2f));
            Rim(root, "Rim East", new Vector3(MaxX + 0.5f, 6f, 0f), new Vector3(1f, 12f, HalfZ * 2f + 2f));
        }

        static void Rim(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        static GameObject BuildLake()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Lake";
            water.transform.position = new Vector3(14f, -3.8f, 0f);
            water.transform.localScale = new Vector3(28f, 7f, HalfZ * 2f); // the top face (y = -0.3) is the surface
            GrayboxMaterials.TintWater(water, new Color(0.04f, 0.32f, 0.38f, 0.8f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();
            return water;
        }

        static void BuildDock()
        {
            // A rotting wooden dock leading out into the lake. At high tide it is underwater.
            var wood = new Color(0.32f, 0.22f, 0.15f);
            for (int i = 0; i < 11; i++)
                S3SceneBuilder.Box("Dock Plank", new Vector3(1.5f + i * 1.0f, 0.05f, 0f), new Vector3(0.9f, 0.3f, 3.6f), wood);
        }

        // ------------------------------------------------------------------ the living walls

        static void BuildWalls(System.Random rng)
        {
            // Bumpy, fleshy walls all the way round, made of big overlapping spheres. Solid, and in two rows.
            for (int row = 0; row < 2; row++)
            {
                float y = 5f + row * 14f;
                for (float x = MinX - 12f; x <= MaxX + 12f; x += 11f)
                {
                    Wall(new Vector3(x, y, HalfZ + 15f), rng);
                    Wall(new Vector3(x, y, -HalfZ - 15f), rng);
                }
                for (float z = -HalfZ - 4f; z <= HalfZ + 4f; z += 11f)
                {
                    Wall(new Vector3(MinX - 15f, y, z), rng);
                    Wall(new Vector3(MaxX + 15f, y, z), rng);
                }
            }
        }

        static void Wall(Vector3 position, System.Random rng)
        {
            float size = 22f + (float)rng.NextDouble() * 6f;
            Vector3 jitter = new Vector3((float)rng.NextDouble() * 3f - 1.5f, (float)rng.NextDouble() * 3f - 1.5f, (float)rng.NextDouble() * 3f - 1.5f);
            float tone = 0.85f + (float)rng.NextDouble() * 0.3f;
            S3SceneBuilder.Ball("Wall", position + jitter, size, Flesh * tone, true);
        }

        // What makes it feel like the inside of something alive: glowing veins, pulsing organs and tendons.
        static void BuildLivingWalls(System.Random rng, List<Light> lights)
        {
            var veinColor = new Color(0.95f, 0.15f, 0.3f);

            // Veins running across the walls, glowing red-pink.
            var veins = new GameObject("Veins").transform;
            for (int i = 0; i < 46; i++)
            {
                int side = rng.Next(4);
                float along = (float)rng.NextDouble();
                float height = 1f + (float)rng.NextDouble() * 15f;
                Vector3 position, direction;
                switch (side)
                {
                    case 0: position = new Vector3(Mathf.Lerp(MinX, MaxX, along), height, HalfZ + 1.2f); direction = new Vector3(1f, (float)rng.NextDouble() - 0.4f, 0f); break;
                    case 1: position = new Vector3(Mathf.Lerp(MinX, MaxX, along), height, -HalfZ - 1.2f); direction = new Vector3(1f, (float)rng.NextDouble() - 0.4f, 0f); break;
                    case 2: position = new Vector3(MinX - 1.2f, height, Mathf.Lerp(-HalfZ, HalfZ, along)); direction = new Vector3(0f, (float)rng.NextDouble() - 0.4f, 1f); break;
                    default: position = new Vector3(MaxX + 1.2f, height, Mathf.Lerp(-HalfZ, HalfZ, along)); direction = new Vector3(0f, (float)rng.NextDouble() - 0.4f, 1f); break;
                }

                float length = 8f + (float)rng.NextDouble() * 12f;
                var vein = S3SceneBuilder.Cyl("Vein", position, new Vector3(0.3f, length * 0.5f, 0.3f), veinColor);
                GrayboxMaterials.TintGlow(vein, veinColor, 1.2f);
                Object.DestroyImmediate(vein.GetComponent<Collider>());
                vein.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                vein.transform.SetParent(veins, true);
                if (i % 9 == 0) lights.Add(AddGlow(position + (side == 0 ? Vector3.back : side == 1 ? Vector3.forward : side == 2 ? Vector3.right : Vector3.left) * 2f, veinColor, 1.6f, 14f));
            }

            // Big organs bulging out of the walls that swell with each breath.
            var organs = new GameObject("Organs").transform;
            Vector3[] spots =
            {
                new Vector3(12f, 7f, HalfZ + 3f), new Vector3(-24f, 9f, HalfZ + 3f), new Vector3(26f, 8f, -HalfZ - 3f),
                new Vector3(MinX - 3f, 8f, -8f), new Vector3(MaxX + 3f, 10f, 12f),
            };
            foreach (var spot in spots)
            {
                var organ = S3SceneBuilder.Ball("Organ", spot, 10f, new Color(0.55f, 0.18f, 0.4f), false);
                GrayboxMaterials.TintGlow(organ, new Color(0.55f, 0.18f, 0.4f), 0.5f);
                organ.AddComponent<BreathScale>().amount = 0.08f;
                organ.transform.SetParent(organs, true);
            }
        }

        static void BuildRibs()
        {
            // Huge pale ribs arching over the whole area, far above the camera.
            for (int r = 0; r < 5; r++)
            {
                float x = -34f + r * 17f;
                var root = new GameObject("Rib");
                const int segments = 16;
                const float span = 30f, height = 24f;
                for (int i = 0; i < segments; i++)
                {
                    float a0 = Mathf.PI * i / segments, a1 = Mathf.PI * (i + 1) / segments;
                    Vector3 p0 = new Vector3(x, Mathf.Sin(a0) * height, Mathf.Cos(a0) * span);
                    Vector3 p1 = new Vector3(x, Mathf.Sin(a1) * height, Mathf.Cos(a1) * span);
                    Vector3 mid = (p0 + p1) * 0.5f;
                    float thickness = 2.2f - Mathf.Abs(i - segments * 0.5f) * 0.02f;

                    var piece = S3SceneBuilder.Box("Rib Piece", mid, new Vector3(thickness, thickness * 0.8f, (p1 - p0).magnitude * 1.1f), Bone * 0.8f);
                    Object.DestroyImmediate(piece.GetComponent<Collider>());
                    piece.transform.rotation = Quaternion.LookRotation(p1 - p0, Vector3.up);
                    piece.transform.SetParent(root.transform, true);
                }
            }
        }

        // ------------------------------------------------------------------ the gate and the hold-a-button puzzle

        static void BuildGateAndPuzzle()
        {
            // A wall across the shore with a gap, closed by a bone gate. The walls are kept low (too high to jump, but not
            // tall enough to hide the players from the camera, which has no see-through fade for walls yet).
            S3SceneBuilder.Box("Choke Wall North", new Vector3(-16f, 1.5f, 15.5f), new Vector3(3f, 3f, 17f), Stone);
            S3SceneBuilder.Box("Choke Wall South", new Vector3(-16f, 1.5f, -15.5f), new Vector3(3f, 3f, 17f), Stone);

            var plateObject = S3SceneBuilder.Box("Pressure Plate", new Vector3(-28f, 0.12f, 17f), new Vector3(5f, 0.24f, 5f), new Color(0.35f, 0.3f, 0.4f));
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.visual = plateObject.GetComponent<Renderer>();
            plateObject.GetComponent<BoxCollider>().isTrigger = true;

            var gateObject = S3SceneBuilder.Box("Bone Gate", new Vector3(-16f, 1.5f, 0f), new Vector3(3f, 3f, 14f), Bone);
            var gate = gateObject.AddComponent<SlidingGate>();
            gate.plates = new[] { plate };
            gate.openOffset = new Vector3(0f, -3.4f, 0f); // sinks into the ground when the plate is held down

            // The heavy glowing stone for anyone doing it alone.
            var stone = S3SceneBuilder.Ball("Heavy Stone", new Vector3(-31f, 0.8f, -12f), 1.6f, new Color(1f, 0.6f, 0.2f), true);
            GrayboxMaterials.TintGlow(stone, new Color(1f, 0.55f, 0.15f), 0.9f);
            stone.AddComponent<Carryable>();
            AddGlow(stone.transform.position + Vector3.up * 1.2f, new Color(1f, 0.6f, 0.25f), 3.5f, 9f);
        }

        // ------------------------------------------------------------------ mushrooms and lights

        static void BuildMushrooms(System.Random rng, List<Light> lights)
        {
            Color[] glowColors =
            {
                new Color(0.15f, 0.95f, 0.9f), new Color(0.9f, 0.3f, 0.85f), new Color(0.4f, 1f, 0.5f),
            };

            var root = new GameObject("Mushrooms").transform;
            for (int i = 0; i < 34; i++)
            {
                float x = -39f + (float)rng.NextDouble() * 37f;
                float z = -23f + (float)rng.NextDouble() * 46f;
                if (Mathf.Abs(x + 16f) < 4f) continue; // not in the doorway of the gate
                if (x > -39f && x < -33f && Mathf.Abs(z) < 5f) continue; // keep the wake-up spot clear

                Color glow = glowColors[rng.Next(glowColors.Length)];
                float height = 0.8f + (float)rng.NextDouble() * 2.6f;
                float cap = 1.1f + (float)rng.NextDouble() * 1.8f;

                var stem = S3SceneBuilder.Cyl("Mushroom Stem", new Vector3(x, height * 0.5f, z), new Vector3(0.25f, height * 0.5f, 0.25f), new Color(0.85f, 0.85f, 0.8f));
                Object.DestroyImmediate(stem.GetComponent<Collider>());
                stem.transform.SetParent(root, true);

                var top = S3SceneBuilder.Ball("Mushroom Cap", new Vector3(x, height, z), cap, glow, false);
                top.transform.localScale = new Vector3(cap, cap * 0.55f, cap);
                GrayboxMaterials.TintGlow(top, glow, 1.6f);
                top.transform.SetParent(root, true);

                if (i % 5 == 0) lights.Add(AddGlow(new Vector3(x, height + 0.8f, z), glow, 2.2f, 14f));
            }
        }

        static Light AddGlow(Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject("Glow Light");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        // ------------------------------------------------------------------ the shrine of the people who came by choice

        static void BuildShrine()
        {
            // Beyond the gate, on the sand: a circle of standing stones around an altar, with folded robes and bowls.
            // A long time ago a group of people gave themselves to the creature on purpose.
            Vector3 center = new Vector3(-7f, 0f, -9f);
            var glow = new Color(0.2f, 0.9f, 0.9f);

            S3SceneBuilder.Box("Altar", center + new Vector3(0f, 0.6f, 0f), new Vector3(3.4f, 1.2f, 2f), Stone);
            S3SceneBuilder.Box("Altar Top", center + new Vector3(0f, 1.3f, 0f), new Vector3(3.8f, 0.3f, 2.4f), Stone * 1.2f);
            var altarLight = S3SceneBuilder.Cyl("Altar Spiral", center + new Vector3(0f, 1.5f, 0f), new Vector3(1.1f, 0.05f, 1.1f), glow);
            GrayboxMaterials.TintGlow(altarLight, glow, 2f);
            Object.DestroyImmediate(altarLight.GetComponent<Collider>());
            AddGlow(center + new Vector3(0f, 3f, 0f), glow, 2.5f, 14f);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * 7f, 0f, Mathf.Sin(angle) * 7f);
                var stoneColumn = S3SceneBuilder.Box("Standing Stone", p + Vector3.up * 1.7f, new Vector3(1f, 3.4f, 1f), Stone);
                stoneColumn.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                var mark = S3SceneBuilder.Cyl("Spiral", p + Vector3.up * 2.2f + (center - p).normalized * 0.55f, new Vector3(0.7f, 0.05f, 0.7f), glow);
                GrayboxMaterials.TintGlow(mark, glow, 1.5f);
                Object.DestroyImmediate(mark.GetComponent<Collider>());
                mark.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(center.x - p.x, 0f, center.z - p.z)); // the disc faces the altar
            }

            // Forty-one folded robes in rows, each with a small bowl. Nobody fought; they stayed.
            var robe = new Color(0.25f, 0.38f, 0.55f);
            for (int i = 0; i < 12; i++)
            {
                float row = i / 4, col = i % 4;
                Vector3 p = center + new Vector3(-5.5f + col * 1.3f, 0.06f, 3.2f + row * 1.4f);
                var cloth = S3SceneBuilder.Box("Folded Robe", p, new Vector3(0.9f, 0.12f, 0.6f), robe);
                Object.DestroyImmediate(cloth.GetComponent<Collider>());
                var bowl = S3SceneBuilder.Cyl("Offering Bowl", p + new Vector3(0.7f, 0.1f, 0f), new Vector3(0.35f, 0.08f, 0.35f), Bone);
                Object.DestroyImmediate(bowl.GetComponent<Collider>());
            }

            // Three carvings around the altar tell why they came.
            Tablet(center + new Vector3(-4f, 0f, -3.5f), "The first carving",
                "In the age before ships, the Tide-Keepers saw a light sink into the sea.\n\nIt did not drown. It swam. It was the size of a mountain, and it was singing.\n\nThey knew the song. It was the sound that waits at the edge of sleep. They knew then that it had come from the Sacred Place: the place where the sea was born, deeper than any light.\n\nAnd they named it the Bearer, because it carried the song.");
            Tablet(center + new Vector3(0f, 0f, -4.5f), "The second carving",
                "The Bearer is older than counting. Inside its body it carries a whole world, and it carries it so that the world does not end.\n\nBut the Bearer is tired, and it cannot carry what it does not have. Rivers must be fed. Forests must be fed. The old world must be mended with new things.\n\nSo the Keepers made a choice. They went down to it, freely, singing, and they were taken in.\n\nThey were not forced. They went because they loved the world that it carried.");
            Tablet(center + new Vector3(4f, 0f, -3.5f), "The third carving",
                "We do not give ourselves to be eaten. We give ourselves to be kept.\n\nHere we rest. Here we are part of the world that is carried.\n\nIf you are reading this, and you are afraid, then you did not come by choice, and you should not be here. We are so sorry.\n\nWe did not know the sea would send others.\n\nWe did not know it would not let them go.");
        }

        static void Tablet(Vector3 position, string title, string text)
        {
            var slab = S3SceneBuilder.Box("Carved Tablet", position + Vector3.up * 1.1f, new Vector3(1.6f, 2.2f, 0.3f), Stone * 1.1f);
            slab.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var inscription = S3SceneBuilder.Box("Inscription", position + Vector3.up * 1.2f + new Vector3(0f, 0f, 0.17f), new Vector3(1.1f, 1.4f, 0.04f), new Color(0.2f, 0.9f, 0.9f));
            GrayboxMaterials.TintGlow(inscription, new Color(0.2f, 0.9f, 0.9f), 1.2f);
            Object.DestroyImmediate(inscription.GetComponent<Collider>());
            inscription.transform.SetParent(slab.transform, true);

            var note = slab.AddComponent<ReadableNote>();
            note.title = title;
            note.text = text;
            note.radius = 3.6f;
        }

        // ------------------------------------------------------------------ the notes of the two travellers

        static void BuildNotes()
        {
            Note(new Vector3(-35f, 0.15f, 4.5f), "A torn page",
                "DAY 1.\n\nWe came in through the water, Marit and me. Nobody remembers the dark part, only the cold, and then the warm.\n\nThe ground here is warm. It moves a little, like something asleep. I told Marit it was only the tide. She laughed. She still laughs, today.\n\nThere are lights everywhere, small and blue and green. Marit says someone must have put them here. I say someone must be coming to find us.\n\nWe will walk until we find the way out. How big can a cave be?");

            Note(new Vector3(-24f, 0.15f, -14f), "Marit's notebook",
                "DAY 4.\n\nMarit says the walls breathe. I counted, to be sure: ten seconds in, ten out. Every time the lake climbs the shore, we both feel sick, as if our own chests were going up and down with it.\n\nWe found a heavy stone that glows. It is warm, and it does not mind being carried. We used it to hold a stone gate open. I think that gate was built to be opened by two people standing together.\n\nSomeone built this place on purpose. Someone planned for visitors.\n\nMarit has started humming along with the heartbeat without noticing.");

            Note(new Vector3(-11f, 0.15f, 3f), "A page, pinned down by a stone",
                "DAY 9.\n\nThe stone houses beyond the gate. The altar. The robes. Marit counted them: forty-one. She did not speak for a whole day after that.\n\nThey are not bones. They are only robes, folded very neatly, each with a bowl beside it. Nobody fought. Nobody ran.\n\nThey stayed.\n\nI read the carvings. They came here because they believed. They thought this was holy. I wonder what they would think of us, wet and frightened, trying so hard to leave.");

            Note(new Vector3(-3f, 0.15f, 7f), "Marit, again",
                "DAY 14.\n\nMarit does not sleep. She walks the shore all night, telling the walls to let us out. Sometimes she shouts. I tell her it can hear her. She says: GOOD.\n\nThe jellyfish gather when she shouts. They go quiet when I hold her hand.\n\nShe says she can feel the creature thinking, and that it is afraid.\n\nI do not know which of us is more frightened.");

            Note(new Vector3(1f, 0.25f, 2.4f), "A page, wet through",
                "DAY 17.\n\nShe is gone.\n\nShe woke before the lights rose, and walked straight out on to the dock, though the lake was high. I called and called. She did not turn around. She waded in up to her chest, and then she swam, and the lake carried her out toward the far door.\n\nI searched the whole shore for her. All day.\n\nI keep telling myself she is only ahead of me. I keep telling myself she found the way, and that she is waiting.");

            Note(new Vector3(30f, 0.15f, -8f), "Found on the far bank",
                "DAY 30? DAY 40?\n\nThere was a scarf on the far bank, folded very neatly, just like the robes. I did not want to touch it. I touched it.\n\nI have started talking to the mushrooms. They are better listeners than I expected.\n\nLast night I saw Marit standing at the edge of the light, by the ruins. She looked at me and put a finger to her lips. I called her name, and she was a jellyfish, floating.\n\nThe quiet here is very big. I counted a thousand breaths today, only to hear something.");

            Note(new Vector3(35f, 0.15f, 5f), "The last page",
                "DAY ??\n\nI see her every day now. She stands at the great door, and she waits for me.\n\nI know the door is only a door. I know the old ones wrote DO NOT BE LOUD. IT HEARS THE RIVER. But when the lake is high, the door glows, and I can feel the sun on the other side. I can smell the sea.\n\nTomorrow I will walk through. I am not afraid any more. Marit says it is only a few steps. Marit says everyone gets out in the end.\n\nI will not take the lamp. She says we will not need it.\n\n(The page ends here. The rest is blank.)");

            // Faint figures at the far end: what the lonely narrator thought they saw. They fade away when you come close.
            PlaceMirage(new Vector3(33f, 0f, 3.5f));
            PlaceMirage(new Vector3(31f, 0f, -3f));
            PlaceMirage(new Vector3(-1f, 0f, 9f));
        }

        static void Note(Vector3 position, string title, string text)
        {
            var paper = S3SceneBuilder.Box("Note", position, new Vector3(0.8f, 0.05f, 1.1f), new Color(0.95f, 0.92f, 0.8f));
            GrayboxMaterials.TintGlow(paper, new Color(0.95f, 0.92f, 0.8f), 0.6f);
            Object.DestroyImmediate(paper.GetComponent<Collider>());
            paper.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            var note = paper.AddComponent<ReadableNote>();
            note.title = title;
            note.text = text;
            AddGlow(position + Vector3.up * 1f, new Color(1f, 0.92f, 0.7f), 1.2f, 6f);
        }

        static void PlaceMirage(Vector3 position)
        {
            var root = new GameObject("Mirage");
            root.transform.position = position;
            var color = new Color(0.8f, 0.9f, 1f, 0.5f);

            var body = S3SceneBuilder.Cyl("Figure Body", position + Vector3.up * 0.95f, new Vector3(0.7f, 0.95f, 0.7f), color);
            GrayboxMaterials.TintWater(body, color);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, true);

            var head = S3SceneBuilder.Ball("Figure Head", position + Vector3.up * 2.2f, 0.7f, color, false);
            GrayboxMaterials.TintWater(head, color);
            head.transform.SetParent(root.transform, true);

            root.AddComponent<Mirage>();
        }

        // ------------------------------------------------------------------ jellyfish

        static void BuildJellyfish(System.Random rng)
        {
            Color[] colors = { new Color(0.4f, 0.9f, 1f, 0.5f), new Color(1f, 0.5f, 0.9f, 0.5f), new Color(0.6f, 1f, 0.7f, 0.5f) };
            for (int i = 0; i < 12; i++)
            {
                Color c = colors[rng.Next(colors.Length)];
                float x = -36f + (float)rng.NextDouble() * 70f;
                float z = -20f + (float)rng.NextDouble() * 40f;
                float y = 5f + (float)rng.NextDouble() * 7f;

                var jelly = S3SceneBuilder.Ball("Jellyfish", new Vector3(x, y, z), 2.2f, c, false);
                jelly.transform.localScale = new Vector3(2.2f, 1.5f, 2.2f);
                GrayboxMaterials.TintWater(jelly, c);
                for (int t = 0; t < 4; t++)
                {
                    float angle = t * Mathf.PI * 0.5f;
                    var tentacle = S3SceneBuilder.Cyl("Tentacle", new Vector3(x + Mathf.Cos(angle) * 0.5f, y - 1.6f, z + Mathf.Sin(angle) * 0.5f), new Vector3(0.08f, 1.0f, 0.08f), new Color(c.r, c.g, c.b, 1f));
                    Object.DestroyImmediate(tentacle.GetComponent<Collider>());
                    tentacle.transform.SetParent(jelly.transform, true);
                }

                var drift = jelly.AddComponent<Drifter>();
                drift.radius = 2.5f + (float)rng.NextDouble() * 4f;
                drift.speed = 0.08f + (float)rng.NextDouble() * 0.12f;
                drift.phase = (float)rng.NextDouble() * 6f;
                drift.bobAmplitude = 0.5f;
            }
        }

        // ------------------------------------------------------------------ the great door

        static void BuildExitDoor(List<Light> lights)
        {
            // A huge glowing ring set into the east wall, like an iris that has not yet opened.
            var violet = new Color(0.65f, 0.3f, 1f);
            var ring = S3SceneBuilder.Cyl("Great Door", new Vector3(MaxX + 0.2f, 6f, 0f), new Vector3(14f, 0.8f, 14f), new Color(0.2f, 0.15f, 0.3f));
            ring.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var core = S3SceneBuilder.Cyl("Door Light", new Vector3(MaxX - 0.6f, 6f, 0f), new Vector3(10f, 0.4f, 10f), violet);
            core.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GrayboxMaterials.TintGlow(core, violet, 2.2f);
            Object.DestroyImmediate(core.GetComponent<Collider>());
            lights.Add(AddGlow(new Vector3(MaxX - 4f, 6f, 0f), violet, 4f, 26f));

            var exit = new GameObject("Area Exit");
            exit.transform.position = new Vector3(MaxX - 4f, 2f, 0f);
            var box = exit.AddComponent<BoxCollider>();
            box.size = new Vector3(6f, 4f, 16f);
            exit.AddComponent<AreaExit>().message = "The great door shivers, and opens a little...\n\nIt does not lead out.\nIt leads deeper.\n\n(This is as far as the first area goes. More to come.)";
        }

        // ------------------------------------------------------------------ breathing

        static void BuildBreathing(GameObject lake, List<Light> lights)
        {
            var breath = new GameObject("The Creature Breathes");
            breath.AddComponent<BreathCycle>();

            var water = lake.AddComponent<BreathWater>();
            water.amplitude = 1.2f;

            var pulse = breath.AddComponent<BreathLight>();
            pulse.lights = lights.ToArray();
        }
    }
}
