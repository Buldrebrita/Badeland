using System.Collections.Generic;
using Badeland.World;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// The rich, glowing look of the inside of the creature (shared by every Chapter 2 room): a colour wash of big
    /// coloured lights, glowing patches and hanging vines on the walls, stalactites, giant glowing trees, strings of
    /// lanterns, ancient ruins, waterfalls that pour down the walls, floating spores and big drifting jellyfish.
    /// </summary>
    public static partial class ChapterTwoBuilder
    {
        static Mesh _coneMesh;

        static readonly Color[] GlowPalette =
        {
            new Color(0.2f, 0.95f, 0.95f), new Color(0.95f, 0.3f, 0.85f), new Color(0.55f, 0.4f, 1f),
            new Color(1f, 0.65f, 0.25f), new Color(0.4f, 1f, 0.55f),
        };

        // ------------------------------------------------------------------ helpers

        // A primitive in the world, with the right look (plain, glowing, see-through, or both).
        static GameObject D(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color,
            float glow = 0f, bool transparent = false, bool solid = false, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (transparent && glow > 0f) GrayboxMaterials.TintGlowTransparent(go, color, glow);
            else if (transparent) GrayboxMaterials.TintWater(go, color);
            else if (glow > 0f) GrayboxMaterials.TintGlow(go, color, glow);
            else GrayboxMaterials.Tint(go, color);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        // A soft halo of light around something glowing (a big see-through ball).
        static void Halo(Transform parent, Vector3 pos, Color color, float diameter, float alpha = 0.1f)
        {
            var halo = D(parent, PrimitiveType.Sphere, "Halo", pos, Vector3.one * diameter, new Color(color.r, color.g, color.b, alpha), 0f, true);
            halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static GameObject Rod(Transform parent, Vector3 a, Vector3 b, float thickness, Color color, float glow = 0f)
        {
            Vector3 d = b - a;
            return D(parent, PrimitiveType.Cylinder, "Rod", (a + b) * 0.5f, new Vector3(thickness, d.magnitude * 0.5f, thickness), color, glow, false, false,
                Quaternion.FromToRotation(Vector3.up, d.normalized));
        }

        static Vector3 InwardAt(Vector3 wall)
        {
            Vector3 inward = new Vector3(Cx - wall.x, 0f, -wall.z);
            return inward.sqrMagnitude < 0.001f ? Vector3.right : inward.normalized;
        }

        // A free spot out towards the edge of the cavern.
        static bool PickSpot(System.Random rng, float margin, float minRho, out Vector3 spot)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                float x = Cx - Rx + (float)rng.NextDouble() * Rx * 2f;
                float z = -Rz + (float)rng.NextDouble() * Rz * 2f;
                if (!FreeSpot(x, z, margin)) continue;
                float nx = (x - Cx) / Rx, nz = z / Rz;
                float rho = Mathf.Sqrt(nx * nx + nz * nz) / Scale(Mathf.Atan2(nz, nx));
                if (rho < minRho) continue;
                spot = new Vector3(x, Height(x, z), z);
                return true;
            }
            spot = Vector3.zero;
            return false;
        }

        static float F2(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ------------------------------------------------------------------ the whole look

        static void Decor(System.Random rng, List<Light> lights, Vector3[] underwaterLights)
        {
            _coneMesh = MeshKit.Cone(1f, 1f, 16);

            // Brighter, more colourful mood than before.
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.48f);
            RenderSettings.fogColor = new Color(0.07f, 0.08f, 0.2f);
            RenderSettings.fogDensity = 0.006f;

            BuildColorWash(lights, underwaterLights);
            BuildGlowPatches(rng);
            BuildVines(rng);
            BuildStalactites(rng);
            BuildGiantTrees(rng, lights, 6);
            BuildLanternStrings(rng, lights);
            BuildSpores(rng);
            BuildWaterfalls(rng);
            BuildRuins(rng, lights, 2);
        }

        // Big coloured lights high up wash the cavern in purple, teal, orange and magenta; cyan light shines up through the water.
        static void BuildColorWash(List<Light> lights, Vector3[] underwater)
        {
            float[] angles = { 0.8f, 2.2f, 3.9f, 5.3f };
            Color[] colors = { new Color(0.6f, 0.35f, 1f), new Color(0.2f, 0.9f, 0.9f), new Color(1f, 0.6f, 0.25f), new Color(1f, 0.3f, 0.7f) };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 wall = WallPos(angles[i], 10f);
                lights.Add(AddGlow(wall + InwardAt(wall) * 7f, colors[i], 2.4f, 48f));
            }
            if (underwater != null)
                foreach (var p in underwater) lights.Add(AddGlow(p, new Color(0.2f, 0.85f, 1f), 3.2f, 17f));
        }

        // Glowing patches on the walls, like luminous moss and glowing eggs, each with a soft halo.
        static void BuildGlowPatches(System.Random rng)
        {
            for (int i = 0; i < 46; i++)
            {
                float a = F2(rng, 0f, Mathf.PI * 2f);
                float h = F2(rng, 1.2f, 13f);
                Vector3 wall = WallPos(a, h);
                Vector3 inward = InwardAt(wall);
                Color c = GlowPalette[rng.Next(GlowPalette.Length)];
                float w = F2(rng, 1.6f, 4.5f), t = F2(rng, 1.2f, 3f);
                var parent = SegmentAt(a);
                Vector3 pos = wall + inward * 0.2f;
                D(parent, PrimitiveType.Sphere, "Glow Patch", pos, new Vector3(w, t, 0.5f), c, 1.8f, false, false, Quaternion.LookRotation(inward));
                Halo(parent, pos + inward * 0.3f, c, Mathf.Max(w, t) * 2.2f, 0.07f);
            }
        }

        // Curtains of vines hanging from the upper walls, with glowing bulbs at the tips.
        static void BuildVines(System.Random rng)
        {
            for (int i = 0; i < 34; i++)
            {
                float a = F2(rng, 0f, Mathf.PI * 2f);
                Vector3 wall = WallPos(a, 14f);
                Vector3 inward = InwardAt(wall);
                Vector3 top = wall + inward * 0.9f;
                float len = F2(rng, 5f, 12f);
                var parent = SegmentAt(a);
                Color green = new Color(0.15f, 0.55f, 0.3f) * F2(rng, 0.8f, 1.3f);
                Rod(parent, top, top - Vector3.up * len + inward * 0.6f, 0.18f, green);
                for (int k = 1; k <= 4; k++)
                {
                    Vector3 p = top - Vector3.up * (len * k / 5f) + inward * (0.6f * k / 5f);
                    D(parent, PrimitiveType.Sphere, "Leaf", p + new Vector3((k % 2 == 0 ? 0.4f : -0.4f), 0f, 0f), new Vector3(1.1f, 0.2f, 0.6f), green * 1.2f, 0f, false, false, Quaternion.Euler(0f, F2(rng, 0f, 180f), 20f));
                }
                Color tip = GlowPalette[rng.Next(GlowPalette.Length)];
                Vector3 end = top - Vector3.up * len + inward * 0.6f;
                D(parent, PrimitiveType.Sphere, "Vine Bulb", end, Vector3.one * 0.6f, tip, 2f);
                Halo(parent, end, tip, 2.4f, 0.1f);
            }
        }

        // Stalactites hanging from the roof, and stalagmites standing at the edges.
        static void BuildStalactites(System.Random rng)
        {
            Color rock = new Color(0.55f, 0.36f, 0.48f);
            for (int i = 0; i < 44; i++)
            {
                float a = F2(rng, 0f, Mathf.PI * 2f);
                float h = F2(rng, 15.8f, 20.5f);
                Vector3 pos = WallPos(a, h);
                float len = F2(rng, 3f, 9f), r = F2(rng, 0.7f, 1.8f);
                MeshKit.Make("Stalactite", _coneMesh, rock * F2(rng, 0.85f, 1.2f), "stone", false, SegmentAt(a), pos, Quaternion.Euler(180f, 0f, 0f), new Vector3(r, len, r));
                if (i % 5 == 0) D(SegmentAt(a), PrimitiveType.Sphere, "Drip Light", pos - Vector3.up * len, Vector3.one * 0.4f, new Color(0.5f, 0.95f, 1f), 2.2f);
            }

            for (int i = 0; i < 26; i++)
            {
                if (!PickSpot(rng, 1.5f, 0.78f, out Vector3 p)) continue;
                float len = F2(rng, 2.5f, 7f), r = F2(rng, 0.8f, 1.9f);
                var cone = MeshKit.Make("Stalagmite", _coneMesh, rock * F2(rng, 0.7f, 1.1f), "stone", false, null, p - Vector3.up * 0.1f, Quaternion.Euler(0f, F2(rng, 0f, 360f), 0f), new Vector3(r, len, r));
                var capsule = cone.AddComponent<CapsuleCollider>();
                capsule.radius = 0.38f; capsule.height = 0.8f; capsule.center = new Vector3(0f, 0.4f, 0f);
            }
        }

        // Giant glowing trees: a thick trunk, glassy glowing canopies, branches with hanging orbs, and a coloured light beneath.
        static void BuildGiantTrees(System.Random rng, List<Light> lights, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!PickSpot(rng, 4f, 0.55f, out Vector3 p)) continue;
                float h = F2(rng, 7f, 10f);
                Color glow = GlowPalette[i % GlowPalette.Length];
                Color bark = new Color(0.36f, 0.22f, 0.3f);
                var root = new GameObject("Giant Tree").transform;
                root.position = p;

                for (int k = 0; k < 3; k++)
                {
                    float d = Mathf.Lerp(2.2f, 1.0f, k / 2f);
                    var seg = D(root, PrimitiveType.Cylinder, "Trunk", p + Vector3.up * (h / 6f + k * h / 3f), new Vector3(d, h / 6f + 0.1f, d), bark * (1f + k * 0.1f), 0f, false, k == 0);
                    GrayboxMaterials.TintWood(seg, bark * (1f + k * 0.1f));
                }
                Vector3 crown = p + Vector3.up * h;
                for (int b = 0; b < 6; b++)
                {
                    float ang = b * Mathf.PI * 2f / 6f + i;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0.55f, Mathf.Sin(ang));
                    Vector3 end = crown + dir.normalized * F2(rng, 3.5f, 5.5f);
                    Rod(root, crown - Vector3.up * 0.6f, end, 0.6f, bark);
                    D(root, PrimitiveType.Sphere, "Orb", end - Vector3.up * 1.2f, Vector3.one * 0.7f, glow, 2.4f);
                    Rod(root, end, end - Vector3.up * 1.2f, 0.06f, glow * 0.8f);
                }
                float[] sizes = { 10f, 7.5f, 5f };
                for (int k = 0; k < 3; k++)
                {
                    var canopy = D(root, PrimitiveType.Sphere, "Canopy", crown + Vector3.up * (k * 1.4f), new Vector3(sizes[k], sizes[k] * 0.45f, sizes[k]), new Color(glow.r, glow.g, glow.b, 0.5f), 0.9f, true);
                    canopy.AddComponent<BreathScale>().amount = 0.03f;
                    canopy.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                Halo(root, crown + Vector3.up, glow, 15f, 0.06f);
                lights.Add(AddGlow(crown - Vector3.up * 1.2f, glow, 2.8f, 22f, root));
            }
        }

        // Strings of warm lanterns slung between two points on the walls, like at a harbour festival.
        static void BuildLanternStrings(System.Random rng, List<Light> lights)
        {
            for (int i = 0; i < 9; i++)
            {
                float a1 = F2(rng, 0f, Mathf.PI * 2f);
                float a2 = a1 + F2(rng, 0.22f, 0.36f);
                float h = F2(rng, 6f, 10f);
                Vector3 w1 = WallPos(a1, h), w2 = WallPos(a2, h + F2(rng, -1f, 1f));
                Vector3 a = w1 + InwardAt(w1) * 0.6f, b = w2 + InwardAt(w2) * 0.6f;
                var parent = SegmentAt(a1);
                Vector3 previous = a;
                const int bulbs = 9;
                for (int k = 1; k <= bulbs; k++)
                {
                    float t = k / (float)bulbs;
                    Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 2.2f);
                    Rod(parent, previous, p, 0.07f, new Color(0.15f, 0.12f, 0.1f));
                    previous = p;
                    if (k < bulbs)
                    {
                        var bulb = D(parent, PrimitiveType.Sphere, "Lantern", p + Vector3.down * 0.3f, Vector3.one * 0.45f, new Color(1f, 0.75f, 0.35f), 2.4f);
                        Halo(parent, bulb.transform.position, new Color(1f, 0.7f, 0.3f), 2.2f, 0.12f);
                    }
                }
                if (i % 2 == 0) lights.Add(AddGlow(Vector3.Lerp(a, b, 0.5f) + Vector3.down * 2f, new Color(1f, 0.7f, 0.35f), 2f, 14f, parent));
            }
        }

        // Floating glowing spores drifting through the air.
        static void BuildSpores(System.Random rng)
        {
            var root = new GameObject("Spores").transform;
            for (int i = 0; i < 100; i++)
            {
                float x = Cx - Rx + 4f + (float)rng.NextDouble() * (Rx * 2f - 8f);
                float z = -Rz + 4f + (float)rng.NextDouble() * (Rz * 2f - 8f);
                if (!Inside(x, z, 4f)) continue;
                Color c = GlowPalette[rng.Next(GlowPalette.Length)];
                var spore = D(root, PrimitiveType.Sphere, "Spore", new Vector3(x, Height(x, z) + F2(rng, 1.5f, 12f), z), Vector3.one * F2(rng, 0.14f, 0.32f), c, 2.6f);
                var drift = spore.AddComponent<Drifter>();
                drift.radius = F2(rng, 1.5f, 4.5f); drift.speed = F2(rng, 0.05f, 0.18f); drift.phase = F2(rng, 0f, 6f); drift.bobAmplitude = F2(rng, 0.4f, 1.1f);
            }
        }

        // Waterfalls pouring down the cavern walls, with foam and mist at the bottom.
        static void BuildWaterfalls(System.Random rng)
        {
            float[] angles = { 0.95f, 2.05f, 3.1f, 4.9f };
            foreach (float a in angles) Waterfall(a, F2(rng, 4f, 7f));
        }

        static void Waterfall(float a, float width)
        {
            Vector3 top = WallPos(a, 15f), bottom = WallPos(a, 0.4f);
            Vector3 inward = InwardAt(bottom);
            Vector3 along = top - bottom;
            Vector3 mid = (top + bottom) * 0.5f + inward * 1.1f;
            var parent = SegmentAt(a);

            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "Waterfall";
            sheet.transform.position = mid;
            sheet.transform.rotation = Quaternion.LookRotation(inward, along.normalized);
            sheet.transform.localScale = new Vector3(width, along.magnitude, 0.35f);
            Object.DestroyImmediate(sheet.GetComponent<Collider>());
            GrayboxMaterials.TintWaterfall(sheet);
            sheet.AddComponent<UVScroll>();
            sheet.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sheet.transform.SetParent(parent, true);

            // Foam and mist where it lands, and a cyan glow.
            float baseX = bottom.x + inward.x * 2.2f, baseZ = bottom.z + inward.z * 2.2f;
            Vector3 floor = new Vector3(baseX, Height(baseX, baseZ) + 0.12f, baseZ);
            D(parent, PrimitiveType.Cylinder, "Waterfall Foam", floor, new Vector3(width * 0.9f, 0.05f, 4f), new Color(0.85f, 1f, 1f), 1.2f);
            for (int i = 0; i < 4; i++)
                Halo(parent, floor + new Vector3(Mathf.Sin(i * 1.7f) * width * 0.3f, 1.5f + i * 0.5f, Mathf.Cos(i * 1.7f) * 1.2f), new Color(0.7f, 0.95f, 1f), 4.5f, 0.09f);
            AddGlow(floor + Vector3.up * 2f, new Color(0.3f, 0.9f, 1f), 2.2f, 17f, parent);
        }

        // Ancient ruins: a stepped platform with broken columns, an arch, glowing runes and a brazier.
        static void BuildRuins(System.Random rng, List<Light> lights, int count)
        {
            for (int n = 0; n < count; n++)
            {
                if (!PickSpot(rng, 7f, 0.4f, out Vector3 c)) continue;
                float yaw = F2(rng, 0f, 360f);
                var root = new GameObject("Ancient Ruins").transform;
                root.position = c;
                root.rotation = Quaternion.Euler(0f, yaw, 0f);
                Color stone = new Color(0.5f, 0.45f, 0.55f);
                Color rune = new Color(0.25f, 0.95f, 0.95f);

                Vector3 L(float x, float y, float z) => root.TransformPoint(new Vector3(x, y, z));
                Solid(root, "Step 1", L(0f, 0.25f, 0f), new Vector3(12f, 0.7f, 12f), stone, root.rotation);
                Solid(root, "Step 2", L(0f, 0.85f, 0f), new Vector3(9f, 0.6f, 9f), stone * 1.08f, root.rotation);

                float[] heights = { 6.5f, 6.5f, 3.5f, 6f, 2.4f, 6.5f };
                for (int k = 0; k < 6; k++)
                {
                    float x = (k % 2 == 0 ? -1f : 1f) * 3.6f, z = (k / 2 - 1) * 3.4f;
                    var column = D(root, PrimitiveType.Cylinder, "Column", L(x, 1.15f + heights[k] * 0.5f, z), new Vector3(1.2f, heights[k] * 0.5f, 1.2f), stone, 0f, false, true);
                    GrayboxMaterials.TintStone(column, stone);
                    D(root, PrimitiveType.Cube, "Rune", L(x, 1.15f + heights[k] * 0.5f, z) + root.rotation * new Vector3(0f, 0f, -0.62f), new Vector3(0.25f, heights[k] * 0.6f, 0.05f), rune, 2.2f, false, false, root.rotation);
                }
                // The arch over the doorway between the two tall columns on one side.
                Solid(root, "Lintel", L(0f, 1.15f + 6.7f, -3.4f), new Vector3(8.6f, 0.9f, 1.4f), stone * 0.95f, root.rotation);
                // A column that has fallen over.
                var fallen = D(root, PrimitiveType.Cylinder, "Fallen Column", L(-1.5f, 2.2f, 5.2f), new Vector3(1.2f, 2.6f, 1.2f), stone * 0.9f, 0f, false, true, root.rotation * Quaternion.Euler(0f, 30f, 90f));
                GrayboxMaterials.TintStone(fallen, stone * 0.9f);

                // A stone brazier in the middle with a glowing flame.
                D(root, PrimitiveType.Cylinder, "Brazier", L(0f, 1.8f, 0f), new Vector3(1.4f, 0.5f, 1.4f), stone * 0.8f, 0f, false, true);
                var flame = D(root, PrimitiveType.Sphere, "Flame", L(0f, 2.9f, 0f), new Vector3(0.9f, 1.4f, 0.9f), new Color(1f, 0.7f, 0.3f), 3f);
                flame.AddComponent<Bobber>().amplitude = 0.08f;
                Halo(root, L(0f, 3f, 0f), new Color(1f, 0.65f, 0.3f), 6f, 0.1f);
                lights.Add(AddGlow(L(0f, 3.4f, 0f), new Color(1f, 0.65f, 0.3f), 2.6f, 18f, root));
            }
        }

        static GameObject Solid(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, Quaternion rotation)
        {
            var go = D(parent, PrimitiveType.Cube, name, pos, scale, color, 0f, false, true, rotation);
            GrayboxMaterials.TintStone(go, color);
            return go;
        }

        // Big, glowing, drifting jellyfish: translucent bells, a bright core, long tentacles, and a light under some of them.
        static void BuildJellyfish(System.Random rng)
        {
            Color[] colors = { new Color(0.35f, 0.95f, 1f), new Color(1f, 0.45f, 0.9f), new Color(0.6f, 1f, 0.7f), new Color(0.7f, 0.5f, 1f), new Color(1f, 0.7f, 0.4f) };
            var lightList = new List<Light>();
            int placed = 0;
            for (int tries = 0; tries < 300 && placed < 18; tries++)
            {
                float x = Cx - Rx + 4f + (float)rng.NextDouble() * (Rx * 2f - 8f);
                float z = -Rz + 4f + (float)rng.NextDouble() * (Rz * 2f - 8f);
                if (!Inside(x, z, 7f)) continue;
                placed++;

                Color c = colors[rng.Next(colors.Length)];
                bool giant = placed % 6 == 0;
                float d = giant ? F2(rng, 7f, 10f) : F2(rng, 2.5f, 5.5f);
                float y = giant ? F2(rng, 11f, 14f) : F2(rng, 5f, 11f);
                Vector3 centre = new Vector3(x, y, z);

                var root = new GameObject("Jellyfish").transform;
                root.position = centre;

                var bell = D(root, PrimitiveType.Sphere, "Bell", centre, new Vector3(d, d * 0.62f, d), new Color(c.r, c.g, c.b, 0.4f), 0.9f, true);
                bell.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                D(root, PrimitiveType.Sphere, "Core", centre + Vector3.down * d * 0.08f, new Vector3(d * 0.42f, d * 0.3f, d * 0.42f), c, 2.2f);
                Halo(root, centre, c, d * 1.9f, 0.07f);

                int tentacles = giant ? 14 : 9;
                for (int t = 0; t < tentacles; t++)
                {
                    float ang = t * Mathf.PI * 2f / tentacles;
                    float rad = d * 0.32f;
                    float len = d * F2(rng, 1.5f, 2.6f);
                    Vector3 top = centre + new Vector3(Mathf.Cos(ang) * rad, -d * 0.25f, Mathf.Sin(ang) * rad);
                    Rod(root, top, top + new Vector3(Mathf.Cos(ang) * 0.4f, -len, Mathf.Sin(ang) * 0.4f), 0.08f + d * 0.01f, new Color(c.r, c.g, c.b), 1.3f);
                }

                bell.AddComponent<BreathScale>().amount = 0.07f;
                var drift = root.gameObject.AddComponent<Drifter>();
                drift.radius = F2(rng, 3f, 8f);
                drift.speed = F2(rng, 0.04f, 0.1f);
                drift.phase = F2(rng, 0f, 6f);
                drift.bobAmplitude = F2(rng, 0.5f, 1.4f);

                if (placed % 3 == 0) AddGlow(centre + Vector3.down * d * 0.4f, c, giant ? 3f : 1.8f, giant ? 26f : 14f, root);
            }
        }
    }
}
