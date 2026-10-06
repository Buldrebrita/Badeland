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
    /// One-click builder for the S3 course test scene: a rectangular circuit run three times, with bounce pads,
    /// a rotating bar, jumping fish (cod, salmon, clownfish), lap counting and subtle per-lap changes.
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

            // ---- The circuit. Counter-clockwise, corridor 8 m wide: outer walls at x +-24 / z +-18, island at x +-16 / z +-10.
            Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 50f), Grass);
            Box("Wall North", new Vector3(0f, 1.5f, 18.5f), new Vector3(50f, 3f, 1f), Wall);
            Box("Wall South", new Vector3(0f, 1.5f, -18.5f), new Vector3(50f, 3f, 1f), Wall);
            Box("Wall East", new Vector3(24.5f, 1.5f, 0f), new Vector3(1f, 3f, 38f), Wall);
            Box("Wall West", new Vector3(-24.5f, 1.5f, 0f), new Vector3(1f, 3f, 38f), Wall);
            Box("Island", new Vector3(0f, 1.5f, 0f), new Vector3(32f, 3f, 20f), Wall);

            // ---- Checkpoints: right, top, left, then the finish line (bottom, where the lap started).
            var cpRight = Gate("Checkpoint 1 (right)", new Vector3(20f, 1.5f, 0f), new Vector3(8f, 3f, 0.6f), new Color(1f, 0.9f, 0.2f, 0.25f));
            var cpTop = Gate("Checkpoint 2 (top)", new Vector3(0f, 1.5f, 14f), new Vector3(0.6f, 3f, 8f), new Color(1f, 0.9f, 0.2f, 0.25f));
            var cpLeft = Gate("Checkpoint 3 (left)", new Vector3(-20f, 1.5f, 0f), new Vector3(8f, 3f, 0.6f), new Color(1f, 0.9f, 0.2f, 0.25f));
            var finish = Gate("Finish line", new Vector3(0f, 1.5f, -14f), new Vector3(0.6f, 3f, 8f), new Color(0.2f, 1f, 0.3f, 0.4f));

            var trackerGo = new GameObject("LapTracker");
            var tracker = trackerGo.AddComponent<LapTracker>();
            tracker.checkpoints = new[] { cpRight, cpTop, cpLeft, finish };
            tracker.totalLaps = 3;

            // ---- Obstacles
            // Right straight: three bounce pads in a row.
            for (int i = 0; i < 3; i++)
            {
                var pad = Box("Bounce Pad " + (i + 1), new Vector3(20f, 0.1f, -6f + i * 6f), new Vector3(3f, 0.2f, 3f), Pink);
                var box = pad.GetComponent<BoxCollider>();
                box.isTrigger = true;
                pad.AddComponent<BouncePad>();
            }

            // Top straight: a bar sweeping the whole width of the corridor.
            var bar = Box("Rotating Bar", new Vector3(0f, 0.5f, 14f), new Vector3(7f, 0.6f, 0.5f), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
            bar.AddComponent<RotatingBar>();

            // ---- Jumping fish
            MakeFish("Fish Cod", cod, new Vector3(16.5f, 0.5f, 3f), new Vector3(23.5f, 0.5f, 3f), 3f, 0f);
            MakeFish("Fish Salmon", salmon, new Vector3(-1f, 0.5f, 10.5f), new Vector3(-1f, 0.5f, 17.5f), 3.5f, 2f);
            MakeFish("Fish Clownfish", clown, new Vector3(-23.5f, 0.5f, 0f), new Vector3(-16.5f, 0.5f, 0f), 3f, 4f);

            // ---- Subtle per-lap changes near the start (see DESIGN.md 4b). Lap 1 is normal.
            var towel = Box("Towel", new Vector3(-4f, 0.06f, -12f), new Vector3(1.6f, 0.1f, 0.9f), new Color(1f, 0.3f, 0.3f));
            var sunglasses = Box("Sunglasses", new Vector3(-4f, 0.16f, -12f), new Vector3(0.5f, 0.1f, 0.2f), new Color(0.05f, 0.05f, 0.05f));
            var chair = Box("Lifeguard Chair", new Vector3(5f, 0.9f, -16.5f), new Vector3(1f, 1.8f, 1f), new Color(0.95f, 0.95f, 0.95f));
            var cone = Cyl("Cone", new Vector3(9f, 0.5f, -12f), new Vector3(0.6f, 0.5f, 0.6f), new Color(1f, 0.5f, 0.1f));
            var sign = Box("Park Sign", new Vector3(-12f, 1.6f, -17.9f), new Vector3(4f, 1.2f, 0.2f), Color.white);
            // Floor objects must not block the player.
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
            player.transform.position = new Vector3(-8f, 1.1f, -14f);
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
            Debug.Log("Badeland: S3 course scene created at " + scenePath + ". Press Play and run to the right (counter-clockwise).");
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

        static void MakeFish(string name, FishSpecies species, Vector3 start, Vector3 end, float arc, float phase)
        {
            var root = new GameObject(name);
            root.transform.position = start;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie along the travel direction
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            GrayboxMaterials.Tint(body, species.color);

            var jumper = root.AddComponent<FishJumper>();
            jumper.species = species;
            jumper.visual = body;
            jumper.startPoint = start;
            jumper.endPoint = end;
            jumper.arcHeight = arc;
            jumper.phase = phase;
        }

        static Checkpoint Gate(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            GrayboxMaterials.TintWater(go, color); // see-through, so it reads as a gate
            go.GetComponent<BoxCollider>().isTrigger = true;
            return go.AddComponent<Checkpoint>();
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
