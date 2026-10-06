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

        public static void Tint(GameObject go, Color color) => Apply(go, color, false);

        /// <summary>Semi-transparent material for water surfaces.</summary>
        public static void TintWater(GameObject go, Color color) => Apply(go, color, true);

        /// <summary>
        /// The animated cartoon sea (Badeland/Water shader). Falls back to a plain translucent blue if the shader is
        /// not available or has an error, so the scene never ends up pink.
        /// </summary>
        public static void ApplyWater(GameObject go, Color fallbackTint)
        {
            Shader shader = Shader.Find("Badeland/Water");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogWarning("Badeland: the water shader is not usable (not imported yet, or it has an error). Using a plain blue instead.");
                TintWater(go, fallbackTint);
                return;
            }

            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Badeland_Water.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader != shader)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void Apply(GameObject go, Color color, bool transparent)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/M_Graybox_" + (transparent ? "Water_" : "") + ColorUtility.ToHtmlStringRGBA(color) + ".mat";

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
