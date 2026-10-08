using System.IO;
using System.Linq;
using Badeland.EditorTools;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

namespace Badeland.Networking.EditorTools
{
    /// <summary>
    /// One-click builder for the S4 online test scene: the S3 course, but with no ready-made player. Instead it has
    /// a NetworkManager that spawns one player per connected person, plus Host / Join buttons on screen.
    /// Only appears once the Netcode for GameObjects package is installed. Menu: Badeland > Create S4 Network Test Scene.
    /// </summary>
    public static class S4SceneBuilder
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        const string SceneName = "Graybox_S4_Network";

        [MenuItem("Badeland/Create S4 Network Test Scene")]
        public static void Build()
        {
            var course = S3SceneBuilder.BuildCourse(false); // the same course, without an offline player
            if (course == null) return;

            // ---- The online player: the normal player plus the network scripts, saved as a prefab.
            Directory.CreateDirectory(PrefabFolder);
            var playerObject = S3SceneBuilder.CreatePlayerObject(course.settings);
            playerObject.name = "NetworkPlayer";
            playerObject.AddComponent<NetworkObject>();
            var networkPlayer = playerObject.AddComponent<NetworkPlayer>();
            networkPlayer.spawnOrigin = course.layout.startSpawn + new Vector3(-3.2f, 0f, 0f); // on the start platform, in a row
            networkPlayer.spawnStep = new Vector3(2.1f, 0f, 0f);
            // The materials the scene will hand to every spawned player.
            Material bodyMaterial = playerObject.GetComponent<Renderer>().sharedMaterial;
            var noseTransform = playerObject.transform.Find("Nose");
            Material noseMaterial = noseTransform != null ? noseTransform.GetComponent<Renderer>().sharedMaterial : null;

            string prefabPath = PrefabFolder + "/NetworkPlayer.prefab";
            AssetDatabase.DeleteAsset(prefabPath); // rebuilt from scratch, so nothing stale is carried over
            var prefab = PrefabUtility.SaveAsPrefabAsset(playerObject, prefabPath);
            Object.DestroyImmediate(playerObject);

            // ---- The network manager: connects players and creates a NetworkPlayer for each one.
            var managerObject = new GameObject("NetworkManager");
            var transport = managerObject.AddComponent<UnityTransport>();
            var manager = managerObject.AddComponent<NetworkManager>();
            if (manager.NetworkConfig == null) manager.NetworkConfig = new NetworkConfig();
            manager.NetworkConfig.PlayerPrefab = prefab;
            manager.NetworkConfig.NetworkTransport = transport;

            // ---- The list of fish species (so a species can be sent over the network as a number).
            var fishObject = new GameObject("FishNetwork");
            var fishNetwork = fishObject.AddComponent<FishNetwork>();
            fishNetwork.allSpecies = course.species;
            fishNetwork.playerMaterial = bodyMaterial;
            fishNetwork.noseMaterial = noseMaterial;

            // ---- Host / Join buttons
            new GameObject("NetworkMenu").AddComponent<NetworkMenu>();

            S3SceneBuilder.Save(course, SceneName,
                "Badeland: S4 network scene created at {0}. Press Play, then click Host. A second player clicks Join.");

            // Netcode loads scenes by their Build Settings entry, so the scene has to be listed there.
            string scenePath = "Assets/_Project/Scenes/Levels/" + SceneName + ".unity";
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
