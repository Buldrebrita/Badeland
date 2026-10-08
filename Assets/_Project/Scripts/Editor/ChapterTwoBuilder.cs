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
        const int WallSegments = 128;
        // The walls rise to 15.5 m, then curve inward into the roof, which closes at 22 m.
        const float WallTop = 15.5f, RoofTop = 22f;
        static readonly float[] WallRows = { -8f, -1f, 1f, 3f, 5f, 7f, 9f, 11f, 13f, 15.5f, 17.5f, 19f, 20.3f, 21.2f, 22f };

        // The lake basin (where the ground dips below the water), and the pools of digestive fluid.
        const float LakeMinX = 9f, LakeMaxX = 19f, LakeHalfZ = 4f, LakeBlend = 9f, LakeDepth = -4f;
        static readonly Vector3[] Pools =
        {
            new Vector3(-24f, 3f, 2.8f), new Vector3(-22f, -10f, 2.2f), new Vector3(-10f, 14f, 3f),
            new Vector3(-10f, -14f, 3f), new Vector3(30f, -9f, 3f), new Vector3(30f, 9f, 2.5f),
        };

        static readonly Vector3[] FlatSpots =
        {
            new Vector3(-35f, 0f, 5f), new Vector3(PlateX, PlateZ, 4.5f), new Vector3(StoneX, StoneZ, 2.5f),
        };
        const float PlateX = -28f, PlateZ = 11f, StoneX = -31f, StoneZ = -10f;

        static readonly Color Flesh = new Color(0.9f, 0.5f, 0.42f);
        static readonly Color Ground = new Color(0.8f, 0.55f, 0.6f);
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

            GrayboxMaterials.ResetTexturedMaterials();
            var rng = new System.Random(2027);
            var glowLights = new List<Light>();

            BuildMood();
            BuildGround();
            BuildWalls();
            BuildLivingWalls(rng, glowLights);
            BuildEye();
            var lake = BuildLake();
            BuildDock(glowLights);
            BuildPools(glowLights);
            BuildProps(rng, glowLights);
            BuildGateAndPuzzle();
            BuildMushrooms(rng, glowLights);
            BuildNotes();
            BuildReviveZones();
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

            var bounds = new GameObject("Area Bounds").AddComponent<AreaBounds>();
            bounds.centerX = Cx; bounds.halfX = Rx; bounds.halfZ = Rz;

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
            float lean = h > 0f ? 0.22f * Mathf.Pow(Mathf.Min(h, WallTop) / 15f, 2f) : 0f;
            float bump = h > 0f ? Mathf.Min(h / 4f, 1f) * (0.035f * Mathf.Sin(a * 17f + h * 0.9f) + 0.03f * Mathf.Sin(a * 29f - h * 0.5f + 1f) + 0.014f * Mathf.Sin(a * 53f + h * 1.9f) + 0.01f * Mathf.Sin(a * 41f - h * 2.6f + 2f)) : 0f;
            float rho = Scale(a) * (1f - lean + bump);
            if (h > WallTop)
            {
                // The roof: the wall's top ring shrinks in like a dome until it closes.
                float u = Mathf.Clamp01((h - WallTop) / (RoofTop - WallTop));
                float topLean = 0.22f * Mathf.Pow(WallTop / 15f, 2f);
                float topBump = Mathf.Min(WallTop / 4f, 1f) * (0.035f * Mathf.Sin(a * 17f + WallTop * 0.9f) + 0.03f * Mathf.Sin(a * 29f - WallTop * 0.5f + 1f));
                rho = Scale(a) * (1f - topLean + topBump) * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
            }
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
                    + 0.25f * Mathf.Sin(z * 0.37f + x * 0.05f)
                    + 0.12f * Mathf.Sin(x * 0.9f + z * 0.7f) * Mathf.Sin(z * 0.8f - x * 0.3f);

            float dx = Mathf.Max(Mathf.Max(LakeMinX - x, 0f), x - LakeMaxX);
            float dz = Mathf.Max(Mathf.Abs(z) - LakeHalfZ, 0f);
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            h = Mathf.Lerp(LakeDepth, h, Smooth(0f, LakeBlend, d));

            // Level spots where the game needs flat ground: the wake-up area, the pressure plate and the heavy stone.
            foreach (var flat in FlatSpots)
            {
                float fd = Mathf.Sqrt((x - flat.x) * (x - flat.x) + (z - flat.y) * (z - flat.y));
                h = Mathf.Lerp(0.3f, h, Smooth(flat.z, flat.z + 4f, fd));
            }

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
            RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.14f);
            RenderSettings.fogDensity = 0.009f;
        }

        // ------------------------------------------------------------------ ground and walls (generated meshes)

        // style: 0 = plain colour, 1 = textured ground, 2 = textured living flesh
        static GameObject MeshObject(string name, Mesh mesh, Color color, int style)
        {
            AssetDatabase.CreateAsset(mesh, MeshFolder + "/" + name.Replace(' ', '_') + ".asset");
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            if (style == 1) GrayboxMaterials.TintTextured(go, color, true);
            else if (style == 2) GrayboxMaterials.TintTextured(go, color, false);
            else GrayboxMaterials.Tint(go, color);
            return go;
        }

        static void BuildGround()
        {
            // One uneven mesh for the whole floor (shore, lake bed and far bank), cut off just outside the walls.
            const float x0 = -46f, z0 = -30f, cell = 1.5f;
            const int nx = 62, nz = 41;

            var vertices = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float x = x0 + i * cell, z = z0 + j * cell;
                    vertices[j * nx + i] = new Vector3(x, Height(x, z), z);
                    uvs[j * nx + i] = new Vector2(x / 6f, z / 6f);
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
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshObject("Cavern Floor", mesh, Ground, 1);
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

                var uv = new Vector2[rows * 2];
                for (int r = 0; r < rows; r++)
                {
                    uv[r * 2] = new Vector2(s * 0.4f, WallRows[r] / 5f);
                    uv[r * 2 + 1] = new Vector2((s + 1) * 0.4f, WallRows[r] / 5f);
                }

                var mesh = new Mesh { name = "Wall " + s };
                mesh.vertices = vertices;
                mesh.uv = uv;
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                var go = MeshObject("Wall " + s, mesh, Flesh, 2);
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

        // What makes it feel like the inside of something alive: big pulsing organs on the walls.
        static void BuildLivingWalls(System.Random rng, List<Light> lights)
        {
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

        // A giant eye in the north wall that watches the party.
        static void BuildEye()
        {
            float a = 1.45f;
            Vector3 wall = WallPos(a, 7f);
            Vector3 inward = new Vector3(Cx - wall.x, 0f, -wall.z).normalized;
            var root = new GameObject("Giant Eye");
            root.transform.position = wall + inward * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(inward);
            root.transform.SetParent(SegmentAt(a), true);

            var ball = S3SceneBuilder.Ball("Eyeball", root.transform.position, 7f, new Color(0.95f, 0.9f, 0.92f), false);
            var iris = S3SceneBuilder.Ball("Iris", root.transform.position, 1f, new Color(0.2f, 0.5f, 0.95f), false);
            var pupil = S3SceneBuilder.Ball("Pupil", root.transform.position, 1f, new Color(0.02f, 0.02f, 0.05f), false);
            GrayboxMaterials.TintGlow(iris, new Color(0.2f, 0.5f, 0.95f), 0.6f);
            foreach (var part in new[] { ball, iris, pupil }) part.transform.SetParent(root.transform, true);
            iris.transform.localPosition = new Vector3(0f, 0f, 3.3f);
            iris.transform.localScale = new Vector3(3.6f, 3.6f, 0.6f);
            pupil.transform.localPosition = new Vector3(0f, 0f, 3.5f);
            pupil.transform.localScale = new Vector3(1.6f, 1.6f, 0.6f);
            root.AddComponent<EyeWatcher>();
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
            GrayboxMaterials.TintWater(water, new Color(0.06f, 0.4f, 0.75f, 0.8f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();
            return water;
        }

        static void BuildDock(List<Light> lights)
        {
            // A rotting wooden bridge on posts, across the lake to the far bank, with rope railings and lanterns.
            var wood = new Color(0.4f, 0.26f, 0.16f);
            var rope = new Color(0.65f, 0.55f, 0.35f);
            var lamp = new Color(1f, 0.65f, 0.25f);
            var root = new GameObject("Bridge").transform;
            const int planks = 26;
            for (int i = 0; i < planks; i++)
            {
                float x = 3f + i;
                var plank = S3SceneBuilder.Box("Plank", new Vector3(x, 0.05f, 0f), new Vector3(0.9f, 0.3f, 3.6f), wood * (0.85f + 0.15f * Mathf.Sin(i * 2.3f)));
                plank.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * 1.7f) * 2f, 0f);
                plank.transform.SetParent(root, true);

                if (i % 3 != 0) continue;
                foreach (float z in new[] { -1.8f, 1.8f })
                {
                    var post = S3SceneBuilder.Cyl("Post", new Vector3(x, -2.4f, z), new Vector3(0.3f, 3.6f, 0.3f), wood * 0.8f);
                    post.transform.SetParent(root, true);

                    var rail = S3SceneBuilder.Box("Rope Rail", new Vector3(x + 1.5f, 1.0f, z), new Vector3(3f, 0.08f, 0.08f), rope);
                    rail.transform.SetParent(root, true);
                    var top = S3SceneBuilder.Cyl("Rail Post", new Vector3(x, 0.6f, z), new Vector3(0.18f, 0.6f, 0.18f), wood);
                    top.transform.SetParent(root, true);

                    if (i % 6 == 0 && z > 0f)
                    {
                        var bulb = S3SceneBuilder.Ball("Lantern", new Vector3(x, 1.5f, z), 0.4f, lamp, false);
                        GrayboxMaterials.TintGlow(bulb, lamp, 2f);
                        bulb.transform.SetParent(root, true);
                        lights.Add(AddGlow(new Vector3(x, 1.6f, z), lamp, 2f, 9f, root));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ props: rocks, crystals, hanging tendrils

        // Plants and rocks grow thickly around the edges of the cavern and thinly in the middle.
        static bool EdgeBias(System.Random rng, float x, float z)
        {
            float nx = (x - Cx) / Rx, nz = z / Rz;
            float rho = Mathf.Sqrt(nx * nx + nz * nz) / Scale(Mathf.Atan2(nz, nx));
            float chance = 0.04f + 0.96f * Smooth(0.45f, 0.85f, rho);
            return rng.NextDouble() < chance;
        }

        static bool FreeSpot(float x, float z, float margin)
        {
            if (!Inside(x, z, margin)) return false;
            if (Mathf.Abs(x + 16f) < 5f) return false;                 // the gate
            if (x > 0f && x < 29f && Mathf.Abs(z) < 3f) return false;  // the bridge
            if (Height(x, z) < 0.1f) return false;                     // the lake basin
            foreach (var pool in Pools)
                if ((x - pool.x) * (x - pool.x) + (z - pool.y) * (z - pool.y) < (pool.z + 2f) * (pool.z + 2f)) return false;
            foreach (var flat in FlatSpots)
                if ((x - flat.x) * (x - flat.x) + (z - flat.y) * (z - flat.y) < (flat.z + 2f) * (flat.z + 2f)) return false;
            return true;
        }

        static void BuildProps(System.Random rng, List<Light> lights)
        {
            var rocks = new GameObject("Rocks").transform;
            for (int tries = 0, placed = 0; tries < 1500 && placed < 34; tries++)
            {
                float x = Cx - Rx + (float)rng.NextDouble() * Rx * 2f, z = -Rz + (float)rng.NextDouble() * Rz * 2f;
                if (!FreeSpot(x, z, 1.5f) || !EdgeBias(rng, x, z)) continue;
                placed++;
                int count = 2 + rng.Next(3);
                for (int k = 0; k < count; k++)
                {
                    float size = 0.9f + (float)rng.NextDouble() * 2.2f;
                    var pos = new Vector3(x + (float)rng.NextDouble() * 2f - 1f, Height(x, z) + size * 0.15f, z + (float)rng.NextDouble() * 2f - 1f);
                    var rock = S3SceneBuilder.Ball("Rock", pos, size, new Color(0.3f, 0.24f, 0.34f) * (0.8f + (float)rng.NextDouble() * 0.5f), true);
                    rock.transform.localScale = new Vector3(size, size * (0.5f + (float)rng.NextDouble() * 0.4f), size * (0.8f + (float)rng.NextDouble() * 0.4f));
                    rock.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    rock.transform.SetParent(rocks, true);
                }
            }

            // Glowing crystal and coral clusters.
            Color[] colors = { new Color(1f, 0.25f, 0.8f), new Color(0.2f, 0.9f, 1f), new Color(1f, 0.6f, 0.2f), new Color(0.6f, 0.4f, 1f) };
            var crystals = new GameObject("Crystals").transform;
            for (int tries = 0, placed = 0; tries < 1500 && placed < 36; tries++)
            {
                float x = Cx - Rx + (float)rng.NextDouble() * Rx * 2f, z = -Rz + (float)rng.NextDouble() * Rz * 2f;
                if (!FreeSpot(x, z, 1.8f) || !EdgeBias(rng, x, z)) continue;
                placed++;
                Color glow = colors[rng.Next(colors.Length)];
                int count = 4 + rng.Next(4);
                for (int k = 0; k < count; k++)
                {
                    float height = 1f + (float)rng.NextDouble() * 2.6f;
                    var pos = new Vector3(x + (float)rng.NextDouble() * 1.6f - 0.8f, Height(x, z) + height * 0.4f, z + (float)rng.NextDouble() * 1.6f - 0.8f);
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    shard.name = "Crystal";
                    shard.transform.position = pos;
                    shard.transform.localScale = new Vector3(0.35f, height * 0.5f, 0.35f);
                    shard.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 40f - 20f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 40f - 20f);
                    GrayboxMaterials.TintGlow(shard, glow, 1.4f); // solid: you cannot walk through it
                    shard.transform.SetParent(crystals, true);
                }
                if (placed % 3 == 0) lights.Add(AddGlow(new Vector3(x, Height(x, z) + 1.5f, z), glow, 2f, 10f, crystals));
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

                // A pitted, eaten-away rim around the pool.
                var rimColor = new Color(0.14f, 0.2f, 0.06f);
                var rim = S3SceneBuilder.Cyl("Etched Rim", new Vector3(pool.x, surface - 0.04f, pool.y), new Vector3(pool.z * 2.5f, 0.04f, pool.z * 2.5f), rimColor);
                Object.DestroyImmediate(rim.GetComponent<Collider>());
                rim.transform.SetParent(root, true);
                for (int k = 0; k < 9; k++)
                {
                    float ang = k * 0.7f + pool.x, dist = pool.z * (1.15f + 0.25f * Mathf.Sin(k * 2.1f));
                    float pitSize = 0.5f + 0.4f * Mathf.Abs(Mathf.Sin(k * 1.3f));
                    var pit = S3SceneBuilder.Cyl("Etch Pit", new Vector3(pool.x + Mathf.Cos(ang) * dist, surface - 0.02f, pool.y + Mathf.Sin(ang) * dist), new Vector3(pitSize, 0.04f, pitSize), rimColor * 0.6f);
                    Object.DestroyImmediate(pit.GetComponent<Collider>());
                    pit.transform.SetParent(root, true);
                }

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

            // The plate sits on a level patch of ground (see FlatSpots), a plain stone button just proud of the floor.
            float plateTop = Height(PlateX, PlateZ) + 0.3f;
            S3SceneBuilder.Box("Plate Step", new Vector3(PlateX, plateTop - 0.18f - 0.6f, PlateZ), new Vector3(7.6f, 1.2f, 7.6f), Stone * 0.9f);
            var plateObject = S3SceneBuilder.Box("Pressure Plate", new Vector3(PlateX, plateTop - 0.6f, PlateZ), new Vector3(6f, 1.2f, 6f), new Color(0.35f, 0.3f, 0.4f));
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.visual = plateObject.GetComponent<Renderer>();

            var gateObject = S3SceneBuilder.Box("Bone Gate", new Vector3(-16f, 1.5f, 0f), new Vector3(3f, 7f, 14f), Bone);
            var gate = gateObject.AddComponent<SlidingGate>();
            gate.plates = new[] { plate };
            gate.openOffset = new Vector3(0f, -7.5f, 0f);

            // The heavy glowing stone for anyone doing it alone.
            var stone = S3SceneBuilder.Ball("Heavy Stone", new Vector3(StoneX, Height(StoneX, StoneZ) + 0.8f, StoneZ), 1.6f, new Color(1f, 0.6f, 0.2f), true);
            GrayboxMaterials.TintGlow(stone, new Color(1f, 0.55f, 0.15f), 0.9f);
            stone.AddComponent<Carryable>();
            AddGlow(stone.transform.position + Vector3.up * 1.2f, new Color(1f, 0.6f, 0.25f), 3.5f, 9f);
        }

        // ------------------------------------------------------------------ where the dead come back

        static void BuildReviveZones()
        {
            // The start of the area (where everyone restarts until a checkpoint is activated), then two checkpoints. Each
            // checkpoint is a grey stone altar with a carving of the item it wants; the item must be found and laid on it.
            StartZone(new Vector3(-35f, 0f, 1f), 7f);
            Checkpoint(new Vector3(-8f, 0f, 0f), "key", new Vector3(-27f, 0f, -13f));
            Checkpoint(new Vector3(31f, 0f, 0f), "cog", new Vector3(-3f, 0f, -15f));
        }

        static void StartZone(Vector3 position, float radius)
        {
            position.y = Height(position.x, position.z) + 0.05f;
            var zone = new GameObject("Start Zone");
            zone.transform.position = position;
            var component = zone.AddComponent<ReviveZone>();
            component.radius = radius;
            component.label = "Start";
            component.livingPlayersRevive = false;
            component.respawn = zone.transform;
        }

        static void Checkpoint(Vector3 position, string itemId, Vector3 itemPosition)
        {
            position.y = Height(position.x, position.z);
            var stoneGrey = new Color(0.55f, 0.55f, 0.58f);

            var zone = new GameObject("Checkpoint Altar (" + itemId + ")");
            zone.transform.position = position + Vector3.up * 0.05f;

            // A grey stone altar, with the carving of the item on top.
            S3SceneBuilder.Box("Altar Base", position + Vector3.up * 0.2f, new Vector3(2.6f, 1.6f, 2.6f), stoneGrey * 0.85f).transform.SetParent(zone.transform, true);
            var top = S3SceneBuilder.Box("Altar Top", position + Vector3.up * 1.05f, new Vector3(3f, 0.2f, 3f), stoneGrey);
            top.transform.SetParent(zone.transform, true);
            float topY = position.y + 1.15f;
            var carving = ItemShape(itemId, new Vector3(position.x, topY + 0.015f, position.z), 1f, new Color(0.25f, 0.25f, 0.28f), true);
            carving.transform.SetParent(zone.transform, true);

            var spot = new GameObject("Item Spot").transform;
            spot.SetParent(zone.transform, true);
            spot.position = new Vector3(position.x, topY + 0.12f, position.z);

            var respawn = new GameObject("Respawn").transform;
            respawn.SetParent(zone.transform, true);
            respawn.position = position + new Vector3(0f, 0.3f, -3.5f);

            var component = zone.AddComponent<ReviveZone>();
            component.radius = 6f;
            component.label = "Checkpoint";
            component.livingPlayersRevive = true;
            component.respawn = respawn;
            component.requiredItemId = itemId;
            component.altarTop = spot;
            component.beacon = AddGlow(position + Vector3.up * 3.5f, new Color(0.8f, 0.9f, 1f), 2f, 12f, zone.transform);

            // The item itself, somewhere in the area, lying on the ground.
            itemPosition.y = Height(itemPosition.x, itemPosition.z) + 0.14f;
            var glow = itemId == "key" ? new Color(1f, 0.8f, 0.25f) : new Color(0.3f, 0.9f, 0.9f);
            var item = ItemShape(itemId, itemPosition, 1f, glow, false);
            item.name = "Item (" + itemId + ")";
            var sphere = item.AddComponent<SphereCollider>();
            sphere.radius = 0.8f;
            item.AddComponent<QuestItem>().itemId = itemId;
            var carry = item.AddComponent<Carryable>();
            carry.radius = 0.3f;
            carry.pickupRadius = 2.2f;
            carry.carrySpeed = 0.95f;
            carry.carryJump = 0.95f;
            AddGlow(itemPosition + Vector3.up * 1f, glow, 2f, 7f, item.transform);
        }

        // The shapes of the quest items. Built lying flat, so they work both as carved marks on the altar and as the objects themselves.
        static GameObject ItemShape(string id, Vector3 position, float scale, Color color, bool engraved)
        {
            var root = new GameObject(engraved ? "Carving (" + id + ")" : "Item Shape (" + id + ")");
            root.transform.position = position;
            float thick = engraved ? 0.03f : 0.12f;

            if (id == "key")
            {
                Part(root, PrimitiveType.Cylinder, new Vector3(-0.6f, 0f, 0f), new Vector3(0.55f, thick * 0.5f, 0.55f), color, engraved);       // the ring
                Part(root, PrimitiveType.Cube, new Vector3(0.1f, 0f, 0f), new Vector3(1.1f, thick, 0.16f), color, engraved);                      // the shaft
                Part(root, PrimitiveType.Cube, new Vector3(0.5f, 0f, -0.17f), new Vector3(0.14f, thick, 0.3f), color, engraved);                  // teeth
                Part(root, PrimitiveType.Cube, new Vector3(0.28f, 0f, -0.12f), new Vector3(0.14f, thick, 0.2f), color, engraved);
            }
            else
            {
                Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.9f, thick * 0.5f, 0.9f), color, engraved);                         // the cog
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 2f / 8f;
                    var tooth = Part(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f), new Vector3(0.22f, thick, 0.22f), color, engraved);
                    tooth.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                }
            }
            root.transform.localScale = Vector3.one * scale;
            return root;
        }

        static GameObject Part(GameObject root, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, bool engraved)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Part";
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (engraved) GrayboxMaterials.Tint(go, color); else GrayboxMaterials.TintGlow(go, color, 1.3f);
            return go;
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
            for (int tries = 0; tries < 2000 && placed < 60; tries++)
            {
                float x = Cx - Rx + (float)rng.NextDouble() * Rx * 2f;
                float z = -Rz + (float)rng.NextDouble() * Rz * 2f;
                if (!Inside(x, z, 2.5f) || !EdgeBias(rng, x, z)) continue;
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
                stem.transform.SetParent(root, true); // the stem is solid

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
            Note(new Vector3(-31f, 0f, 3f), "A torn page",
                "DAY 1.\n\nWe came in through the water, Marit and me. Nobody remembers the dark part, only the cold, and then the warm.\n\nThe ground here is warm, and it moves a little, like something asleep. I told Marit it was only the tide. She laughed.\n\nWe'll walk until we find the way out. How big can a cave be?");

            Note(new Vector3(-27f, 0f, -3.5f), "Marit's handwriting",
                "DAY 2.\n\nThere are green pools all over the shore. Marit dropped her spoon into one, to see.\n\nIt was gone in a breath. The metal went soft and pitted first, like the pool was chewing it.\n\nWe are not going near those. Not ever.");
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
