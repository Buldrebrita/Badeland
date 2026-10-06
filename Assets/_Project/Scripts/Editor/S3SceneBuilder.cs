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
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

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

            // ---- Sea: a big translucent water volume (top face = surface at y 0) over a dark seabed.
            var sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sea.name = "Sea";
            sea.transform.position = new Vector3(0f, -4f, 0f);
            sea.transform.localScale = new Vector3(160f, 8f, 160f);
            GrayboxMaterials.TintWater(sea, new Color(0.1f, 0.55f, 1f, 0.6f));
            sea.GetComponent<BoxCollider>().isTrigger = true;
            sea.AddComponent<WaterVolume>();
            Box("Seabed", new Vector3(0f, -8.5f, 0f), new Vector3(160f, 1f, 160f), new Color(0.05f, 0.25f, 0.6f));

            // ---- The floating circuit (counter-clockwise, 8 m wide). Deck tops sit at y 0.6, 0.6 m above the sea.
            // Bottom and top straights include the four corners. The left straight is a gap crossed on discs.
            Deck("Deck Bottom (start)", new Vector3(0f, 0f, -14f), new Vector3(48f, 1.2f, 8f));
            Deck("Deck Top", new Vector3(0f, 0f, 14f), new Vector3(48f, 1.2f, 8f));
            Deck("Deck Right", new Vector3(20f, 0f, 0f), new Vector3(8f, 1.2f, 20f));
            for (int i = 0; i < 3; i++)
                Disc("Stepping Disc " + (i + 1), new Vector3(-20f, 0f, -6.6f + i * 6.6f), 4.5f);

            // Orange borders (visual only) along the outer edge of each deck.
            Trim("Trim Bottom", new Vector3(0f, 0.62f, -17.8f), new Vector3(48f, 0.1f, 0.4f));
            Trim("Trim Top", new Vector3(0f, 0.62f, 17.8f), new Vector3(48f, 0.1f, 0.4f));
            Trim("Trim Right", new Vector3(23.8f, 0.62f, 0f), new Vector3(0.4f, 0.1f, 20f));

            // Green inflatable bumper posts on the outer corners, and beach balls as soft obstacles.
            foreach (var corner in new[] { new Vector2(22.4f, 16.4f), new Vector2(-22.4f, 16.4f), new Vector2(22.4f, -16.4f), new Vector2(-22.4f, -16.4f) })
                Cyl("Bumper Post", new Vector3(corner.x, 1.6f, corner.y), new Vector3(1.6f, 1f, 1.6f), new Color(0.35f, 0.8f, 0.3f));
            Ball("Beach Ball", new Vector3(-12f, 1.85f, -17f), 2.5f, new Color(1f, 0.3f, 0.3f), true);
            Ball("Beach Ball", new Vector3(13f, 1.85f, -11f), 2.5f, new Color(1f, 0.9f, 0.2f), true);
            Ball("Beach Ball", new Vector3(-8f, 1.85f, 17f), 2.5f, new Color(0.3f, 0.5f, 1f), true);
            Ball("Beach Ball (floating)", new Vector3(30f, 0.6f, 6f), 2.5f, new Color(1f, 0.4f, 0.8f), false);
            Ball("Beach Ball (floating)", new Vector3(-30f, 0.6f, -12f), 2.5f, new Color(0.3f, 0.9f, 0.4f), false);

            // Scenery: far-away decks you cannot reach, for the look of a big park.
            Deck("Far Deck 1", new Vector3(55f, 0f, 20f), new Vector3(20f, 1.2f, 12f));
            Deck("Far Deck 2", new Vector3(-55f, 0f, -25f), new Vector3(16f, 1.2f, 16f));
            Deck("Far Deck 3", new Vector3(10f, 0f, 55f), new Vector3(24f, 1.2f, 10f));

            // ---- Flag gates (the forward arrow shows the direction of travel).
            var yellow = new Color(1f, 0.85f, 0.1f);
            var cpRight = Gate("Checkpoint 1 (right)", new Vector3(20f, 0f, 0f), 0f, yellow);
            var cpTop = Gate("Checkpoint 2 (top)", new Vector3(0f, 0f, 14f), -90f, yellow);
            var cpLeft = Gate("Checkpoint 3 (left)", new Vector3(-20f, 0f, 0f), 180f, yellow);
            var finish = Gate("Finish line", new Vector3(0f, 0f, -14f), 90f, new Color(0.2f, 0.9f, 0.3f));

            var trackerGo = new GameObject("LapTracker");
            var tracker = trackerGo.AddComponent<LapTracker>();
            tracker.checkpoints = new[] { cpRight, cpTop, cpLeft, finish };
            tracker.totalLaps = 3;

            // ---- Obstacles
            // Right straight: three bounce pads in a row.
            for (int i = 0; i < 3; i++)
            {
                var pad = Box("Bounce Pad " + (i + 1), new Vector3(20f, 0.7f, -6f + i * 6f), new Vector3(3f, 0.2f, 3f), Pink);
                pad.GetComponent<BoxCollider>().isTrigger = true;
                pad.AddComponent<BouncePad>();
            }

            // Top straight: a bar sweeping the whole width of the deck.
            var bar = Box("Rotating Bar", new Vector3(0f, 1f, 14f), new Vector3(7f, 0.6f, 0.5f), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
            bar.AddComponent<RotatingBar>();

            // ---- Jumping fish: random places and times. Each area leaps from open water towards the deck.
            FishArea("Fish Area Right", new[] { cod, salmon }, new Vector3(27f, 0.3f, 0f), new Vector3(1f, 0f, 16f), 270f, 8f, 13f, 11);
            FishArea("Fish Area Top", new[] { salmon, clown, cod }, new Vector3(0f, 0.3f, 21f), new Vector3(30f, 0f, 1f), 180f, 8f, 13f, 22);
            FishArea("Fish Area Left (gap)", new[] { clown, cod }, new Vector3(-27f, 0.3f, 0f), new Vector3(1f, 0f, 16f), 90f, 6f, 11f, 33);

            // ---- Subtle per-lap changes near the start (see DESIGN.md 4b). Lap 1 is normal.
            var towel = Box("Towel", new Vector3(-4f, 0.66f, -12f), new Vector3(1.6f, 0.1f, 0.9f), new Color(1f, 0.3f, 0.3f));
            var sunglasses = Box("Sunglasses", new Vector3(-4f, 0.76f, -12f), new Vector3(0.5f, 0.1f, 0.2f), new Color(0.05f, 0.05f, 0.05f));
            var chair = Box("Lifeguard Chair", new Vector3(5f, 1.5f, -16.5f), new Vector3(1f, 1.8f, 1f), new Color(0.95f, 0.95f, 0.95f));
            var cone = Cyl("Cone", new Vector3(9f, 1.1f, -12f), new Vector3(0.6f, 0.5f, 0.6f), new Color(1f, 0.5f, 0.1f));
            var sign = Box("Park Sign", new Vector3(-12f, 1.2f, -14.5f), new Vector3(4f, 1.2f, 0.2f), Color.white);
            // Flat floor objects must not block the player.
            foreach (var go in new[] { towel, sunglasses, cone }) Object.DestroyImmediate(go.GetComponent<Collider>());

            var changesGo = new GameObject("LapChanges");
            var changes = changesGo.AddComponent<LapChanges>();
            changes.tracker = tracker;
            changes.entries = new List<LapChange>
            {
                new LapChange { target = towel, showFromLap = 2 },
                new LapChange { target = sunglasses, showFromLap = 3 },
                new LapChange { target = chair, moveFromLap = 3, moveOffset = new Vector3(0.7f, 0f, 0f) },
                new LapChange { target = cone, hideFromLap = 3 },
                new LapChange { target = sign, tintFromLap = 3, tintColor = new Color(1f, 0.6f, 0.6f) },
            };

            // ---- Player (starts at the bottom, facing the finish line)
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(-8f, 1.8f, -14f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
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

            // ---- Camera rig
            var cam = Camera.main;
            var rig = new GameObject("CameraRig");
            var rigComponent = rig.AddComponent<IsoCameraRig>();
            var rigSo = new SerializedObject(rigComponent);
            var targets = rigSo.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = player.transform;
            rigSo.ApplyModifiedPropertiesWithoutUndo();
            if (cam != null)
            {
                cam.transform.SetParent(rig.transform, false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }

            // ---- Test HUD
            var hud = new GameObject("RaceHud").AddComponent<RaceHud>();
            hud.tracker = tracker;

            string scenePath = SceneFolder + "/Graybox_S3_Course.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Badeland: S3 course scene created at " + scenePath + ". Press Play and run to the right (counter-clockwise). Fall in the water and swim back to a deck.");
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

        static void FishArea(string name, FishSpecies[] pool, Vector3 zoneCenter, Vector3 zoneSize, float heading,
            float minDistance, float maxDistance, int seed)
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
            jumper.minDistance = minDistance;
            jumper.maxDistance = maxDistance;
            jumper.seed = seed;
        }

        // A flag gate: two flag poles 8 m apart with a banner between them, facing the direction of travel.
        static Checkpoint Gate(string name, Vector3 position, float yawDegrees, Color flagColor)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var gate = root.AddComponent<Checkpoint>();
            gate.width = 8f;

            for (int side = -1; side <= 1; side += 2)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Flag Pole";
                pole.transform.SetParent(root.transform, false);
                pole.transform.localPosition = new Vector3(side * 4f, 2f, 0f);
                pole.transform.localScale = new Vector3(0.25f, 2.6f, 0.25f); // 5.2 m tall, from below the water up
                Object.DestroyImmediate(pole.GetComponent<Collider>());
                GrayboxMaterials.Tint(pole, new Color(0.95f, 0.95f, 0.95f));

                var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.name = "Flag";
                flag.transform.SetParent(root.transform, false);
                flag.transform.localPosition = new Vector3(side * 4f - side * 0.7f, 4.2f, 0f);
                flag.transform.localScale = new Vector3(1.4f, 0.9f, 0.05f);
                Object.DestroyImmediate(flag.GetComponent<Collider>());
                GrayboxMaterials.Tint(flag, flagColor);
            }

            // Thin see-through banner joining the poles, so the gap to run through reads at a glance.
            var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            banner.transform.SetParent(root.transform, false);
            banner.transform.localPosition = new Vector3(0f, 4.4f, 0f);
            banner.transform.localScale = new Vector3(8f, 0.3f, 0.05f);
            Object.DestroyImmediate(banner.GetComponent<Collider>());
            GrayboxMaterials.Tint(banner, flagColor);

            return gate;
        }

        // Inflatable deck: saturated blue, floating with its top 0.6 m above the sea.
        static GameObject Deck(string name, Vector3 position, Vector3 scale) => Box(name, position, scale, new Color(0.15f, 0.4f, 0.95f));

        static GameObject Disc(string name, Vector3 position, float diameter)
        {
            // Cylinder primitives are 2 m tall and 1 m radius at scale 1.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = new Vector3(diameter, 0.6f, diameter);
            GrayboxMaterials.Tint(go, new Color(1f, 0.55f, 0.1f));
            return go;
        }

        static void Trim(string name, Vector3 position, Vector3 scale)
        {
            var go = Box(name, position, scale, new Color(1f, 0.6f, 0.1f));
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static GameObject Ball(string name, Vector3 position, float diameter, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            GrayboxMaterials.Tint(go, color);
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            GrayboxMaterials.Tint(go, color);
            return go;
        }

        static GameObject Cyl(string name, Vector3 position, Vector3 scale, Color color)
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
