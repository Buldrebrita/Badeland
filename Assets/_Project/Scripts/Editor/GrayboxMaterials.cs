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
        /// The animated cartoon sea (Badeland/Water shader). Opaque for the big sea, so the course clearly floats ON
        /// the water; see-through for small bodies of water. Falls back to a plain blue if the shader is not
        /// available or has an error, so the scene never ends up pink or empty.
        /// </summary>
        public static void ApplyWater(GameObject go, Color fallbackTint, bool opaque = false)
        {
            Shader shader = Shader.Find("Badeland/Water");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogWarning("Badeland: the water shader is not usable (not imported yet, or it has an error). Using a plain blue instead.");
                if (opaque) Tint(go, new Color(fallbackTint.r, fallbackTint.g, fallbackTint.b, 1f));
                else TintWater(go, fallbackTint);
                return;
            }

            Directory.CreateDirectory(Folder);
            string path = Folder + (opaque ? "/M_Badeland_Sea.mat" : "/M_Badeland_Water.mat");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader != shader)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (opaque)
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                mat.renderQueue = (int)RenderQueue.Geometry;
                mat.SetColor("_ShallowColor", new Color(0.12f, 0.66f, 1f, 1f));
                mat.SetColor("_DeepColor", new Color(0.03f, 0.34f, 0.9f, 1f));
            }
            else
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(mat);

            go.GetComponent<Renderer>().sharedMaterial = mat;
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
                // Copy the material Unity gave this new primitive: valid for whatever render pipeline is in use.
                Material source = renderer.sharedMaterial;
                if (source == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    if (shader == null) return;
                    mat = new Material(shader);
                }
                else
                {
                    mat = new Material(source);
                }

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
