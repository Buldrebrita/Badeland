using System.Collections.Generic;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// What is left of a player who died: a heap of bones and a folded piece of clothing. They fall and tumble where the
    /// player stood, then settle and stay there for the rest of the area. (Placeholder shapes, to be replaced by real
    /// bone and clothing models.)
    /// </summary>
    public class Remains : MonoBehaviour
    {
        readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        float _timer;

        public static void Spawn(Vector3 feet, Quaternion facing, Color clothColor, Collider ignore)
        {
            var root = new GameObject("Remains");
            root.transform.position = feet;
            var remains = root.AddComponent<Remains>();
            var bone = new Color(0.9f, 0.88f, 0.78f);

            remains.Piece(PrimitiveType.Sphere, feet + Vector3.up * 1.55f, new Vector3(0.34f, 0.38f, 0.34f), bone, ignore, facing); // skull
            remains.Piece(PrimitiveType.Capsule, feet + Vector3.up * 1.0f, new Vector3(0.1f, 0.35f, 0.1f), bone, ignore, facing);    // spine
            for (int i = 0; i < 3; i++)
                remains.Piece(PrimitiveType.Capsule, feet + Vector3.up * (1.25f - i * 0.12f), new Vector3(0.08f, 0.28f, 0.08f), bone, ignore, facing * Quaternion.Euler(0f, 0f, 90f)); // ribs
            remains.Piece(PrimitiveType.Sphere, feet + Vector3.up * 0.7f, new Vector3(0.3f, 0.15f, 0.2f), bone, ignore, facing);    // pelvis
            foreach (float side in new[] { -1f, 1f })
            {
                remains.Piece(PrimitiveType.Capsule, feet + facing * new Vector3(side * 0.35f, 1.1f, 0f), new Vector3(0.09f, 0.35f, 0.09f), bone, ignore, facing); // arm
                remains.Piece(PrimitiveType.Capsule, feet + facing * new Vector3(side * 0.15f, 0.4f, 0f), new Vector3(0.11f, 0.42f, 0.11f), bone, ignore, facing); // leg
            }
            remains.Piece(PrimitiveType.Cube, feet + Vector3.up * 0.9f, new Vector3(0.75f, 0.08f, 0.55f), clothColor, ignore, facing); // the clothes, for now a folded cloth
        }

        void Piece(PrimitiveType type, Vector3 position, Vector3 scale, Color color, Collider ignore, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Remains Piece";
            go.transform.SetParent(transform, true);
            go.transform.position = position;
            go.transform.rotation = rotation * Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;

            var col = go.GetComponent<Collider>();
            if (ignore != null && ignore.enabled && col != null) Physics.IgnoreCollision(col, ignore);

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            body.linearVelocity = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.5f, 2f), Random.Range(-1.2f, 1.2f));
            body.angularVelocity = Random.insideUnitSphere * 6f;
            _bodies.Add(body);
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < 4f) return;

            // Settled: freeze the pile, and let living players walk straight through it.
            foreach (var body in _bodies)
            {
                if (body == null) continue;
                body.isKinematic = true;
                var col = body.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
            enabled = false;
        }
    }
}
