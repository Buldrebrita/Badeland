using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Switches the project from Unity's built-in renderer to URP, with settings chosen for Badeland: HDR (needed for
    /// glow), anti-aliasing, soft shadows, and Forward+ so the many coloured lights inside the creature all work.
    /// Menu: Badeland > Render > Switch to URP (glow look). After it, rebuild the scenes (the Badeland menu entries).
    /// </summary>
    public static class UrpSetup
    {
        const string Folder = "Assets/_Project/Settings/URP";

        [MenuItem("Badeland/Render/Switch to URP (glow look)")]
        public static void Switch()
        {
            Directory.CreateDirectory(Folder);
            string assetPath = Folder + "/BadelandURP.asset";
            string rendererPath = Folder + "/BadelandRenderer.asset";

            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                try
                {
                    var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, rendererPath);
                    asset = UniversalRenderPipelineAsset.Create(rendererData);
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Badeland: could not create the URP asset by script (" + e.Message + "). Do it by hand instead: right-click in the Project window > Create > Rendering > URP Asset (with Universal Renderer), then run this menu again. See docs/RENDER_PIPELINE.md.");
                    return;
                }
            }

            Tune(asset);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer != null) UseForwardPlus(renderer);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("Badeland: now using URP (" + assetPath + "). Next: rebuild the scenes from the Badeland menu (S3 course, S4 online, Chapter 2 rooms). Old materials are repaired while the scenes are rebuilt.");
        }

        [MenuItem("Badeland/Render/Switch back to the built-in renderer")]
        public static void SwitchBack()
        {
            GraphicsSettings.defaultRenderPipeline = null;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = null;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            Debug.Log("Badeland: back on the built-in renderer. Rebuild the scenes from the Badeland menu.");
        }

        // Settings that matter for the look. Each is set only if this Unity version has it.
        static void Tune(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", true);
            SetInt(so, "m_MSAA", 4);
            SetInt(so, "m_AdditionalLightsRenderingMode", 2);     // per pixel
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetFloat(so, "m_ShadowDistance", 90f);
            SetBool(so, "m_SupportsCameraDepthTexture", true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Forward+ lifts the limit on how many lights can shine on one object.
        static void UseForwardPlus(UniversalRendererData data)
        {
            var so = new SerializedObject(data);
            SetInt(so, "m_RenderingMode", 2);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        static void SetBool(SerializedObject so, string name, bool value) { var p = so.FindProperty(name); if (p != null) p.boolValue = value; }
        static void SetInt(SerializedObject so, string name, int value) { var p = so.FindProperty(name); if (p != null) p.intValue = value; }
        static void SetFloat(SerializedObject so, string name, float value) { var p = so.FindProperty(name); if (p != null) p.floatValue = value; }
    }
}
