using System.Collections.Generic;
using System.IO;
using Badeland.CameraSystem;
using Badeland.Player;
using Badeland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Chapter 2, second room: "The Sunken Harbour". A bigger cavern with a lagoon, a wrecked sailing ship wedged in it,
    /// an empty lantern-lit village on a terrace, a waterfall and a pool of acid or two. The way on is a stone gate
    /// that opens only while two buttons are held at once (one in the village, one on the ship), so a friend, or the
    /// heavy stone, is needed. Reuses the cavern pieces (walls, roof, ground, plants) from <see cref="ChapterTwoBuilder"/>.
    /// Menu: Badeland > Create Chapter 2 - Room 2 (The Sunken Harbour).
    /// </summary>
    public static partial class ChapterTwoBuilder
    {
        public const string Room2SceneName = "Inside_02_SunkenHarbour";

        // The lagoon basin, the village terrace and the waterfall's plunge pool.
        const float LagMinX = -6f, LagMaxX = 22f, LagHalfZ = 8f, LagBlend = 9f, LagDepth = -4f;
        const float TerMinX = -14f, TerMaxX = 24f, TerMinZ = 13f, TerMaxZ = 25f, TerHeight = 2.5f;
        static readonly Vector2 FallPool = new Vector2(18f, -22.5f);
        const float ShipDeck = 2.2f;

        static readonly Vector3[] Room2Pools =
        {
            new Vector3(30f, -14f, 3f), new Vector3(38f, 13f, 3f), new Vector3(-26f, -18f, 3f), new Vector3(-30f, 16f, 2.5f),
        };

        static readonly Vector3[] Room2FlatSpots =
        {
            new Vector3(-42f, 0f, 5f), new Vector3(-22f, -8f, 3.5f), new Vector3(42f, 0f, 4f),
        };

        static float Room2Height(float x, float z)
        {
            float h = 0.45f * Mathf.Sin(x * 0.2f + 2f) * Mathf.Cos(z * 0.17f)
                    + 0.3f * Mathf.Sin(x * 0.12f - z * 0.15f + 1f)
                    + 0.2f * Mathf.Sin(z * 0.33f + x * 0.06f)
                    + 0.1f * Mathf.Sin(x * 0.9f + z * 0.7f) * Mathf.Sin(z * 0.8f - x * 0.3f);

            // The lagoon.
            float dx = Mathf.Max(Mathf.Max(LagMinX - x, 0f), x - LagMaxX);
            float dz = Mathf.Max(Mathf.Abs(z) - LagHalfZ, 0f);
            h = Mathf.Lerp(LagDepth, h, Smooth(0f, LagBlend, Mathf.Sqrt(dx * dx + dz * dz)));

            // The village terrace: a raised, level shelf along the north shore.
            float tx = Mathf.Max(Mathf.Max(TerMinX - x, 0f), x - TerMaxX);
            float tz = Mathf.Max(Mathf.Max(TerMinZ - z, 0f), z - TerMaxZ);
            h = Mathf.Lerp(TerHeight, h, Smooth(0f, 5f, Mathf.Sqrt(tx * tx + tz * tz)));

            // The plunge pool under the waterfall.
            float fd = Mathf.Sqrt((x - FallPool.x) * (x - FallPool.x) + (z - FallPool.y) * (z - FallPool.y));
            h = Mathf.Lerp(-2.5f, h, Smooth(3.5f, 6.5f, fd));

            foreach (var flat in FlatSpots)
            {
                float d = Mathf.Sqrt((x - flat.x) * (x - flat.x) + (z - flat.y) * (z - flat.y));
                h = Mathf.Lerp(0.3f, h, Smooth(flat.z, flat.z + 4f, d));
            }
            foreach (var pool in Pools)
            {
                float pd = Mathf.Sqrt((x - pool.x) * (x - pool.x) + (z - pool.y) * (z - pool.y));
                h -= 0.8f * (1f - Smooth(pool.z * 0.7f, pool.z * 1.4f, pd));
            }
            return h;
        }

        static bool Room2Reserved(float x, float z)
        {
            if (x > -48f && x < -36f && Mathf.Abs(z) < 7f) return true;        // the wake-up area
            if (x > TerMinX - 3f && x < TerMaxX + 3f && z > TerMinZ - 3f && z < TerMaxZ + 3f) return true; // the village
            if (x > 29f && x < 38f && Mathf.Abs(z) < 10f) return true;         // the gate
            if (x > -10f && x < 26f && Mathf.Abs(z) < 12f) return true;        // lagoon shore and the ship
            if (new Vector2(x - FallPool.x, z - FallPool.y).magnitude < 9f) return true;
            return NearPoolOrFlat(x, z);
        }

        [MenuItem("Badeland/Create Chapter 2 - Room 2 (The Sunken Harbour)")]
        public static void BuildRoom2()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(PrefabFolder);
            MeshFolder = "Assets/_Project/Art/Environment/Inside2"; // its own meshes and materials, so room 1 is left alone
            GrayboxMaterials.CavernSuffix = "_Room2";
            if (Directory.Exists(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
            Directory.CreateDirectory(MeshFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            string settingsPath = SettingsFolder + "/MovementSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            // This room's own shape and rules.
            Cx = 0f; Rx = 52f; Rz = 32f;
            HeightFn = Room2Height;
            ReservedFn = Room2Reserved;
            Pools = Room2Pools;
            FlatSpots = Room2FlatSpots;
            GrayboxMaterials.ResetTexturedMaterials();

            var rng = new System.Random(4242);
            var glowLights = new List<Light>();

            BuildMood();
            RenderSettings.fogColor = new Color(0.03f, 0.07f, 0.14f);
            RenderSettings.ambientLight = new Color(0.2f, 0.24f, 0.4f);
            BuildGround();
            BuildWalls();
            BuildLivingWalls(rng, glowLights);
            BuildEye();
            BuildProps(rng, glowLights);
            BuildMushrooms(rng, glowLights);
            BuildJellyfish(rng);

            var lagoon = BuildLagoon();
            BuildShip(glowLights);
            BuildVillage(glowLights);
            BuildWaterfall();
            BuildPools(glowLights);
            BuildHarbourGate();
            BuildRoom2Checkpoints();
            BuildRoom2Chests();
            BuildRoom2Notes();
            BuildExitDoor(glowLights, "", "The great door opens a crack.\n\nBeyond it, something is breathing very slowly.\n\n(This is as far as the second room goes. More to come.)");
            BuildBreathing(lagoon, glowLights);

            // ---- Wake-up spots, safety nets, camera, HUD.
            var spawnRoot = new GameObject("Wake Up Spots");
            var spawns = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var spot = new GameObject("Wake Up Spot " + i).transform;
                spot.SetParent(spawnRoot.transform);
                float x = -42f + (i % 2) * 2.5f, z = -2f + (i / 2) * 3f;
                spot.position = new Vector3(x, Height(x, z) + 0.3f, z);
                spot.rotation = Quaternion.Euler(0f, 90f, 0f);
                spawns.Add(spot);
            }

            var bounds = new GameObject("Area Bounds").AddComponent<AreaBounds>();
            bounds.centerX = Cx; bounds.halfX = Rx; bounds.halfZ = Rz;

            var safety = new GameObject("Fall Respawn").AddComponent<FallRespawn>();
            safety.fallbackPoint = spawns[0];

            var offlinePlayer = S3SceneBuilder.CreatePlayerObject(settings);
            offlinePlayer.name = "OfflinePlayer";
            string offlinePath = PrefabFolder + "/OfflinePlayer_Room2.prefab";
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
                cam.backgroundColor = new Color(0.01f, 0.03f, 0.05f);
            }

            var startObject = new GameObject("Chapter Start");
            var start = startObject.AddComponent<ChapterStart>();
            start.spawnPoints = spawns.ToArray();
            start.offlinePlayerPrefab = offlinePrefab;
            start.chapterTitle = "Chapter 2";
            start.chapterSubtitle = "The Sunken Harbour";

            new GameObject("Info Hud").AddComponent<InfoHud>();

            string scenePath = SceneFolder + "/" + Room2SceneName + ".unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AddToBuildSettings(scenePath);

            Debug.Log("Badeland: Chapter 2 room 2 created at " + scenePath + ". Press Play to test it on its own. (Room 1's great door leads here.)");
        }

        // ------------------------------------------------------------------ small build helpers

        static GameObject Wood(string name, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            var go = S3SceneBuilder.Box(name, position, scale, color);
            GrayboxMaterials.TintWood(go, color);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        static GameObject Rock(string name, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            var go = S3SceneBuilder.Box(name, position, scale, color);
            GrayboxMaterials.TintStone(go, color);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        static GameObject Visual(GameObject go, Transform parent)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            go.transform.SetParent(parent, true);
            return go;
        }

        // ------------------------------------------------------------------ the lagoon

        static GameObject BuildLagoon()
        {
            float minX = LagMinX - 7.5f, maxX = LagMaxX + 7.5f, halfZ = LagHalfZ + 7.5f;
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Lagoon";
            water.transform.position = new Vector3((minX + maxX) * 0.5f, -3.8f, 0f);
            water.transform.localScale = new Vector3(maxX - minX, 7f, halfZ * 2f);
            GrayboxMaterials.TintWater(water, new Color(0.06f, 0.42f, 0.8f, 0.8f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();
            return water;
        }

        // ------------------------------------------------------------------ the shipwreck

        static void BuildShip(List<Light> lights)
        {
            var root = new GameObject("Shipwreck").transform;
            var wood = new Color(0.42f, 0.27f, 0.17f);
            var darkWood = new Color(0.3f, 0.19f, 0.12f);
            var cloth = new Color(0.88f, 0.82f, 0.66f);

            // The hull, wedged in the lagoon: a long deck you can walk on, with the sides raised.
            Wood("Hull", new Vector3(8f, 0.4f, 0f), new Vector3(22f, 3.6f, 7f), wood, root);                       // deck top at ShipDeck
            Wood("Keel", new Vector3(8f, -1.8f, 0f), new Vector3(20f, 1.2f, 4.5f), darkWood, root);
            Wood("Bow", new Vector3(20.5f, 0.9f, 0f), new Vector3(3f, 2.6f, 3.6f), wood, root);
            for (int side = -1; side <= 1; side += 2)
            {
                Wood("Bulwark", new Vector3(8f, ShipDeck + 0.6f, side * 3.3f), new Vector3(22f, 1.2f, 0.4f), darkWood, root);
                for (int i = 0; i < 6; i++)
                    Wood("Rib", new Vector3(-1.5f + i * 4.2f, 0.2f, side * 3.6f), new Vector3(0.35f, 3.4f, 0.3f), darkWood, root);
            }

            // The stern cabin.
            Wood("Cabin", new Vector3(16f, ShipDeck + 1.6f, 0f), new Vector3(5f, 3.2f, 6f), wood * 0.9f, root);
            Wood("Cabin Roof", new Vector3(16f, ShipDeck + 3.35f, 0f), new Vector3(5.8f, 0.3f, 6.8f), darkWood, root);
            Visual(Wood("Cabin Door", new Vector3(13.45f, ShipDeck + 1f, 0f), new Vector3(0.1f, 2f, 1.2f), new Color(0.15f, 0.1f, 0.07f)), root);
            Visual(Wood("Cabin Window", new Vector3(13.45f, ShipDeck + 2.2f, 1.9f), new Vector3(0.1f, 0.8f, 0.8f), new Color(0.2f, 0.4f, 0.5f)), root);

            // The masts: one standing with a ragged sail, one snapped and leaning.
            var mast = S3SceneBuilder.Cyl("Mast", new Vector3(3.5f, ShipDeck + 5.5f, 0f), new Vector3(0.6f, 5.5f, 0.6f), darkWood);
            GrayboxMaterials.TintWood(mast, darkWood);
            mast.transform.SetParent(root, true);
            Visual(Wood("Yard", new Vector3(3.5f, ShipDeck + 9f, 0f), new Vector3(0.3f, 0.3f, 6.5f), darkWood), root);
            Visual(Wood("Sail", new Vector3(3.7f, ShipDeck + 6.2f, 0f), new Vector3(0.08f, 5.4f, 5.6f), cloth), root);
            Visual(Wood("Torn Sail", new Vector3(3.7f, ShipDeck + 7.4f, 1.8f), new Vector3(0.08f, 1.8f, 1.6f), new Color(0.15f, 0.13f, 0.1f)), root);
            var broken = S3SceneBuilder.Cyl("Broken Mast", new Vector3(10f, ShipDeck + 3f, -1.5f), new Vector3(0.55f, 3.5f, 0.55f), darkWood);
            GrayboxMaterials.TintWood(broken, darkWood);
            broken.transform.rotation = Quaternion.Euler(18f, 0f, -22f);
            broken.transform.SetParent(root, true);
            Visual(Wood("Rope Net", new Vector3(8.5f, ShipDeck + 1.2f, 2.5f), new Vector3(0.05f, 1.6f, 1.8f), new Color(0.65f, 0.55f, 0.35f)), root);

            // A gangplank from the west shore up to the deck.
            var plank = Wood("Gangplank", new Vector3(-7.5f, 1.2f, 0f), new Vector3(9.4f, 0.3f, 3f), wood * 1.1f, root);
            plank.transform.rotation = Quaternion.Euler(0f, 0f, 12.5f);
            for (int side = -1; side <= 1; side += 2)
            {
                var rope = Wood("Gangplank Rail", new Vector3(-7.5f, 2.1f, side * 1.5f), new Vector3(9.4f, 0.08f, 0.08f), new Color(0.65f, 0.55f, 0.35f), root);
                rope.transform.rotation = Quaternion.Euler(0f, 0f, 12.5f);
            }

            // Barrels and crates on the deck, and a hanging lantern.
            Barrel(new Vector3(0f, ShipDeck, 2.4f), root);
            Barrel(new Vector3(1.2f, ShipDeck, 2.7f), root);
            Wood("Crate", new Vector3(0.2f, ShipDeck + 0.5f, -2.5f), new Vector3(1.2f, 1f, 1.2f), wood * 1.15f, root);
            Wood("Crate", new Vector3(12f, ShipDeck + 0.5f, 2.4f), new Vector3(1.1f, 1f, 1.1f), wood * 1.05f, root);
            var lamp = S3SceneBuilder.Ball("Ship Lantern", new Vector3(12.8f, ShipDeck + 2.6f, -2.8f), 0.45f, new Color(1f, 0.75f, 0.35f), false);
            GrayboxMaterials.TintGlow(lamp, new Color(1f, 0.75f, 0.35f), 1.2f);
            lamp.transform.SetParent(root, true);
            lights.Add(AddGlow(lamp.transform.position, new Color(1f, 0.7f, 0.35f), 2f, 11f, root));

            // Button B, on the deck.
            var plate = S3SceneBuilder.Box("Pressure Plate (ship)", new Vector3(7f, ShipDeck + 0.1f, 0f), new Vector3(3f, 0.4f, 3f), new Color(0.35f, 0.3f, 0.4f));
            GrayboxMaterials.TintStone(plate, new Color(0.35f, 0.3f, 0.4f));
            plate.transform.SetParent(root, true);
            ShipPlate = plate.AddComponent<PressurePlate>();
            ShipPlate.visual = plate.GetComponent<Renderer>();
        }

        static PressurePlate ShipPlate;

        static void Barrel(Vector3 position, Transform parent)
        {
            var color = new Color(0.45f, 0.3f, 0.18f);
            var barrel = S3SceneBuilder.Cyl("Barrel", position + Vector3.up * 0.55f, new Vector3(0.9f, 0.55f, 0.9f), color);
            GrayboxMaterials.TintWood(barrel, color);
            barrel.transform.SetParent(parent, true);
            var band = S3SceneBuilder.Cyl("Barrel Band", position + Vector3.up * 0.55f, new Vector3(0.95f, 0.08f, 0.95f), new Color(0.2f, 0.2f, 0.22f));
            Object.DestroyImmediate(band.GetComponent<Collider>());
            band.transform.SetParent(barrel.transform, true);
        }

        // ------------------------------------------------------------------ the empty village

        static void BuildVillage(List<Light> lights)
        {
            var root = new GameObject("Lantern Village").transform;

            Hut(new Vector3(-9f, TerHeight, 20f), 180f, root, lights);
            Hut(new Vector3(0f, TerHeight, 22f), 170f, root, lights);
            Hut(new Vector3(9f, TerHeight, 20.5f), 190f, root, lights);
            Hut(new Vector3(18f, TerHeight, 22f), 180f, root, lights);

            // The well in the middle.
            var stone = new Color(0.45f, 0.43f, 0.46f);
            var well = S3SceneBuilder.Cyl("Well", new Vector3(4.5f, TerHeight + 0.6f, 15.5f), new Vector3(2.2f, 0.6f, 2.2f), stone);
            GrayboxMaterials.TintStone(well, stone);
            well.transform.SetParent(root, true);
            var water = S3SceneBuilder.Cyl("Well Water", new Vector3(4.5f, TerHeight + 1.15f, 15.5f), new Vector3(1.6f, 0.05f, 1.6f), new Color(0.1f, 0.4f, 0.7f));
            Visual(water, root);
            Wood("Well Post", new Vector3(3.3f, TerHeight + 2.2f, 15.5f), new Vector3(0.25f, 2.2f, 0.25f), new Color(0.4f, 0.26f, 0.16f), root);
            Wood("Well Post", new Vector3(5.7f, TerHeight + 2.2f, 15.5f), new Vector3(0.25f, 2.2f, 0.25f), new Color(0.4f, 0.26f, 0.16f), root);
            var wellRoof = Wood("Well Roof", new Vector3(4.5f, TerHeight + 3.4f, 15.5f), new Vector3(3.2f, 0.25f, 1.8f), new Color(0.6f, 0.25f, 0.2f), root);
            wellRoof.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Lamp posts along the shore path, a notice board, crates and barrels.
            for (int i = 0; i < 6; i++) LampPost(new Vector3(-12f + i * 7f, TerHeight, 14.2f), root, lights);
            Wood("Notice Board", new Vector3(-12.5f, TerHeight + 1.6f, 16.5f), new Vector3(2.2f, 1.4f, 0.15f), new Color(0.45f, 0.3f, 0.18f), root);
            Wood("Notice Post", new Vector3(-12.5f, TerHeight + 0.8f, 16.5f), new Vector3(0.2f, 1.6f, 0.2f), new Color(0.35f, 0.22f, 0.13f), root);
            for (int i = 0; i < 5; i++)
            {
                Barrel(new Vector3(-13f + i * 1.1f, TerHeight, 24f), root);
                Wood("Crate", new Vector3(-12f + i * 1.9f, TerHeight + 0.5f, 22.6f), new Vector3(1.2f, 1f, 1.2f), new Color(0.5f, 0.34f, 0.2f) * (0.9f + i * 0.04f), root);
            }

            // A fishing boat pulled up on the shore, and nets hung to dry.
            var hull = Wood("Rowing Boat", new Vector3(-12f, 0.8f, 6f), new Vector3(1.8f, 0.8f, 5f), new Color(0.35f, 0.5f, 0.6f), root);
            hull.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            Visual(Wood("Net", new Vector3(14f, TerHeight + 1.8f, 24.2f), new Vector3(3f, 2.2f, 0.05f), new Color(0.6f, 0.55f, 0.4f)), root);

            // Button A and the heavy stone that can hold it down.
            var plateColor = new Color(0.35f, 0.3f, 0.4f);
            var step = Rock("Plate Step", new Vector3(-1f, TerHeight - 0.4f, 17f), new Vector3(4.6f, 1f, 4.6f), stone * 0.9f, root);
            var plate = Rock("Pressure Plate (village)", new Vector3(-1f, TerHeight - 0.2f + 0.1f, 17f), new Vector3(3.6f, 1f, 3.6f), plateColor, root);
            VillagePlate = plate.AddComponent<PressurePlate>();
            VillagePlate.visual = plate.GetComponent<Renderer>();

            var heavy = S3SceneBuilder.Ball("Heavy Stone", new Vector3(14f, TerHeight + 0.8f, 16.5f), 1.6f, new Color(1f, 0.6f, 0.2f), true);
            GrayboxMaterials.TintStone(heavy, new Color(0.75f, 0.5f, 0.3f));
            heavy.AddComponent<Carryable>();
        }

        static PressurePlate VillagePlate;

        static void Hut(Vector3 position, float yaw, Transform parent, List<Light> lights)
        {
            var root = new GameObject("Hut");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            var wall = new Color(0.55f, 0.38f, 0.24f);
            var roofColor = new Color(0.7f, 0.28f, 0.22f);
            var dark = new Color(0.18f, 0.12f, 0.08f);
            var warm = new Color(1f, 0.78f, 0.4f);

            void Local(string name, Vector3 lp, Vector3 scale, Color color, bool solid, float rotZ = 0f, bool wood = true)
            {
                var go = S3SceneBuilder.Box(name, position + lp, scale, color);
                if (wood) GrayboxMaterials.TintWood(go, color); else GrayboxMaterials.Tint(go, color);
                go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root.transform, true);
            }

            Local("Hut Wall", new Vector3(0f, 1.5f, 0f), new Vector3(5f, 3f, 4.4f), wall, true);
            Local("Hut Base", new Vector3(0f, 0.2f, 0f), new Vector3(5.4f, 0.4f, 4.8f), new Color(0.4f, 0.38f, 0.4f), true, 0f, false);
            Local("Roof Left", new Vector3(-1.3f, 3.65f, 0f), new Vector3(3.2f, 0.3f, 5.2f), roofColor, true, 33f);
            Local("Roof Right", new Vector3(1.3f, 3.65f, 0f), new Vector3(3.2f, 0.3f, 5.2f), roofColor, true, -33f);
            Local("Door", new Vector3(0f, 1f, 2.25f), new Vector3(1.1f, 2f, 0.12f), dark, false);
            Local("Window", new Vector3(-1.7f, 1.9f, 2.25f), new Vector3(0.9f, 0.9f, 0.1f), warm, false, 0f, false);
            Local("Window", new Vector3(1.7f, 1.9f, 2.25f), new Vector3(0.9f, 0.9f, 0.1f), warm, false, 0f, false);
            Local("Chimney", new Vector3(1.8f, 4.3f, -1.2f), new Vector3(0.7f, 1.6f, 0.7f), new Color(0.4f, 0.38f, 0.4f), true, 0f, false);

            var lantern = S3SceneBuilder.Ball("Door Lantern", position + new Vector3(1.0f, 2.3f, 2.5f), 0.4f, warm, false);
            GrayboxMaterials.TintGlow(lantern, warm, 1.0f);
            lantern.transform.SetParent(root.transform, true);
            lights.Add(AddGlow(lantern.transform.position + Vector3.forward * 0.3f, new Color(1f, 0.7f, 0.35f), 1.8f, 9f, root.transform));

            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f); // turned last: everything built above turns with the hut
        }

        static void LampPost(Vector3 position, Transform parent, List<Light> lights)
        {
            var iron = new Color(0.15f, 0.15f, 0.18f);
            var post = S3SceneBuilder.Cyl("Lamp Post", position + Vector3.up * 1.4f, new Vector3(0.14f, 1.4f, 0.14f), iron);
            post.transform.SetParent(parent, true);
            var bulb = S3SceneBuilder.Ball("Lamp", position + Vector3.up * 3f, 0.5f, new Color(1f, 0.8f, 0.45f), false);
            GrayboxMaterials.TintGlow(bulb, new Color(1f, 0.8f, 0.45f), 1f);
            bulb.transform.SetParent(parent, true);
            lights.Add(AddGlow(bulb.transform.position, new Color(1f, 0.72f, 0.38f), 1.6f, 8f, parent));
        }

        // ------------------------------------------------------------------ waterfall

        static void BuildWaterfall()
        {
            // A sheet of falling water down the south wall into its own plunge pool.
            float a = Mathf.Atan2(-22.5f / Rz, (FallPool.x - Cx) / Rx);
            Vector3 top = WallPos(a, 14f), bottom = WallPos(a, 0.5f);
            Vector3 inward = new Vector3(Cx - bottom.x, 0f, -bottom.z).normalized;
            Vector3 mid = (top + bottom) * 0.5f + inward * 1.1f;

            var sheet = S3SceneBuilder.Box("Waterfall", mid, new Vector3(5f, 14f, 0.5f), new Color(0.75f, 0.92f, 1f, 0.6f));
            GrayboxMaterials.TintWater(sheet, new Color(0.75f, 0.92f, 1f, 0.6f));
            sheet.transform.rotation = Quaternion.LookRotation(inward);
            Object.DestroyImmediate(sheet.GetComponent<Collider>());

            // The plunge pool: foam on top.
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pool.name = "Plunge Pool";
            pool.transform.position = new Vector3(FallPool.x, -3.55f, FallPool.y);
            pool.transform.localScale = new Vector3(11f, 6.5f, 9f);
            GrayboxMaterials.TintWater(pool, new Color(0.1f, 0.5f, 0.85f, 0.8f));
            pool.GetComponent<BoxCollider>().isTrigger = true;
            pool.AddComponent<WaterVolume>();

            var foam = S3SceneBuilder.Cyl("Waterfall Foam", new Vector3(FallPool.x, -0.28f, FallPool.y - 2f), new Vector3(6f, 0.02f, 3f), new Color(0.95f, 1f, 1f));
            Object.DestroyImmediate(foam.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------ the gate that needs two buttons

        static void BuildHarbourGate()
        {
            var stone = new Color(0.32f, 0.3f, 0.34f);
            Rock("Gate Wall North", new Vector3(34f, 1.5f, 17.5f), new Vector3(3f, 8f, 21f), stone);
            Rock("Gate Wall South", new Vector3(34f, 1.5f, -17.5f), new Vector3(3f, 8f, 21f), stone);

            var gate = S3SceneBuilder.Box("Harbour Gate", new Vector3(34f, 1.5f, 0f), new Vector3(3f, 8f, 14f), new Color(0.82f, 0.78f, 0.66f));
            GrayboxMaterials.TintStone(gate, new Color(0.82f, 0.78f, 0.66f));
            var sliding = gate.AddComponent<SlidingGate>();
            sliding.plates = new[] { VillagePlate, ShipPlate };
            sliding.openOffset = new Vector3(0f, -9f, 0f);
        }

        // ------------------------------------------------------------------ checkpoints

        static void BuildRoom2Checkpoints()
        {
            StartZone(new Vector3(-42f, 0f, 1f), 7f);
            Checkpoint(new Vector3(-22f, 0f, -8f), "shell", new Vector3(22f, TerHeight, 19f));  // the shell is in the village, behind the last hut
            Checkpoint(new Vector3(42f, 0f, 0f), null, Vector3.zero);                            // already awake, beyond the gate
        }

        // ------------------------------------------------------------------ treasure and notes

        static void BuildRoom2Chests()
        {
            CourseChest("harbour-waterfall", new Vector3(25.5f, Height(25.5f, -25.5f), -25.5f), 0f);
            CourseChest("harbour-ship-bow", new Vector3(-0.5f, ShipDeck, 2.5f), 90f);
            CourseChest("harbour-village", new Vector3(23f, TerHeight, 24f), 200f);
        }

        // The same chest as on the waterpark course (built in CourseExtension), reused here.
        static void CourseChest(string id, Vector3 position, float yaw) => CourseExtension.PlaceChest(id, position, yaw);

        static void BuildRoom2Notes()
        {
            Note(new Vector3(-12.5f, 0f, 15.2f), "A page, pinned to the notice board",
                "DAY 3.\n\nA village! Real houses, with doors and windows, and every lantern still burning.\n\nNobody is home. The beds are made. There are cups on the tables, still half full, and a pot on the fire.\n\nMarit says someone must tend the lamps. I said maybe that is why they stay lit.");
        }
    }
}
