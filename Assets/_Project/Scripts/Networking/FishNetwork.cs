using Badeland.World;
using UnityEngine;

namespace Badeland.Networking
{
    /// <summary>
    /// One per scene. Holds the list of every fish species, so a species can be sent over the network as a
    /// number. The list order must be identical on every machine, which it is because it comes from the scene.
    /// The network messages for catching and throwing fish live on <see cref="NetworkPlayer"/>.
    /// </summary>
    public class FishNetwork : MonoBehaviour
    {
        /// <summary>Every fish species that can be held. The list order is the network id of a species.</summary>
        public FishSpecies[] allSpecies;

        static FishNetwork _instance;

        void Awake() => _instance = this;

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static int IndexOf(FishSpecies species)
        {
            if (_instance == null || species == null) return -1;
            return System.Array.IndexOf(_instance.allSpecies, species);
        }

        public static FishSpecies SpeciesAt(int index)
        {
            if (_instance == null || index < 0 || index >= _instance.allSpecies.Length) return null;
            return _instance.allSpecies[index];
        }
    }
}
