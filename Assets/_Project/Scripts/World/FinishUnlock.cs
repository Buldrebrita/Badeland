using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Switches things on once every player has finished all the laps (the bridge to the big platform inflates).
    /// The objects start switched off.
    /// </summary>
    public class FinishUnlock : MonoBehaviour
    {
        public LapTracker tracker;
        public GameObject[] enableOnFinish;

        void Awake()
        {
            foreach (var go in enableOnFinish)
                if (go != null) go.SetActive(false);
        }

        void OnEnable()
        {
            if (tracker != null) tracker.AllPlayersFinished += Unlock;
        }

        void OnDisable()
        {
            if (tracker != null) tracker.AllPlayersFinished -= Unlock;
        }

        void Unlock()
        {
            foreach (var go in enableOnFinish)
                if (go != null) go.SetActive(true);
        }
    }
}
