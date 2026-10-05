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
    /// One-click builder for the S2 swimming test scene: a pool with steps, a ledge to jump in from,
    /// transparent water, the player and the camera. Menu: Badeland > Create S2 Swimming Test Scene.
    /// </summary>
    public static class S2SceneBuilder
    {
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";

        // Pool footprint (x from -6 to 6, z from 6 to 18). The ground top is y = 0, the pool floor y = -3,
        // and the water surface sits just under the ground edge.
        const float PoolMinX = -6f, PoolMaxX = 6f, PoolMinZ = 6f, PoolMaxZ = 18f;
        const float FloorY = -3f, WaterTopY = -0.3f;

        static readonly Color Grass = new Color(0.35f, 0.75f, 0.45f);
        static readonly Color Tile = new Color(0.85f, 0.85f, 0.9f);
        static readonly Color Block = new Color(0.35f, 0.6f, 0.95f);

        [MenuItem("Badeland/Create S2 Swimming Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            string settingsPath = SettingsFolder + "/MovementSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            // Ground around the pool: four slabs reaching down to the pool floor, so the pool has walls.
            float h = 3f; // slab height, from y = -3 to 0
            Slab("Ground West", -30f, PoolMinX, -30f, 30f, h);
            Slab("Ground East", PoolMaxX, 30f, -30f, 30f, h);
            Slab("Ground South", PoolMinX, PoolMaxX, -30f, PoolMinZ, h);
            Slab("Ground North", PoolMinX, PoolMaxX, PoolMaxZ, 30f, h);

            // Pool floor
            var floor = Box("Pool Floor", new Vector3((PoolMinX + PoolMaxX) / 2f, FloorY - 0.5f, (PoolMinZ + PoolMaxZ) / 2f),
                new Vector3(PoolMaxX - PoolMinX, 1f, PoolMaxZ - PoolMinZ), Tile);

            // Steps down into the pool at the south edge (tops at -0.5, -1.1, -1.7, -2.3).
            for (int i = 0; i < 4; i++)
            {
                float top = -0.5f - 0.6f * i;
                float z = PoolMinZ + 0.5f + i;
                float height = top - FloorY;
                Box("Pool Step " + (i + 1), new Vector3(0f, FloorY + height / 2f, z), new Vector3(4f, height, 1f), Tile);
            }

            // Ledge to jump in from, reached by stairs of 1.2 m so it needs real jumping.
            for (int i = 0; i < 4; i++)
            {
                float top = 1.2f * (i + 1);
                Box("Stair " + (i + 1), new Vector3(10f + i * 3f, top / 2f, 0f), new Vector3(3f, top, 3f), Block);
            }

            // Water volume: the cube's trigger collider is the water, its top face the surface.
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Water";
            float depth = WaterTopY - FloorY;
            water.transform.position = new Vector3((PoolMinX + PoolMaxX) / 2f, FloorY + depth / 2f, (PoolMinZ + PoolMaxZ) / 2f);
            water.transform.localScale = new Vector3(PoolMaxX - PoolMinX, depth, PoolMaxZ - PoolMinZ);
            GrayboxMaterials.TintWater(water, new Color(0.2f, 0.65f, 1f, 0.55f));
            water.GetComponent<BoxCollider>().isTrigger = true;
            water.AddComponent<WaterVolume>();

            // Player
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.1f, -5f);
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
            var so = new SerializedObject(controller);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("visual").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Camera rig (same values as the tuned S1 camera: pitch 30, distance 18)
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

            string scenePath = SceneFolder + "/Graybox_S2_Swimming.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Badeland: S2 swimming scene created at " + scenePath + ". Press Play, walk north and jump in.");
        }

        static void Slab(string name, float minX, float maxX, float minZ, float maxZ, float height)
        {
            Box(name, new Vector3((minX + maxX) / 2f, -height / 2f, (minZ + maxZ) / 2f),
                new Vector3(maxX - minX, height, maxZ - minZ), Grass);
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
    }
}
