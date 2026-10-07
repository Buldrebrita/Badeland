using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Builds a small stand-alone Windows test game of the online scene, to run next to the editor as "player two".
    /// More reliable than Multiplayer Play Mode. Menu: Badeland > Build Test Player (for online testing).
    /// </summary>
    public static class TestPlayerBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Levels/Graybox_S4_Network.unity";
        const string OutputFolder = "Builds/OnlineTest";

        [MenuItem("Badeland/Build Test Player (for online testing)")]
        public static void Build()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Badeland", "The online scene does not exist yet. Run Badeland > Create S4 Network Test Scene first.", "OK");
                return;
            }

            if (!EditorSceneManager_SaveIfNeeded()) return;

            PlayerSettings.runInBackground = true; // the window that is not in front keeps running

            Directory.CreateDirectory(OutputFolder);
            string exe = OutputFolder + "/BadelandOnlineTest.exe";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Badeland: test player built at " + Path.GetFullPath(exe) + ". Run it, then Host in one window and Join in the other.");
                EditorUtility.RevealInFinder(Path.GetFullPath(exe));
            }
            else
            {
                Debug.LogError("Badeland: the test player build failed (" + report.summary.result + "). See the errors above. " +
                               "If it says the Windows build support is missing, install it for this Unity version in Unity Hub (Installs > the gear > Add modules > Windows Build Support).");
            }
        }

        // Ask to save any open scene changes first, so the build uses what you see.
        static bool EditorSceneManager_SaveIfNeeded() => UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }
}
