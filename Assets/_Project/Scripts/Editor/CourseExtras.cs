using System.Collections.Generic;
using Badeland.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Everything on the course beyond the basic loop: the ferry, windmill, water slide, hidden trap and secret room,
    /// the big platform with the monster, scenery, and the lighting/post-processing look. Called by the course builder.
    /// All shapes are gray-box primitives and flat colours for now (see docs/S6_MONSTER_AND_MORE.md).
    /// </summary>
    public static class CourseExtras
    {
        public class Context
        {
            public LapTracker tracker;
            public GameObject sea;
            public Light sun;
            public Camera camera;
        }

        // The big platform in the middle of the sea. The monster fight happens here.
        static readonly Vector3 PlatformCenter = new Vector3(4f, 0f, 0f);
        static readonly Vector3 PlatformSize = new Vector3(14f, 1.2f, 11f);

        public static void Build(Context c)
        {
            BuildFerry();
            BuildWindmill();
            BuildSlide();
            BuildTrapAndRoom();
            BuildArena(c);
            BuildScenery();
            ApplyLook(c);
        }

        // ------------------------------------------------------------------ moving platform

        static void BuildFerry()
        {
            // Crosses the gap in the bottom straight (x 9 to 17), carrying you from west to east.
            var a = new Vector3(11.2f, 0f, -14f);
            var b = new Vector3(14.8f, 0f, -14f);
            var ferry = S3SceneBuilder.Box("Ferry", a, new Vector3(3.5f, 1.2f, 8f), new Color(1f, 0.55f, 0.1f));
            var platform = ferry.AddComponent<MovingPlatform>();
            platform.pointA = a;
            platform.pointB = b;
            platform.period = 7f;
        }

        // ------------------------------------------------------------------ windmill

        static void BuildWindmill()
        {
            // A spinning cross standing across the right straight, between two bounce pads. It turns like a clock
            // face, so you time your run past it or jump the low arm.
            var hub = new GameObject("Windmill");
            hub.transform.position = new Vector3(20f, 3.2f, 3f);

            var armA = S3SceneBuilder.Box("Windmill Arm A", hub.transform.position, new Vector3(6.4f, 0.5f, 0.5f), new Color(0.95f, 0.3f, 0.35f));
            var armB = S3SceneBuilder.Box("Windmill Arm B", hub.transform.position, new Vector3(0.5f, 6.4f, 0.5f), new Color(0.95f, 0.3f, 0.35f));
            Object.DestroyImmediate(armA.GetComponent<Collider>());
            Object.DestroyImmediate(armB.GetComponent<Collider>());
            armA.transform.SetParent(hub.transform, true);
            armB.transform.SetParent(hub.transform, true);

            var cap = S3SceneBuilder.Cyl("Windmill Hub", hub.transform.position, new Vector3(1.2f, 0.4f, 1.2f), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cap.transform.SetParent(hub.transform, true);

            var spin = hub.AddComponent<RotatingBar>();
            spin.rotationAxis = Vector3.forward;
            spin.degreesPerSecond = 50f;
            spin.hitBoxes = new[] { armA.transform, armB.transform };
        }

        // ------------------------------------------------------------------ water slide

        static void BuildSlide()
        {
            // Stairs up to a tower in the top-west corner. Stepping into the slide's mouth on top starts the ride.
            var orange = new Color(1f, 0.55f, 0.1f);
            S3SceneBuilder.Box("Slide Step 1", new Vector3(-18f, 0.8f, 16f), new Vector3(2f, 1.6f, 4f), orange);
            S3SceneBuilder.Box("Slide Step 2", new Vector3(-20f, 1.3f, 16f), new Vector3(2f, 2.6f, 4f), orange);
            S3SceneBuilder.Box("Slide Tower", new Vector3(-22.5f, 1.8f, 16f), new Vector3(3f, 3.6f, 4f), new Color(1f, 0.4f, 0.7f));

            var root = new GameObject("Water Slide");
            var slide = root.AddComponent<WaterSlide>();

            // Down the west side, swirling around the stepping discs, and out over the sea near the bottom deck.
            Vector3[] path =
            {
                new Vector3(-22.5f, 4.1f, 15.0f),
                new Vector3(-22.5f, 3.8f, 11.0f),
                new Vector3(-22.5f, 3.2f, 5.0f),
                new Vector3(-19.0f, 2.6f, 0.5f),
                new Vector3(-22.0f, 2.0f, -4.0f),
                new Vector3(-18.5f, 1.4f, -7.0f),
                new Vector3(-15.0f, 0.9f, -7.8f),
                new Vector3(-12.5f, 0.8f, -8.6f),
            };

            var points = new Transform[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var point = new GameObject("Slide Point " + i).transform;
                point.SetParent(root.transform);
                point.position = path[i];
                points[i] = point;
            }
            slide.points = points;
            slide.Rebuild();

            // The track: a chain of slabs with orange rails, following the curve.
            var track = new GameObject("Slide Track").transform;
            track.SetParent(root.transform);

            const float spacing = 1.4f;
            int segments = Mathf.CeilToInt(slide.Length / spacing);
            float width = slide.laneHalfWidth * 2f + 0.4f;
            for (int i = 0; i < segments; i++)
            {
                slide.SampleAt(i * spacing + spacing * 0.5f, out Vector3 pos, out Vector3 tangent);
                Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);

                var slab = S3SceneBuilder.Box("Slide Slab", pos - Vector3.up * 0.125f, new Vector3(width, 0.25f, spacing * 1.15f), new Color(0.15f, 0.55f, 1f));
                slab.transform.rotation = rot;
                Object.DestroyImmediate(slab.GetComponent<Collider>());
                slab.transform.SetParent(track, true);

                for (int side = -1; side <= 1; side += 2)
                {
                    var rail = S3SceneBuilder.Box("Slide Rail", pos + rot * new Vector3(side * (width * 0.5f), 0.4f, 0f), new Vector3(0.35f, 0.9f, spacing * 1.15f), orange);
                    rail.transform.rotation = rot;
                    Object.DestroyImmediate(rail.GetComponent<Collider>());
                    rail.transform.SetParent(track, true);
                }

                // Support pillars every few slabs.
                if (i % 5 == 2 && pos.y > 1.6f)
                {
                    float height = pos.y - 0.3f + 0.8f;
                    var pillar = S3SceneBuilder.Cyl("Slide Pillar", new Vector3(pos.x, -0.8f + height * 0.5f, pos.z), new Vector3(0.5f, height * 0.5f, 0.5f), new Color(0.9f, 0.9f, 0.95f));
                    Object.DestroyImmediate(pillar.GetComponent<Collider>());
                    pillar.transform.SetParent(track, true);
                }
            }
        }

        // ------------------------------------------------------------------ hidden trap and treasure room

        static void BuildTrapAndRoom()
        {
            // ---- The secret room, far off to the side of the park.
            Vector3 rc = new Vector3(300f, 0f, 0f);
            var tile = new Color(0.85f, 0.8f, 0.95f);
            S3SceneBuilder.Box("Room Floor", rc + new Vector3(0f, -0.5f, 0f), new Vector3(16f, 1f, 16f), tile);
            S3SceneBuilder.Box("Room Wall West", rc + new Vector3(-8.5f, 3f, 0f), new Vector3(1f, 6f, 18f), new Color(0.45f, 0.35f, 0.7f));
            S3SceneBuilder.Box("Room Wall East", rc + new Vector3(8.5f, 3f, 0f), new Vector3(1f, 6f, 18f), new Color(0.45f, 0.35f, 0.7f));
            S3SceneBuilder.Box("Room Wall North", rc + new Vector3(0f, 3f, 8.5f), new Vector3(16f, 6f, 1f), new Color(0.45f, 0.35f, 0.7f));
            S3SceneBuilder.Box("Room Wall South", rc + new Vector3(0f, 3f, -8.5f), new Vector3(16f, 6f, 1f), new Color(0.45f, 0.35f, 0.7f));

            var treasure = S3SceneBuilder.Ball("Treasure", rc + new Vector3(0f, 1.2f, -5f), 1.4f, new Color(1f, 0.82f, 0.1f), false);
            treasure.transform.localScale = new Vector3(1.4f, 1.0f, 1.0f);
            treasure.AddComponent<Spinner>().degreesPerSecond = 140f;
            treasure.AddComponent<Bobber>();

            var exitPad = S3SceneBuilder.Box("Exit Pad", rc + new Vector3(5.5f, 0.1f, 5.5f), new Vector3(3f, 0.2f, 3f), new Color(0.2f, 0.9f, 1f));
            exitPad.GetComponent<BoxCollider>().isTrigger = true;

            var dropPoint = new GameObject("Room Drop Point").transform;
            dropPoint.position = rc + new Vector3(0f, 7f, 0f);

            var returnPoint = new GameObject("Room Return Point").transform;
            returnPoint.position = new Vector3(-14.5f, 1.8f, 12f);

            // The flood: a water box whose top starts just under the floor and rises once the treasure is taken.
            var flood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flood.name = "Room Flood";
            flood.transform.position = rc + new Vector3(0f, -2.05f, 0f);
            flood.transform.localScale = new Vector3(15f, 4f, 15f);
            GrayboxMaterials.ApplyWater(flood, new Color(0.2f, 0.7f, 1f, 0.6f));
            flood.GetComponent<BoxCollider>().isTrigger = true;
            flood.AddComponent<WaterVolume>();

            var lightObject = new GameObject("Room Light");
            lightObject.transform.position = rc + new Vector3(0f, 5f, 0f);
            var roomLight = lightObject.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.range = 28f;
            roomLight.intensity = 6f;
            roomLight.color = new Color(1f, 0.85f, 0.6f);

            var roomObject = new GameObject("Secret Room");
            var room = roomObject.AddComponent<SecretRoom>();
            room.id = 1;
            room.dropPoint = dropPoint;
            room.treasure = treasure.transform;
            room.exitPad = exitPad.transform;
            room.returnPoint = returnPoint;
            room.flood = flood.transform;
            room.roomCenter = rc + new Vector3(0f, 5f, 0f);
            room.roomSize = new Vector3(17f, 14f, 17f);

            // ---- The trap on the top straight: a trap door with a suspicious inflatable duck on it.
            var panel = S3SceneBuilder.Box("Trap Door", new Vector3(-10f, 0f, 16f), new Vector3(4f, 1.2f, 4f), new Color(0.9f, 0.9f, 0.98f));

            var duck = new GameObject("Suspicious Duck");
            duck.transform.position = new Vector3(-10f, 0.6f, 16f);
            var body = S3SceneBuilder.Ball("Duck Body", duck.transform.position + new Vector3(0f, 0.8f, 0f), 1.6f, new Color(1f, 0.9f, 0.15f), false);
            var head = S3SceneBuilder.Ball("Duck Head", duck.transform.position + new Vector3(0f, 1.8f, 0.4f), 1f, new Color(1f, 0.9f, 0.15f), false);
            var beak = S3SceneBuilder.Box("Duck Beak", duck.transform.position + new Vector3(0f, 1.75f, 1f), new Vector3(0.5f, 0.2f, 0.5f), new Color(1f, 0.5f, 0.1f));
            var eyeL = S3SceneBuilder.Ball("Duck Eye", duck.transform.position + new Vector3(-0.25f, 2.05f, 0.8f), 0.2f, Color.black, false);
            var eyeR = S3SceneBuilder.Ball("Duck Eye", duck.transform.position + new Vector3(0.25f, 2.05f, 0.8f), 0.2f, Color.black, false);
            Object.DestroyImmediate(beak.GetComponent<Collider>());
            foreach (var part in new[] { body, head, beak, eyeL, eyeR }) part.transform.SetParent(duck.transform, true);

            var trapObject = new GameObject("Hidden Trap");
            var trap = trapObject.AddComponent<HiddenTrap>();
            trap.id = 1;
            trap.suspicious = duck.transform;
            trap.panel = panel.transform;
            trap.room = room;
        }

        // ------------------------------------------------------------------ the big platform and the monster

        static void BuildArena(Context c)
        {
            var deck = S3SceneBuilder.Deck("Big Platform", PlatformCenter, PlatformSize);

            // Orange border (visual only) and green corner posts, like the rest of the park.
            float hx = PlatformSize.x * 0.5f, hz = PlatformSize.z * 0.5f;
            foreach (var edge in new[]
            {
                new Vector3(0f, 0.62f, hz - 0.2f), new Vector3(0f, 0.62f, -hz + 0.2f),
            })
                S3SceneBuilder.Trim("Platform Trim", PlatformCenter + edge, new Vector3(PlatformSize.x, 0.1f, 0.4f));
            foreach (var edge in new[]
            {
                new Vector3(hx - 0.2f, 0.62f, 0f), new Vector3(-hx + 0.2f, 0.62f, 0f),
            })
                S3SceneBuilder.Trim("Platform Trim", PlatformCenter + edge, new Vector3(0.4f, 0.1f, PlatformSize.z));

            // Beach balls for the party before the monster (they are also things to jump over while dodging).
            S3SceneBuilder.Ball("Party Ball", PlatformCenter + new Vector3(4.5f, 1.85f, 3f), 2.2f, new Color(1f, 0.3f, 0.3f), true);
            S3SceneBuilder.Ball("Party Ball", PlatformCenter + new Vector3(-4.5f, 1.85f, -3f), 2.2f, new Color(0.3f, 0.5f, 1f), true);

            // The bridge from the bottom deck. It only inflates once everyone has finished the laps.
            var bridge = S3SceneBuilder.Box("Bridge", new Vector3(4f, 0f, -7.75f), new Vector3(4f, 1.2f, 4.5f), new Color(1f, 0.4f, 0.7f));
            var unlock = new GameObject("Bridge Unlock").AddComponent<FinishUnlock>();
            unlock.tracker = c.tracker;
            unlock.enableOnFinish = new[] { bridge };

            // ---- Monster pieces (switched off until the strike).
            var telegraph = S3SceneBuilder.Cyl("Telegraph Template", new Vector3(0f, -50f, 0f), new Vector3(1f, 0.02f, 1f), Color.red);
            GrayboxMaterials.TintWater(telegraph, new Color(1f, 0.15f, 0.1f, 0.5f));
            Object.DestroyImmediate(telegraph.GetComponent<Collider>());

            var tentacle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tentacle.name = "Tentacle Template";
            tentacle.transform.position = new Vector3(0f, -50f, 0f);
            GrayboxMaterials.Tint(tentacle, new Color(0.32f, 0.1f, 0.5f));
            Object.DestroyImmediate(tentacle.GetComponent<Collider>());

            var maw = S3SceneBuilder.Cyl("Mouth Template", new Vector3(0f, -50f, 0f), new Vector3(1f, 2f, 1f), new Color(0.45f, 0.05f, 0.1f));
            Object.DestroyImmediate(maw.GetComponent<Collider>());

            // The head: a huge dark sphere with two big eyes, rising out of the sea behind the park.
            var head = new GameObject("Monster Head");
            head.transform.position = new Vector3(4f, -14f, 46f);
            var skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skull.name = "Skull";
            skull.transform.SetParent(head.transform, false);
            skull.transform.localScale = Vector3.one * 24f;
            GrayboxMaterials.Tint(skull, new Color(0.08f, 0.22f, 0.3f));
            Object.DestroyImmediate(skull.GetComponent<Collider>());
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                eye.transform.SetParent(head.transform, false);
                eye.transform.localPosition = new Vector3(side * 5f, 3f, -10.6f);
                eye.transform.localScale = Vector3.one * 5.3f;
                GrayboxMaterials.Tint(eye, new Color(1f, 1f, 0.7f));
                Object.DestroyImmediate(eye.GetComponent<Collider>());

                var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil";
                pupil.transform.SetParent(head.transform, false);
                pupil.transform.localPosition = new Vector3(side * 5f, 3f, -12.6f);
                pupil.transform.localScale = Vector3.one * 2.6f;
                GrayboxMaterials.Tint(pupil, Color.black);
                Object.DestroyImmediate(pupil.GetComponent<Collider>());
            }

            var encounterObject = new GameObject("Monster Encounter");
            var encounter = encounterObject.AddComponent<MonsterEncounter>();
            encounter.tracker = c.tracker;
            encounter.platformCenter = PlatformCenter;
            encounter.platformSize = new Vector3(PlatformSize.x, 1f, PlatformSize.z);
            encounter.platformTopY = 0.6f;
            encounter.sun = c.sun;
            encounter.seaRenderer = c.sea != null ? c.sea.GetComponent<Renderer>() : null;
            encounter.telegraphTemplate = telegraph;
            encounter.tentacleTemplate = tentacle;
            encounter.mawTemplate = maw;
            encounter.head = head;

            // Switch the templates off now, so they are not in the world until used.
            telegraph.SetActive(false);
            tentacle.SetActive(false);
            maw.SetActive(false);
            head.SetActive(false);
        }

        // ------------------------------------------------------------------ scenery

        static void BuildScenery()
        {
            // Big hoops over the course to run through.
            var grey = new Color(0.82f, 0.84f, 0.92f);
            var green = new Color(0.35f, 0.8f, 0.4f);
            Ring("Hoop", new Vector3(20f, 4.4f, -10.5f), 3.8f, Quaternion.identity, grey);
            Ring("Hoop", new Vector3(20f, 4.4f, 10.5f), 3.8f, Quaternion.identity, green);
            Ring("Hoop", new Vector3(-14f, 4.4f, 14f), 3.8f, Quaternion.Euler(0f, 90f, 0f), grey);
            Ring("Hoop", new Vector3(14f, 4.4f, 14f), 3.8f, Quaternion.Euler(0f, 90f, 0f), green);
            Ring("Hoop", new Vector3(-16f, 4.4f, -14f), 3.8f, Quaternion.Euler(0f, 90f, 0f), grey);

            // Drones hovering over the park.
            Vector3[] spots =
            {
                new Vector3(-30f, 12f, -30f), new Vector3(35f, 14f, 10f), new Vector3(-35f, 11f, 25f), new Vector3(10f, 13f, 40f),
                new Vector3(45f, 12f, -35f), new Vector3(0f, 15f, -40f), new Vector3(-45f, 13f, 0f), new Vector3(30f, 11f, 35f),
            };
            for (int i = 0; i < spots.Length; i++) Drone(spots[i], i * 0.9f);
        }

        static void Ring(string name, Vector3 center, float radius, Quaternion rotation, Color color)
        {
            var root = new GameObject(name);
            root.transform.position = center;
            root.transform.rotation = rotation;

            const int segments = 16;
            float segmentLength = 2f * Mathf.PI * radius / segments * 1.15f;
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                var seg = S3SceneBuilder.Box(name + " Segment", center, Vector3.one, color);
                Object.DestroyImmediate(seg.GetComponent<Collider>());
                seg.transform.SetParent(root.transform, false);
                seg.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);
                seg.transform.localScale = new Vector3(0.5f, segmentLength, 0.5f);
            }
        }

        static void Drone(Vector3 position, float phase)
        {
            var root = new GameObject("Drone");
            root.transform.position = position;

            var body = S3SceneBuilder.Box("Drone Body", position, new Vector3(1.6f, 1.2f, 1.6f), new Color(0.2f, 0.7f, 0.9f));
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, true);

            var eye = S3SceneBuilder.Ball("Drone Eye", position + new Vector3(0f, 0f, -0.85f), 0.7f, Color.black, false);
            eye.transform.SetParent(root.transform, true);

            for (int side = -1; side <= 1; side += 2)
            {
                var rotor = S3SceneBuilder.Box("Drone Rotor", position + new Vector3(side * 1.4f, 0.9f, 0f), new Vector3(2.4f, 0.1f, 0.35f), new Color(0.95f, 0.95f, 0.95f));
                Object.DestroyImmediate(rotor.GetComponent<Collider>());
                rotor.transform.SetParent(root.transform, true);
                rotor.AddComponent<Spinner>().degreesPerSecond = 720f;
            }

            var bob = root.AddComponent<Bobber>();
            bob.amplitude = 0.6f;
            bob.speed = 0.9f;
            bob.phase = phase;
        }

        // ------------------------------------------------------------------ the look

        static void ApplyLook(Context c)
        {
            // Warm sun with soft shadows, and bright sky-tinted ambient light.
            if (c.sun != null)
            {
                c.sun.color = new Color(1f, 0.96f, 0.86f);
                c.sun.intensity = 1.25f;
                c.sun.shadows = LightShadows.Soft;
                c.sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.8f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.78f, 0.85f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.5f, 0.55f);
            RenderSettings.fog = false;

            // Post-processing: a little bloom, richer colours, a soft vignette.
            const string path = "Assets/_Project/Settings/PostProcessing.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);

                var bloom = AddTo<Bloom>(profile);
                bloom.threshold.Override(1f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.6f);

                var tone = AddTo<Tonemapping>(profile);
                tone.mode.Override(TonemappingMode.ACES);

                var adjust = AddTo<ColorAdjustments>(profile);
                adjust.postExposure.Override(0.15f);
                adjust.contrast.Override(10f);
                adjust.saturation.Override(18f);

                var vignette = AddTo<Vignette>(profile);
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);

                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            var volumeObject = new GameObject("Post Processing");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            if (c.camera != null)
            {
                var data = c.camera.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = c.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
            }
        }

        static T AddTo<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
