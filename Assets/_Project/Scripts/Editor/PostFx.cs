using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Badeland.EditorTools
{
    public enum PostLook
    {
        /// <summary>Sunny waterpark: a little bloom, rich colours.</summary>
        Waterpark,
        /// <summary>Inside the creature: strong bloom so everything glowing bleeds soft light, deep vignette, purple-blue grade.</summary>
        Inside,
    }

    /// <summary>
    /// Adds the post-processing look (bloom, tone mapping, colour grading, vignette) to a scene being built. Only works
    /// when the project uses URP (see <see cref="UrpSetup"/>); otherwise it says so and does nothing.
    /// </summary>
    public static class PostFx
    {
        public static void Apply(Camera camera, PostLook look)
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.Log("Badeland: the project still uses the built-in renderer, so the glow (bloom) look is skipped. Run the menu Badeland > Render > Switch to URP (glow look), then rebuild this scene.");
                return;
            }

            string path = look == PostLook.Inside ? "Assets/_Project/Settings/PostProcessingInside.asset" : "Assets/_Project/Settings/PostProcessing.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var bloom = Get<Bloom>(profile);
            var tone = Get<Tonemapping>(profile);
            var adjust = Get<ColorAdjustments>(profile);
            var vignette = Get<Vignette>(profile);

            tone.mode.Override(TonemappingMode.ACES);
            bloom.highQualityFiltering.Override(true);

            if (look == PostLook.Inside)
            {
                bloom.threshold.Override(0.75f);
                bloom.intensity.Override(1.6f);
                bloom.scatter.Override(0.72f);
                bloom.tint.Override(new Color(0.92f, 0.9f, 1f));
                adjust.postExposure.Override(0.3f);
                adjust.contrast.Override(14f);
                adjust.saturation.Override(22f);
                adjust.colorFilter.Override(new Color(0.95f, 0.92f, 1f));
                vignette.intensity.Override(0.32f);
                vignette.smoothness.Override(0.5f);
                vignette.color.Override(new Color(0.05f, 0f, 0.12f));
            }
            else
            {
                bloom.threshold.Override(1f);
                bloom.intensity.Override(0.45f);
                bloom.scatter.Override(0.6f);
                adjust.postExposure.Override(0.15f);
                adjust.contrast.Override(10f);
                adjust.saturation.Override(18f);
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volumeObject = new GameObject("Post Processing");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            if (camera != null)
            {
                camera.allowHDR = true;
                var data = camera.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
