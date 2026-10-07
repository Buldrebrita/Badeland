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

        public static void Tint(GameObject go, Color color) => Apply(go, color, false);

        /// <summary>Semi-transparent material for water, markers and the like.</summary>
        public static void TintWater(GameObject go, Color color) => Apply(go, color, true);

        /// <summary>Kept for old callers. (The slide's mesh is double-sided in the mesh itself.)</summary>
        public static void TintDoubleSided(GameObject go, Color color) => Apply(go, color, false);

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

        static void Apply(GameObject go, Color color, bool transparent)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Graybox_" + (transparent ? "Water_" : "") + ColorUtility.ToHtmlStringRGBA(color) + ".mat";

            var renderer = go.GetComponent<Renderer>();
            Shader shader = Template().shader;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = mat == null;
            if (create) mat = new Material(Template());
            else if (mat.shader != shader) mat.shader = shader; // repair: it was made for a different render pipeline

            SetColor(mat, color);
            if (transparent) MakeTransparent(mat);

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
