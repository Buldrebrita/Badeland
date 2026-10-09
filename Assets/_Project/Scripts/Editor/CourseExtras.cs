using System.Collections.Generic;
using System.IO;
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
            public InflatableCourse.Layout layout;
        }

        public static void Build(Context c)
        {
            BuildTrapAndRoom(c.layout);
            BuildMonster(c);
            BuildAlarmAndRailings(c.layout);
            ApplyLook(c);
        }

        // ------------------------------------------------------------------ the finale slide

        /// <summary>The water slide: a smooth track following the given points, with rails and support pillars.</summary>
        public static void BuildSlideFromPath(Vector3[] path)
        {
            var orange = new Color(1f, 0.55f, 0.1f);

            var root = new GameObject("Water Slide");
            var slide = root.AddComponent<WaterSlide>();

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

            // The track is three smooth meshes (a floor and two rails) that follow the curve without any gaps.
            float railX = slide.laneHalfWidth + 0.2f + 0.18f;
            BuildSlideMesh(root.transform, slide, "Slide Floor", slide.laneHalfWidth + 0.2f, 0.125f, new Vector2(0f, -0.125f), new Color(0.15f, 0.55f, 1f));
            BuildSlideMesh(root.transform, slide, "Slide Rail Left", 0.18f, 0.45f, new Vector2(-railX, 0.35f), orange);
            BuildSlideMesh(root.transform, slide, "Slide Rail Right", 0.18f, 0.45f, new Vector2(railX, 0.35f), orange);

            // Support pillars now and then.
            const float spacing = 7f;
            for (float d = 4f; d < slide.Length; d += spacing)
            {
                slide.SampleAt(d, out Vector3 pos, out _);
                if (pos.y < 1.6f) continue;
                float height = pos.y - 0.3f + 0.8f;
                var pillar = S3SceneBuilder.Cyl("Slide Pillar", new Vector3(pos.x, -0.8f + height * 0.5f, pos.z), new Vector3(0.6f, height * 0.5f, 0.6f), new Color(0.9f, 0.9f, 0.95f));
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root.transform, true);
            }
        }

        // One continuous mesh: a rectangle extruded along the slide's curve. Saved as an asset so the scene keeps it.
        static void BuildSlideMesh(Transform parent, WaterSlide slide, string name, float halfWidth, float halfHeight, Vector2 offset, Color color)
        {
            const float step = 0.5f;
            int rings = Mathf.CeilToInt(slide.Length / step) + 1;

            var positions = new Vector3[rings];
            var rotations = new Quaternion[rings];
            for (int i = 0; i < rings; i++)
            {
                slide.SampleAt(Mathf.Min(i * step, slide.Length), out Vector3 pos, out Vector3 tangent);
                positions[i] = pos;
                rotations[i] = Quaternion.LookRotation(tangent, Vector3.up);
            }

            Vector2[] corners =
            {
                new Vector2(-halfWidth, -halfHeight) + offset, new Vector2(halfWidth, -halfHeight) + offset,
                new Vector2(halfWidth, halfHeight) + offset, new Vector2(-halfWidth, halfHeight) + offset,
            };

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int face = 0; face < 4; face++)
            {
                Vector2 a = corners[face], b = corners[(face + 1) % 4];
                int start = vertices.Count;
                for (int i = 0; i < rings; i++)
                {
                    vertices.Add(positions[i] + rotations[i] * new Vector3(a.x, a.y, 0f));
                    vertices.Add(positions[i] + rotations[i] * new Vector3(b.x, b.y, 0f));
                }
                for (int i = 0; i < rings - 1; i++)
                {
                    int v0 = start + 2 * i, v1 = v0 + 1, v2 = v0 + 2, v3 = v0 + 3;
                    triangles.Add(v0); triangles.Add(v2); triangles.Add(v1);
                    triangles.Add(v1); triangles.Add(v2); triangles.Add(v3);
                }
            }

            // Build the surface, work out its normals, then add a second copy facing the other way, so the slide shows
            // from every side whatever the material does (and whichever way the triangles happen to wind).
            var front = new Mesh();
            front.SetVertices(vertices);
            front.SetTriangles(triangles, 0);
            front.RecalculateNormals();
            var frontNormals = new List<Vector3>();
            front.GetNormals(frontNormals);

            int count = vertices.Count;
            var allVertices = new List<Vector3>(vertices);
            allVertices.AddRange(vertices);
            var allNormals = new List<Vector3>(frontNormals);
            foreach (var n in frontNormals) allNormals.Add(-n);
            var allTriangles = new List<int>(triangles);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                allTriangles.Add(triangles[i] + count);
                allTriangles.Add(triangles[i + 2] + count);
                allTriangles.Add(triangles[i + 1] + count);
            }
            Object.DestroyImmediate(front);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(allVertices);
            mesh.SetNormals(allNormals);
            mesh.SetTriangles(allTriangles, 0);
            mesh.RecalculateBounds();

            Directory.CreateDirectory("Assets/_Project/Art/Environment");
            string path = "Assets/_Project/Art/Environment/" + name.Replace(' ', '_') + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            GrayboxMaterials.Tint(go, color);
        }

        // ------------------------------------------------------------------ hidden trap and treasure room

        static void BuildTrapAndRoom(InflatableCourse.Layout layout)
        {
            // ---- The secret room, far off to the side of the park.
            Vector3 rc = new Vector3(0f, 0f, 300f);
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
            returnPoint.position = layout.trapReturn;

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
            var panel = layout.trapPanel; // one of the round float pads on the course

            var duck = new GameObject("Suspicious Duck");
            duck.transform.position = panel.transform.position + new Vector3(1.2f, 0.6f, 0f);
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

        // ------------------------------------------------------------------ the monster

        static void BuildMonster(Context c)
        {
            Vector3 A = c.layout.arenaCenter; // the finish platform: the monster rises from the water beside it

            // ---- Pieces used by the attacks (switched off until used).
            var telegraph = S3SceneBuilder.Cyl("Telegraph Template", new Vector3(0f, -50f, 0f), new Vector3(1f, 0.02f, 1f), Color.red);
            GrayboxMaterials.TintWater(telegraph, new Color(1f, 0.15f, 0.1f, 0.5f));
            Object.DestroyImmediate(telegraph.GetComponent<Collider>());

            // One section of a tentacle or of the neck. They are long chains of these laid along a curve.
            var section = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            section.name = "Section Template";
            section.transform.position = new Vector3(0f, -50f, 0f);
            GrayboxMaterials.Tint(section, new Color(0.32f, 0.12f, 0.52f));
            Object.DestroyImmediate(section.GetComponent<Collider>());

            // A deep-sea horror: near-black, with glowing red eyes, a gaping mouth and crooked teeth.
            var skin = new Color(0.04f, 0.1f, 0.13f);
            var flesh = new Color(0.32f, 0.03f, 0.06f);
            var tooth = new Color(0.85f, 0.83f, 0.7f);
            var teal = skin;

            // ---- The head. It faces its local +Z, and sits below the sea until the strike.
            var head = new GameObject("Monster Head");
            head.transform.position = A + new Vector3(32f, -30f, -16f);

            Part(head.transform, PrimitiveType.Sphere, "Skull", new Vector3(0f, 0f, 0f), Vector3.one * 12f, skin);
            Part(head.transform, PrimitiveType.Sphere, "Brow", new Vector3(0f, 3.4f, 3.2f), new Vector3(9f, 2.2f, 5f), skin * 0.8f);
            Part(head.transform, PrimitiveType.Sphere, "Upper Jaw", new Vector3(0f, -0.5f, 5f), new Vector3(8f, 2.8f, 9f), skin);
            Part(head.transform, PrimitiveType.Sphere, "Inside Of Mouth", new Vector3(0f, -1.3f, 3.5f), new Vector3(6.4f, 2f, 7f), flesh);

            var jawPivot = new GameObject("Jaw Pivot").transform; // the lower jaw swings open around this
            jawPivot.SetParent(head.transform, false);
            jawPivot.localPosition = new Vector3(0f, -2f, 1.5f);
            Part(jawPivot, PrimitiveType.Sphere, "Lower Jaw", new Vector3(0f, 0f, 3.8f), new Vector3(7.2f, 2.2f, 8.4f), skin);

            // Crooked teeth of different lengths, leaning every which way.
            var rng = new System.Random(5);
            for (int i = -3; i <= 3; i++)
            {
                float upperLength = 1.2f + (float)rng.NextDouble() * 1.6f;
                float lowerLength = 1.0f + (float)rng.NextDouble() * 1.6f;
                var upper = Part(head.transform, PrimitiveType.Cube, "Tooth", new Vector3(i * 1.05f, -1.9f - upperLength * 0.25f, 7.7f + (float)rng.NextDouble() * 0.4f), new Vector3(0.5f, upperLength, 0.5f), tooth);
                upper.localRotation = Quaternion.Euler((float)rng.NextDouble() * 24f - 12f, 0f, (float)rng.NextDouble() * 24f - 12f);
                var lower = Part(jawPivot, PrimitiveType.Cube, "Tooth", new Vector3(i * 1.05f, 1.0f + lowerLength * 0.25f, 7.1f + (float)rng.NextDouble() * 0.4f), new Vector3(0.5f, lowerLength, 0.5f), tooth);
                lower.localRotation = Quaternion.Euler((float)rng.NextDouble() * 24f - 12f, 0f, (float)rng.NextDouble() * 24f - 12f);
            }

            // Glowing red eyes with thin vertical slit pupils.
            var eyes = new Transform[2];
            var pupils = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                eyes[i] = PartGlow(head.transform, PrimitiveType.Sphere, "Eye", new Vector3(side * 2.9f, 2.6f, 4.4f), Vector3.one * 3.2f, new Color(1f, 0.12f, 0.05f), 2.5f);
                pupils[i] = Part(head.transform, PrimitiveType.Sphere, "Pupil", new Vector3(side * 2.9f, 2.6f, 5.8f), new Vector3(0.45f, 2.4f, 0.8f), Color.black);
            }

            // Faint cold lights along the skull, like the deep-sea creatures they are not.
            for (int i = 0; i < 16; i++)
            {
                double theta = rng.NextDouble() * Mathf.PI * 2f;
                double up = 0.15 + rng.NextDouble() * 0.8;
                double ring = System.Math.Sqrt(1.0 - up * up);
                Vector3 direction = new Vector3((float)(ring * System.Math.Cos(theta)), (float)up, (float)(ring * System.Math.Sin(theta)));
                PartGlow(head.transform, PrimitiveType.Sphere, "Light Spot", direction * 5.95f, Vector3.one * 0.5f, new Color(0.1f, 0.9f, 0.9f), 2f);
            }

            // ---- The body: big humps that rise out of the sea with the head.
            var body = new GameObject("Monster Body");
            body.transform.position = A + new Vector3(80f, -30f, -30f);
            Part(body.transform, PrimitiveType.Sphere, "Back", Vector3.zero, Vector3.one * 34f, teal);
            Part(body.transform, PrimitiveType.Sphere, "Hump", new Vector3(-14f, -3f, 13f), Vector3.one * 24f, teal * 0.9f);
            Part(body.transform, PrimitiveType.Sphere, "Hump", new Vector3(-12f, -5f, -15f), Vector3.one * 20f, teal * 0.8f);

            // ---- Positions: where the neck comes out of the sea, where the head hangs, where the tentacles come out.
            var neckBase = new GameObject("Neck Base").transform;
            neckBase.position = A + new Vector3(46f, -3f, -8f);
            var lurk = new GameObject("Head Lurk Point").transform;
            lurk.position = A + new Vector3(26f, 9f, -12f);

            Vector3[] anchors =
            {
                A + new Vector3(22f, -1f, 6f), A + new Vector3(22f, -1f, -6f), A + new Vector3(18f, -1f, -18f),
                A + new Vector3(4f, -1f, -20f), A + new Vector3(-8f, -1f, -20f),
            };
            var bases = new Transform[anchors.Length];
            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = new GameObject("Tentacle Base " + i).transform;
                anchor.position = anchors[i];
                bases[i] = anchor;
            }

            var encounterObject = new GameObject("Monster Encounter");
            var encounter = encounterObject.AddComponent<MonsterEncounter>();
            encounter.tracker = c.tracker;
            encounter.platformCenter = A;
            encounter.platformSize = new Vector3(c.layout.arenaSize.x, 1f, c.layout.arenaSize.z);
            encounter.platformTopY = 0.6f;
            encounter.celebrationSeconds = 3f;
            encounter.warningSeconds = 7f;
            encounter.sun = c.sun;
            encounter.seaRenderer = c.sea != null ? c.sea.GetComponent<Renderer>() : null;
            encounter.telegraphTemplate = telegraph;
            encounter.segmentTemplate = section;
            encounter.head = head.transform;
            encounter.jawPivot = jawPivot;
            encounter.eyes = eyes;
            encounter.pupils = pupils;
            encounter.eyeRadius = 1.6f;
            encounter.body = body;
            encounter.neckBase = neckBase;
            encounter.lurkPoint = lurk;
            encounter.tentacleBases = bases;

            // Switch the pieces off now, so they are not in the world until used.
            telegraph.SetActive(false);
            section.SetActive(false);
            head.SetActive(false);
            body.SetActive(false);
        }

        // Like Part, but the colour glows.
        static Transform PartGlow(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Color color, float strength)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GrayboxMaterials.TintGlow(go, color, strength);
            return go.transform;
        }

        // A coloured primitive without a collider, parented and placed relative to its parent.
        static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GrayboxMaterials.Tint(go, color);
            return go.transform;
        }

        // ------------------------------------------------------------------ the danger alarm: red lights and railings

        static void BuildAlarmAndRailings(InflatableCourse.Layout layout)
        {
            // ---- Red alarm beacons on poles around the finish platform.
            Vector3 c0 = layout.arenaCenter;
            float hx = layout.arenaSize.x * 0.5f - 1.5f, hz = layout.arenaSize.z * 0.5f - 1.5f;
            Vector3[] spots =
            {
                c0 + new Vector3(-hx, 0f, -hz), c0 + new Vector3(hx, 0f, -hz), c0 + new Vector3(-hx, 0f, hz),
                c0 + new Vector3(hx, 0f, hz), c0 + new Vector3(0f, 0f, -hz), c0 + new Vector3(0f, 0f, hz),
            };

            var alarmObject = new GameObject("Alarm Lights");
            var lights = new List<Light>();
            var beacons = new List<Renderer>();
            foreach (var spot in spots)
            {
                var pole = S3SceneBuilder.Cyl("Alarm Pole", new Vector3(spot.x, 0.6f + 1.9f, spot.z), new Vector3(0.3f, 1.9f, 0.3f), new Color(0.85f, 0.85f, 0.9f));
                Object.DestroyImmediate(pole.GetComponent<Collider>());
                pole.transform.SetParent(alarmObject.transform, true);

                var beacon = S3SceneBuilder.Ball("Alarm Beacon", new Vector3(spot.x, 0.6f + 4.3f, spot.z), 1f, new Color(0.25f, 0.02f, 0.02f), false);
                beacon.transform.SetParent(alarmObject.transform, true);
                beacons.Add(beacon.GetComponent<Renderer>());

                var lightObject = new GameObject("Alarm Light");
                lightObject.transform.SetParent(alarmObject.transform, true);
                lightObject.transform.position = beacon.transform.position;
                var l = lightObject.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.12f, 0.08f);
                l.range = 34f;
                l.intensity = 0f;
                l.enabled = false;
                lights.Add(l);
            }

            var alarm = alarmObject.AddComponent<AlarmLights>();
            alarm.lights = lights.ToArray();
            alarm.beacons = beacons.ToArray();

            // ---- Railings around the platform. They are built standing, then lowered out of sight; the danger alarm raises them.
            float ox = layout.arenaSize.x * 0.5f - 0.4f, oz = layout.arenaSize.z * 0.5f - 0.4f;
            Vector3[] outline =
            {
                c0 + new Vector3(-ox, 0f, -oz), c0 + new Vector3(ox, 0f, -oz), c0 + new Vector3(ox, 0f, oz), c0 + new Vector3(-ox, 0f, oz),
            };

            var railRoot = new GameObject("Safety Railings");
            var colliders = new List<Collider>();
            for (int i = 0; i < outline.Length; i++)
                Rail(railRoot.transform, colliders, outline[i], outline[(i + 1) % outline.Length]);
            railRoot.transform.position = new Vector3(0f, -4f, 0f);

            var raiser = railRoot.AddComponent<RailingRaiser>();
            raiser.root = railRoot.transform;
            raiser.colliders = colliders.ToArray();
            raiser.raiseMeters = 4f;
        }

        // One straight run of railing: three white bars, red posts, and an invisible solid wall that is too tall to jump.
        static void Rail(Transform root, List<Collider> colliders, Vector3 a, Vector3 b)
        {
            const float deckTop = 0.6f;
            Vector3 mid = (a + b) * 0.5f;
            float length = Vector3.Distance(a, b);
            bool alongX = Mathf.Abs(a.z - b.z) < 0.01f;

            foreach (float height in new[] { 0.9f, 1.9f, 2.9f })
            {
                var bar = S3SceneBuilder.Box("Rail Bar", new Vector3(mid.x, deckTop + height, mid.z),
                    alongX ? new Vector3(length, 0.18f, 0.18f) : new Vector3(0.18f, 0.18f, length), new Color(0.96f, 0.96f, 0.98f));
                Object.DestroyImmediate(bar.GetComponent<Collider>());
                bar.transform.SetParent(root, true);
            }

            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 4f) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (posts - 1f));
                var post = S3SceneBuilder.Cyl("Rail Post", new Vector3(p.x, deckTop + 1.7f, p.z), new Vector3(0.34f, 1.7f, 0.34f), new Color(0.9f, 0.15f, 0.15f));
                Object.DestroyImmediate(post.GetComponent<Collider>());
                post.transform.SetParent(root, true);
            }

            var wall = new GameObject("Rail Wall");
            wall.transform.SetParent(root, true);
            wall.transform.position = new Vector3(mid.x, deckTop + 1.9f, mid.z);
            var box = wall.AddComponent<BoxCollider>();
            box.size = alongX ? new Vector3(length, 3.8f, 0.4f) : new Vector3(0.4f, 3.8f, length);
            colliders.Add(box);
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

            // Post-processing: a little bloom, richer colours, a soft vignette. Only works when URP is the active render
            // pipeline; without it Unity ignores it, so it is skipped (see docs/S6_MONSTER_AND_MORE.md).
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.Log("Badeland: URP is not the active render pipeline in this project, so the post-processing look is skipped.");
                return;
            }

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
