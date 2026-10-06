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
    /// The end of Chapter 1. Once every player has finished the laps and stands on the big start/finish platform for
    /// a few seconds, the party game suddenly turns: the light drops, the sea darkens and a giant sea monster heaves
    /// out of the water beside the platform, its eyes following the players. Players stay fully in control and can
    /// run, jump and swim away, but within <see cref="duration"/> seconds everyone is swallowed, one by one.
    /// Then the screen fades to black.
    ///
    /// Attacks (all with a red/orange circle that shows where, and a warning before they land):
    ///  - Slaps: a tentacle reaches in from the sea, looms over the circle, then slaps down. Knocks you flying.
    ///  - Grabs: a thicker tentacle does the same, but anyone caught inside is grabbed and eaten.
    ///    The circle follows one player, then locks; run out of it in time and you live on.
    ///  - The final lunge: the monster's head lunges over the platform. Nobody escapes it.
    ///
    /// Everything follows the shared clock, so all players see the same attacks at the same moment. Each machine
    /// only decides whether its OWN player is hit.
    /// </summary>
    public class MonsterEncounter : MonoBehaviour
    {
        public static MonsterEncounter Instance { get; private set; }

        /// <summary>True once the monster has struck.</summary>
        public static bool Started => Instance != null && Instance._started;

        /// <summary>Set by the networking layer: true only on the host. Null offline (then this machine decides).</summary>
        public static Func<bool> IsAuthority;
        /// <summary>Set on the host by the networking layer. Sends the start time to every player.</summary>
        public static Action<double> BroadcastStart;

        /// <summary>A message for the on-screen display.</summary>
        public static string StatusText { get; private set; } = "";

        [Header("When it starts")]
        public LapTracker tracker;
        [Tooltip("World centre and size of the big platform area where the fight happens (the size's y is ignored).")]
        public Vector3 platformCenter;
        public Vector3 platformSize = new Vector3(34f, 1f, 24f);
        public float platformTopY = 0.6f;
        [Tooltip("Everyone on the platform for this long, and the monster strikes.")]
        [Min(0f)] public float celebrationSeconds = 5f;
        [Tooltip("If someone never gets there, it strikes anyway this long after the laps are done.")]
        [Min(5f)] public float fallbackSeconds = 40f;

        [Header("The fight")]
        [Tooltip("Everyone is swallowed at the latest this many seconds after the strike.")]
        [Min(10f)] public float duration = 20f;

        [Header("Look (set up by the scene builder)")]
        public Light sun;
        public Renderer seaRenderer;
        public GameObject telegraphTemplate;
        [Tooltip("One tentacle section (a sphere). Tentacles are chains of these.")]
        public GameObject segmentTemplate;
        public GameObject mawTemplate;
        public GameObject head;
        [Tooltip("The monster's eyes and pupils (same order). The pupils follow the players.")]
        public Transform[] eyes;
        public Transform[] pupils;
        public float eyeRadius = 2.65f;
        [Tooltip("Where the tentacles come out of the sea.")]
        public Transform[] tentacleBases;
        [Tooltip("How far the head rises out of the sea.")]
        public float headRiseMeters = 34f;
        [Tooltip("During the final lunge the head moves this far towards the platform (horizontally).")]
        public float headLungeMeters = 22f;

        const int SegmentsPerTentacle = 36;

        class Tentacle
        {
            public GameObject root;
            public Transform[] parts;
            public Vector3 anchor;
            public float thickness = 1f;
        }

        class Attack
        {
            public float time;
            public float telegraph;
            public float radius;
            public bool grab;
            public bool final;
            public bool targeted;
            public int order;
            public int baseIndex;
            public Vector3 spot;

            public GameObject disc;
            public Tentacle tentacle;
            public GameObject effect;
            public Vector3 center;
            public Vector3 hover;
            public float groundY;
            public bool locked;
            public bool resolved;
            public float resolvedAt;
            public bool finished;
        }

        readonly List<Attack> _attacks = new List<Attack>();
        readonly List<PlayerController> _alive = new List<PlayerController>();

        bool _started;
        bool _triggerSent;
        double _startTime;
        float _finishedSeenAt = -1f;
        float _onPlatformFor;
        float _endAt = -1f;
        MaterialPropertyBlock _block;

        Vector3 _headStart;

        // Look before the strike, so we know what to fade from.
        Color _sunColor0, _sky0, _equator0, _ground0, _fogColor0;
        float _sunIntensity0, _fogDensity0;
        bool _fog0;
        Material _seaMaterial;
        AudioSource _rumble;

        void Awake()
        {
            Instance = this;
            _block = new MaterialPropertyBlock();
            foreach (var t in new[] { telegraphTemplate, segmentTemplate, mawTemplate })
                if (t != null) t.SetActive(false);
            if (head != null) _headStart = head.transform.position;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ before the strike

        void Update()
        {
            if (!_started)
            {
                WaitForStrike();
                return;
            }

            float e = (float)(GameClock.Now - _startTime);
            if (e < 0f) return;

            RefreshAlive();
            UpdateLook(e);
            UpdateHead(e);
            UpdateAttacks(e);
            UpdateEnding(e);
        }

        void WaitForStrike()
        {
            bool lapsDone = tracker != null && tracker.AllFinished;
            StatusText = lapsDone ? "Everyone is done! Stay on the big platform..." : "";

            bool authority = IsAuthority == null || IsAuthority();
            if (!authority || _triggerSent) return;

            // Testing shortcut: M summons the monster straight away.
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                Strike();
                return;
            }

            if (!lapsDone) return;
            if (_finishedSeenAt < 0f) _finishedSeenAt = Time.time;

            bool everyoneOnPlatform = true;
            int counted = 0;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsEaten) continue;
                counted++;
                if (!OnPlatform(players[i])) everyoneOnPlatform = false;
            }

            _onPlatformFor = everyoneOnPlatform && counted > 0 ? _onPlatformFor + Time.deltaTime : 0f;

            if (_onPlatformFor >= celebrationSeconds || Time.time - _finishedSeenAt >= fallbackSeconds)
                Strike();
        }

        bool OnPlatform(PlayerController p)
        {
            Vector3 pos = p.transform.position;
            return Mathf.Abs(pos.x - platformCenter.x) <= platformSize.x * 0.5f
                && Mathf.Abs(pos.z - platformCenter.z) <= platformSize.z * 0.5f
                && p.FeetY() > platformTopY - 0.5f;
        }

        void Strike()
        {
            _triggerSent = true;
            double start = GameClock.Now + 0.15; // a moment ahead, so every machine has the message before it starts
            if (BroadcastStart != null) BroadcastStart(start);
            else BeginAt(start);
        }

        /// <summary>Called on every machine with the same start time.</summary>
        public void BeginAt(double startTime)
        {
            if (_started) return;
            _started = true;
            _startTime = startTime;
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

            if (head != null)
            {
                head.SetActive(true);
                head.transform.position = _headStart;
            }

            SetUpRumble();
        }

        // ------------------------------------------------------------------ the plan

        void BuildPlan()
        {
            var rng = new System.Random(20260);
            _attacks.Clear();
            int bases = Mathf.Max(1, tentacleBases != null ? tentacleBases.Length : 1);

            // Slaps every 1.2 s: half aimed at a player, half at a random spot on or near the platform.
            int index = 0;
            for (float t = 1.8f; t < 16.5f; t += 1.2f, index++)
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

            // Grabs, one target each, faster and wider every time.
            float[] grabTimes = { 6.0f, 9.0f, 12.0f, 14.8f };
            float[] grabTelegraph = { 2.3f, 2.1f, 1.9f, 1.7f };
            float[] grabRadius = { 3.0f, 3.3f, 3.6f, 4.0f };
            for (int i = 0; i < grabTimes.Length; i++)
                _attacks.Add(new Attack { time = grabTimes[i], telegraph = grabTelegraph[i], radius = grabRadius[i], grab = true, targeted = true, order = i, baseIndex = (i + 1) % bases });

            // The final lunge: covers everything and ends at exactly `duration`.
            _attacks.Add(new Attack
            {
                time = duration - 3.5f, telegraph = 3.5f, radius = 20f, grab = true, final = true,
                center = platformCenter, spot = platformCenter,
            });
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
                if (a.finished || e < a.time) continue;

                if (a.resolved)
                {
                    AnimateAfterHit(a, e - a.resolvedAt);
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

                if (a.tentacle == null && !a.final && segmentTemplate != null && tentacleBases != null && tentacleBases.Length > 0)
                {
                    a.tentacle = MakeTentacle(tentacleBases[a.baseIndex % tentacleBases.Length].position, a.grab);
                }

                // A targeted circle follows its player, then locks so there is time to run.
                float lockTime = a.grab ? 0.6f : 0.4f;
                if (a.targeted && !a.locked)
                {
                    var target = PickTarget(a.order);
                    if (target != null) a.center = target.transform.position;
                    if (local >= a.telegraph - lockTime) a.locked = true;
                }

                a.groundY = GroundYAt(a.center);

                if (a.disc != null)
                {
                    float grow = a.final ? 1f : Mathf.Lerp(0.35f, 1f, local / a.telegraph);
                    a.disc.transform.position = new Vector3(a.center.x, a.groundY + 0.04f, a.center.z);
                    a.disc.transform.localScale = new Vector3(a.radius * 2f * grow, 0.02f, a.radius * 2f * grow);

                    float pulse = 0.5f + 0.5f * Mathf.Sin(local * 14f);
                    Color c = a.grab ? new Color(1f, 0.1f, 0.1f, 0.35f + 0.3f * pulse) : new Color(1f, 0.55f, 0.1f, 0.3f + 0.25f * pulse);
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
                    PoseTentacle(a.tentacle, tip, Mathf.Lerp(3f, 14f, rise));
                }

                if (local >= a.telegraph) Resolve(a, e);
            }
        }

        void Resolve(Attack a, float e)
        {
            a.resolved = true;
            a.resolvedAt = e;
            if (a.disc != null) { Destroy(a.disc); a.disc = null; }

            var cam = IsoCameraRig.Instance;

            if (a.final)
            {
                // The head lunges over the platform and its mouth opens.
                if (mawTemplate != null)
                {
                    a.effect = Instantiate(mawTemplate);
                    a.effect.SetActive(true);
                    a.effect.name = "Monster Mouth";
                    a.effect.transform.localScale = new Vector3(a.radius * 2f, 2f, a.radius * 2f);
                }

                foreach (var p in PlayerController.All)
                    if (p.IsLocal && !p.IsEaten) p.Eat();
                if (cam != null) cam.Shake(1.2f, 1.6f);
                return;
            }

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue;
                if (!InCircle(p.transform.position, a.center, a.radius) || p.FeetY() > a.groundY + 3.5f) continue;

                if (a.grab)
                {
                    p.Eat(); // grabbed and dragged under
                }
                else
                {
                    Vector3 away = p.transform.position - a.center;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                    p.Knock(away.normalized * 12f, 9f); // slapped into the sea with a splash
                }
            }

            if (cam != null) cam.Shake(a.grab ? 0.5f : 0.3f, a.grab ? 0.45f : 0.3f);
        }

        static bool InCircle(Vector3 pos, Vector3 center, float radius)
        {
            float dx = pos.x - center.x, dz = pos.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }

        // After the hit: the tentacle stays down for a moment, then slides back into the sea.
        void AnimateAfterHit(Attack a, float age)
        {
            if (a.final)
            {
                AnimateMouth(a, age);
                return;
            }

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

            PoseTentacle(a.tentacle, tip, arch);

            if (age > 1.8f)
            {
                Destroy(a.tentacle.root);
                a.tentacle = null;
                a.finished = true;
            }
        }

        void AnimateMouth(Attack a, float age)
        {
            if (a.effect == null) { a.finished = true; return; }

            float up = age < 0.3f ? 1f - Mathf.Pow(1f - age / 0.3f, 2f) : 1f;
            a.effect.transform.position = new Vector3(a.center.x, a.groundY - 2f + up * 3.2f, a.center.z);
            // The final mouth stays: the screen fades to black over it.
        }

        // ------------------------------------------------------------------ tentacles

        Tentacle MakeTentacle(Vector3 anchor, bool grab)
        {
            var t = new Tentacle
            {
                anchor = anchor,
                thickness = grab ? 1.4f : 1f,
                root = new GameObject(grab ? "Grabbing Tentacle" : "Slapping Tentacle"),
                parts = new Transform[SegmentsPerTentacle],
            };

            Color color = grab ? new Color(0.62f, 0.1f, 0.3f) : new Color(0.32f, 0.12f, 0.52f);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);

            for (int i = 0; i < SegmentsPerTentacle; i++)
            {
                var part = Instantiate(segmentTemplate, t.root.transform);
                part.SetActive(true);
                part.name = "Section";
                var r = part.GetComponentInChildren<Renderer>();
                if (r != null) r.SetPropertyBlock(block);
                t.parts[i] = part.transform;
            }

            PoseTentacle(t, anchor + Vector3.up, 3f);
            return t;
        }

        // Lays the sections along a smooth curve from the sea to the tip. Thick at the root, thin at the tip.
        static void PoseTentacle(Tentacle t, Vector3 tip, float arch)
        {
            Vector3 b = t.anchor;
            Vector3 control = (b + tip) * 0.5f + Vector3.up * arch;
            int n = t.parts.Length;
            for (int i = 0; i < n; i++)
            {
                float s = i / (n - 1f);
                float u = 1f - s;
                Vector3 p = u * u * b + 2f * u * s * control + s * s * tip;
                t.parts[i].position = p;
                t.parts[i].localScale = Vector3.one * (Mathf.Lerp(2.9f, 1.1f, s) * t.thickness);
            }
        }

        // ------------------------------------------------------------------ the monster itself

        void UpdateHead(float e)
        {
            if (head == null) return;

            // Heaves up out of the water in about 2.5 seconds, starting right away.
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 2.5f));
            Vector3 pos = _headStart + Vector3.up * (headRiseMeters * rise);

            // The final lunge towards the platform.
            float lungeStart = duration - 3.5f;
            if (e > lungeStart)
            {
                float k = Mathf.SmoothStep(0f, 1f, (e - lungeStart) / 3.5f);
                Vector3 toward = platformCenter - new Vector3(_headStart.x, platformCenter.y, _headStart.z);
                toward.y = 0f;
                pos += toward.normalized * (headLungeMeters * k);
            }

            // A slow, menacing sway.
            pos += new Vector3(0f, Mathf.Sin(e * 1.3f) * 0.6f, 0f);
            head.transform.position = pos;

            // The pupils follow the nearest player: the monster is looking at them.
            if (eyes == null || pupils == null) return;
            Vector3 lookAt = platformCenter;
            float best = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                float d = (_alive[i].transform.position - head.transform.position).sqrMagnitude;
                if (d < best) { best = d; lookAt = _alive[i].transform.position + Vector3.up; }
            }

            int count = Mathf.Min(eyes.Length, pupils.Length);
            for (int i = 0; i < count; i++)
            {
                if (eyes[i] == null || pupils[i] == null) continue;
                Vector3 dir = (lookAt - eyes[i].position).normalized;
                pupils[i].position = eyes[i].position + dir * (eyeRadius * 0.62f);
            }
        }

        // ------------------------------------------------------------------ the mood

        void UpdateLook(float e)
        {
            // Sudden: the change takes half a second.
            float k = Mathf.Clamp01(e / 0.5f);

            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(_sunIntensity0, _sunIntensity0 * 0.2f, k);
                sun.color = Color.Lerp(_sunColor0, new Color(0.45f, 0.5f, 0.9f), k);
            }

            Color dark = new Color(0.12f, 0.16f, 0.3f);
            RenderSettings.ambientSkyColor = Color.Lerp(_sky0, dark, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(_equator0, dark * 0.8f, k);
            RenderSettings.ambientGroundColor = Color.Lerp(_ground0, dark * 0.6f, k);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.Lerp(_fogColor0, new Color(0.04f, 0.08f, 0.16f), k);
            RenderSettings.fogDensity = Mathf.Lerp(_fogDensity0, 0.008f, k);

            if (_seaMaterial != null)
            {
                SetColor(_seaMaterial, "_ShallowColor", Color.Lerp(new Color(0.12f, 0.66f, 1f, 1f), new Color(0.03f, 0.12f, 0.25f, 1f), k));
                SetColor(_seaMaterial, "_DeepColor", Color.Lerp(new Color(0.03f, 0.34f, 0.9f, 1f), new Color(0.01f, 0.04f, 0.12f, 1f), k));
                SetColor(_seaMaterial, "_BaseColor", Color.Lerp(new Color(0.1f, 0.55f, 1f, 1f), new Color(0.02f, 0.08f, 0.2f, 1f), k));
            }

            if (_rumble != null) _rumble.volume = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(e / 1.5f));

            // The text shown while watching.
            StatusText = "";
            foreach (var player in PlayerController.All)
                if (player.IsLocal && player.IsEaten) StatusText = "Swallowed! Watching your friends...";
        }

        static void SetColor(Material m, string property, Color c)
        {
            if (m.HasProperty(property)) m.SetColor(property, c);
        }

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

        // ------------------------------------------------------------------ the end

        void UpdateEnding(float e)
        {
            if (_endAt >= 0f) return;

            bool everyoneEaten = PlayerController.All.Count > 0;
            foreach (var p in PlayerController.All)
                if (!p.IsEaten) everyoneEaten = false;

            if (e >= duration || everyoneEaten) _endAt = e;
        }

        void OnGUI()
        {
            if (!_started || _endAt < 0f) return;

            float e = (float)(GameClock.Now - _startTime);
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
