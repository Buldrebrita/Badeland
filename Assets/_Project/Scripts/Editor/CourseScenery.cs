using System.Collections.Generic;
using Badeland.World;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Everything around the obstacle course: the beach and quay along the south side, a little marina with piers
    /// and boats, ice cream stands, parasols and loungers, crowds cheering by the start and finish, the big coastal
    /// town in the background (only for looks: nobody can go there), breakwaters with a lighthouse, a ferris wheel.
    /// </summary>
    public static class CourseScenery
    {
        const float QuayZ = -30f;     // the water ends here; the land begins
        const float LandTop = 1.6f;

        static float[] _crowdXs = { 0f };

        static readonly Color Sand = new Color(0.95f, 0.85f, 0.6f);
        static readonly Color Concrete = new Color(0.78f, 0.78f, 0.8f);

        // ------------------------------------------------------------------ small helpers

        static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color c, string kind = "plain", bool solid = false, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            switch (kind)
            {
                case "quilt": GrayboxMaterials.TintQuilted(go, c); break;
                case "wood": GrayboxMaterials.TintWood(go, c); break;
                case "stone": GrayboxMaterials.TintStone(go, c); break;
                case "sand": GrayboxMaterials.TintSand(go, c); break;
                case "building": GrayboxMaterials.TintBuilding(go, c); break;
                default: GrayboxMaterials.Tint(go, c); break;
            }
            return go;
        }

        static Color Pick(System.Random rng, params Color[] colors) => colors[rng.Next(colors.Length)];

        static float F(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ------------------------------------------------------------------ the whole scene

        public static void Build(InflatableCourse.Layout layout)
        {
            var rng = new System.Random(2024);
            float r = 90f; // half the width of the beach
            _crowdXs = new[] { layout.arenaCenter.x, -12f, -48f };

            BuildLand();
            BuildBeach(rng, r);
            BuildMarina(rng);
            BuildStands(r);
            BuildCrowds(rng, r);
            BuildTown(rng);
            BuildBreakwaters(r);
            BuildFerrisWheel(r);
        }

        // ------------------------------------------------------------------ land, quay, promenade, road

        static void BuildLand()
        {
            // The water to the horizon, and the big block of land on the south side with its quay face along z = -30.
            Prim(null, PrimitiveType.Cube, "Far Sea", new Vector3(0f, -1.2f, 300f), new Vector3(5000f, 1f, 2600f), new Color(0.12f, 0.45f, 0.9f));
            Prim(null, PrimitiveType.Cube, "Land", new Vector3(0f, LandTop - 2.4f, QuayZ - 225f), new Vector3(1000f, 4.8f, 450f), new Color(0.5f, 0.6f, 0.45f), "plain", true);

            Prim(null, PrimitiveType.Cube, "Beach Sand", new Vector3(0f, LandTop + 0.02f, QuayZ - 16f), new Vector3(700f, 0.04f, 32f), Sand, "sand");
            Prim(null, PrimitiveType.Cube, "Wet Sand", new Vector3(0f, LandTop + 0.03f, QuayZ - 1.5f), new Vector3(700f, 0.04f, 3f), new Color(0.75f, 0.65f, 0.45f), "sand");
            Prim(null, PrimitiveType.Cube, "Promenade", new Vector3(0f, LandTop + 0.03f, QuayZ - 36f), new Vector3(700f, 0.05f, 8f), new Color(0.8f, 0.8f, 0.82f), "stone");
            Prim(null, PrimitiveType.Cube, "Road", new Vector3(0f, LandTop + 0.03f, QuayZ - 48f), new Vector3(1000f, 0.05f, 12f), new Color(0.2f, 0.2f, 0.23f));
            Prim(null, PrimitiveType.Cube, "Pavement", new Vector3(0f, LandTop + 0.03f, QuayZ - 57f), new Vector3(1000f, 0.05f, 6f), new Color(0.7f, 0.7f, 0.72f), "stone");
            for (int i = -48; i <= 48; i++)
                Prim(null, PrimitiveType.Cube, "Lane Mark", new Vector3(i * 8f, LandTop + 0.07f, QuayZ - 48f), new Vector3(3f, 0.02f, 0.3f), Color.white);

            // The lip of the quay with bollards, so the edge reads as a real harbour wall.
            Prim(null, PrimitiveType.Cube, "Quay Lip", new Vector3(0f, LandTop - 0.2f, QuayZ + 0.5f), new Vector3(700f, 0.9f, 1.2f), Concrete, "stone");
            for (int i = -34; i <= 34; i++)
                Prim(null, PrimitiveType.Cylinder, "Bollard", new Vector3(i * 10f, LandTop + 0.4f, QuayZ + 0.3f), new Vector3(0.45f, 0.4f, 0.45f), new Color(0.15f, 0.15f, 0.18f));
        }

        // ------------------------------------------------------------------ the beach

        static void BuildBeach(System.Random rng, float r)
        {
            var root = new GameObject("Beach Life").transform;
            Color[] umbrellas = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.8f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.55f, 0.15f), new Color(0.9f, 0.4f, 0.9f) };

            for (int i = 0; i < 34; i++)
            {
                float x = F(rng, -r - 20f, r + 20f);
                float z = QuayZ - F(rng, 6f, 28f);
                if (NearCrowd(x, 8f) && z > QuayZ - 12f) continue; // keep the front of the course clear for the crowds
                Color c = Pick(rng, umbrellas);
                Parasol(root, new Vector3(x, LandTop, z), c);
                Lounger(root, new Vector3(x - 1.4f, LandTop, z + 0.5f), 10f, Pick(rng, Color.white, c));
                Lounger(root, new Vector3(x + 1.4f, LandTop, z + 0.3f), -8f, Pick(rng, Color.white, c));
                if (rng.NextDouble() < 0.5)
                    Person(root, new Vector3(x - 1.4f, LandTop + 0.35f, z + 0.6f), 180f, Pick(rng, umbrellas), new Color(0.2f, 0.3f, 0.6f), Skin(rng), false, true);
            }

            // Towels, beach balls, a volleyball net and a lifeguard tower.
            for (int i = 0; i < 40; i++)
                Prim(root, PrimitiveType.Cube, "Towel", new Vector3(F(rng, -r - 20f, r + 20f), LandTop + 0.06f, QuayZ - F(rng, 4f, 28f)), new Vector3(1.0f, 0.04f, 1.8f), Pick(rng, umbrellas), "plain", false, new Vector3(0f, F(rng, 0f, 180f), 0f));
            for (int i = 0; i < 10; i++)
                Prim(root, PrimitiveType.Sphere, "Beach Ball", new Vector3(F(rng, -r, r), LandTop + 0.35f, QuayZ - F(rng, 5f, 24f)), Vector3.one * 0.7f, Pick(rng, umbrellas));

            for (int side = -1; side <= 1; side += 2)
                Prim(root, PrimitiveType.Cylinder, "Net Post", new Vector3(30f + side * 5f, LandTop + 1.2f, QuayZ - 20f), new Vector3(0.12f, 1.2f, 0.12f), Color.white);
            Prim(root, PrimitiveType.Cube, "Volleyball Net", new Vector3(30f, LandTop + 1.9f, QuayZ - 20f), new Vector3(10f, 0.9f, 0.03f), new Color(0.95f, 0.95f, 0.95f));

            // The lifeguard tower.
            Vector3 t = new Vector3(-20f, LandTop, QuayZ - 14f);
            for (int i = 0; i < 4; i++)
                Prim(root, PrimitiveType.Cylinder, "Tower Leg", t + new Vector3(i % 2 * 2.4f - 1.2f, 1.6f, i / 2 * 2.4f - 1.2f), new Vector3(0.18f, 1.6f, 0.18f), Color.white, "wood");
            Prim(root, PrimitiveType.Cube, "Tower Cabin", t + new Vector3(0f, 3.7f, 0f), new Vector3(3.2f, 2f, 3.2f), Color.white, "wood");
            Prim(root, PrimitiveType.Cube, "Tower Roof", t + new Vector3(0f, 4.9f, 0f), new Vector3(3.8f, 0.3f, 3.8f), new Color(0.95f, 0.25f, 0.25f), "wood");
            Prim(root, PrimitiveType.Cube, "Tower Stripe", t + new Vector3(0f, 3.4f, 1.62f), new Vector3(3.2f, 0.5f, 0.05f), new Color(0.95f, 0.25f, 0.25f));
            Person(root, t + new Vector3(0f, 2.7f, 1.6f), 0f, new Color(0.95f, 0.25f, 0.25f), new Color(0.95f, 0.25f, 0.25f), Skin(rng), false, false);

            // Palms along the promenade.
            for (int i = 0; i < 26; i++)
                Palm(root, new Vector3(-r - 20f + i * (2f * r + 40f) / 25f, LandTop, QuayZ - 32.5f), rng);
            for (int i = 0; i < 40; i++)
                LampPost(root, new Vector3(-r - 20f + i * (2f * r + 40f) / 39f, LandTop, QuayZ - 39.5f));
        }

        static bool NearCrowd(float x, float distance)
        {
            foreach (float cx in _crowdXs) if (Mathf.Abs(x - cx) < distance) return true;
            return false;
        }

        static Color Skin(System.Random rng) => Pick(rng, new Color(1f, 0.82f, 0.68f), new Color(0.9f, 0.68f, 0.5f), new Color(0.65f, 0.45f, 0.3f), new Color(0.42f, 0.28f, 0.2f), new Color(1f, 0.88f, 0.78f));

        static void Parasol(Transform parent, Vector3 pos, Color color)
        {
            Prim(parent, PrimitiveType.Cylinder, "Parasol Pole", pos + Vector3.up * 1.3f, new Vector3(0.1f, 1.3f, 0.1f), Color.white);
            var top = Prim(parent, PrimitiveType.Sphere, "Parasol Top", pos + Vector3.up * 2.6f, new Vector3(3.2f, 0.7f, 3.2f), color, "quilt");
            Prim(parent, PrimitiveType.Sphere, "Parasol Stripe", pos + Vector3.up * 2.62f, new Vector3(3.22f, 0.7f, 0.5f), Color.white);
        }

        static void Lounger(Transform parent, Vector3 pos, float yaw, Color color)
        {
            var root = new GameObject("Lounger").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(root, PrimitiveType.Cube, "Seat", new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.12f, 1.4f), color, "quilt");
            Prim(root, PrimitiveType.Cube, "Back", new Vector3(0f, 0.7f, -0.8f), new Vector3(0.8f, 0.12f, 0.9f), color, "quilt", false, new Vector3(-35f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Prim(root, PrimitiveType.Cylinder, "Leg", new Vector3(i % 2 * 0.6f - 0.3f, 0.17f, i / 2 * 1.0f - 0.5f), new Vector3(0.06f, 0.17f, 0.06f), new Color(0.3f, 0.3f, 0.32f));
        }

        static void Palm(Transform parent, Vector3 pos, System.Random rng)
        {
            var root = new GameObject("Palm Tree").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            float height = F(rng, 5f, 7f);
            var trunk = new Color(0.55f, 0.38f, 0.2f);
            Vector3 lean = new Vector3(F(rng, -1f, 1f), 0f, F(rng, -0.4f, 0.4f));
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                Prim(root, PrimitiveType.Cylinder, "Trunk", Vector3.up * (height * (t + 0.1f)) + lean * t * t, new Vector3(0.55f - 0.07f * i, height * 0.12f, 0.55f - 0.07f * i), trunk, "wood");
            }
            Vector3 crown = Vector3.up * (height * 1.1f) + lean;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Prim(root, PrimitiveType.Cube, "Frond", crown + new Vector3(Mathf.Cos(a) * 1.4f, -0.25f, Mathf.Sin(a) * 1.4f), new Vector3(3f, 0.08f, 0.8f), new Color(0.2f, 0.75f, 0.3f), "plain", false, new Vector3(0f, -a * Mathf.Rad2Deg, -18f));
            }
            Prim(root, PrimitiveType.Sphere, "Coconuts", crown + Vector3.down * 0.2f, Vector3.one * 0.7f, new Color(0.35f, 0.22f, 0.1f));
        }

        static void LampPost(Transform parent, Vector3 pos)
        {
            Prim(parent, PrimitiveType.Cylinder, "Lamp Post", pos + Vector3.up * 2f, new Vector3(0.14f, 2f, 0.14f), new Color(0.15f, 0.15f, 0.18f));
            Prim(parent, PrimitiveType.Sphere, "Lamp", pos + Vector3.up * 4.1f, Vector3.one * 0.55f, new Color(1f, 0.95f, 0.8f));
        }

        // ------------------------------------------------------------------ the marina

        static void BuildMarina(System.Random rng)
        {
            var root = new GameObject("Marina").transform;
            var wood = new Color(0.45f, 0.3f, 0.18f);
            float[] piers = { -40f, -14f, 14f, 40f };

            foreach (float x in piers)
            {
                // A pier reaching out from the quay on posts.
                Prim(root, PrimitiveType.Cube, "Pier", new Vector3(x, 1.0f, QuayZ + 11f), new Vector3(4f, 0.3f, 24f), wood, "wood", true);
                for (int k = 0; k < 7; k++)
                    foreach (float side in new[] { -1.8f, 1.8f })
                    {
                        Prim(root, PrimitiveType.Cylinder, "Pier Post", new Vector3(x + side, -1.5f, QuayZ + 1f + k * 3.6f), new Vector3(0.4f, 2.6f, 0.4f), wood * 0.8f, "wood");
                        if (k % 2 == 0) Prim(root, PrimitiveType.Cylinder, "Pier Light", new Vector3(x + side, 2.0f, QuayZ + 1f + k * 3.6f), new Vector3(0.14f, 0.9f, 0.14f), new Color(0.15f, 0.15f, 0.18f));
                    }
                Prim(root, PrimitiveType.Cube, "Crate", new Vector3(x + 1.2f, 1.6f, QuayZ + 20f), new Vector3(1f, 1f, 1f), wood * 1.1f, "wood");
                Person(root, new Vector3(x - 1f, 1.15f, QuayZ + 18f), 0f, Pick(rng, Color.white, new Color(0.4f, 0.6f, 0.9f)), new Color(0.2f, 0.25f, 0.35f), Skin(rng), false, false);
            }

            // Boats tied up between the piers.
            Color[] hulls = { Color.white, new Color(0.2f, 0.4f, 0.8f), new Color(0.85f, 0.2f, 0.2f), new Color(0.95f, 0.8f, 0.2f) };
            for (int i = 0; i < 12; i++)
            {
                float x = piers[i % 4] + (i % 8 < 4 ? 6.5f : -6.5f);
                float z = QuayZ + 8f + (i / 4) * 5.5f;
                Boat(root, new Vector3(x, 0f, z), F(rng, -8f, 8f), hulls[i % hulls.Length], i % 3 == 0, rng);
            }
            // The big yacht moored at the end.
            Boat(root, new Vector3(60f, 0f, QuayZ + 22f), 90f, Color.white, true, rng, 2f);
        }

        static void Boat(Transform parent, Vector3 pos, float yaw, Color hull, bool sail, System.Random rng, float scale = 1f)
        {
            var root = new GameObject("Boat").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.localScale = Vector3.one * scale;

            Prim(root, PrimitiveType.Capsule, "Hull", new Vector3(0f, 0.2f, 0f), new Vector3(1.9f, 3.2f, 1.1f), hull, "quilt", false, new Vector3(90f, 0f, 0f));
            Prim(root, PrimitiveType.Cube, "Deck Stripe", new Vector3(0f, 0.65f, 0f), new Vector3(1.5f, 0.06f, 5.4f), new Color(0.65f, 0.5f, 0.35f), "wood");
            if (sail)
            {
                Prim(root, PrimitiveType.Cylinder, "Mast", new Vector3(0f, 3.4f, 0.4f), new Vector3(0.1f, 3.2f, 0.1f), new Color(0.85f, 0.85f, 0.88f));
                Prim(root, PrimitiveType.Cube, "Sail", new Vector3(0.05f, 3.2f, -0.6f), new Vector3(0.05f, 4.2f, 2.4f), new Color(0.98f, 0.97f, 0.92f));
                Prim(root, PrimitiveType.Cube, "Boom", new Vector3(0f, 1.2f, -0.6f), new Vector3(0.1f, 0.1f, 2.6f), new Color(0.85f, 0.85f, 0.88f));
            }
            else
            {
                Prim(root, PrimitiveType.Cube, "Cabin", new Vector3(0f, 1.15f, -0.4f), new Vector3(1.4f, 1f, 1.8f), Color.white, "quilt");
                Prim(root, PrimitiveType.Cube, "Windscreen", new Vector3(0f, 1.45f, 0.55f), new Vector3(1.3f, 0.5f, 0.06f), new Color(0.2f, 0.4f, 0.5f), "plain", false, new Vector3(-20f, 0f, 0f));
            }
            root.gameObject.AddComponent<Bobber>().amplitude = 0.12f;
        }

        // ------------------------------------------------------------------ ice cream stands

        static void BuildStands(float r)
        {
            var root = new GameObject("Ice Cream Stands").transform;
            float[] xs = { -r - 12f, -r * 0.45f, r * 0.45f, r + 12f };
            Color[] awning = { new Color(0.95f, 0.3f, 0.35f), new Color(0.3f, 0.7f, 0.95f), new Color(1f, 0.8f, 0.2f), new Color(0.4f, 0.85f, 0.5f) };
            for (int i = 0; i < xs.Length; i++)
            {
                Vector3 p = new Vector3(xs[i], LandTop, QuayZ - 22f);
                var stand = new GameObject("Ice Cream Stand").transform;
                stand.SetParent(root, false);
                stand.localPosition = p;

                Prim(stand, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.6f, 0f), new Vector3(3.6f, 1.2f, 1.8f), Color.white, "wood", true);
                Prim(stand, PrimitiveType.Cube, "Back", new Vector3(0f, 1.8f, -0.8f), new Vector3(3.6f, 3f, 0.2f), new Color(0.98f, 0.9f, 0.8f), "wood", true);
                for (int k = 0; k < 2; k++)
                    Prim(stand, PrimitiveType.Cube, "Side", new Vector3(k == 0 ? -1.7f : 1.7f, 1.8f, 0f), new Vector3(0.2f, 3f, 1.8f), new Color(0.98f, 0.9f, 0.8f), "wood", true);

                // The striped awning.
                for (int k = 0; k < 8; k++)
                    Prim(stand, PrimitiveType.Cube, "Awning", new Vector3(-1.75f + k * 0.5f, 3.3f, 0.35f), new Vector3(0.5f, 0.1f, 2.4f), k % 2 == 0 ? awning[i] : Color.white, "quilt", false, new Vector3(18f, 0f, 0f));

                // A big ice cream on the roof: a cone and two scoops.
                var cone = MeshKit.Make("Cone", MeshKit.Cone(0.8f, 2f), new Color(0.9f, 0.65f, 0.3f), "plain", false, stand, new Vector3(0f, 4.5f, -0.2f), Quaternion.Euler(180f, 0f, 0f), Vector3.one);
                Prim(stand, PrimitiveType.Sphere, "Scoop", new Vector3(0f, 4.6f, -0.2f), Vector3.one * 1.7f, new Color(1f, 0.7f, 0.8f), "quilt");
                Prim(stand, PrimitiveType.Sphere, "Scoop", new Vector3(0f, 5.8f, -0.2f), Vector3.one * 1.4f, new Color(0.95f, 0.95f, 0.8f), "quilt");
                Prim(stand, PrimitiveType.Sphere, "Cherry", new Vector3(0f, 6.65f, -0.2f), Vector3.one * 0.4f, new Color(0.9f, 0.1f, 0.15f));

                // Queue of customers.
                var rng = new System.Random(i * 7 + 3);
                for (int k = 0; k < 4; k++)
                    Person(stand, new Vector3(-1f + k * 0.7f, 0f, 2.2f + (k % 2) * 0.6f), 180f, Pick(rng, awning), new Color(0.25f, 0.3f, 0.5f), Skin(rng), false, false);
                Person(stand, new Vector3(0f, 0f, -0.4f), 0f, Color.white, new Color(0.2f, 0.2f, 0.3f), Skin(rng), false, false); // the seller
            }
        }

        // ------------------------------------------------------------------ the people

        static void BuildCrowds(System.Random rng, float r)
        {
            var root = new GameObject("Crowds").transform;
            Color[] shirts = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.8f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.55f, 0.15f), new Color(0.9f, 0.4f, 0.9f), Color.white };

            // Packed crowds right behind the quay edge, by the start and the finish: they cheer.
            foreach (float cx in _crowdXs)
                for (int row = 0; row < 3; row++)
                    for (int i = 0; i < 12; i++)
                    {
                        float x = cx - 10f + i * 1.85f + F(rng, -0.3f, 0.3f);
                        float z = QuayZ - 2.5f - row * 2.2f + F(rng, -0.3f, 0.3f);
                        var p = Person(root, new Vector3(x, LandTop, z), F(rng, -20f, 20f), Pick(rng, shirts), Pick(rng, new Color(0.2f, 0.25f, 0.4f), new Color(0.4f, 0.3f, 0.2f), new Color(0.15f, 0.15f, 0.2f)), Skin(rng), true, false);
                    }

            // A thinner row of onlookers all along the quay and on the promenade.
            for (int i = 0; i < 46; i++)
            {
                float x = F(rng, -r - 30f, r + 30f);
                if (NearCrowd(x, 14f)) continue;
                Person(root, new Vector3(x, LandTop, QuayZ - F(rng, 2f, 6f)), F(rng, -30f, 30f), Pick(rng, shirts), new Color(0.25f, 0.3f, 0.5f), Skin(rng), rng.NextDouble() < 0.6, false);
            }
        }

        static GameObject Person(Transform parent, Vector3 pos, float yaw, Color shirt, Color pants, Color skin, bool cheer, bool sit)
        {
            var root = new GameObject(cheer ? "Cheering Person" : "Person");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float legLift = sit ? 0.15f : 0f;
            Prim(root.transform, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.55f, 0.42f, 0.36f), shirt);
            if (!sit)
                for (int s = -1; s <= 1; s += 2)
                    Prim(root.transform, PrimitiveType.Capsule, "Leg", new Vector3(s * 0.14f, 0.42f, 0f), new Vector3(0.22f, 0.42f, 0.22f), pants);
            else
                for (int s = -1; s <= 1; s += 2)
                    Prim(root.transform, PrimitiveType.Capsule, "Leg", new Vector3(s * 0.14f, 0.55f + legLift, 0.4f), new Vector3(0.22f, 0.4f, 0.22f), pants, "plain", false, new Vector3(90f, 0f, 0f));
            Prim(root.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.75f, 0f), Vector3.one * 0.4f, skin);
            Prim(root.transform, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.9f, -0.03f), new Vector3(0.42f, 0.26f, 0.42f), new Color(0.2f, 0.12f, 0.08f));

            Transform left = null, right = null;
            for (int s = -1; s <= 1; s += 2)
            {
                var pivot = new GameObject(s < 0 ? "Left Arm" : "Right Arm").transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(s * 0.34f, 1.42f, 0f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, s * 8f);
                Prim(pivot, PrimitiveType.Capsule, "Arm", new Vector3(0f, -0.28f, 0f), new Vector3(0.14f, 0.3f, 0.14f), skin);
                if (s < 0) left = pivot; else right = pivot;
            }

            if (cheer)
            {
                var cheerer = root.AddComponent<Cheerer>();
                cheerer.leftArm = left;
                cheerer.rightArm = right;
            }
            return root;
        }

        // ------------------------------------------------------------------ the start and finish arches

        /// <summary>A big inflatable arch across the route (the start arch or the finish arch).</summary>
        public static void Arch(string name, Vector3 center, bool finish, Quaternion rotation)
        {
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(center, rotation);
            for (int side = -1; side <= 1; side += 2)
                Prim(root, PrimitiveType.Capsule, "Arch Pillar", new Vector3(side * 9f, 3.8f, -1.5f), new Vector3(1.4f, 3.6f, 1.4f), finish ? Color.white : new Color(1f, 0.85f, 0.15f), "quilt");
            Prim(root, PrimitiveType.Capsule, "Arch Beam", new Vector3(0f, 7.4f, -1.5f), new Vector3(1.4f, 9.2f, 1.4f), finish ? Color.white : new Color(1f, 0.85f, 0.15f), "quilt", false, new Vector3(0f, 0f, 90f));
            if (finish)
            {
                for (int i = 0; i < 18; i++)
                    for (int row = 0; row < 3; row++)
                        Prim(root, PrimitiveType.Cube, "Banner Tile", new Vector3(-8.5f + i * 1f, 6.2f + row * 0.0f - row * 1f + 0.5f, -1.5f), new Vector3(1f, 1f, 0.1f),
                            (i + row) % 2 == 0 ? Color.white : new Color(0.08f, 0.08f, 0.1f));
            }
            else
            {
                Color[] stripes = { new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.8f, 1f), new Color(0.4f, 0.9f, 0.4f) };
                for (int i = 0; i < 18; i++)
                    for (int row = 0; row < 2; row++)
                        Prim(root, PrimitiveType.Cube, "Banner Stripe", new Vector3(-8.5f + i * 1f, 5.8f - row * 1f, -1.5f), new Vector3(1f, 1f, 0.1f), stripes[(i + row) % stripes.Length]);
            }
        }

        // ------------------------------------------------------------------ the town in the background

        static void BuildTown(System.Random rng)
        {
            var root = new GameObject("Town").transform;
            Color[] walls =
            {
                new Color(0.98f, 0.95f, 0.9f), new Color(0.98f, 0.85f, 0.75f), new Color(0.85f, 0.92f, 0.98f), new Color(0.95f, 0.8f, 0.8f),
                new Color(0.88f, 0.95f, 0.85f), new Color(0.98f, 0.92f, 0.7f), new Color(0.8f, 0.85f, 0.95f), new Color(0.95f, 0.95f, 0.97f),
            };

            for (int row = 0; row < 11; row++)
            {
                float z = QuayZ - 76f - row * 34f;
                float x = -470f;
                while (x < 470f)
                {
                    float w = F(rng, 14f, 28f);
                    float d = F(rng, 18f, 26f);
                    float h = F(rng, 14f, 38f) * (1f + row * 0.12f);
                    if (rng.NextDouble() < 0.18) h = F(rng, 50f, 120f);                    // towers
                    if (row == 0 && Mathf.Abs(x) < 60f) h = F(rng, 40f, 70f);               // the big hotels behind the beach
                    Color c = walls[rng.Next(walls.Length)];

                    var b = Prim(root, PrimitiveType.Cube, "Building", new Vector3(x + w * 0.5f, LandTop + h * 0.5f, z), new Vector3(w, h, d), c, "building");
                    Prim(root, PrimitiveType.Cube, "Roof", new Vector3(x + w * 0.5f, LandTop + h + 0.4f, z), new Vector3(w + 0.8f, 0.8f, d + 0.8f), c * 0.7f);
                    if (rng.NextDouble() < 0.4)
                        Prim(root, PrimitiveType.Cube, "Roof Box", new Vector3(x + w * 0.5f + F(rng, -3f, 3f), LandTop + h + 1.8f, z + F(rng, -3f, 3f)), new Vector3(F(rng, 2f, 5f), 2.4f, F(rng, 2f, 5f)), new Color(0.6f, 0.6f, 0.65f));
                    if (h > 70f)
                        Prim(root, PrimitiveType.Cylinder, "Antenna", new Vector3(x + w * 0.5f, LandTop + h + 8f, z), new Vector3(0.4f, 8f, 0.4f), new Color(0.8f, 0.2f, 0.2f));

                    x += w + F(rng, 4f, 9f);
                }
            }

            // Green hills far behind it all.
            for (int i = -3; i <= 3; i++)
                Prim(null, PrimitiveType.Sphere, "Hill", new Vector3(i * 300f, -150f, QuayZ - 760f + (i % 2) * 40f), new Vector3(420f, 380f, 300f), new Color(0.28f, 0.5f, 0.32f));
        }

        // ------------------------------------------------------------------ breakwaters and the lighthouse

        static void BuildBreakwaters(float r)
        {
            var root = new GameObject("Breakwaters").transform;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side < 0f ? -105f : 95f;
                var rng = new System.Random(side > 0 ? 1 : 2);
                Prim(root, PrimitiveType.Cube, "Breakwater", new Vector3(x, 0.8f, 45f), new Vector3(10f, 3.6f, 170f), new Color(0.6f, 0.58f, 0.58f), "stone", true);
                for (int i = 0; i < 40; i++)
                    Prim(root, PrimitiveType.Sphere, "Boulder", new Vector3(x + F(rng, -7f, 7f), F(rng, 0f, 1.5f), F(rng, -35f, 125f)), Vector3.one * F(rng, 2f, 4.5f), new Color(0.5f, 0.48f, 0.5f), "stone");
            }

            // The lighthouse at the end of the east breakwater.
            Vector3 p = new Vector3(95f, 2.6f, 125f);
            for (int i = 0; i < 5; i++)
                Prim(root, PrimitiveType.Cylinder, "Lighthouse", p + new Vector3(0f, 3f + i * 6f, 0f), new Vector3(7f - i * 0.7f, 3f, 7f - i * 0.7f), i % 2 == 0 ? Color.white : new Color(0.9f, 0.2f, 0.2f), "plain", true);
            Prim(root, PrimitiveType.Cylinder, "Gallery", p + new Vector3(0f, 31.5f, 0f), new Vector3(6f, 0.4f, 6f), new Color(0.2f, 0.2f, 0.25f));
            var lamp = Prim(root, PrimitiveType.Sphere, "Lamp", p + new Vector3(0f, 34f, 0f), Vector3.one * 4f, new Color(1f, 0.95f, 0.7f));
            GrayboxMaterials.TintGlow(lamp, new Color(1f, 0.95f, 0.7f), 1.2f);
            Prim(root, PrimitiveType.Sphere, "Lantern Roof", p + new Vector3(0f, 37.5f, 0f), new Vector3(5f, 2.4f, 5f), new Color(0.9f, 0.2f, 0.2f));
        }

        // ------------------------------------------------------------------ ferris wheel on the shore

        static void BuildFerrisWheel(float r)
        {
            Vector3 centre = new Vector3(60f, LandTop + 28f, QuayZ - 86f);
            var root = new GameObject("Ferris Wheel").transform;
            root.position = centre;

            for (int side = -1; side <= 1; side += 2)
            {
                Prim(root, PrimitiveType.Capsule, "Wheel Leg", new Vector3(side * 12f, -14f, 0f), new Vector3(1.8f, 15f, 1.8f), new Color(0.9f, 0.9f, 0.95f), "quilt", false, new Vector3(0f, 0f, -side * -24f));
                Prim(root, PrimitiveType.Capsule, "Wheel Leg", new Vector3(side * 12f, -14f, 0f), new Vector3(1.8f, 15f, 1.8f), new Color(0.9f, 0.9f, 0.95f), "quilt", false, new Vector3(0f, 0f, side * 24f));
            }

            var wheel = new GameObject("Wheel").transform;
            wheel.SetParent(root, false);
            MeshKit.Make("Rim", MeshKit.Torus(24f, 0.7f, 48, 8), new Color(0.95f, 0.35f, 0.4f), "quilt", false, wheel, Vector3.zero, Quaternion.identity, Vector3.one);
            MeshKit.Make("Rim Inner", MeshKit.Torus(14f, 0.4f, 40, 8), new Color(1f, 0.85f, 0.2f), "quilt", false, wheel, Vector3.zero, Quaternion.identity, Vector3.one);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Prim(wheel, PrimitiveType.Cylinder, "Spoke", dir * 12f, new Vector3(0.25f, 12f, 0.25f), Color.white, "plain", false, new Vector3(0f, 0f, a * Mathf.Rad2Deg - 90f));
                Color[] cabs = { new Color(0.3f, 0.8f, 1f), new Color(1f, 0.55f, 0.15f), new Color(0.5f, 1f, 0.5f), new Color(0.9f, 0.4f, 0.9f) };
                Prim(wheel, PrimitiveType.Cube, "Gondola", dir * 24f + Vector3.down * 1.4f, new Vector3(2.8f, 2.2f, 2.8f), cabs[i % 4], "quilt");
            }
            Prim(root, PrimitiveType.Sphere, "Hub", Vector3.zero, Vector3.one * 3f, new Color(0.95f, 0.85f, 0.2f));
            var spin = wheel.gameObject.AddComponent<Spinner>();
            spin.axis = Vector3.forward;
            spin.degreesPerSecond = 5f;
        }

        // ------------------------------------------------------------------ hidden treasure chests

        public static void PlaceChest(string id, Vector3 position, float yaw) => Chest(id, position, yaw, false);

        /// <summary>A treasure chest to find. (Press E next to it.)</summary>
        public static void Chest(string id, Vector3 position, float yaw, bool islet)
        {
            if (islet)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Islet";
                disc.transform.position = new Vector3(position.x, 0f, position.z);
                disc.transform.localScale = new Vector3(5.5f, 0.6f, 5.5f);
                GrayboxMaterials.TintQuilted(disc, new Color(0.1f, 0.78f, 0.8f));
                position.y = 0.6f;
            }

            var wood = new Color(0.45f, 0.27f, 0.12f);
            var gold = new Color(1f, 0.8f, 0.2f);
            var root = new GameObject("Chest (" + id + ")");
            root.transform.position = position;

            var body = Prim(root.transform, PrimitiveType.Cube, "Chest Body", new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1f, 1f), wood, "wood", true);
            Prim(root.transform, PrimitiveType.Cube, "Chest Band", new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 0.2f, 1.1f), gold);

            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(0f, 1f, -0.5f);
            Prim(hinge, PrimitiveType.Cube, "Chest Lid", new Vector3(0f, 0.2f, 0.5f), new Vector3(1.6f, 0.4f, 1f), wood * 1.1f, "wood");
            Prim(root.transform, PrimitiveType.Sphere, "Coins", new Vector3(0f, 0.95f, 0f), new Vector3(1.3f, 0.2f, 0.8f), gold);

            var chest = root.AddComponent<HiddenChest>();
            chest.chestId = id;
            chest.amount = 1;
            chest.lid = hinge;

            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f); // turned last, so everything built above turns with it
        }
    }
}
