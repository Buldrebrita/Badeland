using System.Collections.Generic;
using Badeland.World;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// The waterpark obstacle course: ONE long, continuous inflatable course floating on the sea. It winds in a snake
    /// from the start platform (north-west) back and forth to the finish platform (near the beach). Every obstacle is
    /// joined directly to the next, with square corner pads where the route turns. There is only one route.
    /// The finish platform is also where the monster appears.
    /// </summary>
    public static class InflatableCourse
    {
        public class Section
        {
            public string name;
            public Vector3 center, direction;
            public float length, width;
            public Vector3 start;
        }

        public class Layout
        {
            public List<Section> sections = new List<Section>();
            public Checkpoint[] gates;
            public Vector3 startSpawn;
            public Vector3 startCenter, startSize;
            public Vector3 arenaCenter, arenaSize;       // the finish platform
            public Vector3 courseCenter;                  // the middle of the whole structure
            public Vector2 areaMin, areaMax;              // the play area (x, z)
            public GameObject trapPanel;                  // a square corner pad that gives way under the suspicious duck
            public Vector3 trapReturn;
        }

        // The inflatables' colours (after the reference picture: purple floors, pink edges, yellow and green tubes).
        static readonly Color Purple = new Color(0.58f, 0.4f, 0.88f);
        static readonly Color Pink = new Color(1f, 0.38f, 0.68f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
        static readonly Color Green = new Color(0.35f, 0.82f, 0.3f);
        static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
        static readonly Color Blue = new Color(0.15f, 0.5f, 0.95f);
        static readonly Color Teal = new Color(0.1f, 0.78f, 0.8f);
        static readonly Color Red = new Color(0.95f, 0.3f, 0.3f);
        static readonly Color White = new Color(0.97f, 0.97f, 1f);
        static readonly Color Foamy = new Color(0.92f, 0.98f, 1f);
        static readonly Color Glass = new Color(0.3f, 0.65f, 1f, 0.35f);

        const float Top = 0.6f;     // the top of a deck, above its own middle
        const float Lift = 2.4f;    // how high the whole course floats above the sea (too high to climb out of the water)
        const float Width = 10f;    // how wide the main route is

        static Vector3 _pos, _dir;
        static Layout _layout;
        static Mesh _ringMesh;
        static int _railColor;

        // ------------------------------------------------------------------ the route

        public static Layout Build(FishSpecies cod, FishSpecies salmon, FishSpecies clown)
        {
            MeshKit.Reset();
            _ringMesh = MeshKit.Torus(3.0f, 0.3f);
            _layout = new Layout();
            var L = _layout;
            var gates = new List<Checkpoint>();

            Vector3 east = Vector3.right, south = Vector3.back, west = Vector3.left;

            // ---- 1. The start platform, in the north-west corner of the course.
            L.startCenter = new Vector3(-53f, Lift, 144f);
            L.startSize = new Vector3(24f, 1.2f, 18f);
            StartPlatform(L.startCenter, east);
            Pylons(L.startCenter, east, 24f, 18f);
            L.startSpawn = L.startCenter + new Vector3(-2f, 1.8f, 0f);

            _pos = new Vector3(-41f, Lift, 144f);
            _dir = east;

            // ---- The far end of the snake: two more hard lanes (C and D) before the old lanes.
            Place("C1 Rotating cross", 14f, Width, ObsC1Cross);
            Place("C2 Pendulum corridor", 16f, Width, ObsC2Pendulums);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("C3 Crumbling planks", 14f, Width, ObsC3Crumbling);
            Place("C4 Sliding walls", 14f, Width, ObsC4SlidingWalls);
            Corner("Corner E", south);
            Place("C5 Cross beam", 12f, 1.4f, ObsC5CrossBeam, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Corner("Corner F", west);

            Place("D1 Bounce chain", 16f, Width, ObsD1BounceChain, false);
            Place("D2 Windmills", 14f, Width, ObsD2Windmills);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("D3 Twin ferries", 16f, Width, ObsD3TwinFerries, false);
            Place("D4 Hop stones", 14f, Width, ObsD4HopStones, false);
            Corner("Corner G", south);
            Place("D5 Pendulum bridge", 12f, 3.2f, ObsD5PendulumBridge, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Corner("Corner H", east);

            // ---- The first half of the snake: the hard warm-up lanes (A and B), then down to the old lane.
            Place("A1 Zigzag pads", 16f, Width, ObsA1ZigzagPads, false);
            Place("A2 Sweeper deck", 14f, Width, ObsA2Sweepers);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("A3 Ferry crossing", 14f, Width, ObsA3Ferry, false);
            Place("A4 Springy mattresses", 14f, Width, ObsA4Mattresses);
            Corner("Corner A", south);
            Place("A5 Narrow beam", 12f, 1.4f, ObsA5NarrowBeam, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Corner("Corner B", west);

            Place("B1 Spinning discs", 16f, Width, ObsB1TwoDiscs);
            Place("B2 Log gauntlet", 16f, Width, ObsB2Logs, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("B3 Ball run", 14f, Width, ObsB3Balls);
            Place("B4 Steep slide", 14f, Width, ObsB4Slide);
            Corner("Corner C", south);
            Place("B5 Wobble steps", 12f, 3.2f, ObsB5Steps, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Corner("Corner D", east);

            // ---- The second half: the lanes below.
            Place("2 Wobble bridge", 16f, 3.6f, Obs2WobbleBridge, false);
            Place("3 Tunnel", 16f, 5.6f, Obs3Tunnel);
            Place("4 Bouncing pillars", 14f, Width, Obs4Pillars);
            Place("5 Climbing wall", 12f, Width, Obs5ClimbingWall);
            gates.Add(MakeGate(L.sections.Count - 1));
            var trapPad = Corner("Corner 1", south);                          // the trap door pad
            Place("6 Floating logs", 12f, Width, Obs6Logs, false);
            Corner("Corner 2", west);

            Place("7 Rotating platform", 16f, Width, Obs7RotatingDisc);
            Place("8 Trampoline", 14f, Width, Obs8Trampoline);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("9 Swinging balls", 16f, Width, Obs9Pendulums);
            Place("10 Slide", 14f, Width, Obs10Slide);
            Corner("Corner 3", south);
            Place("11 Balance section", 12f, 2.0f, Obs11Balance, false);
            gates.Add(MakeGate(L.sections.Count - 1));
            Corner("Corner 4", east);

            Place("12 Rotating padded arms", 16f, Width, Obs12PaddedArms);
            Place("13 Arch maze", 16f, Width, Obs13ArchMaze);
            gates.Add(MakeGate(L.sections.Count - 1));
            Place("14 Final bridge", 30f, 4.2f, Obs14FinalBridge, false);

            // ---- 15. The finish platform, joined to the end of the final bridge.
            L.arenaSize = new Vector3(28f, 1.2f, 24f);
            L.arenaCenter = _pos + east * (L.arenaSize.x * 0.5f);
            FinishPlatform(L.arenaCenter, east);
            Pylons(L.arenaCenter, east, 28f, 24f);

            gates.Add(S3SceneBuilder.Gate("Finish line", L.arenaCenter + new Vector3(-L.arenaSize.x * 0.5f + 3f, 0f, 0f), 90f, new Color(0.2f, 0.9f, 0.3f), 16f));
            L.gates = gates.ToArray();

            // The trap door is the first corner pad. The player comes back to the start of the obstacle before it.
            L.trapPanel = trapPad;
            var wallSection = L.sections.Find(q => q.name == "5 Climbing wall");
            L.trapReturn = wallSection.start + wallSection.direction * 2f + Vector3.up * 1.8f;

            L.courseCenter = new Vector3(-9f, 0f, 78f);
            L.areaMin = new Vector2(-82f, -28f);
            L.areaMax = new Vector2(64f, 166f);

            BuildFish(cod, salmon, clown);
            BuildChests();
            return L;
        }

        static Checkpoint MakeGate(int sectionIndex)
        {
            var s = _layout.sections[sectionIndex];
            Vector3 at = s.start + s.direction * s.length;
            return S3SceneBuilder.Gate("Checkpoint " + s.name, at, Mathf.Atan2(s.direction.x, s.direction.z) * Mathf.Rad2Deg, Yellow, 14f);
        }

        // Puts an obstacle at the end of the route so far, and moves the end of the route past it.
        static Transform Place(string name, float length, float width, System.Action<Transform, float, float> build, bool supports = true)
        {
            Vector3 mid = _pos + _dir * (length * 0.5f);
            var root = new GameObject("Obstacle " + name).transform;
            root.SetPositionAndRotation(mid, Quaternion.LookRotation(_dir));
            build(root, length, width);
            if (supports) Pylons(mid, _dir, length, width);

            _layout.sections.Add(new Section { name = name, center = mid, direction = _dir, length = length, width = width, start = _pos });
            _pos += _dir * length;
            return root;
        }

        // A square pad where the route turns. The outer sides have railings; the way in and the way out are open.
        static GameObject Corner(string name, Vector3 newDir)
        {
            Vector3 centre = _pos + _dir * (Width * 0.5f);
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(centre, Quaternion.LookRotation(_dir));

            var pad = Platform(root, "Pad", 0f, 0f, Width, Width, true);
            Pylon(centre + root.right * 2.6f + root.forward * 2.6f); Pylon(centre - root.right * 2.6f - root.forward * 2.6f);
            Pylon(centre - root.right * 2.6f + root.forward * 2.6f); Pylon(centre + root.right * 2.6f - root.forward * 2.6f);
            bool exitRight = Vector3.Dot(newDir, root.right) > 0f;
            float half = Width * 0.5f;
            RailRun(root, new Vector3(-half, Top, half), new Vector3(half, Top, half));                       // straight on is closed
            float outer = exitRight ? -half : half;
            RailRun(root, new Vector3(outer, Top, -half), new Vector3(outer, Top, half));

            _layout.sections.Add(new Section { name = name, center = centre, direction = _dir, length = Width, width = Width, start = _pos });
            _pos = centre + newDir * half;
            _dir = newDir;
            return pad;
        }

        // ------------------------------------------------------------------ building blocks

        static GameObject P(Transform parent, PrimitiveType type, string name, Vector3 lp, Vector3 ls, Color c,
            string kind = "plain", bool solid = true, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = ls;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            switch (kind)
            {
                case "quilt": GrayboxMaterials.TintQuilted(go, c); break;
                case "wood": GrayboxMaterials.TintWood(go, c); break;
                case "stone": GrayboxMaterials.TintStone(go, c); break;
                case "glass": GrayboxMaterials.TintWater(go, c); break;
                default: GrayboxMaterials.Tint(go, c); break;
            }
            return go;
        }

        // A fat rounded inflatable tube between two points (for looks only).
        static GameObject Tube(Transform parent, Vector3 from, Vector3 to, float diameter, Color c)
        {
            Vector3 d = to - from;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Tube";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            go.transform.localScale = new Vector3(diameter, Mathf.Max(d.magnitude * 0.5f, diameter * 0.5f), diameter);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GrayboxMaterials.TintQuilted(go, c);
            return go;
        }

        static void Foam(Transform parent, Vector3 lp, float sx, float sz)
        {
            return; // the course floats well above the water now, so there is no foam under it (see Pylons)
#pragma warning disable CS0162
            P(parent, PrimitiveType.Cube, "Foam", new Vector3(lp.x, 0.03f, lp.z), new Vector3(sx + 1.2f, 0.04f, sz + 1.2f), Foamy, "plain", false);
#pragma warning restore CS0162
        }

        // A thick inflatable platform: a pink rim all round and a purple floor inside, like in the reference picture.
        static GameObject Platform(Transform parent, string name, float x, float z, float w, float l, bool foam = true)
        {
            var rim = P(parent, PrimitiveType.Cube, name, new Vector3(x, 0f, z), new Vector3(w, 1.2f, l), Pink);
            P(parent, PrimitiveType.Cube, "Floor", new Vector3(x, 0.62f, z), new Vector3(w - 1.4f, 0.05f, l - 1.4f), Purple, "plain", false);
            if (foam) Foam(parent, new Vector3(x, 0f, z), w, l);
            return rim;
        }

        // A railing along the edge of the route: three fat tubes, colourful posts, and an invisible wall too tall to jump.
        static void RailRun(Transform root, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float length = d.magnitude;
            if (length < 0.5f) return;
            Vector3 dir = d / length;

            Color[] tubeColors = { Yellow, Green, Yellow };
            float[] heights = { 0.7f, 1.6f, 2.5f };
            for (int i = 0; i < 3; i++)
                Tube(root, a + dir * 0.4f + Vector3.up * heights[i], b - dir * 0.4f + Vector3.up * heights[i], 0.75f, tubeColors[i]);

            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 4f) + 1);
            Color[] postColors = { Orange, Blue, Pink, Green };
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (posts - 1f));
                var post = P(root, PrimitiveType.Capsule, "Rail Post", p + Vector3.up * 1.7f, new Vector3(1.0f, 1.7f, 1.0f), postColors[(i + _railColor) % postColors.Length], "quilt", false);
                P(root, PrimitiveType.Sphere, "Rail Cap", p + Vector3.up * 3.35f, Vector3.one * 0.7f, Yellow, "plain", false);
            }
            _railColor++;

            var wall = new GameObject("Rail Wall");
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = (a + b) * 0.5f + Vector3.up * 1.9f;
            wall.transform.localRotation = Quaternion.LookRotation(dir);
            wall.AddComponent<BoxCollider>().size = new Vector3(0.7f, 3.8f, length);
        }

        // Railings on both long sides of an obstacle.
        static void Rails(Transform r, float length, float width)
        {
            float half = width * 0.5f - 0.2f;
            RailRun(r, new Vector3(-half, Top, -length * 0.5f), new Vector3(-half, Top, length * 0.5f));
            RailRun(r, new Vector3(half, Top, -length * 0.5f), new Vector3(half, Top, length * 0.5f));
        }

        // A thick blue pylon with a pink float at the water, holding the course up (for looks only).
        static void Pylon(Vector3 p)
        {
            float yTop = Lift - 0.4f, yBot = -0.5f;
            P(null, PrimitiveType.Cylinder, "Pylon", new Vector3(p.x, (yTop + yBot) * 0.5f, p.z), new Vector3(1.5f, (yTop - yBot) * 0.5f, 1.5f), Blue, "quilt", false);
            P(null, PrimitiveType.Cylinder, "Pylon Float", new Vector3(p.x, 0f, p.z), new Vector3(3.4f, 0.3f, 3.4f), Pink, "quilt", false);
            P(null, PrimitiveType.Cylinder, "Pylon Foam", new Vector3(p.x, 0.03f, p.z), new Vector3(4.6f, 0.02f, 4.6f), Foamy, "plain", false);
        }

        static void PylonAt(Transform r, float x, float z) => Pylon(r.TransformPoint(new Vector3(x, 0f, z)));

        // Pylons along the middle of a stretch, two abreast on wide stretches.
        static void Pylons(Vector3 centre, Vector3 dir, float length, float width)
        {
            int rows = Mathf.Max(2, Mathf.CeilToInt(length / 6f));
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            for (int i = 0; i < rows; i++)
            {
                Vector3 pos = centre + dir * (((i + 0.5f) / rows - 0.5f) * length);
                if (width >= 8f) { Pylon(pos + right * 2.6f); Pylon(pos - right * 2.6f); }
                else Pylon(pos);
            }
        }

        // A mat that gives under your feet. Its top is at topY (local), and it hangs down to the water.
        static GameObject Soft(Transform parent, string name, float x, float z, float w, float l, float topY, Color c, Color tube,
            float dip = 0.25f, float stiffness = 38f, float damping = 3.2f, float bob = 0.04f)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = new Vector3(x, 0f, z);
            float height = topY + 0.6f;
            P(holder.transform, PrimitiveType.Cube, "Mat", new Vector3(0f, (topY - 0.6f) * 0.5f, 0f), new Vector3(w, height, l), c);
            for (int side = -1; side <= 1; side += 2)
                Tube(holder.transform, new Vector3(side * (w * 0.5f - 0.25f), topY - 0.05f, -l * 0.5f + 0.3f), new Vector3(side * (w * 0.5f - 0.25f), topY - 0.05f, l * 0.5f - 0.3f), 0.6f, tube);
            var soft = holder.AddComponent<SoftPlatform>();
            soft.dip = dip; soft.stiffness = stiffness; soft.damping = damping; soft.idleBob = bob;
            return holder;
        }

        // ------------------------------------------------------------------ start and finish platforms

        static void StartPlatform(Vector3 centre, Vector3 dir)
        {
            var root = new GameObject("Start Platform").transform;
            root.SetPositionAndRotation(centre, Quaternion.LookRotation(dir));
            float w = 18f, l = 24f;
            Platform(root, "Start Platform", 0f, 0f, w, l);
            RailRun(root, new Vector3(-w * 0.5f + 0.2f, Top, -l * 0.5f), new Vector3(-w * 0.5f + 0.2f, Top, l * 0.5f));
            RailRun(root, new Vector3(w * 0.5f - 0.2f, Top, -l * 0.5f), new Vector3(w * 0.5f - 0.2f, Top, l * 0.5f));
            RailRun(root, new Vector3(-w * 0.5f, Top, -l * 0.5f + 0.2f), new Vector3(w * 0.5f, Top, -l * 0.5f + 0.2f));
            // The way out to the first obstacle is only 3.6 m wide: close the rest of the east edge.
            RailRun(root, new Vector3(-w * 0.5f, Top, l * 0.5f - 0.2f), new Vector3(-2.2f, Top, l * 0.5f - 0.2f));
            RailRun(root, new Vector3(2.2f, Top, l * 0.5f - 0.2f), new Vector3(w * 0.5f, Top, l * 0.5f - 0.2f));
            CourseScenery.Arch("Start Arch", centre + dir * (l * 0.5f - 3f), false, Quaternion.LookRotation(dir));
        }

        static void FinishPlatform(Vector3 centre, Vector3 dir)
        {
            var root = new GameObject("Finish Platform").transform;
            root.SetPositionAndRotation(centre, Quaternion.LookRotation(dir));
            float w = _layout.arenaSize.z, l = _layout.arenaSize.x;
            Platform(root, "Finish Platform", 0f, 0f, w, l);
            // Checkered finish line.
            for (int i = 0; i < 16; i++)
                for (int row = 0; row < 2; row++)
                    P(root, PrimitiveType.Cube, "Tile", new Vector3(-8f + i - 0.5f + 0.5f, Top + 0.07f, -l * 0.5f + 3f + row - 0.5f), new Vector3(1f, 0.02f, 1f),
                        (i + row) % 2 == 0 ? Color.white : new Color(0.08f, 0.08f, 0.1f), "plain", false);
            CourseScenery.Arch("Finish Arch", centre - dir * (l * 0.5f - 4f), true, Quaternion.LookRotation(dir));
        }

        // ------------------------------------------------------------------ the obstacles (local: x right, z forward, deck top at y 0.6)

        // 2. Wobble bridge: a narrow chain of soft planks that sink and sway under your feet.
        static void Obs2WobbleBridge(Transform r, float L, float W)
        {
            Color[] colors = { Yellow, Orange, Pink, Teal };
            int n = 8;
            for (int i = 0; i < n; i += 3) PylonAt(r, 0f, -L * 0.5f + 1f + i * 2f);
            for (int i = 0; i < n; i++)
                Soft(r, "Bridge Plank " + i, 0f, -L * 0.5f + 1f + i * 2f, W, 1.9f, Top, colors[i % colors.Length], White, 0.55f, 17f, 0.8f, 0.1f);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f + 0.5f), 1.4f, -L * 0.5f), new Vector3(side * (W * 0.5f + 0.5f), 1.4f, L * 0.5f), 0.55f, Pink);
        }

        // 3. A curved inflatable tunnel of rings, with a see-through skin.
        static void Obs3Tunnel(Transform r, float L, float W)
        {
            float[] yaws = { 0f, 13f, -13f, 0f };
            float len = L / 4f;
            Vector3 p = new Vector3(0f, 0f, -L * 0.5f);
            Quaternion previous = Quaternion.identity;
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0f, yaws[i], 0f);
                Vector3 centre = p + q * Vector3.forward * (len * 0.5f);
                var piece = new GameObject("Tunnel Piece " + i).transform;
                piece.SetParent(r, false);
                piece.localPosition = centre;
                piece.localRotation = q;

                P(piece, PrimitiveType.Cube, "Deck", new Vector3(0f, 0f, 0f), new Vector3(W, 1.2f, len + 0.3f), Pink);
                P(piece, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.62f, 0f), new Vector3(W - 1.2f, 0.05f, len + 0.3f), Purple, "plain", false);
                MeshKit.Make("Tunnel Skin", MeshKit.Tunnel(W * 0.5f - 0.4f, 3.4f, len + 0.2f, 0.2f), Glass, "glass", false, piece, new Vector3(0f, Top, 0f), Quaternion.identity, Vector3.one);
                for (int k = 0; k < 2; k++)
                    MeshKit.Make("Tunnel Ring", _ringMesh, Color.Lerp(White, Blue, 0.35f), "quilt", false, piece, new Vector3(0f, Top, -len * 0.25f + k * len * 0.5f), Quaternion.identity, new Vector3(0.92f, 1.12f, 1f));
                for (int side = -1; side <= 1; side += 2)
                {
                    var wall = new GameObject("Tunnel Wall");
                    wall.transform.SetParent(piece, false);
                    wall.transform.localPosition = new Vector3(side * (W * 0.5f - 0.5f), Top + 1.6f, 0f);
                    wall.AddComponent<BoxCollider>().size = new Vector3(0.6f, 3.4f, len + 0.3f);
                }
                p = centre + q * Vector3.forward * (len * 0.5f);
                previous = q;
            }
            Foam(r, Vector3.zero, W, L);
        }

        // 4. Bouncing pillars to weave between.
        static void Obs4Pillars(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Orange, Teal, Pink, Yellow, Green, Blue };
            float[,] spots = { { -2.6f, -5.6f }, { 1.0f, -5.6f }, { 3.6f, -5.6f }, { -3.6f, -2.4f }, { -0.2f, -2.4f }, { 3.0f, -2.4f }, { -2.8f, 0.8f }, { 0.6f, 0.8f }, { 3.8f, 0.8f }, { -3.4f, 4.0f }, { -0.4f, 4.0f }, { 2.4f, 4.0f } };
            for (int i = 0; i < spots.GetLength(0); i++)
            {
                var pillar = P(r, PrimitiveType.Capsule, "Pillar", new Vector3(spots[i, 0], Top + 1.6f, spots[i, 1]), new Vector3(1.5f, 1.6f, 1.5f), colors[i % colors.Length], "quilt");
                var bob = pillar.AddComponent<Bobber>();
                bob.amplitude = 0.18f; bob.speed = 2.2f; bob.phase = i * 0.8f;
                P(r, PrimitiveType.Cylinder, "Pillar Base", new Vector3(spots[i, 0], Top + 0.1f, spots[i, 1]), new Vector3(2.0f, 0.1f, 2.0f), White, "quilt", false);
            }
        }

        // 5. A sloped climbing wall with big handholds, a platform on top, and a slope down.
        static void Obs5ClimbingWall(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            float topY = Top + 3.0f;
            var mesh = MeshKit.HeightPad("ClimbWall", W - 1.2f, L - 0.4f, (x, z) =>
            {
                if (z < -3.2f) return Top;
                if (z < 0.3f) return Mathf.Lerp(Top, topY, (z + 3.2f) / 3.5f);
                if (z < 2.2f) return topY;
                if (z < 5.2f) return Mathf.Lerp(topY, Top, (z - 2.2f) / 3f);
                return Top;
            }, -0.6f);
            MeshKit.Make("Climbing Wall", mesh, Orange, "plain", true, r, Vector3.zero, Quaternion.identity, Vector3.one);
            Color[] holds = { Red, Green, Blue, Yellow, Teal };
            for (int i = 0; i < 12; i++)
            {
                float x = -3.2f + (i % 4) * 2.1f;
                float z = -2.8f + (i / 4) * 1.15f;
                float y = Top + 3.0f * (z + 3.2f) / 3.5f + 0.2f;
                P(r, PrimitiveType.Sphere, "Handhold", new Vector3(x, y, z), new Vector3(0.7f, 0.5f, 0.7f), holds[i % holds.Length], "plain", false);
            }
        }

        // 6. Floating logs that roll under your feet.
        static void Obs6Logs(Transform r, float L, float W)
        {
            Rails(r, L, W);
            Color[] colors = { Orange, Yellow };
            for (int i = 0; i < 5; i++)
            {
                var holder = new GameObject("Floating Log " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, -0.25f, -4.4f + i * 2.2f);
                if (i % 2 == 0) { PylonAt(r, -W * 0.5f + 0.4f, -4.4f + i * 2.2f); PylonAt(r, W * 0.5f - 0.4f, -4.4f + i * 2.2f); }
                var visual = P(holder.transform, PrimitiveType.Cylinder, "Log", Vector3.zero, new Vector3(1.7f, (W - 1f) * 0.5f, 1.7f), colors[i % 2], "quilt", true, new Vector3(0f, 0f, 90f));
                P(holder.transform, PrimitiveType.Cylinder, "Stripe", Vector3.zero, new Vector3(1.74f, 0.4f, 1.74f), White, "plain", false, new Vector3(0f, 0f, 90f));
                var log = holder.AddComponent<RollingLog>();
                log.visual = visual.transform;
                log.radius = 0.85f;
                log.surfaceSpeed = (i % 2 == 0 ? -1f : 1f) * 2.5f;
            }
        }

        // 7. A big round platform that turns slowly: step on at one end, step off at the other.
        static void Obs7RotatingDisc(Transform r, float L, float W)
        {
            Platform(r, "Entry Deck", 0f, -L * 0.5f + 1.2f, W, 2.4f);
            Platform(r, "Exit Deck", 0f, L * 0.5f - 1.2f, W, 2.4f);
            Rails(r, L, W);

            var disc = new GameObject("Rotating Disc");
            disc.transform.SetParent(r, false);
            disc.transform.localPosition = Vector3.zero;
            P(disc.transform, PrimitiveType.Cylinder, "Disc", Vector3.zero, new Vector3(11.4f, 0.6f, 11.4f), Pink);
            Color[] wedges = { Yellow, Teal, Orange, Green, Blue, Purple };
            for (int i = 0; i < 6; i++)
            {
                var wedge = P(disc.transform, PrimitiveType.Cube, "Wedge", new Vector3(0f, 0.61f, 0f), new Vector3(5.3f, 0.05f, 1.5f), wedges[i], "plain", false);
                wedge.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                wedge.transform.localPosition = Quaternion.Euler(0f, i * 60f, 0f) * new Vector3(0f, 0.61f, 2.9f);
            }
            P(disc.transform, PrimitiveType.Cylinder, "Hub", new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 0.4f, 1.2f), Yellow, "quilt", false);
            Tube(disc.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0f, 2.6f, 0f), 0.6f, Red);
            var spin = disc.AddComponent<SpinningPlatform>();
            spin.degreesPerSecond = 36f;
            spin.radius = 5.7f;
        }

        // 8. A big trampoline that throws you up and forward.
        static void Obs8Trampoline(Transform r, float L, float W)
        {
            Platform(r, "Entry Deck", 0f, -L * 0.5f + 1.2f, W, 2.4f);
            Platform(r, "Exit Deck", 0f, L * 0.5f - 1.2f, W, 2.4f);
            Rails(r, L, W);
            var mat = Soft(r, "Trampoline", 0f, 0f, W - 1.2f, L - 4.6f, Top, Teal, Yellow, 0.35f, 24f, 1.4f, 0.05f);
            var pad = new GameObject("Bounce Trigger");
            pad.transform.SetParent(r, false);
            pad.transform.localPosition = new Vector3(0f, Top + 0.15f, 0f);
            var box = pad.AddComponent<BoxCollider>();
            box.size = new Vector3(W - 2f, 0.3f, L - 5.6f);
            box.isTrigger = true;
            pad.AddComponent<BouncePad>().bounceVelocity = 15f;
            P(r, PrimitiveType.Cylinder, "Target", new Vector3(0f, Top + 0.1f, 0f), new Vector3(5f, 0.04f, 5f), Pink, "plain", false);
            P(r, PrimitiveType.Cylinder, "Target Centre", new Vector3(0f, Top + 0.12f, 0f), new Vector3(2.4f, 0.04f, 2.4f), Yellow, "plain", false);
        }

        // 9. Swinging inflatable balls on ropes.
        static void Obs9Pendulums(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Red, Yellow, Teal };
            for (int i = 0; i < 4; i++)
            {
                float z = -6f + i * 4f;
                var pivot = new GameObject("Pendulum " + i);
                pivot.transform.SetParent(r, false);
                pivot.transform.localPosition = new Vector3(0f, Top + 6.2f, z);

                P(pivot.transform, PrimitiveType.Cylinder, "Rope", new Vector3(0f, -2.1f, 0f), new Vector3(0.15f, 2.1f, 0.15f), new Color(0.9f, 0.85f, 0.65f), "plain", false);
                var ball = P(pivot.transform, PrimitiveType.Sphere, "Swinging Ball", new Vector3(0f, -5.0f, 0f), Vector3.one * 2.2f, colors[i % colors.Length], "quilt", false);
                P(pivot.transform, PrimitiveType.Cylinder, "Stripe", new Vector3(0f, -5.0f, 0f), new Vector3(2.23f, 0.05f, 2.23f), White, "plain", false);

                var swing = pivot.AddComponent<Pendulum>();
                swing.ball = ball.transform;
                swing.ballRadius = 1.1f;
                swing.maxAngle = 55f;
                swing.period = 2.9f;
                swing.phase = i * 1.6f;
            }
            // Tall posts and a beam above each ball, so the ropes have something to hang from.
            for (int i = 0; i < 4; i++)
            {
                float z = -6f + i * 4f;
                for (int side = -1; side <= 1; side += 2)
                    Tube(r, new Vector3(side * (W * 0.5f - 0.6f), Top, z), new Vector3(side * (W * 0.5f - 0.6f), Top + 6.6f, z), 0.9f, Orange);
                Tube(r, new Vector3(-W * 0.5f + 0.6f, Top + 6.5f, z), new Vector3(W * 0.5f - 0.6f, Top + 6.5f, z), 0.9f, Orange);
            }
        }

        // 10. A slippery slide: climb the soft slope, then ride the slide down.
        static void Obs10Slide(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            float topY = Top + 3.4f;
            var mesh = MeshKit.HeightPad("SlideHill", W - 1.2f, 7f, (x, z) => z < 1.8f ? Mathf.Lerp(Top, topY, (z + 3.5f) / 5.3f) : topY, -0.6f);
            MeshKit.Make("Slide Hill", mesh, Blue, "plain", true, r, new Vector3(0f, 0f, -3.5f), Quaternion.identity, Vector3.one);

            // The slide itself, from the top of the hill down to the end of this deck.
            var path = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                float x = Mathf.Sin(t * Mathf.PI * 2f) * 2.2f;
                path[i] = r.TransformPoint(new Vector3(x, Mathf.Lerp(topY + 0.5f, Top + 0.5f, Mathf.SmoothStep(0f, 1f, t)), Mathf.Lerp(-0.4f, L * 0.5f - 0.6f, t)));
            }
            CourseExtras.BuildSlideFromPath(path, "A");
        }

        // 11. Balance section: three narrow planks, each doing something different.
        static void Obs11Balance(Transform r, float L, float W)
        {
            Soft(r, "Wobble 1", 0f, -4f, W, 3.8f, Top, Orange, White, 0.5f, 16f, 0.9f, 0.06f);

            var sway = new GameObject("Swaying Plank");
            sway.transform.SetParent(r, false);
            var box = sway.AddComponent<BoxCollider>();
            box.size = new Vector3(W, 1.2f, 3.8f);
            P(sway.transform, PrimitiveType.Capsule, "Plank", new Vector3(0f, -0.05f, 0f), new Vector3(W + 0.1f, 1.9f, 1.3f), Yellow, "quilt", false, new Vector3(90f, 0f, 0f));
            var platform = sway.AddComponent<MovingPlatform>();
            platform.pointA = r.position - r.right * 0.9f;
            platform.pointB = r.position + r.right * 0.9f;
            platform.period = 2.6f;

            Soft(r, "Wobble 2", 0f, 4f, W, 3.8f, Top, Pink, White, 0.5f, 16f, 0.9f, 0.06f);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f + 0.4f), 1.2f, -L * 0.5f), new Vector3(side * (W * 0.5f + 0.4f), 1.2f, L * 0.5f), 0.5f, Teal);
        }

        // 12. Padded arms turning round a centre pillar.
        static void Obs12PaddedArms(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);

            var holder = new GameObject("Padded Arms");
            holder.transform.SetParent(r, false);
            holder.transform.localPosition = new Vector3(0f, Top + 0.8f, 0f);
            var armA = P(holder.transform, PrimitiveType.Capsule, "Arm A", Vector3.zero, new Vector3(1.0f, 4.4f, 1.0f), Orange, "quilt", false, new Vector3(0f, 0f, 90f));
            var armB = P(holder.transform, PrimitiveType.Capsule, "Arm B", Vector3.zero, new Vector3(1.0f, 4.4f, 1.0f), Pink, "quilt", false, new Vector3(90f, 0f, 0f));
            P(holder.transform, PrimitiveType.Cylinder, "Centre", new Vector3(0f, -0.2f, 0f), new Vector3(1.8f, 1.3f, 1.8f), Yellow, "quilt", false);
            var spin = holder.AddComponent<RotatingBar>();
            spin.degreesPerSecond = 58f;
            spin.hitBoxes = new[] { armA.transform, armB.transform };
            P(r, PrimitiveType.Cylinder, "Pillar", new Vector3(0f, Top + 1.0f, 0f), new Vector3(1.4f, 1.0f, 1.4f), Blue, "quilt", true);
        }

        // 13. A maze of rounded arches: each wall has one doorway, and the doorways alternate left and right.
        static void Obs13ArchMaze(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Yellow, Pink, Teal, Orange };
            float half = W * 0.5f - 0.7f;
            for (int i = 0; i < 4; i++)
            {
                float z = -5.4f + i * 3.6f;
                float open = (i % 2 == 0 ? -1f : 1f) * 2.4f;     // where the doorway is
                float doorHalf = 1.5f;
                Color c = colors[i];

                float leftEdge = -half, rightEdge = half;
                float leftWidth = (open - doorHalf) - leftEdge, rightWidth = rightEdge - (open + doorHalf);
                P(r, PrimitiveType.Cube, "Arch Wall", new Vector3(leftEdge + leftWidth * 0.5f, Top + 2.2f, z), new Vector3(leftWidth, 4.4f, 1.2f), c, "quilt");
                P(r, PrimitiveType.Cube, "Arch Wall", new Vector3(rightEdge - rightWidth * 0.5f, Top + 2.2f, z), new Vector3(rightWidth, 4.4f, 1.2f), c, "quilt");
                P(r, PrimitiveType.Cube, "Arch Top", new Vector3(open, Top + 3.5f, z), new Vector3(doorHalf * 2f, 1.9f, 1.2f), c, "quilt");
                MeshKit.Make("Arch Ring", _ringMesh, White, "quilt", false, r, new Vector3(open, Top + 1.5f, z - 0.62f), Quaternion.identity, new Vector3(0.68f, 0.95f, 1f));
            }
        }

        // 14. The final bridge: long, soft and wavy, with bouncy planks.
        static void Obs14FinalBridge(Transform r, float L, float W)
        {
            Color[] colors = { Orange, Yellow, Pink, Teal, Green };
            int n = 15;
            for (int i = 0; i < n; i++)
            {
                float z = -L * 0.5f + 1f + i * 2f;
                if (i % 3 == 0) PylonAt(r, 0f, z);
                var plank = Soft(r, "Final Plank " + i, 0f, z, W, 1.9f, Top, colors[i % colors.Length], White, 0.55f, 14f, 0.6f, 0.42f);
                if (i % 5 == 2)
                {
                    var pad = new GameObject("Bounce Trigger");
                    pad.transform.SetParent(r, false);
                    pad.transform.localPosition = new Vector3(0f, Top + 0.15f, z);
                    var box = pad.AddComponent<BoxCollider>();
                    box.size = new Vector3(W - 0.4f, 0.3f, 1.7f);
                    box.isTrigger = true;
                    pad.AddComponent<BouncePad>().bounceVelocity = 11f;
                    P(r, PrimitiveType.Cube, "Bounce Arrow", new Vector3(0f, Top + 0.62f, z), new Vector3(W - 1f, 0.03f, 1.0f), Red, "plain", false);
                }
            }
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f + 0.6f), 1.5f, -L * 0.5f), new Vector3(side * (W * 0.5f + 0.6f), 1.5f, L * 0.5f), 0.6f, Pink);
        }

        // ------------------------------------------------------------------ the harder lanes (A and B)

        // A1. Small, widely spaced pads in a zigzag: the bounce makes them hard to land on.
        static void ObsA1ZigzagPads(Transform r, float L, float W)
        {
            Color[] colors = { Orange, Yellow, Pink, Teal, Green, Blue };
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float z = -L * 0.5f + 1.4f + i * (L - 2.8f) / (n - 1);
                float x = (i % 2 == 0 ? -1f : 1f) * 2.9f;
                float d = 2.5f;
                var holder = new GameObject("Zigzag Pad " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(x, 0f, z);
                PylonAt(r, x, z);
                P(holder.transform, PrimitiveType.Cylinder, "Pad", Vector3.zero, new Vector3(d, 0.6f, d), colors[i % colors.Length]);
                P(holder.transform, PrimitiveType.Cylinder, "Foam", new Vector3(0f, 0.03f, 0f), new Vector3(d + 1.2f, 0.02f, d + 1.2f), Foamy, "plain", false);
                var soft = holder.AddComponent<SoftPlatform>();
                soft.dip = 0.45f; soft.stiffness = 24f; soft.damping = 1.6f;
            }
        }

        // A2. A deck with two low sweepers turning opposite ways, and a high one you must go under or time.
        static void ObsA2Sweepers(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            float[] zs = { -4.2f, 0f, 4.2f };
            float[] speeds = { 70f, -90f, 70f };
            Color[] colors = { Yellow, Pink, Teal };
            for (int i = 0; i < 3; i++)
            {
                var holder = new GameObject("Sweeper " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, Top + 0.55f, zs[i]);
                var arm = P(holder.transform, PrimitiveType.Capsule, "Arm", Vector3.zero, new Vector3(0.9f, 4.2f, 0.9f), colors[i], "quilt", false, new Vector3(0f, 0f, 90f));
                P(holder.transform, PrimitiveType.Cylinder, "Hub", new Vector3(0f, -0.2f, 0f), new Vector3(1.4f, 0.6f, 1.4f), Blue, "quilt", false);
                var spin = holder.AddComponent<RotatingBar>();
                spin.degreesPerSecond = speeds[i];
                spin.hitBoxes = new[] { arm.transform };
            }
        }

        // A3. A ferry: the deck slides across an open gap; step on at one side and off at the other.
        static void ObsA3Ferry(Transform r, float L, float W)
        {
            var holder = new GameObject("Ferry");
            holder.transform.SetParent(r, false);
            holder.transform.localPosition = new Vector3(0f, 0f, -L * 0.5f + 3f);
            var box = holder.AddComponent<BoxCollider>();
            box.size = new Vector3(W - 1f, 1.2f, 6f);
            P(holder.transform, PrimitiveType.Cube, "Ferry Deck", Vector3.zero, new Vector3(W - 1f, 1.2f, 6f), Orange, "plain", false);
            P(holder.transform, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.62f, 0f), new Vector3(W - 2.2f, 0.05f, 4.8f), Yellow, "plain", false);
            for (int side = -1; side <= 1; side += 2)
                Tube(holder.transform, new Vector3(side * (W * 0.5f - 1.0f), 0.75f, -2.7f), new Vector3(side * (W * 0.5f - 1.0f), 0.75f, 2.7f), 0.7f, White);
            PylonAt(r, -W * 0.5f + 0.6f, -L * 0.5f + 0.6f); PylonAt(r, W * 0.5f - 0.6f, -L * 0.5f + 0.6f); PylonAt(r, -W * 0.5f + 0.6f, L * 0.5f - 0.6f); PylonAt(r, W * 0.5f - 0.6f, L * 0.5f - 0.6f);
            var platform = holder.AddComponent<MovingPlatform>();
            platform.pointA = r.TransformPoint(new Vector3(0f, 0f, -L * 0.5f + 3f));
            platform.pointB = r.TransformPoint(new Vector3(0f, 0f, L * 0.5f - 3f));
            platform.period = 6f;
            Foam(r, Vector3.zero, W, L);
        }

        // A4. Springy mattresses with gaps between them.
        static void ObsA4Mattresses(Transform r, float L, float W)
        {
            Color[] colors = { Pink, Teal, Yellow };
            for (int i = 0; i < 3; i++)
                Soft(r, "Springy Mattress " + i, 0f, -L * 0.5f + 2.1f + i * 4.9f, W - 1f, 3.9f, Top + 0.2f, colors[i], Orange, 0.6f, 20f, 1.0f, 0.08f);
        }

        // A5. A long, narrow beam over open water, swaying from side to side.
        static void ObsA5NarrowBeam(Transform r, float L, float W)
        {
            var holder = new GameObject("Narrow Beam");
            holder.transform.SetParent(r, false);
            holder.transform.localPosition = Vector3.zero;
            var box = holder.AddComponent<BoxCollider>();
            box.size = new Vector3(1.0f, 1.2f, L);
            P(holder.transform, PrimitiveType.Capsule, "Beam", new Vector3(0f, -0.05f, 0f), new Vector3(1.1f, L * 0.5f, 1.3f), Pink, "quilt", false, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 6; i++)
                P(holder.transform, PrimitiveType.Cylinder, "Band", new Vector3(0f, -0.05f, -L * 0.42f + i * L * 0.168f), new Vector3(1.18f, 0.12f, 1.38f), White, "plain", false, new Vector3(90f, 0f, 0f));
            PylonAt(r, 0f, -L * 0.5f + 1f); PylonAt(r, 0f, 0f); PylonAt(r, 0f, L * 0.5f - 1f);
            var platform = holder.AddComponent<MovingPlatform>();
            platform.pointA = r.position - r.right * 1.0f;
            platform.pointB = r.position + r.right * 1.0f;
            platform.period = 2.3f;
        }

        // B1. Two rotating discs one after the other, turning opposite ways.
        static void ObsB1TwoDiscs(Transform r, float L, float W)
        {
            Platform(r, "Entry Deck", 0f, -L * 0.5f + 0.6f, W, 1.2f);
            Platform(r, "Exit Deck", 0f, L * 0.5f - 0.6f, W, 1.2f);
            Rails(r, L, W);
            for (int k = 0; k < 2; k++)
            {
                var disc = new GameObject("Rotating Disc " + k);
                disc.transform.SetParent(r, false);
                disc.transform.localPosition = new Vector3(0f, 0f, (k == 0 ? -1f : 1f) * 3.65f);
                P(disc.transform, PrimitiveType.Cylinder, "Disc", Vector3.zero, new Vector3(7.0f, 0.6f, 7.0f), k == 0 ? Teal : Orange);
                Color[] wedges = { Yellow, Pink, Green, Blue };
                for (int i = 0; i < 4; i++)
                {
                    var wedge = P(disc.transform, PrimitiveType.Cube, "Wedge", Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0f, 0.61f, 1.7f), new Vector3(1.2f, 0.05f, 3.2f), wedges[i], "plain", false);
                    wedge.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                }
                var spin = disc.AddComponent<SpinningPlatform>();
                spin.degreesPerSecond = k == 0 ? 38f : -46f;
                spin.radius = 3.4f;
            }
        }

        // B2. A gauntlet of fast logs with a swinging ball at the end.
        static void ObsB2Logs(Transform r, float L, float W)
        {
            Rails(r, L, W);
            Color[] colors = { Orange, Yellow };
            for (int i = 0; i < 6; i++)
            {
                var holder = new GameObject("Gauntlet Log " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, -0.25f, -6.8f + i * 2.2f);
                if (i % 2 == 0) { PylonAt(r, -W * 0.5f + 0.4f, -6.8f + i * 2.2f); PylonAt(r, W * 0.5f - 0.4f, -6.8f + i * 2.2f); }
                var visual = P(holder.transform, PrimitiveType.Cylinder, "Log", Vector3.zero, new Vector3(1.7f, (W - 1f) * 0.5f, 1.7f), colors[i % 2], "quilt", true, new Vector3(0f, 0f, 90f));
                P(holder.transform, PrimitiveType.Cylinder, "Stripe", Vector3.zero, new Vector3(1.74f, 0.4f, 1.74f), White, "plain", false, new Vector3(0f, 0f, 90f));
                var log = holder.AddComponent<RollingLog>();
                log.visual = visual.transform;
                log.radius = 0.85f;
                log.surfaceSpeed = (i % 2 == 0 ? -1f : 1f) * (2.6f + i * 0.25f);
            }
            Platform(r, "Landing", 0f, L * 0.5f - 1.1f, W, 2.2f);
            var pivot = new GameObject("Gauntlet Pendulum");
            pivot.transform.SetParent(r, false);
            pivot.transform.localPosition = new Vector3(0f, Top + 6.2f, L * 0.5f - 1.1f);
            P(pivot.transform, PrimitiveType.Cylinder, "Rope", new Vector3(0f, -2.1f, 0f), new Vector3(0.15f, 2.1f, 0.15f), new Color(0.9f, 0.85f, 0.65f), "plain", false);
            var ball = P(pivot.transform, PrimitiveType.Sphere, "Swinging Ball", new Vector3(0f, -5.0f, 0f), Vector3.one * 2.4f, Red, "quilt", false);
            var swing = pivot.AddComponent<Pendulum>();
            swing.ball = ball.transform; swing.ballRadius = 1.2f; swing.maxAngle = 55f; swing.period = 2.5f;
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.6f), Top, L * 0.5f - 1.1f), new Vector3(side * (W * 0.5f - 0.6f), Top + 6.6f, L * 0.5f - 1.1f), 0.9f, Orange);
            Tube(r, new Vector3(-W * 0.5f + 0.6f, Top + 6.5f, L * 0.5f - 1.1f), new Vector3(W * 0.5f - 0.6f, Top + 6.5f, L * 0.5f - 1.1f), 0.9f, Orange);
        }

        // B3. A field of big balls with a sweeper hidden among them.
        static void ObsB3Balls(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Red, Yellow, Green, Pink, Orange, Teal, Red };
            float[,] spots = { { -3.4f, -5.2f, 2.6f }, { 1.6f, -4.4f, 2.8f }, { -1.8f, -1.0f, 3.0f }, { 3.2f, 0.4f, 2.6f }, { -3.2f, 2.6f, 2.8f }, { 1.2f, 3.8f, 2.6f }, { 3.6f, 5.4f, 2.2f } };
            for (int i = 0; i < spots.GetLength(0); i++)
            {
                float d = spots[i, 2];
                var ball = P(r, PrimitiveType.Sphere, "Giant Ball", new Vector3(spots[i, 0], Top + d * 0.5f - 0.1f, spots[i, 1]), Vector3.one * d, colors[i % colors.Length], "quilt");
                P(r, PrimitiveType.Cylinder, "Stripe", ball.transform.localPosition, new Vector3(d * 1.003f, 0.05f, d * 1.003f), White, "plain", false);
            }
            var holder = new GameObject("Hidden Sweeper");
            holder.transform.SetParent(r, false);
            holder.transform.localPosition = new Vector3(0f, Top + 0.5f, 0.6f);
            var arm = P(holder.transform, PrimitiveType.Capsule, "Arm", Vector3.zero, new Vector3(0.8f, 2.6f, 0.8f), Purple, "quilt", false, new Vector3(0f, 0f, 90f));
            var spin = holder.AddComponent<RotatingBar>();
            spin.degreesPerSecond = 80f;
            spin.hitBoxes = new[] { arm.transform };
        }

        // B4. A steep climb and a long, winding slide down.
        static void ObsB4Slide(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            float topY = Top + 3.8f;
            var mesh = MeshKit.HeightPad("SteepHill", W - 1.2f, 7f, (x, z) => z < 1.5f ? Mathf.Lerp(Top, topY, (z + 3.5f) / 5f) : topY, -0.6f);
            MeshKit.Make("Steep Hill", mesh, Green, "plain", true, r, new Vector3(0f, 0f, -3.5f), Quaternion.identity, Vector3.one);
            var path = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float x = Mathf.Sin(t * Mathf.PI * 3f) * 3.0f;
                path[i] = r.TransformPoint(new Vector3(x, Mathf.Lerp(topY + 0.5f, Top + 0.5f, Mathf.SmoothStep(0f, 1f, t)), Mathf.Lerp(-0.4f, L * 0.5f - 0.6f, t)));
            }
            CourseExtras.BuildSlideFromPath(path, "B");
        }

        // B5. A line of small soft steps over the water.
        static void ObsB5Steps(Transform r, float L, float W)
        {
            Color[] colors = { Orange, Teal, Yellow, Pink, Green };
            for (int i = 0; i < 5; i++)
            {
                float z = -L * 0.5f + 1.2f + i * 2.4f;
                var holder = new GameObject("Wobble Step " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3((i % 2 == 0 ? -1f : 1f) * 0.6f, 0f, z);
                PylonAt(r, (i % 2 == 0 ? -1f : 1f) * 0.6f, z);
                P(holder.transform, PrimitiveType.Cylinder, "Step", Vector3.zero, new Vector3(2.2f, 0.6f, 2.2f), colors[i]);
                var soft = holder.AddComponent<SoftPlatform>();
                soft.dip = 0.5f; soft.stiffness = 18f; soft.damping = 1.0f; soft.idleBob = 0.1f;
            }
        }

        // ------------------------------------------------------------------ the far lanes (C and D): the hardest

        // C1. A big rotating cross close to the floor, with a second one spinning the other way behind it.
        static void ObsC1Cross(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            for (int k = 0; k < 2; k++)
            {
                var holder = new GameObject("Cross " + k);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, Top + 0.55f, k == 0 ? -3.4f : 3.4f);
                var a = P(holder.transform, PrimitiveType.Capsule, "Arm A", Vector3.zero, new Vector3(0.9f, 4.4f, 0.9f), k == 0 ? Orange : Teal, "quilt", false, new Vector3(0f, 0f, 90f));
                var b = P(holder.transform, PrimitiveType.Capsule, "Arm B", Vector3.zero, new Vector3(0.9f, 4.4f, 0.9f), k == 0 ? Pink : Yellow, "quilt", false, new Vector3(90f, 0f, 0f));
                P(holder.transform, PrimitiveType.Cylinder, "Hub", new Vector3(0f, -0.2f, 0f), new Vector3(1.5f, 0.6f, 1.5f), Blue, "quilt", false);
                var spin = holder.AddComponent<RotatingBar>();
                spin.degreesPerSecond = k == 0 ? 75f : -95f;
                spin.hitBoxes = new[] { a.transform, b.transform };
            }
        }

        // C2. A corridor of five fast pendulums, out of step with each other.
        static void ObsC2Pendulums(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Red, Yellow, Teal, Pink, Orange };
            for (int i = 0; i < 5; i++)
            {
                float z = -6.4f + i * 3.2f;
                var pivot = new GameObject("Pendulum " + i);
                pivot.transform.SetParent(r, false);
                pivot.transform.localPosition = new Vector3(0f, Top + 6.2f, z);
                P(pivot.transform, PrimitiveType.Cylinder, "Rope", new Vector3(0f, -2.1f, 0f), new Vector3(0.15f, 2.1f, 0.15f), new Color(0.9f, 0.85f, 0.65f), "plain", false);
                var ball = P(pivot.transform, PrimitiveType.Sphere, "Swinging Ball", new Vector3(0f, -5.0f, 0f), Vector3.one * 2.2f, colors[i], "quilt", false);
                var swing = pivot.AddComponent<Pendulum>();
                swing.ball = ball.transform; swing.ballRadius = 1.1f; swing.maxAngle = 58f; swing.period = 2.4f; swing.phase = i * 2.1f;
                for (int side = -1; side <= 1; side += 2)
                    Tube(r, new Vector3(side * (W * 0.5f - 0.6f), Top, z), new Vector3(side * (W * 0.5f - 0.6f), Top + 6.6f, z), 0.9f, Orange);
                Tube(r, new Vector3(-W * 0.5f + 0.6f, Top + 6.5f, z), new Vector3(W * 0.5f - 0.6f, Top + 6.5f, z), 0.9f, Orange);
            }
        }

        // C3. Planks that give way a moment after you stand on them.
        static void ObsC3Crumbling(Transform r, float L, float W)
        {
            Color[] colors = { Yellow, Orange, Pink, Teal, Green, Blue };
            for (int i = 0; i < 6; i++)
            {
                float z = -L * 0.5f + 1.2f + i * 2.32f;
                var holder = new GameObject("Crumbling Plank " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3((i % 2 == 0 ? -1f : 1f) * 1.2f, 0f, z);
                P(holder.transform, PrimitiveType.Cube, "Plank", Vector3.zero, new Vector3(5.2f, 1.2f, 2.0f), colors[i]);
                for (int side = -1; side <= 1; side += 2)
                    Tube(holder.transform, new Vector3(side * 2.4f, 0.55f, -0.8f), new Vector3(side * 2.4f, 0.55f, 0.8f), 0.55f, White);
                holder.AddComponent<CrumblingPlatform>();
                PylonAt(r, (i % 2 == 0 ? -1f : 1f) * 1.2f, z);
            }
        }

        // C4. Padded walls sliding across the lane: wait for the gap.
        static void ObsC4SlidingWalls(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            Color[] colors = { Red, Yellow, Pink };
            for (int i = 0; i < 3; i++)
            {
                var holder = new GameObject("Sliding Wall " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, Top + 1.5f, -4.4f + i * 4.4f);
                var box = holder.AddComponent<BoxCollider>();
                box.size = new Vector3(6.4f, 3f, 1.2f);
                P(holder.transform, PrimitiveType.Cube, "Wall", Vector3.zero, new Vector3(6.4f, 3f, 1.2f), colors[i], "quilt", false);
                Tube(holder.transform, new Vector3(-3.0f, 1.5f, 0f), new Vector3(3.0f, 1.5f, 0f), 0.8f, White);
                float reach = W * 0.5f - 3.3f;
                var mover = holder.AddComponent<MovingPlatform>();
                mover.pointA = holder.transform.position - r.right * (reach * (i % 2 == 0 ? 1f : -1f));
                mover.pointB = holder.transform.position + r.right * (reach * (i % 2 == 0 ? 1f : -1f));
                mover.period = 3.2f + i * 0.4f;
            }
        }

        // C5. A narrow beam with two low sweepers turning across it.
        static void ObsC5CrossBeam(Transform r, float L, float W)
        {
            var holder = new GameObject("Cross Beam");
            holder.transform.SetParent(r, false);
            var box = holder.AddComponent<BoxCollider>();
            box.size = new Vector3(1.0f, 1.2f, L);
            P(holder.transform, PrimitiveType.Capsule, "Beam", new Vector3(0f, -0.05f, 0f), new Vector3(1.1f, L * 0.5f, 1.3f), Teal, "quilt", false, new Vector3(90f, 0f, 0f));
            PylonAt(r, 0f, -L * 0.5f + 1f); PylonAt(r, 0f, 0f); PylonAt(r, 0f, L * 0.5f - 1f);
            for (int k = 0; k < 2; k++)
            {
                var sweeper = new GameObject("Beam Sweeper " + k);
                sweeper.transform.SetParent(r, false);
                sweeper.transform.localPosition = new Vector3(0f, Top + 0.5f, k == 0 ? -2.6f : 2.6f);
                var arm = P(sweeper.transform, PrimitiveType.Capsule, "Arm", Vector3.zero, new Vector3(0.8f, 2.4f, 0.8f), k == 0 ? Orange : Pink, "quilt", false, new Vector3(0f, 0f, 90f));
                var spin = sweeper.AddComponent<RotatingBar>();
                spin.degreesPerSecond = k == 0 ? 85f : -85f;
                spin.hitBoxes = new[] { arm.transform };
            }
        }

        // D1. Small trampoline pads over open water: bounce from one to the next.
        static void ObsD1BounceChain(Transform r, float L, float W)
        {
            Color[] colors = { Teal, Pink, Yellow, Orange };
            for (int i = 0; i < 4; i++)
            {
                float z = -L * 0.5f + 2.2f + i * 4.2f;
                float x = (i % 2 == 0 ? -1f : 1f) * 1.6f;
                var holder = new GameObject("Bounce Pad " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(x, 0f, z);
                P(holder.transform, PrimitiveType.Cylinder, "Pad", Vector3.zero, new Vector3(3.4f, 0.6f, 3.4f), colors[i]);
                P(holder.transform, PrimitiveType.Cylinder, "Target", new Vector3(0f, 0.62f, 0f), new Vector3(2.0f, 0.03f, 2.0f), White, "plain", false);
                var trigger = new GameObject("Bounce Trigger");
                trigger.transform.SetParent(holder.transform, false);
                trigger.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                var box = trigger.AddComponent<BoxCollider>();
                box.size = new Vector3(2.8f, 0.3f, 2.8f);
                box.isTrigger = true;
                trigger.AddComponent<BouncePad>().bounceVelocity = 16.5f;
                PylonAt(r, x, z);
            }
        }

        // D2. Windmills: big arms turning like clock hands, across the lane.
        static void ObsD2Windmills(Transform r, float L, float W)
        {
            Platform(r, "Deck", 0f, 0f, W, L);
            Rails(r, L, W);
            for (int k = 0; k < 2; k++)
            {
                var hub = new GameObject("Windmill " + k);
                hub.transform.SetParent(r, false);
                hub.transform.localPosition = new Vector3(0f, Top + 3.2f, k == 0 ? -3.2f : 3.2f);
                var a = P(hub.transform, PrimitiveType.Capsule, "Arm A", Vector3.zero, new Vector3(0.7f, 3.4f, 0.7f), k == 0 ? Red : Blue, "quilt", false, new Vector3(0f, 0f, 90f));
                var b = P(hub.transform, PrimitiveType.Capsule, "Arm B", Vector3.zero, new Vector3(0.7f, 3.4f, 0.7f), k == 0 ? Yellow : Pink, "quilt", false);
                P(hub.transform, PrimitiveType.Sphere, "Hub", Vector3.zero, Vector3.one * 1.4f, White, "plain", false);
                Tube(r, new Vector3(0f, Top, hub.transform.localPosition.z), new Vector3(0f, Top + 3.0f, hub.transform.localPosition.z), 0.6f, Orange);
                var spin = hub.AddComponent<RotatingBar>();
                spin.rotationAxis = r.forward;
                spin.degreesPerSecond = k == 0 ? 55f : -70f;
                spin.hitBoxes = new[] { a.transform, b.transform };
            }
        }

        // D3. Two ferries going opposite ways across two gaps.
        static void ObsD3TwinFerries(Transform r, float L, float W)
        {
            Platform(r, "Middle Deck", 0f, 0f, W, 2.6f);
            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? -1f : 1f;
                float zStart = side * (L * 0.5f - 2.6f), zEnd = side * 2.4f; // between the outer end and the middle deck
                var holder = new GameObject("Ferry " + k);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, 0f, zStart);
                var box = holder.AddComponent<BoxCollider>();
                box.size = new Vector3(W - 2f, 1.2f, 4.4f);
                P(holder.transform, PrimitiveType.Cube, "Ferry Deck", Vector3.zero, new Vector3(W - 2f, 1.2f, 4.4f), k == 0 ? Orange : Teal, "plain", false);
                P(holder.transform, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.62f, 0f), new Vector3(W - 3.2f, 0.05f, 3.2f), Yellow, "plain", false);
                var platform = holder.AddComponent<MovingPlatform>();
                platform.pointA = r.TransformPoint(new Vector3(0f, 0f, zStart));
                platform.pointB = r.TransformPoint(new Vector3(0f, 0f, zEnd));
                platform.period = k == 0 ? 5.2f : 6.4f;
            }
            Pylon(r.TransformPoint(new Vector3(2f, 0f, 0f))); Pylon(r.TransformPoint(new Vector3(-2f, 0f, 0f)));
        }

        // D4. Small crumbling stones spaced just far enough apart that you must keep moving.
        static void ObsD4HopStones(Transform r, float L, float W)
        {
            Color[] colors = { Pink, Yellow, Teal, Orange, Green };
            for (int i = 0; i < 5; i++)
            {
                float z = -L * 0.5f + 1.4f + i * 2.9f;
                float x = Mathf.Sin(i * 1.9f) * 2.8f;
                var holder = new GameObject("Hop Stone " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(x, 0f, z);
                P(holder.transform, PrimitiveType.Cylinder, "Stone", Vector3.zero, new Vector3(2.2f, 0.6f, 2.2f), colors[i]);
                var crumble = holder.AddComponent<CrumblingPlatform>();
                crumble.standSeconds = 0.35f;
                PylonAt(r, x, z);
            }
        }

        // D5. A narrow soft bridge with swinging balls across it.
        static void ObsD5PendulumBridge(Transform r, float L, float W)
        {
            for (int i = 0; i < 6; i++)
                Soft(r, "Bridge Plank " + i, 0f, -L * 0.5f + 1f + i * 2f, W, 1.9f, Top, i % 2 == 0 ? Pink : Yellow, White, 0.5f, 18f, 0.9f, 0.08f);
            for (int i = 0; i < 2; i++) PylonAt(r, 0f, -L * 0.5f + 1f + i * 10f);
            for (int i = 0; i < 2; i++)
            {
                float z = -2.6f + i * 5.2f;
                var pivot = new GameObject("Bridge Pendulum " + i);
                pivot.transform.SetParent(r, false);
                pivot.transform.localPosition = new Vector3(0f, Top + 6.2f, z);
                P(pivot.transform, PrimitiveType.Cylinder, "Rope", new Vector3(0f, -2.1f, 0f), new Vector3(0.15f, 2.1f, 0.15f), new Color(0.9f, 0.85f, 0.65f), "plain", false);
                var ball = P(pivot.transform, PrimitiveType.Sphere, "Swinging Ball", new Vector3(0f, -5.0f, 0f), Vector3.one * 2.4f, i == 0 ? Red : Blue, "quilt", false);
                var swing = pivot.AddComponent<Pendulum>();
                swing.ball = ball.transform; swing.ballRadius = 1.2f; swing.maxAngle = 52f; swing.period = 2.2f; swing.phase = i * Mathf.PI;
                for (int side = -1; side <= 1; side += 2)
                    Tube(r, new Vector3(side * 2.6f, Top, z), new Vector3(side * 2.6f, Top + 6.6f, z), 0.8f, Orange);
                Tube(r, new Vector3(-2.6f, Top + 6.5f, z), new Vector3(2.6f, Top + 6.5f, z), 0.8f, Orange);
            }
        }

        // ------------------------------------------------------------------ fish and hidden chests

        static void BuildFish(FishSpecies cod, FishSpecies salmon, FishSpecies clown)
        {
            // Fish leap across the route from the open water beside it.
            string[] where = { "C2 Pendulum corridor", "D2 Windmills", "A1 Zigzag pads", "A4 Springy mattresses", "B2 Log gauntlet", "2 Wobble bridge", "4 Bouncing pillars", "6 Floating logs", "9 Swinging balls", "12 Rotating padded arms", "14 Final bridge" };
            var pools = new[] { new[] { cod, salmon }, new[] { salmon, clown, cod }, new[] { clown, cod } };
            int k = 0;
            foreach (var name in where)
            {
                var s = _layout.sections.Find(x => x.name == name);
                if (s == null) continue;
                Vector3 right = Vector3.Cross(Vector3.up, s.direction);
                Vector3 side = Vector3.Dot(s.center - _layout.courseCenter, right) >= 0f ? right : -right;
                float half = Mathf.Max(s.width, 8f) * 0.5f;
                float offset = half + 3.5f;
                Vector3 zone = new Vector3(s.center.x, 0.3f, s.center.z) + side * offset;
                Vector3 across = -side;
                Vector3 size = new Vector3(Mathf.Abs(s.direction.x) * 10f + Mathf.Abs(s.direction.z), 0f, Mathf.Abs(s.direction.z) * 10f + Mathf.Abs(s.direction.x));
                S3SceneBuilder.FishArea("Fish Area " + (k + 1), pools[k % pools.Length], zone, size, Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg, 15f,
                    offset + half + 1f, offset + half + 3.5f, 11 * (k + 1));
                k++;
            }
        }

        static void BuildChests()
        {
            // A chest in a corner of an obstacle, placed in world space.
            void Corner(string id, string section, float x, float z, float y, float yaw)
            {
                var s = _layout.sections.Find(q => q.name == section);
                if (s == null) return;
                Vector3 right = Vector3.Cross(Vector3.up, s.direction);
                Vector3 world = s.center + right * x + s.direction * z + Vector3.up * y;
                CourseScenery.Chest(id, world, Mathf.Atan2(s.direction.x, s.direction.z) * Mathf.Rad2Deg + yaw, false);
            }
            Corner("pillars-corner", "4 Bouncing pillars", 4.1f, -6.2f, Top, 0f);
            Corner("wall-top", "5 Climbing wall", -2f, 1.2f, Top + 3.0f, 90f);       // on top of the climbing wall
            Corner("balls-corner", "9 Swinging balls", 3.9f, 7f, Top, 200f);
            Corner("arch-end", "13 Arch maze", -3.9f, 7.2f, Top, 160f);
            Corner("arms-corner", "12 Rotating padded arms", 3.9f, -7.2f, Top, 20f);
            Corner("sweeper-corner", "A2 Sweeper deck", -3.9f, 5.4f, Top, 180f);
            Corner("ballrun-corner", "B3 Ball run", 3.9f, 5.4f, Top, 200f);
            Corner("cross-corner", "C1 Rotating cross", -3.9f, 5.6f, Top, 180f);
            Corner("windmill-corner", "D2 Windmills", 3.9f, -5.6f, Top, 20f);
            Corner("slide-top", "B4 Steep slide", 2.6f, -1.0f, Top + 3.8f, 90f);

            // And on little islets floating in the water between the lanes (swim there, then back).
            Islet("islet-north", new Vector3(-5f, 0f, 45f));
            Islet("islet-middle", new Vector3(-15f, 0f, 23f));
            Islet("islet-south", new Vector3(10f, 0f, 2f));
            Islet("islet-top-north", new Vector3(-10f, 0f, 133f));
            Islet("islet-top-middle", new Vector3(10f, 0f, 111f));
            Islet("islet-far-north", new Vector3(-15f, 0f, 89f));
            Islet("islet-far-east", new Vector3(40f, 0f, 90f));
        }

        static void Islet(string id, Vector3 centre)
        {
            var disc = P(null, PrimitiveType.Cylinder, "Islet", new Vector3(centre.x, 0f, centre.z), new Vector3(5.5f, 0.6f, 5.5f), Teal, "quilt");
            P(null, PrimitiveType.Cylinder, "Foam", new Vector3(centre.x, 0.03f, centre.z), new Vector3(6.7f, 0.02f, 6.7f), Foamy, "plain", false);
            Tube(null, new Vector3(centre.x, 0.75f, centre.z - 2f), new Vector3(centre.x, 0.75f, centre.z + 2f), 0.5f, White);
            CourseScenery.Chest(id, new Vector3(centre.x, Top, centre.z), 0f, false);
        }
    }
}
