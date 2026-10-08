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
    /// world with its own light, water and weather, all of it breathing. A closed cavern with tall fleshy walls and
    /// uneven ground: a shore with glowing mushrooms, pools of digestive fluid, a hold-a-button gate, a breathing lake
    /// and a great door at the far side. Only two short notes: the party just has to realise they were swallowed.
    /// Menu: Badeland > Create Chapter 2 - Inside the Monster (first area). See docs/CHAPTER2.md and docs/STORY.md.
    /// </summary>
    public static class ChapterTwoBuilder
    {
        public const string SceneName = "Inside_01_WakingShore";
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";
        const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        const string MeshFolder = "Assets/_Project/Art/Environment/Inside";

        // The cavern is an uneven oval: centre x = Cx, half-widths Rx and Rz, with a wobbly outline.
        const float Cx = -1f, Rx = 40f, Rz = 24f;
        const int WallSegments = 64;
        static readonly float[] WallRows = { -8f, -1f, 2f, 5f, 8.5f, 12f, 15.5f };

        // The lake basin (where the ground dips below the water), and the pools of digestive fluid.
        const float LakeMinX = 9f, LakeMaxX = 19f, LakeHalfZ = 4f, LakeBlend = 9f, LakeDepth = -4f;
        static readonly Vector3[] Pools =
        {
            new Vector3(-24f, 3f, 2.8f), new Vector3(-22f, -10f, 2.2f), new Vector3(-10f, 14f, 3f),
            new Vector3(-10f, -14f, 3f), new Vector3(30f, -9f, 3f), new Vector3(30f, 9f, 2.5f),
        };

        static readonly Color Flesh = new Color(0.42f, 0.17f, 0.2f);
        static readonly Color Ground = new Color(0.46f, 0.27f, 0.28f);
        static readonly Color Stone = new Color(0.3f, 0.27f, 0.3f);
        static readonly Color Bone = new Color(0.82f, 0.78f, 0.66f);

        static GameObject[] _segments;

        [MenuItem("Badeland/Create Chapter 2 - Inside the Monster (first area)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(PrefabFolder);
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

            var rng = new System.Random(2027);
            var glowLights = new List<Light>();

            BuildMood();
            BuildGround();
            BuildWalls();
            BuildLivingWalls(rng, glowLights);
            BuildRibs();
            var lake = BuildLake();
            BuildDock();
            BuildPools(glowLights);
            BuildGateAndPuzzle();
            BuildMushrooms(rng, glowLights);
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
                float x = -36f + (i % 2) * 2.5f, z = -2f + (i / 2) * 3f;
                spot.position = new Vector3(x, Height(x, z) + 0.3f, z);
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

        // ------------------------------------------------------------------ shape of the cavern

        // How far out the cavern outline goes in each direction (1 = a plain oval).
        static float Scale(float a) => 1f + 0.05f * Mathf.Sin(3f * a + 1f) + 0.035f * Mathf.Sin(5f * a + 2.5f) + 0.02f * Mathf.Sin(9f * a);

        // A point on the wall: angle around the cavern, and height. Higher up, the wall leans in like a dome and bulges.
        static Vector3 WallPos(float a, float h)
        {
            float lean = h > 0f ? 0.22f * Mathf.Pow(h / 15f, 2f) : 0f;
            float bump = h > 0f ? Mathf.Min(h / 4f, 1f) * (0.035f * Mathf.Sin(a * 17f + h * 0.9f) + 0.03f * Mathf.Sin(a * 29f - h * 0.5f + 1f)) : 0f;
            float rho = Scale(a) * (1f - lean + bump);
            return new Vector3(Cx + Rx * rho * Mathf.Cos(a), h, Rz * rho * Mathf.Sin(a));
        }

        // Is this spot on the floor, a safe distance in from the wall? (A negative margin counts a bit outside too.)
        static bool Inside(float x, float z, float margin)
        {
            float nx = (x - Cx) / Rx, nz = z / Rz;
            float rho = Mathf.Sqrt(nx * nx + nz * nz);
            float a = Mathf.Atan2(nz, nx);
            return rho < Scale(a) - margin / Rz;
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // The height of the ground: gentle hills and hollows, a deep basin for the lake, small dips for the pools.
        static float Height(float x, float z)
        {
            float h = 0.55f * Mathf.Sin(x * 0.23f + 1f) * Mathf.Cos(z * 0.19f)
                    + 0.35f * Mathf.Sin(x * 0.11f - z * 0.14f + 2f)
                    + 0.25f * Mathf.Sin(z * 0.37f + x * 0.05f);

            float dx = Mathf.Max(Mathf.Max(LakeMinX - x, 0f), x - LakeMaxX);
            float dz = Mathf.Max(Mathf.Abs(z) - LakeHalfZ, 0f);
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            h = Mathf.Lerp(LakeDepth, h, Smooth(0f, LakeBlend, d));

            foreach (var pool in Pools)
            {
                float pd = Mathf.Sqrt((x - pool.x) * (x - pool.x) + (z - pool.y) * (z - pool.y));
                h -= 0.8f * (1f - Smooth(pool.z * 0.7f, pool.z * 1.4f, pd));
            }
            return h;
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
                sun.intensity = 0.6f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(65f, 30f, 0f);
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.2f, 0.26f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.09f, 0.11f);
            RenderSettings.fogDensity = 0.009f;
        }

        // ------------------------------------------------------------------ ground and walls (generated meshes)

        static GameObject MeshObject(string name, Mesh mesh, Color color, bool doubleSided)
        {
            AssetDatabase.CreateAsset(mesh, MeshFolder + "/" + name.Replace(' ', '_') + ".asset");
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            if (doubleSided) GrayboxMaterials.TintDoubleSided(go, color);
            else GrayboxMaterials.Tint(go, color);
            return go;
        }

        static void BuildGround()
        {
            // One uneven mesh for the whole floor (shore, lake bed and far bank), cut off just outside the walls.
            const float x0 = -46f, z0 = -30f, cell = 2f;
            const int nx = 47, nz = 31;

            var vertices = new Vector3[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float x = x0 + i * cell, z = z0 + j * cell;
                    vertices[j * nx + i] = new Vector3(x, Height(x, z), z);
                }

            var triangles = new List<int>();
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    float cx = x0 + (i + 0.5f) * cell, cz = z0 + (j + 0.5f) * cell;
                    if (!Inside(cx, cz, -3f)) continue; // reaches a little under the walls so there is no gap

                    int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }

            var mesh = new Mesh { name = "Cavern Floor", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshObject("Cavern Floor", mesh, Ground, false);
        }

        // Tall, solid, leaning walls all the way round, in pieces. Pieces that stand between the camera and a player fade out.
        static void BuildWalls()
        {
            var root = new GameObject("Walls").transform;
            _segments = new GameObject[WallSegments];
            int rows = WallRows.Length;

            for (int s = 0; s < WallSegments; s++)
            {
                float a0 = Mathf.PI * 2f * s / WallSegments;
                float a1 = Mathf.PI * 2f * (s + 1) / WallSegments;

                var vertices = new Vector3[rows * 2];
                for (int r = 0; r < rows; r++)
                {
                    vertices[r * 2] = WallPos(a0, WallRows[r]);
                    vertices[r * 2 + 1] = WallPos(a1, WallRows[r]);
                }

                var triangles = new List<int>();
                for (int r = 0; r < rows - 1; r++)
                {
                    int b0 = r * 2, b1 = r * 2 + 1, t0 = (r + 1) * 2, t1 = (r + 1) * 2 + 1;
                    triangles.Add(t1); triangles.Add(t0); triangles.Add(b0);
                    triangles.Add(t1); triangles.Add(b0); triangles.Add(b1);
                }

                var mesh = new Mesh { name = "Wall " + s };
                mesh.vertices = vertices;
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                float tone = 0.85f + 0.3f * Mathf.Abs(Mathf.Sin(s * 1.7f));
                var go = MeshObject("Wall " + s, mesh, Flesh * tone, true);
                go.transform.SetParent(root, false);
                go.AddComponent<WallFader>();
                _segments[s] = go;
            }
        }

        static Transform SegmentAt(float angle)
        {
            float t = Mathf.Repeat(angle, Mathf.PI * 2f) / (Mathf.PI * 2f);
            return _segments[Mathf.Clamp(Mathf.FloorToInt(t * WallSegments), 0, WallSegments - 1)].transform;
        }

        // What makes it feel like the inside of something alive: glowing veins and pulsing organs on the walls.
        static void BuildLivingWalls(System.Random rng, List<Light> lights)
        {
            var veinColor = new Color(0.95f, 0.15f, 0.3f);

            for (int i = 0; i < 60; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float h = 1.5f + (float)rng.NextDouble() * 12f;
                Vector3 wall = WallPos(a, h);
                Vector3 inward = new Vector3(Cx - wall.x, 0f, -wall.z).normalized;
                Vector3 position = wall + inward * 0.35f;
                Vector3 tangent = new Vector3(-Rx * Mathf.Sin(a), 0f, Rz * Mathf.Cos(a)).normalized;
                Vector3 direction = tangent + Vector3.up * ((float)rng.NextDouble() - 0.4f);

                float length = 8f + (float)rng.NextDouble() * 12f;
                var vein = S3SceneBuilder.Cyl("Vein", position, new Vector3(0.3f, length * 0.5f, 0.3f), veinColor);
                GrayboxMaterials.TintGlow(vein, veinColor, 1.2f);
                Object.DestroyImmediate(vein.GetComponent<Collider>());
                vein.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                vein.transform.SetParent(SegmentAt(a), true);
                if (i % 8 == 0) lights.Add(AddGlow(position + inward * 2f, veinColor, 1.6f, 14f, SegmentAt(a)));
            }

            // Big organs bulging out of the walls that swell with each breath.
            float[] organAngles = { 0.5f, 1.5f, 2.6f, 3.6f, 4.4f, 5.5f };
            for (int i = 0; i < organAngles.Length; i++)
            {
                float a = organAngles[i];
                Vector3 wall = WallPos(a, 6f + i % 3 * 2f);
                Vector3 inward = new Vector3(Cx - wall.x, 0f, -wall.z).normalized;
                var organ = S3SceneBuilder.Ball("Organ", wall - inward * 1.2f, 9f, new Color(0.55f, 0.18f, 0.4f), false);
                GrayboxMaterials.TintGlow(organ, new Color(0.55f, 0.18f, 0.4f), 0.5f);
                organ.AddComponent<BreathScale>().amount = 0.08f;
                organ.transform.SetParent(SegmentAt(a), true);
            }
        }

        static void BuildRibs()
        {
            // Huge pale ribs arching over the cavern, far above the camera.
            for (int r = 0; r < 5; r++)
            {
                float x = -32f + r * 16f;
                var root = new GameObject("Rib");
                const int segments = 16;
                const float span = 21f, height = 24f;
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

        // ------------------------------------------------------------------ lake and dock

        static GameObject BuildLake()
        {
            // The water fills the basin; its top face (y = -0.3) is the surface.
            float minX = LakeMinX - 7.5f, maxX = LakeMaxX + 7.5f, halfZ = LakeHalfZ + 7.5f;
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Lake";
            water.transform.position = new Vector3((minX + maxX) * 0.5f, -3.8f, 0f);
            water.transform.localScale = new Vector3(maxX - minX, 7f, halfZ * 2f);
            GrayboxMaterials.TintWater(water, new Color(0.04f, 0.32f, 0.38f, 0.8f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();
            return water;
        }

        static void BuildDock()
        {
            // A rotting wooden dock leading out into the lake, on posts. At high tide it is nearly underwater.
            var wood = new Color(0.32f, 0.22f, 0.15f);
            for (int i = 0; i < 12; i++)
            {
                float x = 3f + i;
                S3SceneBuilder.Box("Dock Plank", new Vector3(x, 0.05f, 0f), new Vector3(0.9f, 0.3f, 3.6f), wood);
                if (i % 3 == 0)
                    foreach (float z in new[] { -1.6f, 1.6f })
                    {
                        var post = S3SceneBuilder.Cyl("Dock Post", new Vector3(x, -2.9f, z), new Vector3(0.3f, 3f, 0.3f), wood * 0.8f);
                        Object.DestroyImmediate(post.GetComponent<Collider>());
                    }
            }
        }

        // ------------------------------------------------------------------ danger: pools of digestive fluid

        static void BuildPools(List<Light> lights)
        {
            var green = new Color(0.45f, 1f, 0.2f);
            var root = new GameObject("Digestive Pools").transform;
            foreach (var pool in Pools)
            {
                float surface = Height(pool.x, pool.y) + 0.45f;
                var disc = S3SceneBuilder.Cyl("Digestive Pool", new Vector3(pool.x, surface, pool.y), new Vector3(pool.z * 2f, 0.05f, pool.z * 2f), green);
                GrayboxMaterials.TintGlow(disc, green, 1.8f);
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.transform.SetParent(root, true);

                var hazard = disc.AddComponent<AcidPool>();
                hazard.radius = pool.z;
                hazard.surfaceHeight = surface;

                lights.Add(AddGlow(new Vector3(pool.x, surface + 1.2f, pool.y), green, 2f, 9f, root));
            }
        }

        // ------------------------------------------------------------------ the gate and the hold-a-button puzzle

        static void BuildGateAndPuzzle()
        {
            // A wall across the cavern with a gap, closed by a bone gate that sinks into the ground while the plate is held.
            S3SceneBuilder.Box("Choke Wall North", new Vector3(-16f, 1.5f, 14.8f), new Vector3(3f, 7f, 15.6f), Stone);
            S3SceneBuilder.Box("Choke Wall South", new Vector3(-16f, 1.5f, -14.8f), new Vector3(3f, 7f, 15.6f), Stone);

            float plateY = Height(-28f, 11f);
            var plateObject = S3SceneBuilder.Box("Pressure Plate", new Vector3(-28f, plateY - 0.5f, 11f), new Vector3(5f, 1.2f, 5f), new Color(0.35f, 0.3f, 0.4f));
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.visual = plateObject.GetComponent<Renderer>();
            plateObject.GetComponent<BoxCollider>().isTrigger = true;

            var gateObject = S3SceneBuilder.Box("Bone Gate", new Vector3(-16f, 1.5f, 0f), new Vector3(3f, 7f, 14f), Bone);
            var gate = gateObject.AddComponent<SlidingGate>();
            gate.plates = new[] { plate };
            gate.openOffset = new Vector3(0f, -7.5f, 0f);

            // The heavy glowing stone for anyone doing it alone.
            var stone = S3SceneBuilder.Ball("Heavy Stone", new Vector3(-31f, Height(-31f, -10f) + 0.8f, -10f), 1.6f, new Color(1f, 0.6f, 0.2f), true);
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
            int placed = 0;
            for (int tries = 0; tries < 400 && placed < 40; tries++)
            {
                float x = Cx - Rx + (float)rng.NextDouble() * Rx * 2f;
                float z = -Rz + (float)rng.NextDouble() * Rz * 2f;
                if (!Inside(x, z, 3.5f)) continue;
                if (Mathf.Abs(x + 16f) < 4f) continue; // not in the doorway of the gate
                if (x > -39f && x < -33f && Mathf.Abs(z) < 5f) continue; // keep the wake-up spot clear
                float h0 = Height(x, z);
                if (h0 < 0.2f) continue; // not in the lake basin
                bool nearPool = false;
                foreach (var pool in Pools)
                    if ((x - pool.x) * (x - pool.x) + (z - pool.y) * (z - pool.y) < (pool.z + 1.5f) * (pool.z + 1.5f)) nearPool = true;
                if (nearPool) continue;

                Color glow = glowColors[rng.Next(glowColors.Length)];
                float height = 0.8f + (float)rng.NextDouble() * 2.6f;
                float cap = 1.1f + (float)rng.NextDouble() * 1.8f;

                var stem = S3SceneBuilder.Cyl("Mushroom Stem", new Vector3(x, h0 + height * 0.5f, z), new Vector3(0.25f, height * 0.5f, 0.25f), new Color(0.85f, 0.85f, 0.8f));
                Object.DestroyImmediate(stem.GetComponent<Collider>());
                stem.transform.SetParent(root, true);

                var top = S3SceneBuilder.Ball("Mushroom Cap", new Vector3(x, h0 + height, z), cap, glow, false);
                top.transform.localScale = new Vector3(cap, cap * 0.55f, cap);
                GrayboxMaterials.TintGlow(top, glow, 1.6f);
                top.transform.SetParent(root, true);

                if (placed % 5 == 0) lights.Add(AddGlow(new Vector3(x, h0 + height + 0.8f, z), glow, 2.2f, 14f));
                placed++;
            }
        }

        static Light AddGlow(Vector3 position, Color color, float intensity, float range, Transform parent = null)
        {
            var go = new GameObject("Glow Light");
            go.transform.position = position;
            if (parent != null) go.transform.SetParent(parent, true);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        // ------------------------------------------------------------------ two short notes

        static void BuildNotes()
        {
            Note(new Vector3(-31f, 0f, 3f), "A wet scrap of paper",
                "You're awake. Good.\n\nYou were swallowed. Whatever it was, it was bigger than the whole waterpark, and now you are inside it. The ground is warm. The walls breathe.\n\nStay together. Look for a way on.");

            Note(new Vector3(-27f, 0f, -3.5f), "Scratched into a plank",
                "The green pools are not water.\n\nThis place eats. Stay out of them, and be careful when the lake rises.");
        }

        static void Note(Vector3 position, string title, string text)
        {
            position.y = Height(position.x, position.z) + 0.15f;
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
            int placed = 0;
            for (int tries = 0; tries < 200 && placed < 12; tries++)
            {
                float x = -36f + (float)rng.NextDouble() * 72f;
                float z = -20f + (float)rng.NextDouble() * 40f;
                if (!Inside(x, z, 7f)) continue;
                placed++;

                Color c = colors[rng.Next(colors.Length)];
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
                drift.radius = 2f + (float)rng.NextDouble() * 3f;
                drift.speed = 0.08f + (float)rng.NextDouble() * 0.12f;
                drift.phase = (float)rng.NextDouble() * 6f;
                drift.bobAmplitude = 0.5f;
            }
        }

        // ------------------------------------------------------------------ the great door

        static void BuildExitDoor(List<Light> lights)
        {
            // A huge glowing ring set into the east end of the cavern, like an iris that has not yet opened.
            var violet = new Color(0.65f, 0.3f, 1f);
            Vector3 wall = WallPos(0f, 6f);
            var ring = S3SceneBuilder.Cyl("Great Door", new Vector3(wall.x + 0.2f, 6f, 0f), new Vector3(11f, 0.8f, 11f), new Color(0.2f, 0.15f, 0.3f));
            ring.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var core = S3SceneBuilder.Cyl("Door Light", new Vector3(wall.x - 0.6f, 6f, 0f), new Vector3(8f, 0.4f, 8f), violet);
            core.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GrayboxMaterials.TintGlow(core, violet, 2.2f);
            Object.DestroyImmediate(core.GetComponent<Collider>());
            lights.Add(AddGlow(new Vector3(wall.x - 4f, 6f, 0f), violet, 4f, 26f));

            var exit = new GameObject("Area Exit");
            float exitX = wall.x - 3.5f;
            exit.transform.position = new Vector3(exitX, Height(exitX, 0f) + 2f, 0f);
            var box = exit.AddComponent<BoxCollider>();
            box.size = new Vector3(6f, 5f, 14f);
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
