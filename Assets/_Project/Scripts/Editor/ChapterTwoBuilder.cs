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
    /// Builds the first area of Chapter 2, "The Waking Shore": the party wakes up inside the monster. A dark cavern with
    /// glowing mushrooms, ribs arching overhead, a lake that rises and falls with the creature's breath, a hold-a-button
    /// puzzle, ruins and notes from an earlier expedition, and a great door at the far shore.
    /// Menu: Badeland > Create Chapter 2 - Inside the Monster (first area). See docs/CHAPTER2.md.
    /// </summary>
    public static class ChapterTwoBuilder
    {
        public const string SceneName = "Inside_01_WakingShore";
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";
        const string PrefabFolder = "Assets/_Project/Prefabs/Characters";

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
            BuildWalls(rng);
            BuildRibs();
            var lake = BuildLake();
            BuildDock();
            BuildGateAndPuzzle();
            BuildMushrooms(rng, glowLights);
            BuildRuinsAndNotes();
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
                spot.position = new Vector3(-48f + (i % 2) * 2.5f, 0.2f, -2f + (i / 2) * 3f);
                spot.rotation = Quaternion.Euler(0f, 90f, 0f);
                spawns.Add(spot);
            }

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
            // Dark, close and slightly teal. The only real light comes from the glowing plants.
            var sunObject = GameObject.Find("Directional Light");
            if (sunObject != null)
            {
                var sun = sunObject.GetComponent<Light>();
                sun.color = new Color(0.45f, 0.65f, 0.75f);
                sun.intensity = 0.25f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(60f, 20f, 0f);
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.14f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.02f, 0.07f, 0.09f);
            RenderSettings.fogDensity = 0.011f;
        }

        // ------------------------------------------------------------------ ground, lake, walls, ribs

        static void BuildGround()
        {
            // The shore on the west side, and the bank on the far east side. The lake lies between them.
            S3SceneBuilder.Box("Shore", new Vector3(-25f, -0.5f, 0f), new Vector3(70f, 1f, 72f), Sand);
            S3SceneBuilder.Box("Far Bank", new Vector3(55f, -0.5f, 0f), new Vector3(10f, 1f, 72f), Sand);
            S3SceneBuilder.Box("Lake Bed", new Vector3(30f, -7.5f, 0f), new Vector3(40f, 1f, 72f), new Color(0.12f, 0.1f, 0.14f));
        }

        static GameObject BuildLake()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Lake";
            water.transform.position = new Vector3(30f, -3.8f, 0f);
            water.transform.localScale = new Vector3(40f, 7f, 72f); // the top face (y = -0.3) is the surface
            GrayboxMaterials.TintWater(water, new Color(0.04f, 0.32f, 0.38f, 0.8f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();
            return water;
        }

        static void BuildDock()
        {
            // A rotting wooden dock leading out into the lake. At high tide it is underwater.
            var wood = new Color(0.32f, 0.22f, 0.15f);
            for (int i = 0; i < 12; i++)
                S3SceneBuilder.Box("Dock Plank", new Vector3(11f + i * 1.0f, 0.05f, 0f), new Vector3(0.9f, 0.3f, 4f), wood);
        }

        static void BuildWalls(System.Random rng)
        {
            // Bumpy fleshy walls all around, made of big overlapping spheres. Solid, so nobody walks through them.
            for (int row = 0; row < 2; row++)
            {
                float y = 6f + row * 14f;
                for (float x = -68f; x <= 72f; x += 15f)
                {
                    Wall(new Vector3(x, y, 46f), rng);
                    Wall(new Vector3(x, y, -46f), rng);
                }
                for (float z = -34f; z <= 34f; z += 15f)
                {
                    Wall(new Vector3(-74f, y, z), rng);
                    Wall(new Vector3(76f, y, z), rng);
                }
            }
        }

        static void Wall(Vector3 position, System.Random rng)
        {
            float size = 24f + (float)rng.NextDouble() * 8f;
            Vector3 jitter = new Vector3((float)rng.NextDouble() * 4f - 2f, (float)rng.NextDouble() * 3f - 1.5f, (float)rng.NextDouble() * 4f - 2f);
            float tone = 0.85f + (float)rng.NextDouble() * 0.3f;
            var blob = S3SceneBuilder.Ball("Wall", position + jitter, size, Flesh * tone, true);
        }

        static void BuildRibs()
        {
            // Huge pale ribs arching over the whole cavern, far above the camera.
            for (int r = 0; r < 7; r++)
            {
                float x = -57f + r * 19f;
                var root = new GameObject("Rib");
                const int segments = 16;
                const float span = 40f, height = 30f;
                for (int i = 0; i < segments; i++)
                {
                    float a0 = Mathf.PI * i / segments, a1 = Mathf.PI * (i + 1) / segments;
                    Vector3 p0 = new Vector3(x, Mathf.Sin(a0) * height, Mathf.Cos(a0) * span);
                    Vector3 p1 = new Vector3(x, Mathf.Sin(a1) * height, Mathf.Cos(a1) * span);
                    Vector3 mid = (p0 + p1) * 0.5f;
                    float thickness = 2.4f - Mathf.Abs(i - segments * 0.5f) * 0.02f;

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
            S3SceneBuilder.Box("Choke Wall North", new Vector3(-8f, 1.5f, 21.5f), new Vector3(3f, 3f, 29f), Stone);
            S3SceneBuilder.Box("Choke Wall South", new Vector3(-8f, 1.5f, -21.5f), new Vector3(3f, 3f, 29f), Stone);

            var plateObject = S3SceneBuilder.Box("Pressure Plate", new Vector3(-24f, 0.12f, 24f), new Vector3(5f, 0.24f, 5f), new Color(0.35f, 0.3f, 0.4f));
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.visual = plateObject.GetComponent<Renderer>();
            plateObject.GetComponent<BoxCollider>().isTrigger = true;

            var gateObject = S3SceneBuilder.Box("Bone Gate", new Vector3(-8f, 1.5f, 0f), new Vector3(3f, 3f, 14f), Bone);
            var gate = gateObject.AddComponent<SlidingGate>();
            gate.plates = new[] { plate };
            gate.openOffset = new Vector3(0f, -3.4f, 0f); // sinks into the ground when the plate is held down

            // The heavy glowing stone for anyone doing it alone.
            var stone = S3SceneBuilder.Ball("Heavy Stone", new Vector3(-38f, 0.8f, -12f), 1.6f, new Color(1f, 0.6f, 0.2f), true);
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
            for (int i = 0; i < 48; i++)
            {
                float x = -56f + (float)rng.NextDouble() * 62f;
                float z = -32f + (float)rng.NextDouble() * 64f;
                if (Mathf.Abs(x + 8f) < 4f) continue; // not in the doorway of the gate
                if (x > -45f && x < -36f && Mathf.Abs(z) < 6f) continue; // keep the wake-up spot clear

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

                if (i % 6 == 0) lights.Add(AddGlow(new Vector3(x, height + 0.8f, z), glow, 2.2f, 15f));
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

        // ------------------------------------------------------------------ ruins, relics and notes

        static void BuildRuinsAndNotes()
        {
            // A note where you wake up.
            Note(new Vector3(-43f, 0.15f, 3.5f), "A torn page",
                "DAY 1.\n\nWe came in through the water, all of us. Nobody remembers the dark part.\nThe ground is warm. It moves a little, like something asleep.\n\nMarit is counting the lights. She says someone must have put them here.");

            // Mushroom grove note.
            Note(new Vector3(-26f, 0.15f, -14f), "Marit's notebook",
                "DAY 4.\n\nMarit says the walls breathe. I counted: ten seconds in, ten out.\nEvery time the lake climbs the shore I feel sick. We should not stay by the water when it rises.\n\nThe stone is heavy but it does not mind being carried. It is warm too.");

            // The ruins beyond the gate: stone houses, with the same spiral over every door.
            var wall = Stone;
            for (int i = 0; i < 3; i++)
            {
                float x = -2f + i * 4.5f;
                S3SceneBuilder.Box("Ruin Pillar", new Vector3(x, 1.8f, 12f), new Vector3(1.2f, 3.6f, 1.2f), wall);
                S3SceneBuilder.Box("Ruin Pillar", new Vector3(x + 2.8f, 1.8f, 12f), new Vector3(1.2f, 3.6f, 1.2f), wall);
                S3SceneBuilder.Box("Ruin Lintel", new Vector3(x + 1.4f, 4.0f, 12f), new Vector3(4.4f, 0.8f, 1.4f), wall);
                var spiral = S3SceneBuilder.Cyl("Spiral", new Vector3(x + 1.4f, 2.4f, 11.2f), new Vector3(1.0f, 0.1f, 1.0f), new Color(0.2f, 0.9f, 0.9f));
                GrayboxMaterials.TintGlow(spiral, new Color(0.2f, 0.9f, 0.9f), 1.5f);
                Object.DestroyImmediate(spiral.GetComponent<Collider>());
                spiral.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            Note(new Vector3(3f, 0.15f, 8f), "A page, held down by a stone",
                "DAY 9.\n\nWe found the stone houses. Someone built here, long before us.\nAbove every door the same spiral. An eye? A mouth?\n\nMarit has stopped sleeping.");

            // Things from the world above that should not be here. We were not the first to come in through the water.
            var ring = S3SceneBuilder.Cyl("Lifebuoy", new Vector3(6f, 0.15f, -7f), new Vector3(2.4f, 0.15f, 2.4f), new Color(0.95f, 0.95f, 0.95f));
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            var stripe = S3SceneBuilder.Cyl("Lifebuoy Stripe", new Vector3(6f, 0.2f, -7f), new Vector3(2.45f, 0.1f, 1.2f), new Color(0.9f, 0.15f, 0.15f));
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            var sign = S3SceneBuilder.Box("Old Sign", new Vector3(2f, 1.1f, -9f), new Vector3(3.6f, 1.2f, 0.2f), Color.white);
            sign.transform.rotation = Quaternion.Euler(0f, 20f, 12f);
            var slab = S3SceneBuilder.Box("Deflated Inflatable", new Vector3(9f, 0.12f, -10f), new Vector3(3f, 0.24f, 2f), new Color(1f, 0.4f, 0.7f));
            Object.DestroyImmediate(slab.GetComponent<Collider>());
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

        // ------------------------------------------------------------------ jellyfish

        static void BuildJellyfish(System.Random rng)
        {
            Color[] colors = { new Color(0.4f, 0.9f, 1f, 0.5f), new Color(1f, 0.5f, 0.9f, 0.5f), new Color(0.6f, 1f, 0.7f, 0.5f) };
            for (int i = 0; i < 16; i++)
            {
                Color c = colors[rng.Next(colors.Length)];
                float x = -50f + (float)rng.NextDouble() * 100f;
                float z = -30f + (float)rng.NextDouble() * 60f;
                float y = 6f + (float)rng.NextDouble() * 8f;

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
                drift.radius = 3f + (float)rng.NextDouble() * 6f;
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
            var ring = S3SceneBuilder.Cyl("Great Door", new Vector3(59f, 6f, 0f), new Vector3(14f, 0.8f, 14f), new Color(0.2f, 0.15f, 0.3f));
            ring.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var core = S3SceneBuilder.Cyl("Door Light", new Vector3(58.4f, 6f, 0f), new Vector3(10f, 0.4f, 10f), violet);
            core.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GrayboxMaterials.TintGlow(core, violet, 2.2f);
            Object.DestroyImmediate(core.GetComponent<Collider>());
            lights.Add(AddGlow(new Vector3(54f, 6f, 0f), violet, 4f, 28f));

            Note(new Vector3(52f, 0.15f, 4f), "The last page",
                "DAY ?\n\nThe big door in the east opens when it wants.\nThe old ones wrote on the stones: DO NOT BE LOUD. IT HEARS THE RIVER.\n\nI think it is not hungry any more.\nI think it is listening.");

            var exit = new GameObject("Area Exit");
            exit.transform.position = new Vector3(54f, 2f, 0f);
            var box = exit.AddComponent<BoxCollider>();
            box.size = new Vector3(8f, 4f, 16f);
            exit.AddComponent<AreaExit>().message = "The great door stirs...\n\n(This is as far as the first area goes. More to come.)";
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
