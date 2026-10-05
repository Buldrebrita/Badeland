using System.IO;
using Badeland.CameraSystem;
using Badeland.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// One-click builder for the S1 gray-box test scene: ground, jump test blocks, a player and the camera.
    /// Run it from the menu: Badeland > Create S1 Test Scene.
    /// </summary>
    public static class S1SceneBuilder
    {
        const string SceneFolder = "Assets/_Project/Scenes/Levels";
        const string SettingsFolder = "Assets/_Project/Settings";

        [MenuItem("Badeland/Create S1 Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(SettingsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Movement settings asset (reused if it already exists, so your tuning is not overwritten).
            string settingsPath = SettingsFolder + "/MovementSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Tint(ground, new Color(0.35f, 0.75f, 0.45f));

            // Jump test blocks: steps of increasing height, a wide gap and a tall wall.
            MakeBlock("Step 0.5m", new Vector3(5f, 0.25f, 0f), new Vector3(3f, 0.5f, 3f));
            MakeBlock("Step 1m", new Vector3(9f, 0.5f, 0f), new Vector3(3f, 1f, 3f));
            MakeBlock("Step 2m", new Vector3(13f, 1f, 0f), new Vector3(3f, 2f, 3f));
            MakeBlock("Platform A", new Vector3(-6f, 0.5f, 6f), new Vector3(3f, 1f, 3f));
            MakeBlock("Platform B (gap jump)", new Vector3(-6f, 0.5f, 12f), new Vector3(3f, 1f, 3f));
            MakeBlock("Tall wall (too high)", new Vector3(0f, 2.5f, 12f), new Vector3(6f, 5f, 1f));

            // Player: capsule with the controller scripts. The primitive's own collider is removed
            // because CharacterController provides its own.
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            Tint(player, new Color(1f, 0.55f, 0.15f));

            // A small "nose" cube so you can see which way the character faces.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
            nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            Tint(nose, Color.white);

            player.AddComponent<MovementModifiers>();
            var controller = player.AddComponent<PlayerController>(); // adds CharacterController and PlayerInputReader too
            var so = new SerializedObject(controller);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("visual").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Camera rig, with the scene's Main Camera parented to it.
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

            string scenePath = SceneFolder + "/Graybox_S1.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log("Badeland: S1 test scene created at " + scenePath + ". Press Play. Tune the feel in " + settingsPath);
        }

        static void MakeBlock(string name, Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            Tint(block, new Color(0.35f, 0.6f, 0.95f));
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            // Uses the default pipeline material's colour property; in URP the material shows as magenta
            // until converted, so we create a simple URP/Lit material when available.
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            renderer.sharedMaterial = mat;
        }
    }
}
