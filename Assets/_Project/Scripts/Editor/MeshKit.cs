using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Procedural meshes for things the basic shapes cannot make: cones, rings, arched tunnels and lumpy cushions.
    /// Every mesh is saved as an asset (so scenes keep it) and made double-sided, so it shows from both sides and the
    /// collider works whichever way the triangles happen to wind.
    /// </summary>
    public static class MeshKit
    {
        const string Folder = "Assets/_Project/Art/Environment/Course";
        static int _count;

        /// <summary>Start fresh: removes the meshes of an earlier build.</summary>
        public static void Reset()
        {
            if (Directory.Exists(Folder)) AssetDatabase.DeleteAsset(Folder);
            Directory.CreateDirectory(Folder);
            _count = 0;
        }

        static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            // A second, reversed copy of everything makes the mesh double-sided.
            int n = vertices.Count;
            var allVertices = new List<Vector3>(vertices);
            allVertices.AddRange(vertices);
            var allUvs = new List<Vector2>(uvs);
            allUvs.AddRange(uvs);
            var allTriangles = new List<int>(triangles);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                allTriangles.Add(triangles[i] + n);
                allTriangles.Add(triangles[i + 2] + n);
                allTriangles.Add(triangles[i + 1] + n);
            }

            var mesh = new Mesh { name = name, indexFormat = allVertices.Count > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(allVertices);
            mesh.SetUVs(0, allUvs);
            mesh.SetTriangles(allTriangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(mesh, Folder + "/" + name + "_" + (_count++) + ".asset");
            return mesh;
        }

        /// <summary>Puts a mesh in the world as an object, with a collider if wanted.</summary>
        public static GameObject Make(string name, Mesh mesh, Color color, string kind, bool collider, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            switch (kind)
            {
                case "quilt": GrayboxMaterials.TintQuilted(go, color); break;
                case "wood": GrayboxMaterials.TintWood(go, color); break;
                case "stone": GrayboxMaterials.TintStone(go, color); break;
                case "glass": GrayboxMaterials.TintWater(go, color); break;
                default: GrayboxMaterials.Tint(go, color); break;
            }
            return go;
        }

        // ------------------------------------------------------------------ cone

        /// <summary>A cone standing on its base at y = 0, point at y = height.</summary>
        public static Mesh Cone(float radius, float height, int segments = 20)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            v.Add(new Vector3(0f, height, 0f)); uv.Add(new Vector2(0.5f, 1f));
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                v.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius)); uv.Add(new Vector2(i / (float)segments, 0f));
            }
            for (int i = 1; i <= segments; i++) { t.Add(0); t.Add(i + 1); t.Add(i); }
            int centre = v.Count; v.Add(Vector3.zero); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 1; i <= segments; i++) { t.Add(centre); t.Add(i); t.Add(i + 1); }
            return Finish("Cone", v, uv, t);
        }

        // ------------------------------------------------------------------ ring (torus), lying in the XY plane around the Z axis

        public static Mesh Torus(float radius, float tube, int around = 32, int sides = 12)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= around; i++)
            {
                float a = i * Mathf.PI * 2f / around;
                Vector3 centre = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                Vector3 outward = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                for (int j = 0; j <= sides; j++)
                {
                    float b = j * Mathf.PI * 2f / sides;
                    v.Add(centre + outward * (Mathf.Cos(b) * tube) + Vector3.forward * (Mathf.Sin(b) * tube));
                    uv.Add(new Vector2(i / (float)around * 6f, j / (float)sides));
                }
            }
            int row = sides + 1;
            for (int i = 0; i < around; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * row + j, b = a + row;
                    t.Add(a); t.Add(b); t.Add(a + 1);
                    t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            return Finish("Ring", v, uv, t);
        }

        // ------------------------------------------------------------------ tunnel: a thick arched shell along Z, base at y = 0

        public static Mesh Tunnel(float halfWidth, float height, float length, float thickness = 0.45f, int segments = 18)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float z0 = -length * 0.5f, z1 = length * 0.5f;

            Vector3 Arc(float a, float hw, float h) => new Vector3(Mathf.Cos(a) * hw, Mathf.Sin(a) * h, 0f);

            // Outer and inner arches from the right foot, over the top, to the left foot.
            void Strip(float hw, float h)
            {
                int start = v.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float a = Mathf.PI * i / segments;
                    Vector3 p = Arc(a, hw, h);
                    v.Add(new Vector3(p.x, p.y, z0)); uv.Add(new Vector2(i / (float)segments * 4f, 0f));
                    v.Add(new Vector3(p.x, p.y, z1)); uv.Add(new Vector2(i / (float)segments * 4f, 4f));
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = start + i * 2;
                    t.Add(a); t.Add(a + 1); t.Add(a + 2);
                    t.Add(a + 2); t.Add(a + 1); t.Add(a + 3);
                }
            }
            Strip(halfWidth, height);
            Strip(halfWidth - thickness, height - thickness);

            // The two open ends: a rim joining the outer and inner arch.
            foreach (float z in new[] { z0, z1 })
            {
                int start = v.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float a = Mathf.PI * i / segments;
                    Vector3 o = Arc(a, halfWidth, height), n = Arc(a, halfWidth - thickness, height - thickness);
                    v.Add(new Vector3(o.x, o.y, z)); uv.Add(new Vector2(i / (float)segments, 0f));
                    v.Add(new Vector3(n.x, n.y, z)); uv.Add(new Vector2(i / (float)segments, 1f));
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = start + i * 2;
                    t.Add(a); t.Add(a + 1); t.Add(a + 2);
                    t.Add(a + 2); t.Add(a + 1); t.Add(a + 3);
                }
            }
            return Finish("Tunnel", v, uv, t);
        }

        // ------------------------------------------------------------------ lumpy cushion (a height field), centred on the origin

        /// <summary>
        /// A solid pad of the given size. The top surface follows <paramref name="top"/>(x, z) (a height above y = 0), and
        /// the sides drop down to <paramref name="baseY"/>.
        /// </summary>
        public static Mesh HeightPad(string name, float width, float length, System.Func<float, float, float> top, float baseY, float cell = 0.5f)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            int nx = Mathf.CeilToInt(width / cell) + 1, nz = Mathf.CeilToInt(length / cell) + 1;
            float sx = width / (nx - 1), sz = length / (nz - 1);

            Vector3 Top(int i, int j)
            {
                float x = -width * 0.5f + i * sx, z = -length * 0.5f + j * sz;
                return new Vector3(x, top(x, z), z);
            }

            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    Vector3 p = Top(i, j);
                    v.Add(p); uv.Add(new Vector2(p.x / 3f, p.z / 3f));
                }
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }

            // Sides: for every edge of the grid, a wall from the top down to baseY.
            void Wall(List<Vector3> edge)
            {
                int start = v.Count;
                foreach (var p in edge)
                {
                    v.Add(p); uv.Add(new Vector2(p.x / 3f + p.z / 3f, p.y / 3f));
                    v.Add(new Vector3(p.x, baseY, p.z)); uv.Add(new Vector2(p.x / 3f + p.z / 3f, baseY / 3f));
                }
                for (int i = 0; i < edge.Count - 1; i++)
                {
                    int a = start + i * 2;
                    t.Add(a); t.Add(a + 1); t.Add(a + 2);
                    t.Add(a + 2); t.Add(a + 1); t.Add(a + 3);
                }
            }
            var south = new List<Vector3>(); var north = new List<Vector3>(); var west = new List<Vector3>(); var east = new List<Vector3>();
            for (int i = 0; i < nx; i++) { south.Add(Top(i, 0)); north.Add(Top(i, nz - 1)); }
            for (int j = 0; j < nz; j++) { west.Add(Top(0, j)); east.Add(Top(nx - 1, j)); }
            Wall(south); Wall(north); Wall(west); Wall(east);

            return Finish(name, v, uv, t);
        }
    }
}
