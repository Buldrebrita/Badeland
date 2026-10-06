using System.Collections.Generic;
using Badeland.Systems;
using Badeland.World;
using Unity.Netcode;
using UnityEngine;

namespace Badeland.Networking
{
    /// <summary>
    /// One per scene, placed in the scene with a NetworkObject. Connects the jumping fish and fish throwing to
    /// the network:
    ///  - Fish leaps are decided by the shared clock, so every player sees the same fish. This object only
    ///    settles WHO caught a fish: the host accepts the first request for each fish and tells everyone.
    ///  - Throws go through <see cref="NetworkPlayer"/>.
    ///  - Installs the network's shared clock for everything that uses <see cref="GameClock"/>.
    /// </summary>
    public class FishNetwork : NetworkBehaviour
    {
        /// <summary>Every fish species that can be held. The list order is the network id of a species, so it must be the same on every machine.</summary>
        public FishSpecies[] allSpecies;

        static FishNetwork _instance;

        readonly HashSet<long> _takenOnHost = new HashSet<long>();
        FishJumper[] _jumpers;

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

        public override void OnNetworkSpawn()
        {
            _instance = this;
            _jumpers = FindObjectsByType<FishJumper>(FindObjectsSortMode.None);

            // Everything that follows the shared clock now follows the network's.
            GameClock.Provider = () => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
                ? NetworkManager.Singleton.ServerTime.Time
                : Time.timeAsDouble;

            FishJumper.CatchRequested = (jumper, slot) => RequestCatchServerRpc(jumper.Id, slot);

            FishCarrier.NetworkThrow = (from, target, species, seconds) =>
            {
                var thrower = from.GetComponent<NetworkPlayer>();
                var receiver = target.GetComponent<NetworkPlayer>();
                if (thrower == null || receiver == null) return false;
                thrower.RequestThrow(receiver, species, seconds);
                return true;
            };
        }

        public override void OnNetworkDespawn()
        {
            if (_instance == this) _instance = null;
            GameClock.Provider = null;
            FishJumper.CatchRequested = null;
            FishCarrier.NetworkThrow = null;
        }

        // A player thinks they caught a fish. Only the host answers, and only the first request per fish wins.
        [ServerRpc(RequireOwnership = false)]
        void RequestCatchServerRpc(int jumperId, int slot, ServerRpcParams rpcParams = default)
        {
            long key = ((long)jumperId << 32) | (uint)slot;
            if (!_takenOnHost.Add(key)) return; // someone was faster

            ConfirmCatchClientRpc(jumperId, slot, rpcParams.Receive.SenderClientId);
        }

        [ClientRpc]
        void ConfirmCatchClientRpc(int jumperId, int slot, ulong catcherClientId)
        {
            if (_jumpers == null) return;
            for (int i = 0; i < _jumpers.Length; i++)
            {
                if (_jumpers[i].Id != jumperId) continue;
                _jumpers[i].ConfirmCaught(slot, catcherClientId == NetworkManager.LocalClientId);
                return;
            }
        }
    }
}
