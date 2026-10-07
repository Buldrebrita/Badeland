using UnityEngine;
using UnityEngine.SceneManagement;

namespace Badeland.World
{
    /// <summary>
    /// Moves everyone to another scene (the next chapter or area). Offline it just loads the scene. Online the host
    /// loads it for everyone through the network layer, which sets <see cref="NetworkLoad"/>.
    /// The scene must be listed in the Build Settings (the scene builders do this).
    /// </summary>
    public static class ChapterTransition
    {
        /// <summary>Set by the networking layer on the host.</summary>
        public static System.Action<string> NetworkLoad;

        static bool _loading;

        public static void Go(string sceneName)
        {
            if (_loading || string.IsNullOrEmpty(sceneName)) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("Badeland: the scene '" + sceneName + "' is not in the Build Settings, so the game cannot go on to it. " +
                                 "Build it with the Badeland menu (Create Chapter 2 ...), which also adds it to the Build Settings.");
                return;
            }

            _loading = true;

            if (NetworkLoad != null) NetworkLoad(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        /// <summary>Called by the new scene when it has started, so the next transition can happen.</summary>
        public static void Done() => _loading = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _loading = false;
    }
}
