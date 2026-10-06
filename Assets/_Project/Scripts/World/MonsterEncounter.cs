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
    /// The end of Chapter 1. Once every player has finished the laps and stands on the big platform for a few
    /// seconds, the party game suddenly turns: the light drops, the sea darkens and a sea monster attacks. Players
    /// stay fully in control and can run, jump and swim away, but within <see cref="duration"/> seconds everyone is
    /// swallowed, one by one. Then the screen fades to black.
    ///
    /// Attacks:
    ///  - Tentacle slams: a red circle appears, then a tentacle crashes down. Anyone inside is knocked flying (funny).
    ///  - Swallows: a red circle follows one player, locks, then the monster's mouth rises. Anyone inside is eaten.
    ///    Run out of the circle in time and you live on.
    ///  - The final swallow covers everything and cannot be escaped.
    ///
    /// Everything follows the shared clock, so all players see the same attacks at the same moment. Each machine
    /// only decides whether its OWN player is hit.
    /// </summary>
    public class MonsterEncounter : MonoBehaviour
    {
        public static MonsterEncounter Instance { get; private set; }

        /// <summary>Set by the networking layer: true only on the host. Null offline (then this machine decides).</summary>
        public static Func<bool> IsAuthority;
        /// <summary>Set on the host by the networking layer. Sends the start time to every player.</summary>
        public static Action<double> BroadcastStart;

        /// <summary>A message for the on-screen display.</summary>
        public static string StatusText { get; private set; } = "";

        [Header("When it starts")]
        public LapTracker tracker;
        [Tooltip("World centre and size of the big platform (the size's y is ignored).")]
        public Vector3 platformCenter;
        public Vector3 platformSize = new Vector3(14f, 1f, 11f);
        public float platformTopY = 0.6f;
        [Tooltip("Everyone on the platform for this long, and the monster strikes.")]
        [Min(0f)] public float celebrationSeconds = 3f;
        [Tooltip("If someone never gets there, it strikes anyway this long after the laps are done.")]
        [Min(5f)] public float fallbackSeconds = 40f;

        [Header("The fight")]
        [Tooltip("Everyone is swallowed at the latest this many seconds after the strike.")]
        [Min(10f)] public float duration = 20f;

        [Header("Look (set up by the scene builder)")]
        public Light sun;
        public Renderer seaRenderer;
        public GameObject telegraphTemplate;
        public GameObject tentacleTemplate;
        public GameObject mawTemplate;
        public GameObject head;

        class Attack
        {
            public float time;
            public float telegraph;
            public float radius;
            public bool grab;
            public bool final;
            public bool targeted;
            public int order;
            public Vector3 spot;

            public GameObject disc;
            public GameObject effect;
            public Vector3 center;
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
            foreach (var t in new[] { telegraphTemplate, tentacleTemplate, mawTemplate, head })
                if (t != null) t.SetActive(false);
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
            UpdateAttacks(e);
            UpdateEnding(e);
        }

        void WaitForStrike()
        {
            bool lapsDone = tracker != null && tracker.AllFinished;
            StatusText = lapsDone ? "All laps done! Cross the new bridge to the big platform." : "";

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

            SetUpRumble();
        }

        // ------------------------------------------------------------------ the plan

        void BuildPlan()
        {
            var rng = new System.Random(20260);
            _attacks.Clear();

            // Tentacle slams every 1.2 s: half aimed at a player, half at a random spot near the platform.
            int index = 0;
            for (float t = 1.5f; t < 16.5f; t += 1.2f, index++)
            {
                var a = new Attack { time = t, telegraph = 1.1f, radius = 2.4f, grab = false };
                if (index % 2 == 0)
                {
                    a.targeted = true;
                    a.order = index / 2;
                }
                else
                {
                    a.spot = new Vector3(
                        platformCenter.x + ((float)rng.NextDouble() * 2f - 1f) * (platformSize.x * 0.5f + 4f), 0f,
                        platformCenter.z + ((float)rng.NextDouble() * 2f - 1f) * (platformSize.z * 0.5f + 4f));
                }
                _attacks.Add(a);
            }

            // Swallows, one target each, faster and wider every time.
            float[] grabTimes = { 5.5f, 8.5f, 11.5f, 14.5f };
            float[] grabTelegraph = { 2.2f, 2.0f, 1.8f, 1.6f };
            float[] grabRadius = { 3.0f, 3.3f, 3.6f, 4.0f };
            for (int i = 0; i < grabTimes.Length; i++)
                _attacks.Add(new Attack { time = grabTimes[i], telegraph = grabTelegraph[i], radius = grabRadius[i], grab = true, targeted = true, order = i });

            // The final swallow: covers everything and ends at exactly `duration`.
            _attacks.Add(new Attack
            {
                time = duration - 3.5f, telegraph = 3.5f, radius = 18f, grab = true, final = true,
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
                    AnimateEffect(a, e - a.resolvedAt);
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
                foreach (var p in PlayerController.All)
                    if (p.IsLocal && !p.IsEaten) p.Eat();
                if (cam != null) cam.Shake(1.2f, 1.6f);
                a.finished = true;
                return;
            }

            var players = PlayerController.All;
            if (a.grab)
            {
                if (mawTemplate != null)
                {
                    a.effect = Instantiate(mawTemplate);
                    a.effect.SetActive(true);
                    a.effect.name = "Monster Mouth";
                    a.effect.transform.localScale = new Vector3(a.radius * 2f, 2f, a.radius * 2f);
                }

                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten) continue;
                    if (InCircle(p.transform.position, a.center, a.radius) && p.FeetY() < a.groundY + 3.5f)
                        p.Eat(); // swallowed
                }

                if (cam != null) cam.Shake(0.5f, 0.45f);
            }
            else
            {
                if (tentacleTemplate != null)
                {
                    a.effect = Instantiate(tentacleTemplate);
                    a.effect.SetActive(true);
                    a.effect.name = "Tentacle";
                }

                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten) continue;
                    if (!InCircle(p.transform.position, a.center, a.radius) || p.FeetY() > a.groundY + 3f) continue;

                    Vector3 away = p.transform.position - a.center;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.right;
                    p.Knock(away.normalized * 12f, 9f); // flung into the sea with a splash
                }

                if (cam != null) cam.Shake(0.3f, 0.3f);
            }
        }

        static bool InCircle(Vector3 pos, Vector3 center, float radius)
        {
            float dx = pos.x - center.x, dz = pos.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }

        void AnimateEffect(Attack a, float age)
        {
            if (a.effect == null) { a.finished = true; return; }

            // Shoots up fast, holds, then sinks back.
            float up = age < 0.15f ? 1f - Mathf.Pow(1f - age / 0.15f, 2f)
                     : age < 0.6f ? 1f
                     : Mathf.Clamp01(1f - (age - 0.6f) / 0.5f);

            float height = a.grab ? 4f : 6.4f;
            float rise = a.grab ? 3.2f : 5.4f;
            a.effect.transform.position = new Vector3(a.center.x, a.groundY - height * 0.5f + up * rise, a.center.z);

            if (a.grab) a.effect.transform.localScale = new Vector3(a.radius * 2f, height * 0.5f, a.radius * 2f);
            else a.effect.transform.localScale = new Vector3(1.4f, height * 0.5f, 1.4f);

            if (age > 1.2f)
            {
                Destroy(a.effect);
                a.effect = null;
                a.finished = true;
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
            RenderSettings.fogDensity = Mathf.Lerp(_fogDensity0, 0.012f, k);

            if (_seaMaterial != null)
            {
                SetColor(_seaMaterial, "_ShallowColor", Color.Lerp(new Color(0.2f, 0.75f, 1f, 0.65f), new Color(0.03f, 0.12f, 0.25f, 0.85f), k));
                SetColor(_seaMaterial, "_DeepColor", Color.Lerp(new Color(0.03f, 0.3f, 0.85f, 0.85f), new Color(0.01f, 0.04f, 0.12f, 0.95f), k));
                SetColor(_seaMaterial, "_BaseColor", Color.Lerp(new Color(0.1f, 0.55f, 1f, 0.6f), new Color(0.02f, 0.08f, 0.2f, 0.85f), k));
            }

            if (head != null)
            {
                bool show = e > 2f;
                if (head.activeSelf != show) head.SetActive(show);
                float rise = Mathf.SmoothStep(0f, 1f, (e - 2f) / 12f);
                Vector3 p = head.transform.position;
                head.transform.position = new Vector3(p.x, Mathf.Lerp(-14f, 6f, rise), p.z);
            }

            if (_rumble != null) _rumble.volume = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(e / 1.5f));

            // The text shown while watching.
            StatusText = "";
            foreach (var p in PlayerController.All)
                if (p.IsLocal && p.IsEaten) StatusText = "Swallowed! Watching your friends...";
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
