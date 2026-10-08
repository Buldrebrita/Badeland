using System.Collections.Generic;
using Badeland.World;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// The Northern Run: a long extra stretch added to the waterpark course. From the top-west deck the route goes
    /// north across bounce pads, a rotating bar, stepping discs, a windmill and a ferry, turns round on a far island,
    /// and comes back south through a second bar section and more discs to the top-west deck again. Players have to
    /// pass its flag gates in order, so there is no shortcut. Hidden treasure chests are tucked away around it.
    /// </summary>
    public static class CourseExtension
    {
        static readonly Color Pink = new Color(1f, 0.4f, 0.7f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);

        /// <summary>Builds the Northern Run and returns its flag gates, in the order they are passed.</summary>
        public static Checkpoint[] Build()
        {
            // ---- Out: north from the top-west deck.
            S3SceneBuilder.Deck("North Deck 1", new Vector3(-18f, 0f, 27f), new Vector3(6f, 1.2f, 18f));        // z 18 to 36
            for (int i = 0; i < 2; i++) Pad(new Vector3(-18f, 0.7f, 22f + i * 5f));
            Bar(new Vector3(-18f, 1f, 31f), 5f);

            for (int i = 0; i < 3; i++) S3SceneBuilder.Disc("North Disc " + (i + 1), new Vector3(-18f, 0f, 40f + i * 4.8f), 4.4f);  // z 40 to 54

            S3SceneBuilder.Deck("North Island", new Vector3(-18f, 0f, 62f), new Vector3(14f, 1.2f, 14f));      // x -25 to -11, z 55 to 69
            Windmill(new Vector3(-18f, 3.2f, 58f));
            S3SceneBuilder.Ball("Beach Ball", new Vector3(-13f, 1.85f, 66f), 2.6f, new Color(1f, 0.3f, 0.3f), true);
            S3SceneBuilder.Ball("Beach Ball", new Vector3(-22f, 1.85f, 67f), 2.4f, new Color(1f, 0.9f, 0.2f), true);

            // A ferry across the gap to the west turnaround.
            var ferryA = new Vector3(-27.5f, 0f, 62f);
            var ferryB = new Vector3(-38.5f, 0f, 62f);
            var ferry = S3SceneBuilder.Box("North Ferry", ferryA, new Vector3(3.5f, 1.2f, 8f), new Color(1f, 0.55f, 0.1f));
            var platform = ferry.AddComponent<MovingPlatform>();
            platform.pointA = ferryA; platform.pointB = ferryB; platform.period = 8f;
            S3SceneBuilder.Foam(ferry, new Vector3(4.9f, 0.04f, 9.4f));

            S3SceneBuilder.Deck("Turnaround Deck", new Vector3(-46f, 0f, 62f), new Vector3(10f, 1.2f, 14f));   // x -51 to -41, z 55 to 69
            Pad(new Vector3(-46f, 0.7f, 66f));

            // ---- Back: south along a second lane.
            S3SceneBuilder.Deck("North Deck 2", new Vector3(-46f, 0f, 43f), new Vector3(6f, 1.2f, 24f));       // z 31 to 55
            Bar(new Vector3(-46f, 1f, 50f), 5f);
            Bar(new Vector3(-46f, 1f, 38f), 5f);
            for (int i = 0; i < 2; i++) Pad(new Vector3(-46f, 0.7f, 44f + i * 0.0f + (i == 0 ? 0f : 2.5f)));

            for (int i = 0; i < 3; i++) S3SceneBuilder.Disc("Return Disc " + (i + 1), new Vector3(-46f, 0f, 28.2f - i * 4.8f), 4.4f);
            S3SceneBuilder.Deck("Return Deck", new Vector3(-35f, 0f, 14f), new Vector3(22f, 1.2f, 6f));         // x -46 to -24, z 11 to 17
            Pad(new Vector3(-30f, 0.7f, 14f));

            // Bunting on posts along the way, for a festive look.
            for (int i = 0; i < 4; i++) Bunting(new Vector3(-21.5f, 0f, 22f + i * 5.5f), new Vector3(-14.5f, 0f, 22f + i * 5.5f));

            // ---- Flag gates, passed in order.
            var gates = new List<Checkpoint>
            {
                S3SceneBuilder.Gate("Checkpoint N1 (north deck)", new Vector3(-18f, 0f, 36f), 0f, Yellow),
                S3SceneBuilder.Gate("Checkpoint N2 (the island)", new Vector3(-18f, 0f, 59f), 0f, Yellow, 14f),
                S3SceneBuilder.Gate("Checkpoint N3 (turnaround)", new Vector3(-46f, 0f, 58f), 180f, Yellow, 8f),
                S3SceneBuilder.Gate("Checkpoint N4 (return)", new Vector3(-46f, 0f, 33f), 180f, Yellow, 8f),
                S3SceneBuilder.Gate("Checkpoint N5 (return deck)", new Vector3(-34f, 0f, 14f), 90f, Yellow, 8f),
            };

            BuildChests();
            BuildScenery();
            return gates.ToArray();
        }

        // ------------------------------------------------------------------ hidden treasure

        static void BuildChests()
        {
            // Hidden on tiny islets in the open sea (swim to them), behind obstacles, and on the far scenery decks.
            Chest("north-islet", new Vector3(-31f, 0f, 40f), 0f, true);       // a lonely islet west of the first stretch
            Chest("island-corner", new Vector3(-12.4f, 0.6f, 55.8f), 200f, false);   // tucked in the corner of the island
            Chest("turnaround-back", new Vector3(-50f, 0.6f, 68f), 135f, false);    // behind the turnaround
            Chest("south-islet", new Vector3(-36f, 0f, 5f), 20f, true);       // below the return deck
            Chest("far-east", new Vector3(55f, 0.6f, 24f), 270f, false);      // the far scenery deck: swim there
            Chest("far-west", new Vector3(-55f, 0.6f, -20f), 90f, false);
            Chest("far-north", new Vector3(10f, 0.6f, 58f), 180f, false);
        }

        // A little round islet (when asked) with a chest on it, or just the chest.
        static void Chest(string id, Vector3 position, float yaw, bool islet)
        {
            if (islet)
            {
                var disc = S3SceneBuilder.Disc("Islet", new Vector3(position.x, 0f, position.z), 5.5f);
                disc.transform.localScale = new Vector3(5.5f, 0.6f, 5.5f);
                position.y = 0.6f;
            }

            var wood = new Color(0.45f, 0.27f, 0.12f);
            var gold = new Color(1f, 0.8f, 0.2f);
            var root = new GameObject("Chest (" + id + ")");
            root.transform.position = position;

            var body = S3SceneBuilder.Box("Chest Body", position + new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1f, 1f), wood);
            GrayboxMaterials.TintWood(body, wood);
            body.transform.SetParent(root.transform, true);
            var band = S3SceneBuilder.Box("Chest Band", position + new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 0.2f, 1.1f), gold);
            Object.DestroyImmediate(band.GetComponent<Collider>());
            band.transform.SetParent(root.transform, true);

            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(0f, 1f, -0.5f);
            var lid = S3SceneBuilder.Box("Chest Lid", hinge.position + new Vector3(0f, 0.2f, 0.5f), new Vector3(1.6f, 0.4f, 1f), wood * 1.1f);
            GrayboxMaterials.TintWood(lid, wood * 1.1f);
            Object.DestroyImmediate(lid.GetComponent<Collider>());
            lid.transform.SetParent(hinge, true);

            var coins = S3SceneBuilder.Ball("Coins", position + new Vector3(0f, 0.95f, 0f), 1f, gold, false);
            coins.transform.localScale = new Vector3(1.3f, 0.2f, 0.8f);
            coins.transform.SetParent(root.transform, true);

            var chest = root.AddComponent<HiddenChest>();
            chest.chestId = id;
            chest.amount = 1;
            chest.lid = hinge;

            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f); // turned last, so everything built above turns with it
        }

        // ------------------------------------------------------------------ scenery: palm trees, parasols, bunting, lifebuoys

        static void BuildScenery()
        {
            var rng = new System.Random(31);
            // Parasols and palms on the extension decks and the far decks.
            Vector3[] palms = { new Vector3(-21.8f, 0.6f, 66f), new Vector3(-48.5f, 0.6f, 66.5f), new Vector3(-24f, 0.6f, 11.5f), new Vector3(52f, 0.6f, 24f), new Vector3(-52f, 0.6f, -22f), new Vector3(7f, 0.6f, 59f) };
            foreach (var p in palms) Palm(p, rng);

            Parasol(new Vector3(-14f, 0.6f, 58f), new Color(1f, 0.4f, 0.5f));
            Parasol(new Vector3(-43f, 0.6f, 58f), new Color(1f, 0.85f, 0.2f));
            Parasol(new Vector3(58f, 0.6f, 17f), new Color(0.3f, 0.8f, 1f));

            // Lifebuoys floating in the sea beside the route.
            Vector3[] buoys = { new Vector3(-27f, 0f, 30f), new Vector3(-33f, 0f, 48f), new Vector3(-10f, 0f, 45f), new Vector3(-40f, 0f, 20f) };
            foreach (var b in buoys)
            {
                var ring = S3SceneBuilder.Cyl("Lifebuoy", new Vector3(b.x, 0.25f, b.z), new Vector3(2.2f, 0.25f, 2.2f), new Color(1f, 0.3f, 0.25f));
                GrayboxMaterials.TintQuilted(ring, new Color(1f, 0.3f, 0.25f));
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                var hole = S3SceneBuilder.Cyl("Lifebuoy Hole", new Vector3(b.x, 0.3f, b.z), new Vector3(1.0f, 0.27f, 1.0f), new Color(0.1f, 0.4f, 0.9f));
                Object.DestroyImmediate(hole.GetComponent<Collider>());
                ring.AddComponent<Bobber>();
                hole.transform.SetParent(ring.transform, true);
            }
        }

        static void Palm(Vector3 position, System.Random rng)
        {
            var root = new GameObject("Palm Tree").transform;
            float height = 4.5f + (float)rng.NextDouble() * 1.5f;
            var trunkColor = new Color(0.55f, 0.38f, 0.2f);
            Vector3 lean = new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f) * 1.6f;
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                var seg = S3SceneBuilder.Cyl("Trunk", position + Vector3.up * (height * (t + 0.1f)) + lean * t * t, new Vector3(0.55f - 0.07f * i, height * 0.12f, 0.55f - 0.07f * i), trunkColor);
                GrayboxMaterials.TintWood(seg, trunkColor);
                if (i > 0) Object.DestroyImmediate(seg.GetComponent<Collider>());
                seg.transform.SetParent(root, true);
            }

            Vector3 crown = position + Vector3.up * (height * 1.1f) + lean;
            var leaf = new Color(0.2f, 0.75f, 0.3f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                var frond = S3SceneBuilder.Box("Frond", crown + new Vector3(Mathf.Cos(a) * 1.3f, -0.25f, Mathf.Sin(a) * 1.3f), new Vector3(2.8f, 0.08f, 0.7f), leaf);
                frond.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, -18f);
                Object.DestroyImmediate(frond.GetComponent<Collider>());
                frond.transform.SetParent(root, true);
            }
            var nut = S3SceneBuilder.Ball("Coconuts", crown + Vector3.down * 0.2f, 0.7f, new Color(0.35f, 0.22f, 0.1f), false);
            nut.transform.SetParent(root, true);
        }

        static void Parasol(Vector3 position, Color color)
        {
            var pole = S3SceneBuilder.Cyl("Parasol Pole", position + Vector3.up * 1.6f, new Vector3(0.14f, 1.6f, 0.14f), new Color(0.95f, 0.95f, 0.95f));
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            var top = S3SceneBuilder.Ball("Parasol Top", position + Vector3.up * 3.2f, 3.4f, color, false);
            top.transform.localScale = new Vector3(3.4f, 0.8f, 3.4f);
            GrayboxMaterials.TintQuilted(top, color);
            top.transform.SetParent(pole.transform, true);
        }

        static void Bunting(Vector3 a, Vector3 b)
        {
            var pole = S3SceneBuilder.Cyl("Bunting Pole", a + Vector3.up * 2.6f, new Vector3(0.15f, 2.6f, 0.15f), new Color(0.95f, 0.95f, 0.95f));
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            var pole2 = S3SceneBuilder.Cyl("Bunting Pole", b + Vector3.up * 2.6f, new Vector3(0.15f, 2.6f, 0.15f), new Color(0.95f, 0.95f, 0.95f));
            Object.DestroyImmediate(pole2.GetComponent<Collider>());
            Color[] colors = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.8f, 1f), new Color(0.5f, 1f, 0.5f) };
            for (int i = 0; i < 8; i++)
            {
                float t = (i + 0.5f) / 8f;
                Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.up * (4.9f - 0.6f * Mathf.Sin(t * Mathf.PI));
                var flag = S3SceneBuilder.Box("Bunting Flag", pos, new Vector3(0.6f, 0.7f, 0.05f), colors[i % colors.Length]);
                Object.DestroyImmediate(flag.GetComponent<Collider>());
                flag.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                flag.transform.SetParent(pole.transform, true);
            }
        }

        // ------------------------------------------------------------------ obstacles (same kinds as the main loop)

        static void Pad(Vector3 position)
        {
            var pad = S3SceneBuilder.Box("Bounce Pad", position, new Vector3(3f, 0.2f, 3f), Pink);
            pad.GetComponent<BoxCollider>().isTrigger = true;
            pad.AddComponent<BouncePad>();
        }

        static void Bar(Vector3 position, float length)
        {
            var bar = S3SceneBuilder.Box("Rotating Bar", position, new Vector3(length, 0.6f, 0.5f), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
            bar.AddComponent<RotatingBar>();
        }

        static void Windmill(Vector3 position)
        {
            var hub = new GameObject("Windmill");
            hub.transform.position = position;
            var red = new Color(0.95f, 0.3f, 0.35f);
            var armA = S3SceneBuilder.Box("Windmill Arm A", position, new Vector3(6.4f, 0.5f, 0.5f), red);
            var armB = S3SceneBuilder.Box("Windmill Arm B", position, new Vector3(0.5f, 6.4f, 0.5f), red);
            Object.DestroyImmediate(armA.GetComponent<Collider>());
            Object.DestroyImmediate(armB.GetComponent<Collider>());
            armA.transform.SetParent(hub.transform, true);
            armB.transform.SetParent(hub.transform, true);
            var spin = hub.AddComponent<RotatingBar>();
            spin.rotationAxis = Vector3.forward;
            spin.degreesPerSecond = 55f;
            spin.hitBoxes = new[] { armA.transform, armB.transform };
        }
    }
}
