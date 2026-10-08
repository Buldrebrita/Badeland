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
    /// One-click builder for the S3 course test scene: a ring of inflatable decks floating on open water, run
    /// three times, with flag gates, bounce pads, a rotating bar, a gap of stepping discs, random jumping fish
    /// (cod, salmon, clownfish), lap counting and subtle per-lap changes. Fall in and you swim back.
    /// Menu: Badeland > Create S3 Course Test Scene.
    /// </summary>
    public static class S3SceneBuilder
    {
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";
        const string FishFolder = "Assets/_Project/Settings/Fish";

        static readonly Color Grass = new Color(0.35f, 0.75f, 0.45f);
        static readonly Color Wall = new Color(0.35f, 0.6f, 0.95f);
        static readonly Color Pink = new Color(1f, 0.4f, 0.7f);

        [MenuItem("Badeland/Create S3 Course Test Scene")]
        public static void Build()
        {
            var result = BuildCourse(true);
            if (result == null) return;
            Save(result, "Graybox_S3_Course",
                "Badeland: S3 course scene created at {0}. Press Play and run to the right (counter-clockwise). Fall in the water and swim back to a deck.");
        }

        /// <summary>What a built course contains, so other builders (the online scene) can extend it.</summary>
        public class CourseResult
        {
            public UnityEngine.SceneManagement.Scene scene;
            public MovementSettings settings;
            public FishSpecies[] species;
            public LapTracker tracker;
            public IsoCameraRig rig;
            public InflatableCourse.Layout layout;
        }

        public static void Save(CourseResult result, string sceneName, string logFormat)
        {
            string scenePath = SceneFolder + "/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(result.scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(string.Format(logFormat, scenePath));
        }

        /// <summary>Builds the whole course in a new scene (not saved yet). Returns null if the user cancels.</summary>
        public static CourseResult BuildCourse(bool includePlayer)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);
            Directory.CreateDirectory(FishFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            string settingsPath = SettingsFolder + "/MovementSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            var cod = Species("Cod", "Cod", new Color(0.9f, 0.75f, 0.4f), false,
                new MovementModifier { speedMultiplier = 1.4f, jumpMultiplier = 1f });
            var salmon = Species("Salmon", "Salmon", new Color(1f, 0.5f, 0.45f), false,
                new MovementModifier { speedMultiplier = 1f, jumpMultiplier = 1.6f });
            var clown = Species("Clownfish", "Clownfish", new Color(1f, 0.5f, 0f), true,
                new MovementModifier { speedMultiplier = 1f, jumpMultiplier = 1f, wobbleDegrees = 60f, wobbleHz = 1.2f });

            // ---- Sea: a big water volume (top face = surface at y 0) over a seabed.
            var sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sea.name = "Sea";
            sea.transform.position = new Vector3(0f, -4f, 0f);
            sea.transform.localScale = new Vector3(700f, 8f, 700f);
            GrayboxMaterials.ApplySea(sea);
            sea.AddComponent<SeaMotion>();
            sea.GetComponent<BoxCollider>().isTrigger = true;
            sea.AddComponent<WaterVolume>();
            Box("Seabed", new Vector3(0f, -8.5f, 0f), new Vector3(700f, 1f, 700f), new Color(0.1f, 0.45f, 0.9f));

            // ---- The obstacle course: one big inflatable course on the open water, 15 obstacles from start to finish.
            var layout = InflatableCourse.Build(cod, salmon, clown);

            var trackerGo = new GameObject("LapTracker");
            var tracker = trackerGo.AddComponent<LapTracker>();
            tracker.checkpoints = layout.gates;
            tracker.totalLaps = 1;

            // ---- The beach, the marina, the crowds and the town behind them.
            CourseScenery.Build(layout);

            // Nobody can wander off to the beach or the town: the play area is the water and the course.
            var bounds = new GameObject("Play Area").AddComponent<RectBounds>();
            bounds.minX = -layout.radius - 22f; bounds.maxX = layout.radius + 22f;
            bounds.minZ = -28f; bounds.maxZ = layout.radius + 26f;

            // ---- The trap and secret room, the monster and its alarm, and the look.
            CourseExtras.Build(new CourseExtras.Context
            {
                tracker = tracker,
                sea = sea,
                sun = GameObject.Find("Directional Light") != null ? GameObject.Find("Directional Light").GetComponent<Light>() : null,
                camera = Camera.main,
                layout = layout,
            });

            // ---- Player (starts on the start platform). The online scene spawns its players instead.
            GameObject player = null;
            if (includePlayer)
            {
                player = CreatePlayerObject(settings);
                player.transform.position = layout.startSpawn;
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // ---- Camera rig
            var cam = Camera.main;
            var rig = new GameObject("CameraRig");
            var rigComponent = rig.AddComponent<IsoCameraRig>();
            var rigSo = new SerializedObject(rigComponent);
            if (player != null)
            {
                var targets = rigSo.FindProperty("targets");
                targets.arraySize = 1;
                targets.GetArrayElementAtIndex(0).objectReferenceValue = player.transform;
                rigSo.ApplyModifiedPropertiesWithoutUndo();
            }
            if (cam != null)
            {
                cam.transform.SetParent(rig.transform, false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }

            // ---- Test HUD
            var hud = new GameObject("RaceHud").AddComponent<RaceHud>();
            hud.tracker = tracker;

            return new CourseResult
            {
                scene = scene,
                settings = settings,
                species = new[] { cod, salmon, clown },
                tracker = tracker,
                rig = rigComponent,
                layout = layout,
            };
        }

        /// <summary>The player capsule with its controller scripts. Used by the offline scene and as the base of the online player prefab.</summary>
        public static GameObject CreatePlayerObject(MovementSettings settings)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            GrayboxMaterials.Tint(player, new Color(1f, 0.55f, 0.15f));

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
            nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            GrayboxMaterials.Tint(nose, Color.white);

            player.AddComponent<MovementModifiers>();
            var controller = player.AddComponent<PlayerController>();
            player.AddComponent<FishCarrier>();
            var so = new SerializedObject(controller);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("visual").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        // ------------------------------------------------------------------ helpers

        static FishSpecies Species(string assetName, string displayName, Color color, bool debuff, MovementModifier effect)
        {
            string path = FishFolder + "/Fish_" + assetName + ".asset";
            var species = AssetDatabase.LoadAssetAtPath<FishSpecies>(path);
            if (species != null) return species; // keep any tuning done in the inspector

            species = ScriptableObject.CreateInstance<FishSpecies>();
            species.displayName = displayName;
            species.color = color;
            species.isDebuff = debuff;
            species.effect = effect;
            AssetDatabase.CreateAsset(species, path);
            return species;
        }

        internal static void FishArea(string name, FishSpecies[] pool, Vector3 zoneCenter, Vector3 zoneSize, float heading,
            float spread, float minDistance, float maxDistance, int seed)
        {
            var root = new GameObject(name);
            root.transform.position = zoneCenter;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie along the travel direction
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            GrayboxMaterials.Tint(body, pool[0].color);

            var jumper = root.AddComponent<FishJumper>();
            jumper.speciesPool = pool;
            jumper.visual = body;
            jumper.zoneCenter = zoneCenter;
            jumper.zoneSize = zoneSize;
            jumper.headingDegrees = heading;
            jumper.headingSpread = spread;
            jumper.minDistance = minDistance;
            jumper.maxDistance = maxDistance;
            jumper.seed = seed;
        }

        // A flag gate: two flag poles 8 m apart with a banner between them, facing the direction of travel.
        // A start/finish line painted on the deck: two columns of black and white tiles across the whole width.
        static void CheckeredLine(Vector3 center, int tilesAcross)
        {
            var root = new GameObject("Checkered Line");
            for (int i = 0; i < tilesAcross; i++)
            {
                for (int column = 0; column < 2; column++)
                {
                    bool white = (i + column) % 2 == 0;
                    var tile = Box("Tile", new Vector3(center.x - 0.5f + column, center.y, center.z - tilesAcross * 0.5f + i + 0.5f),
                        new Vector3(1f, 0.02f, 1f), white ? new Color(0.97f, 0.97f, 0.97f) : new Color(0.08f, 0.08f, 0.1f));
                    Object.DestroyImmediate(tile.GetComponent<Collider>());
                    tile.transform.SetParent(root.transform, true);
                }
            }
        }

        internal static Checkpoint Gate(string name, Vector3 position, float yawDegrees, Color flagColor, float width = 8f)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var gate = root.AddComponent<Checkpoint>();
            gate.width = width;

            for (int side = -1; side <= 1; side += 2)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Flag Pole";
                pole.transform.SetParent(root.transform, false);
                pole.transform.localPosition = new Vector3(side * width * 0.5f, 2f, 0f);
                pole.transform.localScale = new Vector3(0.25f, 2.6f, 0.25f); // 5.2 m tall, from below the water up
                Object.DestroyImmediate(pole.GetComponent<Collider>());
                GrayboxMaterials.Tint(pole, new Color(0.95f, 0.95f, 0.95f));

                var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.name = "Flag";
                flag.transform.SetParent(root.transform, false);
                flag.transform.localPosition = new Vector3(side * width * 0.5f - side * 0.7f, 4.2f, 0f);
                flag.transform.localScale = new Vector3(1.4f, 0.9f, 0.05f);
                Object.DestroyImmediate(flag.GetComponent<Collider>());
                GrayboxMaterials.Tint(flag, flagColor);
            }

            // Thin see-through banner joining the poles, so the gap to run through reads at a glance.
            var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            banner.transform.SetParent(root.transform, false);
            banner.transform.localPosition = new Vector3(0f, 4.4f, 0f);
            banner.transform.localScale = new Vector3(width, 0.3f, 0.05f);
            Object.DestroyImmediate(banner.GetComponent<Collider>());
            GrayboxMaterials.Tint(banner, flagColor);

            return gate;
        }

        // Inflatable deck: saturated blue, floating with its top 0.6 m above the sea.
        internal static GameObject Deck(string name, Vector3 position, Vector3 scale)
        {
            var color = new Color(0.15f, 0.4f, 0.95f);
            var deck = Box(name, position, scale, color);
            GrayboxMaterials.TintQuilted(deck, color); // puffy, quilted like a pool inflatable
            Foam(deck, new Vector3(scale.x + 1.4f, 0.04f, scale.z + 1.4f));

            // Fat white tubes along the edges (for looks only: you can still climb out of the water onto the deck).
            var tubeColor = new Color(0.97f, 0.97f, 1f);
            for (int side = -1; side <= 1; side += 2)
            {
                var along = Cyl("Edge Tube", new Vector3(position.x, 0.75f, position.z + side * (scale.z * 0.5f - 0.25f)), new Vector3(0.7f, scale.x * 0.5f, 0.7f), tubeColor);
                along.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                GrayboxMaterials.TintQuilted(along, tubeColor);
                Object.DestroyImmediate(along.GetComponent<Collider>());
                along.transform.SetParent(deck.transform, true);

                var across = Cyl("Edge Tube", new Vector3(position.x + side * (scale.x * 0.5f - 0.25f), 0.75f, position.z), new Vector3(0.7f, scale.z * 0.5f, 0.7f), tubeColor);
                across.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                GrayboxMaterials.TintQuilted(across, tubeColor);
                Object.DestroyImmediate(across.GetComponent<Collider>());
                across.transform.SetParent(deck.transform, true);
            }
            return deck;
        }

        // A rim of white foam where a deck meets the water, so decks look like they float IN the sea.
        internal static void Foam(GameObject owner, Vector3 size)
        {
            var foam = Box("Foam", new Vector3(owner.transform.position.x, 0.03f, owner.transform.position.z), size, new Color(0.92f, 0.98f, 1f));
            Object.DestroyImmediate(foam.GetComponent<Collider>());
            foam.transform.SetParent(owner.transform, true);
        }

        internal static GameObject Disc(string name, Vector3 position, float diameter)
        {
            // Cylinder primitives are 2 m tall and 1 m radius at scale 1.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = new Vector3(diameter, 0.6f, diameter);
            GrayboxMaterials.Tint(go, new Color(1f, 0.55f, 0.1f));

            // A round rim of foam around the disc.
            var foam = Cyl("Foam", new Vector3(position.x, 0.03f, position.z), new Vector3(diameter + 1.4f, 0.02f, diameter + 1.4f), new Color(0.92f, 0.98f, 1f));
            Object.DestroyImmediate(foam.GetComponent<Collider>());
            foam.transform.SetParent(go.transform, true);
            return go;
        }

        internal static void Trim(string name, Vector3 position, Vector3 scale)
        {
            var go = Box(name, position, scale, new Color(1f, 0.6f, 0.1f));
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        internal static GameObject Ball(string name, Vector3 position, float diameter, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            GrayboxMaterials.Tint(go, color);
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            GrayboxMaterials.Tint(go, color);
            return go;
        }

        internal static GameObject Cyl(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            GrayboxMaterials.Tint(go, color);
            return go;
        }
    }
}
