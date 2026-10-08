using System.Collections.Generic;
using Badeland.World;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// The waterpark obstacle course: ONE big, continuous inflatable course floating on open water. It follows a wide
    /// arc from the start platform in the west, out over the water, to the finish platform in the east. The player
    /// runs through 15 different obstacles, one after the other, joined by round float pads. Nothing runs beside it.
    /// The finish platform is also where the monster appears.
    /// </summary>
    public static class InflatableCourse
    {
        public class Layout
        {
            public float radius;
            public Checkpoint[] gates;
            public Vector3 startSpawn;
            public Vector3 startCenter, startSize;
            public Vector3 arenaCenter, arenaSize;       // the finish platform
            public GameObject trapPanel;                  // a float pad that gives way under the suspicious duck
            public Vector3 trapReturn;
            public float widest = 12f;
        }

        // The colours of the inflatables.
        static readonly Color Blue = new Color(0.15f, 0.45f, 0.95f);
        static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
        static readonly Color Pink = new Color(1f, 0.4f, 0.7f);
        static readonly Color Green = new Color(0.3f, 0.8f, 0.35f);
        static readonly Color Teal = new Color(0.1f, 0.78f, 0.8f);
        static readonly Color Purple = new Color(0.6f, 0.4f, 0.95f);
        static readonly Color Red = new Color(0.95f, 0.3f, 0.3f);
        static readonly Color White = new Color(0.97f, 0.97f, 1f);
        static readonly Color Foamy = new Color(0.92f, 0.98f, 1f);

        const float Top = 0.6f;   // the top of every deck, above the sea

        static float _radius;
        static Mesh _ringMesh;

        // ------------------------------------------------------------------ the route

        struct Spec
        {
            public string name;
            public float length, width;
            public System.Action<Transform, float, float> build;
            public Spec(string name, float length, float width, System.Action<Transform, float, float> build)
            { this.name = name; this.length = length; this.width = width; this.build = build; }
        }

        // Where the route is on the arc, s metres from the start (the west end).
        static Vector3 PathPos(float s)
        {
            float theta = Mathf.PI - s / _radius;
            return new Vector3(_radius * Mathf.Cos(theta), 0f, _radius * Mathf.Sin(theta));
        }

        static Vector3 PathDir(float s)
        {
            float theta = Mathf.PI - s / _radius;
            return new Vector3(Mathf.Sin(theta), 0f, -Mathf.Cos(theta));
        }

        static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        public static Layout Build(FishSpecies cod, FishSpecies salmon, FishSpecies clown)
        {
            MeshKit.Reset();
            _ringMesh = MeshKit.Torus(3.0f, 0.3f);

            var specs = new List<Spec>
            {
                new Spec("1 Float pads",          20f, 12f, Obs1Pads),
                new Spec("2 Balance beam",        22f,  1.4f, Obs2Beam),
                new Spec("3 Big stairs",          28f,  9f, Obs3Stairs),
                new Spec("4 Bump field",          20f, 10f, Obs4Bumps),
                new Spec("5 Climbing wall",       14f,  9f, Obs5Wall),
                new Spec("6 Tunnel",              16f,  7.4f, Obs6Tunnel),
                new Spec("7 Giant balls",         22f, 12f, Obs7Balls),
                new Spec("8 Bouncy mattresses",   20f,  9f, Obs8Mattresses),
                new Spec("9 Rolling logs",        16f,  8f, Obs9Logs),
                new Spec("10 Net climb",          14f,  9f, Obs10Net),
                new Spec("11 Wave cushions",      24f, 10f, Obs11Waves),
                new Spec("12 Sweeper tunnel",     18f,  7.4f, Obs12Sweepers),
                new Spec("13 Zig-zag",            26f, 12f, Obs13Zigzag),
                new Spec("14 Up and over",        20f,  9f, Obs14UpAndOver),
                new Spec("15 Finale",             14f,  9f, Obs15Finale),
            };

            // How long the whole route is: pad at the start, then each obstacle with a float pad between, the slide, the swim to the finish.
            const float hubRadius = 3.0f, startMargin = 4f, slideLength = 34f, swim = 13f;
            float total = startMargin + hubRadius;
            foreach (var spec in specs) total += spec.length + 2f * hubRadius;
            total -= hubRadius;               // no pad after the finale
            total += slideLength + swim;
            _radius = total / Mathf.PI;

            var layout = new Layout { radius = _radius };

            // ---- Start and finish platforms (big, flat, on the beach side of the water).
            layout.startCenter = new Vector3(-_radius, 0f, -11f);
            layout.startSize = new Vector3(26f, 1.2f, 24f);
            S3SceneBuilder.Deck("Start Platform", layout.startCenter, layout.startSize);

            layout.arenaCenter = new Vector3(_radius, 0f, -12f);
            layout.arenaSize = new Vector3(34f, 1.2f, 26f);
            S3SceneBuilder.Deck("Finish Platform", layout.arenaCenter, layout.arenaSize);

            layout.startSpawn = new Vector3(-_radius, 1.8f, -8f);

            // ---- Walk along the route placing float pads and obstacles.
            var gates = new List<Checkpoint>();
            var obstacleCenters = new List<float>();
            var obstacleWidths = new List<float>();
            float s = startMargin;
            var hubPositions = new List<Vector3>();
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];

                // The float pad in front of this obstacle.
                Vector3 hub = PathPos(s);
                var hubObject = Hub(hub, "Float Pad " + i, i % 2 == 0 ? Orange : Yellow, i == 10);
                hubPositions.Add(hub);
                if (i == 10) layout.trapPanel = hubObject; // under the suspicious duck, just before obstacle 11
                if (i == 3 || i == 6 || i == 9 || i == 12)
                    gates.Add(S3SceneBuilder.Gate("Checkpoint " + gates.Count, hub, Yaw(PathDir(s)), Yellow, 14f));

                // The obstacle itself, laid along the route.
                float start = s + hubRadius;
                float mid = start + spec.length * 0.5f;
                var root = new GameObject("Obstacle " + spec.name).transform;
                root.position = PathPos(mid);
                root.rotation = Quaternion.LookRotation(PathDir(mid));
                spec.build(root, spec.length, spec.width);

                obstacleCenters.Add(mid);
                obstacleWidths.Add(spec.width);
                layout.widest = Mathf.Max(layout.widest, spec.width);

                s = start + spec.length + hubRadius;
            }
            // s is now just after the finale; the slide starts at the end of its tower.
            float slideStart = s - hubRadius;
            BuildFinaleSlide(slideStart, slideLength);

            // The finish line: across the north edge of the finish platform.
            gates.Add(S3SceneBuilder.Gate("Finish line", new Vector3(_radius, 0f, 0.8f), 180f, new Color(0.2f, 0.9f, 0.3f), 18f));
            Checkered(new Vector3(_radius, Top + 0.02f, -2.2f), 20);
            layout.gates = gates.ToArray();

            // A float pad is the trap door. The player is dropped back there after the secret room.
            layout.trapReturn = hubPositions[9] + new Vector3(0f, 1.8f, 0f);

            // ---- Jumping fish beside obstacles 2, 4, 7, 9, 11 and 13, leaping across the route.
            int[] fishAt = { 1, 3, 6, 8, 10, 12 };
            var pools = new[] { new[] { cod, salmon }, new[] { salmon, clown, cod }, new[] { clown, cod } };
            for (int k = 0; k < fishAt.Length; k++)
            {
                int index = fishAt[k];
                float sm = obstacleCenters[index];
                float width = Mathf.Max(obstacleWidths[index], 8f);
                Vector3 pos = PathPos(sm), dir = PathDir(sm);
                Vector3 outward = new Vector3(pos.x, 0f, pos.z).normalized;
                Vector3 side = k % 2 == 0 ? outward : -outward;
                float offset = width * 0.5f + 3f;
                Vector3 zone = pos + side * offset + Vector3.up * 0.3f;
                Vector3 across = -side;
                Vector3 size = new Vector3(Mathf.Abs(dir.x) * 10f + Mathf.Abs(dir.z), 0f, Mathf.Abs(dir.z) * 10f + Mathf.Abs(dir.x));
                S3SceneBuilder.FishArea("Fish Area " + (k + 1), pools[k % pools.Length], zone, size, Yaw(across), 15f,
                    offset + width * 0.5f + 1.5f, offset + width * 0.5f + 4f, 11 * (k + 1));
            }

            // ---- Hidden chests tucked along the route.
            PlaceChests(obstacleCenters);

            return layout;
        }

        // ------------------------------------------------------------------ building blocks

        static GameObject P(Transform parent, PrimitiveType type, string name, Vector3 lp, Vector3 ls, Color c,
            string kind = "quilt", bool solid = true, Vector3 euler = default)
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
                default: GrayboxMaterials.Tint(go, c); break;
            }
            return go;
        }

        // A rounded tube between two points (for looks only).
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
            var foam = P(parent, PrimitiveType.Cube, "Foam", new Vector3(lp.x, 0.03f, lp.z), new Vector3(sx + 1.2f, 0.04f, sz + 1.2f), Foamy, "plain", false);
            foam.transform.localRotation = Quaternion.identity;
        }

        // A flat inflatable deck with fat tubes along its long edges.
        static GameObject Deck(Transform parent, string name, float x, float z, float w, float l, Color c, bool tubes = true)
        {
            var deck = P(parent, PrimitiveType.Cube, name, new Vector3(x, 0f, z), new Vector3(w, 1.2f, l), c);
            Foam(parent, new Vector3(x, 0f, z), w, l);
            if (tubes)
                for (int side = -1; side <= 1; side += 2)
                    Tube(parent, new Vector3(x + side * (w * 0.5f - 0.2f), 0.7f, z - l * 0.5f + 0.4f), new Vector3(x + side * (w * 0.5f - 0.2f), 0.7f, z + l * 0.5f - 0.4f), 0.7f, White);
            return deck;
        }

        // A mat that gives under your feet. Its top is at topY (local), and it hangs down to the water.
        static GameObject Soft(Transform parent, string name, float x, float z, float w, float l, float topY, Color c,
            float dip = 0.25f, float stiffness = 38f, float damping = 3.2f)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = new Vector3(x, 0f, z);
            float height = topY + 0.6f;
            P(holder.transform, PrimitiveType.Cube, "Mat", new Vector3(0f, (topY - 0.6f) * 0.5f, 0f), new Vector3(w, height, l), c);
            for (int side = -1; side <= 1; side += 2)
                Tube(holder.transform, new Vector3(side * (w * 0.5f - 0.2f), topY - 0.1f, -l * 0.5f + 0.4f), new Vector3(side * (w * 0.5f - 0.2f), topY - 0.1f, l * 0.5f - 0.4f), 0.7f, White);
            var soft = holder.AddComponent<SoftPlatform>();
            soft.dip = dip; soft.stiffness = stiffness; soft.damping = damping;
            return holder;
        }

        // A round float pad joining one obstacle to the next. (The trap-door pad is square, so the trap can drop it away.)
        static GameObject Hub(Vector3 position, string name, Color c, bool square = false)
        {
            GameObject hub;
            if (square)
            {
                hub = P(null, PrimitiveType.Cube, name, new Vector3(position.x, 0f, position.z), new Vector3(6.5f, 1.2f, 6.5f), new Color(0.92f, 0.92f, 0.98f));
                P(null, PrimitiveType.Cube, "Foam", new Vector3(position.x, 0.03f, position.z), new Vector3(7.7f, 0.02f, 7.7f), Foamy, "plain", false);
                var ring = MeshKit.Make("Ring", _ringMesh, c, "quilt", false, hub.transform, new Vector3(0f, (Top + 0.05f) / 1.2f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1f / 6.5f, 1f / 6.5f, 1f / 1.2f));
                return hub;
            }

            hub = P(null, PrimitiveType.Cylinder, name, new Vector3(position.x, 0f, position.z), new Vector3(6.5f, 0.6f, 6.5f), c);
            P(null, PrimitiveType.Cylinder, "Foam", new Vector3(position.x, 0.03f, position.z), new Vector3(7.7f, 0.02f, 7.7f), Foamy, "plain", false);
            // An inflated ring around the top edge (for looks only).
            MeshKit.Make("Ring", _ringMesh, White, "quilt", false, null, new Vector3(position.x, Top + 0.05f, position.z), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            return hub;
        }

        static void Checkered(Vector3 center, int tilesAcross)
        {
            var root = new GameObject("Checkered Line");
            for (int i = 0; i < tilesAcross; i++)
                for (int row = 0; row < 2; row++)
                {
                    bool white = (i + row) % 2 == 0;
                    var tile = P(root.transform, PrimitiveType.Cube, "Tile", new Vector3(center.x - tilesAcross * 0.5f + i + 0.5f, center.y, center.z - 0.5f - row), new Vector3(1f, 0.02f, 1f),
                        white ? new Color(0.97f, 0.97f, 0.97f) : new Color(0.08f, 0.08f, 0.1f), "plain", false);
                }
        }

        // ------------------------------------------------------------------ the 15 obstacles (local: x right, z forward, deck top at y 0.6)

        // 1. Floating balance pads: soft round cushions scattered in a zigzag, each giving way underfoot.
        static void Obs1Pads(Transform r, float L, float W)
        {
            Color[] colors = { Orange, Yellow, Pink, Teal, Green, Purple };
            const int n = 8;
            for (int i = 0; i < n; i++)
            {
                float z = -L * 0.5f + 1.8f + i * (L - 3.6f) / (n - 1);
                float x = (i % 2 == 0 ? -1f : 1f) * (1.8f + 0.6f * Mathf.Sin(i * 2.1f));
                float d = 3.2f + 0.6f * Mathf.Sin(i * 1.3f);
                var holder = new GameObject("Float Pad " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(x, 0f, z);
                P(holder.transform, PrimitiveType.Cylinder, "Pad", Vector3.zero, new Vector3(d, 0.6f, d), colors[i % colors.Length]);
                P(holder.transform, PrimitiveType.Cylinder, "Foam", new Vector3(0f, 0.03f, 0f), new Vector3(d + 1.2f, 0.02f, d + 1.2f), Foamy, "plain", false);
                var soft = holder.AddComponent<SoftPlatform>();
                soft.dip = 0.3f;
            }
        }

        // 2. A long, narrow inflatable beam that sways from side to side.
        static void Obs2Beam(Transform r, float L, float W)
        {
            var holder = new GameObject("Balance Beam");
            holder.transform.SetParent(r, false);
            holder.transform.localPosition = Vector3.zero;
            var box = holder.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 1.2f, L);
            P(holder.transform, PrimitiveType.Capsule, "Beam", new Vector3(0f, -0.05f, 0f), new Vector3(1.3f, L * 0.5f, 1.3f), Orange, "quilt", false, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 5; i++) // white bands round the beam
                P(holder.transform, PrimitiveType.Cylinder, "Band", new Vector3(0f, -0.05f, -L * 0.4f + i * L * 0.2f), new Vector3(1.38f, 0.12f, 1.38f), White, "plain", false, new Vector3(90f, 0f, 0f));

            var platform = holder.AddComponent<MovingPlatform>();
            platform.pointA = r.position - r.right * 0.6f;
            platform.pointB = r.position + r.right * 0.6f;
            platform.period = 3.4f;
        }

        // 3. Big inflatable steps: five up, a soft platform on top, five down.
        static void Obs3Stairs(Transform r, float L, float W)
        {
            Color[] colors = { Blue, Teal, Green, Yellow, Orange };
            const float run = 2.4f, rise = 0.5f;
            float z = -L * 0.5f;
            for (int k = 0; k < 5; k++)
            {
                float top = Top + rise * (k + 1);
                var step = P(r, PrimitiveType.Cube, "Step Up " + k, new Vector3(0f, (top - 0.6f) * 0.5f, z + run * 0.5f), new Vector3(W, top + 0.6f, run), colors[k % colors.Length]);
                Tube(r, new Vector3(-W * 0.5f + 0.5f, top - 0.2f, z + 0.3f), new Vector3(W * 0.5f - 0.5f, top - 0.2f, z + 0.3f), 0.7f, White);
                z += run;
            }
            float platformTop = Top + rise * 5;
            Foam(r, new Vector3(0f, 0f, z + 2f), W, 4f);
            Soft(r, "Top Mat", 0f, z + 2f, W, 4f, platformTop, Pink, 0.2f);
            z += 4f;
            for (int k = 4; k >= 0; k--)
            {
                float top = Top + rise * (k + 1);
                P(r, PrimitiveType.Cube, "Step Down " + k, new Vector3(0f, (top - 0.6f) * 0.5f, z + run * 0.5f), new Vector3(W, top + 0.6f, run), colors[k % colors.Length]);
                Tube(r, new Vector3(-W * 0.5f + 0.5f, top - 0.2f, z + run - 0.3f), new Vector3(W * 0.5f - 0.5f, top - 0.2f, z + run - 0.3f), 0.7f, White);
                z += run;
            }
        }

        // 4. A bumpy field of big round domes.
        static void Obs4Bumps(Transform r, float L, float W)
        {
            var mesh = MeshKit.HeightPad("BumpField", W, L, (x, z) =>
            {
                float dome = (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * x / 5f)) * (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * z / 5f));
                return Top + 1.0f * dome * dome * 1.0f + 0.0f;
            }, -0.6f);
            MeshKit.Make("Bump Field", mesh, Pink, "quilt", true, r, Vector3.zero, Quaternion.identity, Vector3.one);
            Foam(r, Vector3.zero, W, L);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.1f), 0.6f, -L * 0.5f + 0.4f), new Vector3(side * (W * 0.5f - 0.1f), 0.6f, L * 0.5f - 0.4f), 0.7f, White);
        }

        // 5. A climbing wall: two soft blocks to jump up onto, then drop down.
        static void Obs5Wall(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Blue);
            var a = P(r, PrimitiveType.Cube, "Wall Block 1", new Vector3(0f, (2.0f - 0.6f) * 0.5f + 0.0f, -2.3f), new Vector3(W, 2.0f + 0.6f, 2.2f), Orange);
            var b = P(r, PrimitiveType.Cube, "Wall Block 2", new Vector3(0f, (3.4f - 0.6f) * 0.5f, 1.2f), new Vector3(W, 3.4f + 0.6f, 2.2f), Yellow);
            Tube(r, new Vector3(-W * 0.5f + 0.5f, 2.0f - 0.2f, -2.3f), new Vector3(W * 0.5f - 0.5f, 2.0f - 0.2f, -2.3f), 0.8f, White);
            Tube(r, new Vector3(-W * 0.5f + 0.5f, 3.4f - 0.2f, 1.2f), new Vector3(W * 0.5f - 0.5f, 3.4f - 0.2f, 1.2f), 0.8f, White);
            // Hand holds on the faces.
            Color[] holds = { Red, Green, Purple, Teal };
            for (int i = 0; i < 12; i++)
            {
                float x = -W * 0.5f + 1f + (i % 6) * (W - 2f) / 5f;
                float y = 0.9f + (i / 6) * 0.9f;
                P(r, PrimitiveType.Sphere, "Hold", new Vector3(x, y, -3.45f), new Vector3(0.5f, 0.5f, 0.35f), holds[i % holds.Length], "plain", false);
                P(r, PrimitiveType.Sphere, "Hold", new Vector3(x, y + 0.7f, 0.05f), new Vector3(0.5f, 0.5f, 0.35f), holds[(i + 1) % holds.Length], "plain", false);
            }
        }

        // 6. A soft inflatable tunnel.
        static void Obs6Tunnel(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Teal, false);
            var mesh = MeshKit.Tunnel(W * 0.5f - 0.2f, 3.9f, L - 0.6f);
            MeshKit.Make("Tunnel", mesh, Yellow, "quilt", true, r, new Vector3(0f, Top, 0f), Quaternion.identity, Vector3.one);
            for (int i = 0; i < 6; i++)
            {
                var ring = MeshKit.Make("Tunnel Ring", _ringMesh, Orange, "quilt", false, r, new Vector3(0f, Top, -L * 0.5f + 1.5f + i * (L - 3f) / 5f), Quaternion.identity, new Vector3(1.2f, 1.35f, 1.2f));
            }
        }

        // 7. Giant balls to climb round (or over).
        static void Obs7Balls(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Blue);
            Color[] colors = { Red, Yellow, Green, Pink, Orange, Purple, Teal, Red };
            float[,] spots =
            {
                { -3.5f, -8f, 2.6f }, { 3.2f, -5.5f, 2.4f }, { -2.5f, -2f, 3.0f }, { 4.0f, 1.0f, 2.6f },
                { -4.2f, 3.8f, 2.8f }, { 1.0f, 6.2f, 3.4f }, { -3.5f, 9f, 2.4f }, { 3.5f, 9.2f, 2.2f },
            };
            for (int i = 0; i < spots.GetLength(0); i++)
            {
                float d = spots[i, 2];
                var ball = P(r, PrimitiveType.Sphere, "Giant Ball", new Vector3(spots[i, 0], Top + d * 0.5f - 0.1f, spots[i, 1]), Vector3.one * d, colors[i % colors.Length]);
                P(r, PrimitiveType.Cylinder, "Stripe", ball.transform.localPosition, new Vector3(d * 1.003f, 0.05f, d * 1.003f), White, "plain", false);
            }
        }

        // 8. Big springy mattresses.
        static void Obs8Mattresses(Transform r, float L, float W)
        {
            Color[] colors = { Yellow, Pink, Teal, Orange };
            for (int i = 0; i < 4; i++)
            {
                float z = -L * 0.5f + 2.2f + i * 5.2f;
                Soft(r, "Mattress " + i, 0f, z, W, 4.4f, Top + 0.2f, colors[i], 0.45f, 26f, 1.7f);
            }
        }

        // 9. Rolling logs: they turn under your feet and carry you backwards.
        static void Obs9Logs(Transform r, float L, float W)
        {
            Color[] colors = { Orange, Yellow };
            for (int i = 0; i < 6; i++)
            {
                var holder = new GameObject("Rolling Log " + i);
                holder.transform.SetParent(r, false);
                holder.transform.localPosition = new Vector3(0f, -0.25f, -6.25f + i * 2.5f);
                var visual = P(holder.transform, PrimitiveType.Cylinder, "Log", Vector3.zero, new Vector3(1.7f, W * 0.5f, 1.7f), colors[i % 2], "quilt", true, new Vector3(0f, 0f, 90f));
                P(holder.transform, PrimitiveType.Cylinder, "Stripe", Vector3.zero, new Vector3(1.74f, 0.4f, 1.74f), White, "plain", false, new Vector3(0f, 0f, 90f));
                var log = holder.AddComponent<RollingLog>();
                log.visual = visual.transform;
                log.radius = 0.85f;
                log.surfaceSpeed = (i % 2 == 0 ? -1f : 1f) * 2.3f;
            }
        }

        // 10. A climbing frame with nets: three ledges to scramble up, then jump down.
        static void Obs10Net(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Green);
            float[] tops = { 1.7f, 2.8f, 3.9f };
            float[] zs = { -3.0f, 1.1f, 5.2f };
            for (int i = 0; i < 3; i++)
            {
                P(r, PrimitiveType.Cube, "Ledge " + (i + 1), new Vector3(0f, (tops[i] - 0.6f) * 0.5f, zs[i]), new Vector3(W, tops[i] + 0.6f, 2.5f), i % 2 == 0 ? Orange : Yellow);
                Tube(r, new Vector3(-W * 0.5f + 0.5f, tops[i] - 0.2f, zs[i] - 1.1f), new Vector3(W * 0.5f - 0.5f, tops[i] - 0.2f, zs[i] - 1.1f), 0.7f, White);
            }
            // Cargo nets hanging down each side, and a net wall at the back.
            var rope = new Color(0.9f, 0.85f, 0.65f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 0; k < 14; k++)
                    P(r, PrimitiveType.Cylinder, "Net Rope", new Vector3(side * (W * 0.5f + 0.1f), 2.4f, -L * 0.5f + 0.5f + k * (L - 1f) / 13f), new Vector3(0.07f, 2.0f, 0.07f), rope, "plain", false);
                for (int k = 0; k < 5; k++)
                    P(r, PrimitiveType.Cylinder, "Net Rope", new Vector3(side * (W * 0.5f + 0.1f), 0.9f + k * 0.8f, 0f), new Vector3(0.07f, L * 0.5f, 0.07f), rope, "plain", false, new Vector3(90f, 0f, 0f));
            }
            for (int k = 0; k < 10; k++)
                P(r, PrimitiveType.Cylinder, "Net Rope", new Vector3(-W * 0.5f + 0.5f + k * (W - 1f) / 9f, 4.5f, 6.9f), new Vector3(0.07f, 2.4f, 0.07f), rope, "plain", false);
        }

        // 11. Tall rolling wave cushions.
        static void Obs11Waves(Transform r, float L, float W)
        {
            var mesh = MeshKit.HeightPad("Waves", W, L, (x, z) => Top + 0.8f * (1f + Mathf.Sin(2f * Mathf.PI * (z + 0.35f * x) / 7.5f)), -0.6f);
            MeshKit.Make("Wave Cushions", mesh, Blue, "quilt", true, r, Vector3.zero, Quaternion.identity, Vector3.one);
            Foam(r, Vector3.zero, W, L);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.1f), 0.6f, -L * 0.5f + 0.4f), new Vector3(side * (W * 0.5f - 0.1f), 0.6f, L * 0.5f - 0.4f), 0.7f, White);
        }

        // 12. A low tunnel with soft sweepers turning inside.
        static void Obs12Sweepers(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Orange, false);
            var mesh = MeshKit.Tunnel(W * 0.5f - 0.2f, 3.6f, L - 0.6f);
            MeshKit.Make("Tunnel", mesh, Pink, "quilt", true, r, new Vector3(0f, Top, 0f), Quaternion.identity, Vector3.one);
            for (int i = 0; i < 6; i++)
                MeshKit.Make("Tunnel Ring", _ringMesh, White, "quilt", false, r, new Vector3(0f, Top, -L * 0.5f + 1.5f + i * (L - 3f) / 5f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f));

            for (int i = 0; i < 2; i++)
            {
                var bar = P(r, PrimitiveType.Cube, "Soft Sweeper", new Vector3(0f, Top + 0.45f, i == 0 ? -4f : 4.5f), new Vector3(W - 1.2f, 0.7f, 0.6f), i == 0 ? Yellow : Teal, "quilt", false);
                var spin = bar.AddComponent<RotatingBar>();
                spin.degreesPerSecond = i == 0 ? 75f : -75f;
            }
        }

        // 13. Zig-zag: big soft walls to wind between.
        static void Obs13Zigzag(Transform r, float L, float W)
        {
            Deck(r, "Deck", 0f, 0f, W, L, Blue);
            Color[] colors = { Red, Yellow, Pink, Green, Orange, Purple };
            for (int i = 0; i < 6; i++)
            {
                float z = -10f + i * 4f;
                float x = (i % 2 == 0 ? -1f : 1f) * 1.8f;
                P(r, PrimitiveType.Cube, "Zigzag Wall " + i, new Vector3(x, Top + 1.6f, z), new Vector3(8.4f, 3.2f, 1.3f), colors[i]);
                Tube(r, new Vector3(x - 4.0f, Top + 3.2f, z), new Vector3(x + 4.0f, Top + 3.2f, z), 0.8f, White);
            }
        }

        // 14. A big ramp up, over the top and down again.
        static void Obs14UpAndOver(Transform r, float L, float W)
        {
            float topY = Top + 3.4f;
            var mesh = MeshKit.HeightPad("UpAndOver", W, L, (x, z) =>
            {
                if (z < -8.4f) return Top;
                if (z < -2f) return Mathf.Lerp(Top, topY, (z + 8.4f) / 6.4f);
                if (z < 2f) return topY;
                if (z < 8.4f) return Mathf.Lerp(topY, Top, (z - 2f) / 6.4f);
                return Top;
            }, -0.6f);
            MeshKit.Make("Ramp", mesh, Orange, "quilt", true, r, Vector3.zero, Quaternion.identity, Vector3.one);
            Foam(r, Vector3.zero, W, L);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.1f), 0.7f, -L * 0.5f + 0.4f), new Vector3(side * (W * 0.5f - 0.1f), topY + 0.1f, -2f), 0.7f, White);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.1f), topY + 0.1f, 2f), new Vector3(side * (W * 0.5f - 0.1f), 0.7f, L * 0.5f - 0.4f), 0.7f, White);
            for (int side = -1; side <= 1; side += 2)
                Tube(r, new Vector3(side * (W * 0.5f - 0.1f), topY + 0.1f, -2f), new Vector3(side * (W * 0.5f - 0.1f), topY + 0.1f, 2f), 0.7f, White);
        }

        // 15. The finale: a steep inflatable climb up a tower. The slide starts at the top (see BuildFinaleSlide).
        static void Obs15Finale(Transform r, float L, float W)
        {
            float topY = Top + 5.5f;
            var mesh = MeshKit.HeightPad("Finale", W, L, (x, z) => z < 0f ? Mathf.Lerp(Top, topY, (z + L * 0.5f) / (L * 0.5f)) : topY, -0.6f);
            MeshKit.Make("Climbing Hill", mesh, Pink, "quilt", true, r, Vector3.zero, Quaternion.identity, Vector3.one);
            Foam(r, Vector3.zero, W, L);

            // A giant ring over the top, with flags: you have nearly made it.
            for (int side = -1; side <= 1; side += 2)
            {
                Tube(r, new Vector3(side * 4.2f, topY, L * 0.5f - 1.2f), new Vector3(side * 4.2f, topY + 4.6f, L * 0.5f - 1.2f), 0.9f, Yellow);
                P(r, PrimitiveType.Cube, "Flag", new Vector3(side * 3.4f, topY + 5.3f, L * 0.5f - 1.2f), new Vector3(1.4f, 0.9f, 0.05f), side < 0 ? Red : Blue, "plain", false);
            }
            Tube(r, new Vector3(-4.2f, topY + 4.6f, L * 0.5f - 1.2f), new Vector3(4.2f, topY + 4.6f, L * 0.5f - 1.2f), 0.9f, Yellow);
        }

        // The slide that follows the finale: a long winding ride from the tower down to the sea, in front of the finish platform.
        static void BuildFinaleSlide(float s0, float length)
        {
            const int count = 12;
            var path = new Vector3[count];
            for (int k = 0; k < count; k++)
            {
                float t = k / (count - 1f);
                float s = s0 + length * t;
                Vector3 pos = PathPos(s), dir = PathDir(s);
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);
                float y = Mathf.Lerp(Top + 6.2f, 1.2f, Mathf.SmoothStep(0f, 1f, t));
                path[k] = pos + right * (Mathf.Sin(t * Mathf.PI * 3f) * 3f) + Vector3.up * y;
            }
            CourseExtras.BuildSlideFromPath(path);
        }

        // ------------------------------------------------------------------ hidden treasure along the route

        static void PlaceChests(List<float> mids)
        {
            // A chest in a corner of an obstacle's frame, put in world space.
            void Corner(string id, int obstacle, float x, float z, float y, float yaw)
            {
                float s = mids[obstacle];
                Vector3 pos = PathPos(s), dir = PathDir(s);
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);
                Vector3 world = pos + right * x + dir * z + Vector3.up * y;
                CourseScenery.Chest(id, world, Yaw(dir) + yaw, false);
            }
            Corner("balls-corner", 6, 5.3f, 10.2f, Top, 200f);     // behind the giant balls
            Corner("tunnel-end", 5, 2.4f, 7.0f, Top, 90f);          // at the back of the dark tunnel
            Corner("zigzag-end", 12, -5.2f, 12.0f, Top, 20f);       // the last pocket of the zig-zag
            Corner("net-back", 9, -3.6f, 6.4f, Top, 160f);          // behind the climbing frame
            Corner("mattress-edge", 7, 3.8f, 9.6f, Top + 0.2f, 0f);   // on the last mattress

            // And on tiny islets floating out in the water, beside the route (swim there).
            Islet("islet-start", mids[0], 17f, 5.5f);
            Islet("islet-middle", mids[7], -18f, 5.5f);
            Islet("islet-late", mids[12], 19f, 5.5f);
        }

        static void Islet(string id, float s, float lateral, float diameter)
        {
            Vector3 pos = PathPos(s), dir = PathDir(s);
            Vector3 right = new Vector3(dir.z, 0f, -dir.x);
            Vector3 centre = pos + right * lateral;
            var disc = P(null, PrimitiveType.Cylinder, "Islet", new Vector3(centre.x, 0f, centre.z), new Vector3(diameter, 0.6f, diameter), Teal);
            P(null, PrimitiveType.Cylinder, "Foam", new Vector3(centre.x, 0.03f, centre.z), new Vector3(diameter + 1.2f, 0.02f, diameter + 1.2f), Foamy, "plain", false);
            CourseScenery.Chest(id, new Vector3(centre.x, Top, centre.z), Yaw(-right), false);
        }
    }
}
