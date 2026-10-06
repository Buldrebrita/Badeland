using System;
using System.Collections.Generic;
using Badeland.CameraSystem;
using Badeland.Player;
using Badeland.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Badeland.World
{
    /// <summary>
    /// The end of Chapter 1.
    ///
    /// 1. The party finishes the laps and gathers on the big start/finish platform.
    /// 2. THE WARNING (about 7 s): the sea heaves, red alarm lights flash, a siren sounds, a voice says "Danger!
    ///    Do not go in the water!", and railings rise around the platform so nobody can jump in.
    /// 3. THE STRIKE: the light drops and a giant sea monster heaves up out of the water beside the platform, on a
    ///    long thick neck, looking at the players. Players stay in control and can run and jump, but within
    ///    <see cref="duration"/> seconds everyone is eaten, one by one.
    ///    - The monster rears back, its eyes lock on one player, then its head suddenly LEAPS forward and snaps. Anyone
    ///      in its jaws is eaten. Watch the eyes and the rearing, and get out of the way.
    ///    - In between, tentacles reach in from the sea and slap down on an orange circle, knocking people flying.
    ///    - The last lunge, at the very end, takes everyone who is left.
    /// 4. The screen fades to black: "To be continued...".
    ///
    /// Everything follows the shared clock, so all players see the same thing at the same moment. Each machine only
    /// decides whether its OWN player is hit.
    /// </summary>
    public class MonsterEncounter : MonoBehaviour
    {
        public static MonsterEncounter Instance { get; private set; }

        /// <summary>True once the sequence (warning and monster) has been started.</summary>
        public static bool Started => Instance != null && Instance._started;

        /// <summary>Set by the networking layer: true only on the host. Null offline (then this machine decides).</summary>
        public static Func<bool> IsAuthority;
        /// <summary>Set on the host by the networking layer. Sends (strike time, railings up, warning length) to everyone.</summary>
        public static Action<double, bool, float> BroadcastStart;

        /// <summary>A message for the on-screen display.</summary>
        public static string StatusText { get; private set; } = "";

        [Header("When it starts")]
        public LapTracker tracker;
        [Tooltip("World centre and size of the big platform area where it happens (the size's y is ignored).")]
        public Vector3 platformCenter;
        public Vector3 platformSize = new Vector3(34f, 1f, 24f);
        public float platformTopY = 0.6f;
        [Tooltip("Everyone on the platform for this long, and the warning starts.")]
        [Min(0f)] public float celebrationSeconds = 3f;
        [Tooltip("If someone never gets there, the warning starts anyway this long after the laps are done.")]
        [Min(5f)] public float fallbackSeconds = 40f;
        [Tooltip("How long the danger alarm lasts before the monster strikes.")]
        [Min(0f)] public float warningSeconds = 7f;

        [Header("The fight")]
        [Tooltip("Everyone is eaten at the latest this many seconds after the strike.")]
        [Min(10f)] public float duration = 20f;
        [Tooltip("How far in front of its jaws the head stops when it leaps, in metres.")]
        public float jawReach = 4.5f;

        [Header("Look (set up by the scene builder)")]
        public Light sun;
        public Renderer seaRenderer;
        public GameObject telegraphTemplate;
        [Tooltip("One section of a tentacle or the neck (a sphere). They are chains of these.")]
        public GameObject segmentTemplate;
        [Tooltip("The monster's head. It faces its local +Z. Jaw pivot, eyes and pupils are inside it.")]
        public Transform head;
        public Transform jawPivot;
        public Transform[] eyes;
        public Transform[] pupils;
        public float eyeRadius = 1.6f;
        [Tooltip("The body: a big hump that rises out of the sea with the head.")]
        public GameObject body;
        [Tooltip("Where the neck comes out of the sea.")]
        public Transform neckBase;
        [Tooltip("Where the head hangs while it watches the players.")]
        public Transform lurkPoint;
        [Tooltip("Where the slapping tentacles come out of the sea.")]
        public Transform[] tentacleBases;

        [Header("Sound (optional)")]
        [Tooltip("A recorded voice saying the warning. If empty, the computer's own voice is used on Windows.")]
        public AudioClip voiceClip;

        const int TentacleSections = 36;
        const int NeckSections = 30;
        const string WarningLine = "Danger! Do not go in the water!";

        class Chain
        {
            public GameObject root;
            public Transform[] parts;
            public Vector3 anchor;
        }

        class Attack
        {
            public float time;          // slaps: when the telegraph starts. Lunges: when the jaws snap.
            public float telegraph;
            public float radius;
            public bool grab;           // true = a lunge of the head (eats). false = a tentacle slap (knocks).
            public bool final;
            public bool targeted;
            public int order;
            public int baseIndex;
            public Vector3 spot;

            public GameObject disc;
            public Chain tentacle;
            public Vector3 center;
            public Vector3 hover;
            public float groundY;
            public bool locked;
            public bool resolved;
            public float resolvedAt;
            public bool finished;

            // Lunge state
            public PlayerController targetPlayer;
            public Vector3 lockedPoint;
            public Vector3 strikePos;
            public bool roared;
        }

        readonly List<Attack> _attacks = new List<Attack>();
        readonly List<PlayerController> _alive = new List<PlayerController>();

        bool _started;
        bool _fightBegan;
        bool _triggerSent;
        bool _raiseRails;
        double _strikeTime;
        float _warning;
        float _finishedSeenAt = -1f;
        float _onPlatformFor;
        float _endAt = -1f;
        bool _voiceOne, _voiceTwo;
        MaterialPropertyBlock _block;

        Chain _neck;
        Vector3 _bodyStart;
        Quaternion _headRotation = Quaternion.identity;

        // Look before the warning, so we know what to fade from.
        Color _sunColor0, _sky0, _equator0, _ground0, _fogColor0;
        float _sunIntensity0, _fogDensity0;
        bool _fog0;
        Material _seaMaterial;
        AudioSource _siren, _rumble, _effects, _voice;
        AudioClip _roarClip, _chompClip;

        void Awake()
        {
            Instance = this;
            _block = new MaterialPropertyBlock();
            foreach (var t in new[] { telegraphTemplate, segmentTemplate })
                if (t != null) t.SetActive(false);
            if (head != null) head.gameObject.SetActive(false);
            if (body != null) { _bodyStart = body.transform.position; body.SetActive(false); }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ before the warning

        void Update()
        {
            if (!_started)
            {
                WaitForStart();
                return;
            }

            float e = (float)(GameClock.Now - _strikeTime);
            if (e < 0f)
            {
                UpdateWarning(_warning + e);
                return;
            }

            if (!_fightBegan) BeginFight();

            RefreshAlive();
            UpdateLook(e);
            UpdateMonster(e);
            UpdateAttacks(e);
            UpdateEnding(e);
        }

        void WaitForStart()
        {
            bool lapsDone = tracker != null && tracker.AllFinished;
            StatusText = lapsDone ? "Everyone is done! Stay on the big platform..." : "";

            bool authority = IsAuthority == null || IsAuthority();
            if (!authority || _triggerSent) return;

            // Testing shortcuts: M = the whole sequence now (warning, then the monster). N = the monster at once.
            if (Keyboard.current != null)
            {
                if (Keyboard.current.mKey.wasPressedThisFrame) { Launch(warningSeconds, EveryoneOnPlatform()); return; }
                if (Keyboard.current.nKey.wasPressedThisFrame) { Launch(0f, false); return; }
            }

            if (!lapsDone) return;
            if (_finishedSeenAt < 0f) _finishedSeenAt = Time.time;

            bool everyoneOnPlatform = EveryoneOnPlatform();
            _onPlatformFor = everyoneOnPlatform ? _onPlatformFor + Time.deltaTime : 0f;

            if (_onPlatformFor >= celebrationSeconds)
                Launch(warningSeconds, true);
            else if (Time.time - _finishedSeenAt >= fallbackSeconds)
                Launch(warningSeconds, everyoneOnPlatform); // no railings if someone is still swimming about
        }

        bool EveryoneOnPlatform()
        {
            int counted = 0;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsEaten) continue;
                counted++;
                if (!OnPlatform(players[i])) return false;
            }
            return counted > 0;
        }

        bool OnPlatform(PlayerController p)
        {
            Vector3 pos = p.transform.position;
            return Mathf.Abs(pos.x - platformCenter.x) <= platformSize.x * 0.5f
                && Mathf.Abs(pos.z - platformCenter.z) <= platformSize.z * 0.5f
                && p.FeetY() > platformTopY - 0.5f;
        }

        void Launch(float warning, bool raiseRails)
        {
            _triggerSent = true;
            // A moment ahead, so every machine has the message before the warning begins.
            double strike = GameClock.Now + 0.15 + warning;
            if (BroadcastStart != null) BroadcastStart(strike, raiseRails, warning);
            else BeginAt(strike, raiseRails, warning);
        }

        /// <summary>Called on every machine with the same times.</summary>
        public void BeginAt(double strikeTime, bool raiseRails, float warningSeconds)
        {
            if (_started) return;
            _started = true;
            _strikeTime = strikeTime;
            _raiseRails = raiseRails;
            _warning = warningSeconds;
            StatusText = "";

            BuildPlan();

            if (sun != null) { _sunColor0 = sun.color; _sunIntensity0 = sun.intensity; }
            _sky0 = RenderSettings.ambientSkyColor;
            _equator0 = RenderSettings.ambientEquatorColor;
            _ground0 = RenderSettings.ambientGroundColor;
            _fog0 = RenderSettings.fog;
            _fogColor0 = RenderSettings.fogColor;
            _fogDensity0 = RenderSettings.fogDensity;
            if (seaRenderer != null) _seaMaterial = seaRenderer.material; // our own copy, so the shared asset is untouched

            _effects = gameObject.AddComponent<AudioSource>();
            _effects.spatialBlend = 0f;
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.spatialBlend = 0f;
            _roarClip = MakeRoar();
            _chompClip = MakeChomp();

            if (_warning > 0.1f)
            {
                _siren = gameObject.AddComponent<AudioSource>();
                _siren.clip = MakeSiren();
                _siren.loop = true;
                _siren.spatialBlend = 0f;
                _siren.volume = 0f;
                _siren.Play();

                if (_raiseRails && RailingRaiser.Instance != null)
                    RailingRaiser.Instance.Raise(_strikeTime - _warning + 0.4);
            }
        }

        // ------------------------------------------------------------------ the warning

        void UpdateWarning(float elapsed)
        {
            float k = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, _warning));
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);

            if (SeaMotion.Instance != null) SeaMotion.Instance.Roughness = Mathf.SmoothStep(0f, 1f, k);
            if (AlarmLights.Instance != null) AlarmLights.Instance.Intensity = 1f;

            if (sun != null)
            {
                sun.color = Color.Lerp(_sunColor0, new Color(1f, 0.25f, 0.2f), 0.5f * pulse * Mathf.Min(1f, k * 3f));
                sun.intensity = _sunIntensity0 * Mathf.Lerp(1f, 0.7f, k);
            }

            if (_siren != null) _siren.volume = Mathf.Lerp(0f, 0.55f, Mathf.Clamp01(elapsed / 0.8f));

            // The voice says it at the start and again halfway through.
            if (!_voiceOne && elapsed >= 0.3f)
            {
                _voiceOne = true;
                VoiceAlarm.Speak(WarningLine, voiceClip, _voice);
            }
            if (!_voiceTwo && _warning >= 5f && elapsed >= _warning * 0.5f)
            {
                _voiceTwo = true;
                VoiceAlarm.Speak(WarningLine, voiceClip, _voice);
            }

            StatusText = pulse > 0.35f ? "DANGER! DON'T GO IN THE WATER!" : "";
        }

        // ------------------------------------------------------------------ the strike

        void BeginFight()
        {
            _fightBegan = true;

            if (_siren != null) { _siren.Stop(); _siren = null; }
            if (AlarmLights.Instance != null) AlarmLights.Instance.Intensity = 0.7f;
            if (SeaMotion.Instance != null) SeaMotion.Instance.Roughness = 1f;

            SetUpRumble();

            if (body != null) body.SetActive(true);
            if (head != null && lurkPoint != null)
            {
                head.gameObject.SetActive(true);
                head.position = lurkPoint.position + RestOffset;
            }
            if (segmentTemplate != null && neckBase != null)
                _neck = MakeChain(neckBase.position, NeckSections, "Monster Neck", new Color(0.1f, 0.3f, 0.38f));
        }

        // The head starts below the water, beside and under where it hangs.
        static readonly Vector3 RestOffset = new Vector3(8f, -26f, 4f);

        void BuildPlan()
        {
            var rng = new System.Random(20260);
            _attacks.Clear();
            int bases = Mathf.Max(1, tentacleBases != null ? tentacleBases.Length : 1);

            // Tentacle slaps every 1.3 s: half aimed at a player, half at a random spot near the platform.
            int index = 0;
            for (float t = 1.6f; t < 17f; t += 1.3f, index++)
            {
                var a = new Attack { time = t, telegraph = 1.2f, radius = 2.6f, grab = false, baseIndex = index % bases };
                if (index % 2 == 0)
                {
                    a.targeted = true;
                    a.order = index / 2;
                }
                else
                {
                    a.spot = new Vector3(
                        platformCenter.x + ((float)rng.NextDouble() * 2f - 1f) * (platformSize.x * 0.5f + 3f), 0f,
                        platformCenter.z + ((float)rng.NextDouble() * 2f - 1f) * (platformSize.z * 0.5f + 3f));
                }
                _attacks.Add(a);
            }

            // The head's lunges: the jaws snap at these moments. One target each, in turn.
            float[] lungeTimes = { 6.5f, 9.5f, 12.5f, 15.3f };
            for (int i = 0; i < lungeTimes.Length; i++)
                _attacks.Add(new Attack { time = lungeTimes[i], radius = 4.5f, grab = true, targeted = true, order = i });

            // The last lunge takes everyone left, at exactly `duration`.
            _attacks.Add(new Attack { time = duration, radius = 60f, grab = true, final = true });
        }

        // ------------------------------------------------------------------ attacks

        void RefreshAlive()
        {
            _alive.Clear();
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
                if (!players[i].IsEaten) _alive.Add(players[i]);
            _alive.Sort((a, b) => a.NetworkId.CompareTo(b.NetworkId));
        }

        PlayerController PickTarget(int order)
        {
            if (_alive.Count == 0) return null;
            return _alive[order % _alive.Count];
        }

        float GroundYAt(Vector3 pos)
        {
            bool onPlatform = Mathf.Abs(pos.x - platformCenter.x) <= platformSize.x * 0.5f
                           && Mathf.Abs(pos.z - platformCenter.z) <= platformSize.z * 0.5f;
            return onPlatform ? platformTopY : 0.02f;
        }

        void UpdateAttacks(float e)
        {
            foreach (var a in _attacks)
            {
                if (a.finished) continue;

                if (a.grab) { UpdateLunge(a, e); continue; }
                if (e < a.time) continue;

                if (a.resolved)
                {
                    AnimateSlapAfter(a, e - a.resolvedAt);
                    continue;
                }

                float local = e - a.time;

                if (a.disc == null && telegraphTemplate != null)
                {
                    a.disc = Instantiate(telegraphTemplate);
                    a.disc.SetActive(true);
                    a.disc.name = "Telegraph";
                    if (!a.targeted) a.center = a.spot;
                }

                if (a.tentacle == null && segmentTemplate != null && tentacleBases != null && tentacleBases.Length > 0)
                    a.tentacle = MakeChain(tentacleBases[a.baseIndex % tentacleBases.Length].position, TentacleSections, "Slapping Tentacle", new Color(0.32f, 0.12f, 0.52f));

                // A targeted circle follows its player, then locks so there is time to run.
                if (a.targeted && !a.locked)
                {
                    var target = PickTarget(a.order);
                    if (target != null) a.center = target.transform.position;
                    if (local >= a.telegraph - 0.4f) a.locked = true;
                }

                a.groundY = GroundYAt(a.center);

                if (a.disc != null)
                {
                    float grow = Mathf.Lerp(0.35f, 1f, local / a.telegraph);
                    a.disc.transform.position = new Vector3(a.center.x, a.groundY + 0.04f, a.center.z);
                    a.disc.transform.localScale = new Vector3(a.radius * 2f * grow, 0.02f, a.radius * 2f * grow);

                    float pulse = 0.5f + 0.5f * Mathf.Sin(local * 14f);
                    Color c = new Color(1f, 0.55f, 0.1f, 0.3f + 0.25f * pulse);
                    var r = a.disc.GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        _block.SetColor("_BaseColor", c);
                        _block.SetColor("_Color", c);
                        r.SetPropertyBlock(_block);
                    }
                }

                // The tentacle rises out of the sea and looms over the circle.
                if (a.tentacle != null)
                {
                    float rise = Mathf.SmoothStep(0f, 1f, local / 0.7f);
                    float height = Mathf.Lerp(16f, 10f, local / a.telegraph);
                    float sway = Mathf.Sin(local * 5f) * 0.8f;
                    a.hover = a.center + new Vector3(sway, height, 0f);
                    Vector3 tip = Vector3.Lerp(a.tentacle.anchor + Vector3.up * 1f, a.hover, rise);
                    PoseChain(a.tentacle, tip, Mathf.Lerp(3f, 14f, rise), 2.9f, 1.1f);
                }

                if (local >= a.telegraph) ResolveSlap(a, e);
            }
        }

        void ResolveSlap(Attack a, float e)
        {
            a.resolved = true;
            a.resolvedAt = e;
            if (a.disc != null) { Destroy(a.disc); a.disc = null; }

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue;
                if (!InCircle(p.transform.position, a.center, a.radius) || p.FeetY() > a.groundY + 3f) continue;

                Vector3 away = p.transform.position - a.center;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                p.Knock(away.normalized * 12f, 9f); // slapped into the sea with a splash
            }

            var cam = IsoCameraRig.Instance;
            if (cam != null) cam.Shake(0.3f, 0.3f);
        }

        static bool InCircle(Vector3 pos, Vector3 center, float radius)
        {
            float dx = pos.x - center.x, dz = pos.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }

        // After the slap: the tentacle stays down for a moment, then slides back into the sea.
        void AnimateSlapAfter(Attack a, float age)
        {
            if (a.tentacle == null) { a.finished = true; return; }

            Vector3 ground = new Vector3(a.center.x, a.groundY + 0.5f, a.center.z);
            Vector3 home = a.tentacle.anchor + Vector3.up * 1f;

            Vector3 tip;
            float arch;
            if (age < 0.12f)
            {
                float k = age / 0.12f;
                tip = Vector3.Lerp(a.hover, ground, k * k); // the slap
                arch = Mathf.Lerp(14f, 2f, k);
            }
            else if (age < 0.9f)
            {
                tip = ground;
                arch = 2f;
            }
            else
            {
                float k = Mathf.Clamp01((age - 0.9f) / 0.8f);
                tip = Vector3.Lerp(ground, home, k);
                arch = Mathf.Lerp(2f, 6f, k);
            }

            PoseChain(a.tentacle, tip, arch, 2.9f, 1.1f);

            if (age > 1.8f)
            {
                Destroy(a.tentacle.root);
                a.tentacle = null;
                a.finished = true;
            }
        }

        // The jaws snap shut at a.time. Anyone inside is eaten. The head's movement is in UpdateMonster.
        void UpdateLunge(Attack a, float e)
        {
            if (!a.roared && e >= a.time - 1.1f)
            {
                a.roared = true;
                if (_effects != null && _roarClip != null) _effects.PlayOneShot(_roarClip, a.final ? 1f : 0.8f);
            }

            if (!a.resolved && e >= a.time)
            {
                a.resolved = true;
                if (_effects != null && _chompClip != null) _effects.PlayOneShot(_chompClip, 1f);

                var players = PlayerController.All;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten) continue;

                    bool inJaws = a.final || (InCircle(p.transform.position, a.lockedPoint, a.radius) && p.FeetY() < a.lockedPoint.y + 4f);
                    if (inJaws) p.Eat(); // swallowed
                }

                var cam = IsoCameraRig.Instance;
                if (cam != null) cam.Shake(a.final ? 1.2f : 0.6f, a.final ? 1.6f : 0.5f);
            }

            if (e > a.time + 1.5f) a.finished = true;
        }

        // ------------------------------------------------------------------ the monster itself

        void UpdateMonster(float e)
        {
            if (head == null || lurkPoint == null) return;

            // Heaves up out of the water in about 2.5 seconds, starting right away.
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 2.5f));
            Vector3 sway = new Vector3(Mathf.Sin(e * 0.9f) * 0.8f, Mathf.Sin(e * 1.3f) * 0.6f, Mathf.Cos(e * 0.7f) * 0.8f);
            Vector3 basePos = lurkPoint.position + RestOffset * (1f - rise) + sway;

            // By default the head hangs there and looks at the nearest player, mouth a little open.
            Vector3 lookAt = NearestPlayerPoint(basePos);
            Vector3 pos = basePos;
            float jaw = 8f + Mathf.Sin(e * 2.2f) * 3f;

            // A lunge in progress takes over the head.
            Attack lunge = null;
            foreach (var a in _attacks)
            {
                if (!a.grab) continue;
                if (e >= a.time - 1.2f && e < a.time + 1.4f) { lunge = a; break; }
            }

            if (lunge != null) PoseLunge(lunge, e, basePos, ref pos, ref lookAt, ref jaw);

            // Turn the head to face where it is looking.
            Vector3 dir = lookAt - pos;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
                float turn = lunge != null ? 1f - Mathf.Exp(-18f * Time.deltaTime) : 1f - Mathf.Exp(-5f * Time.deltaTime);
                _headRotation = Quaternion.Slerp(_headRotation, want, turn);
            }

            head.SetPositionAndRotation(pos, _headRotation);
            if (jawPivot != null) jawPivot.localRotation = Quaternion.Euler(jaw, 0f, 0f);

            // The body rises with the head, and the neck joins them.
            if (body != null) body.transform.position = _bodyStart + Vector3.up * (30f * rise);
            if (_neck != null)
                PoseChain(_neck, pos - _headRotation * Vector3.forward * 3.4f, 16f, 8f, 6.5f);

            // The eyes follow what the head is looking at.
            if (eyes != null && pupils != null)
            {
                int count = Mathf.Min(eyes.Length, pupils.Length);
                for (int i = 0; i < count; i++)
                {
                    if (eyes[i] == null || pupils[i] == null) continue;
                    Vector3 toward = (lookAt - eyes[i].position).normalized;
                    pupils[i].position = eyes[i].position + toward * (eyeRadius * 0.62f);
                }
            }
        }

        Vector3 NearestPlayerPoint(Vector3 from)
        {
            Vector3 best = platformCenter + Vector3.up;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                float d = (_alive[i].transform.position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = _alive[i].transform.position + Vector3.up; }
            }
            return best;
        }

        // Rear back and stare at the target, then suddenly leap, snap, and slide back.
        void PoseLunge(Attack a, float e, Vector3 basePos, ref Vector3 pos, ref Vector3 lookAt, ref float jaw)
        {
            // Who is it after? Chosen when the monster starts to rear back.
            if (a.targetPlayer == null && !a.final && !a.locked) a.targetPlayer = PickTarget(a.order);

            Vector3 aim = a.final
                ? platformCenter + Vector3.up * 1.2f
                : (a.targetPlayer != null ? a.targetPlayer.transform.position + Vector3.up * 0.5f : platformCenter);

            // The aim follows the player until a moment before the leap, then stops: that is the chance to dodge.
            if (!a.locked && e >= a.time - 0.5f)
            {
                a.locked = true;
                a.lockedPoint = aim;
            }
            Vector3 target = a.locked ? a.lockedPoint : aim;

            Vector3 flatToHead = new Vector3(basePos.x - target.x, 0f, basePos.z - target.z);
            if (flatToHead.sqrMagnitude < 0.01f) flatToHead = Vector3.right;
            flatToHead.Normalize();

            float reach = a.final ? 0f : jawReach;
            a.strikePos = target + flatToHead * reach + Vector3.up * (a.final ? 4f : 1.2f);
            Vector3 rearPos = basePos + Vector3.up * 3f + flatToHead * 3f;

            lookAt = target;
            float tStart = a.time - 1.2f, tLunge = a.time - 0.3f;

            if (e < tLunge)
            {
                // Rearing back: it lifts, pulls away and opens its jaws, eyes fixed on the target.
                float p = Mathf.SmoothStep(0f, 1f, (e - tStart) / (tLunge - tStart));
                pos = Vector3.Lerp(basePos, rearPos, p);
                jaw = Mathf.Lerp(8f, 26f, p);
            }
            else if (e < a.time)
            {
                // The leap: fast and sudden.
                float p = (e - tLunge) / 0.3f;
                p *= p;
                pos = Vector3.Lerp(rearPos, a.strikePos, p);
                jaw = Mathf.Lerp(26f, 60f, p);
            }
            else if (e < a.time + 0.3f)
            {
                // The snap.
                pos = a.strikePos;
                jaw = Mathf.Lerp(60f, 0f, Mathf.Clamp01((e - a.time) / 0.1f));
            }
            else
            {
                // Slides back to its watching place.
                float p = Mathf.SmoothStep(0f, 1f, (e - a.time - 0.3f) / 1.1f);
                pos = Vector3.Lerp(a.strikePos, basePos, p);
                jaw = Mathf.Lerp(0f, 8f, p);
                lookAt = Vector3.Lerp(target, NearestPlayerPoint(basePos), p);
            }
        }

        // ------------------------------------------------------------------ chains (tentacles and the neck)

        Chain MakeChain(Vector3 anchor, int sections, string name, Color color)
        {
            var chain = new Chain { anchor = anchor, root = new GameObject(name), parts = new Transform[sections] };

            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);

            for (int i = 0; i < sections; i++)
            {
                var part = Instantiate(segmentTemplate, chain.root.transform);
                part.SetActive(true);
                part.name = "Section";
                var r = part.GetComponentInChildren<Renderer>();
                if (r != null) r.SetPropertyBlock(block);
                chain.parts[i] = part.transform;
            }

            PoseChain(chain, anchor + Vector3.up, 3f, 2.9f, 1.1f);
            return chain;
        }

        // Lays the sections along a smooth curve from the sea to the tip. Thick at the root, thinner at the tip.
        static void PoseChain(Chain chain, Vector3 tip, float arch, float rootSize, float tipSize)
        {
            Vector3 b = chain.anchor;
            Vector3 control = (b + tip) * 0.5f + Vector3.up * arch;
            int n = chain.parts.Length;
            for (int i = 0; i < n; i++)
            {
                float s = i / (n - 1f);
                float u = 1f - s;
                Vector3 p = u * u * b + 2f * u * s * control + s * s * tip;
                chain.parts[i].position = p;
                chain.parts[i].localScale = Vector3.one * Mathf.Lerp(rootSize, tipSize, s);
            }
        }

        // ------------------------------------------------------------------ the mood

        void UpdateLook(float e)
        {
            // Sudden: the change takes half a second.
            float k = Mathf.Clamp01(e / 0.5f);

            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(_sunIntensity0 * 0.7f, _sunIntensity0 * 0.2f, k);
                sun.color = Color.Lerp(new Color(1f, 0.5f, 0.45f), new Color(0.45f, 0.5f, 0.9f), k);
            }

            Color dark = new Color(0.12f, 0.16f, 0.3f);
            RenderSettings.ambientSkyColor = Color.Lerp(_sky0, dark, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(_equator0, dark * 0.8f, k);
            RenderSettings.ambientGroundColor = Color.Lerp(_ground0, dark * 0.6f, k);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.Lerp(_fogColor0, new Color(0.04f, 0.08f, 0.16f), k);
            RenderSettings.fogDensity = Mathf.Lerp(_fogDensity0, 0.008f, k);

            if (_seaMaterial != null && _seaMaterial.HasProperty("_BaseColor"))
                _seaMaterial.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(0.12f, 0.2f, 0.4f), k));

            if (_rumble != null) _rumble.volume = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(e / 1.5f));

            // The text shown while watching.
            StatusText = "";
            foreach (var player in PlayerController.All)
                if (player.IsLocal && player.IsEaten) StatusText = "Swallowed! Watching your friends...";
        }

        // ------------------------------------------------------------------ sounds (simple generated placeholders)

        void SetUpRumble()
        {
            const int rate = 44100;
            var data = new float[rate * 2];
            var rng = new System.Random(1);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                // Whole numbers of cycles over the 2 s clip, so it loops without a click.
                data[i] = Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 57f * t) * 0.3f
                        + ((float)rng.NextDouble() - 0.5f) * 0.12f;
            }

            var clip = AudioClip.Create("MonsterRumble", data.Length, 1, rate, false);
            clip.SetData(data, 0);

            _rumble = gameObject.AddComponent<AudioSource>();
            _rumble.clip = clip;
            _rumble.loop = true;
            _rumble.spatialBlend = 0f;
            _rumble.volume = 0f;
            _rumble.Play();
        }

        // A two-tone alarm: 0.5 s per tone, loops cleanly.
        static AudioClip MakeSiren()
        {
            const int rate = 44100;
            var data = new float[rate * 2];
            float[] tones = { 440f, 620f, 440f, 620f };
            for (int i = 0; i < data.Length; i++)
            {
                int segment = Mathf.Min(3, i / (rate / 2));
                float t = i / (float)rate;
                data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * tones[segment] * t)) * 0.18f; // a harsh square wave
            }
            var clip = AudioClip.Create("Siren", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // A growl: noise and a falling tone, 0.9 s.
        static AudioClip MakeRoar()
        {
            const int rate = 44100;
            var data = new float[(int)(rate * 0.9f)];
            var rng = new System.Random(7);
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float u = i / (float)data.Length;
                float freq = Mathf.Lerp(160f, 55f, u);
                phase += 2f * Mathf.PI * freq / rate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.1f));
                data[i] = (Mathf.Sin(phase) * 0.6f + ((float)rng.NextDouble() - 0.5f) * 0.5f) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("Roar", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // A heavy snap: a short burst of noise that dies away.
        static AudioClip MakeChomp()
        {
            const int rate = 44100;
            var data = new float[(int)(rate * 0.35f)];
            var rng = new System.Random(11);
            for (int i = 0; i < data.Length; i++)
            {
                float u = i / (float)data.Length;
                data[i] = ((float)rng.NextDouble() - 0.5f) * 1.4f * Mathf.Pow(1f - u, 3f);
            }
            var clip = AudioClip.Create("Chomp", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------------------------------------------------------ the end

        void UpdateEnding(float e)
        {
            if (_endAt >= 0f) return;

            bool everyoneEaten = PlayerController.All.Count > 0;
            foreach (var p in PlayerController.All)
                if (!p.IsEaten) everyoneEaten = false;

            if (e >= duration + 0.2f || everyoneEaten) _endAt = e;
        }

        void OnGUI()
        {
            if (!_started || _endAt < 0f) return;

            float e = (float)(GameClock.Now - _strikeTime);
            float fade = Mathf.Clamp01((e - (_endAt + 1.2f)) / 1.2f);
            if (fade <= 0f) return;

            GUI.depth = -100;
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float textFade = Mathf.Clamp01((e - (_endAt + 3f)) / 1.2f);
            if (textFade > 0f)
            {
                var style = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = new Color(1f, 1f, 1f, textFade);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "To be continued...", style);
            }
        }
    }
}
