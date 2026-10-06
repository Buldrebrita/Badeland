using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.EditorTools
{
    /// <summary>Saved-to-disk gray-box materials, shared by the scene builders.</summary>
    public static class GrayboxMaterials
    {
        const string Folder = "Assets/_Project/Art/Materials";

        public static void Tint(GameObject go, Color color) => Apply(go, color, false, false);

        /// <summary>Semi-transparent material for water surfaces.</summary>
        public static void TintWater(GameObject go, Color color) => Apply(go, color, true, false);

        /// <summary>
        /// A see-through blue for small bodies of water (the secret room's flood). Uses the render pipeline's own
        /// default material, so it can never come out pink.
        /// </summary>
        public static void ApplyWater(GameObject go, Color tint) => TintWater(go, tint);

        /// <summary>
        /// The big sea: a solid, bright blue with white ripple lines (a generated texture that SeaMotion scrolls).
        /// Built on the render pipeline's default material, so it always draws.
        /// </summary>
        public static void ApplySea(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            Texture2D ripples = SeaRippleTexture();

            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Badeland_Sea.mat";
            AssetDatabase.DeleteAsset(path); // always rebuilt, so old versions never linger

            Material mat = NewMaterial(renderer);
            mat.name = "M_Badeland_Sea";
            SetTexture(mat, ripples);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", new Vector2(110f, 110f)); // a tile every few metres
            AssetDatabase.CreateAsset(mat, path);

            renderer.sharedMaterial = mat;
        }

        static void SetTexture(Material mat, Texture2D texture)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
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

        // The URP "Lit" shader. Found by its fixed asset id first (a shader looked up by name can come back empty,
        // which gives a pink material), then by name, then from the render pipeline's default material.
        const string UrpLitGuid = "933532a4fcc9baf4fa0491de14d08ed7";
        static Shader _lit;

        static Shader LitShader()
        {
            if (_lit != null) return _lit;

            string path = AssetDatabase.GUIDToAssetPath(UrpLitGuid);
            if (!string.IsNullOrEmpty(path)) _lit = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (_lit == null && pipeline != null && pipeline.defaultMaterial != null) _lit = pipeline.defaultMaterial.shader;
            return _lit;
        }

        /// <summary>A new material that is guaranteed to use the render pipeline's lit shader.</summary>
        static Material NewMaterial(Renderer renderer)
        {
            Shader lit = LitShader();
            if (lit != null) return new Material(lit);
            return new Material(DefaultMaterial(renderer)); // last resort
        }

        /// <summary>The render pipeline's own default material (URP Lit). Never depends on a shader's name.</summary>
        static Material DefaultMaterial(Renderer renderer)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null) return pipeline.defaultMaterial;
            if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;
            return new Material(Shader.Find("Standard"));
        }

        /// <summary>A solid colour that shows on both sides of a surface (for hand-built meshes like the slide).</summary>
        public static void TintDoubleSided(GameObject go, Color color) => Apply(go, color, false, true);

        static void Apply(GameObject go, Color color, bool transparent, bool doubleSided)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Graybox_" + (transparent ? "Water_" : "") + (doubleSided ? "DS_" : "") + ColorUtility.ToHtmlStringRGBA(color) + ".mat";

            var renderer = go.GetComponent<Renderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = NewMaterial(renderer);

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

                if (doubleSided)
                {
                    if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                    mat.doubleSidedGI = true;
                }

                if (transparent)
                {
                    // URP Lit transparent settings (what the "Surface Type: Transparent" dropdown sets).
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }

                AssetDatabase.CreateAsset(mat, path);
            }

            renderer.sharedMaterial = mat;
        }
    }
}
