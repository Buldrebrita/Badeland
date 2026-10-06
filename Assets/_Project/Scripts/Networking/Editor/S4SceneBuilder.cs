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
            playerObject.AddComponent<NetworkPlayer>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(playerObject, PrefabFolder + "/NetworkPlayer.prefab");
            Object.DestroyImmediate(playerObject);

            // ---- The network manager: connects players and creates a NetworkPlayer for each one.
            var managerObject = new GameObject("NetworkManager");
            var transport = managerObject.AddComponent<UnityTransport>();
            var manager = managerObject.AddComponent<NetworkManager>();
            if (manager.NetworkConfig == null) manager.NetworkConfig = new NetworkConfig();
            manager.NetworkConfig.PlayerPrefab = prefab;
            manager.NetworkConfig.NetworkTransport = transport;

            // ---- Fish and clock sync (an in-scene network object).
            var fishObject = new GameObject("FishNetwork");
            fishObject.AddComponent<NetworkObject>();
            var fishNetwork = fishObject.AddComponent<FishNetwork>();
            fishNetwork.allSpecies = course.species;

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
