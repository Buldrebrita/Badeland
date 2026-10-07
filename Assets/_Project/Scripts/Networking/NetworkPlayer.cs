using Badeland.CameraSystem;
using Badeland.Player;
using Badeland.Systems;
using Badeland.World;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine;

namespace Badeland.Networking
{
    /// <summary>
    /// Makes a player work online. Put on the player prefab next to a NetworkObject.
    ///
    /// Model for the spike: each player's own machine runs their own movement (so controls feel instant) and
    /// publishes the result; everyone else sees a smoothed copy. Owner-authoritative is the simplest model and
    /// fine for co-op with friends. It trusts every player, so it is not cheat-proof. See docs/S4_NETWORK.md.
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        /// <summary>What the owner publishes about their player, many times a second.</summary>
        public struct PlayerState : INetworkSerializable, System.IEquatable<PlayerState>
        {
            public Vector3 position;
            public float yaw;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref yaw);
            }

            public bool Equals(PlayerState other) => position == other.position && Mathf.Approximately(yaw, other.yaw);
        }

        [Header("Spawn (first player; the others line up beside them)")]
        public Vector3 spawnOrigin = new Vector3(-8f, 1.8f, -16f);
        public Vector3 spawnStep = new Vector3(0f, 0f, 1.6f);

        [Tooltip("Smoothing speed for other players. Higher = snappier, lower = smoother.")]
        public float remoteSmoothing = 18f;

        readonly NetworkVariable<PlayerState> _state = new NetworkVariable<PlayerState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Index of the fish held (see FishNetwork.AllSpecies), or -1 for none.
        readonly NetworkVariable<int> _heldFish = new NetworkVariable<int>(
            -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        static readonly System.Collections.Generic.Dictionary<ulong, NetworkPlayer> ByClient =
            new System.Collections.Generic.Dictionary<ulong, NetworkPlayer>();

        // True once the monster has swallowed this player. Written by the owner, read by everyone.
        readonly NetworkVariable<bool> _eaten = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        CharacterController _cc;
        PlayerController _controller;
        PlayerInputReader _input;
        FishCarrier _carrier;
        Renderer _bodyRenderer;
        Renderer _noseRenderer;
        bool _hasState;

        public ulong ClientId => OwnerClientId;
        public bool HoldsFish => _heldFish.Value >= 0;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputReader>();
            _carrier = GetComponent<FishCarrier>();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0) _bodyRenderer = renderers[0];
            if (renderers.Length > 1) _noseRenderer = renderers[1];
        }

        public override void OnNetworkSpawn()
        {
            ByClient[OwnerClientId] = this;
            _controller.NetworkId = (int)OwnerClientId;
            ApplySceneMaterials();
            Tint();

            if (IsOwner)
            {
                // Place at this player's spot on the start line. The controller must be off while teleporting.
                _cc.enabled = false;
                transform.position = spawnOrigin + spawnStep * (int)(OwnerClientId % 4);
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                _cc.enabled = true;

                _carrier.Caught += OnLocalCaught;
                _carrier.Lost += OnLocalLost;

                // Hook the shared pieces up to the network. Only our own player does this.
                GameClock.Provider = () => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
                    ? NetworkManager.Singleton.ServerTime.Time
                    : Time.timeAsDouble;
                FishJumper.CatchRequested = (jumper, slot) => RequestCatchServerRpc(jumper.Id, slot);
                FishCarrier.NetworkThrow = (from, target, species, seconds) =>
                {
                    var receiver = target.GetComponent<NetworkPlayer>();
                    if (receiver == null) return false;
                    RequestThrow(receiver, species, seconds);
                    return true;
                };

                // Trap, treasure and the monster all go through the host.
                HiddenTrap.TriggerRequested = id => RequestTrapServerRpc(id);
                SecretRoom.TreasureRequested = id => RequestTreasureServerRpc(id);
                MonsterEncounter.IsAuthority = () => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
                if (IsServer) MonsterEncounter.BroadcastStart = (strike, rails, warning) => StartEncounterClientRpc(strike, rails, warning);

                _controller.Swallowed += OnLocalSwallowed;
                _controller.Revived += OnLocalRevived;
                if (IsServer) ChapterTransition.NetworkLoad = name => NetworkManager.SceneManager.LoadScene(name, LoadSceneMode.Single);

                var rigForPrimary = IsoCameraRig.Instance;
                if (rigForPrimary != null) rigForPrimary.PrimaryTarget = transform;
            }
            else
            {
                // Other players' avatars are moved by the network, never by local input or physics.
                _controller.IsLocal = false;
                _carrier.IsLocal = false;
                _controller.enabled = false;
                _input.enabled = false;
                _cc.enabled = false;
                _heldFish.OnValueChanged += OnHeldFishChanged;
                OnHeldFishChanged(-1, _heldFish.Value);
                _eaten.OnValueChanged += (_, eaten) => { if (eaten) _controller.Eat(); else _controller.Revive(); };
                if (_eaten.Value) _controller.Eat();
                _state.OnValueChanged += (_, v) => _hasState = true;
                if (_state.Value.position != Vector3.zero)
                {
                    transform.position = _state.Value.position;
                    _hasState = true;
                }
            }

            // The camera frames every player.
            var rig = IsoCameraRig.Instance;
            if (rig != null) rig.AddTarget(transform);

            // Players survive a change of scene (the next chapter). The new scene has its own camera, so join it.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var newRig = IsoCameraRig.Instance;
            if (newRig == null) return;
            newRig.AddTarget(transform);
            if (IsOwner) newRig.PrimaryTarget = transform;
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (ByClient.TryGetValue(OwnerClientId, out var registered) && registered == this) ByClient.Remove(OwnerClientId);

            var rig = IsoCameraRig.Instance;
            if (rig != null) rig.RemoveTarget(transform);

            if (IsOwner)
            {
                _carrier.Caught -= OnLocalCaught;
                _carrier.Lost -= OnLocalLost;
                GameClock.Provider = null;
                FishJumper.CatchRequested = null;
                FishCarrier.NetworkThrow = null;
                HiddenTrap.TriggerRequested = null;
                SecretRoom.TreasureRequested = null;
                MonsterEncounter.IsAuthority = null;
                MonsterEncounter.BroadcastStart = null;
                _controller.Swallowed -= OnLocalSwallowed;
                _controller.Revived -= OnLocalRevived;
                ChapterTransition.NetworkLoad = null;
            }
            else
            {
                _heldFish.OnValueChanged -= OnHeldFishChanged;
            }
        }

        void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                _state.Value = new PlayerState
                {
                    position = transform.position,
                    yaw = transform.eulerAngles.y,
                };
                return;
            }

            if (!_hasState) return;

            // Glide towards the latest published position instead of jumping between network updates.
            var s = _state.Value;
            float k = 1f - Mathf.Exp(-remoteSmoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, s.position, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, s.yaw, 0f), k);
        }

        // ---------------------------------------------------------------- fish

        void OnLocalCaught(FishSpecies species)
        {
            _heldFish.Value = FishNetwork.IndexOf(species);
        }

        void OnLocalLost(FishSpecies species, FishLossReason reason)
        {
            _heldFish.Value = -1;
        }

        void OnHeldFishChanged(int previous, int current)
        {
            _carrier.MirrorHeld(FishNetwork.SpeciesAt(current));
        }

        void OnLocalSwallowed() => _eaten.Value = true;
        void OnLocalRevived() => _eaten.Value = false;

        // ---------------------------------------------------------------- trap, treasure, monster

        // The host remembers when each trap was last opened, and which treasures are gone.
        static readonly System.Collections.Generic.Dictionary<int, double> TrapBusyUntil = new System.Collections.Generic.Dictionary<int, double>();
        static readonly System.Collections.Generic.HashSet<int> TreasuresTaken = new System.Collections.Generic.HashSet<int>();

        [ServerRpc]
        void RequestTrapServerRpc(int trapId)
        {
            double now = NetworkManager.ServerTime.Time;
            if (TrapBusyUntil.TryGetValue(trapId, out double until) && now < until) return;
            TrapBusyUntil[trapId] = now + 9.0; // open time plus a little rest
            TrapOpenedClientRpc(trapId, now + 0.05);
        }

        [ClientRpc]
        void TrapOpenedClientRpc(int trapId, double time) => HiddenTrap.OpenById(trapId, time);

        [ServerRpc]
        void RequestTreasureServerRpc(int roomId)
        {
            if (!TreasuresTaken.Add(roomId)) return; // someone was faster
            TreasureTakenClientRpc(roomId, NetworkManager.ServerTime.Time + 0.05);
        }

        // Runs on everyone. The treasure is ours only on the machine of the player who took it.
        [ClientRpc]
        void TreasureTakenClientRpc(int roomId, double time) => SecretRoom.ConfirmById(roomId, time, IsOwner);

        [ClientRpc]
        void StartEncounterClientRpc(double strikeTime, bool raiseRails, float warningSeconds)
        {
            if (MonsterEncounter.Instance != null) MonsterEncounter.Instance.BeginAt(strikeTime, raiseRails, warningSeconds);
        }

        // The host remembers which fish are already taken (only used on the host).
        static readonly System.Collections.Generic.HashSet<long> TakenOnHost = new System.Collections.Generic.HashSet<long>();

        // Our player thinks it caught a fish. Only the host answers, and only the first request per fish wins.
        [ServerRpc]
        void RequestCatchServerRpc(int jumperId, int slot)
        {
            long key = ((long)jumperId << 32) | (uint)slot;
            if (!TakenOnHost.Add(key)) return; // someone was faster

            ConfirmCatchClientRpc(jumperId, slot);
        }

        // Runs on everyone. The fish is gone for all; it is ours only on the machine of the player who caught it.
        [ClientRpc]
        void ConfirmCatchClientRpc(int jumperId, int slot)
        {
            var jumpers = FishJumper.All;
            for (int i = 0; i < jumpers.Count; i++)
            {
                if (jumpers[i].Id != jumperId) continue;
                jumpers[i].ConfirmCaught(slot, IsOwner);
                return;
            }
        }

        /// <summary>Ask the host to pass a fish to another player. Called by the thrower's own machine.</summary>
        public void RequestThrow(NetworkPlayer target, FishSpecies species, float seconds)
        {
            int index = FishNetwork.IndexOf(species);
            if (index < 0) return;
            ThrowFishServerRpc(target.OwnerClientId, index, seconds);
        }

        [ServerRpc]
        void ThrowFishServerRpc(ulong targetClientId, int speciesIndex, float seconds)
        {
            Debug.Log("Badeland: host received a fish throw from player " + OwnerClientId + " to player " + targetClientId);

            // The host checks the target is still empty-handed, then forwards the fish to the target's own machine.
            if (!ByClient.TryGetValue(targetClientId, out var target))
            {
                Debug.LogWarning("Badeland: throw target " + targetClientId + " not found on the host.");
                return;
            }
            if (target.HoldsFish)
            {
                Debug.Log("Badeland: throw refused, the target is already holding a fish.");
                return;
            }

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
            };
            target.ReceiveFishClientRpc(speciesIndex, seconds, rpcParams);
            ShowThrowClientRpc(targetClientId, speciesIndex);
        }

        [ClientRpc]
        void ReceiveFishClientRpc(int speciesIndex, float seconds, ClientRpcParams rpcParams = default)
        {
            // Runs on the target player's own machine.
            if (!IsOwner) return;
            Debug.Log("Badeland: you were hit by a thrown fish (species " + speciesIndex + ").");
            _carrier.ReceiveThrown(FishNetwork.SpeciesAt(speciesIndex), seconds);
        }

        // Everyone except the thrower (who already saw it fly) sees the fish arc across.
        [ClientRpc]
        void ShowThrowClientRpc(ulong targetClientId, int speciesIndex)
        {
            if (IsOwner) return;
            if (!ByClient.TryGetValue(targetClientId, out var target)) return;
            var species = FishNetwork.SpeciesAt(speciesIndex);
            if (species == null) return;
            ThrownFishVisual.Spawn(transform.position + Vector3.up, target.transform.position + Vector3.up, species.color);
        }

        // ---------------------------------------------------------------- look

        // Use the materials stored in the scene, so a stale or broken material inside the prefab file cannot show up as pink.
        void ApplySceneMaterials()
        {
            if (_bodyRenderer != null && FishNetwork.PlayerMaterial != null) _bodyRenderer.sharedMaterial = FishNetwork.PlayerMaterial;
            if (_noseRenderer != null && FishNetwork.NoseMaterial != null) _noseRenderer.sharedMaterial = FishNetwork.NoseMaterial;
        }

        void Tint()
        {
            // A different colour per player so you can tell them apart.
            Color[] colors =
            {
                new Color(1f, 0.55f, 0.15f), new Color(0.25f, 0.7f, 1f),
                new Color(0.9f, 0.3f, 0.7f), new Color(0.5f, 0.85f, 0.3f),
            };
            Color c = colors[(int)(OwnerClientId % (ulong)colors.Length)];

            if (_bodyRenderer == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            _bodyRenderer.SetPropertyBlock(block);
        }
    }
}
