using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Saved-to-disk gray-box materials, shared by the scene builders.
    ///
    /// Every material is made by copying what Unity itself puts on a brand-new primitive. That is always right for
    /// whichever render pipeline the project is using at that moment (the built-in renderer or URP), so it can never
    /// come out pink because of a shader we picked. Materials that already exist but use a different shader (for
    /// example after the render pipeline changed) are switched over in place.
    /// </summary>
    public static class GrayboxMaterials
    {
        const string Folder = "Assets/_Project/Art/Materials";

        public static void Tint(GameObject go, Color color) => Apply(go, color, false, 0f, "grain");

        /// <summary>Puffy inflatable look: a quilted pillow pattern (waterpark decks and slides).</summary>
        public static void TintQuilted(GameObject go, Color color) => Apply(go, color, false, 0f, "quilt");

        /// <summary>Rough stone look (rocks, walls, altars).</summary>
        public static void TintStone(GameObject go, Color color) => Apply(go, color, false, 0f, "stone");

        /// <summary>Wood grain (docks, bridges, huts, ships).</summary>
        public static void TintWood(GameObject go, Color color) => Apply(go, color, false, 0f, "wood");

        /// <summary>A building with rows of windows (the grid repeats every three metres up the wall).</summary>
        public static void TintBuilding(GameObject go, Color color) => Apply(go, color, false, 0f, "building");

        /// <summary>Sand: fine speckle.</summary>
        public static void TintSand(GameObject go, Color color) => Apply(go, color, false, 0f, "sand");

        /// <summary>A colour that glows (bioluminescent plants, the monster's eyes). Strength 1 is a soft glow, 3 is bright.</summary>
        public static void TintGlow(GameObject go, Color color, float strength = 1.5f) => Apply(go, color, false, strength, null);

        /// <summary>Semi-transparent material for water, markers and the like.</summary>
        public static void TintWater(GameObject go, Color color) => Apply(go, color, true, 0f, null);

        /// <summary>Kept for old callers. (The slide's mesh is double-sided in the mesh itself.)</summary>
        public static void TintDoubleSided(GameObject go, Color color) => Apply(go, color, false, 0f, "grain");

        /// <summary>
        /// A material with a generated, tiling texture: mottled living flesh with veins (or, for the ground, a pitted
        /// version). The mesh needs UV coordinates.
        /// </summary>
        public static void TintTextured(GameObject go, Color color, bool ground)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Badeland_" + (ground ? "CavernGround" : "CavernFlesh") + CavernSuffix + ".mat";

            // Many objects share one material: make it once per build (deleting and remaking it for every wall piece
            // left the earlier pieces pointing at a deleted material, which shows as magenta).
            var cached = ground ? _cavernGround : _cavernFlesh;
            if (cached != null && AssetDatabase.Contains(cached))
            {
                go.GetComponent<Renderer>().sharedMaterial = cached;
                return;
            }
            AssetDatabase.DeleteAsset(path);

            Material mat = new Material(Template());
            SetColor(mat, color);
            string textureProperty = TextureProperty(mat);
            if (textureProperty != null) mat.SetTexture(textureProperty, FleshTexture(ground));
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.55f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);
            if (ground) _cavernGround = mat; else _cavernFlesh = mat;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material _cavernGround, _cavernFlesh;

        /// <summary>Added to the cavern material names, so each room keeps its own and building one never breaks another.</summary>
        public static string CavernSuffix = "";

        /// <summary>Forget the shared cavern materials, so the next build makes fresh ones.</summary>
        public static void ResetTexturedMaterials() { _cavernGround = null; _cavernFlesh = null; }

        // Soft generated textures that are multiplied with a colour: grain, quilted pillows, stone, wood.
        static Texture2D DetailTexture(string kind)
        {
            string folder = "Assets/_Project/Art/Textures";
            string path = folder + "/Detail_" + kind + "_v1.png";
            Directory.CreateDirectory(folder);

            if (!File.Exists(path))
            {
                const int size = 128;
                const float tau = Mathf.PI * 2f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)size, v = y / (float)size;
                        float hash = Hash(x, y);
                        float blotch = 0.5f + (Mathf.Sin(tau * 2f * u + 1.7f * Mathf.Sin(tau * 3f * v)) + Mathf.Sin(tau * 3f * v + 1.3f * Mathf.Sin(tau * 2f * u + 1f))) * 0.25f;
                        float value;

                        switch (kind)
                        {
                            case "quilt":
                            {
                                // Pillow baffles: 4 x 4 cushions per tile, bright in the middle, dark in the seams.
                                float fu = Mathf.Repeat(u * 4f, 1f), fv = Mathf.Repeat(v * 4f, 1f);
                                float pillow = Mathf.Sin(Mathf.PI * fu) * Mathf.Sin(Mathf.PI * fv);
                                value = Mathf.Lerp(0.62f, 1f, Mathf.Pow(pillow, 0.6f)) - 0.03f * hash;
                                break;
                            }
                            case "stone":
                            {
                                float cracks = Mathf.Abs(Mathf.Sin(tau * (3f * u + 0.7f * Mathf.Sin(tau * 2f * v))));
                                float crack = 1f - Mathf.SmoothStep(0f, 0.06f, cracks);
                                value = 0.78f + 0.12f * blotch - 0.35f * crack + 0.1f * (hash - 0.5f);
                                break;
                            }
                            case "wood":
                            {
                                float rings = 0.5f + 0.5f * Mathf.Sin(tau * (8f * v + 0.6f * Mathf.Sin(tau * u)));
                                float board = Mathf.Abs(Mathf.Sin(tau * 2f * u));
                                float seam = 1f - Mathf.SmoothStep(0f, 0.05f, board);
                                value = 0.7f + 0.2f * rings - 0.3f * seam + 0.06f * (hash - 0.5f);
                                break;
                            }
                            case "building":
                            {
                                // A wall with two columns and two rows of windows per tile.
                                float fu = Mathf.Repeat(u * 2f, 1f), fv = Mathf.Repeat(v * 2f, 1f);
                                bool window = fu > 0.2f && fu < 0.8f && fv > 0.25f && fv < 0.75f;
                                bool frame = fu > 0.15f && fu < 0.85f && fv > 0.2f && fv < 0.8f;
                                value = window ? 0.3f + 0.1f * blotch : frame ? 0.55f : 0.93f - 0.05f * (hash - 0.5f);
                                break;
                            }
                            case "sand":
                                value = 0.9f + 0.06f * blotch + 0.12f * (hash - 0.5f);
                                break;
                            default: // grain
                                value = 0.84f + 0.1f * blotch + 0.08f * (hash - 0.5f);
                                break;
                        }

                        value = Mathf.Clamp01(value);
                        tex.SetPixel(x, y, new Color(value, value, value, 1f));
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static Texture2D FleshTexture(bool ground)
        {
            string folder = "Assets/_Project/Art/Textures";
            string path = folder + (ground ? "/CavernGround.png" : "/CavernFlesh.png");
            Directory.CreateDirectory(folder);

            const int size = 256;
            const float tau = Mathf.PI * 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    // Whole numbers of cycles, so the tile repeats without a seam.
                    float mottle = Mathf.Sin(tau * (2f * u) + 1.3f * Mathf.Sin(tau * 3f * v))
                                 + Mathf.Sin(tau * (3f * v) + 1.1f * Mathf.Sin(tau * 2f * u + 1f))
                                 + Mathf.Sin(tau * (u + v) + 0.8f * Mathf.Sin(tau * 5f * u));
                    float shade = Mathf.Lerp(0.6f, 1f, 0.5f + mottle / 6f);

                    float veinA = Mathf.Abs(Mathf.Sin(tau * (3f * u + 0.5f * Mathf.Sin(tau * 2f * v))));
                    float veinB = Mathf.Abs(Mathf.Sin(tau * (4f * v + 0.4f * Mathf.Sin(tau * 3f * u + 2f))));
                    float vein = Mathf.Max(1f - Mathf.SmoothStep(0f, 0.09f, veinA), 1f - Mathf.SmoothStep(0f, 0.07f, veinB));

                    float fine = Mathf.Sin(tau * 11f * u + 2f * Mathf.Sin(tau * 7f * v)) * Mathf.Sin(tau * 9f * v + 1f); // pits and pores
                    float pits = ground ? Mathf.Clamp01(fine * 0.5f + 0.1f) : Mathf.Clamp01(fine * 0.3f);

                    Color c = new Color(shade, shade, shade, 1f);
                    c = Color.Lerp(c, new Color(0.55f, 0.2f, 0.28f), vein * (ground ? 0.35f : 0.6f));
                    c = Color.Lerp(c, new Color(0.35f, 0.3f, 0.32f), pits * 0.5f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>A see-through blue for small bodies of water (the secret room's flood).</summary>
        public static void ApplyWater(GameObject go, Color tint) => TintWater(go, tint);

        /// <summary>
        /// The big sea: a solid, bright blue with white ripple lines (a generated texture that SeaMotion scrolls).
        /// </summary>
        public static void ApplySea(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Badeland_Sea.mat";
            AssetDatabase.DeleteAsset(path); // rebuilt every time, so an old broken version never lingers

            Material mat = new Material(Template());
            mat.name = "M_Badeland_Sea";
            Texture2D ripples = SeaRippleTexture();

            string textureProperty = TextureProperty(mat);
            if (textureProperty != null)
            {
                mat.SetTexture(textureProperty, ripples);
                mat.SetTextureScale(textureProperty, new Vector2(110f, 110f)); // a tile every few metres
            }

            SetColor(mat, Color.white);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.8f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);

            renderer.sharedMaterial = mat;
        }

        static void Apply(GameObject go, Color color, bool transparent, float glow, string textureKind)
        {
            Directory.CreateDirectory(Folder);

            // Textured materials tile once per roughly three metres, so big and small objects have the same grain.
            int tiling = 1, tilingY = 1;
            if (textureKind != null)
            {
                Vector3 s = go.transform.lossyScale;
                tiling = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)) / 3f), 1, textureKind == "sand" ? 60 : 12);
                tilingY = textureKind == "building" ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(s.y) / 3f), 1, 40) : tiling;
            }
            string kindTag = textureKind != null ? textureKind + tiling + "x" + tilingY + "_" : "";
            string path = Folder + "/M_Graybox_" + (transparent ? "Water_" : "") + (glow > 0f ? "Glow" + Mathf.RoundToInt(glow * 10f) + "_" : "") + kindTag + ColorUtility.ToHtmlStringRGBA(color) + ".mat";

            var renderer = go.GetComponent<Renderer>();
            Shader shader = Template().shader;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = mat == null;
            if (create) mat = new Material(Template());
            else if (mat.shader != shader) mat.shader = shader; // repair: it was made for a different render pipeline

            SetColor(mat, color);
            if (textureKind != null)
            {
                string textureProperty = TextureProperty(mat);
                if (textureProperty != null)
                {
                    mat.SetTexture(textureProperty, DetailTexture(textureKind));
                    mat.SetTextureScale(textureProperty, new Vector2(tiling, tilingY));
                }
            }
            if (transparent) MakeTransparent(mat);
            if (glow > 0f) MakeGlow(mat, color, glow);

            if (create) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);

            renderer.sharedMaterial = mat;
        }

        // ------------------------------------------------------------------ helpers

        // What Unity puts on a new primitive in this project: valid for the active render pipeline.
        static Material _template;

        static Material Template()
        {
            if (_template != null) return _template;

            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _template = probe.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(probe);
            return _template;
        }

        static void SetColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        static string TextureProperty(Material mat)
        {
            if (mat.HasProperty("_BaseMap")) return "_BaseMap";
            if (mat.HasProperty("_MainTex")) return "_MainTex";
            return null;
        }

        // Makes a material give off light of its own colour.
        static void MakeGlow(Material mat, Color color, float strength)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * strength);
        }

        // Switches a material to alpha blending, in the way the active shader expects.
        static void MakeTransparent(Material mat)
        {
            if (mat.HasProperty("_Mode"))
            {
                // The built-in Standard shader: "Fade/Transparent" mode.
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (mat.HasProperty("_Surface"))
            {
                // URP Lit: what the "Surface Type: Transparent" dropdown sets.
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        // A seamless tile: soft blue with thin, wobbly white lines where several waves cancel out.
        static Texture2D SeaRippleTexture()
        {
            string folder = "Assets/_Project/Art/Textures";
            string path = folder + "/SeaRipples.png";
            Directory.CreateDirectory(folder);

            if (!File.Exists(path))
            {
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var shallow = new Color(0.14f, 0.68f, 1f, 1f);
                var deep = new Color(0.05f, 0.42f, 0.95f, 1f);
                const float tau = Mathf.PI * 2f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)size, v = y / (float)size;
                        // Whole numbers of cycles in each direction, so the tile repeats without a seam.
                        float field = Mathf.Sin(tau * 3f * u) + Mathf.Sin(tau * 3f * v + 1f)
                                    + Mathf.Sin(tau * (2f * u + 2f * v)) + Mathf.Sin(tau * (2f * u - 3f * v) + 2f);
                        float lines = 1f - Mathf.SmoothStep(0f, 0.4f, Mathf.Abs(field));
                        float shade = 0.5f + 0.25f * (Mathf.Sin(tau * u) + Mathf.Sin(tau * v)); // gentle light and dark patches
                        Color c = Color.Lerp(deep, shallow, shade);
                        c = Color.Lerp(c, Color.white, lines * 0.6f);
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
